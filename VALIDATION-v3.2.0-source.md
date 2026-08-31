# PublisherStudio 3.2.0 source validation

This source release is statically validated in an environment without the .NET SDK or PowerShell runtime; no local compile claim is made.

Maintained source audits passed for the working tree: architecture policy; 60 cross-platform boundary checks; async continuation policy across 80 source files (1,102 await tokens); service resilience across 1,375 service methods plus 3 iterator/yield methods; component resilience across 2,687 component methods; iterator exception policy; prerender JavaScript interop safety; Panel Studio persistence; C# XML documentation across 6,286 declarations in 252 files; Razor XML documentation across 48 component types and 3,443 direct `@code` members; and the dedicated 3.2.0 release audit.

The 3.2.0 audit covers version consistency, optional WSL2 routing/fallback, parent-prepared DevExpress/documentation reuse, correct Windows-to-WSL DevExpress license bridging, local-first LocalGPT packaging ownership, WSL setup helpers, target-architecture AppImage finishing, system-variable ownership, and explicit InteractiveServer boundaries. Delegated WSL children skip Node.js preflight because browser assets and documentation are parent-prepared. Bash syntax and source delimiter/here-string lexical validation pass for the modified release helpers.

The final source ZIP is additionally checked for duplicate/unsafe entries, CRC integrity, exact extraction byte equality, structured XML/JSON parsing, and the critical maintained audits rerun from the exact extracted archive. Windows/WSL2 or native Linux remains the authoritative Linux runtime build test; macOS remains authoritative for Apple-native finishing.
