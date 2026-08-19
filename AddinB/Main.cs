using System.Runtime.Loader;
using SampleXamlLibrary;

namespace AddinB;

public static class Main
{
	public static void Start(bool enterContextualReflection)
	{
		if (enterContextualReflection)
		{
			StartWithContextualReflection();
		}
		else
		{
			StartWithoutContextualReflection();
		}
	}

	private static void StartWithoutContextualReflection()
	{
		WriteInfo(
			$"Contextual ALC = " +
			$"{AssemblyLoadContext.CurrentContextualReflectionContext?.Name ?? "<none>"}");

		WriteInfo(
			$"Main ALC = " +
			$"{AssemblyLoadContext.GetLoadContext(typeof(Main).Assembly)?.Name}");

		WriteInfo(
			$"ViewModel ALC = " +
			$"{AssemblyLoadContext.GetLoadContext(typeof(ViewModel).Assembly)?.Name}");

		var window = new SampleWindow();
		var templateAssembly = window.TemplateDataTypeAssembly;

		WriteInfo(
			$"XAML DataType ALC = " +
			$"{AssemblyLoadContext.GetLoadContext(templateAssembly)?.Name}");

		WriteInfo($"AddinB template DataType resolved {FormatAssembly(templateAssembly)} from {FormatLoadContext(templateAssembly)}");

		var contentAssembly = window.CurrentContentAssembly;
		WriteInfo($"AddinB content ViewModel is {FormatAssembly(contentAssembly)} from {AssemblyLoadContext.GetLoadContext(contentAssembly)?.Name}");
		var hasTemplate = window.HasTemplateForCurrentContent();
		WriteInfo($"AddinB implicit template match: {hasTemplate}");

		var expectedAssembly = typeof(ViewModel).Assembly;
		WriteInfo($"AddinB expects {FormatAssembly(expectedAssembly)} from {AssemblyLoadContext.GetLoadContext(expectedAssembly)?.Name}");
		window.Close();

		if (!hasTemplate)
		{
			throw new InvalidOperationException($"No implicit DataTemplate matched {window.CurrentContent}!");
		}
	}

	private static void StartWithContextualReflection()
	{
		var thisALC = AssemblyLoadContext.GetLoadContext(typeof(Main).Assembly);
		using (thisALC!.EnterContextualReflection())
		{
			WriteInfo(
				$"Contextual ALC = " +
				$"{AssemblyLoadContext.CurrentContextualReflectionContext?.Name ?? "<none>"}");

			WriteInfo(
				$"Main ALC = " +
				$"{AssemblyLoadContext.GetLoadContext(typeof(Main).Assembly)?.Name}");

			WriteInfo(
				$"ViewModel ALC = " +
				$"{AssemblyLoadContext.GetLoadContext(typeof(ViewModel).Assembly)?.Name}");

			var window = new SampleWindow();
			var templateAssembly = window.TemplateDataTypeAssembly;

			WriteInfo(
				$"XAML DataType ALC = " +
				$"{AssemblyLoadContext.GetLoadContext(templateAssembly)?.Name}");

			WriteInfo($"AddinB template DataType resolved {FormatAssembly(templateAssembly)} from {FormatLoadContext(templateAssembly)}");

			var contentAssembly = window.CurrentContentAssembly;
			WriteInfo($"AddinB content ViewModel is {FormatAssembly(contentAssembly)} from {AssemblyLoadContext.GetLoadContext(contentAssembly)?.Name}");
			var hasTemplate = window.HasTemplateForCurrentContent();
			WriteInfo($"AddinB implicit template match: {hasTemplate}");

			var expectedAssembly = typeof(ViewModel).Assembly;
			WriteInfo($"AddinB expects {FormatAssembly(expectedAssembly)} from {AssemblyLoadContext.GetLoadContext(expectedAssembly)?.Name}");
			window.Close();

			if (!hasTemplate)
			{
				throw new InvalidOperationException($"No implicit DataTemplate matched {window.CurrentContent}!");
			}
		}
	}

	private static string FormatAssembly(System.Reflection.Assembly? assembly)
	{
		if (assembly is null)
		{
			return "<none>";
		}

		var name = assembly.GetName();
		return $"{name.Name}, Version={name.Version}";
	}

	private static string FormatLoadContext(System.Reflection.Assembly? assembly)
	{
		return assembly is null ? "<none>" : AssemblyLoadContext.GetLoadContext(assembly)?.Name ?? "<none>";
	}

	private static void WriteInfo(string message)
	{
		var previousColor = Console.ForegroundColor;
		Console.ForegroundColor = ConsoleColor.Blue;
		Console.WriteLine(message);
		Console.ForegroundColor = previousColor;
	}
}
