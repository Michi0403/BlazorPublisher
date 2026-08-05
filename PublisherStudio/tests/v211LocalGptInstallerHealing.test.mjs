import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';

const root = path.resolve(import.meta.dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');
const installer = read('src/PublisherStudio.InstallerConsole/Program.cs');
const release = read('Build-Release.ps1');
const governance = [
  read('AGENTS.md'),
  read('README.md'),
  read('RELEASE.md'),
  read('docs/articles/installer-and-updates.md'),
  read('src/PublisherStudio.InstallerConsole/README.md'),
].join('\n');

test('PublisherStudio follows the LocalGPT application then setup extraction order', () => {
  const appDownload = installer.indexOf('setupAsset: false');
  const appExtract = installer.indexOf('ExtractZipWithFallback(zipPath, targetPath, logger)');
  const setupDownload = installer.indexOf('setupAsset: true');
  const setupExtract = installer.indexOf('ExtractZipWithFallback(setupZipPath, targetPath, logger)');
  assert.ok(appDownload >= 0 && appDownload < appExtract);
  assert.ok(appExtract < setupDownload && setupDownload < setupExtract);
  assert.match(installer, /var targetPath = Path\.Combine\(localAppData, "PublisherStudio"\)/);
  assert.equal((installer.match(/ExtractZipWithFallback\([^;]+targetPath, logger\)/g) ?? []).length, 2);
});

test('one-click command injection matches the maintained LocalGPT setup pattern', () => {
  const parse = installer.slice(installer.indexOf('public static CliOptions Parse(string[] args)'));
  const defaults = parse.slice(parse.indexOf('if (argsList.Count == 0)'), parse.indexOf('for (var i = 0; i < argsList.Count; i++)'));
  for (const command of [
    '--install-publisherstudio',
    '--update-publisherstudio',
    '--install-ffmpeg',
    '--start-publisherstudio',
    '--shortcuts',
  ]) assert.ok(defaults.includes(`argsList.Add("${command}")`), command);
  assert.doesNotMatch(defaults, /force-delete/i);
});

test('release and launchers expose only the requested PublisherStudio actions', () => {
  assert.match(release, /Compress-Archive -Path \$appFolder -DestinationPath \$appZip/);
  assert.match(release, /Compress-Archive -Path \$setupFolder -DestinationPath \$setupZip/);
  const expected = ['Install.cmd', 'Start.cmd', 'Update.cmd'];
  assert.deepEqual(fs.readdirSync(path.join(root, 'installer-launchers')).sort(), expected);
  for (const file of expected) {
    assert.equal(
      fs.readFileSync(path.join(root, 'installer-launchers', file), 'utf8'),
      fs.readFileSync(path.join(root, 'src', 'PublisherStudio.InstallerConsole', file), 'utf8'),
    );
  }
});

test('repository guidance has one authoritative product root and no operational legacy route', () => {
  assert.match(governance, /%LOCALAPPDATA%\\PublisherStudio/);
  assert.match(governance, /Install, Update, Start, and Folder/);
  assert.doesNotMatch(governance, /%LOCALAPPDATA%\\Programs\\PublisherStudio/);
  assert.doesNotMatch(governance, /use existing legacy PublisherStudio install root/i);
  assert.doesNotMatch(governance, /PublisherStudio\.Setup\.repair\.exe/);
});
