using System.IO;
using System.Reflection;
using System.Runtime.Loader;

internal class Program
{
	private static readonly Dictionary<string, AddinLoadContext> LoadContextsByAddin = new();
	private static readonly Dictionary<string, Assembly> SampleLibrariesByAddin = new();
	private static Assembly? firstSampleLibrary;

	// This controls if Addins are loaded from the start into Default ALC or isolated ALC
	// Doesn't make any difference
	private static bool LoadAddinsInDefaultALC = false;

	// This controls if libraries are loaded into Default ALC or respective addin's isolated ALC
	// If false => Results in type identity mismatch
	// If true => Single instance is loaded and shared by all addins, no type identity mismatch
	private static bool LoadSampleLibrariesInDefaultALC = false;

	// This controls if the WPF windows are instantiated with contextual reflection or not
	// Doesn't make any difference in the outcome of this conflict, only difference is that it
	private static bool UseContextualReflection = true;

	// When there are two exact same libraries loaded into different ALCs
	// And there is XAML which type references an assembly, it results in a mismatch because
	// The XAML parser doesn't know which assembly to bind to and picks the first one it finds

	[STAThread]
	private static void Main()
	{
		var root = FindSolutionRoot();
		var configuration = GetConfiguration();
		var addinAPath = Path.Combine(root, "AddinA", "bin", configuration, "net8.0-windows", "AddinA.dll");
		var addinBPath = Path.Combine(root, "AddinB", "bin", configuration, "net8.0-windows", "AddinB.dll");

		AppDomain.CurrentDomain.AssemblyResolve += ResolveLikeHostGlobalResolver;

		PreloadSampleLibrary("AddinA", addinAPath);
		PreloadSampleLibrary("AddinB", addinBPath);

		bool addinAResult, addinBResult;

		if (LoadAddinsInDefaultALC)
		{
			addinAResult = RunAddin("AddinA", addinAPath);
			addinBResult = RunAddin("AddinB", addinBPath);
		}
		else
		{
			addinAResult = RunAddinInALC("AddinA", addinAPath);
			addinBResult = RunAddinInALC("AddinB", addinBPath);
		}

		Console.WriteLine();
		WriteColored(
		  $"Result: AddinA={(addinAResult ? "OK" : "FAILED")}, AddinB={(addinBResult ? "OK" : "FAILED")}",
		  addinAResult && addinBResult ? ConsoleColor.Green : ConsoleColor.Red);

		if (!addinAResult || !addinBResult)
		{
			Environment.ExitCode = 1;
		}
	}

	private static AddinLoadContext PreloadSampleLibrary(string addinName, string addinAssemblyPath)
	{
		if (LoadContextsByAddin.TryGetValue(addinName, out var existingAlc))
		{
			return existingAlc;
		}

		var addinDirectory = Path.GetDirectoryName(addinAssemblyPath)!;
		var sampleLibraryPath = Path.Combine(addinDirectory, "SampleXamlLibrary.dll");
		var alc = new AddinLoadContext(addinName, addinAssemblyPath);
		var assembly = LoadSampleLibrariesInDefaultALC
		  ? LoadSampleLibraryInDefault(sampleLibraryPath)
		  : alc.LoadFromAssemblyPath(sampleLibraryPath);

		LoadContextsByAddin[addinName] = alc;
		SampleLibrariesByAddin[addinName] = assembly;
		firstSampleLibrary ??= assembly;

		WriteInfo($"{addinName} preloaded {FormatAssembly(assembly)} from {AssemblyLoadContext.GetLoadContext(assembly)?.Name}");
		return alc;
	}

	private static Assembly LoadSampleLibraryInDefault(string sampleLibraryPath)
	{
		var assemblyName = AssemblyName.GetAssemblyName(sampleLibraryPath);
		var existingAssembly = AssemblyLoadContext.Default.Assemblies.FirstOrDefault(assembly =>
		  AssemblyName.ReferenceMatchesDefinition(assembly.GetName(), assemblyName));

		if (existingAssembly is not null)
		{
			WriteInfo($"Default ALC already has {FormatAssembly(existingAssembly)} from {existingAssembly.Location}");
			return existingAssembly;
		}

		return AssemblyLoadContext.Default.LoadFromAssemblyPath(sampleLibraryPath);
	}

