# PublisherStudio 3.9.2 — frontend popup and lifecycle repair

## Fixed

- Repaired the **Page appearance & effects** window that still opened as an oversized DevExpress popup with its actual editor stranded in the upper-left corner. The popup now uses the established page-effect footprint, the editor fills that footprint, and the effect list scrolls inside the window while its header and footer stay reachable.
- Completed the general `DxPopup` migration repair instead of treating Spreadsheet/Media/Page Effects as unrelated one-offs. The real studio/dialog surface is now the direct `BodyContentTemplate` child for all maintained large popups, so the shared viewport contract can actually constrain it.
- Matched the DevExpress popup shell to the established size of each workbench (Story, Spreadsheet, Data Manager, Data Visual, Component Studio, Picture Studio, Media Studio, Barcode Studio, Streaming Studio, Panel Studio, Media Converter and Page Effects) instead of wrapping every editor in the same 1800×1180 shell.
- Large workbench surfaces now fill the popup body and keep their existing internal scroll panes; the DevExpress modal body remains a fallback scroll owner. Content-sized chooser dialogs remain internally scrollable instead of being stretched into a workbench.
- Standalone popup components are gated by their visibility/readiness state before their maintenance layout boundary is rendered. Closed editors therefore no longer leave empty DevExpress FormLayout item trees in the live editor DOM.
- Panel Studio now treats an interaction-surface miss during initial `DxPopup` portal materialization as transient. It retries a bounded number of renders before reporting a persistent initialization failure, avoiding the previous first-render error/toast followed by a successful later bind.
- Browser diagnostics no longer replace `EventTarget` listener methods, timers, animation-frame scheduling, microtasks or observer constructors. DevExpress owns those browser lifecycle primitives; PublisherStudio still reports `window.error`, unhandled rejections and explicitly guarded PublisherStudio runtime calls without changing third-party callback identity/timing.

## Preserved and regression-checked

- The recovered Mainframe/print-design surface, page rulers, page thumbnails and inspector geometry remain intact.
- Data Visual chooser caption contrast and same-row checkbox/caption alignment remain in place.
- Shape corner radius remains applied in Mainframe and print/export rendering.
- Interactive website exports retain the presentation playback control bar and its document-controlled visibility.
- Existing DevExpress/DevExtreme ownership, localization data, publication services, export models and InteractiveServer render-mode directives are not redesigned by this patch.
- No application service implementation was changed for this frontend repair.

## Maintenance guards

- `RAZORUI0014` now rejects a maintenance `DxFormLayout` inserted directly between a `DxPopup` body and the real studio/dialog surface.
- The popup viewport audit now includes the Page Effects surface contract.
- JavaScript diagnostics validation now rejects global replacement of browser/vendor lifecycle primitives.
- `UI-STYLE-GUIDE.md` documents direct popup-body ownership, inactive-studio DOM gating, Page Effects sizing and observational browser diagnostics.

## Version

`3.9.1 -> 3.9.2`.

The version-number rollover rule remains satisfied; neither the minor nor patch slot reaches two digits.
