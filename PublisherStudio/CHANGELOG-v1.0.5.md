# PublisherStudio v1.0.5 beta stabilization

- Reworked WebM presentation export around a page-sized canvas compositor. The current-tab capture is cropped to the largest publication page and recorded from a dedicated canvas stream, avoiding browser viewport bars and fragile Region/Element Capture behavior.
- Disabled the normal JavaScript interop timeout for long presentation recordings and added stage-specific browser/server error messages.
- Fixed publisher mouse interaction state: connector ports no longer cover resize handles, repeated selection no longer causes unnecessary rerenders during pointer capture, active connector mode exits when a normal object is selected, and drag operations reconnect to the current Blazor DOM element after a render.
- Replaced arithmetic z-index changes with deterministic adjacent swaps and normalized layer positions. One Up/Down click now moves exactly one layer.
- Added CSV/TSV/JSON/XML file import to publication data objects, plus XML row parsing for charts, tables, gauges, sparklines, and KPIs.
- Added CSV/TSV/JSON/XML/TXT value import to Barcode Studio.
- Hardened the Story Editor window against RichEdit ribbon-height changes. Ribbon tab changes now trigger a bounded resize refresh instead of pushing the editor outside the visible dialog.
- Clamped and relabeled text-frame border width as millimetres.
