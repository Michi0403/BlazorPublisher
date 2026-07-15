# PublisherStudio architecture

## Boundaries

- **PublisherStudio.Web** is the product. It owns the ASP.NET Core loopback host, Blazor Server UI, DevExpress integration, document model, file services, and local API endpoints.
- **PublisherStudio.InstallerConsole** is an optional deployment helper. It has no reference to the web project and only installs or launches published output.
- **WinUI is intentionally absent from the first solution.** A future shell can call `Program.BuildWebApp()` and navigate WebView2 to the endpoint written to `server.json`, but the editor does not depend on it.

## Editing engines

- DevExpress `DxRibbon` provides the Publisher-like command surface.
- DevExpress `DxRichEdit` edits the rich-text story of one selected text frame.
- The publication page is an HTML absolute-positioned object surface. Pointer interop performs selection, drag, resize, snapping, and crop panning. All authoritative values are committed back to the C# document model in millimetres.

## File model

A `.pubstudio.json` file contains pages, guides, polymorphic elements, rich-text HTML bytes, and image data URLs. This makes the foundation portable and self-contained. Production work should later support external asset packages to avoid very large JSON files.

## Security boundary

Imported preview HTML is stripped of active elements, event-handler attributes, and `javascript:` URLs before it is rendered. The RichEdit document bytes remain the source for subsequent editing.
