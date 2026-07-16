# v1.0.2 — media import, raster export, media cues, and Picture Studio tools

- Direct video insertion now inspects the browser's selected `File` object through an object URL before embedding it, avoiding a large base64 round-trip merely to read metadata.
- Added extension-based MIME fallback for MP4, M4V, WebM, Ogg video, MOV, MP3, M4A/AAC, WAV, Ogg audio, and FLAC.
- PNG and JPEG export now freezes video at a visible/poster/trim-start frame, removes live media controls from the raster surface, and exports every publication page.
- Multi-page PNG/JPEG exports are delivered as a ZIP containing one image per page; a one-page document downloads the image directly.
- Added animation timeline actions for Play Media, Pause Media, and Stop Media, including editor preview and animated website runtime behavior.
- Added Picture Studio procedural renderers: Grain Noise, Motion Blur, Wind, and Ocean Waves.
- Added direct-draw Square, Rectangle, Ellipse, and Arrow tools that create editable shape layers.
- Added an editable Arrow shape to Picture Studio insert, render, and context-menu workflows.
