import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';

const root = path.resolve(import.meta.dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

const webProject = read('src/PublisherStudio.Web/PublisherStudio.Web.csproj');
const setupProject = read('src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj');
const release = read('Build-Release.ps1');
const allRuntimes = read('Build-AllRuntimes.ps1');
const guard = read('build/Assert-PublishConfiguration.ps1');

const webProfiles = [
  ['winx64.pubxml', 'win-x64', 'winx64'],
  ['winarm64.pubxml', 'win-arm64', 'winarm64'],
  ['linx64.pubxml', 'linux-x64', 'linx64'],
  ['linarm64.pubxml', 'linux-arm64', 'linarm64'],
  ['macosx64.pubxml', 'osx-x64', 'macosx64'],
  ['macosarm64.pubxml', 'osx-arm64', 'macosarm64'],
];
const setupProfiles = [
  ['FolderProfile.pubxml', 'win-x64', 'setupwin-x64'],
  ['winx64.pubxml', 'win-x64', 'setupwin-x64'],
  ['winarm64.pubxml', 'win-arm64', 'setupwin-arm64'],
  ['linuxx64.pubxml', 'linux-x64', 'setuplin-x64'],
  ['linuxarm64.pubxml', 'linux-arm64', 'setuplin-arm64'],
  ['macosx64.pubxml', 'osx-x64', 'setupmacos-x64'],
  ['macosarm64.pubxml', 'osx-arm64', 'setupmacos-arm64'],
];

function assertCommonProfile(profile, runtime, outputProperty, outputFolder) {
  assert.match(profile, new RegExp(`<RuntimeIdentifier>${runtime}</RuntimeIdentifier>`));
  assert.match(profile, /<SelfContained>true<\/SelfContained>/);
  assert.match(profile, /<PublishSingleFile>false<\/PublishSingleFile>/);
  assert.match(profile, /<PublishTrimmed>false<\/PublishTrimmed>/);
  assert.match(profile, /<PublishReadyToRun>false<\/PublishReadyToRun>/);
  assert.match(profile, /<DeleteExistingFiles>true<\/DeleteExistingFiles>/);
  assert.match(profile, new RegExp(`<${outputProperty}>\\.\\.\\\\\\.\\.\\\\artifacts\\\\release\\\\${outputFolder}\\\\</${outputProperty}>`));
  assert.doesNotMatch(profile, /IncludeNativeLibrariesForSelfExtract|EnableCompressionInSingleFile|PublishSingleFile>true/);
}

test('all project and profile publishes are self-contained multi-file outputs', () => {
  for (const project of [webProject, setupProject]) {
    assert.match(project, /<SelfContained Condition="'\$\(RuntimeIdentifier\)' != ''">true<\/SelfContained>/);
    assert.match(project, /<PublishSingleFile>false<\/PublishSingleFile>/);
    assert.match(project, /<PublishTrimmed>false<\/PublishTrimmed>/);
    assert.match(project, /<PublishReadyToRun>false<\/PublishReadyToRun>/);
  }
  for (const [file, runtime, folder] of webProfiles) {
    assertCommonProfile(read(`src/PublisherStudio.Web/Properties/PublishProfiles/${file}`), runtime, 'PublishUrl', folder);
  }
  for (const [file, runtime, folder] of setupProfiles) {
    assertCommonProfile(read(`src/PublisherStudio.InstallerConsole/Properties/PublishProfiles/${file}`), runtime, 'PublishDir', folder);
  }
});

test('release scripts and Visual Studio profiles share the artifact layout', () => {
  assert.match(release, /\$multiFileSelfContainedProperties\s*=\s*@\(/);
  assert.equal((release.match(/\+\s*\$multiFileSelfContainedProperties/g) ?? []).length, 2);
  assert.doesNotMatch(release, /PublishSingleFile=true|IncludeNativeLibrariesForSelfExtract=true|EnableCompressionInSingleFile=true/);
  assert.doesNotMatch(release, /Join-Path\s+\$artifacts\s+"PublisherStudio\.Setup\.exe"/);
  for (const [, runtime, folder] of webProfiles) {
    assert.ok(release.includes(`"${runtime}"`), runtime);
    assert.ok(release.includes(`AppFolder = "${folder}"`), folder);
    assert.ok(allRuntimes.includes(`"${runtime}"`), runtime);
  }
  for (const [file,, folder] of setupProfiles.filter(([file]) => file !== 'FolderProfile.pubxml')) {
    assert.ok(release.includes(`SetupFolder = "${folder}"`), `${file}:${folder}`);
  }
});

test('every maintained configuration payload is copied and checked after publish', () => {
  for (const marker of [
    'Content Update="appsettings*.json"',
    'Content Update="Localization\\**\\*.json"',
    'Content Update="Configuration\\**\\*"',
    'None Update="Configuration\\**\\*"',
    'PublisherStudioConfigurationFile Include="appsettings*.json;Configuration\\**\\*;Localization\\**\\*.json"',
    'ValidatePublisherConfigurationFilesForPublish',
  ]) assert.ok(webProject.includes(marker), marker);
  assert.match(release, /Assert-PublishedConfigurationFiles -SourceRoot \$webDirectory -PublishRoot \$appFolder/);
});

test('publish configuration validation remains explicit and source-backed', () => {
  assert.match(guard, /Publish configuration validation passed/);
  assert.match(webProject, /ValidatePublisherConfigurationFilesForPublish/);
});
