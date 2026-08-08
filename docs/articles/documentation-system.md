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

GitHub Pages uses the same pinned-snapshot workflow as LocalGPT. The tracked repository-root `.github/pages/publisherstudio-kawaii-docs.zip` is validated for path safety, the persistent theme selector, the cat-paw favicon, relative project-page links, and API pages before deployment.

A successful Windows Debug or Release build now validates the exact generated `wwwroot/help-docs` tree and refreshes `.github/pages/publisherstudio-kawaii-docs.zip` automatically. `Update-GitHubPagesSnapshot.cmd` remains available for an explicit refresh and only accepts generated documentation whose status version matches the current project. GitHub Actions validates and publishes that single no-Jekyll artifact, including `api/index.html`. The authored `docs` tree is never replaced by generated Pages output.