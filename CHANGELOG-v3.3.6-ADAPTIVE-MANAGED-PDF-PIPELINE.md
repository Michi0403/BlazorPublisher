# PublisherStudio 3.3.6 — adaptive managed PDF pipeline

- Preserved every already-rendered browser PDF chunk until the managed merge completes; print-book generation now replaces only its own HTML file instead of clearing the shared chunk directory.
- Upgraded the shared `LocalGPT.ReleasePackaging` contract to 1.0.2 with MIT-licensed PDFsharp 6.2.4 PDF merging.
- Large documentation is browser-printed in bounded, memory-adaptive chunks and merged by the packaging helper instead of relying on one huge Chromium/Edge print job or a multi-gigabyte DocFX PDF.
- PublisherStudio includes a synchronized source fallback for `LocalGPT.ReleasePackaging` so a clean PublisherStudio checkout can build the helper without requiring a pre-built LocalGPT package cache.
- A complete cover/table-of-contents print is rendered separately before body chunks so chunking does not truncate the document index.
- qpdf/Ghostscript remain optional optimization accelerators; the normal browser-chunk + managed-merge path has no commercial PDF dependency.
- The final PDF remains embedded in `wwwroot/help-docs` and the Full self-contained packaging/notarization-resume behavior is preserved.
