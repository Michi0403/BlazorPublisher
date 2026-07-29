import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';

const root = path.resolve(import.meta.dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

const release = read('Build-Release.ps1');
const webProfiles = [
  ['winx64.pubxml', 'win-x64', 'winx64', 'setupwinx64'],
  ['winarm64.pubxml', 'win-arm64', 'winarm64', 'setupwinarm64'],
  ['linx64.pubxml', 'linux-x64', 'linx64', 'setuplinx64'],
  ['linarm64.pubxml', 'linux-arm64', 'linarm64', 'setuplinarm64'],
  ['macosx64.pubxml', 'osx-x64', 'macosx64', 'setupmacosx64'],
  ['macosarm64.pubxml', 'osx-arm64', 'macosarm64', 'setupmacosarm64'],
];

test('only web-host publish profiles remain', () => {
  const webRoot = path.join(root, 'src', 'PublisherStudio.Web', 'Properties', 'PublishProfiles');
  assert.deepEqual(
    fs.readdirSync(webRoot).filter(name => name.endsWith('.pubxml')).sort(),
    webProfiles.map(([name]) => name).sort(),
  );
  const setupRoot = path.join(root, 'src', 'PublisherStudio.InstallerConsole', 'Properties', 'PublishProfiles');
  assert.equal(fs.existsSync(setupRoot), false);
  assert.deepEqual(fs.readdirSync(webRoot).filter(name => name.endsWith('.pubxml.user')), []);
});

test('web profiles and release folder names use one runtime token', () => {
  for (const [file, runtime, appFolder, setupFolder] of webProfiles) {
    const profile = read(`src/PublisherStudio.Web/Properties/PublishProfiles/${file}`);
    assert.match(profile, new RegExp(`<RuntimeIdentifier>${runtime}</RuntimeIdentifier>`));
    assert.match(profile, new RegExp(`<PublishUrl>\\.\\.\\\\\\.\\.\\\\artifacts\\\\release\\\\${appFolder}\\\\</PublishUrl>`));
    for (const marker of ['<SelfContained>true</SelfContained>', '<PublishSingleFile>false</PublishSingleFile>', '<PublishTrimmed>false</PublishTrimmed>', '<PublishReadyToRun>false</PublishReadyToRun>'])
      assert.ok(profile.includes(marker), `${file}:${marker}`);
    assert.ok(release.includes(`AppFolder = "${appFolder}"`), appFolder);
    assert.ok(release.includes(`SetupFolder = "${setupFolder}"`), setupFolder);
    assert.ok(release.includes(`SetupAsset = "${setupFolder}"`), setupFolder);
  }
  assert.doesNotMatch(release, /SetupFolder\s*=\s*"[^"]*-/);
});
