# PublisherStudio 3.0.1

PublisherStudio 3.0.1 is the **Cross-platform Backend Boundaries** release.

The application remains the existing PublisherStudio architecture. This release removes unused Windows-only package baggage and moves host-sensitive filesystem, executable discovery, font, capture, hotkey, process-loopback and permission behavior behind neutral interfaces with Windows and Unix implementations selected once by dependency injection.

The source audit did not find a maintained GDI+/`System.Drawing` rendering backend that would justify rewriting PublisherStudio or introducing a replacement graphics library. `System.Drawing.Common` is therefore removed rather than replaced. External cross-platform libraries remain a last resort.

The release tooling is cross-platform hardened as well: the PublisherStudio authored DocFX source payload is included, Node.js can be resolved/provisioned on Windows/macOS/Linux, generated HTML is accessibility/link checked before the expensive PDF render, and Pages validation records the difference between a genuinely tagged browser PDF and the DocFX plugin's HTML-accessibility fallback.

Release and local-development entry points now fail fast on incomplete source packages and run the cross-platform boundary guard before the long build.

This handoff is source-only and was not built with .NET or PowerShell in the packaging environment. See `CHANGELOG-v3.0.1-CROSS-PLATFORM-BACKEND-BOUNDARIES.md` and `VALIDATION-v3.0.1-source.md`.
