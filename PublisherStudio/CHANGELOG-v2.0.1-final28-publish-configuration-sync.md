# BlazorPublisher 2.0.1 final28 — publish configuration synchronization

## Fixed

- All application and installer RID publishes are now self-contained and multi-file.
- Removed installer single-file and self-extraction settings from the release script and every Visual Studio setup profile.
- Removed the invalid release behavior that exposed a bare setup executable without its required adjacent runtime files.
- Synchronized Visual Studio profile destinations with `Build-Release.ps1` under `artifacts\release` for all six supported runtimes.
- Corrected the previously inconsistent setup output folder names for Linux, macOS, Windows ARM64, and the legacy folder profile.
- Explicitly deploys `appsettings*.json`, every file under `Configuration`, and all localization JSON files.
- Added post-publish checks in the web project and release script so missing configuration files stop the release before ZIP creation.
- Updated installer and root publishing documentation to match the actual asset names and multi-file workflow.

## Safeguard

Added `build/Assert-PublishConfiguration.ps1` and wired it into direct Visual Studio builds, single-runtime release builds, all-runtime builds, and reviewed manifest refreshes. It rejects profile/script drift, missing runtime coverage, single-file settings, incomplete configuration deployment, and misleading standalone setup artifacts.

Existing final19 security and 1-Wire preservation files were not changed.

## Installer workflow completion

- Restored the no-command double-click install/update routine without force-deleting `%LOCALAPPDATA%\BlazorPublisher`.
- The default routine updates the application, checks/installs FFmpeg, restores shortcuts and starts PublisherStudio.
- Added default, install, update, start, no-browser, FFmpeg-check, FFmpeg-install and explicit-uninstall launchers.
- Kept installer project launchers, release launcher mirrors, Visual Studio profiles and release validation synchronized.
- Added an installer-workflow safeguard to direct, local-development, per-RID and all-runtime release entry points.
- Synchronized the setup console status text with the real no-command routine and safeguarded it against reverting to the former help-only message.
