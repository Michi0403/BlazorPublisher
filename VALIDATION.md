# PublisherStudio 2.1.10 validation

This source revision restores the repository-local DocFX 2.78.5 tool manifest, keeps one authored documentation tree, and publishes GitHub Pages from one tracked ZIP artifact. The Pages workflow and validators are name-adapted from the maintained LocalGPT implementation.

The installer and release paths use the same runtime naming contract as LocalGPT, including `linuxx64` and `linuxarm64`, while retaining the PublisherStudio application and setup names and `%LOCALAPPDATA%\PublisherStudio` installation root. Both application and setup archives are acquired and validated before an existing installation is modified.

Source-side checks in the delivered package cover XML and workflow parsing, Python validator syntax, safe Pages archive extraction, version alignment, publish-profile/runtime mapping, missing build-script references, absence of the removed JavaScript SHA allowlist, and absence of duplicate generated documentation trees.

A successful owner-side Windows build is still required to verify .NET 10 compilation, licensed DevExpress assets, DocFX HTML generation, the complete HTML-backed PDF, every runtime publish, setup execution, GitHub release downloads, shortcuts, startup, Pages snapshot refresh, and the real GitHub Pages deployment.
