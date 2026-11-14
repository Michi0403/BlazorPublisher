# Validation — PublisherStudio 4.7.9 family protocol compatibility

The following source audit is wired through `build/Assert-PowerShellCompatibility.ps1`:

```powershell
pwsh ./build/Assert-FamilyProtocolCompatibility.ps1
```

It verifies the common one-wire options factory, `OrganicPluginProtocolCodec` use, established HTTP profile response, codec-aware live capability parsing and Council project context, and MVC enum configuration. This is **not** an executed cross-product protocol round trip.

Manual matrix: LocalGPT 7.1.5 + PublisherStudio 4.7.9 across Windows, macOS, Linux; existing/updated peers; 1-Wire TCP/UDP and HTTP; enum-string and numeric legacy inputs; MFA, pairing, Council WorkResult/ApprovalRequired, HTTP profile casing, MVC JS clients, Blazor SignalR circuits and MCP JSON-RPC on LocalGPT.
