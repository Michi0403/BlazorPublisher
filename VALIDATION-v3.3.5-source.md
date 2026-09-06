# PublisherStudio 3.3.5 source validation

Static validation covers version consistency, the Apple-notarization interpolation repair, the newly added early PowerShell parser preflight, a lexical guard against future unbraced variable-plus-colon interpolation, compressed embedded documentation, Full/self-contained Unix/macOS packaging, resumable notarization/reuse, Homebrew Ghostscript/RPM support, architecture/cross-platform boundaries, async/service resilience, and the four reviewed InteractiveServer boundaries.

This environment does not contain PowerShell or .NET, so the macOS `pwsh Build-Release.ps1` execution itself is not claimed here. On the Mac, the new PublisherStudio parser preflight now runs before source/package preparation and is the authoritative syntax check.
