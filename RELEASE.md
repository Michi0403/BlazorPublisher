# PublisherStudio 4.0.0

PublisherStudio 4.0.0 repairs the remaining Story/Text Studio caret and formatting-toolbar race isolated by the Mainframe text-component lifecycle reproduction.

The RichEdit document and caret are no longer two-way controlled by Interactive Server component state. PublisherStudio provides the initial document for a keyed editor generation, DevExpress owns the live edit/selection/formatting state in the browser, and **Apply** exports that live document back to the Mainframe model as the explicit commit boundary. Selection notifications remain available to selection-dependent Story Editor commands but no longer cause StoryEditor to rerender, preventing delayed server-side selection snapshots from perturbing the live caret or toolbar. Stale selections are discarded when an editor generation changes or closes.

The toolbar jumping through arbitrary values is treated as part of the same stale-selection race; this release does not change new-text font defaults merely to mask that symptom.

The 3.9.9 RichEdit localization/input ownership repair and the Panel Studio/inspector repairs remain in place. LocalGPT is unchanged.

The 3.9.9 → 4.0.0 change is the repository's required single-digit minor/patch rollover, not an intentional breaking-release declaration.

No .NET build was attempted in this environment. See `CHANGELOG-v4.0.0-STORY-RICHEDIT-LIVE-STATE-OWNERSHIP.md` and `VALIDATION-v4.0.0-source.md`.
