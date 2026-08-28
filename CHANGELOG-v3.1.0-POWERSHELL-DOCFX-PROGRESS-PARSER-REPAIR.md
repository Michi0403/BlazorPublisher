# PublisherStudio 3.1.0 — PowerShell DocFX Progress Parser Repair

## Fixed

- Fixed the DocFX progress formatter in `build/Build-Documentation.ps1` from `"[DocFX] $name: $percent%"` to `"[DocFX] ${name}: $percent%"`.
- This removes the Windows PowerShell parser error `InvalidVariableReferenceWithDrive` caused by the colon immediately following `$name`.

## Preserved

- Full release documentation and PDF generation remain enabled.
- Cross-platform build guards remain enabled for Windows, macOS, and Linux.
- Existing Node.js 20+ discovery/reuse remains unchanged; PublisherStudio does not intentionally provision another Node when a suitable existing runtime is found.
- DevExpress/Spreadsheet preparation, application/runtime/UI/InteractiveServer/deployment behavior, and wire protocol integration remain unchanged.

Version: **3.1.0**.
