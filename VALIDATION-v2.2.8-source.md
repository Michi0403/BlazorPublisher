# PublisherStudio 2.2.8 source validation

Static/source-side validation performed without claiming a .NET compile in this environment:

- MSBuild XML parses after adding the post-documentation Pages snapshot target.
- The generated namespace-page template now has `html lang="en"` while retaining viewport, title, main/article landmarks, theme assets, and local API links.
- Snapshot seeding is limited to non-design-time Windows Debug/Release PublisherStudio.Web builds and can be disabled with `SeedPublisherStudioGitHubPagesSnapshotOnBuild=false`.
- Manual snapshot selection requires a matching `documentation-status.json` version instead of blindly preferring Release output.
- The tracked 2.2.5 Kawaii snapshot still validates as a complete artifact (607 HTML pages / 591 API pages, including `api/index.html`); the first real 2.2.8 build is expected to replace it automatically.
- `PublisherDocumentationViewerService.NormalizeUrl` retains the user-confirmed `normalized.StartsWith('/')` implementation.
