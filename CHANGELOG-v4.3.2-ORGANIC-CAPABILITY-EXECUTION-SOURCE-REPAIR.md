# PublisherStudio 4.3.2 — executable capability catalog and Council bridge

## Release scope

Source-only changes based on the supplied PublisherStudio 4.3.1 archive and paired LocalGPT 6.1.3 archive. This is a **partial architecture audit remediation**, not a claim that every previously reported issue is resolved.

### Implemented

- **Council bridge request forwarding (partial audit C).** Accept caller-configurable `MaxRounds`, `MaxParallelModels`, and implementation-artifact intent; preserve caller-chosen memory saving. Removed the artificial answer-only Council prompt wrapper. The 1-Wire response now carries its full Council result JSON as `PublisherAiChatResponse.ResultJson` alongside the existing text answer and run correlation.
- **Browser approval safety.** A browser-supplied request cannot self-attest user confirmation for consequential artifact building. The bridge forces `UserConfirmedArtifactBuild = false`; explicit confirmation must follow the maintained Council/human-collaboration process.
- **True capability executability checks (partial audit E).** Added `IOrganicExecutableCapabilityHandler` to extend the organic dispatcher with DI-registered executable handlers. Unknown descriptors remain visible in operator-owned catalog configuration but are marked offline by the capability catalog. The executor does not interpret an arbitrary database entry as executable code.
- **New real media operations (partial audit E).** Registered data-driven 1-Wire capability descriptors and handlers for `publisher.media.jobs`, `publisher.media.profiles`, and approval-gated `publisher.media.cancel`. The handlers delegate to existing `IMediaConversionService`, bound output job count, omit local source/output file paths, validate job GUIDs, and retain existing permission/approval gates. All new entries reside in the existing serializable DXFunction seed catalog.
- **Regression tests.** Added four Python source-integration checks under `tests/static`; the existing PublisherStudio GIF animation Node test continues to pass.
- **Versioning.** Updated both PublisherStudio Web and InstallerConsole project versions **4.3.1 → 4.3.2**. No two-digit second/third segment is introduced.

### Outstanding, not falsely claimed as fixed

- Unrestricted PublisherStudio creative operations (edit, render, convert, publish, multimedia export) are not all accessible through the 1-Wire broker. The new handlers cover three truthful service operations only.
- Full cross-application typed artifact/event bus and UI playback/action handoffs are not implemented; `ResultJson` is exposed to callers but not yet consumed by a general typed chat UI.
- Complete autonomous goal planning, self-replanning, durable restart recovery and media runtime/provider adapter discovery remain cross-app work.
- No end-to-end PublisherStudio + LocalGPT approval/execution UI test was executed.

### Verification and constraints

- The committed strict PowerShell/DevExpress architecture gates, policies and build-target wiring were left untouched.
- PowerShell and .NET are unavailable here; neither the gates nor the compiler ran. This is **not build-verified**.
- `python -m unittest discover -s tests/static -p 'test_*release.py' -v`: **4 passed**.
- `node --test tests/js/picture-studio-gif-frames.test.mjs`: **1 passed**.
- JSON DXFunction catalog parses successfully with Python's standard JSON parser. ZIP integrity verified after packaging.
- On a supported host, run the existing architecture/maintenance gates, restore/build both projects, and test 1-Wire permissions, approval-gated media cancellation, catalog updates, structured Council results and media editor UX before publishing a binary release.
