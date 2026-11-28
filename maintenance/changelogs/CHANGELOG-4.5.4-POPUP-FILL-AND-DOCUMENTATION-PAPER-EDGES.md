# PublisherStudio 4.5.4 — Scoped popup fill and documentation paper tears

## Popup layout
- Keep the DevExpress DxPopup as the outer modal owner; remove redundant inner padding and permanently reserved scrollbar gutters **only where the popup body's direct child is a recognized PublisherStudio dialog**.
- Remove the nested border, shadow, duplicate rounding and width restriction from the PublisherStudio-owned `.publisher-dialog` inside DxPopup. Keep its existing header, body typography, action buttons, click handlers, colors and scrolling.
- Do **not** globally reset DevExpress dropdown, notification, or unrelated popup internals. Do not touch DevExpress lifecycle/render tree or architectural maintenance validators.

## Documentation effect
- Retain the selected Option 2 glowing paw, click scratch animation, held drag, exact-position cuts through rendered text, theme-dependent glows, scratch audio, and on-navigation/refresh reset from 4.5.3.
- Add jagged tear geometry, continuous raised paper lips, folded flaps on tear ends and sides, frayed fiber strands and surface shadows for a more realistic ripped-paper appearance in both dark and light modes.
- No DocFX pipeline, generated HTML snapshot, export, navigation, publication model, document source, or application renderer changes.

## Versions and validation
- Bump PublisherStudio.Web and PublisherStudio.InstallerConsole from 4.5.3 to 4.5.4.
- 15/15 Node.js source/JavaScript tests pass.
- JavaScript syntax check passes.
- Synthetic Chromium popup layout: PublisherStudio direct-child dialog aligns to modal body, no inner frame, unrelated body keeps padding and scrollbar gutter.
- Synthetic Chromium dark/light docs: aligned rendered-content tears, held drag, six holes from two strokes, twelve lifted paper lips, preserved DOM, navigation reset, no page errors.
- The licensed DevExpress runtime, actual Windows build, real installed UI popup catalog and full DocFX generation were **not** executed. They still require Windows acceptance testing.
