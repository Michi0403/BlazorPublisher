import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';

const root = path.resolve(import.meta.dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');
const installerRoot = 'src/PublisherStudio.InstallerConsole';
const launchers = [
  'Default.cmd', 'Install.cmd', 'Update.cmd', 'Start.cmd', 'Start-NoBrowser.cmd',
  'Check-FFmpeg.cmd', 'Install-FFmpeg.cmd', 'Uninstall.cmd',
];

test('double-click invokes a preservation-first default update and FFmpeg routine', () => {
  const program = read(`${installerRoot}/Program.cs`);
  const parse = program.slice(program.indexOf('public static CliOptions Parse(string[] args)'));
  const block = parse.match(/if \(argsList\.Count == 0\)[\s\S]*?return options;/)[0];
  for (const marker of [
    'UpdateBlazorPublisher = true',
    'StartBlazorPublisher = true',
    'InstallFfmpeg = true',
    'DesktopShortcuts = true',
    'StartMenuShortcuts = true',
  ]) assert.ok(block.includes(marker), marker);
  assert.doesNotMatch(block, /ForceDelete|--force-delete/);
  assert.doesNotMatch(program, /The setup help will be shown\./);
  assert.match(program, /Running the default preservation-first install and update routine\./);
});

test('installer launchers, mirrors, shortcuts and Visual Studio profiles stay synchronized', () => {
  const program = read(`${installerRoot}/Program.cs`);
  for (const launcher of launchers) {
    const projectLauncher = read(`${installerRoot}/${launcher}`);
    assert.equal(projectLauncher, read(`installer-launchers/${launcher}`), launcher);
    if (launcher !== 'Uninstall.cmd') assert.doesNotMatch(projectLauncher, /--force-delete/);
    assert.ok(program.includes(`"${launcher}"`), launcher);
  }
  const defaultLauncher = read(`${installerRoot}/Default.cmd`);
  for (const marker of ['SETUP_EXE', 'SETUP_REPAIR', '.incoming', 'move /y', ':setup_repair_failed']) assert.ok(defaultLauncher.includes(marker), marker);
  assert.match(defaultLauncher, /call "%SETUP_EXE%"\s*$/m);
  const profiles = JSON.parse(read(`${installerRoot}/Properties/launchSettings.json`)).profiles;
  assert.ok(profiles['BlazorPublisher Default Install and Update']);
  assert.ok(Object.keys(profiles).length >= launchers.length);
});

test('builds deploy and enforce the complete setup workflow', () => {
  const project = read(`${installerRoot}/PublisherStudio.InstallerConsole.csproj`);
  const release = read('Build-Release.ps1');
  const development = read('Build-LocalDevelopment.ps1');
  const allRuntimes = read('Build-AllRuntimes.ps1');
  assert.match(project, /<None Update="\*\.cmd" CopyToOutputDirectory="Always" CopyToPublishDirectory="Always" \/>/);
  for (const launcher of launchers) assert.ok(release.includes(`"${launcher}"`), launcher);
  assert.match(project, /<None Update="\*\.cmd" CopyToOutputDirectory="Always" CopyToPublishDirectory="Always" \/>/);
});
