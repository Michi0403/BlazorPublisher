# PublisherStudio 3.9.1 — DxPopup viewport containment

## Release gate

- [x] Mainframe, rulers, page canvas and restored print-design geometry remain unchanged from 3.9.0.
- [x] Existing InteractiveServer render-mode directives are unchanged from 3.9.0.
- [x] Large Studio `DxPopup`s own a definite responsive height as well as width/max-height.
- [x] Oversized Studio content remains inside the DevExpress modal boundary and is reachable by internal scrolling.
- [x] Spreadsheet Studio and Media Studio use the same general popup viewport contract rather than one-off clipping fixes.
- [x] Barcode Studio remains compatible with the shared contract even when its content grows beyond the current viewport.
- [x] DevExpress dropdown/listbox portals are excluded from modal overflow ownership.
- [x] Selection, Mainframe object geometry, export geometry, publication serialization and input routing are unchanged by this CSS/layout repair.
- [x] No listener, pointer-capture, object-URL or JavaScript lifecycle behavior was added or changed.
- [x] The shared popup viewport contract is covered by the maintained Razor source audit.

## Fixed

### Large Studio dialogs no longer bleed outside `DxPopup`

The DevExpress migration left several Studio roots with legacy `height: 100%` assumptions from the former full-screen backdrop implementation while their new `DxPopup` shells only had a maximum height. That combination allowed complex controls to measure outside the visible popup or leave essential rows unreachable.

All thirteen large editor/studio popups now declare a definite responsive popup height (`min(1180px, 96vh)`) while retaining their existing width and maximum-height limits. This includes Spreadsheet, Media, Data Manager, Data Visual, Story, Picture, Panel, Streaming, Media Converter, Page Effects, DevExtreme Component, Barcode and New Publication studios.

A shared late CSS contract additionally:

- bounds DevExpress modal dialogs to the current browser viewport;
- makes the DevExpress modal body the fallback scroll viewport;
- gives modal content the required shrinkability (`min-width/min-height: 0`);
- constrains former backdrop-filling Studio roots to the popup body and gives them internal overflow fallback;
- preserves DevExpress dropdown/listbox portals as independent overlays.

This is a containment repair rather than a replacement of DevExpress components or a return to manual modal backdrops.

## Style guide

`UI-STYLE-GUIDE.md` now records the general rule that a large `DxPopup` needs a definite responsive height whenever descendants use percentage heights, and that scroll ownership belongs inside the DevExpress modal rather than outside its border.

## Maintenance guard

`build/audit_razor_maintenance_contract.py` now requires the `DEVEXPRESS_POPUP_VIEWPORT_CONTRACT` marker and the essential viewport/overflow declarations. `Assert-DevExpressComponentRetention.ps1` retains that source-level protection.

## Version

3.9.0 -> 3.9.1.
