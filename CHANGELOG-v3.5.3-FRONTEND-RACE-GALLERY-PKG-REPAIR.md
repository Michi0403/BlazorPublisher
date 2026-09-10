# PublisherStudio 3.5.3 — frontend race, Gallery media and macOS PKG repair

## Interaction, stacking, input and frontend-failure release gate

- [x] PublisherStudio browser assets now carry the current 3.5.3 release identity instead of the stale 3.3.5 cache key that could pair current Blazor markup/server code with an older browser runtime.
- [x] The native range-event coalescer no longer intercepts DevExpress RichEdit, DevExpress Blazor or DevExtreme-owned controls. Vendor widgets retain their own caret, scroll, resize and input lifecycles.
- [x] Story Editor shell layout no longer synthesizes global `window.resize` events at all. The shell only constrains the RichEdit host width; DevExpress keeps ownership of its own viewport/caret lifecycle, eliminating the observed reflow oscillator rather than merely slowing it down.
- [x] Website export quality sliders use invariant-culture binding. A browser value such as `0.58` can no longer be interpreted as `58` under a `de-DE` UI culture and displayed as `5800%`.
- [x] Picture insertion accepts up to 64 selected image files in one picker operation. Gallery/TileView components bound to the live Publication media data object therefore receive every inserted picture instead of requiring repeated single-file insertion.
- [x] Existing drag/drop insertion remains available and multi-selection uses a small cascade so newly inserted pictures remain individually selectable.
- [x] The existing RichEdit OpenXML document remains authoritative; the supplied publication contained valid OpenXML-backed text, so the regression is treated as browser/layout lifecycle rather than replacing user text or inventing a new document format.
- [x] macOS PKG output is now a `productbuild` distribution package around the validated `pkgbuild` component package. The final artifact is re-opened with `pkgutil --expand-full`; signed packages are additionally checked with `pkgutil --check-signature`. It must also pass macOS `installer -showChoicesXML`, which exercises the same package reader used by Installer.app without installing anything. Any package that fails those readbacks is deleted instead of being shipped.

## Evidence addressed

The supplied runtime HTML was running PublisherStudio 3.5.2 while `site.css`, `publisherInterop.js`, `componentRuntime.js`, localization runtime and media modules still used a `v=3.3.5` browser cache identity. This can leave old browser code paired with newer server/component code. The supplied export screenshot also showed `Picture quality: 5800%`, matching a decimal-culture parse failure for a native range value such as `0.58` under German culture. The supplied publication contains a Gallery bound to the live `PublicationMedia` data object and an OpenXML-backed text frame; neither needs a new data model.

## Preserved behavior

PublisherStudio's current InteractiveServer ownership, Mainframe object model, live Publication media dataset, DevExpress/DevExtreme component rendering, installer lifecycle ownership checks, `server.json` rendezvous behavior and the 3.5.0–3.5.2 documentation/release repairs remain in place.
