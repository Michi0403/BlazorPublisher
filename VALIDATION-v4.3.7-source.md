# PublisherStudio 4.3.7 source and browser verification

Baseline: PublisherStudio 4.3.6 source. Comparison: PublisherStudio 4.2.2 (known working Picture Studio drawing canvas).

1. No DevExpress/popup/architecture guard edits; no Python maintenance scripts added.
2. Picture Editor now treats `_initialized` as an observed browser binding state and rechecks the canvas on every after-render. The JavaScript module observes portal canvas replacement and arranges one asynchronous Blazor re-render when a canvas first appears after a missing-attachment attempt.
3. Browser render is double-buffered; stale async image/GIF/selection renders cannot write to the live canvas. Previous content remains until the latest render is complete.
4. Headless Chromium directly exercised the production Picture Studio JS and animated GIF dependency: red PNG RGBA=250,20,20,255 at 64×40; popup canvas replacement retains those pixels; GIF frame0 red/frame1 green/frame0 red; brush dispatch calls PictureStrokeCommitted; late-mounted popup triggers PictureCanvasAttached and displays PNG; page errors: zero.
5. The production browser GIF Apply callback completed with `data:image/gif;base64,` output; decoding its bytes yielded two nonempty frames with distinct red/green content and 100/200 ms delays. GIF palette quantization is expected.
6. Node GIF codec fixture test and strengthened canvas source test passed; syntax checks passed.
7. JS diagnostics inventory refreshed and exact normalized SHA-256 parity verified for all 17 maintained JS modules.
8. Web and InstallerConsole versions 4.3.7; Picture Studio module imports versioned 4.3.7.

Not run: Windows PowerShell architecture gates, .NET/DevExpress compilation, full Blazor publication UI. The browser test proves the specified JavaScript lifecycle and pixel behavior only; it does not certify the entire application.
