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

## Network and FFmpeg resilience

Release assets are downloaded to resumable `.part` files and validated before the installed application is changed. Re-running setup reuses a complete validated ZIP or resumes an incomplete transfer.

During a normal install/update, setup also checks FFmpeg. On Windows it uses `winget --source winget` for `Gyan.FFmpeg`, prints a heartbeat while the package manager is busy, retries once through the package-manager cache, and stops FFmpeg provisioning after a 15-minute total budget. FFmpeg failure is non-fatal; use `--skip-ffmpeg` to omit the check or `--install-ffmpeg` to retry it separately later.
