# PublisherStudio GitHub Pages payload

`publisherstudio-kawaii-docs.zip` is the authoritative tracked publishing snapshot for the repository-root GitHub Actions workflow.

The generated directories `PublisherStudio/docs/_site/` and `PublisherStudio/src/PublisherStudio.Web/wwwroot/help-docs/` remain ignored because they are build output. `PublisherStudio\Update-GitHubPagesSnapshot.cmd` validates the generated documentation, materializes any namespace landing pages that DocFX referenced but omitted, then updates two equivalent tracked publication forms:

- `.github/pages/publisherstudio-kawaii-docs.zip` for the GitHub Actions Pages workflow;
- `/docs` with `.nojekyll` for repositories still configured to publish Pages from the default branch `/docs` directory.

Both outputs are prepared and validated before either existing publication payload is replaced. Commit the changed archive and `/docs` mirror together.
