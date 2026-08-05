import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';

const root = path.resolve(import.meta.dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');
const installerRoot = 'src/PublisherStudio.InstallerConsole';
const launchers = ['Install.cmd', 'Update.cmd', 'Start.cmd'];

test('double-click follows the LocalGPT-aligned AppData install, update, shortcut, and start routine', () => {
  const program = read(`${installerRoot}/Program.cs`);
  const parse = program.slice(program.indexOf('public static CliOptions Parse(string[] args)'));
  const block = parse.slice(parse.indexOf('if (argsList.Count == 0)'), parse.indexOf('for (var i = 0; i < argsList.Count; i++)'));
  for (const marker of [
    'argsList.Add("--install-publisherstudio")',
    'argsList.Add("--update-publisherstudio")',
    'argsList.Add("--install-ffmpeg")',
    'argsList.Add("--start-publisherstudio")',
    'argsList.Add("--shortcuts")',
  ]) assert.ok(block.includes(marker), marker);
  assert.doesNotMatch(block, /ForceDelete|--force-delete/);
  assert.match(program, /Path\.Combine\(localAppData, "PublisherStudio"\)/);
  assert.doesNotMatch(program, /Path\.Combine\(localAppData, "Programs", "PublisherStudio"\)/);
  assert.match(program, /Running the default install, update, shortcut, and start routine\./);
  assert.match(program, /ExtractZipWithFallback\(zipPath, targetPath, logger\)/);
  assert.match(program, /ExtractZipWithFallback\(setupZipPath, targetPath, logger\)/);
  assert.match(program, /TryStartDetachedSetup\(args\)/);
});

test('only the mandatory Install, Update, Start, and folder shortcuts are maintained', () => {
  const program = read(`${installerRoot}/Program.cs`);
  for (const launcher of launchers) {
    const projectLauncher = read(`${installerRoot}/${launcher}`);
    assert.equal(projectLauncher, read(`installer-launchers/${launcher}`), launcher);
    assert.doesNotMatch(projectLauncher, /--force-delete/);
    assert.ok(program.includes(`"${launcher}"`), launcher);
  }
  for (const obsolete of ['Default.cmd', 'Start-NoBrowser.cmd', 'Check-FFmpeg.cmd', 'Install-FFmpeg.cmd', 'Uninstall.cmd']) {
    assert.equal(fs.existsSync(path.join(root, installerRoot, obsolete)), false, obsolete);
    assert.equal(fs.existsSync(path.join(root, 'installer-launchers', obsolete)), false, obsolete);
  }
  for (const marker of ['PublisherStudio Folder.lnk', 'PublisherStudio Install.url', 'PublisherStudio Update.url', 'PublisherStudio Start.url']) {
    assert.ok(program.includes(marker), marker);
  }
  const profiles = JSON.parse(read(`${installerRoot}/Properties/launchSettings.json`)).profiles;
  assert.deepEqual(Object.keys(profiles).sort(), ['PublisherStudio Install', 'PublisherStudio Start', 'PublisherStudio Update']);
});

test('release archives use the same wrapper extraction contract as LocalGPT', () => {
  const project = read(`${installerRoot}/PublisherStudio.InstallerConsole.csproj`);
  const release = read('Build-Release.ps1');
  for (const launcher of launchers) {
    assert.ok(project.includes(`<None Update="${launcher}" CopyToOutputDirectory="Always" CopyToPublishDirectory="Always" />`), launcher);
    assert.ok(release.includes(`"${launcher}"`), launcher);
  }
  assert.match(release, /Compress-Archive -Path \$appFolder -DestinationPath \$appZip/);
  assert.match(release, /Compress-Archive -Path \$setupFolder -DestinationPath \$setupZip/);
  assert.doesNotMatch(release, /Write-ReleaseManifest|Write-BootstrapRepairManifest|PublisherStudio\.Setup\.repair\.exe/);
});
