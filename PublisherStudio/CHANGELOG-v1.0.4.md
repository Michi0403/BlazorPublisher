# PublisherStudio v1.0.4 beta

- Changed presentation video export to use the largest publication page as the recording frame instead of the browser viewport.
- Added Chromium/Edge Element Capture and Region Capture support so current-tab recordings are cropped to the publication frame without browser-side black bars.
- Requests the resulting video track at the publication frame resolution and reports the exported frame dimensions in the editor status message.
- Keeps the existing full-tab recording as a compatibility fallback when page-region capture is unavailable or a different capture surface is selected.
