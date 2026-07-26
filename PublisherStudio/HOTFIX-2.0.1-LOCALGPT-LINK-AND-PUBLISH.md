# PublisherStudio 2.0.1 LocalGPT link and publish hotfix

This source candidate aligns PublisherStudio with the repaired LocalGPT 2.0.1 organic 1-Wire transport and removes the fragile source-only protocol package from the normal build path. It is source-only because the delivery environment does not contain the .NET SDK or a licensed DevExpress asset-generation environment.

## Connection recovery

- Discovery accepts the compact 32 KiB LocalGPT UDP advertisement.
- The approved TCP `HelloAck` immediately triggers a `CapabilityRequest`.
- Complete capabilities, skills, UI features, and hardware are applied when the TCP response arrives.
- User/component cancellation is treated as a normal disconnect instead of a connection failure.
- The shared TCP message ceiling is 8 MiB.

## Publish recovery

- `PublisherStudio.Web` references the synchronized protocol source project directly for restore, debug, build, and RID-specific publish.
- The project reference strips application publish globals (`RuntimeIdentifier`, `SelfContained`, trimming/single-file/AOT flags) from the protocol class library.
- `Build-Release.ps1` packs the local protocol source without an application RID, then publishes the application and installer for the requested RID.
- The previously bundled source-only `.nupkg` was removed. The release script creates a normal package containing the compiled protocol DLL.

## Maintainer verification

From the `PublisherStudio` folder:

```powershell
Remove-Item .\src\PublisherStudio.Web\bin, .\src\PublisherStudio.Web\obj -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item .\src\LocalGPT.WireProtocolVersion\bin, .\src\LocalGPT.WireProtocolVersion\obj -Recurse -Force -ErrorAction SilentlyContinue

.\Prepare-DevExpressAssets.ps1
.\Build-Release.ps1 -Runtime win-x64
```

Other supported release values are `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, and `osx-arm64`.

Run LocalGPT first. PublisherStudio should show the discovered LocalGPT peer, connect over TCP, wait for approval in the LocalGPT frontend, and populate the shared capability directory after approval.
