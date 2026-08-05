import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import test from 'node:test';
import { execFileSync } from 'node:child_process';

const projectRoot = path.resolve(import.meta.dirname, '..');
const repositoryRoot = path.resolve(projectRoot, '..');
const validator = path.join(repositoryRoot, '.github/scripts/prepare-pages-artifact.py');

function prepareFixture() {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'publisherstudio-pages-repair-'));
  const source = path.join(root, 'source');
  const output = path.join(root, 'output');
  fs.cpSync(path.join(repositoryRoot, 'docs'), source, { recursive: true });
  return { root, source, output };
}

test('Pages preparation materializes omitted DocFX namespace landing pages before strict validation', () => {
  const fixture = prepareFixture();
  try {
    const pageName = 'PublisherStudio.Controllers.Streaming.UseCases.NativeCaptureController.html';
    fs.writeFileSync(
      path.join(fixture.source, 'api', pageName),
      '<!doctype html><html><body><a class="xref" href="PublisherStudio.Controllers.Streaming.html">PublisherStudio.Controllers.Streaming</a></body></html>\n',
      'utf8',
    );

    execFileSync('python3', [
      validator,
      '--source', fixture.source,
      '--output', fixture.output,
      '--expected-version', '2.1.9',
    ], { stdio: 'pipe' });

    const namespacePage = path.join(fixture.output, 'api', 'PublisherStudio.Controllers.Streaming.html');
    assert.equal(fs.existsSync(namespacePage), true);
    assert.match(fs.readFileSync(namespacePage, 'utf8'), /data-publisherstudio-generated-namespace-page="true"/);
    assert.match(fs.readFileSync(namespacePage, 'utf8'), /NativeCaptureController/);

    const metadata = JSON.parse(fs.readFileSync(path.join(fixture.output, 'github-pages-deployment.json'), 'utf8'));
    assert.equal(metadata.generatedDocfxNamespacePages, 1);
    assert.equal(metadata.localLinksValidated, true);
  } finally {
    fs.rmSync(fixture.root, { recursive: true, force: true });
  }
});

test('Pages preparation still rejects ordinary missing files after namespace repair', () => {
  const fixture = prepareFixture();
  try {
    fs.writeFileSync(
      path.join(fixture.source, 'api', 'PublisherStudio.Controllers.BrokenController.html'),
      '<!doctype html><html><body><a href="DefinitelyMissing.html">broken</a></body></html>\n',
      'utf8',
    );

    assert.throws(
      () => execFileSync('python3', [
        validator,
        '--source', fixture.source,
        '--output', fixture.output,
        '--expected-version', '2.1.9',
      ], { stdio: 'pipe' }),
      error => {
        const stderr = String(error.stderr || '');
        return stderr.includes('Documentation contains invalid local links') && stderr.includes('DefinitelyMissing.html');
      },
    );
  } finally {
    fs.rmSync(fixture.root, { recursive: true, force: true });
  }
});

test('documentation build invokes the namespace repair before final publication', () => {
  const build = fs.readFileSync(path.join(projectRoot, 'build/Build-Documentation.ps1'), 'utf8');
  const repair = fs.readFileSync(path.join(projectRoot, 'build/Repair-DocfxNamespacePages.ps1'), 'utf8');
  assert.match(build, /Repair-PublisherStudioDocfxNamespacePages -SiteRoot \$siteRoot/);
  assert.match(build, /apiNamespacePageCount/);
  assert.match(repair, /data-publisherstudio-generated-namespace-page/);
  assert.match(repair, /StartsWith\(\$prefix, \[StringComparison\]::Ordinal\)/);
});
