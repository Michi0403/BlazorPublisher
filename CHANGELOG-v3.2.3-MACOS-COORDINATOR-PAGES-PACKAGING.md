# PublisherStudio 3.2.3 — DocFX cache and headless macOS packaging repair

- Advanced 3.2.2 to 3.2.3 under the repository's one-digit minor/patch version policy.
- Fixed the corrupt asynchronous yellow cleanup display by suppressing PowerShell file-provider progress in release cleanup, documentation cleanup, and native packaging while keeping the actual cleanup work intact.
- Release cleanup still deletes every repository-local `src/**/bin` and `src/**/obj` directory and now emits one deterministic completion line instead of per-file provider progress.
- Added a durable documentation payload cache outside repository `bin`/`obj`, keyed by version, compiled documentation assembly/XML, documentation source inputs, and documentation build/repair scripts.
- Commits validated DocFX HTML/API output to that durable cache before PDF rendering, so a PDF timeout or interrupted build can resume from the completed HTML tree instead of rebuilding the generated API site.
- Caches a successfully validated PDF beside the HTML payload and reuses it when the cache key still matches.
- Restored a 30-minute default DocFX per-navigation timeout on macOS and other hosts; `DOCFX_PDF_TIMEOUT` remains an explicit operator override.
- Added a shared PublisherStudio/LocalGPT PDF-render lock so the two large DocFX/Chromium PDF jobs do not run concurrently on the same machine.
- Removed Finder AppleEvent DMG layout automation that repeatedly timed out with macOS error `-1712`. DMGs are now created and verified headlessly with `hdiutil`, retaining the `.app`, Applications alias, and bundled background artwork asset.
- Replaced component-inferred PKG construction with an explicit `/Applications/PublisherStudio.app` root payload and validates the finished package layout using `pkgutil --payload-files`; an invalid or uninspectable PKG is removed instead of being accepted as a release artifact.
- Preserved the existing macOS launcher, icon/signing, Pages HTML-only snapshot behavior, cross-platform coordinator, Linux packaging, optional WSL2 path, editor behavior, and explicit InteractiveServer component boundaries.
