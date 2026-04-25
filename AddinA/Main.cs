using System.IO;
using System.Runtime.Loader;
using Microsoft.Xaml.Behaviors;
using Microsoft.Xaml.Behaviors.Core;
using System.Windows.Markup;

namespace AddinA;

public static class Main
{
  public static void Start()
  {
    var behaviorAssembly = typeof(Interaction).Assembly;
    WriteInfo($"AddinA expects {behaviorAssembly.GetName().Name} from {AssemblyLoadContext.GetLoadContext(behaviorAssembly)?.Name}");

    var looseXamlPath = Path.Combine(Path.GetDirectoryName(typeof(Main).Assembly.Location)!, "SampleWindow.xaml");
    using var looseXaml = File.OpenRead(looseXamlPath);
    var parsed = XamlReader.Load(looseXaml);
    WriteInfo($"AddinA parser returned {parsed.GetType().Assembly.GetName().Name} from {AssemblyLoadContext.GetLoadContext(parsed.GetType().Assembly)?.Name}");
    _ = (ChangePropertyAction)parsed;
  }

  private static void WriteInfo(string message)
  {
    var previousColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.WriteLine(message);
    Console.ForegroundColor = previousColor;
  }
}
