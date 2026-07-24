import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, '..');
const installer = readFileSync(resolve(root, 'src/PublisherStudio.InstallerConsole/Program.cs'), 'utf8');
const provisioner = readFileSync(resolve(root, 'src/PublisherStudio.InstallerConsole/FfmpegProvisioner.cs'), 'utf8');
const locator = readFileSync(resolve(root, 'src/PublisherStudio.Web/Backend/Streaming/Encoding/FfmpegLocator.cs'), 'utf8');

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
assert.match(installer, /ValidateZipArchive\(zipPath/);
assert.match(installer, /ValidateZipArchive\(setupZipPath/);
assert.ok(installer.indexOf('ValidateZipArchive(setupZipPath') < installer.indexOf('DeleteIfExists(targetPath'), 'both ZIPs must be validated before force-delete');
assert.match(installer, /return 1;\s*\}\s*try\s*\{\s*if \(options\.CheckFfmpeg/);
assert.match(installer, /Timeout\.InfiniteTimeSpan/);

console.log('installer resilience contract tests passed');
