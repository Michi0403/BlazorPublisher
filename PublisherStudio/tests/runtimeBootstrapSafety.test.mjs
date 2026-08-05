import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const web = path.join(root, 'src', 'PublisherStudio.Web');
const read = (...parts) => fs.readFileSync(path.join(web, ...parts), 'utf8');

const program = read('Program.cs');
assert.match(program, /if \(requestedPort > 0\)\s*\n\s*options\.Listen\(IPAddress\.Loopback, requestedPort\);/);
assert.match(program, /options\.Limits\.MaxRequestBodySize = null;/);
assert.match(program, /endpointWriter\.DeleteOwnedEndpoint\(\);/);

const hostServices = read('Services', 'ApplicationHostServices.cs');
assert.match(hostServices, /void DeleteOwnedEndpoint\(\);/);
assert.match(hostServices, /processId\.TryGetInt32\(out var ownerProcessId\)/);

const launch = JSON.parse(read('Properties', 'launchSettings.json'));
assert.equal(launch.profiles['PublisherStudio.Web'].applicationUrl, 'http://127.0.0.1:58071');

const composition = read('PublisherStudioServiceCollectionExtensions.cs');
assert.match(composition, /if \(implementation is null\)\s*\n\s*return null;/);
assert.doesNotMatch(composition, /new ServiceArchitectureDescriptor\(contract, implementation!,/);

const architecture = read('Services', 'Automation', 'BusinessObjectContextService.cs');
assert.match(architecture, /ImplementationType = implementationType \?\? throw new ArgumentNullException/);
assert.match(architecture, /OrderBy\(descriptor => descriptor\.ImplementationType\.FullName \?\? descriptor\.ImplementationType\.Name, StringComparer\.Ordinal\)/);

const organic = read('Services', 'OrganicPlugins', 'OrganicCapabilityAndExecutionServices.cs');
assert.match(organic, /FFmpeg available: \{media\.Available\}/);
assert.doesNotMatch(organic, /media\.IsAvailable/);
assert.match(organic, /OrganicWireProtocol\.MaximumMessageBytes/);

const webProject = read('PublisherStudio.Web.csproj');
assert.match(webProject, /PackageReference Include="LocalGPT\.WireProtocolVersion"/);
assert.doesNotMatch(webProject, /ProjectReference[^>]*LocalGPT\.WireProtocolVersion/);
assert.equal(fs.existsSync(path.join(root, 'src', 'LocalGPT.WireProtocolVersion')), false);
assert.match(organic, /OrganicWireProtocol\.MaximumMessageBytes/);
const releaseScript = fs.readFileSync(path.join(root, 'Build-Release.ps1'), 'utf8');
assert.match(releaseScript, /Ensure-WireProtocolPackage\.ps1/);
assert.doesNotMatch(releaseScript, /dotnet pack|"pack"/);
assert.match(releaseScript, /--disable-parallel/);
assert.match(releaseScript, /Copy-Item -LiteralPath \$wireProtocolPackage -Destination \(Join-Path \$protocolAppDirectory \$wireProtocolPackageName\) -Force/);
assert.match(releaseScript, /Copy-Item -LiteralPath \$wireProtocolPackage -Destination \(Join-Path \$protocolSetupDirectory \$wireProtocolPackageName\) -Force/);

console.log('PublisherStudio runtime/bootstrap safety contracts passed.');
