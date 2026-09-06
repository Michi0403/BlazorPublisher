# PublisherStudio 3.4.5 source validation

This source package was validated statically in the assistant environment. A real .NET build, Microsoft Edge print, macOS Developer ID signing, and Apple notarization cannot be executed here.

## Regression checks

- Source version metadata is consistently 3.4.5.
- `build/Build-Documentation.ps1` defines `$localApplicationData` before browser discovery uses it.
- Large release documentation uses the adaptive browser-chunk path and refuses the legacy giant DocFX PDF fallback when that authoritative path fails.
- Durable browser chunks and the LocalGPT.ReleasePackaging PDF merge helper remain wired.
- PowerShell compatibility guards remain present for Windows PowerShell 5.1 and modern pwsh.
- Artifact-local notarization keeps `submit` one-shot and retries only idempotent `history`, `info`, and `log` operations.
- Notary state records both the submitted SHA-256 and final stapled SHA-256 so expected stapling mutation does not discard completed state.
- PublisherStudio routed pages keep their existing InteractiveServer render modes.
- No repository-local `bin` or `obj` build-state directories are included in the source package.

## Failure addressed

PublisherStudio 3.4.4 could enter the DocFX PDF plug-in fallback on macOS because `$localApplicationData` was referenced under StrictMode before being initialized. The observed run then generated a 1,364,811,627-byte PDF and failed the 268,435,456-byte release ceiling when Ghostscript/Homebrew was unavailable. Version 3.4.5 restores browser discovery and fails fast instead of allowing that large fallback path.
