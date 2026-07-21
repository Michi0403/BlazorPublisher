# PublisherStudio 1.0.44

## DevExtreme component reliability

- Fixed editable Menu and Context Menu components being treated as missing-data components when they use their own manual item list.
- Manual menus now render without a publication dataset; data-driven menus still show the missing-data guidance when their selected dataset is unavailable.
- Date normalization now uses both configured fields and the discovered dataset schema. Invalid date values become `null`, and Scheduler rows without a valid start date are ignored instead of aborting component rendering.
- REST and live-data rows pass through the same date normalization before DevExtreme receives them.
- The browser-native Menu fallback can now repaint when live menu items change.

## Canvas interaction

- DevExtreme components can be dragged from their rendered content after a short movement threshold while ordinary clicks remain available to the component.
- Selection is no longer committed during pointer-down, preventing Blazor from recreating the DevExtreme host before a drag starts.
- A completed drag selects the moved component and persists its position through the normal editor state/undo path.

## Connector attachment and routing

- Publication objects can store persistent custom connector points at arbitrary percentages inside their bounds.
- Right-click an object and choose **Add connector point here** or **Add connector point at center**.
- Starting or ending a connector directly on an object creates a highlighted preview point and persists it when the connector is committed.
- Custom points follow object movement, resizing, and rotation.
- Selected curved connectors expose two Bézier handles and a route handle.
- Dragging the selected connector line itself moves the route, making arrow placement adjustable without grabbing the small route handle.
- Connector route coordinates and custom attachment points are persisted in publication files and used by print and HTML export geometry.

## Compatibility and fixes

- Fixed the C# 14 `field` keyword conflict and incorrect collection property access in Component Studio source-column discovery.
- Exported `initializeSignalConnectors` from the JavaScript module used by the Blazor canvas.
- Publication format updated to `1.42`.
