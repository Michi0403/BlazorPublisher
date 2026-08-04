# PublisherStudio 2.0.4 validation status

PublisherStudio 2.0.4 is a service-owned compiler/composition repair candidate based on the preservation-first 2.0.2 installer recovery and the 2.0.3 Windows build-policy correction.

Validated in this environment:

- Application and setup version are 2.0.4.
- Wire protocol package remains 2.1.1.
- The complete repository Node contract suite passes 97 of 97 tests when the prepared DevExpress browser runtime is present.
- The sanitized extracted source package passes 96 of 97 tests; the sole expected failure is the intentionally omitted generated/licensed `wwwroot/vendor/devextreme-dist/js/dx.all.js` asset.
- Architecture policy passes in static, methods, runtime, structure and combined modes.
- The exact stale static calls and constructor call sites reported by the maintainer are absent.
- Data contracts are owned by `PublisherStudio.BusinessObjects`; the BusinessObjects layer has no dependency on PublisherStudio services or `ILogger`.
- JSON, XML/MSBuild, JavaScript and Python source syntax checks pass.
- ZIP integrity, portable path and generated-output exclusion checks pass.

Not executable in this environment:

- .NET 10 compilation and analyzers.
- Windows PowerShell 5.1 build targets.
- DevExpress licensed asset preparation.
- Windows installer self-replacement and publish workflows.
- Live camera, screen capture and process-loopback tests.

The package therefore remains UNVERIFIED until the maintainer runs the native Windows build and runtime tests.
