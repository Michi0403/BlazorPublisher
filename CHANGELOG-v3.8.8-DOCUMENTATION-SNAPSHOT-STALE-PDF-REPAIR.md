# PublisherStudio 3.8.8 — documentation snapshot stale-PDF repair

- Added retry-aware stale versioned PDF pruning to generated DocFX publication roots.
- Replaced silent best-effort output-root deletion with verified replace-not-merge publication semantics.
- Added GitHub Pages snapshot self-healing for stale generated `PublisherStudio-*.pdf` files when the expected current PDF exists.
- Preserved strict failure when the expected current PDF is absent.
- Advanced the durable documentation cache schema to invalidate older publication semantics.
- Extended documentation architecture auditing and repository instructions so stale generated PDFs cannot silently reappear.
