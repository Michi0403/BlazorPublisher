# v1.0.2 — recorded-video decode and timeline legibility fix

- Fixed recorded video data URLs whose codec parameter contained a comma (for example `video/webm;codecs=vp8,opus`). The comma was interpreted as the data-URL payload separator and produced an undecodable source after the recording returned from JavaScript to Blazor.
- Recorded media is now transferred with the container MIME type only while preserving the original encoded bytes.
- Existing malformed embedded recording URLs are normalized when opened in Media Studio, so affected documents can be recovered without recording again.
- Video recording now prefers broadly compatible VP8 WebM when the browser can both record and play it.
- DevExpress Range Selector labels now hide overlaps, and timeline ruler labels have clearer spacing and contrast.
- Removed the remaining `100vw` workspace constraint and hardened shell/status-bar sizing so the right edge and zoom controls remain inside the viewport.
