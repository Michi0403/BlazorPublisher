# GitHub beta release

Build one runtime at a time:

```powershell
.\Build-Release.ps1 -Runtime win-x64
```

The compact application asset names are:

- `winx64.zip`
- `winarm64.zip`
- `linx64.zip`
- `linarm64.zip`
- `macosx64.zip`
- `macosarm64.zip`

The script also creates a runtime-specific standalone setup executable. For `win-x64` it additionally creates the public generic asset `PublisherStudio.Setup.exe`.

The installer reads only `releases/latest`, then matches both the current platform and CPU architecture. It does not scan older releases and does not fall back to another operating system or architecture.

The application is installed below `%LOCALAPPDATA%\BlazorPublisher\Application`; the independently runnable single-file installer is stored below `%LOCALAPPDATA%\BlazorPublisher\Setup`. `Install.cmd`, `Update.cmd`, `Start.cmd`, and `Uninstall.cmd` remain in the installation root and locate the setup recursively.

`Build-Release.ps1` rejects:

- incomplete self-contained application payloads,
- setup output containing `PublisherStudio.Setup.dll`, `.deps.json`, or `.runtimeconfig.json` sidecars,
- and suspiciously small setup apphosts.

The setup also runs an isolated `--help` self-test before it replaces the installed installer. This prevents the previous broken state where `PublisherStudio.Setup.exe` was copied without its required `PublisherStudio.Setup.dll`.

All application and Start Menu icons use `assets\PublisherStudio.ico`. The shortcut copy is named `BlazorPublisher.ico` to avoid reusing the old cached llama icon path.
