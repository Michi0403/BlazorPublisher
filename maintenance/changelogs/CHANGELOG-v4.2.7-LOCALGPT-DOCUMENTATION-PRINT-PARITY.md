# PublisherStudio 4.2.7 — LocalGPT documentation printer parity

## Incident

PublisherStudio 4.2.4 through 4.2.6 compiled the application and passed architecture, XML-documentation and DocFX HTML preflight, but could wait indefinitely at the first Edge PDF cover/index operation. The new 4.2.6 one-page browser probe passed and therefore did not exercise the failing real PDF output location or the actual print-book. LocalGPT 6.0.8 successfully uses an established browser/managed-merge workflow on the same Windows machine.

## Implementation

- Port the actual LocalGPT 6.0.8 `Build-Documentation.ps1` browser-print and durable-chunk implementation into PublisherStudio's existing PowerShell documentation owner (adapted only for PublisherStudio function names and its .NET DLL tool invocation). The PublisherStudio documentation content, DocFX inputs, styling, authoring contracts and mandatory PDF requirement remain unchanged.
- Restore LocalGPT's Chromium/Edge print flags, including `--virtual-time-budget=180000`; remove the speculative single-page browser probe, preferred-profile sorting, and special 48-entry front-matter job flow added in 4.2.5/4.2.6.
- Restore short isolated browser-profile/cache paths (`bp/p<guid>/c`) and LocalGPT's explicit Windows browser-output/profile path checks, preventing silent native path overflow before a print attempt starts.
- Use LocalGPT's same durable cover/index and paged-body cache naming and its ordered UTF-8 `pdf-merge-inputs.txt` manifest with `pdf-merge --input-list`. Invoke the authoritative `LocalGPT.ReleasePackaging` tool through `dotnet` only when the resolved tool is a DLL. Require the runtime tool to advertise the `pdf-merge-input-list` capability rather than relying on a filename or NuGet version alone.
- Restore LocalGPT's per-cover and per-body-chunk start, completion and duration lines. Add a concise 5-second heartbeat during an individual browser print and explicit `completed/total PDF parts` counters so a pending print remains observable in the Visual Studio Output console. Diagnostic pipe reads remain bounded after forced browser termination.
- Preserve the existing final PDF integrity requirement, automatic DocFX documentation build, documentation publication and all PowerShell architecture guards; do not silently skip a failed PDF or claim an incomplete document is complete.

## Scope

PublisherStudio only. No changes to LocalGPT, application services, localization catalogs, Editor/RichEdit behavior, or managed PDF merge implementation. Existing NuGet package `LocalGPT.ReleasePackaging` version 1.1.2 is retained.

## Runtime verification boundary

This source release was inspected against LocalGPT's implementation and tested for packaging and source invariants. Windows PowerShell 5.1, Microsoft Edge, and the full .NET 10/DocFX pipeline are not available in the authoring environment, so success on the developer's Windows host remains to be verified. A first-pass Chromium test in this container was inconclusive because even a trivial PDF print did not complete.
