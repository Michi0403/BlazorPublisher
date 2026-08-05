# PublisherStudio 2.1.1 validation

The source package is validated structurally in this environment. The final Windows build and one-click installation remain the release authority.

## Completed source checks

- Repository contract tests cover the LocalGPT-aligned installer, application architecture, documentation, localization, streaming, editors, and release workflow.
- Architecture audit passes in combined PublisherStudio mode.
- XML documentation coverage protects maintained public and protected C# declarations.
- English and German localization catalogs have matching keys.
- JSON, XML/MSBuild, YAML, JavaScript, Python, and Markdown link checks pass.
- The installer contract fixes the root to `%LOCALAPPDATA%\PublisherStudio`, extracts application and setup wrappers into that root, and maintains only Install, Update, Start, and Folder shortcuts.
- GitHub Pages extraction and the Kawaii system/dark/light documentation theme remain covered.

## Required Windows checks

1. Run `Prepare-DevExpressAssets.cmd`.
2. Run `Build-LocalDevelopment.cmd` with every guard enabled.
3. Confirm `bin/Debug/net10.0/wwwroot/help-docs/index.html`, `documentation-status.json`, `PublisherStudio.Web.xml`, and `PublisherStudio-2.1.1.pdf` exist.
4. Run `Build-Release.cmd` and `Build-AllRuntimes.cmd`.
5. Delete or rename any test installation, then double-click the published `PublisherStudio.Setup.exe` with no arguments.
6. Confirm the only product root is `%LOCALAPPDATA%\PublisherStudio` and that the application and setup runtime wrappers are both present there.
7. Confirm Desktop and Start Menu contain working PublisherStudio Install, Update, Start, and Folder entries.
8. Run Update while setup is installed, confirm the temporary-copy handoff replaces setup successfully, and confirm the application starts on `http://127.0.0.1:58071`.
9. Open `/help`, HTML documentation, API reference, and PDF; test system, dark, and light modes at 100% zoom without horizontal page scrollbars.
10. Publish a GitHub release and run **Publish shipped PublisherStudio documentation** with GitHub Pages set to **GitHub Actions**.
