# Documentation system

PublisherStudio documentation follows the same release shape as LocalGPT while using PublisherStudio names, routes, and source layout.

## Inputs

- maintained Markdown under `docs/articles`;
- public and protected C# XML comments from `PublisherStudio.Web.xml`;
- the built `PublisherStudio.Web.dll` for API metadata.

## Outputs

- searchable DocFX HTML under `wwwroot/help-docs`;
- `PublisherStudio-<version>.pdf`;
- generated API pages;
- `documentation-status.json`;
- a searchable XML-comment catalog exposed by the running app.

## Kawaii design guide

The website and PDF use the same soft pink, violet, and warm neutral palette. Decorative motion respects reduced-motion preferences. Light, dark, and system themes are selectable from a dedicated control that remains above the navigation layer.

The decoration is a smile, not a fog machine: headings stay clear, code remains readable, and technical warnings keep their seriousness.

## GitHub Pages

GitHub Pages uses the same pinned-snapshot workflow as LocalGPT. The tracked `.github/pages/publisherstudio-kawaii-docs.zip` is validated for path safety, the persistent theme selector, the cat-paw favicon, relative project-page links, and API pages before deployment.

After a successful application build, run `Update-GitHubPagesSnapshot.cmd` to refresh that snapshot from the exact generated `wwwroot/help-docs` tree. GitHub Actions publishes the validated snapshot and never guesses that ignored build output exists in a clean checkout.
