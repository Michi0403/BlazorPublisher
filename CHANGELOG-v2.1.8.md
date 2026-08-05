# PublisherStudio 2.1.8

## Installer completion

- The setup now downloads the exact runtime assets through GitHub's stable `releases/latest/download/<asset>` endpoint first, avoiding a mandatory GitHub API lookup.
- The release API remains a strict exact-name fallback rather than guessing another runtime asset.
- Both application and setup archives are acquired and validated before an existing installation is changed.
- Explicit `--publisherstudio-zip` and `--publisherstudio-setup-zip` inputs are now used as local archives and are no longer overwritten by a download.
- Archives must retain the expected `winx64` / `setupwinx64` style wrapper directory, preserving `%LOCALAPPDATA%\PublisherStudio\<runtime>` and `%LOCALAPPDATA%\PublisherStudio\setup<runtime>`.
