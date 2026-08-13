# PublisherStudio 2.6.1 — Build localization and XML documentation repair

## Build blocker

- Removed the duplicate `Text.Type` localization entry that caused `Assert-LocalizationIntegrity.ps1` to abort the PublisherStudio.Web build.
- Audited the remainder of the 2.6.0 localization append and removed five additional duplicate keys that would have become the next failures after `Text.Type` was corrected.
- Kept the improved German translations from the later entries when deduplicating (`Standardfarbe`, `Live-Vorschau`, the translated parse/fetch help text, and the reviewed Panel save wording).
- English and German catalogs now contain the same **3,069 unique case-insensitive keys** with no duplicate-key findings.

## Compiler-visible XML documentation repair

- Removed ten generated `///` documentation blocks that had been inserted inside `SystemFontCatalog.ReadOpenTypeFace()` executable code.
- This directly addresses the reported `CS1587` warnings and the invalid `<see cref="in"/>` references that produced `CS1584` / `CS1658` warnings.
- Strengthened `build/xml_documentation.py` validation so an XML documentation block that is not attached to a recognized C# declaration is now a source-validation failure. This closes the gap where declaration coverage could pass while the C# compiler still reported orphan XML comments.
- XML documentation remains deterministic at **5,389 direct declarations across 180 maintained C# files**; repeated enrichment makes zero changes.

## Scope

- No PublisherStudio feature behavior from 2.6.0 was intentionally changed.
- No LocalGPT source was changed or repackaged for this release.
- The harmless existing `CS0067` warning for `OrganicCapabilityCatalog.Changed` is unrelated to this repair and was not hidden or changed.

## Version

- PublisherStudio Web: **2.6.1**
- PublisherStudio InstallerConsole: **2.6.1**
- Publication format: **1.56**, unchanged
- 1-Wire protocol: unchanged
