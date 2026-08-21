# PublisherStudio 2.9.5 — Panel Studio Razor Parser Compatibility

## Build repair

- Corrected the new Panel Studio behavior/helper markup that collided with Razor directive names. Loop variables named `page` and `helper` are now emitted through explicit `@(...)` expressions, so Razor does not interpret `@page` or `@helper` as directives.
- Corrected common-method captions that used `@method()` and were compiled as attempted method invocations. Captions now render the method name plus `()` as an explicit expression.
- The repair directly addresses the authoritative Windows build errors reported at PanelStudio.razor lines 349, 365, 421, 430 and 558: RZ9979, RZ2005, RZ1011, RZ1002 and CS0149.

## Retained functionality

- Panel Studio publication object addresses, declarative behaviors, common component methods, right-click actions and JavaScript helper UX remain intact.
- The DevExtreme 25.2.9 preparation/runtime-license provenance repair from 2.9.4 remains intact: exact `devextreme@25.2.9` license generator resolution, generated-key staging, versioned browser resources, no-store export reads and prepared SHA-256/provenance metadata.
- The internal `devextreme-dist` / spreadsheet package metadata mismatch remains diagnostic only; the npm lock integrity and prepared asset hashes remain authoritative.
- Target framework remains `net10.0`; DevExpress/DevExtreme remains 25.2.9.
- No database/schema migration was introduced.
