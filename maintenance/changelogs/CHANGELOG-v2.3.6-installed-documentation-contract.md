# PublisherStudio 2.3.6 — installed documentation contract

- Keeps the 2.3.5 in-app viewer and the existing GitHub Pages workflow unchanged.
- Release archives now fail validation unless `wwwroot/help-docs/index.html`, `api/index.html`, status metadata and the versioned PDF are present.
- The installer rejects incomplete application archives before extraction and verifies the installed HTML/API documentation immediately after extraction.
- This makes the customer installation use the same verified documentation payload that was used for the release and Pages snapshot.
