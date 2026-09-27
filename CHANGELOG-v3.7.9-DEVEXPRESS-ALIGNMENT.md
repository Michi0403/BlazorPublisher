# PublisherStudio 3.7.9 — DevExpress browser/runtime alignment

## Fixed

- Aligns the npm/browser DevExpress dependency lane with the already configured .NET DevExpress 25.2.10 package lane. `devextreme-dist` and `devexpress-aspnetcore-spreadsheet` now request 25.2.10 instead of 25.2.9.
- Refreshes exact npm lock versions and adds a stale-lock repair path before `npm ci`, preventing `Prepare-DevExpressAssets.ps1` from generating a 25.2.9 runtime-key marker that later fails the `PublisherStudio.Web.csproj` publish guard.
- Supports npm lock metadata that can omit SRI for these packages by retaining exact package versions/resolved URLs and validating prepared DevExtreme assets with SHA-256. No fake integrity value is written.
- Updates active DevExtreme script, stylesheet, VectorMap, Spreadsheet and localization cache-busters to 25.2.10.
- Updates PublisherStudio browser-module cache-busters and both application/installer version identities to 3.7.9.
- Updates current third-party notices to identify DevExpress/DevExtreme 25.2.10.

## Preserved

- The exact-version runtime-key guard remains enabled. A publish still fails rather than silently mixing DevExtreme and DevExpress versions.
- `Prepare-DevExpressAssets.ps1` remains the authoritative place that clears generated vendor payloads, restores pinned npm packages and generates the public runtime key on the licensed build machine.
- Existing editor, deployment, documentation, InteractiveServer and cross-platform behavior is otherwise unchanged.

## Validation boundary

No `dotnet`, MSBuild, NuGet, publish, installer or GitHub operation was run for this handoff. Source consistency and maintained JavaScript syntax were checked independently.
