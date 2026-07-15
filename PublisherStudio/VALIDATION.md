# Validation status

The generated source tree was checked without restoring proprietary packages.

## Completed checks

- JSON configuration files parse successfully.
- Project files are valid XML.
- JavaScript passes `node --check`.
- C# and Razor code passed lexical delimiter scans.
- Razor component tags are balanced.
- CSS delimiter structure is balanced.
- The dependency scan finds only `DevExpress.Blazor` and `DevExpress.Blazor.RichEdit`, both constrained to version `25.2.*`.
- No `bin`, `obj`, `.vs`, compiled assemblies, symbols, DevExpress binaries, license keys, databases, or AI/Ollama/EF/WinUI dependencies are included.

## Not completed in the generation environment

A real `dotnet restore` and `dotnet build` could not be run because this environment has neither the .NET 10 SDK nor access to the licensed DevExpress package feed. Static validation cannot replace the compiler.

Run the authoritative validation on a machine with your DevExpress feed configured:

```powershell
dotnet restore PublisherStudio.sln
dotnet build PublisherStudio.sln -c Debug
dotnet run --project src/PublisherStudio.Web
```

The first compiler run is also the right place to detect a signature difference in a specific DevExpress 25.2 patch release.
