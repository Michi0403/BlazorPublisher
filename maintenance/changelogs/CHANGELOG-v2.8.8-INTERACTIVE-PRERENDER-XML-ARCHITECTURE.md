# PublisherStudio 2.8.8 — InteractiveServer prerender and Razor XML architecture

- Restores the LocalGPT-style page render contract: routed application pages own `@rendermode InteractiveServer` (prerendering enabled by default) while nested editor components inherit that circuit. The JavaScript diagnostics bridge remains the one intentional `prerender: false` browser-only island.
- Prevents prerender-only component instances from issuing JavaScript interop during disposal. Browser cleanup is now gated by state established only after successful `OnAfterRenderAsync` attachment.
- Guards Editor background dirty-state JavaScript synchronization until the interactive circuit has attached.
- Adds a prerender JavaScript interop architecture audit that rejects JavaScript calls from pre-render lifecycle methods and rejects unguarded JavaScript disposal.
- Adds XML documentation to explicit Razor component fields, properties, methods, nested types, records, and enums, plus a documented partial class for every Razor component.
- Extends the XML documentation build gate so both direct C# source and Razor `@code` members must remain fully documented with summary/param/returns/value/typeparam quality checks.
- Preserves the existing five reviewed render boundaries, with four prerendered application pages plus one browser-only diagnostics island.
- No 1-Wire protocol change.
