# PublisherStudio v1.0.51 release

See `CHANGELOG-v1.0.51.md`, `docs/ANIMATION_EXPORT.md`, `docs/COMPONENT_RUNTIME.md`, `docs/STREAMING.md`, and `VALIDATION.md`.

This revision repairs shared pointer ownership and tooltip presentation in the main Publisher frame and self-contained HTML exports. DevExtreme visual hover state is cleared when the pointer moves to another overlapping publication object, decorative exported objects no longer steal pointer events from interactive surfaces beneath them, and application help tooltips use the browser top layer with stable object ownership across nested SVG/chart content.

Signal Arrow geometry, source/target resolution, publication coordinates, document formats, streaming integration, and the single-application architecture remain unchanged.

The source package intentionally excludes `bin`, `obj`, `.vs`, `.git`, restored `node_modules`, local credentials, recordings, and generated runtime caches.

Application and installer version `1.0.51`; publication format `1.45`; picture format `1.2`. There is no separate Media Host executable or release payload.
