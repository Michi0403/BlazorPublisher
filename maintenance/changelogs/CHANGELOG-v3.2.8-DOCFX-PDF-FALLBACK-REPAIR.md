# PublisherStudio 3.2.8 — DocFX PDF fallback repair

## Fixed

- Repaired the cached-HTML PDF fallback in `build/Build-Documentation.ps1`. Durable HTML can be reused without restoring DocFX, so the PDF fallback now resolves the repository-local DocFX tool lazily before invocation.
- If the repository-local tool cannot be restored, the existing isolated pinned DocFX 2.78.5 tool path is resolved or installed before the PDF command is invoked. `Invoke-PublisherStudioDocfx` therefore no longer receives a null command target.
- Lowered the browser-print source-page limit from 1500 to 600. The current 732-page documentation goes directly to the DocFX PDF plug-in instead of first attempting the failing monolithic Microsoft Edge print on macOS.
- The direct DocFX path now prints an explicit host message when the page threshold is exceeded.

## Preserved

- PublisherStudio application behavior is unchanged.
- The 3.2.7 `ApplicationPathService` method-diagnostics repair remains intact.
- Future2/open-source positioning and DevExpress licensing clarification remain intact.
- macOS architecture/Rosetta diagnostics, exact Mach-O architecture manifests, native packaging, launcher behavior, durable documentation caching, and transient staging cleanup remain unchanged.
- The reviewed `InteractiveServer` render-mode map remains unchanged.

## Version

- Version advanced from 3.2.7 to 3.2.8 because the release/documentation script changed.
