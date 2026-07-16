# Validation status

The source tree was checked without restoring proprietary packages.

## Completed checks

- JSON configuration files parse successfully.
- Project files are valid XML.
- JavaScript passes `node --check`.
- C# and Razor files pass lexical delimiter scans.
- Razor component tags pass a structural balance scan.
- Direct `@page.` Razor identifier collisions are absent.
- CSS delimiter structure is balanced.
- The dependency scan finds only `DevExpress.Blazor` and `DevExpress.Blazor.RichEdit`, both constrained to `25.2.*`.
- No GIMP/Inkscape source, package, executable, or asset is included.
- No `bin`, `obj`, `.vs`, compiled assemblies, symbols, DevExpress binaries, license keys, databases, or AI/Ollama/EF/WinUI dependencies are included.

## Not completed in the generation environment

A real `dotnet restore` and `dotnet build` could not be run because this environment has neither the .NET 10 SDK nor access to the licensed DevExpress package feed. Static validation cannot replace your compiler.

Run the authoritative validation on a machine with the DevExpress feed configured:

```powershell
dotnet restore PublisherStudio.sln
dotnet build PublisherStudio.sln -c Debug
dotnet run --project src/PublisherStudio.Web
```

When reporting a failure, include the first compiler error and the affected source line. Later errors are often cascading Razor diagnostics.
