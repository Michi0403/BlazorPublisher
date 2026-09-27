# PublisherStudio 3.8.0 — DevExpress asset and streaming maintenance

## DevExpress / DevExtreme 25.2.10

- Kept `devextreme-dist` and `devexpress-aspnetcore-spreadsheet` pinned to 25.2.10.
- Replaced full `package-lock.json | ConvertFrom-Json` parsing in `Prepare-DevExpressAssets.ps1` with a Node.js lock probe. This avoids the Windows PowerShell 5.1 failure caused by npm lockfile v3's legal empty-string root package key.
- A lock entry is considered complete when it has the exact required version plus either npm SRI or an exact resolved package URL.
- Removed destructive pre-cleaning of live `wwwroot/vendor` assets and selected `node_modules` folders before lock validation/restore. A failure before asset copying now leaves the previous browser runtime intact.
- Aligned browser-side schema-5 validation with the preparation script: when npm SRI is absent, the exact-version/resolved-URL plus prepared SHA-256 fallback is accepted.
- Retained same-version enforcement between the DevExtreme browser runtime and the runtime-license generator.

## Streaming

- Optional media output, recording output, and hotkey GUID fields are now type-checked before `JsonElement.TryGetGuid`.
- JSON `null` for `targetId` is treated as no target rather than raising `InvalidOperationException`.

## Version

- PublisherStudio application and installer advance from 3.7.9 to 3.8.0.
- Active PublisherStudio browser-module cache busters advance to 3.8.0.
