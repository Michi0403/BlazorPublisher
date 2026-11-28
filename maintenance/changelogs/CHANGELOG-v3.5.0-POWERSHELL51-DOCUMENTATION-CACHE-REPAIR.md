# PublisherStudio 3.5.0 — PowerShell 5.1 documentation-cache and Debug PDF contract repair

## Fixed

- Repairs the reproduced Windows build failure in `Save-PublisherStudioDocumentationHtmlCache`. The documentation cache copied each generated DocFX entry with a three-positional-argument `Join-Path` expression. PowerShell 6+ accepts the third argument through `AdditionalChildPath`, but the maintained Windows build invokes Windows PowerShell 5.1, where only `Path` and `ChildPath` are available positionally. The generated `api` directory therefore became the unsupported third positional argument after DocFX and the HTML accessibility/local-link preflight had already succeeded.
- Ports the already proven LocalGPT repair by nesting two-argument `Join-Path` calls for the cache destination. The resulting path is unchanged, while the expression remains valid on Windows PowerShell 5.1 and modern pwsh.
- Extends `Assert-PowerShellCompatibility.ps1` with the same AST-based `Join-Path` compatibility guard used by LocalGPT. Bare `Join-Path` calls with more than `Path + ChildPath` positionally are now rejected during the early compatibility preflight instead of near the end of documentation generation.
- Ports the immediately following LocalGPT Debug-documentation contract repair as well: `Directory.Build.targets` requests a complete PDF only for Release, so the final embedded-PDF assertion now runs only when `-RequirePdf` was requested or a complete PDF was actually generated. A normal Debug build can therefore finish with the already validated HTML/API/XML documentation instead of hitting the next known false PDF failure after the cache repair.

## Preserved

- DocFX metadata/build behavior, generated HTML accessibility and local-link validation, durable documentation caching, PDF generation/resume, and publication layout are unchanged apart from the compatible cache path construction and the corrected Debug-versus-Release PDF requirement gate.
- PublisherStudio application runtime behavior, persistence, Panel Studio, media/FFmpeg behavior, organic/1-Wire protocols, installer workflow, and deployment layout are unchanged.
- Existing routed `InteractiveServer` ownership and inherited child-component circuits are unchanged.

## Version

PublisherStudio Web and InstallerConsole move from `3.4.9` to `3.5.0`, following the maintained one-digit minor/patch-slot rollover policy.

## Validation boundary

The supplied Windows build is the authoritative reproduction: PublisherStudio compiled, DocFX completed with zero warnings/errors, 737 generated HTML files passed the accessibility/local-link preflight, and Windows PowerShell then failed while saving the validated HTML cache because the first `api` entry was bound as an unsupported third positional `Join-Path` argument. This environment does not provide Windows PowerShell or `pwsh`, and no `dotnet` build/publish is run here; validation is source/static and the next native Windows build remains the runtime gate.
