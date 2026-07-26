# PublisherStudio v1.0.93 — remaining work

- Native `.NET 10` and DevExpress compile/startup validation must be performed on the user's Windows development machine.
- Real cryptographic LocalGPT identity, certificate/key management, signed link grants, and encrypted payloads remain future work.
- UART, SPI, and MQTT transports are not implemented; current operation is TCP plus UDP discovery.
- Browser screen capture still requires the browser's own user-gesture permission. The PublisherStudio approval workflow cannot and should not bypass browser security prompts.
- Very large binary media should move through referenced files/streams or a future chunked transport rather than one JSON envelope, even though offline limits are maximized.
- Automatic insertion of AI-generated text remains intentionally reviewable. The user must accept/place a proposal in the target editor; richer direct editor adapters can be added per component.
- Complete visual regression testing across every DevExpress/Bootstrap theme, browser engine, export combination, and localization is broader than the included source-contract suite.
- Certificate-backed link revocation across application reinstalls remains future work. The current user can disconnect, hide, deny, or remove per-peer permissions from the frontend.
