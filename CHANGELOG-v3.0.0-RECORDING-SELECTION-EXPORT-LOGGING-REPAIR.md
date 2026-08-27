# PublisherStudio 3.0.0 — Recording, selected-range export and logging repair

## Version rollover

PublisherStudio advances from 2.9.9 to 3.0.0. The repository's single-digit minor/patch rule makes 2.9.10 invalid, and rolling patch 9 forward would also roll minor 9 forward, so the next valid identity is 3.0.0. LocalGPT remains unchanged at 3.3.0.

## Video Studio recording repair

- Fixed the completed-recording insertion path that could overwrite the previously selected segment before selecting the newly inserted recording. The new recording is now selected without committing its temporary recording fields back into the old primary clip.
- Preserved the canonical `MediaTimelineEditService.InsertAt` sequence insertion path and the existing retained-browser-recording ownership rules.
- Kept the routed Editor page as the `InteractiveServer` owner. No nested `@rendermode` directive was added to `MediaStudio`, `InspectorPanel`, or other editor child components.

## DevExpress timeline warning repair

- The publication timeline overview now calculates its RangeSelector tick intervals from the full timeline duration instead of the current zoom window.
- This prevents a heavily zoomed viewport from asking DevExpress to generate an excessive number of overview ticks, which was the source of the `W2003 - Tick interval is too small` browser warning visible in the supplied console capture.

## Preview asset / 404 repair

- Video Studio no longer removes the previous transient `/api/assets/media/{id}` entry immediately when a new preview source is registered.
- Replaced preview URLs are moved to a deferred-release set and removed only after Blazor has completed the DOM render that replaces the browser `src`; final teardown releases everything after browser-side Media Studio disposal.
- This removes the browser-render race visible as stale preview requests returning HTTP 404 while Blazor was still applying the new `src` value.
- Selected-range exports use the stable segment asset registration (`PublicationMediaAssetStore.GetOrRegister(segment)`) rather than sending large data URLs through JS interop.

## Ctrl / Shift timeline selection

- Plain click selects one timeline clip and establishes the Shift anchor.
- Ctrl-click / Command-click adds or removes clips from the selected timeline set.
- Shift-click selects the contiguous range from the anchor to the clicked clip.
- Ctrl/Command + Shift extends the current selected set with the anchored contiguous range.
- The inspector still has one primary clip while all selected clips remain visibly selected; the primary clip gets an additional marker.

## Output: export selected ranges

Video Studio Output now contains two selected-range commands:

- **Export selected ranges separately…** — renders every selected playable clip into its own file.
- **Export selected ranges combined…** — renders the selected clips in canonical timeline order and concatenates the rendered results into one browser-generated video.

For each selected clip, a committed non-point temporal selection is used when present; otherwise its trim range is exported. Existing Video Studio layers/effects, playback-rate policy, audio gain/mute state, capture dimensions, codec policy and browser `MediaRecorder` policy remain in the render path. The existing single-current-range render command remains available.

## PublisherStudio.log restored and made explicit

- `LoggingCore:FileCore:FilePath` is explicitly set to `PublisherStudio.log` in `appsettings.json`.
- PublisherStudio continues to use the LocalGPT-derived `LoggingConfigurationService` + `FileLoggerProvider` + queued `FileLogger` design.
- The file logger now creates/opens the configured log file during provider construction, so the text log is physically present even before the first accepted queued event is written.
- The existing JavaScript diagnostics bridge remains active, so browser exceptions reported through `publisherStudioJavaScriptDiagnostics` continue into the application logger and therefore the file provider.

## Localization and UI

The new selected-range export commands, progress/failure/cancellation states and file-count text were added to all six maintained localization catalogs (`en-US`, `de-DE`, `es-ES`, `fr-FR`, `ja-JP`, `uk-UA`) with exact key parity.

## Source-only validation

No .NET build was performed for this source handoff. Validation is intentionally source/static only and includes:

- JavaScript syntax checking with Node for `mediaStudioInterop.js`;
- JSON parsing for appsettings, package metadata and all localization catalogs;
- release identity / rollover checks;
- render-mode boundary checks;
- recording overwrite-race token checks;
- preview-asset lifetime checks;
- Ctrl/Shift selection and selected-range export checks;
- logging-path / eager log-file creation checks;
- JavaScript diagnostics SHA-256 manifest refresh.

See `VALIDATION-v3.0.0-source.md` and `build/audit_release_3_0_0.py`.
