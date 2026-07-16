# BlazorPublisher v0.4.1 Razor SVG text hotfix

- Fixed the Razor compiler error `'<text>' and '</text>' tags cannot contain attributes` in `WordArtView.razor`.
- Razor reserves `<text>` as a pseudo-element, which conflicts with attributed SVG `<text>` nodes.
- Added the dependency-free `SvgWordArtText` Blazor component. It emits real SVG `text` and `textPath` elements through `RenderTreeBuilder` while Razor markup uses an ordinary component tag.
- Preserved the v0.3.1 solid-fill fallback, gradients, outlines, shadows, extrusion layers, custom text paths, printing, and export rendering.
- No document-model, host, service, controller, installer, package, or format-version changes.
