# PublisherStudio 2.5.8 changelog

## XML documentation parser/build hygiene repair

- Carried the corrected shared C# XML-documentation parser into PublisherStudio so both repositories use the same declaration scanning rules.
- Fixed expression-bodied members whose expression owns an object/collection/switch initializer. The scanner now consumes the full expression through its real `};`/`;` terminator instead of later treating constructor arguments or statements inside the expression as new declarations.
- Tightened type-declaration recognition so executable pattern expressions such as `if (... is null)` cannot become false documentation insertion sites.
- Removed **16 definitely invalid generated XML documentation blocks** nested inside executable bodies/collection initializers that were exposed by the repaired parser.
- Fixed `inheritdoc` enrichment so inherited documentation remains authoritative. The generator no longer adds local parameter/type-parameter/return/value tags on top of inherited contract tags, preventing the same duplicate-parameter class of DocFX warning repaired in LocalGPT.
- Re-ran the sophisticated documentation enhancer with the repaired parser. Coverage/quality now validates **5,360 direct maintained declarations across 180 C# source files**, and a second pass performs **0 additions / 0 enrichments**.

## Runtime/source preservation

- No intentional PublisherStudio runtime/editor behavior was changed by this maintenance release.
- Existing Panel Studio persistence/geometry, Organic/1-Wire Ping/Pong and capability synchronization, AI Assist, media/export/streaming, viewport behavior, and installer behavior are preserved.

## Version

- PublisherStudio Web and installer: **2.5.8**.
- The consumed LocalGPT 1-Wire protocol remains **2.1.1**; no protocol schema changed.
- This is a source-only repair package. No .NET/MSBuild/PowerShell/DocFX build was executed in this environment; the owner Windows build remains authoritative.
