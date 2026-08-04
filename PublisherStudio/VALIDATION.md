# PublisherStudio 2.0.5 validation status

PublisherStudio 2.0.5 is a narrow Windows build and release-script correction based on the preservation-first installer recovery and the service-owned 2.0.4 compiler repair.

Validated in this environment:

- Application, setup, browser runtime and npm package versions are aligned at 2.0.5.
- Wire protocol package remains independently pinned to 2.1.1.
- 100 source-contract tests pass.
- One additional test requires the intentionally omitted licensed/generated `wwwroot/vendor/devextreme-dist/js/dx.all.js` asset.
- Architecture policy passes in static, methods, runtime, structure and combined modes.
- The Python architecture-audit unit suite passes 4 of 4 tests.
- `SpreadsheetDocumentService` has one DI constructor and no primary/secondary-constructor conflict.
- The release script contains no unbraced variable immediately followed by a colon.
- The six reported deterministic services are explicitly classified as exception-transparent pure helpers and remain DI-owned instance services.
- JavaScript diagnostics hashes, JSON/XML structure, archive paths and active version surfaces are validated.

Not executable in this environment:

- .NET 10 compilation and analyzers.
- Windows PowerShell 5.1 build/release targets.
- DevExpress licensed asset preparation.
- Windows installer self-replacement and publish workflows.
- Live camera, screen capture and process-loopback tests.

The package therefore remains UNVERIFIED until the maintainer runs the native Windows build and runtime tests.
