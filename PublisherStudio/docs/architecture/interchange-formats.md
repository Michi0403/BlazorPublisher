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

| Format | Role | Initial direction | Expected loss behavior |
|---|---|---:|---|
| PublisherStudio video project | Native | Planned import + export | Authoritative timeline/session state |
| OpenTimelineIO | Interchange | Planned first editable timeline adapter | Effects unsupported by OTIO reported; media references/package policy explicit |
| CMX 3600 EDL | Interchange | Planned import + export | Cuts and simple dissolves only |
| FCPXML | Interchange | Later | Capability report required |
| AAF | Interchange | Later, after dependency/licence review | Complex media/effect mappings; explicit compatibility report |
| MP4/WebM/MKV/image sequence | Delivery | Existing or planned render targets | Flattened render |

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
