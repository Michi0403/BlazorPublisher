# PublisherStudio 3.5.1 source validation

## Validation scope

PublisherStudio 3.5.1 is source/static validated only. No `dotnet build`, `dotnet test`, `dotnet publish`, native macOS packaging, signing/notarization, PKG execution or GitHub access was used in this environment.

## 3.5.1 checks

- PublisherStudio Web and InstallerConsole report `3.5.1`, respecting the maintained one-digit minor/patch-slot policy.
- Package/documentation/release metadata reports `3.5.1`.
- Release source fingerprinting invalidates same-version native artifacts when maintained source changes.
- Unix publish verifies the managed assembly semantic version before packaging and writes `RELEASE-VERSION.txt` plus `SOURCE-SHA256.txt`.
- The macOS launcher validates the packaged version/source identity before endpoint reuse or application start.
- PublisherStudio `server.json` publishes `Version` and `ExecutablePath` alongside PID/base URL/port.
- A responding endpoint is reusable only when PID, semantic version and packaged executable path match the current installed runtime.
- PKG `preinstall` stops a running PublisherStudio app before replacing `/Applications/PublisherStudio.app` and clears the logged-in user's runtime endpoint.
- PKG `postinstall` reopens the newly installed application when the previous version had been running.
- Packaged startup uses a durable per-user runtime working directory and a per-user PublisherStudio log path.
- Application startup repairs invalid inherited current directories and logs assembly/executable/base/working-directory identity.
- The 3.5.0 PowerShell 5.1 documentation-cache repair and Debug PDF gating remain intact.
- Existing InteractiveServer render-mode ownership remains unchanged.
- Source-package hygiene rejects `bin`, `obj`, `__pycache__`, Python bytecode, traversal paths and symlinks.

## Expected macOS field evidence

A 3.5.1 package should log `Runtime identity: version=3.5.1 source=<64 hex> app=...`, then application startup identity containing `assembly=3.5.1.0` and an executable under the installed PublisherStudio bundle. An upgrade performed while an older PublisherStudio is running must stop that old process before replacement and must not reconnect to its old runtime endpoint afterward.

## Static audit results

The maintained Python source audits passed on the final 3.5.1 tree before packaging:

- release audit: 5 routed pages, 4 routed `InteractiveServer` boundaries and 6 localization catalogs;
- application architecture policy: passed;
- cross-platform boundaries: 60 checks passed;
- async continuation policy: 80 source files, 1102 await tokens, 465 `ConfigureAwait(false)`, 583 renderer-affine `ConfigureAwait(true)`, 49 configured async disposals and 5 configured async streams;
- component method resilience: 2687 component methods passed;
- prerender JavaScript interop safety: 2687 component methods checked, including 13 attachment-gated JavaScript-aware disposal methods;
- service resilience: 1377 service methods passed and 3 iterator/yield methods retained their reviewed try/finally diagnostics policy;
- XML documentation: 6316 C# declarations across 252 maintained source files plus 3444 direct Razor `@code` members across 48 component types;
- generated macOS launcher, PKG `preinstall`, and PKG `postinstall` templates all pass POSIX `/bin/sh -n` syntax validation after placeholder substitution.

PowerShell itself is unavailable in this environment, so the PowerShell packaging source was not parser-executed here. The native PKG lifecycle still requires the Mac field test to validate Installer.app behavior, process handoff, signing/notarization and relaunch semantics.
