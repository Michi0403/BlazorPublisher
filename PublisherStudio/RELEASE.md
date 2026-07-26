# PublisherStudio v1.0.85 release

See `CHANGELOG-v1.0.85.md`, `SOURCE-CHANGES-v1.0.85.txt`, `TEST-RESULTS-v1.0.85.txt` and `docs/architecture/task-ledger.md`.

v1.0.85 evolves the existing application without replacing working components. It introduces interface-first DI services and loopback controllers for OpenSCAD, Video Studio interchange, LocalGPT/AICouncil browser input, screenshots, business-object context, code editing, localization/paths and render-export capability analysis. It also repairs current-frame PNG/JPEG/SVG rendering of canvas/video effects while preserving the existing export and interactive HTML paths.

The OpenSCAD implementation is an open node graph with catalog-driven properties, renderer registration and `$t` animation tracks, deliberately forming the foundation for a later visual builder rather than a closed generator.

Application and installer version is `1.0.85`. Publication format remains `1.55`. Native .NET/Razor/DevExpress and OpenSCAD executable validation are still required on a licensed development workstation; all repository contract, JavaScript, JSON, XML and archive checks are recorded in the test report.
