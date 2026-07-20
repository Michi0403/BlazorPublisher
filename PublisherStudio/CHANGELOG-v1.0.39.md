# PublisherStudio v1.0.39 — browser-native application components, REST/OData, and single-file websites

## Professional interactive components

PublisherStudio can now insert a curated set of DevExtreme controls as normal publication objects. The catalogue is deliberately limited to browser-native components that run from the already bundled `dx.all.js` runtime and therefore remain compatible with the single-file HTML export model.

Supported publication components:

- Data Grid and Tree List
- Scheduler and Pivot Grid
- Edit Form
- Text Box, Text Area, Number Box, Date Box, Check Box, Select Box, and Tag Box
- Gallery and Tile View
- Menu and Context Menu
- Tab Panel, Multi View, Splitter, and Scroll View
- Button

Existing PublisherStudio charts remain independent first-class chart objects. Their current field mapping, animation, mouse interaction, and document compatibility are unchanged.

## Component Studio

- Added a Component Studio with sections for identity, document/page scope, data source, fields and editor types, behavior, dynamic layout panels, actions, and advanced options.
- Components can be switched between supported kinds without replacing the publication element.
- Field discovery reads parsed publication data or probes a REST/OData endpoint from the real browser context, so CORS and authentication behavior are tested where the exported page will execute.
- Fields support data type, caption, visibility, editability, required validation, width, format, editor type, Pivot Grid area/summary, and lookup datasets.
- Advanced DevExtreme options can be supplied as JSON and are merged into the safe generated options.
- Custom JavaScript actions are disabled by default and require an explicit per-component opt-in.

## Publication data, REST, and OData

Every component can use one of four source modes:

1. A reusable PublisherStudio data object, including spreadsheet selections, embedded tables, parsed JSON/XML/delimited data, page/object data, web polling, and webhook snapshots.
2. A static embedded snapshot.
3. A direct REST/JSON endpoint through a DevExtreme `CustomStore`.
4. A direct OData service through a DevExtreme `ODataStore`.

REST connections support:

- GET/POST/PUT/PATCH/DELETE load methods
- headers, request body, JSON result path, and optional credentials
- client-side or remote sorting/filtering/paging parameters
- key fields
- separate create, update, and delete URLs and methods
- optional key suffixes for write URLs
- Data Grid, Tree List, Scheduler, form, and editor workflows

OData connections support versions 2, 3, and 4, configured key field/key type, browser credentials/headers, remote operations, and native store editing.

Standalone editors can initialize from the first matching dataset/endpoint row and participate in value-change actions. Form submission uses the configured DevExtreme store when possible, including OData. Existing rows with a configured key use update when update is enabled; otherwise enabled insert is used. An explicitly configured REST URL remains available for custom submit contracts. Browser `mailto:` actions can prepare an email from form/component values without adding a backend mail service.

## Smart actions and component connections

- Actions can run on click, item click, selection/value change, form submit, grid row changes, and Scheduler appointment changes.
- Built-in actions include next/previous/go-to page, open URL, prepare email, refresh, show/hide/toggle another element, submit data, set another component value, apply/clear a filter, and opt-in custom script.
- Selection actions use the selected row rather than an empty event object.
- Document-wide target components resolve to the corresponding component instance on the current page, preserving smart connections across synchronized page layouts.
- Duplicating a page or component remaps component-action targets to the duplicated objects instead of leaving links pointed at the originals.
- Buttons and menu items can use data fields in URL, mail, confirmation, and value templates.

## Menus, pages, and synchronized components

- Menu and Context Menu can generate a navigation dataset from the current publication pages.
- Page targets use stable page IDs and work in both interactive-presentation and website exports.
- Components can be page-local or document-wide synchronized.
- Document-wide components synchronize content, data bindings, fields, actions, and styling while retaining independent position and size on each page. This allows one menu definition to fit portrait and landscape pages separately.
- Switching a synchronized component back to page scope removes the linked copies from the other pages without deleting the selected instance.

## Dynamic layout containers

- Splitter, Tab Panel, Multi View, and Scroll View can host configurable child components.
- Panels support title, size constraints, collapse state, optional safe HTML content, child component kind, field definitions, and an independent publication dataset.
- Child controls reuse the same browser runtime and do not introduce Blazor, Razor, ASP.NET Core, or extra exported files.

## Two single-file HTML exports

The existing HTML export remains an interactive presentation with PublisherStudio transitions, animation sequencing, media, charts, and controls.

A second **single-file website** export now provides:

- hash-based page routes that also work from `file://`
- browser Back/Forward history
- stable page-ID, page-name, page-number, and generated-slug navigation
- animated page changes
- responsive fit-to-viewport behavior
- interactive DevExtreme components and live data

Both export modes still produce one `.html` file. The file embeds the DevExtreme stylesheet/runtime, jQuery, the generated public DevExtreme runtime license, PublisherStudio data/component runtimes, publication content, and local media assets. The DevExtreme runtime is embedded once per export, not once per component.

## Export and runtime hardening

- Component hosts are cleaned before initialization, including cloned export DOM that has no live runtime state. This prevents cloned editor markup from being initialized twice.
- Forms validate required fields before submission.
- REST and OData rows are materialized for forms, menus, context menus, and standalone editors when the control needs an immediate local value.
- Reusable layout-panel snapshots retain number, Boolean, and date value types.
- Direct endpoint credentials and headers are never invented or proxied automatically. Exported browser calls still require an endpoint that permits the exported page through CORS and its authentication policy.
- Private API secrets must not be placed in exported HTML. Protected services should remain behind the PublisherStudio/LocalGPT host or another controlled proxy.

## Compatibility

- Application/package version updated to `1.0.39`.
- Publication document format updated from `1.36` to `1.37` for the new `devExtremeComponent` element type.
- DevExtreme remains pinned to `25.2.8`.
- Existing documents and standalone chart objects remain compatible.
- Run `Prepare-DevExpressAssets.cmd` on the licensed build machine before building/publishing so the matching public browser runtime license is generated.
