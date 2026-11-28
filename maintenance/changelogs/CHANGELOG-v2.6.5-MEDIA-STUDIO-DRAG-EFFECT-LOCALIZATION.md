# PublisherStudio 2.6.5 — Media Studio drag, effects and localization

## Selected Picture Studio object export

- Mainframe `Export selected object as` now delegates Picture Studio-backed images to the Picture Studio compositor instead of the generic DOM crop path.
- Merged PNG and merged SVG use the complete authored Picture Studio canvas; layered SVG keeps the editable Picture Studio document.
- The existing merged/layered choice remains explicit.

## Shared studio drag-and-drop

- Added one PublisherStudio drag-transfer contract for internal studio objects and named media payloads.
- Picture Studio layers can be reordered by dragging in the layer list.
- Dropped image/media payloads can be materialized through the same shared transfer format used by Mainframe media dragging.
- Picture Studio pre-renders drag-out representations so composed/layered objects can be dragged out with authored names rather than falling back to an anonymous browser image.
- Video Studio segments, video effect layers and video filters use the same shared drag-transfer contract.
- Dropping a timeline segment between clips moves it there; dropping it inside another clip uses the existing timeline split/insert semantics.
- External video/audio drops onto the timeline use the same insertion-point logic rather than a separate importer-only path.

## Video Studio ribbon and effect layers

- Replaced the overloaded Video Studio command bar with categorized Ribbon tabs: Home, Edit, Layers, Effects and Output.
- Video layers and their filters are presented as draggable stacks with common layer/effect affordances.
- Chroma key, vignette, blur, grain and color adjustments refresh the composited canvas immediately, including while playback is paused.
- Runtime enum normalization accepts both numeric and string enum transport, avoiding effects silently disappearing when serializer representation changes.
- Pixel effects use a readback-oriented Canvas2D context; CSS/video effects and pixel effects share one composed layer pipeline.
- The interactive 3D blob renderer now paints a back cap, lit side walls and live-video depth slices rather than simulating depth as a repeated 2D shadow.
- The same extruded browser runtime is emitted for Mainframe/Panel/standalone HTML interchange.

## Media time safety

- Media poster inspection, preview seek, range playback and seek restoration reject NaN/Infinity before assigning `HTMLMediaElement.currentTime`.
- The Media Studio, Picture Studio, Publisher and video-effect browser assets are cache-busted at 2.6.5 so an older 2.5.0 runtime cannot remain active after upgrading.

## Localization and icons

- Main publication Ribbon, Page Navigator, Inspector, Picture Studio and Video Studio now resolve source-owned UI text through `IFileLocalizationService`.
- `IFileLocalizationService.GetText` maps canonical English literals to existing localization keys so maintained components can localize without a second string system.
- German coverage was completed for the current Mainframe/Picture/Video editing surfaces; unchanged values are principally numbers, product names or terms normally written identically in German.
- Application command icons now use CSS icon classes with escaped Unicode code points and a symbol-safe font stack instead of culture-sensitive literal glyphs.

## Compatibility

- PublisherStudio Web and InstallerConsole are 2.6.5.
- Publication format remains 1.58; no document migration is required.
- Picture Studio format remains 1.5.
- No database migration is required.
- Word-processing/RichEdit and its print pipeline were not changed.
- LocalGPT was not changed by this release.
