# PublisherStudio 2.0.5 — Windows build and release-script correction

PublisherStudio 2.0.5 corrects the Windows-only failures reported after the service-owned 2.0.4 compiler repair. The architecture remains instance/service-driven: no application statics were added and shared data contracts remain under `PublisherStudio.BusinessObjects`.

## Corrected

- `SpreadsheetDocumentService` now uses one DI constructor instead of combining a primary constructor with an invalid parameterless secondary constructor.
- `Build-Release.ps1` now delimits `expectedManifest` before the following colon, making the script valid in Windows PowerShell 5.1.
- Six deterministic instance services are explicitly classified as exception-transparent pure helpers by the maintained logging policy. They remain DI-owned services; no static shortcut or catch-and-rethrow noise was introduced.
- Application, setup, browser/runtime and package versions are aligned to 2.0.5.

## Maintainer verification required

The source package is structurally tested, but the native .NET 10 build, Windows PowerShell 5.1 release scripts, DevExpress restore and runtime installers still require execution on the maintainer's Windows machine.
