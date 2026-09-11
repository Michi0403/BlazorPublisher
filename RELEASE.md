# PublisherStudio 3.6.0

PublisherStudio 3.6.0 proactively repairs the same macOS release defects exposed by the LocalGPT 4.0.8 coordinator run.

The apphost JIT entitlement is now a checked-in minimal plist. The early macOS trust preflight validates that exact file with `plutil`, and native signing normalizes and lints a temporary XML1 copy before `codesign`. This moves entitlement-format failures ahead of the expensive documentation and package lane.

The documentation browser renderer also watches for a stable, structurally complete PDF while the browser is still alive. Once complete, a lingering renderer is closed immediately rather than consuming the full 480-second safety timeout for every successful PDF part.

The 3.5.9 XML documentation repair and earlier logging, installer, render-affinity, and overlay-deployment protections remain intact.

See `CHANGELOG-v3.6.0-MACOS-SIGNING-PDF-RENDER-LATENCY-REPAIR.md` and `VALIDATION-v3.6.0-source.md`.
