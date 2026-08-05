import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = (...parts) => fs.readFileSync(path.join(root, ...parts), 'utf8');

const webProject = read('src', 'PublisherStudio.Web', 'PublisherStudio.Web.csproj');
const protocolBootstrap = read('build', 'Ensure-WireProtocolPackage.ps1');
const solution = read('PublisherStudio.sln');
const connection = read('src', 'PublisherStudio.Web', 'Services', 'OrganicPlugins', 'LocalGptConnectionService.cs');
const discovery = read('src', 'PublisherStudio.Web', 'HostedServices', 'OrganicPlugins', 'LocalGptDiscoveryHostedService.cs');
const release = read('Build-Release.ps1');
const settings = JSON.parse(read('src', 'PublisherStudio.Web', 'appsettings.json'));

assert.match(webProject, /PackageReference Include="LocalGPT\.WireProtocolVersion"/);
assert.doesNotMatch(webProject, /ProjectReference[^>]*LocalGPT\.WireProtocolVersion/);
assert.doesNotMatch(solution, /LocalGPT\.WireProtocolVersion/);
assert.equal(fs.existsSync(path.join(root, 'src', 'LocalGPT.WireProtocolVersion')), false);
assert.match(protocolBootstrap, /lib\/net10\.0\/LocalGPT\.WireProtocolVersion\.dll/);
assert.match(discovery, /received\.Buffer\.Length > OrganicWireProtocol\.MaximumDiscoveryBytes/);
assert.match(connection, /case OrganicWireMessageType\.HelloAck:[\s\S]*?OrganicWireMessageType\.CapabilityRequest/);
assert.match(connection, /catch \(OperationCanceledException\) when \(cancellationToken\.IsCancellationRequested\)/);
assert.equal(settings.OrganicPlugins.MaximumMessageBytes, 8 * 1024 * 1024);
assert.doesNotMatch(release, /dotnet pack|"pack"/);
assert.match(release, /Ensure-WireProtocolPackage\.ps1/);
assert.match(release, /WireProtocolPackageUrl = ""/);
assert.ok(!fs.existsSync(path.join(root, 'packages', 'LocalGPT.WireProtocolVersion.2.0.0.nupkg')), 'A stale source-only package must not shadow the project reference.');

console.log('PublisherStudio LocalGPT handshake and RID-safe source/publish contracts passed.');
