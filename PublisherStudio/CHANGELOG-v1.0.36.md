# PublisherStudio v1.0.36 — Automatic web parsing and spreadsheet data objects

## Web/API and webhook parsing

- Web snapshots now parse automatically when they are fetched, saved, opened in the data manager, or when parsing options such as response format, JSON path, delimiter, or header handling change.
- Editing an embedded source snapshot triggers parsing when the editor loses focus, while Save still performs a final authoritative parse before the object is stored.
- JSON arrays of objects are expanded directly into one row per object and one column per property. The parser now also unwraps JSON that arrives as an encoded JSON string.
- Common wrapper properties (`data`, `items`, `results`, `records`, and `rows`) are recognized automatically. An object with one array property is also treated as a wrapped row array.
- Nested JSON objects are flattened into dotted field names instead of being left as opaque object blobs.
- Source snapshots and binding configuration remain embedded in the publication, so failed polling can continue to use the last valid parsed data.

## Complete field availability in visuals

- Every parsed column is now shown in the chart data-field picker, including text, Boolean, and date fields such as `title` and `completed`.
- Numeric fields are plotted directly, Boolean fields map to `1` and `0`, and non-empty text/date fields count as `1`. This makes all source fields usable without inventing arbitrary numeric values for text.
- The same type-aware conversion is carried into the browser visualization runtime, so canvas previews, live polling, website export, and video capture use the same values.
- Range, bubble-size, and financial OHLC assignments remain restricted to numeric or Boolean-compatible fields.
- New visuals prefer numeric fields, then Boolean fields, while still falling back to other fields when a source contains no numeric measures.

## Spreadsheet selection to publication data

- Added **Create data object** to Spreadsheet Studio's command bar, custom ribbon, and PublisherStudio spreadsheet footer.
- Users can select a rectangular cell range and turn it into a publication data object without leaving the spreadsheet editor.
- The creation dialog previews the selected rows, proposes an object name, detects whether the first row looks like headers, and lets the user correct every column name.
- When no header row is selected, `Column 1`, `Column 2`, and similar placeholders are shown, but the user must enter actual names before creation. Blank and duplicate column names are rejected.
- Completely blank trailing rows and columns inside the selected range are removed. Entire worksheet/row/column selections are rejected with a clear request to choose a bounded range.
- The resulting snapshot stores the workbook, sheet, and range reference and is immediately available to every chart, grid, gauge, and KPI visual in the publication.

## Spreadsheet ribbon naming

- Renamed PublisherStudio's custom far-left spreadsheet tab from **Home** to **Start**.
- The built-in DevExpress **Home** tab is renamed to **All** when its runtime DOM label is available. Even when DevExpress changes its internal markup, the **Start** fallback guarantees that two tabs are no longer both named Home.

## Compatibility and package

- Publication format marker updated to `1.36`.
- npm package metadata updated to `1.0.36`.
- Added optional spreadsheet source-reference metadata to data objects; older publications load with an empty reference.
