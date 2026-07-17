# BlazorPublisher InstallerConsole

`PublisherStudio.Setup.exe` is the self-contained bootstrapper for BlazorPublisher.
It follows the same release/bootstrap pattern as the LocalGPT installer, with only
Publisher-specific names and actions.

A double-click with no arguments:

1. reads `Michi0403/BlazorPublisher/releases/latest`;
2. selects the application ZIP for the current OS and CPU architecture;
3. installs to `%LOCALAPPDATA%\BlazorPublisher`;
4. stores the app in `Application\` and the bootstrapper in `Setup\`;
5. creates `Install.cmd`, `Update.cmd`, `Start.cmd`, and `Uninstall.cmd`;
6. creates the four Start Menu entries using `BlazorPublisher.ico`;
7. starts `PublisherStudio.Web` on the requested port (`0` means an available port);
8. opens the URL reported by the running application.

Supported application asset names are `winx64.zip`, `winarm64.zip`, `linx64.zip`,
`linarm64.zip`, `macosx64.zip`, and `macosarm64.zip`.

```powershell
PublisherStudio.Setup.exe --install --start --port 0
PublisherStudio.Setup.exe --update --start --port 0
PublisherStudio.Setup.exe --start --port 0
PublisherStudio.Setup.exe --uninstall --force
```

The installed command files discover `PublisherStudio.Setup.exe` recursively and
forward additional arguments. They do not depend on a hard-coded absolute path.
