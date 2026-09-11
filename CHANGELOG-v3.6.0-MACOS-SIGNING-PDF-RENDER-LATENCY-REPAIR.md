# PublisherStudio 3.6.0 - macOS signing and PDF render latency repair

## Fixed

- Applied the same macOS entitlement hardening identified from the LocalGPT 4.0.8 release failure: the .NET apphost JIT entitlement is now a checked-in minimal plist instead of PowerShell-generated XML.
- macOS release trust preflight requires `plutil` and validates the exact entitlement asset before documentation or native package work begins. Native signing normalizes and lints a temporary XML1 copy before passing it to `codesign`.
- Browser documentation PDF rendering now detects a stable, structurally complete output while Chromium/Edge is still alive and closes a lingering renderer immediately. The existing 480-second timeout remains only for genuinely incomplete renders instead of being paid for every successful chunk.

## Preserved

- 3.5.9 XML documentation contract repair.
- 3.5.8 installer compile/logging-integrity repair, the maintained logging baseline, the single shared file writer, render-affinity protections, Story Editor attachment guard, and overlay installer contract.
- Developer ID nested-code signing order, notarization, architecture validation, durable PDF chunk reuse, and shared release packaging behavior.

Version 3.6.0 follows the repository rule that 3.5.9 rolls over to 3.6.0 rather than creating 3.5.10.
