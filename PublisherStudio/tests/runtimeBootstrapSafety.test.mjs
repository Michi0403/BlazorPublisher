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
assert.equal(launch.profiles['PublisherStudio.Web'].applicationUrl, 'http://127.0.0.1:5198');

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
assert.match(webProject, /ProjectReference Include="\.\.\\LocalGPT\.WireProtocolVersion\\LocalGPT\.WireProtocolVersion\.csproj"[\s\S]*?GlobalPropertiesToRemove=/);
assert.ok(fs.existsSync(path.join(root, 'src', 'LocalGPT.WireProtocolVersion', 'LocalGPT.WireProtocolVersion.csproj')));
assert.match(organic, /OrganicWireProtocol\.MaximumMessageBytes/);
const releaseScript = fs.readFileSync(path.join(root, 'Build-Release.ps1'), 'utf8');
assert.match(releaseScript, /Packing the synchronized LocalGPT 1-Wire protocol project without an application RID/);
assert.match(releaseScript, /(?:dotnet pack \$wireProtocolProject|"pack", \$wireProtocolProject)/);
assert.match(releaseScript, /(?:-p:RuntimeIdentifier= -p:RuntimeIdentifiers=|"-p:RuntimeIdentifier=",[\s\S]*?"-p:RuntimeIdentifiers=")/);
assert.match(releaseScript, /Copy-Item \$wireProtocolPackage \(Join-Path \$protocolAppDirectory \$wireProtocolPackageName\)/);
assert.match(releaseScript, /Copy-Item \$wireProtocolPackage \(Join-Path \$protocolSetupDirectory \$wireProtocolPackageName\)/);

console.log('PublisherStudio runtime/bootstrap safety contracts passed.');
