import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';
import { execFileSync } from 'node:child_process';

const projectRoot = path.resolve(import.meta.dirname, '..');
const repositoryRoot = path.resolve(projectRoot, '..');
const readProject = relative => fs.readFileSync(path.join(projectRoot, relative), 'utf8');
const readRepository = relative => fs.readFileSync(path.join(repositoryRoot, relative), 'utf8');

test('GitHub Pages automation is discoverable at repository root and validates its pinned asset', () => {
  assert.equal(fs.existsSync(path.join(projectRoot, '.github')), false);
  const workflow = readRepository('.github/workflows/publish-shipped-docs.yml');
  for (const marker of [
    '.github/pages/publisherstudio-kawaii-docs.zip',
    '.github/scripts/prepare-pages-artifact.py',
    'actions/upload-pages-artifact@v4',
    'actions/deploy-pages@v4',
    '--expected-version "$EXPECTED_VERSION"',
    'PublisherStudio/src/PublisherStudio.Web/PublisherStudio.Web.csproj',
  ]) assert.ok(workflow.includes(marker), marker);

  const output = path.join(projectRoot, '.tmp-pages-v219');
  execFileSync('python3', [
    path.join(repositoryRoot, '.github/scripts/prepare-pages-artifact.py'),
    '--archive', path.join(repositoryRoot, '.github/pages/publisherstudio-kawaii-docs.zip'),
    '--output', output,
    '--expected-version', '2.1.9',
  ], { stdio: 'pipe' });
  const metadata = JSON.parse(fs.readFileSync(path.join(output, 'github-pages-deployment.json'), 'utf8'));
  assert.equal(metadata.version, '2.1.9');
  assert.ok(metadata.htmlFiles >= 20);
  assert.equal(metadata.localLinksValidated, true);
  assert.equal(metadata.pdfFileName, 'PublisherStudio-2.1.9.pdf');
  fs.rmSync(output, { recursive: true, force: true });
});

test('snapshot refresh and verified source packaging preserve repository-root automation', () => {
  const updater = readProject('build/Update-GitHubPagesSnapshot.ps1');
  assert.match(updater, /\$repositoryRoot = Split-Path -Parent \$projectRoot/);
  assert.match(updater, /Join-Path \$repositoryRoot "\.github\\pages\\publisherstudio-kawaii-docs\.zip"/);
  assert.match(updater, /\$temporaryArchive/);
  assert.match(updater, /--expected-version \$expectedVersion/);
  assert.match(updater, /did not pass final validation/);
  assert.match(updater, /\$entry\.LastWriteTime = \[DateTimeOffset\]::new\(1980/);

  const sourcePackage = readProject('New-VerifiedSourcePackage.ps1');
  assert.match(sourcePackage, /\$repositoryRoot/);
  assert.match(sourcePackage, /BlazorPublisher\/\.github\/workflows\/publish-shipped-docs\.yml/);
  assert.match(sourcePackage, /BlazorPublisher\/\.github\/pages\/publisherstudio-kawaii-docs\.zip/);
  assert.match(sourcePackage, /PublisherStudio\/docs\/\(_site\|input\|api\|\\\.tools\|\\\.print-book\)/);
  assert.equal(fs.existsSync(path.join(projectRoot, 'docs/.print-book')), false);
});

test('release ZIP creation is deterministic and installer validation rejects malformed wrappers', () => {
  const release = readProject('Build-Release.ps1');
  assert.match(release, /function New-PublisherStudioReleaseArchive/);
  assert.match(release, /Sort-Object FullName/);
  assert.match(release, /Release archive entry count/);
  assert.match(release, /New-PublisherStudioReleaseArchive -SourceDirectory \$appFolder/);
  assert.match(release, /New-PublisherStudioReleaseArchive -SourceDirectory \$setupFolder/);
  assert.doesNotMatch(release, /Compress-Archive/);
  assert.match(release, /\$entry\.LastWriteTime = \[DateTimeOffset\]::new\(1980/);

  for (const guardPath of ['build/Assert-PublishConfiguration.ps1', 'build/Assert-InstallerWorkflow.ps1']) {
    const guard = readProject(guardPath);
    assert.match(guard, /New-PublisherStudioReleaseArchive/);
    assert.match(guard, /may not use Compress-Archive/);
    assert.doesNotMatch(guard, /'Compress-Archive -Path \$appFolder/);
  }

  const installer = readProject('src/PublisherStudio.InstallerConsole/Program.cs');
  for (const marker of [
    'duplicate path',
    'outside required wrapper',
    'does not contain required executable',
    'GetExpectedPublishedExecutable(runtimeIdentifier, setupAsset: false)',
    'GetExpectedPublishedExecutable(runtimeIdentifier, setupAsset: true)',
  ]) assert.ok(installer.includes(marker), marker);
});


test('repository guidance has no stale protocol package or nested-workflow instructions', () => {
  const repositoryReadme = readRepository('README.md');
  assert.match(repositoryReadme, /LocalGPT\.WireProtocolVersion.*2\.1\.1/s);
  assert.match(repositoryReadme, /repository root in \[`.github\/`\]/);
  assert.doesNotMatch(repositoryReadme, /WireProtocolVersion\.2\.1\.0/);
  assert.doesNotMatch(repositoryReadme, /PublisherStudio\/\.github/);
});
