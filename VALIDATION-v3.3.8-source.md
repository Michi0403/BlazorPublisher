# PublisherStudio 3.3.8 source validation

This validation mirrors the LocalGPT fix because PublisherStudio contained the same release-validator mismatch: `Build-Documentation.ps1` can emit `html-browser-chunked`, while the previous `Build-Release.ps1` allow-list did not include it.

Source checks completed in this environment:

- version policy and current-version references: passed for 3.3.8; minor/patch remain single digits;
- current release audit: passed;
- `Build-Release.ps1`: accepts `html-browser-chunked` and continues to enforce HTML preflight, PDF accessibility policy, source-page/API completeness, physical API output, PDF identity/size metadata, and sane-size ceiling;
- `build/Build-Documentation.ps1`: post-processing recognizes chunked browser PDFs as browser-backed for accessibility fallback semantics;
- InteractiveServer source audit: all four routed application pages remain `@rendermode InteractiveServer`, `Error.razor` remains the intentional static fatal-error fallback, and child/editor components inherit those page circuits;
- package-only LocalGPT.ReleasePackaging ownership and existing release/resume/signing architecture guards remain present.

No .NET or PowerShell runtime is available in this environment, so no `dotnet` build/publish and no `pwsh Build-Release.ps1` execution is claimed. The source-level regression audits are the available verification here.
