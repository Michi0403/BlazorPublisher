import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { execFileSync } from 'node:child_process';

const root = path.resolve(import.meta.dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

function writeSizedFile(file, bytes, prefix) {
  fs.mkdirSync(path.dirname(file), { recursive: true });
  const prefixBuffer = Buffer.from(prefix, 'utf8');
  const content = Buffer.alloc(bytes, 0x20);
  prefixBuffer.copy(content, 0);
  fs.writeFileSync(file, content);
}

test('2.1.8 prepares pinned DevExtreme assets from nested npm layouts', () => {
  const temp = fs.mkdtempSync(path.join(os.tmpdir(), 'publisherstudio-devextreme-'));
  try {
    const project = path.join(temp, 'PublisherStudio.Web');
    fs.mkdirSync(path.join(project, 'tools'), { recursive: true });
    fs.copyFileSync(
      path.join(root, 'src/PublisherStudio.Web/tools/prepare-devexpress-assets.mjs'),
      path.join(project, 'tools/prepare-devexpress-assets.mjs')
    );
    fs.writeFileSync(path.join(project, 'package.json'), JSON.stringify({
      type: 'module',
      dependencies: { 'devextreme-dist': '25.2.8' }
    }));

    // Model a package layout with an extra directory level. The preparation
    // step must normalize this to the stable wwwroot/vendor contract.
    writeSizedFile(
      path.join(project, 'node_modules/devextreme-dist/package/js/dx.all.js'),
      600 * 1024,
      '/* DevExtreme 25.2.8 */'
    );
    writeSizedFile(
      path.join(project, 'node_modules/devextreme-dist/package/css/dx.light.css'),
      80 * 1024,
      '/* DevExtreme light theme 25.2.8 */'
    );
    fs.mkdirSync(path.join(project, 'node_modules/devexpress-aspnetcore-spreadsheet/dist'), { recursive: true });
    fs.writeFileSync(path.join(project, 'node_modules/devexpress-aspnetcore-spreadsheet/dist/dx-aspnetcore-spreadsheet.js'), 'spreadsheet');
    fs.mkdirSync(path.join(project, 'node_modules/jquery/dist'), { recursive: true });
    fs.writeFileSync(path.join(project, 'node_modules/jquery/dist/jquery.min.js'), 'jquery');
    fs.mkdirSync(path.join(project, 'wwwroot/vendor'), { recursive: true });
    fs.writeFileSync(
      path.join(project, 'wwwroot/vendor/devextreme-license.js'),
      "DevExpress.config({ licenseKey: 'public-runtime-key' });"
    );

    execFileSync(process.execPath, [path.join(project, 'tools/prepare-devexpress-assets.mjs')], {
      cwd: project,
      stdio: 'pipe'
    });

    const js = path.join(project, 'wwwroot/vendor/devextreme-dist/js/dx.all.js');
    const css = path.join(project, 'wwwroot/vendor/devextreme-dist/css/dx.light.css');
    const metadataPath = path.join(project, 'wwwroot/vendor/devextreme-assets.meta.json');
    assert.ok(fs.statSync(js).size >= 512 * 1024);
    assert.ok(fs.statSync(css).size >= 64 * 1024);
    assert.ok(fs.existsSync(metadataPath));
    const metadata = JSON.parse(fs.readFileSync(metadataPath, 'utf8'));
    assert.equal(metadata.devExtremeVersion, '25.2.8');
    assert.deepEqual(metadata.assets.map(asset => asset.path), [
      'devextreme-dist/js/dx.all.js',
      'devextreme-dist/css/dx.light.css'
    ]);
    assert.ok(metadata.assets.every(asset => /^[a-f0-9]{64}$/.test(asset.sha256)));
  } finally {
    fs.rmSync(temp, { recursive: true, force: true });
  }
});

test('release and publish guards require the DevExtreme client-asset manifest', () => {
  const powershell = read('Prepare-DevExpressAssets.ps1');
  const release = read('Build-Release.ps1');
  const project = read('src/PublisherStudio.Web/PublisherStudio.Web.csproj');
  const module = read('src/PublisherStudio.Web/tools/prepare-devexpress-assets.mjs');
  const sourcePackage = read('New-VerifiedSourcePackage.ps1');

  assert.match(powershell, /devextreme-assets\.meta\.json/);
  assert.match(powershell, /Get-FileHash -LiteralPath \$assetPath -Algorithm SHA256/);
  assert.match(release, /devextreme-assets\.meta\.json/);
  assert.match(project, /<DevExtremeClientAssetMetadata>/);
  assert.match(module, /dereference:\s*true/);
  assert.match(module, /ensureDevExtremeAsset\("js\/dx\.all\.js"/);
  assert.match(module, /ensureDevExtremeAsset\("css\/dx\.light\.css"/);
  assert.doesNotMatch(module, /cdn3\.devexpress\.com|unpkg\.com|jsdelivr\.net/);
  assert.match(sourcePackage, /devextreme-assets\.meta\.json/);
});
