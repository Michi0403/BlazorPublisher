# PublisherStudio 2.9.1 source validation

This is a source-only validation record. No `dotnet`, MSBuild, NuGet restore, build, publish, pack, EF migration, or database command was executed while preparing this archive.

Validated with static/non-.NET tooling:

- PublisherStudio Web and InstallerConsole versions are 2.9.1;
- DevExpress 25.2.9, net10.0, dotnet-ef 10.0.11, and LocalGPT 1-Wire 2.1.1 remain unchanged;
- XML documentation coverage/quality passes for 6,064 direct maintained C# declarations across 243 files, including 345 individually documented enum members;
- Razor XML documentation coverage/quality passes for 47 component types and 3,311 explicit `@code` declarations;
- summaries must be non-empty and contextual; applicable `<param>`, `<typeparam>`, `<returns>`, and `<value>` elements must contain explanatory text;
- the XML documentation enhancer is idempotent after the 2.9.1 enrichment pass;
- the application architecture, async-continuation, component-resilience, service-resilience, iterator-exception, and prerender-interop static audits pass;
- runtime/editor C#/Razor source is comment-equivalent to the 2.9.0 baseline except for deliberate 2.9.1 version/cache-buster identifiers;
- no EF migration/database schema change was introduced;
- generated `bin`, `obj`, cache, and Python bytecode directories are excluded from the returned archive.
