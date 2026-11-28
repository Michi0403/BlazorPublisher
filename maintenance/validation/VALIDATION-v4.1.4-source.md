# PublisherStudio 4.1.4 source validation

## Scope

Source-only repair of the DevExpress browser-asset provisioning contract. No .NET build, restore, publish, GitHub access, or licensed DevExpress asset generation was performed in this environment.

## Dependency baseline

- `PublisherStudio.Web.csproj` retains `DevExpressVersion` **25.2.10**.
- `package.json` and `package-lock.json` retain `devextreme-dist` and `devexpress-aspnetcore-spreadsheet` **25.2.10**.
- maintained Microsoft/System .NET patch references remain **10.0.12**.
- repository scans contain zero occurrences of the superseded DevExpress and .NET patch versions.

## Provisioning behavior

- ordinary non-design-time builds default `PrepareSpreadsheetAssetsOnBuild` to `true`;
- `PrepareSpreadsheetClientAssets` always runs the lightweight `-EnsureCurrent` preflight before compilation;
- a valid prepared 25.2.10 payload exits without Node/npm/npx work;
- missing/stale/mixed/hash-invalid payloads invoke the existing PowerShell provisioning flow automatically;
- the provisioning script requires the project `DevExpressVersion`, `devextreme-dist`, and `devexpress-aspnetcore-spreadsheet` declarations to agree before seeding; and
- an explicit `PrepareSpreadsheetAssetsOnBuild=false` remains only for delegated builds that reuse already-prepared assets.

## Static checks performed

- XML parsing of the changed project files succeeds;
- JSON parsing of `package.json` and `package-lock.json` succeeds;
- current application/installer/package/cache-buster identities are 4.1.4;
- active DevExpress declarations/cache-busters remain 25.2.10;
- no superseded DevExpress/.NET patch references are present;
- application architecture policy audit passed;
- async continuation and async-boundary architecture audits passed;
- service resilience audit passed;
- Razor maintenance architecture audit passed;
- transient UI-state ownership audit passed;
- Panel Studio persistence audit passed;
- XML documentation coverage/quality validation passed;
- cross-platform boundary audit passed;
- the unchanged DevExpress Node helper modules pass `node --check`; and
- the complete `build/` tree remains byte-for-byte unchanged from 4.1.3.

PowerShell itself is not installed in this environment, so the provisioning script was not executed here. Its actual restore/license-generation path must run on the licensed development/build machine.

A licensed Windows/macOS/Linux developer machine must perform the first normal build to exercise the actual DevExpress restore/license generation path.
