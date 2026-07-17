# BlazorPublisher InstallerConsole

`PublisherStudio.Setup.exe` is a Git-free release installer for Windows.

A double-click with no arguments downloads the latest application ZIP from `Michi0403/BlazorPublisher`, installs it to `%LOCALAPPDATA%\Programs\BlazorPublisher`, generates `Install.cmd`, `Update.cmd`, `Start.cmd`, and `Uninstall.cmd`, creates a Start Menu folder, starts the web host, and opens its detected loopback URL.

Expected release asset name:

```text
BlazorPublisher-win-x64.zip
```

The ZIP must contain the output of `dotnet publish` for `PublisherStudio.Web`.

```powershell
PublisherStudio.Setup.exe --install
PublisherStudio.Setup.exe --update
PublisherStudio.Setup.exe --start
PublisherStudio.Setup.exe --uninstall --force
```
