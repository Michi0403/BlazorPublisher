# PublisherStudio 3.9.1

PublisherStudio 3.9.1 fixes the remaining general `DxPopup` viewport regression without changing the restored Mainframe/ruler design surface from 3.9.0.

The large Spreadsheet, Media, Data Manager, Data Visual, Story, Picture, Panel, Streaming, Media Converter, Page Effects, DevExtreme Component, Barcode and New Publication studios now give their DevExpress popup shell a definite responsive height. The shared popup CSS bounds dialogs to the browser viewport, makes the DevExpress modal body scroll when necessary, and constrains the legacy full-height Studio roots to that body. This addresses controls that previously bled beyond the popup border or became unreachable. DevExpress dropdown/listbox portals remain independent overlays.

The version advances from 3.9.0 to 3.9.1. Mainframe/ruler geometry, publication/export data, DevExpress 25.2.10 and InteractiveServer boundaries are unchanged. No .NET build/publish and no GitHub/online access were used for this source-only handoff.

See `CHANGELOG-v3.9.1-DXPOPUP-VIEWPORT-CONTAINMENT.md` and `VALIDATION-v3.9.1-source.md`.
