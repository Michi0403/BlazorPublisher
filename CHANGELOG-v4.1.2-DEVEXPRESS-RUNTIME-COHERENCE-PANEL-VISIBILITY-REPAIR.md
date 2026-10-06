# PublisherStudio 4.1.2 — DevExpress runtime coherence and Panel visibility repair

## Summary

PublisherStudio 4.1.2 uses the 4.1.1 lifecycle diagnostics to repair two concrete defects exposed by the blank scratch-Panel workflow instead of adding another render delay.

The captured failure occurs while DevExpress is processing popup/dropdown custom-element changes. At the same time, the supplied 4.1.1 source carries two different DevExpress runtime versions: the .NET/npm configuration requests 25.2.10 while the prepared DevExtreme browser payload, runtime-key metadata and license payload are 25.2.10. PublisherStudio 3.7.6, supplied by the user as the known-good Panel Studio reference, used one coherent 25.2.10 lane.

The runtime screenshot also shows newly inserted scratch elements present in the inspector/list while the selected element's `Visible` state has already become false. New publication elements default to visible; the later inspector directly assigned the DevExpress checkbox callback into `selected.Visible`, making editor initialization capable of changing authored visibility before an explicit user visibility action.

## Fixed

- Restored one coherent DevExpress/DevExtreme **25.2.10** runtime lane across PublisherStudio.Web:
  - `DevExpress.Blazor` / `DevExpress.Blazor.RichEdit` package version property;
  - npm `devextreme-dist` and `devexpress-aspnetcore-spreadsheet` versions and lock metadata;
  - prepared DevExtreme and Spreadsheet browser payloads;
  - prepared DevExtreme runtime-key/license metadata;
  - active DevExtreme/Spreadsheet/localization cache-busters; and
  - current third-party notices.
- The prepared browser payload is restored byte-for-byte from the supplied, working PublisherStudio 3.7.6 source. This removes the 25.2.10 .NET/custom-element implementation running against 25.2.10 prepared browser/runtime metadata instead of trying to compensate for that mismatch in Panel Studio code.
- Kept strict prepared-asset hash verification. `dx.all.js`, `dx.light.css` and the license payload match the restored 25.2.10 metadata; no integrity gate is disabled.
- Replaced the selected-element visibility inspector's direct `DxCheckBox.CheckedChanged` mutation with an explicit DevExpress `Show`/`Hide` action. Browser component initialization can no longer write `PublicationElement.Visible` as a side effect of constructing the inspector.
- Added an information diagnostic whenever a Panel Studio element is inserted, including element ID/kind, visibility, geometry and authoring revision.
- Added an information diagnostic whenever element visibility is explicitly toggled. A future runtime record can therefore distinguish creation state from an intentional user visibility action.

## Preserved

- The 4.1.1 browser/C# Panel Studio lifecycle diagnostics remain enabled for runtime verification.
- The 4.1.0 direct popup-body topology remains; no maintenance FormLayout shell is reintroduced into the dynamic popup body.
- The 4.0.8 authored-render revision and 4.0.9 first-element refresh remain unchanged.
- `DxPopup` remains the modal owner; no native/manual modal or DevExpress vendor monkey-patch is introduced.
- Website/single-file export keeps strict prepared DevExtreme asset verification.
- Publication schema, Panel Library documents, saved modules, behaviors, data connections and export model remain unchanged.
- No PowerShell maintenance script code is weakened or bypassed. The JavaScript diagnostics hash manifest is refreshed only for the intentional localization-runtime cache-buster change.

## Version

`4.1.1 -> 4.1.2`, following the maintained single-digit version-slot rule.
