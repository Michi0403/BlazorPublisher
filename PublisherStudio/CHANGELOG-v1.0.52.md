# PublisherStudio 1.0.52

## Standalone DevExtreme tooltip coordinates

- Fixed DevExtreme data-visual tooltips in self-contained presentation and site HTML exports when a page is centered, scaled, or animated with CSS transforms.
- Exported chart, pie/doughnut, polar, sparkline, gauge, Sankey, funnel/pyramid, and treemap tooltips now render inside their publication object instead of the untransformed document body. The tooltip therefore shares the exact page/stage transform used by the hovered visual.
- The tooltip owner keeps `overflow: visible`, while the page still clips at the publication boundary. This avoids the old body-coordinate offset without changing chart hit testing, point hover, pointer ownership, overlap cleanup, or z-order wiring.
- The main authoring application keeps its existing DevExtreme tooltip container behavior; the new container is selected only inside `.website-publication` standalone playback.

## Compatibility

- Signal Arrow/Connector coordinates, geometry, triggers, chaining, and reset behavior are unchanged.
- Publication JSON remains `1.45`; Picture Studio format remains `1.2`.
- Streaming, FFmpeg, recording, provider, LAN, device, installer, save/export, animation, media, and component workflows are unchanged.

## Validation

- Reproduced the displaced doughnut tooltip with the supplied `Untitled Publication (37).html` at a transformed presentation scale.
- Verified in headless Chromium that the tooltip moves from `document.body` into the hovered publication object and remains aligned with the green doughnut segment after the stage/page transforms are applied.
- Verified that the existing pointer-ownership guard still hides the tooltip and clears point/series hover when another overlapping object owns the pointer.
- Every project JavaScript file passes `node --check`; all Node contract suites pass.
- A full `dotnet restore`/`dotnet build` is not claimed in this packaging environment because the .NET 10 SDK and licensed DevExpress package feed are unavailable.
