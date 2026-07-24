# Interchange format capability plan

PublisherStudio distinguishes three format roles:

- **Native project:** complete PublisherStudio editing state and future extensions.
- **Interchange:** editable exchange with another application; may be lossy.
- **Delivery:** final rendered output; round-trip editing is not expected.

No external format becomes the internal domain model.

## Picture Studio

| Format | Role | Initial direction | Expected loss behavior |
|---|---|---:|---|
| PublisherStudio picture JSON | Native | Import + export | Lossless for supported picture model |
| OpenRaster (`.ora`) | Interchange | Planned import + export | Unsupported live/procedural effects rasterized per layer with report |
| SVG | Interchange/delivery | Existing export, planned structured import | Unsupported filters/fonts reported or outlined/rasterized by explicit option |
| PNG, JPEG, WebP, TIFF | Delivery/raster interchange | Existing common raster workflow; TIFF planned | Flattened image |
| PSD | Interchange | Later, only with a mature licensed adapter | Feature-dependent; never the canonical model |

## Video Studio

| Format | Role | Initial direction | Expected loss behavior |
|---|---|---:|---|
| PublisherStudio video project | Native | Planned import + export | Authoritative timeline/session state |
| OpenTimelineIO | Interchange | First planned editable timeline adapter | Effects unsupported by OTIO reported; media remains externally referenced or packaged explicitly |
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
7. deterministic tests with representative fixtures.

The first implementation should be one contained vertical slice in its owning Studio, not a Mainframe-wide generic importer.
