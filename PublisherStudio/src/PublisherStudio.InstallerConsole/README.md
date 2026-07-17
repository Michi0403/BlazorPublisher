# BlazorPublisher InstallerConsole

`PublisherStudio.Setup.exe` installs and controls BlazorPublisher without requiring Git.

A double-click with no arguments reads the latest release from `Michi0403/BlazorPublisher`, selects the application ZIP for the current operating system and CPU architecture, installs it below `%LOCALAPPDATA%\BlazorPublisher`, creates the four command launchers and Start Menu entries, starts the web host, and opens its reported loopback URL.

The installed layout is:

```text
%LOCALAPPDATA%\BlazorPublisher\
├─ Application\
├─ Setup\PublisherStudio.Setup.exe
├─ BlazorPublisher.ico
├─ Install.cmd
├─ Update.cmd
├─ Start.cmd
└─ Uninstall.cmd
```

The command files search recursively for `PublisherStudio.Setup.exe`, so moving the setup into its `Setup` subdirectory does not break Start, Update, Repair, or Uninstall. Optional arguments are forwarded:

```powershell
Start.cmd --port 58071
PublisherStudio.Setup.exe --start --port 58071
```

The setup is published as a self-contained single file. The application publish remains a normal self-contained ASP.NET Core payload because its static web assets belong beside the executable.
