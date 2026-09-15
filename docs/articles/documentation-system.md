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

## Deep-space Kawaii visual contract

The website background is intentionally alive without behaving like a repeating wallpaper. Each page load creates a bounded set of independently timed stars with different sizes, brightness ranges, drift vectors, and subtle white, lavender, pink, blue, and warm-yellow tones. A very small number of CSS-built satellites move slowly through the field. The old repeating dot layer remains only as faint depth.

Navigation rails, the article surface, cards, API groups, details panels, and tab panels use translucent glass backgrounds with blur rather than applying opacity to their text. Desktop layouts also reserve a symmetric outer gutter so neither navigation rail hugs the browser edge. `prefers-reduced-motion` keeps the same visual language while stopping star and satellite motion.

LocalGPT documentation follows the same visual contract so the two products feel like one documentation family.
