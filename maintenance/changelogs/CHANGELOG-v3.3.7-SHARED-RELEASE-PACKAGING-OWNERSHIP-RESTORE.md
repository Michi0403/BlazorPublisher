# PublisherStudio 3.3.7 — shared release-packaging ownership restore

## Fixed

- Removed the duplicated `src/LocalGPT.ReleasePackaging` source tree that was accidentally introduced in 3.3.6. PublisherStudio is again a package consumer, matching the established `LocalGPT.WireProtocolVersion` ownership model.
- `build/Ensure-ReleasePackagingPackage.ps1` now resolves the authoritative `LocalGPT.ReleasePackaging` 1.0.2 package from the Publisher cache, an explicit/environment/sibling LocalGPT checkout, the per-user LocalGPT NuGet cache, or the matching LocalGPT release asset. It never compiles LocalGPT helper source in the PublisherStudio repository.
- Kept NuGet.org enabled only while installing the already-built tool so its MIT-licensed PDFsharp dependency can resolve.
- Removed the XML-documentation/preflight failure caused by PublisherStudio scanning the accidentally duplicated LocalGPT helper source.

## Preserved

- Adaptive bounded browser PDF rendering and managed PDF merge/optimization through `LocalGPT.ReleasePackaging` 1.0.2.
- Compressed PDF embedding in `wwwroot/help-docs`.
- Full/self-contained release packages, notarization resume/reuse, Homebrew RPM support, configurable documentation/release caches, and the existing PublisherStudio application architecture.

## Validation

Static release, architecture, cross-platform, async-continuation, service-resilience, XML-documentation, and four reviewed `InteractiveServer` boundary checks are retained. PublisherStudio source audits now explicitly fail if a duplicate `src/LocalGPT.ReleasePackaging` project reappears.
