# Installer and updates

PublisherStudio uses the same simple deployment shape as LocalGPT. The setup is a one-click operation and keeps every runtime beneath one product-owned AppData folder.

## Installed layout

The installation root is:

```text
%LOCALAPPDATA%\PublisherStudio
```

Application and setup releases keep their runtime wrappers inside that root:

```text
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

1. resolve and download the exact matching application and setup release assets;
2. validate both ZIP wrappers and required executables before changing the installation;
3. extract both archives into `%LOCALAPPDATA%\PublisherStudio`;
4. ensure FFmpeg is available;
5. create the required Desktop and Start Menu shortcuts;
6. start PublisherStudio on port `58071`.

No command-line argument is required.
The setup first uses GitHub's exact `releases/latest/download` asset URL and falls back to the release API when necessary. It requires exact runtime assets. On Windows x64 those assets are `winx64.zip` and `setupwinx64.zip`; a missing pair is an error rather than permission to install another architecture.


## Updates

Normal updates extract reviewed release files over the existing product root. The root is not deleted unless `--force-delete` is explicitly supplied. Files that are not part of the incoming release remain untouched.

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
