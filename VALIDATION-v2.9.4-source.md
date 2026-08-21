# PublisherStudio 2.9.4 source validation

This archive is **source-only and not compiled** in the preparation environment. No `dotnet`, MSBuild, NuGet restore/publish or EF command was run, and no private DevExpress license, generated runtime key, `node_modules`, or generated licensed vendor payload is packaged.

Static validation completed against the modified source:

- JavaScript syntax checks passed for `publisherInterop.js`, `prepare-devexpress-assets.mjs`, and `resolve-devextreme-package-root.mjs`;
- a synthetic preparation probe reproduced the reported condition where `devextreme-dist/package.json` says 25.2.8 while the project/npm lock says 25.2.9; preparation continued, overlaid the authoritative `devextreme@25.2.9` runtime, and emitted schema-4 asset metadata rather than throwing the false 2.9.3 error;
- XML documentation coverage/quality passed for 6,124 direct C# declarations and 3,352 direct Razor members;
- architecture policy passed;
- async continuation policy passed for 78 source files;
- service resilience passed for 1,325 service methods plus four policy-compliant iterators;
- component resilience passed for 2,639 component methods;
- iterator exception policy passed;
- prerender JavaScript safety passed;
- Panel Studio persistence passed after refreshing the reviewed `publisherInterop.js` diagnostics SHA-256;
- `App.razor` and standalone export both load the generated non-modular runtime key directly after `dx.all.js`;
- preparation resolves the exact `devextreme@25.2.9` package once, invokes that package's own `bin/devextreme-license.js`, and exposes the same package root to the browser-asset copier;
- the Node asset copier no longer owns, guesses, or validates a `wwwroot/vendor/devextreme-license.js` path;
- `devextreme-dist` internal `package.json` version mismatches are warnings only; npm lock integrity, exact `devextreme` runtime provenance and prepared SHA-256 values are authoritative.

The user's licensed Windows .NET 10 + DevExpress build remains authoritative for compilation and actual runtime-key generation.
