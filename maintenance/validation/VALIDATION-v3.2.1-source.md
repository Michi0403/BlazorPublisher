# PublisherStudio 3.2.1 source validation

This source release is statically validated in an environment without the .NET SDK, PowerShell runtime, or macOS browser stack; no local compile or native macOS PDF-build claim is made.

The 3.2.1 release audit verifies version consistency, preservation of the 3.2.0 WSL2/Linux release contract, local-first LocalGPT packaging ownership, the two-profile browser PDF renderer, validation-before-acceptance of browser PDF candidates, explicit compatibility accessibility metadata, bounded macOS DocFX fallback timeout, operator `DOCFX_PDF_TIMEOUT` override support, typed system-variable ownership, and explicit InteractiveServer boundaries.

Maintained architecture, async, service/component resilience, iterator, prerender/interop, Panel Studio persistence, cross-platform, C#/Razor XML-documentation, Bash syntax, and structured source checks are run before final packaging. The final ZIP is additionally extracted and compared byte-for-byte with the prepared source tree, checked for unsafe/duplicate ZIP entries and CRC errors, and the critical audits are rerun from that exact extraction.
