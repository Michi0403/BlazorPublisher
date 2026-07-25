# PublisherStudio v1.0.74 release

See `CHANGELOG-v1.0.74.md`, `SOURCE-CHANGES-v1.0.74.txt`, `TEST-RESULTS-v1.0.74.txt`, `docs/architecture/structured-website-export.md`, `docs/ARCHITECTURE.md`, and `VALIDATION.md`.

v1.0.74 keeps the existing standalone HTML exports and adds a structured static website ZIP containing `index.html`, separate CSS/JavaScript, and content-addressed media assets. It projects the established standalone builder, preserving component, animation, interaction, live-data, media-sequence, Signal Connector, and DevExtreme runtime behavior without maintaining a second website renderer.

Preserve-source mode externalizes media byte-for-byte and removes Base64 representation overhead. Optional PNG, WebP, AVIF, and WebM delivery processing runs locally in the browser with explicit lossless/lossy labeling, capability checks, smaller-result gating, warnings, and optional original-video playback fallback. Text-oriented ZIP entries may use browser Deflate; already compressed assets stay stored.

Application and installer version is `1.0.74`. Publication format remains `1.52`; Picture Studio format remains `1.4`; dependency versions and sets are unchanged.
