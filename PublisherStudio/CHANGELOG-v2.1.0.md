# PublisherStudio 2.1.0 - Kawaii documentation and help

## Documentation

- Added XML summaries for 3,420 maintained public and protected C# declarations.
- Added an XML coverage guard without introducing application statics.
- Added DocFX generation for conceptual and API documentation.
- Added a versioned PDF book generated from the same source set.
- Added a Kawaii website/PDF theme with working system, dark, and light modes.
- Added layout guardrails that prevent unnecessary wide-screen horizontal scrolling.
- Replaced the generic DocFX brand mark with a PublisherStudio paw.

## Application help

- Added an InteractiveServer `/help` page.
- Added ribbon actions for the HTML guide, PDF, API reference, and build status.
- Added a service-owned documentation catalog and controller.
- Kept documentation status models in `PublisherStudio.BusinessObjects`.

## Publishing

- Added a GitHub Pages workflow that extracts and validates the complete documentation tree shipped in a release ZIP.
- Kept support for Windows backslash ZIP members through exact `ZipInfo` extraction.
- Added documentation files to the normal publish graph and release validation.

## Quality

- Removed the dynamic Blazor render-tree sequence warning from SVG WordArt.
- Added an explicit Windows platform guard before creating process-loopback capture.
- Removed release-only unused cancellation-variable warnings.
- Kept LocalGPT optional and retained wire protocol version 2.1.1.
