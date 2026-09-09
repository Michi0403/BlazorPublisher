# PublisherStudio 3.5.2 source validation

## Validation scope

PublisherStudio 3.5.2 is source/static validated only. No `dotnet build`, `dotnet test`, `dotnet publish`, native macOS packaging, signing/notarization, PKG execution or GitHub access was used in this environment.

## Build failure addressed

The supplied `pwsh Build-Release.ps1` invocation failed during parsing because a double-quoted string contained `$mode:`. The release script now uses `${mode}:`, and the source release audit rejects the same ambiguous unscoped `$name:` form.

## 3.5.2 checks

- PublisherStudio Web, InstallerConsole, package metadata and documentation/release metadata report `3.5.2` under the one-digit minor/patch policy.
- The 3.5.1 source-fingerprint, published-assembly-version and payload identity checks remain intact.
- `server.json` remains a runtime rendezvous mechanism: packaged-app ownership is distinguished from alternate/debug hosts rather than inferred from “different executable = stale”.
- Stale installed-app endpoints remain replaceable; responsive alternate-host endpoints remain reusable and are never terminated solely for version/path differences.
- PKG lifecycle endpoint deletion is ownership-aware and preserves active alternate-host endpoint records.
- Process termination remains limited to `/Applications/PublisherStudio.app` ownership.
- Dead endpoint owners are still cleaned up.
- Durable per-user runtime/log paths and runtime identity logging remain intact.
- The 3.5.0 PowerShell 5.1 documentation-cache fix and Debug PDF gating remain intact.
- Existing InteractiveServer ownership remains unchanged.
- Source-package hygiene rejects `bin`, `obj`, `__pycache__`, Python bytecode, traversal paths and symlinks.

## Static validation performed

The final tree passed the maintained source-only checks available without running the .NET toolchain:

- release audit: 5 routed pages, 4 routed `InteractiveServer` boundaries and 6 localization catalogs;
- application architecture policy: passed;
- cross-platform boundaries: 60 checks passed;
- async continuation policy: 80 source files, 1102 await tokens, 465 `ConfigureAwait(false)`, 583 renderer-affine `ConfigureAwait(true)`, 49 configured async disposals and 5 configured async streams;
- component method resilience: 2687 component methods passed;
- prerender JavaScript interop safety: 2687 component methods checked, including 13 attachment-gated JavaScript-aware disposal methods;
- service resilience: 1377 service methods passed and 3 iterator/yield methods retained their reviewed try/finally diagnostics policy;
- XML documentation: 6316 C# declarations across 252 maintained source files plus 3444 direct Razor `@code` members across 48 component types;
- generated macOS launcher, PKG `preinstall`, and PKG `postinstall` templates pass POSIX `/bin/sh -n` syntax validation after placeholder substitution;
- focused shell ownership tests confirm that alternate/debug endpoints survive launcher/PKG ownership checks while stale installed-app endpoints are cleaned up.

PowerShell itself is unavailable in this environment, so the actual Mac `pwsh` parser/build remains the authoritative runtime confirmation.
