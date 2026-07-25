# PublisherStudio v1.0.73 changelog

## Canonical open video-project model

- Video projects are no longer treated as one flat clip list. `VideoProjectDocument` preserves project identity, source format/version, frame rate, canvas dimensions, ordered typed tracks, explicit clip placement, gaps, source ranges, source rates, speed, transitions, markers, and adapter metadata.
- `VideoElement.VideoProject` persists the canonical imported project. The existing media sequence remains the editable/render-compatible projection of the selected video track, so v1.0.72 selections, cut sections, layer-bound regions, chroma key, and live filter stacks continue to work on imported clips.
- Switching the editable project track commits the current projection back to that canonical track before another track is loaded. Additional imported video tracks and audio/subtitle/data tracks remain preserved for later full multitrack orchestration.
- Publication format advances to `1.52`; older publications continue to load through the existing normalization and migration paths.

## Implemented open project import base

- OpenTimelineIO `.otio` import maps clips, gaps, tracks, source ranges, rates, markers, linear time warps, media references, and transition timing into the canonical project.
- OTIO bundle `.otioz` import reads the required top-level `content.otio` and embeds safely matched media from the bundle.
- MLT XML `.mlt` and Kdenlive `.kdenlive` import maps profiles, producers/chains, playlists, blanks, multitrack order, source ranges, speed and transition timing. This also provides the common base for Shotcut projects stored as MLT XML.
- GStreamer Editing Services `.xges` import maps layers, audio/video track types, assets, source in-points, explicit timeline positions, and durations.
- OpenShot `.osp` import maps project profile, files, clips, layers, positions, source ranges, and transition timing.
- CMX 3600 `.edl` import provides a cuts/reel/source fallback and always reports the frame-rate assumption and structural limitations.
- Every adapter returns a compatibility report. Unsupported foreign effects are retained as metadata where possible and reported as approximation/loss instead of being silently represented as native PublisherStudio filters.

## Missing-media and Studio workflow

- Video Studio adds **Import open project…** and **Relink project media…** commands to both the `DxRibbon` workflow and context menu.
- Project files can be selected or dropped directly onto Video Studio. Empty-canvas/page media insertion remains separate.
- Offline clips remain visible in the project sequence with their retained timing and reference details instead of disappearing during normalization.
- Multiple media files can be relinked in one operation by imported identifier, reel, URI/path filename, or filename stem. The compatibility panel reports unresolved and unmatched sources.
- The Properties panel exposes source format, canvas/rate, track/transition counts, missing-media count, editable video-track selection, and adapter issues.

## Fact-checked format doctrine

- Added `docs/architecture/video-project-import-doctrine.md` with the project/media distinction, canonical mapping, safety rules, implemented base, and a roadmap for FCPXML, AAF, subtitle tracks, image sequences and LUTs.
- OBS Scene Collection JSON is classified as a high-value gamer/creator import for Mainframe streaming scenes, not falsely treated as an editorial Video Studio timeline.
- Media references can be preserved even when the browser cannot decode the source container/codec. Direct playback, proxy/transcode support, and project interchange are documented as separate capabilities.

## Import safety and dependency policy

- JSON imports have bounded size and depth.
- XML imports prohibit DTD processing and external resolution.
- OTIOZ imports enforce compressed-input, entry-count, individual-entry and total-expanded-size bounds, reject unsafe paths, and perform no network fetch.
- The implementation uses the .NET base class library only. NuGet, npm, native binary, and external-process dependency sets are unchanged.

## Version and compatibility

- Application, installer, streaming runtime capability, npm package, and lock-file version is `1.0.73`.
- Publication format is `1.52`; Picture Studio format remains `1.4`.
- v1.0.73 is an interchange/preservation foundation. It edits and previews one active video-track projection; simultaneous multitrack video compositing and audio mixing are not claimed in this release.
