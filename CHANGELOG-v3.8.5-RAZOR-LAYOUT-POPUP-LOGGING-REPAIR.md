# PublisherStudio 3.8.5 — Razor layout, popup and diagnostics repair

- Repaired the complete 3.8.4 Razor maintenance finding set across all maintained components.
- Wrapped component output in approved DevExpress Blazor layout ownership and replaced all maintained `<section>` tags with DevExpress layout-backed equivalents.
- Replaced the documentation native `<dialog>` with `DxPopup`.
- Migrated editor/studio hand-built modal backdrops to `DxPopup` ownership; native content remains only inside the DevExpress popup body.
- Extended the architecture audit to reject native `role="dialog"` and manual `modal-backdrop`/`dialog-backdrop` ownership.
- Converted procedural render-time expression properties to simple method-backed properties with structured logging.
- Preserved existing method-level component diagnostics, InteractiveServer boundaries, DevExpress controls, and the DevExtreme 25.2.10 asset pipeline.
- Kept the PowerShell 5.1/cross-platform DevExpress asset preparation repairs intact.
