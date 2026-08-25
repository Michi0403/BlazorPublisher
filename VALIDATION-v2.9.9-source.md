# PublisherStudio 2.9.9 source validation

**SOURCE-NOT-COMPILED.** This release was prepared without running `dotnet`, MSBuild, NuGet restore/publish, Visual Studio compilation or a licensed DevExpress build. The user's Windows build remains the authoritative compile/runtime gate.

## Reported regression addressed

The reproduced 2.9.8 workflow retained successive screen recordings successfully, but the first retained browser Blob could remain outside the canonical Video Studio sequence. Starting another `MediaRecorder` could then release that retained Blob, making the new recording appear to replace the old one. This was a sequence-ownership problem rather than a capture failure.

PublisherStudio 2.9.9 closes that path by requiring every completed video recording to acquire an explicit canonical sequence position before another capture can silently replace its retained browser media. Subsequent recording starts expose first/between/last placement before the browser capture picker opens, and insertion continues through `MediaTimelineEditService` / `TimelineEdits.InsertAt` rather than a parallel ordering model.

## Media-workbench enhancement

Video Studio now reuses maintained Panel / Div viewport presets alongside common video standards and custom dimensions. Capture choices include landscape, vertical and square presets up through 8K within the existing runtime policy. Frame-rate choices include cinema/NTSC fractional rates and high-refresh presets through 240 FPS. Browser-side option normalization preserves fractional values instead of truncating them.

The 2.9.8 quality work remains intact: source/native recordings remain downloadable at original browser-retained size, no-effect native render can preserve source bytes, and effect rendering remains decoded-frame-driven rather than returning to an unspecified zero-rate canvas cadence.

## Source checks

The final source gate ran the repository's maintained static checks that do not require .NET compilation:

- `build/audit_application_architecture.py --root . --product publisherstudio --mode all`
- `build/audit_async_continuations.py --source-root src/PublisherStudio.Web`
- `build/audit_component_resilience.py --root .`
- `build/audit_prerender_interop_safety.py --root .`
- `build/audit_service_resilience.py --root . --product publisherstudio`
- `build/audit_iterator_exception_policy.py --root .`
- `build/audit_panelstudio_persistence.py`
- `build/Assert-XmlDocumentationCoverage.py .`
- Node `--check` for all maintained `src/PublisherStudio.Web/wwwroot/js/*.js`
- exact six-catalog localization parity / JSON validation
- exact maintained JavaScript diagnostics hash validation through the 2.9.9 release audit
- `build/audit_release_2_9_9.py`

## Final static-audit results

- application architecture policy: passed
- async continuation policy: **79 source files / 1,093 await tokens**; 461 `ConfigureAwait(false)`, 578 renderer-affine `ConfigureAwait(true)`, 49 explicitly configured async disposals, 5 configured async streams
- component method resilience: **2,679 component methods**, zero legacy exemptions
- prerender JavaScript interop safety: **2,679 component methods**, 13 JavaScript-aware disposal methods attachment-gated
- service resilience: **1,345 service methods** plus 4 maintained iterator/yield methods, zero exemptions/skips
- iterator exception policy: passed
- Panel Studio persistence/source architecture audit: passed
- C# XML documentation: **6,186 declarations / 248 source files**
- Razor XML documentation: **48 component types / 3,420 direct `@code` members**
- localization: **3,370 keys in exact parity across all six catalogs**
- JavaScript diagnostics/hash coverage: **16 maintained browser files** passed
- JavaScript syntax: **16 maintained browser files** passed Node `--check`
- PublisherStudio 2.9.9 release contract: **103 checks** passed

## Release-contract evidence

- PublisherStudio Web, InstallerConsole, npm package and package lock are **2.9.9**, with single-digit minor/patch slots.
- DevExpress/DevExtreme remains **25.2.9**.
- A newly completed video recording receives a first/between/last sequence boundary and cannot be silently discarded by starting another capture while it is still uncommitted.
- Retained recording commit state survives metadata enrichment/re-render recovery so a committed clip does not regress to an uncommitted state.
- Recording insertion uses the existing canonical timeline projection and does not replace unrelated sequence clips or inherit a selected clip's trim/effect state.
- Video capture dimensions reuse Panel / Div viewport presets in addition to video standards, streaming presets and custom dimensions.
- Frame-rate configuration preserves 23.976, 29.97, 59.94 and other fractional values and supports maintained presets through 240 FPS.
- The 2.9.8 Story Editor caret repair, Mainframe layer drag ordering, recording download repair, Edge MediaCapabilities fallback and source-frame-driven render path remain present.
- InteractiveServer ownership remains on routed pages; nested editor components continue to inherit the routed Editor circuit.
- Six localization catalogs remain in exact key parity.
- The JavaScript diagnostics manifest matches all maintained browser modules after the final source changes.

No generated binary output is included in the release ZIP; `bin`, `obj`, `.git`, `.vs`, test result folders, node modules and Python cache artifacts are excluded.
