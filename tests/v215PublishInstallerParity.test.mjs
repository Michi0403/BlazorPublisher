import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';

const root = path.resolve(import.meta.dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

test('publish guard accepts and validates the maintained return-hashtable release mapping', () => {
  const guard = read('build/Assert-PublishConfiguration.ps1');
  const release = read('Build-Release.ps1');
  assert.match(guard, /\(\?:return\\s\+\)\?@\\\{/);

  const mappings = [
    ['win-x64', 'winx64', 'setupwinx64'],
    ['win-x86', 'winx86', 'setupwinx86'],
    ['win-arm64', 'winarm64', 'setupwinarm64'],
    ['linux-x64', 'linx64', 'setuplinx64'],
    ['linux-arm64', 'linarm64', 'setuplinarm64'],
    ['osx-x64', 'macosx64', 'setupmacosx64'],
    ['osx-arm64', 'macosarm64', 'setupmacosarm64']
  ];
  for (const [runtime, app, setup] of mappings) {
    const escaped = runtime.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const match = release.match(new RegExp(`"${escaped}"\\s*\\{\\s*return\\s+@\\{([^}]*)\\}`));
    assert.ok(match, runtime);
    for (const marker of [
      `AppAsset = "${app}.zip"`,
      `SetupAsset = "${setup}.zip"`,
      `AppFolder = "${app}"`,
      `SetupFolder = "${setup}"`
    ]) assert.ok(match[1].includes(marker), `${runtime}: ${marker}`);
  }
});

test('one-click installer requires exact latest-release assets for the current runtime', () => {
  const installer = read('src/PublisherStudio.InstallerConsole/Program.cs');
  assert.match(installer, /repos\/\{repo\}\/releases\/latest/);
  for (const [runtime, app] of [
    ['win-x64', 'winx64'], ['win-x86', 'winx86'], ['win-arm64', 'winarm64'],
    ['linux-x64', 'linx64'], ['linux-arm64', 'linarm64'],
    ['osx-x64', 'macosx64'], ['osx-arm64', 'macosarm64']
  ]) {
    assert.ok(installer.includes(`"${runtime}" => "${app}"`), runtime);
  }
  assert.match(installer, /GetExpectedReleaseAssetName\(runtimeIdentifier, setupAsset\)/);
  assert.match(installer, /string\.Equals\(name, expectedAssetName, StringComparison\.OrdinalIgnoreCase\)/);
  assert.match(installer, /Refusing to guess or deploy another runtime/);
  assert.doesNotMatch(installer, /Falling back to first matching setup mode/);
});

test('normal install and update preserve the product root unless force-delete is explicit', () => {
  const installer = read('src/PublisherStudio.InstallerConsole/Program.cs');
  assert.match(installer, /Path\.Combine\(localAppData, "PublisherStudio"\)/);
  assert.doesNotMatch(installer, /Programs[\\\\/]PublisherStudio|Path\.Combine\(localAppData, "Programs"/);
  assert.match(installer, /if \(options\.ForceDelete\)\s*DeleteIfExists\(targetPath, logger\);/);
  assert.match(installer, /ZipFile\.ExtractToDirectory\(zipPath, targetPath, overwriteFiles: true\)/);
  assert.match(installer, /Deleting existing path because --force-delete was used/);
  assert.match(installer, /Run again with --uninstall --force-delete/);

  const installMethod = installer.slice(
    installer.indexOf('private static async Task InstallPublisherStudioAsync'),
    installer.indexOf('private static void UninstallPublisherStudioWindows')
  );
  assert.equal((installMethod.match(/DeleteIfExists\(targetPath, logger\)/g) ?? []).length, 1);
  assert.match(installMethod, /if \(options\.ForceDelete\)/);
});

test('installer helper source comments are attached safely and no CS1587 marker remains', () => {
  for (const file of [
    'ColorConsoleLogger.cs',
    'ColorConsoleLoggerConfiguration.cs',
    'ColorConsoleLoggerProvider.cs'
  ]) {
    const source = read(`src/PublisherStudio.InstallerConsole/Helper/${file}`);
    assert.doesNotMatch(source, /^\/\/\/$/m);
    assert.match(source, /Based on the \.NET custom console logging sample/);
  }
});
