# PublisherStudio 2.1.1

## Installer and deployment

- Moved the canonical installation root to `%LOCALAPPDATA%\PublisherStudio`.
- Replaced the custom preservation/manifest deployment layer with LocalGPT-style ZIP extraction.
- Application and setup wrapper folders are extracted into the same product root.
- Added temporary-copy setup execution so an installed setup can replace itself.
- Restored no-argument one-click install/update/start behavior.
- Reduced maintained launchers to Install, Update, and Start; Folder is a direct shortcut.
- Desktop and Start Menu shortcuts are mandatory in the default Windows workflow.
- Retained former `--*-blazorpublisher` names as compatibility aliases.

## Repository rules

- Replaced installer guards, tests, launch profiles, release checks, Help text, and documentation that enforced the superseded deployment system.
- Release ZIP creation now uses the same wrapper-preserving `Compress-Archive` pattern as LocalGPT.
- Removed the unused deployment layout, manifest, and transaction service sources.

## Compatibility

- PublisherStudio application version: `2.1.1`.
- LocalGPT 1-Wire protocol version: `2.1.1`.
