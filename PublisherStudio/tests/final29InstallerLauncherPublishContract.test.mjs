import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';

const root = path.resolve(import.meta.dirname, '..');
const project = fs.readFileSync(
  path.join(root, 'src', 'PublisherStudio.InstallerConsole', 'PublisherStudio.InstallerConsole.csproj'),
  'utf8',
);
const launchers = [
  'Default.cmd',
  'Install.cmd',
  'Update.cmd',
  'Start.cmd',
  'Start-NoBrowser.cmd',
  'Check-FFmpeg.cmd',
  'Install-FFmpeg.cmd',
  'Uninstall.cmd',
];

test('all reviewed PublisherStudio launchers are explicitly and generically deployed', () => {
  assert.match(
    project,
    /<None Update="\*\.cmd" CopyToOutputDirectory="Always" CopyToPublishDirectory="Always" \/>/,
  );

  for (const launcher of launchers) {
    const escaped = launcher.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const item = new RegExp(
      `<None Update="${escaped}">\\s*` +
      '<CopyToOutputDirectory>Always<\\/CopyToOutputDirectory>\\s*' +
      '<CopyToPublishDirectory>Always<\\/CopyToPublishDirectory>\\s*' +
      '<\\/None>',
    );
    assert.match(project, item, launcher);
  }
});
