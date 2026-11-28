# PublisherStudio 3.4.0 source validation

This source package was prepared without GitHub access and without running a .NET build, matching the supplied-source workflow.

## Static checks performed

- Version references were checked for PublisherStudio 3.4.0, including the required 3.3.9 -> 3.4.0 rollover.
- Notarization orchestration was checked against the LocalGPT companion implementation for equivalent recovery semantics.
- Transient Apple/network failures are retried without terminating the release; genuine terminal Apple statuses remain fatal.
- Recoverable Keychain/profile conditions pause/retry; upload state is persisted before polling; Apple `In Progress` no longer fails at the local checkpoint.
- Durable PDF reuse rejects incomplete files without an `%%EOF` marker.
- Adaptive browser PDF chunking remains the default and complete chunks are now durable/reusable.
- Existing PublisherStudio InteractiveServer/prerender, architecture, cross-platform, async, service, and component guards remain in place; no application render modes were changed.
- Final source audit: passed.
- Application architecture audit: passed.
- Cross-platform boundary audit: passed (60 checks).
- Service resilience audit: passed (1376 service methods plus 3 iterator/yield methods).
- Component resilience audit: passed (2687 component methods).
- Iterator exception-policy audit: passed (3 iterator/yield methods).
- Async continuation audit: passed (80 source files, 1102 await tokens).
- Prerender JavaScript interop safety audit: passed (2687 component methods; 13 attachment-gated JS-aware disposal methods).
- Panel Studio persistence audit: passed.

## Not performed

- No `dotnet build`, `dotnet test`, notarization submission, code signing, Homebrew installation, or browser PDF render was executed in this environment.
