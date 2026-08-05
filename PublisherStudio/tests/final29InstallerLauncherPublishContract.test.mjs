import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';

const root = path.resolve(import.meta.dirname, '..');
const project = fs.readFileSync(
  path.join(root, 'src', 'PublisherStudio.InstallerConsole', 'PublisherStudio.InstallerConsole.csproj'),
  'utf8',
);
const launchers = ['Install.cmd', 'Update.cmd', 'Start.cmd'];

test('the three mandatory PublisherStudio launchers are explicitly published', () => {
  for (const launcher of launchers) {
    const escaped = launcher.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    assert.match(
      project,
      new RegExp(`<None Update="${escaped}" CopyToOutputDirectory="Always" CopyToPublishDirectory="Always" \\/>`),
      launcher,
    );
  }
  assert.doesNotMatch(project, /Default\.cmd|Start-NoBrowser\.cmd|Check-FFmpeg\.cmd|Install-FFmpeg\.cmd|Uninstall\.cmd/);
});
