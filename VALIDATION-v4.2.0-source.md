# PublisherStudio 4.2.0 — offline source validation

**Source of truth:** `PublisherStudio-4.1.9-source.zip`, not the earlier 4.0.2/4.0.3 release.

## Scope and method

The 4.1.9 archive was extracted and compared by relative filename and exact byte content. The migration removes precisely 132 `.py` files under `build/` and `.github/scripts/`, plus `build/PythonRuntime.Common.ps1`. All other originally supplied files remain present. The project versions are the only changes under `src/`; every other `src/` byte is identical to 4.1.9. In particular, `src/PublisherStudio.Web/Services/PanelStudioTextService.cs` retains SHA-256 `83411046351f58524cb648e624ecf5cfecb0f20d8a7abd1838e33045a3dd3ab3`.

Both `.csproj` versions were parsed as XML and verified as 4.2.0. A search confirms zero shipped `.py` files, zero active interpreter invocation in PowerShell/MSBuild/WSL/GitHub Actions entrypoints, and no unresolved literal repository-owned `.ps1` references. The LocalGPT helper's optional sibling-repository notice verifier was identified separately and is not a missing PublisherStudio script.

Verified source contracts include `Build-Release.ps1` referencing LocalGPT.ReleasePackaging 1.1.2, native `dotnet <helper>.dll` invocation for documentation/native packaging, Docs and Pages guards remaining connected, the 4.1.9 repository build-storage integration retained, and the Razor retention gate preserving the `RAZORUI0015` structural-renderer marker. A source-only replay of key Razor component boundaries across the 49 maintained components found no primary owner, boundary or popup-wrapper violations.

## Limitations

The packaging machine does not have `pwsh`, Windows PowerShell 5.1, the .NET SDK, DevExpress licensing/browser asset setup, macOS signing, or the associated release workloads. Consequently **no real PowerShell parser run, Debug compile, Release publish, DocFX PDF render, installer build or UI test occurred**. Native PowerShell checks are not equivalent by definition to Roslyn, a Razor compiler or complete live acceptance tests. Resolve any actual host errors with the full host log rather than bypassing the gate.

## First execution

The intended normal commands, with no separate Python, PDF tool installation or manual asset bootstrap, are:

```powershell
pwsh ./Build-LocalDevelopment.ps1 -Configuration Debug
pwsh ./Build-Release.ps1
```

Run each from the extracted repository root. A complete Debug and macOS/Windows Release test remains outstanding.
