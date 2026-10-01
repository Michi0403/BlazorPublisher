# PublisherStudio 4.0.0 — Story RichEdit live-state ownership repair

## Reproduction that isolated the remaining race

The 3.9.9 localization repair removed the doubled English/German RichEdit text, but the caret and formatting-toolbar jump remained. The decisive reproduction was component-lifecycle specific:

- opening and leaving the untouched default text frame without applying it did not trigger the race;
- after applying Story Editor content back to the Mainframe, reopening that text frame did trigger the race; and
- a newly inserted text frame could trigger the race immediately.

The formatting toolbar could jump through arbitrary values at the same time as the caret. This is therefore treated as the same stale-selection race, not as a font-size/default-format mismatch.

## Live RichEdit state now has one owner

`StoryEditor.razor` no longer two-way binds `DxRichEdit.DocumentContent` or `DxRichEdit.Selection` through Interactive Server component state.

Previously, live RichEdit document/caret changes could round-trip through the server component, schedule a parent render, and then feed server-side editor state back into the same RichEdit. Multiple delayed selection/render snapshots can arrive after a newer browser-side caret state. The ribbon/toolbar then reflects whichever selection snapshot wins that race, including arbitrary formatting values.

4.0.0 changes the ownership contract:

- PublisherStudio supplies `_content` to `DocumentContent` as the initial document for the current keyed RichEdit generation.
- DevExpress RichEdit owns the live document, caret, selection and formatting state while Story Editor is open.
- `SelectionChanged` is observed only so PublisherStudio commands that explicitly operate on selected story text retain their existing behavior. The observed `_selection` is never passed back into RichEdit.
- The automatic Blazor render that normally follows a `SelectionChanged` callback is explicitly suppressed. Merely moving the caret or changing a selection therefore cannot rerender the StoryEditor/RichEdit subtree and replay an older formatting state.
- Apply remains the commit boundary: PublisherStudio exports the live RichEdit document to OpenXML/HTML and updates the Mainframe text component once.
- Closing the editor clears the remembered selection and RichEdit reference.
- Any keyed document-generation replacement, including legacy HTML → OpenXML conversion, discards the old selection before creating the new editor generation.

The 3.9.9 protections remain: RichEdit is excluded from PublisherStudio DOM localization, shared canvas/controller input yields to the full RichEdit surface, and RichEdit toolbar clicks do not schedule the outer Story Editor layout bridge.

## Maintenance protection

The build-wired shared editor interaction guard now rejects:

- reintroduction of `@bind-DocumentContent="_content"`;
- reintroduction of `@bind-Selection="_selection"`;
- a selection observer that does not suppress its automatic StoryEditor rerender; and
- document-generation replacement without stale-selection reset.

The dedicated `build/audit_release_4_0_0.py` source gate verifies those ownership rules together with the carried RichEdit localization/input protections, active version identities and JavaScript diagnostics manifest.

## Version policy

3.9.9 rolls to **4.0.0** because PublisherStudio does not use two-digit minor or patch slots. This is a version-policy rollover, not an intentional compatibility break.

LocalGPT is unchanged in this release.

No `dotnet`, MSBuild, NuGet restore/publish, GitHub access or online repository access was used to prepare this source repair.
