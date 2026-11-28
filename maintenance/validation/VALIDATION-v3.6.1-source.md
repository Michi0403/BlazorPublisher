# PublisherStudio 3.6.1 source validation

Validation is source/static. The assistant did not use GitHub and did not run `dotnet build`, publish, setup execution, Apple signing/notarization, or PowerShell-only release steps.

Validated statically:

- Web and installer project versions, npm package identities, documentation identity and browser cache-busters are 3.6.1 and obey the one-digit minor/patch policy;
- setup operator input is enabled only for an interactive, non-redirected console and uses a background reader so ordinary setup output continues;
- the Matrix operator keeps meta controls available independently of ordinary shell forwarding and launches shell jobs with redirected output and `CreateNoWindow=true`;
- host-aware shell discovery covers Auto/zsh/bash/sh/pwsh/PowerShell/cmd where available;
- Ctrl+C and `:cancel` own one setup cancellation token used by downloads, retry delays, install/update stages and process waits, and cancellation is rethrown through generic resilience catches to the dedicated exit-130 path;
- setup-owned child processes and FFmpeg provisioning are registered for PID inspection/signaling and best-effort cleanup, while human operator shell jobs are classified separately;
- XML documentation quality passes for 6,383 direct C# declarations and 3,445 Razor declarations;
- async continuation validation remains unchanged at 80 source files, 1,102 await tokens, 429 `ConfigureAwait(false)`, 619 renderer-affine `ConfigureAwait(true)`, 49 configured await-using disposals and 5 configured async streams;
- component resilience passes for 2,687 component methods, prerender interop safety passes with 13 attachment-gated JavaScript-aware disposal methods, and service resilience passes for 1,382 service methods plus 3 iterator methods;
- application architecture, cross-platform boundaries, iterator policy and Panel Studio persistence audits pass;
- the logging baseline remains unchanged and the single shared background file-writer contract remains intact;
- the focused `build/audit_release_3_6_1.py` guard passes for Matrix shell/job/signal wiring, cancellation propagation, setup-child ownership, overlay deployment, logging baseline and prior 3.6.0 release protections;
- the overlay installer contract and the previous macOS entitlement/PDF-render protections remain present.

The user's Visual Studio/macOS `pwsh` builds remain the authoritative compiler and execution tests.
