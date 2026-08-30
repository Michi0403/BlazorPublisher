# PublisherStudio 3.1.6 source validation

## Scope

This maintenance pass follows the supplied Windows release run through successful PublisherStudio compilation, complete DocFX/PDF generation, all three Windows application/setup publishes, and successful `linux-x64` Full application publishing. The fatal exception originates in the shared LocalGPT-owned release-packaging helper during TAR.GZ commit, not in PublisherStudio application compilation.

No .NET SDK or PowerShell runtime is available in this validation environment, so no local compiler, PowerShell parser, native package materialization, or installer execution is claimed here.

## Repair

- PublisherStudio now requires `LocalGPT.ReleasePackaging` 1.0.1 rather than the known-broken 1.0.0 package.
- The corrected helper closes its TAR.GZ/DEB output streams before final file moves on Windows.
- PublisherStudio continues to consume rather than duplicate the LocalGPT-owned packaging source, matching the existing 1-Wire package ownership model.
- Windows setup and Linux/macOS Full/Light package lanes remain source-wired.
- Editor, recording/export, Panel Studio, localization, and InteractiveServer runtime behavior are unchanged.

## Static source validation

The maintained source tree passes:

- PublisherStudio 3.1.6 release audit.
- Architecture policy audit.
- Async continuation audit: 80 files, 1,102 await tokens, 465 `ConfigureAwait(false)`, 583 renderer-affine `ConfigureAwait(true)`, 49 explicitly configured await-using disposals, and 5 configured async streams.
- Service resilience audit: 1,375 service methods and 3 iterator/yield methods, with zero exemptions/skips.
- Component resilience audit: 2,687 component methods, zero legacy exemptions.
- Iterator exception-policy audit: 3 iterator/yield methods, zero exemptions.
- Prerender JavaScript interop safety audit: 2,687 component methods; 13 JavaScript-aware disposal methods are attachment-gated.
- Panel Studio persistence audit.
- Cross-platform boundary audit: 60 checks.
- XML documentation audit: 6,286 direct C# declarations across 252 maintained source files, plus 48 Razor component types and 3,443 direct `@code` members.
- Structured metadata parsing: 6 XML/MSBuild files and 42 JSON files parse cleanly with duplicate JSON keys rejected.
- Publish-profile matrix still resolves all seven RIDs: win-x64, win-x86, win-arm64, linux-x64, linux-arm64, osx-x64, and osx-arm64.

The final delivery ZIP is CRC/path/duplicate checked, freshly extracted, and the critical release/architecture/resilience/cross-platform/XML checks are rerun against that exact extraction before handoff.
