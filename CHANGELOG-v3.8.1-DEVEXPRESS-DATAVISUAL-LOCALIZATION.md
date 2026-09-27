# PublisherStudio 3.8.1 — DevExpress, DataVisual and localization maintenance

- Reimplements the Data Visual editor's ordinary interactive HTML controls with DevExpress Blazor controls while preserving the existing editor workflow and styling intent.
- Adds a source-controlled DevExpress component-retention build gate. Protected Razor components cannot silently lose DevExpress components or gain additional native interactive controls without an explicit reviewed baseline change.
- Repairs DevExpress 25.2.10 asset preparation on Windows PowerShell 5.1 and modern PowerShell: npm v3 `package-lock.json` metadata is parsed by a checked-in Node helper invoked as a file, avoiding both `ConvertFrom-Json`'s empty root-package-key failure and `node -e` quote rewriting. The helper is platform-neutral and works with Windows, macOS and Linux Node runtimes.
- Keeps existing browser assets in place until replacement preparation succeeds, copies both `devextreme-dist` and `devexpress-aspnetcore-spreadsheet`, and overlays the authoritative exact-version DevExtreme runtime used by the license generator.
- Ensures `dx.all.js` loads before the generated DevExtreme license script so `DevExpress` exists when the runtime license executes.
- Refreshes the maintained JavaScript SHA-256 inventory for the changed browser files.
- Improves DataVisual category presentation for funnel/pyramid labels, and resolves default visual titles through the active localization service. Newly inserted visual names/titles use the current UI culture instead of writing `Chart` into a German Mainframe.
- Adds/repairs localized Data Visual editor labels including title, data object, argument/category, series grouping, argument axis, repeated categories, point order, mapping, data fields and visual type names.
- Strengthens localization maintenance: every maintained culture must remain in exact English-key parity, and new English-identical German UI values fail validation unless explicitly reviewed as language-neutral.
- Advances PublisherStudio and installer versions from 3.8.0 to 3.8.1 and refreshes current frontend cache-busters/npm package metadata.

This source handoff was prepared without running dotnet/MSBuild/NuGet or using GitHub.
