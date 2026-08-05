# PublisherStudio GitHub Pages payload

`publisherstudio-kawaii-docs.zip` is the tracked publishing snapshot for GitHub Pages.

The generated directories `docs/_site/` and
`src/PublisherStudio.Web/wwwroot/help-docs/` are deliberately ignored by Git, so a
clean GitHub Actions checkout cannot publish either directory directly. The workflow
validates and extracts this ZIP instead.

After a successful owner-side documentation build, run `Update-GitHubPagesSnapshot.cmd`
to replace the snapshot with the complete contents of the app's `wwwroot/help-docs`
directory, then commit the changed archive.
