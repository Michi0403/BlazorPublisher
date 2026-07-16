# BlazorPublisher v0.6 publication data and DevExpress visuals

## Reusable publication data

- Added document-level `PublicationDataObject` records that can be reused by multiple visuals.
- Added JSON input for an object, an array of objects, or an object containing a `data` array.
- Added pasted comma-, semicolon-, tab-, or pipe-delimited text with quoted-field support and optional header rows.
- Added inferred Text, Number, Boolean, and DateTime column metadata.
- Added a live publication-object source for the current page or all pages, exposing page/object names, kinds, geometry, layer order, visibility, and lock state.
- Added a publication data manager with source editing, parse/refresh, preview, sample data, save, delete, and reuse protection.

## Insertable DevExpress visuals

- Added Cartesian charts with bar, line, spline, scatter, area, step, stacked, and full-stacked subtypes.
- Added pie and doughnut charts.
- Added polar line, area, bar, and scatter charts.
- Added line/area/bar/win-loss sparklines.
- Added circular bar gauges.
- Added publication data tables rendered by DevExpress Grid.
- Added KPI objects rendered with a value and DevExpress Progress Bar.
- Added a live visual editor for source selection, category/series/value mapping, component subtype, labels, legends, title, gauge/KPI range, and table row/filter settings.
- Added Insert ribbon commands, a contextual Data Tools tab, right-click commands, layer/inspector integration, double-click editing, and shared page/print rendering.
- Canceling a newly inserted visual now removes only its temporary placeholder; opening the data manager from the visual wizard resumes the pending visual afterward.

## Architecture and compatibility

- Increased the publication file format to `1.7`; older files load with empty data collections.
- Kept the ASP.NET Core/Interactive Blazor host, controllers, services, InstallerConsole, Picture Studio, Story Editor, and page model intact.
- Added `PublicationDataService` to the existing dependency-injection wiring; no route, controller, installer, or WinUI dependency was introduced.
- Added no package. The project still references only `DevExpress.Blazor` and `DevExpress.Blazor.RichEdit` `25.2.*`.
- Maps were intentionally not added because useful map components require an external GIS/tile service and commonly an API key, which conflicts with the self-contained dependency rule.
