# PublisherStudio 2.0.1 remaining runtime verification

## Next maintainer checks

1. Rebuild `PublisherStudio.sln`; confirm `OrganicPlugins.razor` compiles with `JsonValueKind` resolved.
2. Start from Visual Studio on `127.0.0.1:5198` and verify no Kestrel endpoint override occurs unless an explicit installer/CLI port is supplied.
3. Open `/organic-plugins`, connect to LocalGPT 2.0.1 and verify the UI waits for LocalGPT frontend approval before enabling capabilities.
4. Request a Story Editor Council proposal, close/reopen the editor, refresh proposals and explicitly insert at the caret.
5. Request a screenshot; verify PublisherStudio confirmation occurs before the browser asks for current-session capture permission.
6. Exercise Panel/Div movement, dual video range boundaries, clickable sequence parts, flex liveboards and exported hover/tooltips/signal arrows.
7. Switch English/German from the application localization settings and verify file overrides persist.

## Deliberately still open

- Native build/runtime acceptance across all supported platforms and licensed DevExpress packages.
- Real protocol signing/encryption/key lifecycle and authenticated discovery.
- UART/SPI/MQTT transports.
- Cross-origin/DRM screen capture and OS-global input automation.
- Native OpenSCAD process execution and complete visual graph builder.
- Full LSP/IDE-grade editor, arbitrary exporter assembly discovery and complete historic-string translation.
- Chunked multi-gigabyte media transfer over 1-Wire.
