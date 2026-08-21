# PublisherStudio 2.9.3 — Panel Behaviors & DevExtreme Asset Repair

## Panel Studio behavior/object interface

- Publication elements can persist declarative behavior rules with common triggers such as Click, Double-click, Change, Focus, Blur, Hover and Load.
- Common actions are selectable without hand-writing scripts: click/focus/blur, refresh, show/hide/toggle visibility, enable/disable, set text/value, call an allow-listed component method, page navigation and URL navigation.
- Every addressable publication object receives a stable `publication://.../element/...` address. Panel Studio lists targets across the current panel, nested panels and publication pages so a behavior can address sibling or publication-level objects without CSS-selector guessing.
- The right inspector exposes compact quick actions and a behavior builder; right-click context menus expose the same frequent actions directly on DIV-studio components.
- DevExtreme-aware method suggestions include repaint/reset/refresh/value/selection/filter operations where appropriate. Media and data/live-source elements expose their relevant common methods.
- Existing advanced JavaScript remains available, now with helper snippets and generated object-interface calls so users do not have to memorize the scripting API.
- The standalone browser runtime consumes the same persisted behavior descriptors. C# remains the authoring/domain model; standalone HTML executes the explicit browser behavior runtime rather than pretending arbitrary application C# automatically becomes WebAssembly.

## DevExtreme 25.2.9 browser asset repair

- `Prepare-DevExpressAssets.ps1` clears the generated DevExtreme, Spreadsheet and jQuery vendor targets before preparation and retries Windows removals, including a rename-away fallback for temporary `EPERM` locks.
- Targeted `node_modules` DevExpress package folders are cleared before `npm ci`, preventing npm tar extraction from reusing a partially locked package tree.
- The Node preparation helper verifies restored and copied package versions and records SHA-256/size metadata for prepared runtime assets.
- Standalone export no longer infers a DevExtreme version by regex-searching minified `dx.all.js`. It compares the generated license version marker, prepared asset manifest and copied `devextreme-dist/package.json` instead.
- Export fetches prepared runtime assets with `cache: no-store`, and the running application uses explicit `25.2.9` cache identifiers for DevExtreme runtime/CSS/maps/license. This prevents a browser session from retaining a previous 25.2.8 payload after correct files have been regenerated.
- `THIRD-PARTY-NOTICES.md` is aligned to DevExpress/DevExtreme 25.2.9.
- Generated commercial browser payloads/private licensing material are not included in this source archive; `Prepare-DevExpressAssets.cmd` still prepares them on the licensed build machine.

## Version/toolchain

- PublisherStudio and InstallerConsole are 2.9.3.
- Target framework remains `net10.0`.
- DevExpress/DevExtreme remains 25.2.9.
- LocalGPT 1-Wire protocol remains independently versioned.
