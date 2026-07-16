# v1.0 — media studios and page timeline

## Audio and video objects

- Publications can embed audio and video as normal movable, resizable, orderable, lockable, animatable page elements.
- **Insert > Media** accepts common browser-supported audio and video formats.
- Video objects preserve a generated poster frame, fit mode, controls, volume, mute, loop, playback rate, trim range, timeline start, and playback trigger.
- Audio objects preserve a compact or waveform presentation, accent color, controls, volume, fades, mute, loop, playback rate, trim range, timeline start, and playback trigger.
- Media objects participate in layers, selection, copy/duplicate/delete, undo/redo, right-click commands, interactions, page duplication, JSON save/open, and standalone animated HTML export.

## Video Studio and Audio Studio

- Video Studio imports clips or records the camera or screen through the browser `MediaRecorder` API.
- Audio Studio imports clips, records a microphone, or generates a small WAV tone as a self-contained example source.
- Both studios provide transport controls and non-destructive trim handles backed by DevExpress `DxRangeSelector`.
- Audio Studio adds simple volume, playback-rate, fade-in, fade-out, mute, and loop controls.
- Recordings are returned to Interactive Server in bounded text chunks, avoiding Blazor binary stream-reference compatibility problems.
- Studio right-click menus expose playback, playhead-based trim commands, trim reset, mute/loop, and source replacement.

## Page timeline

- An on-demand timeline is docked to the bottom of the workspace, so it behaves like the Pages and Properties panes instead of creating another toolbar row.
- It provides a page playhead, play/pause/stop transport, visible-range selector, ruler, tracks, and media/animation clips.
- Animation clips can be moved and resized; media clips can be moved and trimmed directly.
- Explicit animation positions coexist with the existing trigger-based sequence. A context command returns an animation to normal trigger timing.
- Timeline context menus cover preview, timing, duplicate/delete, play/pause, trim at playhead, page duration, animation presets, and media insert/create commands.
- Page thumbnails now have contextual transition and duration commands.

## Export behavior

- Standalone animated HTML export includes embedded media, trim boundaries, volume, playback rate, fades, looping, page-entry playback, click playback, and media interaction actions.
- Static print output keeps a useful media poster/waveform representation while suppressing active controls.
- The semantic page timeline remains suitable for later PowerPoint timing-tree and deterministic video-renderer modules. Native animated PowerPoint generation and encoded video export are not included in this release.

## Compatibility

- The publication document format advances from `1.8` to `1.9`.
- Existing publications load with a ten-second default page timeline and no media objects.
- No new NuGet or JavaScript package is required.
