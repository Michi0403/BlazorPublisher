# PublisherStudio 4.5.2 — architecture-compliant build repair

Source-level correction of defects introduced in previous deliveries. No maintenance/architecture gates were weakened, disabled, exempted or rebaselined.

- Added the exact `Editable GIF frames` localization key in all six maintained culture catalogs, including a German translation.
- Replaced the animated GIF frame-selection native button with a `DxButton`, keeping frame thumbnail and selection behavior.
- Replaced two native AI dialog close buttons with `DxButton`; retained close/cancel ownership introduced in 4.5.0.
- Moved LocalGPT model preference text splitting/deduplication from Editor and OrganicPlugins UI into the existing DI-injected `PublicationEditorTextService` with bounded error diagnostics.
- Updated GIF frame styling for a focusable DevExpress action control.
- Added a focused offline regression test covering source controls, text-service ownership and localization completeness.
- Updated application Web and InstallerConsole versions from 4.5.1 to 4.5.2. Unrelated installation/publishing machinery and user data unchanged.

## Validation

- Offline Node JavaScript/source tests and JSON parsing; no Windows PowerShell build gates could run in this environment.
- Requires Visual Studio / licensed DevExpress / .NET 10 build verification by the user; source checks are not a compiler guarantee.
