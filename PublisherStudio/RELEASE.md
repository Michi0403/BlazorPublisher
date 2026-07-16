# GitHub beta release

Run:

```powershell
.\Build-Release.ps1 -Runtime win-x64
```

Upload these two generated files from `artifacts/release` to the same GitHub release:

- `BlazorPublisher-win-x64.zip`
- `PublisherStudio.Setup-win-x64.exe`

Users download and double-click the setup EXE. The setup executable scans the newest published GitHub releases (including pre-releases), selects the matching application ZIP, installs it into `%LOCALAPPDATA%\Programs\BlazorPublisher`, creates Start Menu commands for Start, Install/Repair, Update, Uninstall, and opens the application.

The DevExpress package source and license must already be available on the build machine when the release assets are created.
