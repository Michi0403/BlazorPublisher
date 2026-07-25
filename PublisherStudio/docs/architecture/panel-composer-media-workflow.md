# Panel composer and media workflow architecture

## Purpose

PublisherStudio v1.0.81 turns Panel / Div Studio and Media Converter Studio into reusable parts of the existing local-first monolith. They do not introduce a second page/component system, a microfrontend or a separate media backend.

## Shared panel component model

`PanelElement` contains one or more `PublicationPanelView` instances. Every view contains the same authoritative `PublicationElement` subclasses used by normal publication pages. A panel therefore reuses:

- media, text, shape and interactive elements;
- DevExtreme charts, grids, KPI and other data visuals;
- publication data objects and web-source bindings;
- Chat and live streaming source elements;
- sandboxed HTML experiences;
- nested `PanelElement` compositions.

Mainframe preview, Panel Studio preview, print/raster paths and HTML exporters consume the same model. Panel Studio is an editor projection over that model rather than a parallel dashboard format.

## Reusable modules

A `PublicationElementTemplate` stores a complete polymorphic `PublicationElement` prototype in `PublicationDocument.ComponentTemplates`. This intentionally preserves more than appearance: data bindings, REST/OData configuration, chart mappings, media settings, interactions, nested panels and other element-specific properties are copied with the prototype.

Templates are document-local. They contain authored publication content, not protected machine configuration. Provider tokens, OAuth state, stream keys and other protected streaming credentials remain outside publications under the existing protected stores.

The publication format is `1.55`. Older documents load with an empty template collection and continue to work.

## Panel gesture ownership

Panel arranging follows the repository gesture contract:

1. Browser JavaScript owns drag movement, drop previews and pointer-resize movement.
2. The user sees a muted pre-rendered ghost while positioning a new component.
3. Blazor receives only the final normalized position/size through `CommitPanelElementBounds`.
4. Test-interactions mode disables arrangement hitboxes so the real embedded component receives input.
5. Listener, pointer-capture and drag state are removed on rebind/disposal.

This prevents Blazor Server round-trip latency from turning high-frequency movement into a queue and prevents the panel overlay from stealing input when interaction testing is active.

## Reusable media-conversion boundary

The authoritative conversion contracts live in `Domain/MediaConversionModels.cs`. `IMediaConversionService` is reusable directly by Interactive Server components and indirectly through `MediaConversionController` for another local frontend.

```text
Mainframe / VideoStudio / Media Converter Studio / local API client
                            |
                            v
                IMediaConversionService
                            |
                            v
                 external FFmpeg process
```

The Controller owns HTTP model binding and file responses. The Service owns capability discovery, profiles, temporary files, process arguments, progress, cancellation and cleanup. Components own only user interaction and hand-off state.

## FFmpeg option policy

PublisherStudio exposes common structured options for trim, dimensions, scaling, frame rate, codecs, quality, bitrate, pixel format, audio layout, loudness, deinterlacing, metadata and filter graphs. An advanced argument field covers FFmpeg options not yet represented by a dedicated control.

Arguments are passed through `ProcessStartInfo.ArgumentList`; no command shell is used. PublisherStudio rejects arguments that would take ownership of its input, output or progress channel, because those would break job monitoring or redirect files outside the managed workflow. Codec, muxer, stream mapping, hardware acceleration, color, filter and quality arguments remain available where the installed FFmpeg build accepts them.

FFmpeg remains external. Its actual encoders, licensing and hardware support depend on the user's installed build.

## Mainframe and VideoStudio hand-offs

- Mainframe recorded presentation output can become the converter's source immediately after export.
- Completed converter output can be inserted into the publication or opened in VideoStudio.
- VideoStudio sends the selected clip plus optional committed source-time range as a `MediaConversionInsertRequest`.
- The converter returns ordinary media bytes and MIME/type information; Mainframe remains the owner of publication insertion.

Video selection geometry is clip-local. The overlay maps against the selected segment's source trim start/end, while the inspector and converter receive the same source-time values. Browser media whose duration is initially unknown is probed again through duration, seekable and buffered ranges before the model is reconciled.

## Local-first size policy

PublisherStudio does not impose a product-specific media/document byte quota. File pickers and local multipart conversion entry points request `long.MaxValue`, and Kestrel's local request-body ceiling remains disabled.

This is not a promise of unlimited physical resources. The practical limit remains available RAM, browser implementation, temporary disk space, filesystem limits, operating-system process limits and the installed FFmpeg build. The user controls those resources in this offline-first application.

Removing byte quotas does not remove structural security:

- archive paths are validated;
- DTD/entity expansion and active SVG/XML content remain prohibited;
- imported HTML remains sandboxed and scripts remain opt-in;
- recursive panels are normalized and bounded;
- canonical import validation still precedes commit.
