# PublisherStudio 3.9.9 — Story RichEdit ownership, toolbar race and inspector button repair

## Story document/render ownership

- The application-wide DOM localization runtime no longer mutates DOM below the DevExpress RichEdit root (`.dxreRoot` / `.story-rich-edit`). RichEdit document text is user-authored publication content and must not be rewritten by the application chrome translator.
- This removes the observed mixed-language/overlaid default story where the English OpenXML document and the German DOM translation fought over the same rendered text.
- DevExpress remains responsible for RichEdit's own localized ribbon/editor UI through the installed DevExpress localization packages. PublisherStudio continues to localize its surrounding Story Editor chrome normally.
- The existing OpenXML document remains authoritative. No document migration, text replacement or preview-to-document overwrite was added.

## RichEdit toolbar/caret race

- The shared browser input-ownership boundary now treats the complete `.dxreRoot` as vendor-owned while focus or pointer interaction is inside RichEdit. Canvas/controller/gamepad routing yields for document caret navigation, selection, RichEdit ribbon dropdowns and other RichEdit interaction instead of competing for the same input.
- The Story Editor layout bridge no longer schedules its shell-width refresh from clicks inside the RichEdit host. RichEdit toolbar operations therefore stay inside DevExpress' own selection/format/layout lifecycle instead of racing a PublisherStudio layout timer.
- The print-command interception remains in place and is evaluated before the RichEdit-host click isolation, so Story Editor print behavior is preserved.

## Inspector localization action

- `Edit all application strings` is now a real `DxButton` instead of a native HTML button.
- A one-button `property-button-row` now occupies the full inspector width. The localization action caption may wrap inside the DevExpress button instead of overflowing its old one-third-width native control.
- Existing two- and three-button property rows retain their current grid layouts.

## Maintenance protection

- The build-wired localization integrity guard now requires RichEdit to remain excluded from DOM translation.
- The build-wired shared input/Panel Studio interaction guard now requires `.dxreRoot` ownership and rejects reintroduction of Story Editor layout scheduling from clicks inside the RichEdit host.
- Existing async-only, continuation, component/service resilience, Panel Studio persistence, prerender interop, iterator and Razor maintenance rules remain enabled.

No .NET build, restore, MSBuild, publish, GitHub access or online repository access was used for this source repair.
