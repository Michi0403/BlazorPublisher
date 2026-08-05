# PublisherStudio 2.1.9 release completion

PublisherStudio 2.1.9 completes the release-facing contracts around installation, archive creation, documentation publication, and GitHub Pages deployment.

The application and setup remain separate runtime-specific release assets. The setup downloads exact asset names, validates both ZIP wrappers before extraction, installs below `%LOCALAPPDATA%\PublisherStudio`, creates the selected shortcuts, and starts the local application.

GitHub Pages is driven by the repository-root `.github/workflows/publish-shipped-docs.yml` workflow. The tracked `.github/pages/publisherstudio-kawaii-docs.zip` snapshot is checked against the current project version, PDF manifest, local links, theme assets, and API payload before it is extracted into the Pages artifact. Refresh it after a successful documentation build with `PublisherStudio\Update-GitHubPagesSnapshot.cmd`.

Owner-side Windows publishing remains the authoritative compiled verification because the source package does not contain private DevExpress build credentials.


Pages completion: the updater now materializes omitted DocFX namespace pages, validates the repaired tree, and transactionally updates both the Actions snapshot and the `/docs` no-Jekyll branch mirror.
