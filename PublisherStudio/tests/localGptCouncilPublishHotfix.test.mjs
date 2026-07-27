import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = (...parts) => fs.readFileSync(path.join(root, ...parts), 'utf8');

const webProject = read('src', 'PublisherStudio.Web', 'PublisherStudio.Web.csproj');
const protocolProject = read('src', 'LocalGPT.WireProtocolVersion', 'LocalGPT.WireProtocolVersion.csproj');
const protocol = read('src', 'LocalGPT.WireProtocolVersion', 'OneWireProtocolContracts.cs');
const connection = read('src', 'PublisherStudio.Web', 'Services', 'OrganicPlugins', 'LocalGptConnectionService.cs');
const discovery = read('src', 'PublisherStudio.Web', 'HostedServices', 'OrganicPlugins', 'LocalGptDiscoveryHostedService.cs');
const release = read('Build-Release.ps1');
const settings = JSON.parse(read('src', 'PublisherStudio.Web', 'appsettings.json'));

assert.match(webProject, /ProjectReference Include="\.\.\\LocalGPT\.WireProtocolVersion\\LocalGPT\.WireProtocolVersion\.csproj"[\s\S]*?GlobalPropertiesToRemove="Platform;PlatformTarget;RuntimeIdentifier;RuntimeIdentifiers;SelfContained/);
assert.match(protocolProject, /<GeneratePackageOnBuild>false<\/GeneratePackageOnBuild>/);
assert.match(protocol, /MaximumDiscoveryBytes = 32 \* 1024/);
assert.match(discovery, /received\.Buffer\.Length > OrganicWireProtocol\.MaximumDiscoveryBytes/);
assert.match(connection, /case OrganicWireMessageType\.HelloAck:[\s\S]*?OrganicWireMessageType\.CapabilityRequest/);
assert.match(connection, /catch \(OperationCanceledException\) when \(cancellationToken\.IsCancellationRequested\)/);
assert.equal(settings.OrganicPlugins.MaximumMessageBytes, 8 * 1024 * 1024);
assert.match(release, /(?:dotnet pack \$wireProtocolProject|"pack", \$wireProtocolProject)/);
assert.match(release, /(?:-p:RuntimeIdentifier= -p:RuntimeIdentifiers=|"-p:RuntimeIdentifier=",[\s\S]*?"-p:RuntimeIdentifiers=")/);
assert.match(release, /WireProtocolPackageUrl = ""/);
assert.ok(!fs.existsSync(path.join(root, 'packages', 'LocalGPT.WireProtocolVersion.2.0.0.nupkg')), 'A stale source-only package must not shadow the project reference.');

console.log('PublisherStudio LocalGPT handshake and RID-safe source/publish contracts passed.');
