# PublisherStudio 4.8.3

- Check that the web app and installer versions match while retaining the 1-Wire traffic journal checks.
- Exclude Visual Studio working state from the release source fingerprint.
- Keep the workbook and its footer visible when the spreadsheet AI form is open.
- Keep mainframe selection overlays below studio dialogs and suspend canvas input for reconnect and AI dialogs.

## Editor validation

- [x] Panel Studio interaction lifecycle and authoring geometry guards.
- [x] Edge shows the workbook below the AI form and accepts test input.
- [ ] Edge verifies that a selected mainframe object cannot move or resize behind a modal and the same selection is usable after closing it.
