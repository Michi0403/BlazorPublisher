# PublisherStudio 2.9.6 source validation

Validation was intentionally source-only. No `dotnet` build, publish, test or runtime launch was performed.

## Passed repository checks

- `audit_application_architecture.py --mode all`
- `audit_async_continuations.py`
- `audit_component_resilience.py`
- `audit_prerender_interop_safety.py`
- `audit_service_resilience.py`
- `audit_iterator_exception_policy.py`
- `audit_panelstudio_persistence.py`
- `Assert-XmlDocumentationCoverage.py`
- `node --check` for `componentRuntime.js` and `publisherInterop.js`
- JavaScript diagnostics SHA-256 manifest alignment for both changed runtime files
- `audit_release_2_9_6.py`: 98 release-contract checks

## Release-contract evidence

- Web, installer and npm metadata resolve to 2.9.6; minor/patch slots remain single digit.
- DevExpress stays at 25.2.9.
- Both complete-publication starter templates parse as JSON, contain multiple pages, page transitions and authored object animations.
- All three Div starter templates parse as PanelElement wrappers with at least one view.
- Local template folders are `%LOCALAPPDATA%\PublisherStudio\PublisherTemplates` and `%LOCALAPPDATA%\PublisherStudio\DivTemplates`.
- Seed copy is non-destructive and local template identifiers are constrained to top-level JSON filenames.
- Repeated Div insertion has explicit identity regeneration and internal reference remapping before ordinary Panel normalization.
- Native media/form controls are excluded from wrapper/signal click ownership.
- Signal runtime includes play/pause/ended and component-event triggers plus media recursion suppression.
- Panel/Div child objects retain canonical element IDs and are recursively available as signal targets.
- Six localization catalogs are in exact 3,327-key parity.

## Toolchain limitation

The archive is a source release prepared for the user's normal .NET/DevExpress build environment. Compilation status is deliberately not claimed here because `dotnet` is unavailable in this execution environment.
