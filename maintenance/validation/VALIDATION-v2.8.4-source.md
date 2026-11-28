# PublisherStudio 2.8.4 source validation

This package is intentionally source-only. No `dotnet`, MSBuild, restore, build, publish, or pack command was run while preparing it.

Static checks performed include:

- targeted 2.8.4 compile/architecture audit;
- method-granular Razor component resilience audit;
- broad all-service resilience audit;
- application architecture audit;
- async continuation audit;
- Panel Studio persistence and interaction audits;
- documentation/1-Wire audit;
- XML documentation coverage audit;
- JSON and project XML parsing;
- JavaScript syntax/diagnostics inventory checks where available without a .NET build;
- ZIP integrity verification after packaging.

The reported `CS0103` is repaired by restoring a declared `selectedElementIds` value, produced by `PublicationEditorTextService`, before the `initializeCanvas` anonymous-object payload references it.

The new component resilience policy intentionally does not auto-wrap thousands of existing Razor methods in one maintenance release. Existing legacy signatures are explicitly inventoried; new Razor methods are rejected unless they include method-local try/catch and structured logging. This gives PublisherStudio an enforceable migration path instead of weakening the rule or performing a broad mechanical rewrite that could destabilize the editor/layer system.
