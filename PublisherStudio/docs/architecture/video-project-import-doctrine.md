# Video-project import doctrine

## Decision

PublisherStudio keeps one native canonical project model and treats every external project file as an adapter input. The importer parses into temporary state, validates and reports approximations, missing assets, and unsupported features, and only then commits a `VideoProjectDocument`.

Project format is not a media codec or container. Most timeline formats store editorial decisions and references to media; they do not guarantee that the referenced video, audio, image sequence, font, LUT, plug-in, or generated asset is embedded. PublisherStudio therefore preserves source identifiers and original paths, marks unresolved media, and provides explicit relinking. OTIOZ is the important exception in the initial base because it can package `content.otio` with referenced media.

The v1.0.73 editor preserves all imported tracks, gaps, placements, source ranges, playback speeds, markers, transitions, and source references in `VideoProjectDocument`. Video Studio exposes an active video-track projection through its existing clip editor. Editing that projection writes back to the selected canonical track. This release is an interchange and preservation foundation, not full multitrack compositing: additional video tracks and imported audio tracks are retained but are not yet simultaneously mixed into the preview canvas.

## Fact-checked base

| Format | Role in PublisherStudio | v1.0.73 status | Why it belongs in the base | Known limits |
|---|---|---:|---|---|
| OpenTimelineIO (`.otio`) | Preferred neutral editorial interchange | Import | OTIO models timelines, tracks, stacks, clips, gaps, transitions, markers, source ranges, media references, and time effects without pretending to contain the actual media. | Application-specific effects and metadata may not have an equivalent PublisherStudio filter or layer. Nested stacks are flattened with a loss report in this first adapter. |
| OpenTimelineIO bundle (`.otioz`) | Portable OTIO timeline plus media package | Import | The defined bundle contains top-level `content.otio` and can include media, making it useful for moving projects between machines. | Bundle paths and sizes are validated. Only media references that can be matched safely are embedded. |
| MLT XML (`.mlt`) | Open-source editor/project interchange | Import | MLT XML is the common timeline representation behind MLT-based editors and describes producers, playlists, multitracks, filters, and transitions. | Arbitrary MLT services and filters are retained as metadata or reported when no canonical mapping exists. |
| Kdenlive project (`.kdenlive`) | Common open-source NLE project | Import | Kdenlive projects are MLT XML and are widely used by Linux and cross-platform creators. | Bin-only metadata, title generators, proxy policy, compositions, and Kdenlive-specific effects may need relinking or approximation. |
| Shotcut project (`.mlt`) | Common creator/gamer editor project | Import through MLT | Shotcut stores projects as MLT XML, so the same safe adapter covers a popular lightweight creator workflow. | Shotcut-specific filter parameters are preserved as adapter metadata until mapped to native PublisherStudio filters. |
| GES/XGES (`.xges`) | GStreamer/Pitivi timeline interchange | Import | GStreamer Editing Services provides a structured timeline model and serializes projects as XGES. It fits live-media and Linux creator workflows. | GES child properties, effect assets, nested timelines, and non-video/audio track types may be approximated or retained only as metadata. |
| OpenShot project (`.osp`) | Accessible JSON NLE project | Import | OpenShot uses an open JSON project representation with files, clips, layers, and transitions. It is common enough to be useful without adding a binary dependency. | Animated keyframes, titles, Blender-generated assets, and effects without canonical equivalents receive compatibility warnings. |
| CMX 3600 EDL (`.edl`) | Cuts-only fallback interchange | Import | EDL remains a useful least-common-denominator for edit decisions and reel/source relinking. | Frame rate is not reliably self-describing; complex layers, filters, regions, most audio detail, and modern metadata are not represented. The importer requires the user to verify rate/timecode. |

## Formats that make sense next

