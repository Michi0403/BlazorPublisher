# PublisherStudio 2.3.7 — GitHub Pages LocalGPT parity

- Replaces the repository-owned PublisherStudio Pages workflow with the same two-job workflow structure used by LocalGPT.
- Removes the additional served-site verification/deployment-marker job from the repository workflow so there is one repository-owned Pages deployment path.
- Keeps the current version-matched PublisherStudio Pages ZIP, DocFX validation, build/release snapshot seeding, in-app documentation, release packaging, and API generation unchanged.
- Documents the required GitHub Pages repository setting: **Source = GitHub Actions**. A GitHub-generated `pages-build-deployment` job is external to the repository and indicates branch-based Pages is still enabled.
- Bumps PublisherStudio.Web and PublisherStudio.InstallerConsole to 2.3.7.
