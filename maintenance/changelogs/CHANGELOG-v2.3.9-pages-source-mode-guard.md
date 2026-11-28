# PublisherStudio 2.3.9 - GitHub Pages source-mode guard

## Why this change exists

GitHub Actions logs proved that PublisherStudio had two independent Pages publishers:

1. `.github/workflows/publish-shipped-docs.yml` uploaded and deployed the validated DocFX/Kawaii artifact containing `api/index.html`.
2. GitHub's repository-level legacy Pages configuration also launched the generated `pages-build-deployment` workflow, which checked out `main`, ran Jekyll against `./docs`, and could subsequently replace the deployed artifact with the old documentation-source tree.

The second workflow is not stored in `.github/workflows`; GitHub creates it from the repository Pages setting when the publishing source is a branch/path such as `main /docs`.

## What 2.3.9 changes

- Keeps the PublisherStudio workflow structurally aligned with the working LocalGPT workflow.
- Adds a pre-deployment guard which queries the repository Pages configuration and requires `build_type=workflow`.
- If the repository is still configured for legacy branch publishing, the PublisherStudio workflow now fails visibly instead of reporting a misleading green deployment that can be overwritten afterwards.
- The error tells the maintainer to select **Settings -> Pages -> Build and deployment -> Source -> GitHub Actions** and then rerun the workflow.
- The source documentation version labels are advanced to 2.3.9 as well, so stale 2.2.x labels cannot survive in the source tree.

## Important boundary

Changing the repository Pages publishing source is an administrative repository setting. A normal workflow `GITHUB_TOKEN` with `pages: write` is not guaranteed to have the Administration permission required by GitHub's Pages settings REST endpoint, so this source package deliberately does not secretly mutate that repository setting. It detects the wrong mode and refuses to publish until the repository is configured exactly like LocalGPT.
