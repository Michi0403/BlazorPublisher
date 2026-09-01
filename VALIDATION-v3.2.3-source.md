# PublisherStudio 3.2.3 source validation

Validation for this handoff is intentionally source-only. No .NET restore/build/publish, DocFX render, GitHub access, macOS `hdiutil`, `pkgbuild`, `pkgutil`, Finder automation, or application execution was performed in the handoff environment.

Static validation performed:

- `build/audit_release_3_2_3.py` checks the 3.2.3 version, one-digit minor/patch policy, durable DocFX HTML/PDF cache markers, the 30-minute default timeout, shared cross-product PDF lock, deterministic `bin`/`obj` cleanup, headless DMG construction/verification, explicit PKG root layout/payload validation, preserved InteractiveServer boundaries, the LocalGPT-owned packaging helper version, and the HTML-only Pages contract.
- Project XML, package JSON, and DocFX JSON are parsed by the audit.
- PowerShell source was checked structurally for balanced delimiters/strings with a source-only scanner; native PowerShell parsing is not available in this environment.
- The final source ZIP is tested with `unzip -t` after creation.

Runtime/native package validation must occur on the normal target build hosts. In particular, the first macOS release run should confirm `hdiutil verify` succeeds for every DMG and `pkgutil --payload-files` sees `/Applications/PublisherStudio.app/Contents/Info.plist` plus the app executable payload in every accepted PKG.
