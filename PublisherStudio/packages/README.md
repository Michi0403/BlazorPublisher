# LocalGPT WireProtocol package drop

For package-mode development, copy the official LocalGPT release asset `LocalGPT.WireProtocolVersion.2.0.1.nupkg` into this directory. The repository NuGet configuration adds this folder without clearing your normal NuGet or DevExpress feeds.

```powershell
dotnet restore .\src\PublisherStudio.Web\PublisherStudio.Web.csproj -p:UseLocalWireProtocolProject=false
dotnet build .\src\PublisherStudio.Web\PublisherStudio.Web.csproj -c Debug -p:UseLocalWireProtocolProject=false --no-restore
```

Normal source development defaults to the synchronized project under `src/LocalGPT.WireProtocolVersion`.
