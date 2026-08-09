# PublisherStudio GitHub Pages snapshot

`publisherstudio-kawaii-docs.zip` is the single tracked Pages payload. Debug/Release documentation builds and `Build-Release.ps1` refresh it from the current, version-matched DocFX output.

The repository workflow intentionally mirrors LocalGPT: `Validate and package Kawaii documentation` -> `Deploy GitHub Pages` using `actions/upload-pages-artifact` and `actions/deploy-pages`.

## Required repository setting

In **Settings -> Pages -> Build and deployment**, set **Source = GitHub Actions**.

Do not use **Deploy from a branch** for this repository. GitHub creates its own `pages-build-deployment` workflow for branch-based Pages; that can deploy after the repository-owned workflow and overwrite the Actions artifact with stale branch content.

For diagnostics or an explicit local refresh, `Update-GitHubPagesSnapshot.cmd` remains available. Automatic seeding can be disabled for a special build with `-p:SeedPublisherStudioGitHubPagesSnapshotOnBuild=false`.
