# XAML parser type-identity mismatch

This repository explores a WPF BAML/XAML type-resolution problem in applications that use multiple `AssemblyLoadContext` (ALC) instances.

Read more about this @:
- https://github.com/dotnet/wpf/issues/1700
- https://github.com/dotnet/wpf/issues/11848
- https://github.com/dotnet/wpf/pull/11849
- https://forums.autodesk.com/t5/revit-api-forum/revit-2027-manifestsettings-deduplication-mechanism-from-revit/td-p/14105252

## Projects

| Project | Role |
| --- | --- |
| `Revit` | Mock host executable for the reproduction. |
| `AddinA`, `AddinB` | WPF add-ins loaded by the host. |
| `SampleXamlLibraryForAddinA`, `SampleXamlLibraryForAddinB` | Libraries used by the add-ins. |

## Prerequisites

- Windows and the .NET 8 SDK used by the WPF build.

## Build and run

```powershell
dotnet build .\XamlParserTypeIdentityMismatch.slnx
dotnet run --project .\Revit\Revit.csproj
```

The most relevant switch in [Revit/Program.cs](Revit/Program.cs) is `LoadSampleLibrariesInDefaultALC`,
because it controls the type-identity mismatch exercised by this reproduction.
When set to `false`, each add-in loads its library into its own isolated ALC.
The application domain then contains two instances of an assembly with the same name and version, loaded in different ALCs.
In this configuration, the BAML parser resolves the type reference to the most recently loaded matching assembly by name and version.
Consequently, the resolved type matches one add-in's library but mismatches the other add-in's library.

The host logs the ALCs involved in BAML type resolution and template lookup.
