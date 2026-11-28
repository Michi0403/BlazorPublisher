# PublisherStudio 4.4.5 — Native DevExtreme Chat discoverability and independent AI provider

Baseline: PublisherStudio 4.4.4. Source-only release; not build verified.

- Restores distinct **Chat** and **AI Chat** entries in Insert → Professional components. The ordinary Chat remains provider independent; the AI entry opens the existing DevExtreme Chat Studio to configure its AI mode. The quick AI Chat insertion button remains visible without LocalGPT.
- Adds `OpenAiServer` as an optional `PublicationChatAiMode`. The browser uses the same-origin PublisherStudio AI endpoint; the host sends OpenAI chat-completions requests with `IHttpClientFactory` and stores no key inside the publication, export or browser.
- The inserted AI Chat defaults to LocalGPT when linked; otherwise host-configured OpenAI; otherwise ordinary Chat. Authors can override through Component Studio.
- Host provisioning: `PUBLISHERSTUDIO_OPENAI_API_KEY` and `PUBLISHERSTUDIO_OPENAI_MODEL`; optional `PUBLISHERSTUDIO_OPENAI_CHAT_COMPLETIONS_URL` (HTTPS or loopback HTTP only). Credentials must remain host-side. No user-supplied arbitrary endpoint is accepted via chat requests.
- Existing LocalGPT Council execution and its separate approval boundary remain unchanged.
- JavaScript diagnostics manifest updated for reviewed component runtime changes.

## Limitations

- DevExtreme jQuery `dxChat` and DevExpress Blazor `DxAIChat` are distinct components. This release exposes AI modes for the existing exportable **DevExtreme** component; it **does not add a native Blazor `DxAIChat` component** to the publication canvas. That needs its own Blazor runtime host and distinct standalone export behavior, plus licensed-package compiler verification.
- Direct OpenAI works only with the configured running PublisherStudio host. Exported static websites retain local DevExtreme Chat, but an AI reply requires a separately deployed secure backend, never a key embedded in HTML.
- Responses are completed messages, not token-streamed.
- .NET build, full PowerShell architecture gates, and a live real-provider call have not run in this environment.
