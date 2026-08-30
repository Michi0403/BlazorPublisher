# PublisherStudio 3.1.6

PublisherStudio 3.1.6 is the **LocalGPT release-packaging 1.0.1 consumption** maintenance release.

The supplied Windows release log proves PublisherStudio builds its RID-neutral application, documentation, all three Windows application/setup RIDs, and the linux-x64 Full application payload before failing in the shared LocalGPT packaging helper. PublisherStudio itself does not own the failing TAR implementation.

This release updates PublisherStudio's shared-helper requirement from `LocalGPT.ReleasePackaging` 1.0.0 to 1.0.1. The corrected helper remains authored and packaged by LocalGPT, following the same source-ownership model as the 1-Wire NuGet package.

The intended package matrix remains unchanged: Windows setup + portable ZIPs, Linux Full/Light TAR.GZ/DEB/RPM/AppImage outputs, and macOS Full/Light application/TAR.GZ plus DMG completion on macOS.

See `CHANGELOG-v3.1.6-LOCALGPT-PACKAGING-101-CONSUMPTION.md` and `VALIDATION-v3.1.6-source.md`.
