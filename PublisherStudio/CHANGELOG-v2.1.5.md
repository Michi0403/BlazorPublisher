# PublisherStudio 2.1.5

PublisherStudio 2.1.5 repairs the Windows publish guard and locks the one-click installer to the same release/deployment shape used by LocalGPT, reduced to PublisherStudio's smaller feature set.

## Build-policy repair

- The publish-profile guard accepts the maintained `return @{ ... }` switch mapping used by `Build-Release.ps1`.
- The guard still validates every runtime, archive, profile, wrapper folder, and setup/application pairing.
- Three installer logger source comments were moved onto valid language elements, removing `CS1587` warnings.

## Exact release assets

- The installer queries `releases/latest` from `Michi0403/BlazorPublisher`.
- Runtime selection is exact and refuses fallback assets.
- Windows x64 requires `winx64.zip` and `setupwinx64.zip`.
- Both wrapper folders are extracted beneath `%LOCALAPPDATA%\PublisherStudio`.

## Preservation rules

- A normal double-click, install, or update never deletes the PublisherStudio product root.
- Existing files are overwritten only when the incoming release contains the same path.
- Unrelated files and user data remain untouched.
- Recursive product-root deletion is available only through an explicit `--force-delete` operation.
- Uninstall is a preview unless `--force-delete` is also supplied.
