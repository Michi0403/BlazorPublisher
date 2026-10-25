# PublisherStudio 4.4.9 — Picture Studio GIF frame visibility

Source baseline: the user's `PublisherStudio-4.4.8-editor-first-render-contract-repair-source(1).zip`.
Release status: **offline source candidate; .NET and the installed Editor were not run**.

## Changes actually introduced in 4.4.9

- Added a horizontally scrolling, selectable **GIF frame filmstrip** to the Picture Studio canvas workspace. Each frame uses its associated editable raster layer's data URL when present and displays its time delay; selecting a frame uses the existing `PictureStudioState.SetActiveAnimationFrame` path.
- The existing drawing canvas and frame selection/editing/export logic remain in place. The canvas occupies the remaining workspace below the filmstrip when editing an animation; other media retain the previous canvas layout.
- The footer now says **Apply animated GIF** for an animated picture, but continues to use the existing `ApplyMerged` implementation; this is not a newly rewritten encoder.
- Incremented PublisherStudio.Web **and** PublisherStudio.InstallerConsole from 4.4.8 to **4.4.9**.
- Added `picture-studio-frame-strip-release.test.cjs` to guard the filmstrip, active-frame routing, canvas preservation and output label.

## Inherited, not newly fixed or proven

- 4.4.8 already removed a bad `InspectorPanel.OnInsertAiChat` parameter that prevented initial Editor rendering, and added a component contract source guard. Both are preserved.
- 4.4.8 also carried earlier Picture Studio canvas lifecycle/GIF revisions; this release does **not** prove that the installed Editor can show and draw over an ordinary PNG/GIF in a live Blazor/DevExpress popup. The customer's repeated runtime report remains an open acceptance requirement.
- This release does not claim to resolve every historical PublisherStudio AI Assist, spreadsheet insertion, browser integration, or public-test failure from the previous conversation.

## Verification

- `node --test tests/js/*.test.*`: **9 passed** (including the existing GIF decoder, canvas regression and Editor parameter-contract tests).
- No PowerShell source guards, .NET compilation/build, full-page browser render, installed Windows publish, nor real publisher document was tested.

## Target-machine acceptance

1. Clean-extract/build PublisherStudio; open Editor and Picture Studio.
2. Import an ordinary PNG, verify visible pixels and brush edits.
3. Import a multiple-frame GIF; verify frame strip, frame selection, edits to selected frame, and successful animated-GIF application/export.
4. Confirm `InspectorPanel` binding errors do not recur and AI Assist interaction is independently checked.
