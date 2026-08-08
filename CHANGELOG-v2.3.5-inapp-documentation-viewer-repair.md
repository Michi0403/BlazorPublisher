# PublisherStudio 2.3.5 — in-app documentation viewer repair

- Keeps the 2.3.4 GitHub Pages API deployment and served-site verification pipeline unchanged.
- Replaces the runtime documentation viewer's native-dialog JavaScript synchronization with a Blazor-owned modal overlay.
- HTML, PDF and API views therefore remain in-app features independent of the static GitHub Pages publication path.
- Keeps the existing same-origin URL validation, Kawaii DocFX output, PDF generation, Pages snapshot and post-deploy API verification intact.
