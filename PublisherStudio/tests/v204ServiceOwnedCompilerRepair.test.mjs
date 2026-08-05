import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';
import { fileURLToPath } from 'node:url';

const testDirectory = path.dirname(fileURLToPath(import.meta.url));
const repositoryRoot = path.resolve(testDirectory, '..');
const read = relativePath => fs.readFileSync(path.join(repositoryRoot, relativePath), 'utf8');

const allSourceFiles = directory => {
  const results = [];
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      if (['bin', 'obj', 'node_modules'].includes(entry.name)) continue;
      results.push(...allSourceFiles(fullPath));
    } else if (/\.(cs|razor)$/i.test(entry.name)) {
      results.push(fullPath);
    }
  }
  return results;
};

test('2.0.4 keeps data contracts in BusinessObjects and behavior in services', () => {
  const webRoot = path.join(repositoryRoot, 'src', 'PublisherStudio.Web');
  assert.equal(fs.existsSync(path.join(webRoot, 'Domain')), false);
  assert.equal(fs.existsSync(path.join(webRoot, 'BusinessObjects')), true);

  const sources = allSourceFiles(webRoot).map(file => fs.readFileSync(file, 'utf8')).join('\n');
  const businessObjectSources = allSourceFiles(path.join(webRoot, 'BusinessObjects'))
    .map(file => fs.readFileSync(file, 'utf8'))
    .join('\n');
  assert.doesNotMatch(businessObjectSources, /using\s+PublisherStudio\.Services(?:\.|;)/);
  assert.doesNotMatch(businessObjectSources, /\bILogger(?:<|\b)/);
  assert.doesNotMatch(sources, /namespace\s+PublisherStudio\.Domain\b/);
  assert.doesNotMatch(sources, /using\s+PublisherStudio\.Domain\s*;/);
  assert.doesNotMatch(sources, /PictureDocument\.(?:CreateDefault|FromRaster)\s*\(/);
  assert.doesNotMatch(sources, /PublicationDocument\.CreateDefault\s*\(/);
  assert.doesNotMatch(sources, /PublicationComponentService\.(?:ComponentName|Friendly)\s*\(/);
  assert.doesNotMatch(sources, /PagePreset\.(?:All|Find)\b/);
  assert.doesNotMatch(sources, /StoryPageLayout\.(?:Default|Normalize)\s*\(/);
  assert.doesNotMatch(sources, /PublicationFileService\.SafeFileName\s*\(/);
  assert.doesNotMatch(sources, /StreamingChatSendResult\.(?:NotFound|Accepted|NotConfigured|Failed)\b/);
});

test('2.0.4 resolves startup and Razor render mode through maintained host boundaries', () => {
  const program = read('src/PublisherStudio.Web/Program.cs');
  assert.match(program, /AddPublisherStudioApplication\(builder\.Configuration,\s*startupLogger\)/);
  assert.match(program, /CreateLogger\("PublisherStudio\.Startup"\)/);

  const imports = read('src/PublisherStudio.Web/Components/_Imports.razor');
  assert.equal((imports.match(/@using static Microsoft\.AspNetCore\.Components\.Web\.RenderMode/g) ?? []).length, 1);
  assert.equal((imports.match(/@using Microsoft\.AspNetCore\.Components\.Web\s*$/gm) ?? []).length, 1);
});

test('2.0.4 routes former static helpers through injected services and factories', () => {
  const editor = read('src/PublisherStudio.Web/Components/Editor/DevExtremeComponentEditor.razor');
  assert.match(editor, /@inject\s+PublicationComponentService\s+Components/);
  assert.match(editor, /Components\.ComponentName\(/);
  assert.match(editor, /Components\.Friendly\(/);

  const inspector = read('src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor');
  assert.match(inspector, /@inject\s+IPagePresetCatalog\s+PagePresets/);
  assert.match(inspector, /PagePresets\.GetAll\(\)/);
  assert.match(inspector, /PagePresets\.Find\(/);

  const registrations = read('src/PublisherStudio.Web/StreamingServiceCollectionExtensions.cs');
  for (const service of [
    'IStreamingChatResultFactory',
    'INativeCaptureSessionFactory',
    'IWindowsProcessLoopbackCaptureFactory',
    'IPlatformChatServiceFactory',
    'ILanStreamingServerFactory'
  ]) {
    assert.match(registrations, new RegExp(`AddSingleton<${service},`));
  }
});

test('2.0.4 validates release mappings semantically and keeps synchronized folder names', () => {
  const guard = read('build/Assert-PublishConfiguration.ps1');
  assert.match(guard, /\[Regex\]::Match\(\s*\$release/);
  assert.match(guard, /AppFolder/);
  assert.match(guard, /SetupFolder/);
  assert.match(guard, /SetupAsset/);

  const releaseScript = read('Build-Release.ps1');
  const windowsMapping = releaseScript.match(/"win-x64"\s*\{\s*@\{(?<body>[\s\S]*?)\}\s*\}/)?.groups?.body ?? '';
  assert.match(windowsMapping, /AppFolder\s*=\s*"winx64"/);
  assert.match(windowsMapping, /SetupFolder\s*=\s*"setupwinx64"/);
  assert.match(windowsMapping, /SetupAsset\s*=\s*"setupwinx64"/);
});

test('active version is aligned across release surfaces', () => {
  assert.match(read('src/PublisherStudio.Web/PublisherStudio.Web.csproj'), /<Version>2\.1\.1<\/Version>/);
  assert.match(read('src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'), /<Version>2\.1\.1<\/Version>/);
  assert.equal(JSON.parse(read('src/PublisherStudio.Web/package.json')).version, '2.1.1');
});
