# PublisherStudio 4.1.9 source release checklist

Baseline: `PublisherStudio-4.1.8-source`. New source: `PublisherStudio-4.1.9-source`.

## Modified source files (7)

- `global.json`
- `src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj`
- `src/PublisherStudio.Web/Components/App.razor`
- `src/PublisherStudio.Web/PublisherStudio.Web.csproj`
- `src/PublisherStudio.Web/dotnet-tools.json`
- `src/PublisherStudio.Web/package-lock.json`
- `src/PublisherStudio.Web/package.json`

## New release documentation

- `CHANGELOG-v4.1.9-NET10-SECURITY.md`
- `RELEASE-CHECKLIST-v4.1.9-NET10-SECURITY.md`
- `VALIDATION-v4.1.9-NET10-SECURITY.md`

## Maintained invariants

- All original source files preserved byte-for-byte except the listed modified files.
- No 10.0.12 active `PackageReference` survives in either application.
- Both repository SDK pins are `10.0.402` and relevant NuGet pins are `10.0.13`.
- DevExpress stays at 25.2.10.
- Publish/restore must be confirmed by owner on a build machine; no .NET build, NuGet restore, VS design-time build or runtime smoke test performed here.
