# PublisherStudio 4.3.6 source-only validation

Baseline: PublisherStudio 4.3.5. GIF canvas baseline and unit tests remain included.

- JavaScript diagnostics: exact set/hash parity for 17 current maintained browser JS files, CRLF/CR-to-LF normalized UTF-8 SHA-256; reviewed `pictureStudioInterop.js` hash updated.
- Localization: 3,439 keys in each of six locale JSON catalogs, parity, no case-insensitive duplicate keys; no missing static `LT()`/`.GetText()` literal; both newly introduced GIF texts translated.
- Razor: each component has exactly one required typed ILogger injection, including `MediaStudioTitlebar`.
- Service: `SupportsCapability` method-local try/catch with `logger.LogError` and rethrow.
- PowerShell-only: zero Python test/maintenance files shipped. Explicit migration inventory includes the exact three previously shipped tests from PublisherStudio 4.3.4, which are backed up if old ZIPs were overlaid.
- Node: `tests/js/picture-studio-gif-frames.test.mjs` and `tests/js/picture-studio-canvas-regression.test.mjs` passed.
- Web and InstallerConsole assembly versions both 4.3.6, with browser module version queries 4.3.6.
- PowerShell architecture/audit guard source and `Directory.Build.targets`: unchanged.

**Not executed:** Any PowerShell guard, .NET build or Visual Studio/DevExpress runtime test. Do not classify these checks as build success. A Windows Debug build is still required for compiler/analyzer and actual guarded policy confirmation.
