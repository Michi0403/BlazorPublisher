# PublisherStudio v1.0.86 release

See `CHANGELOG-v1.0.86.md`, `SOURCE-CHANGES-v1.0.86.txt`, `TEST-RESULTS-v1.0.86.txt` and `docs/architecture/task-ledger.md`.

v1.0.86 is a focused build-unblocking release on top of v1.0.85. It fixes the .NET SDK `NETSDK1022` duplicate-content error by applying output/publish metadata to the SDK's implicit localization content items with `Content Update`, rather than re-including those project-local JSON files. Default Web SDK content discovery remains enabled, and all v1.0.85 application features are retained.

Application and installer version is `1.0.86`. Publication format remains `1.55`. Native .NET/Razor/DevExpress validation must continue on the licensed developer workstation; repository contract, JavaScript, JSON, XML and archive checks are recorded in the test report.
