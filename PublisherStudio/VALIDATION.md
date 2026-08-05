# PublisherStudio 2.1.7 validation

PublisherStudio 2.1.7 is a source-validated documentation-layout milestone. The owner-side Windows build, publish, installer, browser, and GitHub Actions runs remain authoritative.

## Completed checks

- Complete repository contract suite: **134 passed, 0 failed**.
- Architecture policy audit: passed.
- XML documentation coverage remains enforced for public and protected declarations.
- English and German application localization catalogs remain synchronized.
- The Kawaii desktop shell uses one rail-width variable for both side panels and one gap variable for both center gaps.
- The center article has no fixed maximum width and consumes the remaining layout column.
- The desktop shell fills the available viewport for short pages and grows through normal document scrolling for longer pages.
- The pinned GitHub Pages snapshot includes the same CSS, an explicit left documentation rail, the right in-article rail, and the current 2.1.7 PDF.
- The Pages artifact preparation validator passed for **52 HTML pages** and **37 API pages**.
- Kawaii Auto, Light, and Dark persistence, paw branding, path safety, and snapshot hashes remain validated.
- Normal install/update preservation and the `%LOCALAPPDATA%\PublisherStudio` product root remain unchanged.

## Required owner-side checks

1. Run `Prepare-DevExpressAssets.cmd`.
2. Run `Build-LocalDevelopment.cmd` with all guards enabled.
3. Run `Build-Release.cmd` and `Build-AllRuntimes.cmd`.
4. Open `/help-docs/articles/getting-started.html` at 100% zoom on a desktop-width window.
5. Confirm the two rails have equal width and equal distance from the article.
6. Confirm a short article fills one page without an unnecessary vertical scrollbar.
7. Confirm a long article uses only normal document scrolling and no nested panel scrollbar.
8. Confirm Auto, Light, and Dark modes retain the same geometry.
9. Publish a GitHub release and run the pinned-snapshot Pages workflow.
