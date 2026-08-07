# PublisherStudio 2.2.5 repair release

This source candidate keeps the 2.2.4 application feature set and repairs the release-facing regressions that accumulated after the 2.1.9/2.1.10 documentation milestone.

- Restores the last proven Kawaii website shell instead of the later root-rail/responsive override.
- Restores the browser JavaScript diagnostics gate and its reviewed hash inventory; the documentation viewer is guarded too.
- Restores strict public/protected XML documentation coverage and documents the newer organic profile members.
- Repairs `New-VerifiedSourcePackage.ps1` for the flattened repository layout.
- Makes GitHub Pages validate the tracked snapshot against the project version before deployment.
- Requires a substantial, complete HTML-backed PDF instead of accepting a tiny fallback.
- Keeps the authored `docs/` tree separate from generated Pages output.

The owner-side Windows .NET 10 / DevExpress build remains authoritative for compilation and release publishing.
