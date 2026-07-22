# PublisherStudio 1.0.55

## Offline system fonts without losing manual entry

- Added a shared offline `SystemFontCatalog` that discovers font families installed on the computer running PublisherStudio.
- Windows system and per-user font directories, macOS system/user font directories, Linux font directories, and Fontconfig are supported.
- A built-in OpenType name-table reader covers TTF, OTF, TTC, and OTC files when Fontconfig is unavailable.
- WordArt and Picture Studio now use an editable system-font picker backed by a datalist. Users can still type any manual CSS/font-family value.
- RichEdit replaces its fixed font-name entries with the discovered system catalog and keeps user input enabled.
- Spreadsheet Studio applies the same catalog to every font-name ribbon instance and accepts custom values.
- No online font service or web-font download is used. The small built-in list is retained only as an emergency fallback when the operating system exposes no readable font metadata.

## Twitch OAuth window lifetime

- Twitch Device Code authorization now checks whether the browser accepted the popup reservation and navigation.
- The reserved authorization window is no longer closed unconditionally when setup fails.
- Network, Client ID, and provider errors are rendered inside the authorization window with an explicit Close button, while cancellation and successful completion still close it deliberately.

## Explicit and reliable streaming stops

- The Streaming ribbon now separates **Stop streaming**, **Stop recording**, and **Stop session**.
- **Stop streaming** disables active provider outputs without killing recording or Company/LAN delivery.
- **Stop recording** explicitly closes the recording pipelines while the remaining session continues.
- **Stop session** remains the complete shutdown path.
- Browser MediaRecorder shutdown now requests the final chunk, waits for recorder stop and pending Blob-to-WebSocket writes, and only then closes ingest sockets and capture tracks. This prevents the final recording data from being dropped during shutdown.
- Streaming-session mutations are serialized so explicit recording/output stop commands cannot race each other.

Application and installer version: `1.0.55`. Publication format remains `1.45`; picture format remains `1.2`.
