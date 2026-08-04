# BlazorPublisher InstallerConsole

`PublisherStudio.Setup.exe` is the entry point of the Git-free, self-contained single-file release installer. Keep it in the published setup directory so the stable launchers, icon, protocol evidence and release manifest remain available.

A double-click with no arguments downloads the latest application ZIP from `Michi0403/BlazorPublisher`, installs it to `%LOCALAPPDATA%\Programs\BlazorPublisher`, generates `Install.cmd`, `Update.cmd`, `Start.cmd`, and `Uninstall.cmd`, creates a Start Menu folder, starts the web host, and opens its detected loopback URL.

Expected Windows x64 release assets from `Build-Release.ps1`:

```text
winx64.zip
setupwinx64.zip
```

The application ZIP contains the complete multi-file `PublisherStudio.Web` publish output. The setup ZIP contains the standalone setup executable plus launchers, repair executable, icon, protocol evidence and release manifest. Extract the setup ZIP as a unit so launcher repair remains available.

```powershell
.\PublisherStudio.Setup.exe --install-blazorpublisher
.\PublisherStudio.Setup.exe --update-blazorpublisher
.\PublisherStudio.Setup.exe --start-blazorpublisher
.\PublisherStudio.Setup.exe --uninstall --force-delete
```

For the normal Windows workflow, run `Install.cmd`, `Update.cmd`, `Start.cmd`, or `Uninstall.cmd` from the extracted setup folder.

## Network and FFmpeg resilience

Release assets are downloaded to resumable `.part` files and validated before the installed application is changed. Re-running setup reuses a complete validated ZIP or resumes an incomplete transfer.

During a normal install/update, setup also checks FFmpeg. On Windows it uses `winget --source winget` for `Gyan.FFmpeg`, prints a heartbeat while the package manager is busy, retries once through the package-manager cache, and stops FFmpeg provisioning after a 15-minute total budget. FFmpeg failure is non-fatal; use `--skip-ffmpeg` to omit the check or `--install-ffmpeg` to retry it separately later.
