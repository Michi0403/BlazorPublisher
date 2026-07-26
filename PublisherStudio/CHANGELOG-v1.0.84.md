# PublisherStudio v1.0.84

## Video/OpenSCAD interchange compiler compatibility

- Replaced the unavailable `Math.Hypot` call with a private, numerically stable `EuclideanDistance` implementation based on scaled square-root evaluation.
- The helper is safe for zero-length edges and avoids unnecessary overflow/underflow risk if polygon coordinates are expanded beyond the current normalized 0–1 range.
- Changed the internal polygon-resampling parameter from `IReadOnlyList<MediaFramePoint>` to the concrete `List<MediaFramePoint>` already produced by normalization and fallback paths, clearing CA1859 while retaining the same private call contract.
- Replaced the four LINQ `ToList()` materializations identified by IDE0305 with C# collection expressions.
- OpenSCAD generation, source/target polygon resampling, animated HTML canvas output, temporal selection layers and Mainframe/Panel interchange remain behaviorally unchanged.

## Regression protection

- Extended the Video Studio 3D/interchange contract test to reject a returning `Math.Hypot` dependency, interface-based hot-path signature or `ToList()` materialization in this service.
- Application, installer, npm, structured-export and streaming runtime versions are advanced to `1.0.84`.

## Validation

- All repository contract suites pass through `npm test`.
- JavaScript syntax, JSON, XML/MSBuild parsing, whitespace and release-archive extraction checks pass.
- Native .NET/Razor/DevExpress compilation still requires a workstation with the .NET 10 SDK and configured licensed DevExpress package source.
