import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, '..');
const read = relative => readFileSync(resolve(root, relative), 'utf8');
const installer = read('src/PublisherStudio.InstallerConsole/Program.cs');
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
assert.match(installer, /Path\.Combine\(localAppData, "PublisherStudio"\)/);
assert.doesNotMatch(installer, /Path\.Combine\(localAppData, "Programs", "PublisherStudio"\)/);
assert.match(installer, /ExtractZipWithFallback\(zipPath, targetPath, logger\)/);
assert.match(installer, /ExtractZipWithFallback\(setupZipPath, targetPath, logger\)/);
assert.match(installer, /TryStartDetachedSetup/);
assert.match(installer, /PUBLISHERSTUDIO_SETUP_DETACHED/);
assert.match(installer, /runtimeIdentifier: GetRuntimeIdentifier\(\)/);
assert.match(installer, /GetReleaseAssetTokens\(runtimeIdentifier\)/);
assert.match(installer, /Timeout\.InfiniteTimeSpan/);
assert.doesNotMatch(installer, /PublisherStudioInstallLayout|PublisherStudioDeploymentService|PublisherStudioReleaseManifest/);
assert.doesNotMatch(installer, /RemoveLegacyMediaHostPayload|PublisherStudio\.MediaHost\*/);

assert.match(release, /Compress-Archive -Path \$appFolder -DestinationPath \$appZip/);
assert.match(release, /Compress-Archive -Path \$setupFolder -DestinationPath \$setupZip/);
assert.match(release, /Assert-ReleaseArchiveLayout -ArchivePath \$appZip/);
assert.match(release, /Assert-ReleaseArchiveLayout -ArchivePath \$setupZip/);
assert.doesNotMatch(release, /SchemaVersion = 2|Write-ReleaseManifest|Write-BootstrapRepairManifest|PublisherStudio\.Setup\.repair\.exe|publisherstudio-bootstrap-repair/);

console.log('installer resilience and LocalGPT-aligned AppData deployment contract tests passed');
