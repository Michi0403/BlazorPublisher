# Interchange format capability plan

PublisherStudio distinguishes three format roles:

- **Native project:** complete PublisherStudio editing state and future extensions.
- **Interchange:** editable exchange with another application; may be lossy.
- **Delivery:** final rendered output; round-trip editing is not expected.

No external format becomes the internal domain model. Every import is parsed into temporary canonical state, validated, accompanied by a compatibility report, and only then committed.

## Picture Studio

| Format | Role | Status | Mapping and loss behavior |
|---|---|---:|---|
| PublisherStudio picture JSON | Native | Import + export | Lossless for the current picture model |
| SVG / SVGZ | Interchange/delivery | **Structured import + export** | Visual paths, shapes, text, images and `use` instances become separate vector layers; groups/layer labels, transforms, definitions, gradients, masks and clips are retained in sanitized SVG markup. Scripts, executable attributes and online assets are rejected. |
| OpenRaster (`.ora`) | Interchange | **Layered import** | PNG/JPEG/WebP and SVG layers are imported in stack order. Nested group names, visibility, opacity, locking and common blend modes are retained; unsupported group compositing and layer formats are reported. |
| PNG, JPEG, GIF, WebP | Raster interchange/delivery | Import + export where applicable | Flattened image layer |
| TIFF | Raster interchange | Planned | Flattened image layer; only after a built-in or explicitly approved decoder exists |
| PSD | Interchange | Deferred | Not implemented without a mature, explicitly approved and licence-reviewed adapter |

The Path tool is independent from SVG import: it is a node-placement vector tool (click nodes; Enter/double-click to finish; Shift closes) rather than a freehand brush alias.

## Publisher page system

| Format | Role | Status | Mapping and loss behavior |
|---|---|---:|---|
| PublisherStudio publication JSON | Native | Import + export | Authoritative pages, objects, data and editing state |
| OpenDocument Drawing (`.odg`, `.fodg`) | Page interchange | **Import** | Drawing pages map to PublisherStudio pages; text boxes, embedded images, basic shapes and SVG path/polygon/polyline objects remain editable at the closest canonical level. |
| OpenDocument Presentation (`.odp`, `.fodp`) | Page interchange | **Import** | Slides map to PublisherStudio pages using master-page dimensions. Unsupported transitions, animations, charts and foreign objects are reported instead of silently discarded. |
| PDF | Delivery/page placement | Existing export; import deferred | Import would require an explicitly approved renderer and would not be assumed editable |
| HTML/SVG/PNG/JPEG/video | Delivery | Existing export | Rendered output, not canonical round-trip state |

## Video Studio

The complete decision matrix, media doctrine, and source review are in [`video-project-import-doctrine.md`](video-project-import-doctrine.md).

| Format | Role | Status | Mapping and loss behavior |
|---|---|---:|---|
| PublisherStudio `VideoProjectDocument` | Native | **Persisted** | Canonical tracks, gaps, explicit placements, source ranges/rates, source references, markers, transitions, cut sections, and native layered live effects. |
| OpenTimelineIO (`.otio`) | Interchange | **Import** | Tracks, clips, gaps, transitions, markers, source ranges, references and supported time effects; nested stacks and unsupported metadata receive a report. |
| OpenTimelineIO bundle (`.otioz`) | Interchange/package | **Import** | Safely reads top-level `content.otio` and matches bounded bundled media. |
| MLT XML / Kdenlive / Shotcut (`.mlt`, `.kdenlive`) | Open-source editor interchange | **Import** | Producers/chains, playlists, multitracks, blanks, source ranges, timewarp hints and transition metadata. Unsupported MLT services are retained/reported. |
| GES/XGES (`.xges`) | Open-source editor interchange | **Import** | Assets, layers, clips, source/timeline timing and audio/video track identity. |
| OpenShot (`.osp`) | Open-source editor interchange | **Import** | JSON files, clips, layers and transitions with explicit compatibility reporting. |
| CMX 3600 (`.edl`) | Cuts-only fallback | **Import** | Event timing, reel/source names and basic transition codes. Frame rate must be verified; richer state is outside EDL capability. |
| FCPXML | Interchange | Later | Dedicated resources/roles/compound-clip mapping and loss matrix required. |
| AAF | Professional interchange | Later after dependency/licence review | Binary essence and effect mapping require an approved adapter. |
| OBS Scene Collection | Streaming-scene interchange | Planned for Mainframe, not Video Studio | Maps OBS scenes/sources/filters/transforms to streaming inputs and compositions, not to an editorial track timeline. |

v1.0.73 preserves all imported tracks but edits and previews one active video-track projection. Full multitrack compositing and audio mixing are deliberately not claimed yet.

## Audio Studio

| Format | Role | Initial direction | Expected loss behavior |
|---|---|---:|---|
| PublisherStudio audio session | Native | Planned import + export | Authoritative tracks, automation and effects |
| Broadcast WAV / WAV stems | Interchange/delivery | First planned professional exchange | Track/effect state rendered into stems; timing metadata retained where supported |
| FLAC, AIFF, Ogg/Opus | Interchange/delivery | Planned | Flattened per file/stem |
| MIDI | Interchange | Planned for note/automation-capable tracks | Audio effects and rendered sound not represented |
| AAF/OMF | Interchange | Later | Explicit capability and loss report |

## Adapter contract

Every adapter must provide:

1. format identity and version;
2. import/export capabilities;
3. a parse result built in temporary state;
4. validation errors and missing-asset reporting;
5. unsupported/flattened feature reporting;
6. round-trip expectations;
7. deterministic tests with representative fixtures;
8. no new dependency unless explicitly approved.

## Panel / Div Studio

| Format | Role | Status | Mapping and loss behavior |
|---|---|---:|---|
| PublisherStudio panel JSON (`.publisher-panel.json`) | Native panel project | **Import + export** | Lossless for views, local canvas, nested publication elements, component/data bindings, HTML sandbox policy, interactions, and layer order. Mainframe placement is preserved when an imported draft is saved over an existing object. |
| JSON Canvas (`.canvas`) | Open layout/graph interchange | **Import + export** | Standard text, link, file, group, position, size, color, node order and edges remain consumable by JSON Canvas tools. PublisherStudio adds an optional `publisherStudioElement` extension for richer round trips; other tools may ignore it. |
| HTML (`.html`, `.htm`) | Web content import/delivery | **Import + export through publication output** | Imported as a sandboxed HTML element. HTML does not preserve Panel Studio views, bindings, interactions, component identity, or the complete editor graph and is therefore not treated as the native project format. |

Panel import is transactional at the editor boundary: the dialog edits an isolated clone, normalizes unique descendant IDs, and commits a second isolated clone only after the selected Mainframe target is still present. Adding a second object to a standalone HTML/DIV element promotes that object to a real `PanelElement` rather than discarding the newly authored content.
