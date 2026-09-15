# PublisherStudio 3.6.8 source validation

This is a source-only validation record. A .NET build was not run in this environment.

Validated statically:

- PublisherStudio Web, installer-console, package metadata, current documentation identity, and application JavaScript cache-busting identity use 3.6.8.
- `docs/templates/publisherstudio/public/main.js` passes `node --check`.
- The maintained Kawaii theme CSS and JavaScript exactly match the shipped `wwwroot/help-docs/styles` copies.
- The tracked Pages snapshot contains the same Kawaii CSS and JavaScript as the maintained theme source.
- Shipped documentation HTML and tracked Pages HTML use the SHA-256-derived 12-character cache keys for the current Kawaii CSS and JavaScript.
- The deep-space layer contains independent star timing/position variables, colored-star classes, CSS satellite/planet markup and styles, reduced-motion handling, translucent panel variables, and the desktop outer-gutter override.
- Existing historical documentation artifacts were not regenerated or relabeled; the next normal documentation build remains responsible for refreshing generated API/PDF content.

No claim is made that the C# projects were compiler-tested in this environment.
