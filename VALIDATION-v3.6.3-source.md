# PublisherStudio 3.6.3 source validation

This release changes only documentation-build resilience. It does not claim a `dotnet`, Visual Studio, native signing, notarization, or PowerShell runtime build in the validation environment.

Static validation requires the maintained release, architecture, async-continuation, component/service resilience, prerender safety, iterator policy, cross-platform, Panel Studio persistence, XML-documentation, logging-baseline, overlay-installer, and 3.6.1 operator controls to remain green. The 3.6.3 release audit additionally guards bounded low-memory browser timeouts, isolated profile state, profile-owned child cleanup, fresh-profile chunk retries, durable chunk reuse, and refusal of monolithic DocFX/Playwright fallback for chunked or low-memory builds unless explicitly overridden.

The source ZIP must be extracted after packaging and the high-value guards rerun against the delivered bytes. The user's Visual Studio/macOS build remains the authoritative end-to-end compiler and release test.
