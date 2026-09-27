# PublisherStudio 3.8.4 — Razor layout and diagnostics guard

- Added a build-breaking aggregate Razor maintenance audit that reports all findings with exact file/line/column diagnostics and architectural repair choices.
- Approved component-level layout owners are exactly `DxGridLayout`, `DxCarousel`, `DxDrawer`, `DxFormLayout`, `DxSplitter`, `DxStackLayout`, and `DxTabs`.
- `<section>` and `<dialog>` are forbidden in maintained Razor source. `DxPopup`/reviewed DevExpress window-prompt controls replace native dialogs.
- Every Razor component must own a typed `ILogger<ComponentName>` injection; Razor/code-behind methods require method-local diagnostics and structured logging.
- Expression-bodied computed Razor properties must delegate to named methods; asynchronous computation belongs in the correct lifecycle/event and stores render state.
- DevExpress component retention now also asserts that the Razor maintenance guard remains present and wired in `Directory.Build.targets`.
- Hardened localization identical-text baseline membership and Razor component-root discovery for Windows PowerShell 5.1 scalar/collection semantics.
- Preserved the DevExpress/DevExtreme 25.2.10 asset pipeline; this release does not alter the repaired browser-asset preparation flow.
- Updated repository instructions with the same non-negotiable component architecture.

This release deliberately does not mass-edit current Razor violations. The next real build is expected to enumerate them so they can be repaired together instead of being hidden by feature deletion or native-HTML substitutions.
