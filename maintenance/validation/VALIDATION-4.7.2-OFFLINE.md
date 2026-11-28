# PublisherStudio 4.7.2 — offline validation report

Source baseline: supplied PublisherStudio 4.7.1 repository ZIP.

- Project version increased consistently in `PublisherStudio.Web.csproj` and `PublisherStudio.InstallerConsole.csproj` to 4.7.2.
- `OrganicPlugins.razor:146` uses the same explicitly typed `bool` lambda form already used by other DevExpress checkboxes in the repository.
- `ConnectByWebUrlAsync` has method-local try/catch plus structured logging; original validation and 1-Wire connection statements are retained.
- Origin splitting and error message joining now belong to the existing injected `PublicationEditorTextService`.
- Mandatory early 1-Wire preflight rejects reintroduction of the old direct checkbox callback and direct component string processing.
- Confirmed no LocalGPT sources, DevExpress package references, security gates or 1-Wire DTOs were modified.

**Environment restriction:** `pwsh`, Windows PowerShell and `dotnet` are unavailable in this source editing environment. A completed Visual Studio build and actual Razor generator compilation have not been verified; the operator's build is authoritative.
