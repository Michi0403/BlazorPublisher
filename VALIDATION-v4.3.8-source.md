# PublisherStudio 4.3.8 source verification

- Baseline: 4.3.7.
- Scope: object-context AI Assist -> correlated result -> native editor document insertion.
- No edits to `build/Assert-*.ps1`, Picture Studio C#/JS, or retained GIF tests. `PageSurface.razor` changed only the DevExpress object context menu, not drawing/canvas rendering.
- Existing `tests/static/Assert-AiAssistIntegration.ps1` verifies source wiring and architecture assertions when PowerShell is present.
- All six localization catalogs have identical keys (3 newly localized AI context actions).
- Full .NET compilation, PowerShell 5.1 gates, browser + DevExpress integration and live 1-Wire approval/insertion require the actual Windows host. NOT performed here.
