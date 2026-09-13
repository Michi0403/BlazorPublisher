# PublisherStudio 3.6.5 source validation

Source-only validation for the macOS malloc spawn-flood repair.

- Confirm macOS bundle inspection uses bounded `file -b` batches and validates one returned description per supplied path.
- Confirm architecture validation returns the Mach-O inventory and the signing stage reuses it rather than performing a second per-file bundle scan.
- Confirm the legacy `file $item.FullName` per-payload invocation is absent from `NativeReleasePackaging.ps1`.
- Preserve Developer ID component signing/verification, nested code-bundle signing, app verification, DMG/PKG signing, Gatekeeper, stapling and notarization contracts.
- Preserve the 3.6.4 PowerShell interpolation parser guard and re-run the maintained release, architecture, async-affinity, component/service resilience, prerender, iterator, Panel Studio persistence, cross-platform and XML-documentation audits.
- Verify the delivered source ZIP from a fresh extraction and keep build outputs/caches out of the archive.

`pwsh`, `dotnet`, signing and notarization are not executed by this source validation environment; the macOS release build remains the authoritative runtime/compiler/signing test.

## Source-gate results

The maintained Python source gates completed successfully for this source tree:

- 3.6.5 release audit.
- Application architecture policy.
- Async continuation policy: 80 source files, 1,102 await tokens.
- Component resilience: 2,687 component methods with method-local diagnostic boundaries.
- Cross-platform boundaries: 60 checks.
- Iterator exception policy.
- Panel Studio persistence and reviewed InteractiveServer boundaries.
- PowerShell variable interpolation policy.
- Prerender JavaScript interop safety.
- Service resilience: 1,382 service methods plus 3 iterator/yield service methods.
- XML documentation quality: 6,383 direct declarations across 254 maintained C# files.

The packaging-specific audit also asserts that the old per-file `& $fileCommand $item.FullName` scan is absent and that the architecture inventory is passed directly to `Sign-MacBundle`.
