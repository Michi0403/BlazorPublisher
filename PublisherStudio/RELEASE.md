# PublisherStudio v1.0.39 release

See `CHANGELOG-v1.0.39.md` and `docs/COMPONENT_RUNTIME.md`.

Source release notes:

- Added browser-native Data Grid, Tree List, Scheduler, Pivot Grid, Form/editor, collection, navigation, button, and layout-container publication objects.
- Added direct REST/JSON `CustomStore` and OData `ODataStore` connections with field discovery, client/remote processing, CRUD, headers, credentials, and reusable publication datasets.
- Added smart component actions for page navigation, REST/form submission, mail preparation, refresh, visibility, component values, and filters.
- Added page-local and document-wide synchronized components with page-specific placement.
- Added Menu/Context Menu page navigation and a second single-file website export with hash routing and browser history.
- Preserved existing standalone charts and the current interactive-presentation HTML export.
- Both exports remain one HTML file and embed the existing DevExtreme runtime only once.
- Publication format is now `1.37`; application/package version is `1.0.39`.
- Run `Prepare-DevExpressAssets.cmd` on the licensed build machine before building or publishing.
- Run `npm run test:timeline` and `npm run test:components` from `src/PublisherStudio.Web` for the Node regression suites.
