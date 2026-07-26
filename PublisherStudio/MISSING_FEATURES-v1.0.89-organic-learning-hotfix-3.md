# PublisherStudio v1.0.89 — Missing Features after Organic Learning Hotfix 3

## Build validation still required

- Native Windows `dotnet build`, DevExpress and installer execution were not available in the packaging environment. The complete JavaScript/source-contract suite passes; Michael's Visual Studio build remains authoritative.

## Text proposal workflow

- Generated text is deliberately review-only. Automatic insertion into the selected publication component is not implemented because the final target selection and mutation must remain a user action.
- If several compatible LocalGPT/PublisherStudio peers are connected, explicit peer routing UI is still required.
- A richer document/component picker can replace the current free-text target field in a later iteration.

## One-wire production hardening

- Authenticated discovery, real signature/encryption key management, chunked large-media transfer and UART/SPI/MQTT transports remain future work.
- Long-running work survives in the current process/store model; distributed multi-host recovery is not implemented.

## UI and accessibility

- The Organic Plugins page is themed through its existing surface styles, but a complete cross-theme/accessibility audit of every legacy PublisherStudio surface remains open.
- A consolidated global approval work bar integrated into every Studio workspace, rather than the Organic Plugins management page and existing queues, can be expanded further.
