# PublisherStudio browser component runtime

PublisherStudio v1.0.39 adds browser-native DevExtreme components as publication elements. This subsystem intentionally does **not** embed arbitrary DevExpress Blazor or ASP.NET Core controls. Exported publications must remain usable as a single HTML file, so only controls available in the bundled non-modular DevExtreme browser runtime are exposed.

## Supported catalogue

| Category | Components |
|---|---|
| Data | Data Grid, Tree List, Scheduler, Pivot Grid |
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
- optional explicitly enabled script actions

`PublicationComponentService` creates one plain JSON client contract. The editor, print surface, interactive-presentation export, and website export all use that same contract through `componentRuntime.js`.

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
5. generated public `devextreme-license.js`
6. PublisherStudio live-data runtime
7. PublisherStudio component runtime
8. presentation or website navigation runtime

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
