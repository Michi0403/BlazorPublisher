# PublisherStudio publish configuration policy

PublisherStudio keeps Visual Studio publish profiles for both the web host and installer. `Build-Release.ps1` is the automated lane; the checked-in `.pubxml` files are the normal developer lane. Both lanes use the same runtime identifiers, multi-file/self-contained settings, output folders and asset tokens.

## Runtime policy

Every RID-specific application and setup publish is:

- self-contained;
- multi-file (`PublishSingleFile=false`);
- untrimmed;
- not ReadyToRun.

The project files apply `SelfContained=true` only when a `RuntimeIdentifier` is present. This keeps ordinary RID-less development builds valid while making every reviewed publish self-contained. The release script and all Visual Studio profiles set the same properties explicitly.

## Shared release location

Both publishing paths write to `artifacts\release`:

- application folders: `winx64`, `winx86`, `winarm64`, `linx64`, `linarm64`, `macosx64`, `macosarm64`;
- setup folders: `setupwinx64`, `setupwinx86`, `setupwinarm64`, `setuplinx64`, `setuplinarm64`, `setupmacosx64`, `setupmacosarm64`.

`Build-Release.ps1` creates the matching ZIP assets. Because the setup is multi-file, the release workflow does not copy a bare `PublisherStudio.Setup.exe` to the artifact root. Use the complete setup folder or setup ZIP.

## Configuration payload

The web project publishes all maintained runtime configuration sources:

- `appsettings*.json`;
- every file below `Configuration`;
- every localization JSON file below `Localization`.

The project has an after-publish validation target, and `Build-Release.ps1` independently checks that every discovered source configuration file exists in the application publish folder before creating ZIPs.

## Enforcement

`build/Assert-PublishConfiguration.ps1` verifies:

- project defaults;
- all application and installer publish profiles;
- runtime identifiers and output folders;
- the shared release-script property list;
- configuration-copy and post-publish validation markers;
- all-runtime release coverage;
- absence of single-file-only settings and misleading standalone setup artifacts.

The guard runs from direct Windows/Visual Studio builds, `Build-Release.ps1`, `Build-AllRuntimes.ps1`, and the reviewed manifest refresh workflow.
