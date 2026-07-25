# PublisherStudio v1.0.81

## Panel / Div Studio composer

- Rebuilt Panel / Div Studio as a real visual composer instead of a form-only panel editor.
- Added a DevExpress Ribbon for views, arrangement, reusable modules, insertion and layer commands.
- Added a categorized drag-and-drop component ribbon with pre-rendered component previews.
- The canvas now shows the actual shared PublisherStudio components while arranging a panel, including data visuals, media, chat, live sources, HTML experiences and nested panels.
- Dragging a component shows a muted live ghost at the proposed location before it is committed.
- Existing panel components can be moved and resized with local browser-owned pointer handling; Blazor receives only committed bounds.
- Added Arrange and Test-interactions modes so authored buttons, menus, data components and embedded experiences can be exercised without accidentally moving them.
- Added reusable component modules. A selected component can be saved, updated, copied or deleted as a document-level template including its data bindings, web-source configuration, chart settings, media settings and nested content.
- Added component-specific inspectors plus an advanced JSON editor for complete configuration access.
- Kept recursive panel normalization, shared component rendering and HTML export support. Nested panels remain bounded to eight levels.
- Improved fallback icons and Ribbon/palette styling where missing SVG/icon resources previously left blank commands.

## Media Converter Studio

- Rebuilt Media Converter Studio with DevExpress Ribbon tabs, a context menu, drag-and-drop source loading, visible commands and conversion job management.
- Added reusable conversion targets and profiles for PublisherStudio web output, general browser media, video editing, streaming, archival and custom workflows.
- Added configurable trim start/duration, arbitrary width and height, fit/fill/stretch behavior, aspect-ratio preservation and frame rate.
- Added video encoder, encoder preset, CRF, bitrate, maximum bitrate, buffer and pixel-format controls.
- Added audio encoder, bitrate, sample rate, channels and EBU loudness-normalization controls.
- Added deinterlacing, fast-start, metadata copying, custom metadata, video/audio filter graphs and advanced FFmpeg arguments.
- Advanced arguments use `ProcessStartInfo.ArgumentList`; PublisherStudio blocks only input/output/progress arguments owned by the service while retaining codec, muxer, mapping, filter, color, hardware and quality options.
- Added saved user profiles alongside capability-aware built-in profiles.
- Added completed-job actions for download, Mainframe insertion and opening video output in VideoStudio.
- The service/controller contract remains reusable by Mainframe, VideoStudio, other components and external local frontends.

## Mainframe and VideoStudio workflow

- Recorded Mainframe WebM exports can flow directly into Media Converter Studio after the regular export completes.
- Converted media can be inserted back into the active publication without leaving the Mainframe workflow.
- Mainframe and VideoStudio can open a supplied media source in the converter with suggested trim/profile options.
- VideoStudio can send the selected clip or committed source-time range to Media Converter Studio.
- Corrected visible VideoStudio selection geometry to use the selected clip's source trim window rather than the whole source duration.
- Added late-duration recovery for WebM and other media whose browser metadata initially reports `0.01`, infinity or no usable duration.

## Local-first file-size policy

- Removed PublisherStudio-defined upload/import byte ceilings from the affected Blazor and interchange paths.
- Blazor file streams use `long.MaxValue`, Kestrel request-body limits remain disabled locally, and media-conversion multipart limits use `long.MaxValue`.
- Structural safety remains intact: archive traversal checks, XML/DTD restrictions, sandbox permissions and canonical validation are not weakened.
- PublisherStudio does not claim infinite physical capacity. Available memory, browser behavior, temporary storage, filesystem limits and the installed FFmpeg build still define the machine's practical ceiling.

## Persistence and compatibility

- Added document-level reusable component templates.
- Publication format advanced from `1.54` to `1.55`.
- Application, installer, npm, lock-file, runtime and structured-export manifest versions advanced to `1.0.81`.
- NuGet and npm dependency sets are unchanged.
