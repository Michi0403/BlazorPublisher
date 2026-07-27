# LocalGPT protocol package cache

PublisherStudio no longer contains or revisions a second `LocalGPT.WireProtocolVersion` source project. It consumes only the authoritative DLL-backed NuGet package built by LocalGPT.

`Build-LocalDevelopment.ps1`, `Build-Release.ps1`, and Visual Studio restore prepare this cache before restoring `PublisherStudio.Web`. They search, in order:

1. an explicitly supplied LocalGPT repository (`-LocalGptRepository`),
2. the `LOCALGPT_REPOSITORY` environment variable,
3. the per-user LocalGPT shared NuGet cache,
4. the official LocalGPT GitHub release asset.

Downloaded `.nupkg` files are ignored by Git and must not be committed to BlazorPublisher. The official asset name is:

```text
LocalGPT.WireProtocolVersion.2.0.1.nupkg
```
