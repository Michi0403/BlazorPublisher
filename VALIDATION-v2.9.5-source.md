# PublisherStudio 2.9.5 source validation

This archive is **source-only and not compiled** in the preparation environment. No `dotnet`, MSBuild, NuGet restore/publish or EF command was run, and no private DevExpress license, generated runtime key, `node_modules`, or generated licensed vendor payload is packaged.

Static validation completed against the modified source:

- the five Razor parser collision sites reported by the authoritative Windows build are rewritten with explicit Razor expressions;
- no direct `@page.`, `@helper.` or `@method()` expression remains in `PanelStudio.razor`;
- JavaScript syntax checks pass for maintained PublisherStudio browser modules available to Node syntax validation;
- the reviewed JavaScript diagnostics SHA-256 inventory was refreshed for the 2.9.5 `publisherInterop.js` cache/release marker;
- the 2.9.4 DevExtreme preparation/runtime-key provenance assertions remain present;
- Panel Studio behavior persistence/object-interface source assertions remain present;
- target framework remains .NET 10 and the application/installer/package versions are aligned at 2.9.5;
- archive hygiene excludes build output, `node_modules`, Python caches and generated licensed DevExpress vendor assets.

The user's licensed Windows .NET 10 + DevExpress build remains authoritative for Razor compilation and runtime validation.
