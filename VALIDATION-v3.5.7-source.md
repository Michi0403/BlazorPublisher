# PublisherStudio 3.5.7 source validation

Source-only validation performed in the delivery environment; no `dotnet build`, `dotnet publish`, Windows setup execution, or native macOS packaging was run here.

Validated invariants:

- PublisherStudio project/setup/package and browser asset identities are 3.5.7.
- Blazor async-continuation audit passes across 80 maintained source files: 1,102 await tokens, 429 background `ConfigureAwait(false)` continuations, 619 renderer-affine `ConfigureAwait(true)` continuations, 49 configured await-using disposals, and 5 configured async streams.
- Component resilience and prerender interop audits pass across 2,687 component methods; 13 JavaScript-aware disposal methods remain attachment-gated.
- Service resilience audit passes across 1,382 ordinary service methods plus 3 iterator/yield service methods after consolidating file logging to one provider-owned sink.
- Story Editor `OnAfterRenderAsync` attaches its browser layout bridge once per visible surface and resets the attachment state when hidden.
- No active PublisherStudio installer source contains the removed transactional staging/whole-wrapper replacement workflow.
- Windows installer extraction targets `%LOCALAPPDATA%\PublisherStudio`, stops only the owned packaged runtime, and overlays the application/setup wrapper ZIPs into that canonical root.
- PublisherStudio release orchestration compiles the setup project before documentation/PDF/notarization work.
- Existing macOS productbuild/readback/signing/notarization and `server.json` ownership/rendezvous behavior remains present.
