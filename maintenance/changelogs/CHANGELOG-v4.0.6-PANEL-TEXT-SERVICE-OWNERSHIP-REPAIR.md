# PublisherStudio 4.0.6 — Panel Studio text-service ownership repair

## Summary

PublisherStudio 4.0.6 is a focused maintenance follow-up to 4.0.5. It repairs the reported text-service ownership build failure in `PanelStudio` without weakening the maintenance guard or changing Panel Studio behavior.

## Text-service ownership repair

- `PanelStudio` no longer performs `string.Join` directly in Razor when presenting Data Visual value fields.
- The already injected `PanelStudioTextService` now owns value-list formatting through `FormatList`, with method-local diagnostics and a safe fallback.
- The editor continues to display the same comma-and-space separated field list and continues to parse edits through the existing `PanelStudioTextService.ParseList` path.

## Compatibility

No publication schema, saved document format, Panel Studio geometry, DevExpress ownership, LocalGPT code or maintenance/build script is changed in this release.
