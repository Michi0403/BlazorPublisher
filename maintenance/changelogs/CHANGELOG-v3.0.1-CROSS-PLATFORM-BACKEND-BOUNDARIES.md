# PublisherStudio 3.0.1 - Cross-platform backend boundaries

PublisherStudio 3.0.1 hardens the backend and release/documentation pipeline for Windows, macOS and Linux without replacing the existing application architecture or introducing a new graphics stack.

## Platform runtime boundary

- Added `IPublisherPlatformRuntimeService` with dedicated Windows and Unix implementations.
- Centralized host-specific path comparison, path containment, FFmpeg executable/install discovery, command extensions, font directories, native video-device discovery, capture backend capabilities, Unix secret-file permissions and hardware-encoder preference.
- `PublisherStudioServiceCollectionExtensions` now selects Windows or Unix implementations once at composition time and common services consume the neutral interfaces.
- Replaced common Windows-named hotkey and process-loopback interfaces with `IGlobalHotkeyNativeService`, `IProcessLoopbackNativeService` and `IProcessLoopbackCaptureFactory`, with Windows and Unix implementations behind DI.
- Added Windows/Unix native device discovery implementations and moved LAN/documentation path-containment checks to the platform runtime service.

## Windows-only dependency cleanup

- Removed the unused `System.Drawing.Common` package. Maintained PublisherStudio source has no `System.Drawing`, `Bitmap`, `Graphics` or GDI+ backend usage that requires the package.
- Removed the unused explicit `System.Security.Cryptography.ProtectedData` package. Windows ASP.NET Core Data Protection can still opt into DPAPI at the composition root without making common services depend on that package.
- The project-level DevExpress asset preparation command now selects `powershell` on Windows and `pwsh` elsewhere and normalizes the script path with `System.IO.Path`.

## Cross-platform documentation and release tooling

- Restored the authored `docs/` source payload to the source package, including DocFX configuration, TOCs, PDF cover and PublisherStudio theme sources.
- Added the shared portable Node.js resolver/provisioner used by the documentation pipeline for Windows, macOS and Linux.
- Added generated-HTML accessibility/link validation before the expensive PDF render.
- Browser-produced PDFs request tagged-PDF and document-outline output and skip post-processing that could destroy structure metadata.
- DocFX PDF-plugin output records `html-accessibility-fallback` when the complete HTML handbook passed the strict accessibility preflight but the plugin cannot emit `/StructTreeRoot` tags.
- GitHub Pages validation preserves that distinction instead of falsely labeling an untagged PDF as tagged.

## Release guards

- Added source-package completeness and cross-platform boundary preflights to release and local-development builds.
- Added a static cross-platform audit that rejects new common-service OS detection, GDI/System.Drawing dependencies, obsolete Windows-only common interfaces and unguarded path-containment regressions.
- Updated the async continuation policy for the existing renderer-affine selected-timeline export helper.

No GitHub remote state is modified by these source changes.
