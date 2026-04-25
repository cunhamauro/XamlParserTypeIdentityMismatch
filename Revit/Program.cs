using System.IO;
using System.Reflection;
using System.Runtime.Loader;

internal class Program
{
  [STAThread]
  private static void Main(string[] args)
  {
    var root = FindSolutionRoot();
    var configuration = GetConfiguration();
    var addinAPath = Path.Combine(root, "AddinA", "bin", configuration, "net8.0-windows", "AddinA.dll");
    var addinBPath = Path.Combine(root, "AddinB", "bin", configuration, "net8.0-windows", "AddinB.dll");

    var addinAResult = RunAddin("AddinA", addinAPath);
    var addinBResult = RunAddin("AddinB", addinBPath);

    Console.WriteLine();
    WriteColored(
      $"Result: AddinA={(addinAResult ? "OK" : "FAILED")}, AddinB={(addinBResult ? "OK" : "FAILED")}",
      addinAResult && addinBResult ? ConsoleColor.Green : ConsoleColor.Red);

    if (!addinAResult || !addinBResult)
    {
      Environment.ExitCode = 1;
    }
  }

  private static bool RunAddin(string addinName, string assemblyPath)
  {
    Console.WriteLine();
    WriteInfo($"Loading {addinName} from {assemblyPath}");

    try
    {
      var alc = new AddinLoadContext(addinName, assemblyPath);
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

  private static void InvokeStart(Assembly assembly, string addinName)
  {
    var mainType = assembly.GetType($"{addinName}.Main", throwOnError: true)!;
    var startMethod = mainType.GetMethod("Start", BindingFlags.Public | BindingFlags.Static)
      ?? throw new MissingMethodException(mainType.FullName, "Start");

    startMethod.Invoke(null, null);
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

internal sealed class AddinLoadContext : AssemblyLoadContext
{
  private readonly AssemblyDependencyResolver resolver;

  public AddinLoadContext(string addinName, string mainAssemblyPath)
    : base($"{addinName}-ALC", isCollectible: false)
  {
    resolver = new AssemblyDependencyResolver(mainAssemblyPath);
  }

  protected override Assembly? Load(AssemblyName assemblyName)
  {
    var assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
    return assemblyPath is null ? null : LoadFromAssemblyPath(assemblyPath);
  }
}
