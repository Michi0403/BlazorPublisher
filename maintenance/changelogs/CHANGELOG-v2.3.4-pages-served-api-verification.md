# PublisherStudio 2.3.4 - Pages served API verification

- Aligns final documentation publication with the proven LocalGPT pattern: one authoritative DocFX site tree is copied intact; the API subtree is no longer reconstructed by a PublisherStudio-only special case.
- Requires `api/index.html` before and after the application documentation publication copy.
- Canonicalizes duplicated leading HTML doctypes introduced by repaired/generated pages.
- Adds the SHA-256 of `api/index.html` to Pages validation metadata.
- Adds a per-run `deployment-marker.json` to the Pages artifact.
- Adds a post-deployment GitHub Pages smoke test that waits for the exact commit marker and then requires the served root and `/api/index.html` to return the PublisherStudio Kawaii documentation. A deployment that stages the API but serves a 404 can no longer report green.
