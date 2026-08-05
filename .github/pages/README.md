# PublisherStudio GitHub Pages payload

`publisherstudio-kawaii-docs.zip` is the tracked publishing snapshot for GitHub Pages.

The generated directories `PublisherStudio/docs/_site/` and
`PublisherStudio/src/PublisherStudio.Web/wwwroot/help-docs/` are deliberately ignored by
Git, so a clean GitHub Actions checkout cannot publish either directory directly.
The repository-root workflow validates and extracts this ZIP instead.

After a successful PublisherStudio documentation build, run
`PublisherStudio\Update-GitHubPagesSnapshot.cmd`, then commit the changed archive.
