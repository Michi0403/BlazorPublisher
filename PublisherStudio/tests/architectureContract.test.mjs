import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const web = path.join(root, 'src', 'PublisherStudio.Web');
const read = (...parts) => fs.readFileSync(path.join(root, ...parts), 'utf8');

const agents = read('AGENTS.md');
const overview = read('docs', 'architecture', 'system-overview.md');
const streaming = read('docs', 'architecture', 'streaming.md');
const formats = read('docs', 'architecture', 'interchange-formats.md');
const program = read('src', 'PublisherStudio.Web', 'Program.cs');
const composition = read('src', 'PublisherStudio.Web', 'Services', 'Streaming', 'StreamingServiceCollectionExtensions.cs');
const mediaHostClient = read('src', 'PublisherStudio.Web', 'Services', 'Streaming', 'MediaHost', 'StreamingMediaHostClient.cs');

assert.match(agents, /UseCases.*subnamespace/s);
assert.match(agents, /Do not introduce competing top-level application patterns/);
assert.match(agents, /Main application routes belong to MVC controllers/);
assert.match(agents, /Monolith first/);
assert.match(overview, /UI\[Blazor Components\] --> C\[Controllers\]/);
assert.match(streaming, /former `StreamingRuntimeEndpoints` aggregation no longer exists/);
assert.match(formats, /OpenRaster/);
assert.match(formats, /OpenTimelineIO/);
assert.match(formats, /Broadcast WAV/);

for (const decision of [
  'ADR-001-monolith-first.md',
  'ADR-002-controller-entry-points.md',
  'ADR-003-usecase-subnamespaces.md',
  'ADR-004-native-and-interchange-formats.md'
]) assert.ok(fs.existsSync(path.join(root, 'docs', 'decisions', decision)), `${decision} is missing.`);

for (const directory of ['Backend', 'Controllers', 'HostedServices', 'Services'])
  assert.ok(fs.existsSync(path.join(web, directory)), `${directory} architectural root is missing.`);

for (const forbidden of ['Endpoints', 'Features', 'Handlers', 'Commands', 'Queries', 'UseCases', 'Infrastructure', 'Application'])
  assert.equal(fs.existsSync(path.join(web, forbidden)), false, `Forbidden top-level architectural root exists: ${forbidden}`);

const allFiles = [];
function walk(directory) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const full = path.join(directory, entry.name);
    if (entry.isDirectory()) walk(full);
    else allFiles.push(full);
  }
}
walk(web);
assert.equal(allFiles.some(file => /Endpoints\.cs$/i.test(file)), false, 'Endpoint aggregation files must not return.');
assert.equal(fs.existsSync(path.join(web, 'Services', 'StreamingRuntime')), false, 'The old StreamingRuntime service folder must not return.');

assert.match(program, /builder\.Services\.AddPublisherStreaming\(\)/);
assert.doesNotMatch(program, /MapPublisherStreamingRuntime|StreamingRuntimeEndpoints/);
assert.doesNotMatch(program, /app\.Map(?:Get|Post|Put|Delete)\(/);
assert.match(composition, /AddSingleton<StreamingSessionUseCases>/);
assert.match(composition, /AddSingleton<StreamingIngestUseCases>/);
assert.match(composition, /AddSingleton<GlobalHotkeyService>/);
assert.match(mediaHostClient, /StreamingRuntimeUseCases runtime/);
assert.match(mediaHostClient, /StreamingSessionUseCases sessions/);
assert.doesNotMatch(mediaHostClient, /MediaSessionRegistry sessions/);
assert.doesNotMatch(mediaHostClient, /NativeDeviceDiscovery\.DiscoverAsync/);

const controllerDir = path.join(web, 'Controllers', 'Streaming', 'UseCases');
const controllers = fs.readdirSync(controllerDir).filter(name => name.endsWith('Controller.cs'));
assert.deepEqual(controllers.sort(), [
  'NativeCaptureController.cs',
  'StreamingChatController.cs',
  'StreamingIngestController.cs',
  'StreamingLanController.cs',
  'StreamingRuntimeController.cs',
  'StreamingSessionController.cs'
]);
const controllerText = controllers.map(name => fs.readFileSync(path.join(controllerDir, name), 'utf8')).join('\n');
for (const route of [
  'api/mediahost/capabilities',
  'api/mediahost/devices',
  'api/mediahost/native-captures',
  'api/mediahost/sessions',
  'ingest/websocket',
  'webrtc/publisher',
  'stream/{sessionId:guid}/{**asset}',
  'watch/{sessionId:guid}'
]) assert.ok(controllerText.includes(route) || controllerText.includes(route.split('/').at(-1)), `Streaming route contract missing: ${route}`);

const backendFiles = allFiles.filter(file => file.includes(`${path.sep}Backend${path.sep}`) && file.endsWith('.cs'));
for (const file of backendFiles) {
  const text = fs.readFileSync(file, 'utf8');
  assert.doesNotMatch(text, /PublisherStudio\.Controllers|Microsoft\.AspNetCore\.Mvc/, `Backend depends on MVC/controller code: ${path.relative(root, file)}`);
}

console.log('PublisherStudio architecture contract checks passed.');
