# Installer and updates

PublisherStudio uses the same simple deployment shape as LocalGPT. The setup is a one-click operation and keeps every runtime beneath one product-owned AppData folder.

## Installed layout

The installation root is:

```
%LOCALAPPDATA%\PublisherStudio
```

Application and setup releases keep their runtime wrappers inside that root:

```
PublisherStudio\
  winx64\
    PublisherStudio.Web.exe
  setupwinx64\
    PublisherStudio.Setup.exe
    Install.cmd
    Update.cmd
    Start.cmd
```

The wrapper names vary by operating system and architecture, but the product root does not move to `Programs` and does not reuse the former `BlazorPublisher` application folder.

## One-click behavior

Double-clicking `PublisherStudio.Setup.exe` performs the normal install/update path:

1. download the matching application release;
2. extract it into `%LOCALAPPDATA%\PublisherStudio`;
3. download and refresh the matching setup release;
4. ensure FFmpeg is available;
5. create the required Desktop and Start Menu shortcuts;
6. start PublisherStudio on preferred port `58071`, falling back to an OS-assigned loopback port if Windows rejects that port.

No command-line argument is required.
The setup queries the newest published GitHub release and requires exact runtime assets. On Windows x64 those assets are `winx64.zip` and `setupwinx64.zip`; a missing pair is an error rather than permission to install another architecture.

## Updates

Normal updates stage and validate both the application and setup archives before replacing the architecture-specific runtime/setup wrapper directories as one rollback-capable transaction. The surrounding `%LOCALAPPDATA%\PublisherStudio` user-data root is preserved. The root is not deleted unless `--force-delete` is explicitly supplied.

When setup is started from its installed setup folder, it continues from a temporary copy before extraction. This lets the installed setup executable and launchers be replaced during the same one-click update.

## Required shortcuts

Both Desktop and Start Menu receive these entries:

- **PublisherStudio Install**;
- **PublisherStudio Update**;
- **PublisherStudio Start**;
- **PublisherStudio Folder**.

The Install, Update, and Start entries call the checked-in command files from the matching setup runtime folder. The Folder entry opens `%LOCALAPPDATA%\PublisherStudio` directly.

## Compatibility

Former `--*-blazorpublisher` command-line names remain accepted so an older launcher can invoke the repaired setup. New launchers use the PublisherStudio names.

## Per-user storage and detected path layout

PublisherStudio keeps writable application state in the current user's application-data directory by default. The Windows compatibility root remains `%LOCALAPPDATA%\PublisherStudio`; installer and application state use the same product root. macOS uses the current user's Application Support location, and Linux uses the host user data location (`XDG_DATA_HOME` when configured, otherwise the normal `~/.local/share` fallback).

On first boot and subsequent starts PublisherStudio creates the required user-owned directories and writes `Configuration/path-layout.json`. The Help page exposes the detected user root, user configuration file, and path report so support flows can work from the actual machine layout.

Portable application directories and common system-wide roots remain valid discovery/install locations but are not silently selected for mutable user configuration. FFmpeg and other tool discovery remains independent: explicit paths, portable tool folders, per-user package locations, Homebrew/Linuxbrew, normal Unix system paths, and Windows package-manager/system locations can all be discovered without moving PublisherStudio's own state out of the user's application-data root.
