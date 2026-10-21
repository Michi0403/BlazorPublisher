# PublisherStudio 4.3.1 — Media Studio source ownership cleanup

This release is a focused, behavior-preserving extraction from 4.3.0, not a replacement of Media Studio or a change to media processing.

## Code movement

- `MediaStudio.razor` keeps its existing DevExpress ribbon, media preview, timeline, properties, recording and effect controls with their current markup, session IDs, and event flow.
- All original `@code` C# content was transferred **without modification** to eight named `MediaStudio.*.razor.cs` partial declarations, individually below 1,500 source lines. Same type, instance, DI properties, service dependencies, and callbacks.
- The existing popup titlebar and footer have been extracted as `MediaStudioTitlebar` and `MediaStudioFooter`. Both are passive presentation surfaces; Media Studio remains responsible for translating labels, enabling actions, cancellation, exports and save operations.
- The original `PublisherStudio.Services` and `PublisherStudio.BusinessObjects` architecture, media services, persisted contracts, localization catalogs, preview state, and JavaScript integrations are unchanged.
- The existing shared `mediaStudioInterop.js` remains where it is: MediaStudio, InspectorPanel and the Editor page share it. Moving it into a private collocated component module would change that shared consumer contract. No new global scripts are registered.

## Architecture enforcement retained

- Updated the existing component-diagnostics manifest to track the original catch/log/notification members at their **new** source locations; the aggregate protected catch threshold increased from 61 to 431 based on the unchanged moved source.
- Updated the DevExpress retention manifest to account for two DevExpress buttons and one native close button moving to child components. Total protected DevExpress minimum is at least the previous 202, and native controls are not expanded.
- Added pre-existing string operation locations to the text-ownership baseline for the relocated members. Guard logic and build targets have not been disabled or weakened.
- Annotated the small header/footer as structural subcomponents because adding a new layout box inside the existing DxPopup would change sizing.

## Scope and validation

No new media features, encoders, model workflows, graphics effects, migrations, JavaScript assets, service registrations or behavior changes were introduced. Further migration of component-owned workflow logic into Services should be reviewed separately, one operation and its tests at a time; a mechanical code-behind split does not itself constitute that migration.

The source C# concatenation and repository-source integrity were checked statically. A Windows .NET/DevExpress build and actual media use were **not** executed in this environment. See `VALIDATION-v4.3.1-source.md`.
