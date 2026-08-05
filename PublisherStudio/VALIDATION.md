# PublisherStudio 2.1.9 validation

The maintained-source validation covers installer asset selection, exact AppData layout, archive wrappers, deterministic release ZIP creation, version alignment, idempotent documentation source rewriting, repository-root GitHub Pages discovery, pinned snapshot version/PDF/link validation, DocFX namespace-page repair, byte-identical `/docs` branch mirroring, build-guard consistency, architecture policies, and application regression contracts.

The repository test suite passes 147 tests after the Pages repair. The exact eleven missing namespace-link cases reported from the owner build normalize into four generated namespace landing pages while an unrelated missing file remains a hard validation failure.

A successful owner-side Windows release build must still confirm the .NET 10 publishes, licensed DevExpress assets, complete DocFX HTML/PDF payload, all runtime archives, setup execution, exact GitHub release asset downloads, shortcuts, application startup, snapshot refresh, and one real GitHub Pages deployment.
