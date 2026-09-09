# PublisherStudio 3.5.0 source validation

## Scope

This handoff is validated from the supplied PublisherStudio 3.4.9 source ZIP only. No `dotnet build`, `dotnet publish`, GitHub access, remote-repository access, or native release packaging is performed. The user's Windows build remains the authoritative compiler/runtime gate.

## Reproduced failure addressed

The supplied Windows Debug build successfully compiled PublisherStudio, generated the complete DocFX site, and passed generated HTML accessibility/local-link validation. It then failed in `Save-PublisherStudioDocumentationHtmlCache` with `PositionalParameterNotFound` for `api`.

The failing expression was:

`Join-Path $temporary 'site' $entry.Name`

That expression relies on PowerShell 6+'s `AdditionalChildPath` behavior. `Directory.Build.targets` invokes `powershell.exe`, so Windows PowerShell 5.1 receives `api` as an unsupported third positional argument. PublisherStudio 3.5.0 ports the proven LocalGPT repair and uses nested two-argument `Join-Path` calls instead.

The same LocalGPT failure sequence exposed a second Debug-only contract mismatch immediately after the cache repair: MSBuild enables `-RequirePdf` only for Release, while the documentation script used to perform an unconditional final embedded-PDF assertion. PublisherStudio had the same latent shape, so 3.5.0 also ports that proven follow-up instead of requiring another long documentation build to rediscover it.

## 3.5.0 regression checks

The release-specific source audit verifies that:

- Web and InstallerConsole report version `3.5.0`, respecting the maintained one-digit minor/patch-slot policy;
- package/documentation/release metadata reports `3.5.0`;
- `Save-PublisherStudioDocumentationHtmlCache` uses nested two-argument `Join-Path` calls;
- the previous three-positional-argument cache expression is absent;
- `Assert-PowerShellCompatibility.ps1` stores the parsed script AST and rejects bare `Join-Path` calls with more than two positional arguments, explicitly guarding Windows PowerShell 5.1's lack of `AdditionalChildPath`;
- `Directory.Build.targets` still requests the complete PublisherStudio documentation PDF only for Release by default;
- the final runtime/source-tree PDF assertion is gated by `if ($RequirePdf -or $pdfGenerated)`, so HTML-only Debug builds remain valid while Release still requires the versioned PDF;
- the 3.4.9 Organic Plugins naming/localization repair remains present;
- localization catalog parity and routed `InteractiveServer` ownership remain unchanged;
- no `bin` or `obj` source artifacts are packaged.

## Executed source checks

- `build/audit_release_3_5_0.py` — passed: version surfaces, one-digit version-slot policy, Windows PowerShell 5.1 nested `Join-Path` cache repair, early AST compatibility guard, Debug-versus-Release PDF contract, inherited 3.4.9 UI/localization repair, six-catalog parity, routed render ownership, and package hygiene.
- `build/audit_application_architecture.py --root . --product publisherstudio --mode all` — passed.
- `build/audit_cross_platform_boundaries.py` — passed: 60 checks; no platform leaks detected.
- `build/audit_async_continuations.py --source-root src/PublisherStudio.Web` — passed for 80 source files: 1102 await tokens, 465 `ConfigureAwait(false)`, 583 renderer-affine `ConfigureAwait(true)`, 49 explicitly configured await-using disposals, and 5 configured async streams.
- `build/audit_prerender_interop_safety.py --root .` — passed: 2687 component methods checked; 13 JavaScript-aware disposal methods are attachment-gated.
- `build/audit_component_resilience.py --root .` — passed: 2687 component methods own method-local diagnostics boundaries; no legacy exemptions.
- `build/audit_iterator_exception_policy.py --root .` — passed: 3 iterator/yield methods use the maintained logged try/finally policy; no exemptions.
- `build/audit_service_resilience.py --root . --product publisherstudio` — passed: 1377 service methods own try/catch + diagnostics; 3 iterator/yield methods own try/finally + diagnostics.
- `build/Assert-XmlDocumentationCoverage.py src` — passed: 6313 direct C# declarations across 252 maintained source files and 3444 direct Razor `@code` members across 48 component types.

Direct routed-page ownership check: 5 routed pages total, 4 explicit routed `InteractiveServer` boundaries, with only `Error.razor` intentionally static.

## Limitations

This environment provides neither `powershell` nor `pwsh`, so the PowerShell compatibility guard itself is not claimed as executed here. These checks also do not prove C# or Razor compilation. The next Windows build is the authoritative runtime confirmation of the PowerShell 5.1 cache repair and the Debug documentation contract.
