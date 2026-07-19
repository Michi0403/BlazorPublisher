# PublisherStudio v1.0.35 — Complete chart-series and web-data workbench

## Complete chart and visualization catalogue

- Added every DevExtreme Cartesian series type available in the pinned 25.2.8 runtime: bar, line, spline, scatter, area, spline area, step line/area, all stacked and full-stacked variants, range area/bar, bubble, candlestick, and stock.
- Added/finished pie and doughnut, every PolarChart type, every Sparkline type, plus publisher-focused bar/circular/linear gauges, range selector, Sankey, funnel, pyramid, tree map, DevExpress/DevExtreme data grid, and KPI progress visuals.
- Added dedicated field mapping for low/high ranges, bubble size, OHLC financial data, Sankey targets, and tree-map parents.
- Added every visualization kind to the Insert ribbon, canvas insertion menu, visual editor, object context menu, and Data Tools quick-type menu.
- Polar charts now support grouped and multiple value series. Chart and PolarChart output use DevExtreme series templates so each generated group owns only its own points. Range, bubble, and financial charts no longer create duplicate series from their auxiliary fields.
- Removed internal gauge point truncation. Publication source rows remain intact; the DevExtreme grid pages the complete row/column set and only small error fallbacks intentionally show a compact preview.

## Generic web-data layer

- Added a chart-independent `PublicationWebBinding` contract for local monolith API requests, external REST requests, webhooks, response parsing, polling, snapshots, and future stream transports.
- Added a server-side request service with configurable method, headers, body, JSON path, response format, delimiter, refresh interval, refresh-on-open, snapshot fallback, and `0 = no timeout`.
- Added no PublisherStudio-defined request-body, upload, or response-size ceiling.
- Added per-binding synchronization so background polling, manual refresh, and export refresh cannot mutate the same source concurrently.
- The monolith registry reuses unchanged immutable data snapshots instead of recopying large source tables during unrelated canvas interaction, and unregisters a publication when its scoped editor state is disposed.
- Added tokenized webhook inboxes that accept POST or PUT payloads without an application size cap. A webhook binding can be saved before its first payload arrives.
- Added a local read-only monolith API for system status, open-publication summaries, publication/page snapshots, data metadata, and row snapshots.
- Added tokenized CORS row endpoints for explicitly enabled standalone HTML reconnects without opening every publication API route to arbitrary browser origins.
- The `Stream` transport is reserved for the planned LAN/VLC/provider streaming workbench and intentionally reports that it is not implemented yet.

## Export behavior

- Website and presentation-video export force a final server-side refresh of every enabled web data object before capture.
- Self-contained website export embeds DevExtreme CSS, jQuery, the DevExtreme runtime, the PublisherStudio live-data runtime, and the latest rows.
- Exported HTML renders from its embedded snapshot with no server. Optional JavaScript polling can use a CORS-enabled external endpoint or reconnect to the local monolith. The exporter embeds the current loopback origin as the initial default, while `?publisherApi=http://127.0.0.1:PORT` overrides it after a restart or port change.
- Webhook-backed exports can also update through the tokenized monolith snapshot route.
- Each page's visual components are refreshed as it becomes active during video recording.

## Spreadsheet ribbon naming

- Renamed PublisherStudio's complete, far-left workflow ribbon tab to **Home**.
- Renamed the built-in command-dense DevExpress **Home** tab to **All controls** at runtime, eliminating the duplicate Home labels while preserving every native command.
- Removed a duplicate begin-synchronization event registration.

## Compatibility and package

- Publication format marker updated to `1.35`.
- npm package metadata updated to `1.0.35`.
- Older publications receive generated webhook/export tokens and normalized binding defaults when loaded.
- Node.js remains a developer/build-machine requirement only. Published users do not need Node.js or npm.
