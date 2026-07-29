import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

test('PublisherStudio release uses the RID-neutral protocol package and explicit RID restore', () => {
  const release = read('Build-Release.ps1');
  assert.match(release, /WireProtocolVersion = "2\.1\.0"/);
  assert.match(release, /"restore", \$webProject, "-r", \$Runtime/);
  assert.doesNotMatch(release, /UseLocalWireProtocolProject/);
  assert.match(release, /Ensure-WireProtocolPackage\.ps1/);
  assert.match(release, /RestoreAdditionalProjectSources/);
  assert.match(release, /"publish", \$webProject/);
  assert.ok(fs.existsSync(path.join(root, 'Build-AllRuntimes.ps1')));
  const all = read('Build-AllRuntimes.ps1');
  for (const rid of ['win-x64','win-arm64','linux-x64','linux-arm64','osx-x64','osx-arm64']) assert.match(all, new RegExp(rid));
  const local = read('Build-LocalDevelopment.ps1');
  assert.match(local, /Ensure-WireProtocolPackage\.ps1/);
  assert.match(local, /--disable-parallel/);
  assert.match(local, /-maxcpucount:1/);
  assert.ok(local.indexOf('restore", $webProject') < local.indexOf('build", $webProject'));
});

test('PublisherStudio consumes only the authoritative LocalGPT NuGet contract', () => {
  const project = read('src/PublisherStudio.Web/PublisherStudio.Web.csproj');
  const solution = read('PublisherStudio.sln');
  const bootstrap = read('build/Ensure-WireProtocolPackage.ps1');
  assert.match(project, /PackageReference Include="LocalGPT\.WireProtocolVersion" Version="\$\(LocalGptWireProtocolVersion\)"/);
  assert.doesNotMatch(project, /ProjectReference[^>]*LocalGPT\.WireProtocolVersion/);
  assert.doesNotMatch(solution, /LocalGPT\.WireProtocolVersion/);
  assert.equal(fs.existsSync(path.join(root, 'src', 'LocalGPT.WireProtocolVersion')), false);
  assert.match(bootstrap, /releases\/latest\/download\/\$packageName/);
  assert.match(bootstrap, /lib\/net10\.0\/LocalGPT\.WireProtocolVersion\.dll/);
  const readme = read('README.md');
  assert.match(readme, /Optional LocalGPT organic wiring/);
  assert.match(readme, /no `src\/LocalGPT\.WireProtocolVersion` directory/);
  assert.match(readme, /organic adaptation system/);
});

test('Panel Studio arrange mode renders live content beneath a single interaction hit layer', () => {
  const studio = read('src/PublisherStudio.Web/Components/Editor/PanelStudio.razor');
  const view = read('src/PublisherStudio.Web/Components/Editor/PanelView.razor');
  const preview = read('src/PublisherStudio.Web/Components/Editor/PanelElementPreview.razor');
  const interop = read('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js');
  const css = read('src/PublisherStudio.Web/wwwroot/css/site.css');
  assert.match(studio, /DesignerMode="false" DesignPreviewOnly="false"/);
  assert.match(view, /if \(DesignPreviewOnly\)/);
  assert.match(view, /<PanelElementPreview Item="element"/);
  assert.match(preview, /case DataVisualElement visual:/);
  assert.doesNotMatch(interop, /panel-studio-hitbox\.selected'\)\.forEach\(node => node\.classList\.remove\('selected'\)\)/);
  assert.doesNotMatch(interop, /hitbox\.classList\.add\('selected'\)/);
  assert.match(css, /--publisher-z-tooltip: 2200/);
  assert.match(css, /panel-studio-dialog select\{appearance:none/);
});

test('every MVC controller action is covered by structured start, completion and exception logging', () => {
  const program = read('src/PublisherStudio.Web/Program.cs');
  const filter = read('src/PublisherStudio.Web/Diagnostics/ControllerRequestLoggingFilter.cs');
  assert.match(program, /Filters\.AddService<ControllerRequestLoggingFilter>/);
  assert.match(filter, /Controller action \{Controller\}\.\{Action\} started/);
  assert.match(filter, /LogError/);
  assert.match(filter, /catch \(OperationCanceledException/);
  assert.match(filter, /TryGetValue\("controller"/);
  assert.match(filter, /TryGetValue\("action"/);
  assert.doesNotMatch(filter, /GetValueOrDefault/);
});

