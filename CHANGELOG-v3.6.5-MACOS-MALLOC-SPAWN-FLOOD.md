# PublisherStudio 3.6.5 - macOS malloc spawn-flood repair

PublisherStudio 3.6.5 is a narrow release-pipeline repair on top of 3.6.4. The macOS native packaging path inspected the application bundle by starting the `file` utility once for every payload file during architecture validation and then repeated the same full scan before Developer ID signing. On affected macOS/PowerShell environments, each child launch can emit `MallocStackLogging: can't turn off malloc stack logging because it was not enabled.`, producing hundreds of lines that bury useful build and signing diagnostics.

## Changed

- Adds bounded macOS bundle inspection through `file -b` batches of 96 paths while preserving one description per input file.
- Fails closed if a batch fails or returns fewer/more descriptions than files supplied, so architecture validation cannot silently accept an incomplete inventory.
- Returns the already validated Mach-O inventory from the architecture check and reuses it for Developer ID signing instead of rescanning the full bundle.
- Preserves per-component Developer ID signing and verification, nested code-bundle signing, app-bundle verification, DMG/PKG signing, Gatekeeper checks, notarization, stapling, and notary transaction semantics.
- Does not suppress or filter native stderr. Genuine signing/notary diagnostics remain visible; the repair removes the avoidable subprocess amplification that could drown them.
- Keeps the 3.6.4 PowerShell interpolation parser repair and all prior documentation, installer, editor, logging and cross-platform release behavior unchanged.

No PublisherStudio editor/runtime feature behavior changes in this release.
