# PublisherStudio v1.0.19 recording and presentation export stabilization

Versions v1.0.12 through v1.0.18 were used for iterative commits without separate changelog files. Changelog tracking resumes with this release; the missing files are intentionally not reconstructed after the fact.

- Reworked browser camera, microphone, and screen recordings so the completed media remains a browser-side `Blob` with a retained object URL instead of being converted to an embedded data URL immediately.
- Added a persistent **Download recording** action in Video Studio and Audio Studio. A completed recording remains downloadable after recording stops, after rerenders, and after an embedding failure.
- Delayed publication embedding until **Insert media** or **Apply media** is selected. Embedding is transferred in binary-backed Base64 chunks and no longer uses PublisherStudio's former 160 MB data-URL limit.
- Added recording size, embedding progress, and explicit data-protection status to Media Studio. If embedding fails, PublisherStudio attempts to download the completed recording automatically and keeps the download action available while the studio remains open.
- Removed the fixed 160 MB import stream limit so practical media size is governed by browser, runtime, memory, and storage capabilities rather than an application policy.
- Extended the server-side JS interop lifetime for long chunked offline media transfers.
- Fixed video-export page transitions by animating a fixed page shell instead of overwriting the fitted page transform.
- Unified animated HTML and video presentation sizing around one calculated publication frame.
- Portrait-only publications retain a portrait frame. When landscape and portrait pages are mixed, the presentation frame is landscape and portrait pages are centered with side bars rather than stretched.
- Every page contributes to the frame calculation. Portrait-only projects use the maximum native width and height; mixed-orientation projects normalize every page to landscape for the maximum long-side and short-side dimensions, then center each original page proportionally inside that frame.
- Updated animated HTML export to use a fixed centered stage, preserving page scale, page-transition animation, and the same page-fitting behavior used by video export.
- Updated the publication format marker to `1.19`; older publication files remain loadable.
- Installer, barcode, picture editing, snap, grouping, and drag-to-insert behavior were not changed by this release.
