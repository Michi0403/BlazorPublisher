# PublisherStudio v1.0.34 release

See `CHANGELOG-v1.0.34.md`.

# GitHub beta release

Run:

```powershell
.\Build-Release.ps1 -Runtime win-x64
```

The release script requires the .NET 10 SDK, Node.js 20+/npm on the build machine, access to the DevExpress NuGet and npm packages, and a valid DevExpress license. Node.js is not a runtime prerequisite for installed releases. It calls `Prepare-SpreadsheetAssets.ps1`, which finds standard Node.js and NVM installations, restores the Spreadsheet browser packages, copies the required Spreadsheet/DevExtreme/jQuery files into the local `wwwroot/vendor` tree, verifies them, and then publishes the self-contained application and setup launcher.

Upload the generated runtime and setup assets from `artifacts/release` to the same GitHub release. For Windows x64 these are:

- `winx64.zip`
- `setupwinx64.zip`
- `PublisherStudio.Setup.exe`

Keep the runtime token in every application ZIP. As in the LocalGPT installer, both the platform token and processor token must match. `winx64.zip`, `winarm64.zip`, `linx64.zip`, `linarm64.zip`, `macosx64.zip`, and `macosarm64.zip` identify the supported runtime packages. Do not rename a runtime-specific archive to an unrelated generic name.

Users download and run the setup executable. It scans the newest published GitHub releases, including pre-releases, selects the matching application ZIP, installs it into `%LOCALAPPDATA%\Programs\BlazorPublisher`, creates Start Menu commands for Start, Install/Repair, Update, and Uninstall, and opens the application.

`Build-Release.ps1` validates the publish output before creating ZIP files. A self-contained Windows package must contain `PublisherStudio.Web.exe`, `hostfxr.dll`, and `hostpolicy.dll`; incomplete output is stopped before upload. The PublisherStudio icon is embedded into the application and setup executable and copied into the payload.

The source archive intentionally excludes `node_modules` and generated proprietary DevExpress browser assets. Release builds restore them from the licensed feeds. The resulting published application serves all Spreadsheet resources from its own loopback ASP.NET Core server and does not need a CDN or internet connection at runtime.
