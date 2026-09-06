# LocalGPT package cache

PublisherStudio does not own duplicate source for LocalGPT shared contracts or release tooling.

- `LocalGPT.WireProtocolVersion` is consumed as the authoritative DLL-backed NuGet package produced by LocalGPT.
- `LocalGPT.ReleasePackaging` is consumed as the authoritative .NET tool package produced by LocalGPT. It provides native release helpers plus the managed PDF merge/optimization commands used by documentation builds.

`build/Ensure-ReleasePackagingPackage.ps1` searches an explicitly configured LocalGPT repository, `LOCALGPT_REPOSITORY`, a sibling `LocalGPT` checkout, the per-user `LocalGPT/NuGet` cache, and finally the matching asset from the latest LocalGPT release. It never builds `LocalGPT.ReleasePackaging` source inside PublisherStudio.

The package cache is transient build input. Generated/downloaded `.nupkg` files remain ignored by Git.
