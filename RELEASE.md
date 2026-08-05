# PublisherStudio 2.1.9

The maintained application source is in `PublisherStudio/`. Repository-level automation, including GitHub Pages deployment, is intentionally stored in `.github/` where GitHub Actions can discover it.

See `PublisherStudio/RELEASE.md`, `PublisherStudio/CHANGELOG-v2.1.9.md`, and `PublisherStudio/VALIDATION.md`.


Pages completion: the updater now materializes omitted DocFX namespace pages, validates the repaired tree, and transactionally updates both the Actions snapshot and the `/docs` no-Jekyll branch mirror.