| Format | Recommended destination | Priority | Doctrine |
|---|---|---:|---|
| FCPXML | Video Studio | Later | Publicly documented and useful, but its resources, roles, magnetic timeline, compound clips, effects, and versioned schemas need a dedicated adapter and explicit loss matrix. It is not an open-source format, so it is compatibility work rather than part of the open base. |
| AAF | Video Studio and Audio Studio | Later after dependency/licence review | Professionally important but binary, complex, and strongly tied to essence/media handling. Do not introduce a large dependency without review and deterministic fixtures. |
| SRT, WebVTT, ASS/SSA | Subtitle tracks in Video Studio | Near-term | Common, open/text-based, easy to validate, and directly maps to `Subtitle` tracks. Styling loss must be reported for SRT/WebVTT. |
| Image-sequence manifest | Video Studio | Near-term | PNG/JPEG/WebP/EXR sequences are common for animation and VFX. Import should be an explicit sequence asset with frame rate and missing-frame report, not hundreds of unrelated clips. |
| LUT formats (`.cube`, `.3dl`) | Video layer filter | Near-term | Useful to streamers, gamers, and filmmakers. A LUT must be a native layer filter with live preview and render parity, not hidden importer metadata. |
| OBS Scene Collection JSON | Mainframe streaming scene import | High creator/gamer value, separate adapter | OBS scene collections describe scenes, sources, filters, transforms, and streaming-oriented composition rather than an editorial video timeline. Import belongs in the BlazorStudio Mainframe, mapping sources to live inputs and scenes to publication/streaming compositions. It should not be disguised as a Video Studio timeline import. |
| Blender VSE `.blend` | Prefer OTIO/EDL/rendered media | Not a base adapter | `.blend` is an application-native binary database, not a stable neutral interchange contract. Blender-side OTIO/EDL export or rendered media is safer. |
| DaVinci Resolve `.drp` | Prefer OTIO/FCPXML/AAF/EDL | Not a base adapter | `.drp` is application-native. Use documented exchange formats rather than reverse-engineering a project archive. |
| Audacity `.aup3` | Audio Studio through stems/WAV | Not a Video Studio adapter | It is an application-native database. Audio exchange should use WAV/BWF stems and timing metadata. |

## Media-format doctrine

A filename extension does not prove browser playback support. Containers and codecs are separate decisions. PublisherStudio may preserve and relink a media reference even when the browser cannot decode it; rendering or proxy generation can later use an approved local media pipeline.

### Practical video ingest

- **Direct browser-oriented base:** MP4/M4V, WebM, Ogg Video, and QuickTime/MOV when the browser supports the contained codecs.
- **Preserve/relink, proxy may be required:** Matroska/MKV, AVI, MPEG transport/program streams, MXF, camera-raw formats, and professional mezzanine codecs.
- **Animation/VFX:** image sequences should become one sequence source with frame rate, not a pile of unrelated layers.

### Practical audio ingest

- **Direct base:** WAV/BWF, FLAC, MP3, AAC/M4A, Ogg/Vorbis, and Ogg/Opus when supported.
- **Project interchange:** stems should retain a common timeline origin, source rate, and track identity.
- **Later:** AIFF, CAF, MIDI, and AAF/OMF adapters belong to Audio Studio with explicit capability reports.

### Practical image ingest

- **Direct base:** PNG, JPEG, WebP, SVG/SVGZ, GIF where animation policy is explicit, and OpenRaster for layered pictures.
- **Later/proxy:** TIFF, EXR, HDR, camera raw, and layered formats that need an approved decoder.
- Picture Studio remains the canonical place for editable raster/vector layers. Video Studio consumes a picture document or rendered frame source rather than cloning the picture model into every clip.

## Canonical mapping

External adapters map into:

- `VideoProjectDocument`: project name, source format/version, frame rate, dimensions, tracks, transitions, markers, metadata, and active track.
- `MediaTimelineTrack`: kind, order, enabled/muted/locked state, and placed segments.
- `PublicationMediaSegment`: timeline placement and duration, source range, rate/speed, source reference, missing state, cut sections, and native video layers/filters.
- `MediaTimelineTransition`: canonical transition kind plus original adapter metadata.
- `MediaSourceReference`: stable imported identifier, URI/path, reel name, MIME hint, missing state, and adapter metadata.

Unsupported external effects are never silently claimed as native. The adapter either maps them, retains enough metadata for a future adapter update, flattens only when a rendered source exists, or reports loss.

## Safety and dependency rules

- JSON has bounded depth and project size.
- XML disables DTD processing and external resolution.
- Archives have bounded total size, entry count, entry size, and normalized paths; traversal is rejected.
- Missing media never causes the whole timeline to disappear.
- No network fetch occurs during import or relinking.
- The initial base uses only the .NET BCL; no new NuGet or npm dependency is introduced.
- Test fixtures and a compatibility report are required for every new adapter.

## Primary specifications reviewed

- Academy Software Foundation, OpenTimelineIO repository and adapter/schema documentation: https://github.com/AcademySoftwareFoundation/OpenTimelineIO
- MLT XML documentation: https://www.mltframework.org/docs/mltxml/
- Kdenlive project file documentation: https://docs.kdenlive.org/en/project_and_asset_management/file_management/project_files.html
- Shotcut project/MLT documentation: https://shotcut.org/notes/mlt-xml/
- GStreamer Editing Services documentation: https://gstreamer.freedesktop.org/documentation/gst-editing-services/
- OpenShot project-file documentation/source: https://www.openshot.org/static/files/user-guide/files.html and https://github.com/OpenShot/openshot-qt
- Apple Final Cut Pro XML documentation: https://developer.apple.com/documentation/professional-video-applications/fcpxml-reference
- OBS Studio source/documentation for scene collections: https://github.com/obsproject/obs-studio
