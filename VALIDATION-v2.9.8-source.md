# PublisherStudio 2.9.8 source validation

**SOURCE-NOT-COMPILED.** This release was prepared without running `dotnet`, MSBuild, NuGet restore/publish, Visual Studio compilation or a licensed DevExpress build. The user's Windows build remains the authoritative compile/runtime gate.

## Reported regressions addressed

1. **Story Editor caret jumping after DOCX download** — the RichEdit host was observing/re-triggering its own resize response. The self-observation and unconditional global resize were removed; shell-width changes now gate the resize notification.
2. **Mainframe Layers not directly draggable** — layer rows now provide drag/drop ordering while committing through the existing selected-layer position service path.
3. **New recording replacing/obscuring an existing sequence choice** — a completed video recording now requires an explicit first/between/last boundary choice when clips already exist, and the recording is inserted through the existing timeline service.
4. **Recording/source download unreachable** — the command now accepts either the retained recording Blob or the currently selected embedded source.
5. **Rendered video severe FPS collapse** — the supplied rendered WebM was independently inspected as 3828×1962 VP9 with 115 frames over about 28 seconds, approximately 4.17 FPS. Full-source no-effect render is now exact-byte source download; visual-effect baking follows decoded source frames where supported and otherwise uses an explicit source/fallback cadence.
6. **Repeated Edge capability-probe errors** — the supplied browser trace showed `MediaCapabilities.encodingInfo` rejecting `type: "record"`; this optional probe is now compatibility-wrapped and disabled after that browser-specific unsupported-enum response.
7. **Successful render reported as failed** — expected circuit teardown after the file is already downloaded no longer reclassifies the completed browser export as a render failure.

## Source checks

The final source gate runs the repository's maintained static checks that do not require .NET compilation:

- `build/audit_application_architecture.py --root . --product publisherstudio --mode all`
- `build/audit_async_continuations.py --source-root src/PublisherStudio.Web`
- `build/audit_component_resilience.py --root .`
- `build/audit_prerender_interop_safety.py --root .`
- `build/audit_service_resilience.py --root . --product publisherstudio`
- `build/audit_iterator_exception_policy.py --root .`
- `build/audit_panelstudio_persistence.py`
- `build/Assert-XmlDocumentationCoverage.py .`
- Node `--check` for all maintained `src/PublisherStudio.Web/wwwroot/js/*.js`
- exact six-catalog localization parity/JSON validation through the 2.9.8 release audit
- exact maintained JavaScript diagnostics hash validation through the 2.9.8 release audit
- `build/audit_release_2_9_8.py`

## Final static-audit results

- application architecture policy: passed
- async continuation policy: **79 source files / 1,093 await tokens**; 461 `ConfigureAwait(false)`, 578 renderer-affine `ConfigureAwait(true)`, 49 explicitly configured async disposals, 5 configured async streams
- component method resilience: **2,669 component methods**, zero legacy exemptions
- prerender JavaScript interop safety: **2,669 component methods**, 13 JavaScript-aware disposal methods attachment-gated
- service resilience: **1,345 service methods** plus 4 maintained iterator/yield methods, zero exemptions/skips
- iterator exception policy: passed
- Panel Studio persistence/source architecture audit: passed
- C# XML documentation: **6,186 declarations / 248 source files**
- Razor XML documentation: **48 component types / 3,401 direct `@code` members**
- repository-equivalent component diagnostics baseline: **100 component files** passed
- localization: **3,349 keys in exact parity across all six catalogs**
- JavaScript diagnostics/hash coverage: **16 maintained browser files** passed
- JavaScript syntax: **16 maintained browser files** passed Node `--check`
- PublisherStudio 2.9.8 release contract: **100 checks** passed

## Release-contract evidence

- PublisherStudio Web, InstallerConsole, npm package and package lock are **2.9.8**, with single-digit minor/patch slots.
- DevExpress/DevExtreme remains **25.2.9**.
- Story Editor layout observes only the owning shell and dispatches RichEdit resize only after a real shell-width transition.
- Layers drag/drop reuses `SetSelectedLayerPosition`; Front/Up/Down/Back and the rest of object manipulation remain present.
- Recording placement reuses `TimelineEdits.SegmentTimelineStart` and `TimelineEdits.InsertAt`; it does not create a second media sequence model.
- Fresh recordings do not inherit the selected clip's cut/effect state.
- Selected embedded media is downloadable even when the transient retained-recording Blob is not the active source.
- Full-range/no-effect/native-size video render preserves the original media bytes; visual-effect baking retains native dimensions and source-driven cadence where supported.
- The Edge-invalid `record` capability probe is isolated behind a one-time compatibility fallback rather than producing one error per codec candidate.
- Permission picker cancellation and expected post-download circuit teardown are not duplicated as alarming JavaScript diagnostics.
- Six localization catalogs remain in exact key parity.
- InteractiveServer boundaries remain unchanged and nested editor components inherit the routed Editor circuit.
- The JavaScript diagnostics manifest is regenerated from all maintained browser modules after the final source changes.

No generated binary output is included in the release ZIP; `bin`, `obj`, `.git`, `.vs`, test result folders, node modules and Python cache artifacts are excluded.
