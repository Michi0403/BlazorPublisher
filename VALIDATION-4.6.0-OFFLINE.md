# PublisherStudio 4.6.0 — offline verification

- 21/21 JavaScript regression test files passed (`tests/js/*.test.cjs` and `*.test.mjs`).
- `node --check` passed for both modified runtime JavaScript files.
- Chromium headless browser fixture confirmed that programmatically queued mousedown, mousemove, mouseup draw a real visible stroke on an HTML canvas and the automation pointer appears.
- Browser fixture does not exercise the installed .NET app, live 1-Wire connection, interactive user permissions or the Windows desktop.
- No .NET/PowerShell build or GitHub access.
