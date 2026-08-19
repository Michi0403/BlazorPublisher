# PublisherStudio 2.9.1 — XML documentation completeness

## Repository-wide documentation completion

- Re-audited every maintained PublisherStudio C# declaration regardless of accessibility, including classes, interfaces, controllers, services, extensions, records, structs, enums, constructors, methods, properties, fields, events, delegates, and nested types.
- Closed a previous audit blind spot by documenting and enforcing individual enum members. The 2.9.0 source had 345 enum values without XML summaries even though their containing enums were documented.
- Re-validated all Razor component types through their `.razor.cs` partial declarations and all explicit declarations authored inside Razor `@code` blocks.
- XML documentation now covers 6,064 direct C# declarations across 243 maintained C# files plus 3,311 explicit Razor `@code` declarations across 47 Razor components.
- Required `<param>`, `<typeparam>`, `<returns>`, and `<value>` elements must contain explanatory text, not merely exist as empty XML elements.
- Existing meaningful authored comments are preserved; the enhancer fills missing documentation and enriches only documentation that fails the contextual quality gate.

## Documentation tooling

- Upgraded `build/xml_documentation.py` so enum values participate in the same deterministic declaration scan as classes, methods, properties, fields, and events.
- Strengthened tag validation to reject empty contract documentation.
- Upgraded `build/razor_xml_documentation.py` so Razor component type and `@code` member validation use the same contextual documentation rules as ordinary C# source.
- Simplified the Python documentation entry points so both C# and Razor validation receive the same `src` root, avoiding path-dependent omissions.
- A second enrichment pass is idempotent and reports no source documentation changes.

## Source integrity

- This release is documentation/tooling-only apart from the 2.9.1 assembly/version and JavaScript cache-buster identifiers.
- PublisherStudio runtime/editor logic from 2.9.0 is otherwise unchanged.
- The 2.9.0 recovery-cancellation and WebM insertion repairs remain intact.
- DevExpress remains 25.2.9, .NET remains net10.0, and the LocalGPT 1-Wire protocol remains 2.1.1.
- No EF migration or database schema change was introduced.
