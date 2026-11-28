# PublisherStudio 4.8.7

- The macOS PKG is built as an Installer distribution, like LocalGPT's: welcome and read-me pages from `README.md`, the license page from `LICENSE.MD` followed by `THIRD-PARTY-NOTICES.md`, and a background image (`assets/PublisherStudio.png`, or `-DmgBackgroundPath`). Before 4.8.7 the PKG was a bare `productbuild --package` without any of these pages.
- The build stops when one of these files is missing or empty, and checks the expanded, shipped PKG: each resource must be present and referenced by the Installer distribution.
- `THIRD-PARTY-NOTICES.md` is added to the upload-ready release bundle next to the README and license.

Not built here; PKG creation needs macOS (`pkgbuild`, `productbuild`, `pkgutil`), so the first check is a Mac release build.

## Check on the Mac
- [ ] The Installer shows the welcome, read-me and license pages with the PublisherStudio background.
- [ ] The license page ends with the third-party notices.
- [ ] The release folder contains `THIRD-PARTY-NOTICES.md`.
