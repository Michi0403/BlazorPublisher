# BlazorPublisher v1.0.6 — media circuit and pointer-state stabilization

## Fixed

- Embedded audio and video are now served to the editor through local ranged asset URLs instead of being repeated as multi-megabyte data URLs in Blazor render batches.
- Direct video insertion, imported Video Studio sources, camera recording, and screen recording use the same stable media delivery path.
- Standalone website and SVG exports re-embed local media assets as data URLs, so exported files remain portable.
- Clicking a publication object after clearing selection no longer flashes the selection and immediately clears it again.
- Move and resize pointer capture is held by the stable publisher stage and is reset on pointer cancellation, browser focus loss, visibility changes, or a new pointer operation.
- Stale mouse-down operations can no longer build up into a drag/click loop.
- Native audio/video controls no longer trigger a publication-object rerender merely to stop click propagation.
- Media Studio accepts supported video/audio extensions when Windows or the browser supplies an empty or generic MIME type.
- JavaScript module disposal now ignores expected circuit-disconnect and cancellation exceptions during shutdown.
