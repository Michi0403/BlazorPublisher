# PublisherStudio 3.2.8

PublisherStudio 3.2.8 repairs the durable-HTML documentation PDF fallback exposed by the 3.2.7 macOS release build.

A validated DocFX HTML payload can be restored from PublisherStudio's durable cache without restoring the repository-local DocFX command. If the subsequent Microsoft Edge monolithic print failed, the fallback then attempted to invoke an unresolved command target and PowerShell stopped with `The expression after '&' ... was not valid`. The PDF path now resolves/restores DocFX lazily before invoking the plug-in, with the existing pinned 2.78.5 isolated tool path retained as the secondary fallback.

The browser-print source-page limit is now 600. The current 732-page PublisherStudio documentation therefore goes directly to the DocFX PDF plug-in instead of spending time on a macOS Edge print that has already failed at that size. Smaller documentation sets can still use the compact browser-print path.

All 3.2.7 method-diagnostics repair, 3.2.6 macOS architecture diagnostics, working installed-app launcher behavior, Future2 positioning, DevExpress licensing clarification, durable documentation cache, staging cleanup, headless DMG/PKG packaging, and reviewed InteractiveServer boundaries remain intact.

See `CHANGELOG-v3.2.8-DOCFX-PDF-FALLBACK-REPAIR.md` and `VALIDATION-v3.2.8-source.md`.
