# BlazorPublisher v0.3.2 RichEdit popup hotfix

- Fixed the custom **Edit story** modal stacking order. Its previous `z-index: 10000` covered DevExpress RichEdit drop-downs and dialogs, whose popup stack starts at 1050.
- Restored visibility and interaction for the built-in RichEdit Download menu, font and font-size lists, paragraph/page color pickers, Page Layout menus, Insert Caption/Add Text menus, Bookmark dialog, and Paste drop-down.
- Added a visible field catalogue beside Quick Fields that explains the application-specific field codes and points to RichEdit's additional built-in field commands.
- Kept the existing document model, host, editor workflow, and custom story download buttons unchanged.

The main Paste command still depends on browser clipboard permissions and a user gesture. The drop-down itself is no longer hidden by the story modal.
