# PublisherStudio 4.3.2 — source-only verification

- Base: PublisherStudio 4.3.1 source ZIP.
- Web and InstallerConsole versions: 4.3.2.
- JSON function catalog parsed and checked for three executable media operations and approval policy.
- `python -m unittest discover -s tests/static -p 'test_*release.py' -v`: **4 passed**.
- `node --test tests/js/picture-studio-gif-frames.test.mjs`: **1 passed**.
- Browser-originated artifact confirmation cannot bypass Council approval (`UserConfirmedArtifactBuild = false`).
- Source ZIP CRC verification performed at packaging time.

**Not run:** PowerShell architecture gates (PowerShell absent); .NET/DevExpress build; browser integration; authenticated 1-Wire connection, actual conversion jobs or approval dialogs. These remain mandatory validation in the supported build environment.
