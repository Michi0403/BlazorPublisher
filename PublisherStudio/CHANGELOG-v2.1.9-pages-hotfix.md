# PublisherStudio 2.1.9 Pages publication hotfix

- Materializes missing DocFX namespace landing pages instead of suppressing or ignoring their dead breadcrumb links.
- Keeps strict local-link validation for every ordinary missing, escaping, or root-relative publication URL.
- Packages the validator-prepared documentation tree rather than the unnormalized build directory.
- Maintains a byte-identical repository-root `/docs` mirror with `.nojekyll` alongside the authoritative `.github/pages/publisherstudio-kawaii-docs.zip` snapshot.
- Makes branch-based `/docs` Pages configuration and the dedicated GitHub Actions deployment publish the same payload.
- Replaces the snapshot ZIP and `/docs` mirror through one rollback-capable update transaction.
- Prevents machine-local deployment metadata, Python caches, and interrupted Pages staging directories from leaking into verified source packages.
