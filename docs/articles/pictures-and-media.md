# Pictures and media

Picture Studio and Media Studio keep editing details inside the selected publication object.

## Picture Studio

Picture documents can contain raster, text, shape, fill, and procedural layers. Crop, masks, transparency, filters, tint, blend modes, drawing, and polygon selections remain editable until you apply the result.

PublisherStudio stores both a rendered image and the editable picture document. The rendered image keeps normal publication export simple; the picture document lets you return for non-destructive edits.

## Video and audio

Media objects use ordered segments. You can trim, split, merge, copy, replace, and arrange segments without changing the object's page position or layer order.

Video regions are stored as normalized source coordinates, so they remain aligned across different source sizes. Audio uses time only and does not pretend to have a two-dimensional region.

## Recording preview

The live preview keeps the active browser media stream attached to the preview element. If Blazor replaces the video element, a bounded watchdog reattaches the stream. Saved recording data stays separate from the preview surface.

Recording quality is controlled before capture with source/native, streaming-master, streaming-output, or custom dimensions plus frame rate, codec preference, and explicit video/audio bitrate targets. These settings do not remove or flatten media segments, page layer order, crop regions, or retained recording recovery.

## Local editing overlays

Internal media sections and selection overlays never become page siblings or picture layers. They are transient editor projections inside a local positioned stacking context. The publication object remains the canonical content, while browser-side pointer feedback stays lightweight and disposable.

## Open picture and publication interchange

PublisherStudio keeps editable source information when it can and reports losses when a target format cannot represent a feature. Supported adapter families include SVG / SVGZ, OpenRaster, OpenDocument Drawing, and OpenDocument Presentation. The native PublisherStudio project remains the complete editable source.

## Open video projects

Project format is not a media codec or container. PublisherStudio can inspect and import project structures from OpenTimelineIO, MLT XML, Kdenlive, Shotcut, XGES, OpenShot, and CMX 3600 EDL. OBS Scene Collection data may be used as source context where supported.

The first editable projection is the active video-track projection, not full multitrack compositing. Missing media is reported for relinking instead of silently discarded, and the original project remains the safest archival copy. 🎞️

Audio interchange can preserve production metadata through Broadcast WAV where the selected import or export path supports it.
