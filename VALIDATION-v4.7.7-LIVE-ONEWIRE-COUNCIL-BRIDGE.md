# PublisherStudio 4.7.7 — 1-Wire / AI component acceptance

## Scope and limitations

Static source review only in the generating environment. No .NET/PowerShell build, browser automation, live signed 1-Wire link, or Windows/macOS/Linux runtime test was performed here.

1. With LocalGPT 7.1.3 and PublisherStudio 4.7.7, pair once, then invoke a PublisherStudio AI component with Ollama model(s) installed in LocalGPT but not assigned in AICore. Verify `CapabilityRequest`/`CapabilityResponse` precedes Council dispatch and that successful `WorkResult` produces visible AI content.
2. Install/remove an Ollama model without disconnecting PublisherStudio; retry an AI component and verify LocalGPT goal planning re-discovers models without changing explicit model selections.
3. Change LocalGPT 1-Wire permission or disable `council.run` after pairing; verify the refreshed capability check blocks the request with an actionable error, not an empty result.
4. Force a stale exact Council team binding. Verify PublisherStudio displays LocalGPT's `AssignedModelUnavailable` error; apply any team repair only after local human approval through LocalGPT's Steward.
5. Use a valid Council result with `Status=Completed` and verify normal output; a failed/cancelled `WorkResult` must never produce `LocalGPT returned an empty Council result` as its primary cause.
6. Inspect the HTML rendered by the two `DxComboBox` inputs; their generated IDs must match `publisher-media-team` / `publisher-object-team`. Run keyboard and accessibility checks.
7. Check that 1-Wire MFA, signed/encrypted transport, local/discovery endpoints, installer and updater, DevExpress pages, tab automation, and Media Studio capture still work on Windows, macOS and Linux. Tab automation's command execution was **not** repaired by this change and needs separate evidence.
