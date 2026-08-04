import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';

const root = path.resolve(import.meta.dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

const release = read('Build-Release.ps1');
const allRuntimes = read('Build-AllRuntimes.ps1');
const profiles = [
  ['winx64.pubxml', 'win-x64', 'winx64', 'setupwinx64'],
  ['winx86.pubxml', 'win-x86', 'winx86', 'setupwinx86'],
  ['winarm64.pubxml', 'win-arm64', 'winarm64', 'setupwinarm64'],
  ['linx64.pubxml', 'linux-x64', 'linx64', 'setuplinx64'],
  ['linarm64.pubxml', 'linux-arm64', 'linarm64', 'setuplinarm64'],
  ['macosx64.pubxml', 'osx-x64', 'macosx64', 'setupmacosx64'],
  ['macosarm64.pubxml', 'osx-arm64', 'macosarm64', 'setupmacosarm64'],
];

const assertProfile = (relative, runtime, folder, singleFile) => {
  const profile = read(relative);
  assert.match(profile, new RegExp(`<RuntimeIdentifier>${runtime}</RuntimeIdentifier>`));
  const expectedOutput = `..\\..\\artifacts\\release\\${folder}\\`;
  assert.ok(profile.includes(`<PublishUrl>${expectedOutput}</PublishUrl>`) || profile.includes(`<PublishDir>${expectedOutput}</PublishDir>`), `${relative}:effective publish output`);
  for (const marker of [
    '<SelfContained>true</SelfContained>',
    `<PublishSingleFile>${singleFile ? 'true' : 'false'}</PublishSingleFile>`,
    '<DeleteExistingFiles>true</DeleteExistingFiles>',
  ]) assert.ok(profile.includes(marker), `${relative}:${marker}`);
  if (singleFile) {
    assert.ok(profile.includes('<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>'), `${relative}:native self-extract`);
    assert.ok(profile.includes('<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>'), `${relative}:single-file compression`);
  }
  assert.doesNotMatch(profile, /<PublishTrimmed>true<\/PublishTrimmed>/);
  assert.doesNotMatch(profile, /<PublishReadyToRun>true<\/PublishReadyToRun>/);
};

test('developer application and installer profiles remain available', () => {
  const webRoot = path.join(root, 'src', 'PublisherStudio.Web', 'Properties', 'PublishProfiles');
  const setupRoot = path.join(root, 'src', 'PublisherStudio.InstallerConsole', 'Properties', 'PublishProfiles');
  assert.deepEqual(fs.readdirSync(webRoot).filter(name => name.endsWith('.pubxml')).sort(), profiles.map(([name]) => name).sort());
  assert.deepEqual(
    fs.readdirSync(setupRoot).filter(name => name.endsWith('.pubxml')).sort(),
    profiles.map(([name]) => name).sort(),
  );
  assert.deepEqual(fs.readdirSync(webRoot).filter(name => name.endsWith('.pubxml.user')), []);
  assert.deepEqual(fs.readdirSync(setupRoot).filter(name => name.endsWith('.pubxml.user')), []);
});

test('developer profiles and scripted release lane share runtime and folder contracts', () => {
  for (const [file, runtime, appFolder, setupFolder] of profiles) {
    assertProfile(`src/PublisherStudio.Web/Properties/PublishProfiles/${file}`, runtime, appFolder, false);
    assertProfile(`src/PublisherStudio.InstallerConsole/Properties/PublishProfiles/${file}`, runtime, setupFolder, true);
    assert.ok(release.includes(`"${runtime}"`), runtime);
    assert.ok(release.includes(`AppFolder = "${appFolder}"`), appFolder);
    assert.ok(release.includes(`SetupFolder = "${setupFolder}"`), setupFolder);
    assert.ok(release.includes(`SetupAsset = "${setupFolder}"`), setupFolder);
    assert.ok(allRuntimes.includes(`"${runtime}"`), `Build-AllRuntimes:${runtime}`);
  }
  assert.doesNotMatch(release, /PublishSingleFile=true|IncludeNativeLibrariesForSelfExtract=true|EnableCompressionInSingleFile=true/);
});
