import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, '..');
const read = relative => readFileSync(resolve(root, relative), 'utf8');
const installer = read('src/PublisherStudio.InstallerConsole/Program.cs');
const deployment = read('src/PublisherStudio.InstallerConsole/Installation/PublisherStudioDeploymentService.cs');
const layout = read('src/PublisherStudio.InstallerConsole/Installation/PublisherStudioInstallLayout.cs');
const manifest = read('src/PublisherStudio.InstallerConsole/Installation/PublisherStudioReleaseManifest.cs');
const provisioner = read('src/PublisherStudio.InstallerConsole/FfmpegProvisioner.cs');
const locator = read('src/PublisherStudio.Web/Services/Streaming/Encoding/FfmpegLocator.cs');
const release = read('Build-Release.ps1');

assert.match(provisioner, /"--source", "winget"/);
assert.match(provisioner, /"--disable-interactivity"/);
assert.match(provisioner, /TimeSpan\.FromMinutes\(15\)/);
assert.match(provisioner, /process\.Kill\(entireProcessTree: true\)/);
assert.match(provisioner, /ProgressHeartbeat = TimeSpan\.FromSeconds\(30\)/);
assert.match(provisioner, /ffmpeg -version|ArgumentList\.Add\("-version"\)/);
assert.match(provisioner, /FindWinGetPackageExecutables/);
assert.match(locator, /FindWinGetPackageExecutables/);

assert.match(installer, /GetJsonWithRetryAsync/);
assert.match(installer, /RangeHeaderValue\(resumeAt, null\)/);
assert.match(installer, /\.part/);
assert.match(installer, /ReadWithStallTimeoutAsync/);
assert.match(installer, /TimeSpan\.FromMinutes\(2\)/);
assert.match(installer, /ValidateZipArchive\(applicationZipPath/);
assert.match(installer, /ValidateZipArchive\(setupZipPath/);
assert.ok(
  installer.indexOf('ValidateZipArchive(setupZipPath') < installer.indexOf('deployment.DeployApplication'),
  'both ZIPs must be validated before the runtime folder is touched',
);
assert.match(installer, /deployment\.DeploySetup\(setupZipPath, layout\)/);
assert.match(installer, /return 1;\s*\}\s*try\s*\{\s*if \(options\.CheckFfmpeg/);
assert.match(installer, /Timeout\.InfiniteTimeSpan/);
assert.match(installer, /runtimeIdentifier: layout\.RuntimeIdentifier/);
assert.match(installer, /GetReleaseAssetTokens\(runtimeIdentifier\)/);
assert.doesNotMatch(installer, /RemoveLegacyMediaHostPayload|PublisherStudio\.MediaHost\*/);

assert.match(layout, /Array\.Find\(candidates, ContainsInstallation\)/);
assert.match(layout, /TryResolveRunningSetupFolder\(root\)/);
assert.match(layout, /matchingRuntimeFolder/);
assert.match(layout, /RuntimeIdentifierFromFolder/);
assert.match(layout, /PublisherStudio setup does not support architecture/);
assert.match(deployment, /MergeManagedPayload\(payloadRoot, layout\.ApplicationDirectory/);
assert.doesNotMatch(deployment, /Directory\.Move\(layout\.ApplicationDirectory/);
assert.match(deployment, /previousManifest\?\.Files/);
assert.match(deployment, /FileMatchesManifestHash/);
assert.match(deployment, /modified after the previous release/);
assert.match(deployment, /preserved unrelated or modified files/);
assert.match(deployment, /RollBackManagedMerge/);
assert.match(deployment, /ScheduleWindowsSetupReplacement/);
assert.match(deployment, /string\.Equals\(fullCandidate, fullRoot/);
assert.match(deployment, /expectedRuntimeIdentifier/);
assert.match(deployment, /Apply-PublisherStudioSetupUpdate\.ps1/);
assert.doesNotMatch(deployment, /\[IO\.Path\]::GetRelativePath/);
assert.match(deployment, /publisherstudio-bootstrap-repair\.json/);
assert.match(deployment, /missing the launcher repair payload required for existing Windows installations/);
assert.match(deployment, /PublisherStudio setup file merge completed/);
assert.match(deployment, /SHA256\.HashData/);
assert.match(deployment, /unsupported symbolic link/);
assert.match(deployment, /uncatalogued file/);
assert.match(deployment, /Move-Item -LiteralPath \$temporary -Destination \$target -Force/);
assert.match(manifest, /List<PublisherStudioReleaseFile> Files/);
assert.match(release, /SchemaVersion = 2/);
assert.match(release, /Get-FileHash -LiteralPath \$_\.FullName -Algorithm SHA256/);
assert.match(release, /New-ReleaseArchive -SourceRoot \$appFolder/);
assert.match(release, /RootFolderName \$profile\.AppFolder/);
assert.match(release, /PublisherStudio\.Setup\.repair\.exe/);
assert.match(release, /Write-BootstrapRepairManifest/);
assert.match(release, /PreludeRoot \$setupFolder/);
assert.match(release, /launcher repair prelude/);
assert.match(release, /-LastFile \$setupExecutable/);

console.log('installer resilience and preservation contract tests passed');
