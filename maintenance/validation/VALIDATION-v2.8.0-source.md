# PublisherStudio 2.8.0 source validation

- Version policy: PublisherStudio.Web and PublisherStudio.InstallerConsole set to 2.8.0.
- Browser JavaScript: `mediaStudioInterop.js` parses with `node --check`.
- Configuration/localization JSON: parsed successfully after adaptive policy and translation additions.
- JavaScript diagnostics manifest: refreshed for the modified Media Studio interop file.
- Adaptive recording: actual browser track settings drive automatic bitrate/codec selection; codec-specific video plus track-derived audio MediaCapabilities payloads are used where supported; no fixed 4K capture requirement is introduced.
- Capture-pressure hardening: recording Blob is retained before metadata enrichment; capture tracks are released before delayed poster analysis; poster canvas has a configurable pixel budget and does not alter source video bytes.
- Streaming: adaptive settings are serialized with publications, forwarded to the media host, and applied independently to video/audio recommendations for provider and LAN outputs.
- Configuration: provider-specific knowledge can be disabled and is not required for runtime policy validity.
- Render-mode regression: explicit `@rendermode` file count remains 5, including the same 3 explicit InteractiveServer boundaries as 2.7.9.
- No dotnet restore/build/publish/pack was executed.
