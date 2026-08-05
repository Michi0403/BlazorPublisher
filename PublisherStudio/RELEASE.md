# PublisherStudio 2.1.2 application-language and build-policy release

PublisherStudio 2.1.2 keeps the working LocalGPT deployment pattern from 2.1.1, restores the logging-integrity policy required by the Windows build, and exposes the application language selector globally.

## Maintained deployment contract

- Product root: `%LOCALAPPDATA%\PublisherStudio`.
- Application ZIP and setup ZIP retain their runtime wrapper directories.
- Both archives extract into the same PublisherStudio root.
- Double-click performs install/update, FFmpeg preparation, shortcut creation, and start.
- Desktop and Start Menu contain Install, Update, Start, and Folder entries.
- Setup runs from a temporary copy when it must replace its installed executable.
- Former `--*-blazorpublisher` command names remain compatibility aliases only.

## Removed deployment contract

The release no longer uses:

- alternate product roots or automatic legacy-root selection;
- release ownership manifests;
- staged repair manifests;
- application/setup transaction services;
- the expanded Default, no-browser, FFmpeg, and uninstall launcher set.

The application still supports explicit FFmpeg and uninstall command-line operations, but those are not mandatory launcher entries.

## Release assets

Each runtime publishes the same two wrapper ZIPs as LocalGPT, for example:

```text
winx64.zip
setupwinx64.zip
```

The separately versioned `LocalGPT.WireProtocolVersion` package remains `2.1.1`.
