# GitHub release

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

The setup assets are runtime-specific, for example
`PublisherStudio.Setup-winx64.exe`. The Windows x64 build additionally creates the
public bootstrap name `PublisherStudio.Setup.exe`.

Upload the application ZIP and matching setup executable to the same GitHub
release. The installer reads only `releases/latest`; it does not fall back to an
older release or another architecture.

The application and setup are published self-contained and single-file. The setup
publish also keeps `Install.cmd`, `Update.cmd`, `Start.cmd`, `Uninstall.cmd`, and the
Publisher icon in its local publish directory. After installation the structure is:

```text
%LOCALAPPDATA%\BlazorPublisher\
├─ Application\
├─ Setup\PublisherStudio.Setup.exe
├─ BlazorPublisher.ico
├─ Install.cmd
├─ Update.cmd
├─ Start.cmd
├─ Uninstall.cmd
└─ installation.json
```

The command files resolve the setup executable and let the installed setup infer
the installation root from its own `Setup\` directory. They forward `%*` and
preserve the `--port` startup option, so no trailing-backslash path is passed by
the default command flow. `ResolveLaunch` prefers `Application\` and then searches the
entire installation tree, so an existing compatible extracted layout remains
startable.
