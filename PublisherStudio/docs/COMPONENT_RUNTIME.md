# PublisherStudio browser component runtime

PublisherStudio v1.0.41 provides browser-native DevExtreme components as publication elements. This subsystem intentionally does **not** embed arbitrary DevExpress Blazor or ASP.NET Core controls. Exported publications must remain usable as a single HTML file, so only controls available in the bundled non-modular DevExtreme browser runtime are exposed.

## Supported catalogue

| Category | Components |
|---|---|
| Data | Data Grid, Tree List, Scheduler, Pivot Grid |
| Maps | Map, Vector Map |
| Forms/editors | Form, Text Box, Text Area, Number Box, Date Box, Check Box, Select Box, Tag Box |
| Collections | Gallery, Tile View |
| Navigation | Menu, Context Menu, Button |
| Layout | Tab Panel, Multi View, Splitter, Scroll View |

Charts remain PublisherStudio `DataVisualElement` objects. They are not migrated into the generic component element.

## Shared contract

A `DevExtremeComponentElement` stores:

- component kind and title
- page or document scope
- data connection
- fields/editor definitions and lookup datasets
- behavior options
- layout panels
- events/actions
- optional advanced options JSON
- optional normalized CSS classes and inline declarations
- optional explicitly enabled script actions
- map/vector-map provider, layer, drawing, and content-viewport configuration

`PublicationComponentService` creates one plain JSON client contract. The editor, print surface, interactive-presentation export, and website export all use that same contract through `componentRuntime.js`.

## Maps and vector drawing

`Map` uses DevExtreme `dxMap` and can bind rows by latitude/longitude or address, create tooltips, and group ordered points into routes. Provider keys remain publication data and are therefore visible to recipients of exported HTML; use only browser-safe scoped keys.

`VectorMap` uses `dxVectorMap`. PublisherStudio loads the bundled DevExtreme World, Europe, Eurasia, Africa, USA, and Canada data scripts and can also render without a base layer. Publication rows become live markers. The Component Studio stores hand-drawn markers, lines, and polygons as longitude/latitude features and can import Point, LineString, Polygon, MultiLineString, and MultiPolygon GeoJSON. Preview clicks are converted from screen coordinates through the widget's geographic conversion API.

Map and Vector Map are materialized from REST/OData before initial rendering and rebuild their markers, routes, or layers during configured live refreshes.

## Content viewport and CSS

Text/Docx frames, spreadsheet frames, Map, and Vector Map use a shared persisted content viewport: X/Y offset plus scale. In positioning mode the editor drags and wheel-zooms the content instead of moving/resizing the outer publication object. The same transform is applied in print, interactive-presentation export, and website export and composes with text/spreadsheet fit modes.

DevExtreme publication components can specify sanitized CSS class tokens and inline declarations. Selector blocks and script-like CSS are not accepted; advanced visual behavior that cannot be represented safely as declarations should remain in the publication stylesheet/application theme.

## Data modes

### Publication data object

Uses PublisherStudio's existing parsed data objects. This includes tables, JSON/XML/delimited input, spreadsheet-range objects, publication page/object data, web polling, and webhook snapshots. When live HTML fetching is permitted on a web data object, the browser runtime can refresh it; otherwise the embedded snapshot remains available.

### Static snapshot

Embeds the current parsed rows and never performs a browser fetch. Use this for portable or confidential reports that do not need live updates.

### REST

Uses a DevExtreme `CustomStore`. Client mode fetches raw rows and lets DevExtreme process them in the browser. Remote mode sends DevExtreme load options such as filter, sort, skip, take, grouping, and summaries to the endpoint.

Remote mode is a contract, not magic: the endpoint must understand these parameters and return either rows or a processed `{ data, totalCount, summary, groupCount }` result.

CRUD can use separate insert/update/delete URLs and methods. Update/delete endpoints can append the encoded key to the URL. The runtime sends JSON bodies for insert and update.

### OData

Uses the bundled DevExtreme `ODataStore`, with OData version 2, 3, or 4 and a configured key field/key type. DevExtreme performs OData query generation and native store write operations.

## Forms and editors

Forms are generated from the configured field list and support required validation, field-specific editor types, formats, and lookup datasets. A submit action can use the configured store (including OData) or an explicit REST URL. When update is enabled and the form row has its configured key, the store update operation is used; otherwise an enabled insert operation creates a new row.

Standalone editors read their initial value from `InitialValue` or the first source row/primary field. Value-change actions expose both `value` and the primary field name to templates and smart connections.

A mail action opens a browser `mailto:` URL with templated recipient, subject, and body. It prepares the user's mail client; it does not silently send mail.

## Smart actions

Actions can navigate pages, open URLs, prepare mail, refresh, change element visibility, submit values, set another component option, or filter another component.

Document-wide components have a shared component ID. During configuration export, a smart action targeting a shared component is mapped to the corresponding concrete element ID on the current page. This prevents a menu or editor on page 2 from accidentally controlling the page-1 copy.

Custom script uses `new Function` and is disabled unless the component's `AllowCustomScript` flag is enabled. It should be treated as trusted publication code.

