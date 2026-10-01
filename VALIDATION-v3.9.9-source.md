# PublisherStudio 3.9.9 source validation

## Scope

Source-only repair validation for the supplied Story Editor mixed/doubled text, RichEdit toolbar/caret race, controller/input arbitration and inspector button rendering evidence. No .NET compilation, MSBuild, restore, publish, GitHub access or online repository access was performed.

## Diagnosis represented by the source

- The Story Editor keeps one authoritative OpenXML document in `DxRichEdit` through `_content`; this release does not duplicate or replace that document model.
- `localizationRuntime.js` previously observed the whole document and did not exclude `.dxreRoot`. The German localization catalog contains translations for the default English story text, so runtime mutation could rewrite RichEdit's rendered text while DevExpress retained the original OpenXML content.
- `initializeStoryEditorLayout` previously scheduled bounded layout work for every button/tab click in the Story Editor shell, including RichEdit's own ribbon. That PublisherStudio timer could overlap the RichEdit selection/format lifecycle visible in the supplied font-size/caret evidence.
- The page-language action remained a native `<button>` inside a property row whose default grid had three columns, explaining both the non-DevExpress appearance and narrow wrapped/overflowing caption.

## Repair verification

Static checks confirm:

- `.dxreRoot` and `.story-rich-edit` are excluded from PublisherStudio DOM localization;
- shared native/vendor input ownership recognizes `.dxreRoot` as one RichEdit interaction surface;
- Story Editor print interception remains before the host-click isolation;
- clicks inside the RichEdit host do not schedule the outer Story Editor layout bridge;
- the localization action is a `DxButton` and no native `OpenTranslationEditor` button remains;
- one-child property button rows use one full-width column and the localization button caption wraps inside its DevExpress control;
- the build-wired localization integrity and shared interaction lifecycle guards contain corresponding regression checks;
- `publisherInterop.js` and `localizationRuntime.js` pass Node syntax checking;
- the normalized JavaScript diagnostics SHA-256 manifest matches the changed maintained scripts;
- project, installer, npm package/lock and browser cache-busting identities are aligned at 3.9.9.

## Maintained source audits

The following maintained source audits passed after the repair:

- async-only component/service architecture;
- async continuation policy;
- Razor component method resilience;
- service resilience;
- Panel Studio persistence and JavaScript diagnostics hash validation;
- prerender JavaScript interop safety;
- iterator exception policy; and
- Razor maintenance architecture.

The user's Windows .NET 10 + DevExpress build remains authoritative for Razor/C# compilation and runtime behavior.
