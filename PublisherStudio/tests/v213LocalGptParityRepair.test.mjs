import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';

const root = path.resolve(import.meta.dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

test('2.1.9 repairs the installer compiler expression and the orphan XML comment', () => {
  const installer = read('src/PublisherStudio.InstallerConsole/Program.cs');
  assert.match(installer, /_ = Process\.Start\(startInfo\)\s*\?\? throw new InvalidOperationException/);
  assert.doesNotMatch(installer, /(?<!_ = )Process\.Start\(startInfo\) \?\? throw/);

  const webData = read('src/PublisherStudio.Web/Controllers/WebDataController.cs');
  const exportIndex = webData.indexOf('public IActionResult ExportRows');
  assert.ok(exportIndex > 0);
  const preceding = webData.slice(Math.max(0, exportIndex - 400), exportIndex);
  assert.equal((preceding.match(/<summary>/g) ?? []).length, 1);
  assert.equal((preceding.match(/<\/summary>/g) ?? []).length, 1);
});

test('release publishing is the PublisherStudio subset of the LocalGPT shared all-runtime lane', () => {
  const release = read('Build-Release.ps1');
  const all = read('Build-AllRuntimes.ps1');
  for (const marker of [
    '[string]$Runtime = "all"',
    'Prepare-PublisherStudioDocumentation',
    'Cached one verified documentation payload for all RID publishes.',
    'foreach ($rid in $runtimes)',
    'Assert-PublisherStudioDocumentationPayload',
    'New-PublisherStudioReleaseArchive -SourceDirectory $appFolder',
    'New-PublisherStudioReleaseArchive -SourceDirectory $setupFolder'
  ]) assert.ok(release.includes(marker), marker);
  assert.match(all, /Runtime = "all"/);
  assert.doesNotMatch(release, /Programs\\PublisherStudio|BlazorPublisher/);
});

test('GitHub Pages deploys the pinned validated Kawaii snapshot with the LocalGPT workflow shape', () => {
  const workflow = read('../.github/workflows/publish-shipped-docs.yml');
  for (const marker of [
    'actions/checkout@v6',
    'actions/configure-pages@v5',
    'actions/upload-pages-artifact@v4',
    'actions/deploy-pages@v4',
    '.github/pages/publisherstudio-kawaii-docs.zip',
    '.github/scripts/prepare-pages-artifact.py'
  ]) assert.ok(workflow.includes(marker), marker);
  assert.equal(fs.existsSync(path.join(root, '..', '.github/scripts/extract-shipped-docs.py')), false);
  assert.ok(fs.existsSync(path.join(root, '..', '.github/pages/publisherstudio-kawaii-docs.zip')));
  execFileSync('python3', [
    path.join(root, '..', '.github/scripts/prepare-pages-artifact.py'),
    '--archive', path.join(root, '..', '.github/pages/publisherstudio-kawaii-docs.zip'),
    '--output', path.join(root, '.tmp-pages-validation-v213')
  ], { stdio: 'pipe' });
  fs.rmSync(path.join(root, '.tmp-pages-validation-v213'), { recursive: true, force: true });
});

test('Kawaii website assets use PublisherStudio names only and include the maintained theme controls', () => {
  const js = read('docs/templates/publisherstudio/public/main.js');
  const css = read('docs/templates/publisherstudio/public/main.css');
  for (const marker of ['publisherstudio-docs-theme', 'mountThemeControl', 'persistTheme', 'Auto', 'Light', 'Dark']) {
    assert.ok(js.includes(marker), marker);
  }
  for (const marker of ['publisherstudio-kawaii-docs', 'overflow-x: clip', 'data-bs-theme="dark"']) {
    assert.ok(css.includes(marker), marker);
  }
  assert.doesNotMatch(js, /localgpt/i);
  assert.doesNotMatch(css, /localgpt/i);
  for (const file of ['favicon.svg', 'favicon.ico', 'logo.svg']) {
    assert.ok(fs.existsSync(path.join(root, 'docs/templates/publisherstudio/public', file)), file);
  }
});