## Menus and page navigation

The Component Studio can create a publication-data object containing the current page ID, page name, and menu text. Menu item clicks call the common `PublisherStudioNavigation` API.

The interactive-presentation runtime resolves page ID, name, or index. The website runtime additionally assigns stable hash routes such as `#/dashboard`, uses browser history, and keeps the entire site in one HTML file.

Regenerate the page-navigation dataset after adding, deleting, or renaming pages.

## Layout panels

Splitter, Tab Panel, Multi View, and Scroll View can host nested browser controls. Each panel can bind to a publication data object and choose a child component/editor kind. Nested controls are rendered by the same runtime and require no server-side UI adapter.

## Single-file export

Both HTML modes embed, in order:

1. DevExtreme CSS
2. publication CSS/content
3. jQuery
4. `dx.all.js`
5. bundled DevExtreme vector-map data scripts
6. generated public `devextreme-license.js`
7. PublisherStudio live-data runtime
8. PublisherStudio component runtime
9. presentation or website navigation runtime

The public runtime license must be generated by `Prepare-DevExpressAssets.cmd` on the licensed build machine. The private DevExpress license is never stored in the source package or exported page.

## Endpoint and security rules

A standalone HTML file is a browser application. Direct REST/OData calls therefore follow browser security rules:

- the endpoint must permit the page's origin through CORS
- `file://` pages can be treated as a null origin by servers
- cookies require compatible SameSite/CORS settings and `WithCredentials`
- browser mixed-content rules block HTTP APIs from HTTPS pages
- headers and URLs embedded in the publication can be inspected by the recipient

Do not embed private API keys or unrestricted bearer tokens. Put protected operations behind a local PublisherStudio/LocalGPT service or another scoped backend and expose only the capabilities required by the publication.

## Official DevExtreme references

- CustomStore: https://js.devexpress.com/jQuery/Documentation/ApiReference/Data_Layer/CustomStore/
- ODataStore: https://js.devexpress.com/jQuery/Documentation/ApiReference/Data_Layer/ODataStore/
- Data source stores: https://js.devexpress.com/jQuery/Documentation/ApiReference/Data_Layer/DataSource/Configuration/store/
- Component gallery: https://js.devexpress.com/jQuery/Demos/WidgetsGallery/

## Signal targeting (v1.0.41)

Signal connectors can target a DevExtreme component wrapper or an inner CSS selector. When a Map or VectorMap object is chosen as a motion target and no selector is supplied, PublisherStudio automatically animates the component's inner content source rather than the publication object's outer geometry. Completion selectors may address generated DevExtreme HTML/SVG nodes when a specific chart point, map region, button, or other sub-element must receive a click, hover, highlight, class, or visibility action.

These actions are executed by the embedded offline signal runtime. Component live-data refresh remains governed by the component's data connection and may require network access, but the signal sequence itself does not.

## Menu navigation and live publication schemas (v1.0.43)

Menu rows use stable page IDs, not page labels. Editable menu items store one explicit destination: publication page, external URL, or none. Data-driven menus can map `text`, `targetPageId`, `url`, `id`, and `parentId` from any publication data object. The built-in `Publication pages` object provisions these fields and is regenerated from the current document during rendering and export.

Component Studio derives source-property choices from the selected data object's resolved columns. Direct REST/OData fields become selectable after endpoint discovery. Manual menus deliberately bypass data/API and CRUD requirements.

PublisherStudio loads its pinned DevExtreme `dx.all.js` once. The DevExpress Blazor resource manager still emits the other registered resources but has `CommonResources.DevExtremeJS` unregistered to avoid duplicate bundle initialization.

## Component resilience and canvas geometry (v1.0.44)

Manual Menu and Context Menu configurations are self-contained and do not require a publication data object. Data-driven variants continue to resolve the selected source. The runtime normalizes date fields discovered from both component fields and publication-data schema metadata before passing rows to DevExtreme; Scheduler rows with unusable start dates are skipped.

On the editor canvas, a pointer interaction inside a DevExtreme host remains a native component click until the movement threshold is crossed. Crossing that threshold converts the interaction to a publication-object move without first recreating the Blazor component host.

Publication objects may contain custom connector ports expressed as normalized `XPercent`/`YPercent` coordinates. Connector endpoints can reference those ports by ID. Curved connectors persist two page-space Bézier control points; the editor exposes endpoint, control, and route handles and also allows dragging the selected path to translate both controls.


## Orientation and hidden-container resizing (v1.0.45)

DevExtreme's base Menu CSS assigns `height:100%` to every menu item wrapper. PublisherStudio keeps that behavior for horizontal menubars but overrides it for vertical menus so each item uses its natural row height. Vertical menus remain full-width and scroll inside the publication object only when necessary.

The browser runtime attaches a resize observer to every component host. It calls the component's supported `updateDimensions()` and `repaint()` methods after object resizing or hidden-to-visible transitions. Tab Panel, Multi View, and Splitter additionally refresh their nested component hosts after selection, resize, collapse, and expansion events. The same runtime and collected application CSS are embedded into presentation and website HTML exports.
