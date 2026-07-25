# PublisherStudio v1.0.74 release

See `PublisherStudio/CHANGELOG-v1.0.74.md`, `PublisherStudio/SOURCE-CHANGES-v1.0.74.txt`, `PublisherStudio/TEST-RESULTS-v1.0.74.txt`, `PublisherStudio/RELEASE.md`, `PublisherStudio/docs/architecture/structured-website-export.md`, and `PublisherStudio/VALIDATION.md`.

v1.0.74 preserves the standalone HTML exports and adds an ordinary file-structured static website ZIP. CSS, ordered JavaScript runtimes, and deduplicated media/font assets are externalized from the same generated publication runtime, reducing Base64-heavy HTML without changing publication behavior.

The export dialog provides exact source preservation, PNG pixel-lossless output, optional WebP/AVIF image optimization, optional WebM VP9/VP8 + Opus video delivery with original fallback, and optional ZIP Deflate for text files. Browser capability/failure/size checks preserve the source rather than silently degrading an export.

Application and installer version is `1.0.74`. Publication format remains `1.52`; Picture Studio format remains `1.4`; dependency versions and sets are unchanged.
