# PublisherStudio 2.0.0 remaining and externally verifiable work

## Requires maintainer compilation/runtime evidence

- Run Debug and Release builds with the licensed DevExpress packages and exact .NET 10 SDK.
- Start from Visual Studio, installer and each supported runtime package and verify endpoint/port ownership.
- Pair with LocalGPT 2.0.0, approve on both frontends where configured, and exercise Story Editor text proposals, screenshots, screen-reader sessions and input commands.
- Confirm German/English and additional file-localized strings throughout production publishing/export flows.

## Deliberately open

- Real 1-Wire signing/encryption/key lifecycle and authenticated discovery.
- UART/SPI/MQTT transport implementations.
- Cross-origin or DRM-protected screen/media capture, which remains constrained by browser security.
- Operating-system-global mouse/keyboard automation. Current hands functions are browser-scoped and frontend-confirmed.
- Native OpenSCAD process invocation and exact geometry export.
- Complete visual OpenSCAD graph builder.
- Full LSP/IDE-grade source editor.
- Runtime assembly plugin discovery for arbitrary exporters.
- Complete translation of every historic literal and every third-party UI surface.
- Full device matrix acceptance for pen/touch/gamepad and every browser.
- Multi-gigabyte chunked media transport over 1-Wire; use project/file references and bounded results until that protocol extension exists.

## Maintainer debug sequence

1. Restore `PublisherStudio.sln`; verify NuGet resolves `packages/LocalGPT.WireProtocolVersion.2.0.0.nupkg` for offline source debugging.
2. Run `Build-Release.ps1`; verify it downloads the authoritative LocalGPT package unless `-UseBundledWireProtocolPackage` was deliberately supplied.
3. Start on `127.0.0.1:5198`, open `/localization`, load/save overrides and switch culture.
4. Open `/organic-plugins`, connect to LocalGPT and confirm the UI remains waiting until LocalGPT approves the link.
5. In Story Editor, request a story, close/reopen the editor, refresh proposals and explicitly insert one at the caret.
6. Request a screenshot and verify PublisherStudio approval occurs before the browser asks for current-session capture permission.
7. Export an interactive publication and verify hover, tooltips, signal arrows and liveboard layout.
