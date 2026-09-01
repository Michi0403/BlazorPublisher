# PublisherStudio 3.2.2 source validation

This source handoff is validated statically in the assistant environment. The environment does not provide the .NET SDK, PowerShell, MSBuild, C# compiler, macOS `hdiutil`, `codesign`, `pkgbuild`, or Homebrew, so no native compile/package execution is claimed.

Validation targets include version coherence, macOS coordinator RID selection, `BaseUrl` launcher discovery, macOS icon/DMG/PKG wiring, HTML-only Pages preparation and release-PDF metadata, optional Homebrew RPM behavior, WSL2 and native Linux compatibility, unchanged InteractiveServer boundaries, architecture/async/resilience/prerender/Panel Studio/cross-platform audits, structured XML/JSON parsing, shell syntax, ZIP integrity, and exact fresh-extraction byte comparison.
