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

const assertCommonProfile = (relative, runtime, folder) => {
  const profile = read(relative);
  assert.match(profile, new RegExp(`<RuntimeIdentifier>${runtime}</RuntimeIdentifier>`));
  assert.ok(profile.includes(`<PublishUrl>..\\..\\artifacts\\release\\${folder}\\</PublishUrl>`), `${relative}:PublishUrl`);
  for (const marker of [
    '<SelfContained>true</SelfContained>',
    '<PublishTrimmed>false</PublishTrimmed>',
    '<DeleteExistingFiles>true</DeleteExistingFiles>',
  ]) assert.ok(profile.includes(marker), `${relative}:${marker}`);
  if (profile.includes('<PublishDir>')) {
    assert.ok(profile.includes(`<PublishDir>..\\..\\artifacts\\release\\${folder}\\</PublishDir>`), `${relative}:PublishDir`);
  }
  if (profile.includes('<PublishReadyToRun>')) {
    assert.ok(profile.includes('<PublishReadyToRun>false</PublishReadyToRun>'), `${relative}:PublishReadyToRun`);
  }
  return profile;
};

const assertWebProfile = (relative, runtime, folder) => {
  const profile = assertCommonProfile(relative, runtime, folder);
  assert.ok(profile.includes('<PublishSingleFile>false</PublishSingleFile>'), `${relative}:PublishSingleFile`);
  assert.match(profile, /<(?:PublishProtocol|WebPublishMethod|PublishProvider)>FileSystem<\/(?:PublishProtocol|WebPublishMethod|PublishProvider)>/);
  assert.match(profile, /<(?:Platform|LastUsedPlatform)>Any CPU<\/(?:Platform|LastUsedPlatform)>/);
};

const assertSetupProfile = (relative, runtime, folder) => {
  const profile = assertCommonProfile(relative, runtime, folder);
  assert.ok(profile.includes('<PublishSingleFile>true</PublishSingleFile>'), `${relative}:PublishSingleFile`);
  assert.ok(profile.includes(`<PublishDir>..\\..\\artifacts\\release\\${folder}\\</PublishDir>`), `${relative}:PublishDir`);
  assert.ok(profile.includes('<PublishProtocol>FileSystem</PublishProtocol>'), `${relative}:PublishProtocol`);
  assert.ok(profile.includes('<Platform>Any CPU</Platform>'), `${relative}:Platform`);
};

test('developer application and installer profiles remain available', () => {
  const webRoot = path.join(root, 'src', 'PublisherStudio.Web', 'Properties', 'PublishProfiles');
  const setupRoot = path.join(root, 'src', 'PublisherStudio.InstallerConsole', 'Properties', 'PublishProfiles');
  const expected = profiles.map(([name]) => name).sort();
  assert.deepEqual(fs.readdirSync(webRoot).filter(name => name.endsWith('.pubxml')).sort(), expected);
  assert.deepEqual(fs.readdirSync(setupRoot).filter(name => name.endsWith('.pubxml')).sort(), expected);
  assert.deepEqual(fs.readdirSync(webRoot).filter(name => name.endsWith('.pubxml.user')), []);
  assert.deepEqual(fs.readdirSync(setupRoot).filter(name => name.endsWith('.pubxml.user')), []);
});

test('developer profiles and scripted release lane preserve their supported contracts', () => {
  for (const [file, runtime, appFolder, setupFolder] of profiles) {
    assertWebProfile(`src/PublisherStudio.Web/Properties/PublishProfiles/${file}`, runtime, appFolder);
    assertSetupProfile(`src/PublisherStudio.InstallerConsole/Properties/PublishProfiles/${file}`, runtime, setupFolder);
    assert.ok(release.includes(`"${runtime}"`), runtime);
    assert.ok(release.includes(`AppFolder = "${appFolder}"`), appFolder);
    assert.ok(release.includes(`SetupFolder = "${setupFolder}"`), setupFolder);
    assert.ok(release.includes(`SetupAsset = "${setupFolder}"`), setupFolder);
    assert.ok(allRuntimes.includes(`"${runtime}"`), `Build-AllRuntimes:${runtime}`);
  }
  assert.doesNotMatch(release, /PublishSingleFile=true|IncludeNativeLibrariesForSelfExtract=true|EnableCompressionInSingleFile=true/);
});
