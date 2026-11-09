# PublisherStudio 4.7.3 — offline validation report

Source baseline: supplied PublisherStudio 4.7.2 repository ZIP.

- `PublisherStudio.Web.csproj` and `PublisherStudio.InstallerConsole.csproj` are both at 4.7.3.
- All `tests/js/*.test.cjs` pass under Node 22, including the new `onewire-traffic-journal-473.test.cjs`.
- Python emulations of these guards report no findings over the whole source tree:
  - `Assert-ServiceMethodResilience`
  - `Assert-LoggingIntegrity`
  - `Assert-TextServiceOwnership`
  - `Assert-SystemVariableInitialization`
  - `Assert-LocalizationIntegrity`: identical keys across six catalogs, every `LT(...)` literal present in en-US, no new German-identical values.
- `await security.UnprotectIncomingAsync(envelope, cancellationToken)` is retained verbatim in `LocalGptConnectionService` (ONWIRE-READY contract).
- The new component has no native interactive tags, `<section>`, `<dialog>` or `<details>`. Its `.razor.cs` file is a documentation-only partial.

**Environment restriction:** `pwsh`, Windows PowerShell and `dotnet` are unavailable here. A Visual Studio build, Razor generation and a live 1-Wire session have not been verified; the operator's build is authoritative.
