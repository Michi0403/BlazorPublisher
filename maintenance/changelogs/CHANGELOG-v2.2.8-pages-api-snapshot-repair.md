# PublisherStudio 2.2.8 — GitHub Pages API snapshot repair

- Debug and Release builds now validate and regenerate `.github/pages/publisherstudio-kawaii-docs.zip` from the documentation tree produced by that exact build.
- The snapshot target receives the current build output explicitly, preventing stale Release documentation from overriding fresh Debug documentation.
- Manual `Update-GitHubPagesSnapshot.cmd` now selects only a Debug/Release documentation tree whose version matches `PublisherStudio.Web.csproj`.
- Synthetic DocFX namespace landing pages now include `lang="en"`, fixing the Pages accessibility rejection seen for `PublisherStudio.Controllers.Streaming`, `PublisherStudio.Hubs`, `PublisherStudio.Services.MediaStudio`, and `PublisherStudio.Services.PictureStudio`.
- Because the Pages validator already requires `api/index.html`, a successful automatic snapshot now necessarily contains the API reference root before GitHub Actions can deploy it.
- Stale marker-bearing DocFX PDF link-validation placeholders are removed safely before documentation generation, while real authored PDFs are left untouched.
- The 2.2.7 documentation-viewer viewport polish and the user-confirmed `NormalizeUrl` `StartsWith('/')` compiler fix remain intact.
