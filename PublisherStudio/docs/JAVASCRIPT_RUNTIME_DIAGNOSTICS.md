# JavaScript runtime diagnostics

PublisherStudio loads `wwwroot/js/javascript-diagnostics.js` before vendor, Blazor, and maintained application scripts. Maintained first-party JavaScript functions and callbacks use explicit `try`/`catch` boundaries. Failures are written with `console.error`, buffered until the interactive bridge is attached, and forwarded through an interactive `DotNetObjectReference` to `ILogger`, so browser failures also appear in the Visual Studio application output.

The runtime additionally observes uncaught browser errors and unhandled promise rejections and guards event, timer, animation-frame, microtask, and observer callbacks. Third-party and minified vendor sources are not rewritten; their uncaught failures are still observed by the early global diagnostics runtime.

`build/Assert-JavaScriptDiagnostics.ps1` and `build/javascript-diagnostics-files.sha256` fail direct, development, and release builds when a maintained browser file is added or changed without review, loses its diagnostics marker, lacks error reporting, or introduces an empty catch. Existing security, 1-Wire, runtime-value, and structure checks remain independent and unchanged.
