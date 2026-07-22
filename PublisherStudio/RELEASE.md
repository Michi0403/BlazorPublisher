# PublisherStudio v1.0.52 release

See `CHANGELOG-v1.0.52.md`, `docs/ANIMATION_EXPORT.md`, `docs/COMPONENT_RUNTIME.md`, `docs/STREAMING.md`, and `VALIDATION.md`.

This revision corrects DevExtreme data-visual tooltip coordinates in self-contained presentation and site HTML exports. Tooltips now use the transformed publication object as their container, so centered/scaled pages, stage fitting, and page animation no longer leave the tooltip in unscaled document-body coordinates.

The fix is export-local. Main-application DevExtreme behavior, chart hit testing, pointer ownership, overlap cleanup, Signal Arrow geometry, publication coordinates, document formats, streaming integration, and the single-application architecture remain unchanged.

The source package intentionally excludes `bin`, `obj`, `.vs`, `.git`, restored `node_modules`, local credentials, recordings, and generated runtime caches.

Application and installer version `1.0.52`; publication format `1.45`; picture format `1.2`. There is no separate Media Host executable or release payload.
