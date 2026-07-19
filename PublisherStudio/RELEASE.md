# PublisherStudio v1.0.35 release

See `CHANGELOG-v1.0.35.md`.

Source release notes:

- Run `Prepare-SpreadsheetAssets.cmd` once after extracting a clean source package. Node.js 20+ is required for developers/build machines only.
- End-user published installations contain the local Spreadsheet/DevExtreme browser assets and do not require Node.js or internet access.
- No PublisherStudio-defined upload or web-response size limit is imposed. Memory, disk, the browser, and DevExpress are the practical boundaries.
- A licensed .NET 10 / DevExpress 25.2.8 build must be completed on the release machine before binaries are distributed.
