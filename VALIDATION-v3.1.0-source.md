# PublisherStudio 3.1.0 source validation

- Patched the exact 3.0.9 source ZIP supplied/tested in this conversation.
- Fixed only the invalid PowerShell interpolation in `build/Build-Documentation.ps1`: `${name}:` now delimits the variable before the colon.
- Searched PowerShell sources for the same unbraced variable-before-colon pattern; the repaired DocFX line is no longer present.
- Current PublisherStudio identity was advanced from 3.0.9 to 3.1.0 in accordance with the single-digit minor/patch version rule.
- Full documentation/PDF generation, cross-platform guards, Node reuse, DevExpress, and Spreadsheet behavior remain enabled and unchanged.
- No .NET build and no GitHub access were used.
