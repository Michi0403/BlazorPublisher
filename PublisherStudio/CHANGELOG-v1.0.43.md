# PublisherStudio v1.0.43 — navigable menus and live publication fields

## Component Studio menus

- Menu and Context Menu items can independently target a publication page, an external HTTP/HTTPS/mail link, or no navigation action.
- Publication pages are no longer inserted implicitly. External-only and action-only menus are valid.
- Data-driven menus use stable page IDs from the live **Publication pages** object and work in the editor, presentation export, and offline website export.
- Older menus without a usable ItemClick action receive a safe navigation fallback at runtime.
- Horizontal and vertical Menu construction is selectable.

## Fields and data mappings

- Data fields are selected from properties provisioned by the active publication data object or discovered endpoint instead of relying on guessed free-text names.
- The Data & API key field is selected from the same source-property list.
- Component mappings remain selected from configured fields, including menu text, parent, target page, and external URL properties.
- The built-in self-updating data objects expose publication pages, document metadata, and publication objects at render/export time.

## Component-specific settings

- Behavior settings are filtered to controls that actually consume them.
- Scheduler-only options no longer appear for Menu or unrelated controls.
- Border, word-wrap, edit, selection, and paging controls are shown only where the browser runtime implements them.
- Manual menus hide irrelevant API, key-field, and CRUD configuration and explain where item destinations are edited.

## Runtime loading

- The DevExpress Blazor resource manager no longer emits a second DevExtreme bundle when PublisherStudio's pinned `dx.all.js` is loaded manually.
- This prevents DevExtreme `E0024` and leaves one deterministic browser runtime for the editor and component previews.

## Versions

- Application/package/installer: `1.0.43`.
- Publication document format: `1.41`.
- Picture Studio format remains `1.2`.
