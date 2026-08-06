# PublisherStudio GitHub Pages payload

`publisherstudio-kawaii-docs.zip` is the single tracked publishing snapshot for GitHub Pages.

The authored `docs/` tree and generated `docs/_site/` output are not branch-deployment mirrors. GitHub Actions validates and extracts this ZIP, adds `.nojekyll`, and deploys the resulting static artifact directly.

After a successful owner-side documentation build, run `Update-GitHubPagesSnapshot.cmd` to replace the snapshot with the complete contents of `src/PublisherStudio.Web/wwwroot/help-docs/`, then commit only the changed archive and intentional source changes.
