# PublisherStudio 2.3.1 — documentation viewer diagnostics manifest repair

- Keeps the 2.3.0 native-browser close callback repair for the HTML/PDF/API documentation modal.
- Refreshes the reviewed JavaScript diagnostics SHA-256 manifest for `wwwroot/js/documentationViewer.js`, so the maintained diagnostics integrity gate recognizes the reviewed change.
- Preserves the working 2.2.10 console-release wiring, Kawaii DocFX layout, API reference, PDF generation, Pages snapshot seeding, service resilience, and the confirmed `NormalizeUrl` char overload fix.
- Uses the three-part version line without two-digit minor or patch segments.
