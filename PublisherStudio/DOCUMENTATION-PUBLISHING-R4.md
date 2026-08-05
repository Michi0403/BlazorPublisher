# GitHub Pages publishing correction R4

The previous workflow tried to publish
`src/PublisherStudio.Web/wwwroot/help-docs` directly. That directory is
listed in `.gitignore`, so it exists after a local documentation build but does not
exist in a clean GitHub Actions checkout.

R4 publishes `.github/pages/publisherstudio-kawaii-docs.zip`, a tracked snapshot of the
same generated site. The workflow performs path-safety checks, validates the Kawaii
theme, persistent theme selector, API pages and cat-paw favicon, then uploads the
extracted tree as the Pages artifact.

The deployment job references the `github-pages` environment. If that environment
was deleted, GitHub creates it again when the default-branch deployment runs. The
workflow no longer deploys from release tags, so the tag protection that rejected
`v2.1.4` is not involved.


After a successful PublisherStudio build, run `Update-GitHubPagesSnapshot.cmd` to replace the tracked snapshot with the exact generated DocFX tree from `wwwroot/help-docs`. This is the PublisherStudio-named maintenance step; the workflow and validator stay line-for-line shaped like LocalGPT.
