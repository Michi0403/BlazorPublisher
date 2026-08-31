# PublisherStudio 3.1.9 source validation

Static validation for this source handoff covers the macOS four-RID release lane, Windows/Linux host defaults, Homebrew `rpm` discovery/provisioning, explicit Linux RPM targets, optional RPM/AppImage failure policy, container opt-in behavior, local-first LocalGPT.ReleasePackaging consumption, macOS executable modes, PublisherStudio architecture/component/service/interop policies, InteractiveServer boundaries, XML documentation, structured-file parsing, archive safety, and exact extracted-ZIP equality.

Current Homebrew documentation lists `rpm` as a supported macOS formula (`brew install rpm`). RPM's maintained `rpmbuild` documentation supports `--target` for selecting the package platform. AppImage remains Linux-only, so a Mac build does not claim native Homebrew AppImage support.

This environment does not contain the .NET SDK or PowerShell, so it does not claim a local `dotnet build`, PowerShell execution, RPM/DMG/AppImage creation, or installer execution. The user's Windows 3.1.7 build remains compile evidence for the application and Windows setup path; this release changes release-host orchestration/native packaging scripts rather than PublisherStudio runtime behavior.
