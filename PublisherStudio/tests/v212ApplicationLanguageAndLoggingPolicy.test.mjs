import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';

const root = path.resolve(import.meta.dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

test('logging policy source required by the Windows guard is present and strong', () => {
  const policy = read('docs/LOGGING_INTEGRITY.md');
  const guard = read('build/Assert-LoggingIntegrity.ps1');
  assert.match(policy, /Logging removal is not cleanup/);
  assert.match(policy, /injected `ILogger<T>`/);
  assert.ok(guard.includes("docs\\LOGGING_INTEGRITY.md"));
  assert.match(guard, /Logging integrity policy is missing or was weakened/);
});

test('PublisherStudio application exposes the LocalGPT-style culture selector', () => {
  const layout = read('src/PublisherStudio.Web/Components/Layout/MainLayout.razor');
  const service = read('src/PublisherStudio.Web/Services/Configuration/FileLocalizationService.cs');
  const contract = read('src/PublisherStudio.Web/Services/Configuration/IApplicationConfigurationServices.cs');
  const program = read('src/PublisherStudio.Web/Program.cs');
  const controller = read('src/PublisherStudio.Web/Controllers/ConfigurationController.cs');
  assert.match(layout, /publisherstudio-language-select/);
  assert.match(layout, /BuildCultureSelectionUrl\(Navigation\.Uri, culture\)/);
  assert.match(layout, /GetCultureDisplayName\(culture\)/);
  assert.match(contract, /BuildCultureSelectionUrl/);
  assert.match(service, /QueryHelpers\.AddQueryString\("\/api\/configuration\/localization\/select"/);
  assert.match(program, /new QueryStringRequestCultureProvider/);
  assert.match(program, /new CookieRequestCultureProvider/);
  assert.match(controller, /BuildCultureRedirectUrl\(localReturnUrl, selected\)/);
  assert.match(controller, /ILogger<ConfigurationController>/);
  assert.match(controller, /Cache-Control/);
  assert.match(controller, /logger\.LogInformation/);
});

test('installer console remains dependency-light and has no language subsystem', () => {
  const project = read('src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj');
  const program = read('src/PublisherStudio.InstallerConsole/Program.cs');
  assert.doesNotMatch(project, /PublisherStudio\.Web|DevExpress|Localization/);
  assert.doesNotMatch(program, /InstallerLocalizationService|--change-language|--select-language/);
  assert.equal(fs.existsSync(path.join(root, 'src/PublisherStudio.InstallerConsole/Services/Localization')), false);
});


test('repository ownership rules keep data in BusinessObjects and localization out of setup', () => {
  const agents = read('AGENTS.md');
  assert.match(agents, /`BusinessObjects` for authoritative documents/);
  assert.match(agents, /application localization is owned by `IFileLocalizationService`/i);
  assert.match(agents, /installer console remains a dependency-light bootstrap application/i);
  assert.doesNotMatch(agents, /Domain \/ Models/);
});

test('2.1.7 application version advances without changing wire protocol 2.1.1', () => {
  assert.match(read('src/PublisherStudio.Web/PublisherStudio.Web.csproj'), /<Version>2\.1\.7<\/Version>/);
  assert.match(read('src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'), /<Version>2\.1\.7<\/Version>/);
  assert.equal(JSON.parse(read('src/PublisherStudio.Web/package.json')).version, '2.1.7');
  assert.match(read('Directory.Build.props'), /<LocalGptWireProtocolVersion>2\.1\.1<\/LocalGptWireProtocolVersion>/);
});
