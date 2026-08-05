# PublisherStudio setup

`PublisherStudio.Setup.exe` is the self-contained setup entry point. Its deployment contract intentionally matches LocalGPT, with PublisherStudio names and without Ollama or learning-base actions.

## One-click behavior

Double-clicking setup with no command-line arguments:

1. downloads the release matching the current operating system and architecture;
2. extracts the application ZIP into `%LOCALAPPDATA%\PublisherStudio`;
3. extracts the setup ZIP into the same root;
4. checks FFmpeg;
5. creates the required Desktop and Start Menu entries;
6. starts PublisherStudio on port `58071`.

The installation keeps the runtime wrappers from the release ZIP. A Windows x64 installation therefore contains:

```text
%LOCALAPPDATA%\PublisherStudio\
  winx64\
    PublisherStudio.Web.exe
  setupwinx64\
    PublisherStudio.Setup.exe
    Install.cmd
    Update.cmd
    Start.cmd
```

## Maintained actions

```powershell
.\PublisherStudio.Setup.exe --install-publisherstudio --start-publisherstudio --shortcuts
.\PublisherStudio.Setup.exe --update-publisherstudio --start-publisherstudio --shortcuts
.\PublisherStudio.Setup.exe --start-publisherstudio
```

The three command files above are the maintained launchers. Shortcut provisioning creates **Install**, **Update**, **Start**, and **Folder** entries on both Desktop and Start Menu. Older `--*-blazorpublisher` switches remain accepted only so an existing shortcut can reach the repaired setup.

## Update safety

Install and update extract over the existing PublisherStudio root and do not delete it unless `--force-delete` is explicitly supplied. When setup runs from its installed wrapper, it starts a temporary copy first so the setup executable and command files can be refreshed without changing the shortcut path.

Release assets keep the LocalGPT wrapper shape, for example `winx64.zip` and `setupwinx64.zip`.
