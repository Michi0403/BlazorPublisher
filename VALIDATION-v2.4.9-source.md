# PublisherStudio 2.4.9 source validation

Source/static validation only. No .NET restore, build, publish or runtime execution was performed.

## Passed maintained/static audits

- Application architecture audit: PASS.
- Documentation/1-Wire contract audit: PASS.
- Service resilience audit: PASS for 1,243 service methods; 4 iterator methods and 4 direct Program/Startup methods remain intentionally skipped by the maintained audit.
- JavaScript syntax: `localizationRuntime.js` PASS with Node syntax validation.
- JavaScript diagnostics manifest: regenerated with normalized SHA-256 inventory for all 16 maintained browser JS files.
- English/German localization catalogs: valid UTF-8 JSON, no mojibake markers, 3,035 matching keys.
- Project XML: all `.csproj` files parsed successfully.
- Version policy: Web and InstallerConsole are `2.4.9`; documentation source version is `2.4.9`.

## Targeted localization checks

- Selected-culture runtime fetches `en-US` as its maintained source-text catalog.
- Structured catalog keys are included in the browser source-to-target map.
- UI text translation traverses ordinary text nodes with `document.createTreeWalker`.
- Mutation observation includes `characterData` so later Blazor/DevExpress text updates are translated as well.
- Authored publication content remains excluded from automatic DOM localization.
- Localization integrity guard requires recent media/editor keys and at least 3,000 aligned English/German entries.

## Preserved behavior

Panel Studio geometry/interop, media capture release, Mainframe rendering, exporters, streaming and 1-Wire behavior from 2.4.8 are not redesigned by this patch.
