# PublisherStudio 4.5.3 offline validation

- Source baseline: user-supplied PublisherStudio 4.5.2 full-source ZIP.
- JS tests: 13/13 passed (`node --test tests/js/*.test.*`).
- All six locale JSON dictionaries parse and contain the exact `LT("Apply animated GIF")` phrase key, with German explicitly translated.
- `node --check docs/templates/publisherstudio/public/main.js`: passed.
- Chromium synthetic-page test: tear mask cuts through text in the DocFX content region after drag; corresponding SVG paper edge appears; no accidental text selection; hash navigation and reload restore normal content; both dark/light themes passed with no page JS errors.
- Windows PowerShell localization validator, .NET build, licensed DevExpress rendering, and full documentation build: **not executed**.
