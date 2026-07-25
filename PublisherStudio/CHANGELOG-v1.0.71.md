# PublisherStudio v1.0.71 changelog

## Video play-canvas regression repair

- Video Studio now keeps the preview video stretched across the complete play canvas by default instead of presenting a small centered picture-in-picture frame.
- **Stretch**, **Fill canvas**, and **Fit whole** remain selectable and persisted. Legacy v1.0.70 videos that still carry the old implicit `Contain` default open as **Stretch** until the user explicitly chooses a fit mode.
- Video preview CSS no longer applies the old `max-height` ceiling that detached the media surface from the available Studio canvas.

## Bottom-docked playback and temporal selection

- The Studio-owned playback/selection control is docked to the bottom edge of the video canvas.
- The timestamp/range track stays visible directly above the dock, with start/end handles, selected range, point selection, playhead, and drop marker sharing one stable full-canvas coordinate system.
- The temporal overlay now owns the complete play canvas even when **Fit whole** creates letterboxing. Only the spatial frame-region overlay follows the rendered source-pixel rectangle.
- The sequence playhead updates locally during playback and scrubbing so source time and project time remain visibly synchronized.

## Explicit pointer modes

- Video Studio exposes **Select time**, **Place playhead**, **Add cutline**, and **Frame region** in the ribbon, the video-canvas control dock, and the context menu.
- **Select time** clicks one timestamp or drags a range.
- **Place playhead** scrubs the selected source clip without replacing its selected range.
- **Add cutline** places the project playhead and splits the selected sequence clip at that exact source timestamp.
- **Frame region** remains the only mode that activates the spatial polygon overlay.

## Overlay and Z-order ownership

- Video, temporal interaction, frame-region interaction, insertion placement, and drag/drop feedback now use explicit local layers inside the play-canvas stacking context.
- The inactive frame-region overlay is hidden instead of leaving its dim veil, help panel, or action controls above normal playback.
- Native video controls, fullscreen activation, and Picture-in-Picture remain disabled inside Video Studio so they cannot steal Studio pointer gestures.
- Video drag/drop uses the full play-canvas temporal overlay and keeps the green insertion marker at the selected clip timestamp.

## Commands and persistence

- Video sizing commands are available in the `DxRibbon` and Video Studio context menu with checked-state labels.
- The selected fit mode is marked explicit when applied from Video Studio or the Inspector, preserving later user choices.
- Application and installer version is `1.0.71`.
- Publication format remains `1.49`; Picture Studio format remains `1.4`.
- No NuGet, npm, or native dependency was added or changed.
