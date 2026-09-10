# PublisherStudio 3.5.7 — render affinity, logging, and installer repair

PublisherStudio 3.5.7 repairs the editor/runtime regressions reported against 3.5.6 without changing the canonical `%LOCALAPPDATA%\PublisherStudio` product root or undoing the cross-platform macOS packaging work.

- Story Editor no longer re-enters its RichEdit layout bridge from every `OnAfterRenderAsync` pass. The bridge is attached once per visible editor surface and is reset when the editor closes, preventing render/caret/layout feedback from repeatedly rescheduling browser layout work.
- Renderer-affine continuations found in maintained component helper flows were corrected to `ConfigureAwait(true)`; service/background continuations retain `ConfigureAwait(false)`. The maintained async-continuation audit remains authoritative.
- The file logger no longer creates one queue and writer thread per logging category. All category loggers share one bounded provider-owned sink and one writer thread, removing competing opens of `PublisherStudio.log`, exception amplification, hundreds of potential writer threads, and avoidable allocation/GC pressure.
- Development first-chance diagnostics explicitly ignore failures originating inside the file-log sink so diagnostics cannot recursively amplify logger I/O failures.
- Windows setup returned to the repository-defined LocalGPT-aligned extraction contract: `win*` and `setupwin*` archives extract into the same `%LOCALAPPDATA%\PublisherStudio` root after stopping only the owned packaged runtime. The unapproved transactional wrapper-replacement dialect was removed.
- `Build-Release.ps1` now restores and compiles the installer before the expensive documentation/PDF/notarization lane. Setup-only compiler failures therefore stop early instead of appearing after hours of release work.
- Existing macOS app identity, `server.json` rendezvous semantics, productbuild distribution PKG validation, signing/notarization, and alternate-host preservation are retained.