	private static bool RunAddin(string addinName, string assemblyPath)
	{
		Console.WriteLine();
		WriteInfo($"Loading {addinName} into Default ALC from {assemblyPath}");

		try
		{
			var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);

			WriteInfo($"{addinName} assembly ALC: {AssemblyLoadContext.GetLoadContext(assembly)?.Name}");
			InvokeStart(assembly, addinName);
			WriteSuccess($"{addinName}: started successfully");
			return true;
		}
		catch (TargetInvocationException ex) when (ex.InnerException is not null)
		{
			WriteFailure($"{addinName}: failed");
			WriteFailure(ex.InnerException.ToString());
			return false;
		}
		catch (Exception ex)
		{
			WriteFailure($"{addinName}: failed");
			WriteFailure(ex.ToString());
			return false;
		}
	}

	private static bool RunAddinInALC(string addinName, string assemblyPath)
	{
		Console.WriteLine();
		WriteInfo($"Loading {addinName} into its own ALC from {assemblyPath}");

		var alc = GetPreloadedLoadContext(addinName);

		try
		{
			var assembly = alc.LoadFromAssemblyPath(assemblyPath);

			WriteInfo($"{addinName} assembly ALC: {AssemblyLoadContext.GetLoadContext(assembly)?.Name}");
			InvokeStart(assembly, addinName);
			WriteSuccess($"{addinName}: started successfully");
			return true;
		}
		catch (TargetInvocationException ex) when (ex.InnerException is not null)
		{
			WriteFailure($"{addinName}: failed");
			WriteFailure(ex.InnerException.ToString());
			return false;
		}
		catch (Exception ex)
		{
			WriteFailure($"{addinName}: failed");
			WriteFailure(ex.ToString());
			return false;
		}
	}

	private static AddinLoadContext GetPreloadedLoadContext(string addinName)
	{
		return LoadContextsByAddin.TryGetValue(addinName, out var alc)
		  ? alc
		  : throw new InvalidOperationException($"Could not find preloaded ALC for {addinName}.");
	}

	private sealed class AddinLoadContext : AssemblyLoadContext
	{
		private readonly string addinName;
		private readonly AssemblyDependencyResolver resolver;

		public AddinLoadContext(string addinName, string mainAssemblyPath)
		  : base($"{addinName}-ALC", isCollectible: false)
		{
			this.addinName = addinName;
			resolver = new AssemblyDependencyResolver(mainAssemblyPath);
		}

		protected override Assembly? Load(AssemblyName assemblyName)
		{
			if (assemblyName.Name == "SampleXamlLibrary" &&
				SampleLibrariesByAddin.TryGetValue(addinName, out var sampleLibrary))
			{
				return sampleLibrary;
			}

			var assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
			return assemblyPath is null ? null : LoadFromAssemblyPath(assemblyPath);
		}

		protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
		{
			var libraryPath = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
			return libraryPath is null ? IntPtr.Zero : LoadUnmanagedDllFromPath(libraryPath);
		}
	}

	private static Assembly? ResolveLikeHostGlobalResolver(object? sender, ResolveEventArgs args)
	{
		if (args.Name is null || new AssemblyName(args.Name).Name != "SampleXamlLibrary")
		{
			return null;
		}

		var requestingAssemblyName = args.RequestingAssembly?.GetName().Name;
		if (requestingAssemblyName is not null &&
			SampleLibrariesByAddin.TryGetValue(requestingAssemblyName, out var addinSpecificAssembly))
		{
			WriteInfo($"Resolver returned {requestingAssemblyName}'s {FormatAssembly(addinSpecificAssembly)} from {AssemblyLoadContext.GetLoadContext(addinSpecificAssembly)?.Name}");
			return addinSpecificAssembly;
		}

		WriteInfo($"Resolver returned first global {FormatAssembly(firstSampleLibrary)} for requester {requestingAssemblyName ?? "<null>"}");
		return firstSampleLibrary;
	}

	private static void InvokeStart(Assembly assembly, string addinName)
	{
		var mainType = assembly.GetType($"{addinName}.Main", throwOnError: true)!;
		var startMethod = mainType.GetMethod(
			"Start",
			BindingFlags.Public | BindingFlags.Static)
			?? throw new MissingMethodException(mainType.FullName, "Start");

		startMethod.Invoke(null, new object[] { UseContextualReflection });
	}

	private static string FormatAssembly(Assembly? assembly)
	{
		if (assembly is null)
		{
			return "<none>";
		}

		var name = assembly.GetName();
		return $"{name.Name}, Version={name.Version}";
	}

	private static void WriteInfo(string message) => WriteColored(message, ConsoleColor.Blue);

	private static void WriteSuccess(string message) => WriteColored(message, ConsoleColor.Green);

	private static void WriteFailure(string message) => WriteColored(message, ConsoleColor.Red);

	private static void WriteColored(string message, ConsoleColor color)
	{
		var previousColor = Console.ForegroundColor;
		Console.ForegroundColor = color;
		Console.WriteLine(message);
		Console.ForegroundColor = previousColor;
	}

	private static string FindSolutionRoot()
	{
		var directory = new DirectoryInfo(AppContext.BaseDirectory);

		while (directory is not null)
		{
			if (File.Exists(Path.Combine(directory.FullName, "XamlParserTypeIdentityMismatch.slnx")))
			{
				return directory.FullName;
			}

			directory = directory.Parent;
		}

		throw new DirectoryNotFoundException("Could not find XamlParserTypeIdentityMismatch.slnx above the host output directory.");
	}

	private static string GetConfiguration()
	{
#if DEBUG
		return "Debug";
#else
    return "Release";
#endif
	}
}
