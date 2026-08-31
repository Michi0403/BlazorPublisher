# Windows + WSL Linux releases

PublisherStudio supports WSL2 as an optional, headless Linux build backend controlled by the Windows release script. Developers without WSL continue to get the existing Windows release. Native Linux developers continue to use the same `pwsh ./Build-Release.ps1` lane.

## Set up Ubuntu/Debian WSL once

After installing an Ubuntu/Debian **WSL2** distro and completing its first-launch user initialization, run from the PublisherStudio repository:

```powershell
.\Setup-WslLinuxBuild.ps1 -Provision
```

The helper verifies WSL2 first, then installs PowerShell, .NET 10 SDK, Python 3, `rpmbuild`, and `appimagetool` prerequisites. A WSL1 distro is rejected with the `wsl.exe --set-version <name> 2` conversion command instead of being provisioned into an unsupported release backend. It does not install Docker/Podman. Provisioning never happens during a normal release unless `-ProvisionWslBuildTools` is explicitly supplied. Choose a distro with `-Distribution Ubuntu` or `WSL_BUILD_DISTRO`.

## DevExpress license

The private DevExpress license remains build-machine material. DevExpress' normal paths are `%APPDATA%\DevExpress\DevExpress_License.txt` on Windows and `$HOME/.config/DevExpress/DevExpress_License.txt` on Linux; the case-sensitive `DevExpress_LicensePath` and `DevExpress_License` environment variables are also supported.

The Windows coordinator automatically bridges a valid Windows license into the WSL child process using the Windows-to-WSL environment direction; `DevExpress_LicensePath` is path-translated before Linux receives it. It is not written into the repository or release artifacts. For a persistent standalone WSL license instead, use:

```powershell
.\Setup-WslLinuxBuild.ps1 -CopyWindowsDevExpressLicense
# or an explicit secure file
.\Setup-WslLinuxBuild.ps1 -DevExpressLicenseFile C:\secure\DevExpress_License.txt
```

## Release behavior

`Build-Release.ps1` defaults to `-WslLinux Auto`. On Windows it builds the native Windows x64/x86/ARM64 application/setup outputs. If an initialized WSL distro has PowerShell, .NET 10, Python, and a usable DevExpress license, Linux x64/ARM64 are delegated to WSL. Source is mirrored into the WSL Linux filesystem before compilation. The Windows parent prepares DevExpress browser assets and complete documentation once; the Linux child validates and reuses those assets instead of regenerating them. Node.js is therefore not a required WSL provisioning prerequisite for the delegated PublisherStudio child.

PublisherStudio's LocalGPT.ReleasePackaging dependency stays LocalGPT-owned. The Windows coordinator resolves the 1.0.1 NuGet tool package locally without installing the Unix tool on Windows, places it in the mirrored source, and the WSL child installs/uses it inside Linux.

If WSL is absent/unready, host-aware `-Runtime all` continues Windows-only. Explicit Linux RIDs retain the existing Windows cross-publish fallback. `-WslLinux Require` turns missing WSL into an error; `-WslLinux Off` disables it. `-WslShutdown IfStarted` stops only a distro that was not already running before the release. `-KeepWslBuildTree` keeps the Linux mirror for diagnostics.

For each delegated RID, Full/Light TAR.GZ and DEB are mandatory. RPM/AppImage are optional unless `-RequireOptionalNativePackages` is set. Provisioned WSL supplies `rpmbuild` and `appimagetool`; AppImage target architecture is selected with `ARCH`, and WSL uses extract-and-run mode to avoid requiring FUSE. Docker remains optional via `-UseContainerPackaging`.

`-Runtime all-rids` on Windows can also attempt portable macOS cross-publishes, but DMG creation, Apple signing, and notarization remain macOS-native finalization tasks.
