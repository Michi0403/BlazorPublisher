# GitHub beta release

Run:

```powershell
.\Build-Release.ps1 -Runtime win-x64
```

Upload these two generated files from `artifacts/release` to the same GitHub release:

- `BlazorPublisher-win-x64.zip`
- `PublisherStudio.Setup-win-x64.exe`

Keep the runtime name in every application ZIP. As in the LocalGPT installer, both
the platform token and processor token must match. Names such as
`BlazorPublisher-win-x64.zip` and `winx64.zip` are accepted; `linarm64.zip`,
`winarm64.zip`, and generic unrelated ZIP files are not used on Windows x64. The
installer also verifies the runtime recorded inside `PublisherStudio.Web.deps.json`
before replacing an existing installation. Do not rename a runtime-specific archive
to a generic name.

Users download and double-click the setup EXE. The setup executable scans the newest published GitHub releases (including pre-releases), selects the matching application ZIP, installs it into `%LOCALAPPDATA%\Programs\BlazorPublisher`, creates Start Menu commands for Start, Install/Repair, Update, Uninstall, and opens the application.

`Build-Release.ps1` now validates the publish output before creating the ZIP. A
self-contained Windows package must contain `PublisherStudio.Web.exe`,
`hostfxr.dll`, and `hostpolicy.dll`; a broken package is stopped before upload. The
PublisherStudio icon is embedded into the app and setup executable and copied into
the payload so every Start Menu entry uses the product icon instead of an arbitrary
third-party `.ico` file.

The DevExpress package source and license must already be available on the build machine when the release assets are created.
