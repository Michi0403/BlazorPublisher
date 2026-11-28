# PublisherStudio 3.1.7 source validation

## Scope

The supplied PublisherStudio 3.1.6 Windows release run proves the RID-neutral application/documentation build, all three Windows application/setup RIDs, `linux-x64` publish, TAR.GZ, and DEB generation all succeed. The run fails only when the release script requires RPM packaging on Windows without `rpmbuild`, Docker, or Podman.

This release makes the default pipeline host-aware and removes the unnecessary shared Unix packaging-tool dependency from Windows-only PublisherStudio releases.

No .NET SDK or PowerShell runtime is available in this validation environment, so no local compiler, PowerShell parser, native RPM/AppImage/DMG materialization, or installer execution is claimed here.

## Repair

- `Runtime=all` now selects the current host OS family; `all-rids` is the explicit cross-host option.
- Windows builds retain Windows x64/x86/ARM64 application and one-click setup outputs and do not enter Linux/macOS packaging.
- Linux/macOS release lanes prepare the LocalGPT-owned packaging helper only when they actually need it.
- `LocalGPT.ReleasePackaging` resolution is local-first and can build the package from an available LocalGPT checkout using LocalGPT's own package-publisher script.
- No implicit GitHub download occurs for the packaging helper; online fallback requires an explicit package URL.
- RPM/AppImage are non-mandatory Linux-native finishers; Docker/Podman is opt-in.
- Windows command launchers and PowerShell release/development entry points initialize UTF-8 console handling.

## Static source validation

The maintained source tree passes:

- PublisherStudio 3.1.7 release audit.
- Architecture policy audit.
- Async continuation audit: 80 files, 1,102 await tokens, 465 `ConfigureAwait(false)`, 583 renderer-affine `ConfigureAwait(true)`, 49 explicitly configured await-using disposals, and 5 configured async streams.
- Service resilience audit: 1,375 service methods and 3 iterator/yield methods, with zero exemptions/skips.
- Component resilience audit: 2,687 component methods, zero legacy exemptions.
- Iterator exception-policy audit: 3 iterator/yield methods, zero exemptions.
- Prerender JavaScript interop safety audit: 2,687 component methods; 13 JavaScript-aware disposal methods are attachment-gated.
- Panel Studio persistence audit.
- Cross-platform boundary audit: 60 checks.
- XML documentation audit: 6,286 direct C# declarations across 252 maintained source files, plus 48 Razor component types and 3,443 direct `@code` members.
- Structured metadata parsing: 20 XML/MSBuild files and 42 JSON files parse cleanly with duplicate JSON keys rejected.
- PowerShell source delimiter/here-string lexical validation passed for the modified release/package scripts.

All seven maintained RID profiles remain available for explicit targeting, while host-aware defaults prevent a Windows build from being failed by Linux/macOS native packaging requirements. The final delivery ZIP is CRC/path/duplicate checked, freshly extracted, and the critical static audits are rerun against that exact extraction before handoff.
