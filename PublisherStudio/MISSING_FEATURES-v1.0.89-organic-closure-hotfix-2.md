# Missing features after PublisherStudio v1.0.89 organic source-closure hotfix 2

## Security and transports

- Actual 1-Wire signing/encryption and managed peer keys. SHA-256/CRC/error checking and encrypted-payload reservation are present; authenticated encryption is not yet enabled.
- UART, SPI and MQTT adapters. PublisherStudio currently uses the shared transport-neutral contract with TCP/UDP.
- Authenticated discovery/pairing for untrusted LANs.
- Chunked/binary media transfer beyond bounded inline/reference payloads.

## Organic UI and workflows

- Full visual audit of all legacy hard-coded colors across every PublisherStudio/DevExpress theme. The organic surfaces are theme-aware, but complete historical-page verification requires the owner browser/build environment.
- A generalized visual editor for binding arbitrary advertised organic skills to every ribbon/menu/context location. Current descriptors and connection-aware state are implemented; not every historical command has a dynamic binding entry yet.
- Persisted recurring screen-reader sessions across application restarts. Current sessions are bounded and non-stacking within the running process.
- Fully unattended OS-global mouse/keyboard control. Browser-gated, permission-controlled interaction remains intentional.

## Council/model scheduling

- Execution of local LLMs inside PublisherStudio itself. PublisherStudio advertises hardware and capabilities; LocalGPT remains the Council/model host.
- Automatic hardware benchmarking and proficiency scoring. Current descriptors carry CPU/GPU/token-road information but do not benchmark drivers/models automatically.

## Architecture maintenance

- Additional bounded subnamespacing of large historical services after compiler-backed dependency mapping. The current hotfix avoids a broad move that could break controller/frontend/backend wiring.
- Automated generation/synchronization of the PublisherStudio protocol mirror from a released LocalGPT protocol artifact. The two delivered source trees are byte-identical now; release-feed automation remains open.

## Owner-environment validation

- Native Debug/Release build with .NET 10 and licensed DevExpress packages.
- Real browser user-gesture permission tests for screen capture, recurring capture and input execution.
- Multi-GPU and CPU/GPU concurrent Council tests against real Ollama/LM Studio processes through LocalGPT.
