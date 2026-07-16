# BlazorPublisher v0.3.1 hotfix

- Replaced programmatic WordArt SVG construction with a dedicated Razor SVG component.
- Added a solid face beneath gradient WordArt so text remains visible when a browser or rasterizer rejects the SVG paint server.
- Added safe defaults for empty/transparent WordArt colors and empty display text.
- Gave editor and print render trees separate SVG IDs to prevent gradient/path ID collisions.
- Applied the same WordArt renderer to the canvas and print publication.
