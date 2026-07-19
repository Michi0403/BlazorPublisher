# PublisherStudio v1.0.36 release

See `CHANGELOG-v1.0.36.md`.

Source release notes:

- Run `Prepare-SpreadsheetAssets.cmd` once after extracting a clean source package. Node.js 20+ is required for developers/build machines only.
- End-user published installations contain the local Spreadsheet/DevExtreme browser assets and do not require Node.js or internet access.
- No PublisherStudio-defined upload or web-response size limit is imposed. Memory, disk, the browser, and DevExpress are the practical boundaries.
- Web/API/webhook snapshots are parsed automatically on fetch and save. JSON arrays and common wrapper objects expose all scalar fields to visuals.
- Spreadsheet Studio can create publication data objects from a selected cell range. Users review header handling and column names before the snapshot is added.
- A licensed .NET 10 / DevExpress 25.2.8 build must be completed on the release machine before binaries are distributed.
