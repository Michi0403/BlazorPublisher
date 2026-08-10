# PublisherStudio 2.4.8 source validation

Source-only validation; no .NET restore/build/publish was performed.

- PASS — PublisherStudio.Web version is 2.4.8.
- PASS — PublisherStudio.InstallerConsole version is 2.4.8.
- PASS — documentation source version is 2.4.8.
- PASS — `refreshPanelStudioDesignSurface` exists as an ES-module export and as a `window.publisherStudio` Blazor interop bridge entry.
- PASS — Panel Studio lifecycle guard rejects a missing global design-surface refresh bridge.
- PASS — duplicate identical Panel Studio initialization errors are user-notification deduplicated.
- PASS — Media Studio Stop/Cancel/Dispose share immediate browser-capture track release.
- PASS — Panel and Print live-source renderers key instances by `(Id, SourceKind)` so obsolete capture components dispose on source-kind changes.
- PASS — each rendered live-source component owns a unique runtime capture id while preserving its publication model id for editor activation.
- PASS — changed browser module URLs carry the 2.4.8 cache key.
- PASS — JavaScript syntax checks completed for publisherInterop.js, mediaStudioInterop.js and streamingInterop.js.
- PASS — JavaScript diagnostics hashes were refreshed after maintained JS changes.
- PASS — targeted source-policy equivalents passed for async continuation baselines, text-service ownership, Panel Studio lifecycle wiring, JavaScript diagnostics hashes, XML project/target parsing and release-version consistency. The normal Windows owner build remains authoritative for the PowerShell/MSBuild guard chain.
