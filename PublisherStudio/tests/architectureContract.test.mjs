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
const composition = read('src', 'PublisherStudio.Web', 'StreamingServiceCollectionExtensions.cs');
const mediaHostClient = read('src', 'PublisherStudio.Web', 'Services', 'Streaming', 'MediaHost', 'StreamingMediaHostClient.cs');
const hotkeyService = read('src', 'PublisherStudio.Web', 'Services', 'Streaming', 'Hotkeys', 'GlobalHotkeyService.cs');
const hotkeyHostedService = read('src', 'PublisherStudio.Web', 'HostedServices', 'Streaming', 'GlobalHotkeyHostedService.cs');
const chatHub = read('src', 'PublisherStudio.Web', 'Hubs', 'Streaming', 'Chat', 'PlatformChatHub.cs');
const webRtcHub = read('src', 'PublisherStudio.Web', 'Hubs', 'Streaming', 'Lan', 'WebRtcSignalingHub.cs');
const chatService = read('src', 'PublisherStudio.Web', 'Services', 'Streaming', 'Chat', 'PlatformChatService.cs');
const webRtcService = read('src', 'PublisherStudio.Web', 'Services', 'Streaming', 'Lan', 'WebRtcSignalingService.cs');

assert.match(agents, /Controllers.*start.*backend/is);
assert.match(agents, /Hubs.*persistent.*connection/is);
assert.match(agents, /Services.*reusable.*Components.*Controllers.*Hubs.*HostedServices/is);
assert.match(agents, /data processing.*technical I\/O/is);
assert.match(agents, /UseCases.*subnamespace/s);
assert.match(agents, /Do not introduce competing top-level application patterns/);
assert.match(agents, /Monolith first/);
assert.doesNotMatch(agents, /`Backend` for/);
assert.match(overview, /C\[Controllers: backend request entry\]/);
assert.match(overview, /HB\[Hubs: persistent connection entry\]/);
assert.match(overview, /UI\[Blazor Components\] --> S\[Reusable Services/);
assert.match(streaming, /no separate `Backend` architectural root/);
assert.match(streaming, /PlatformChatHub.*Hubs\/Streaming/s);
assert.match(formats, /OpenRaster/);
assert.match(formats, /OpenTimelineIO/);
assert.match(formats, /Broadcast WAV/);

for (const decision of [
  'ADR-001-monolith-first.md',
  'ADR-002-controller-and-hub-entry-points.md',
  'ADR-003-usecase-subnamespaces.md',
  'ADR-004-native-and-interchange-formats.md',
  'ADR-005-services-own-reusable-processing-and-io.md'
]) assert.ok(fs.existsSync(path.join(root, 'docs', 'decisions', decision)), `${decision} is missing.`);

for (const directory of ['Components', 'Controllers', 'Hubs', 'HostedServices', 'Services'])
  assert.ok(fs.existsSync(path.join(web, directory)), `${directory} architectural root is missing.`);

for (const forbidden of ['Backend', 'Endpoints', 'Features', 'Handlers', 'Commands', 'Queries', 'UseCases', 'Infrastructure', 'Application'])
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

for (const file of allFiles.filter(file => file.endsWith('.cs'))) {
  const text = fs.readFileSync(file, 'utf8');
  assert.doesNotMatch(text, /namespace PublisherStudio\.Backend|using PublisherStudio\.Backend/, `Removed Backend namespace returned: ${path.relative(root, file)}`);
}

for (const file of allFiles.filter(file => file.endsWith('.cs') && !file.includes(`${path.sep}Controllers${path.sep}`))) {
  const text = fs.readFileSync(file, 'utf8');
  assert.doesNotMatch(text, /(?:public|internal)\s+(?:sealed\s+|static\s+|abstract\s+)?class\s+\w*Controller\b/, `Controller-named implementation exists outside Controllers: ${path.relative(root, file)}`);
}
for (const file of allFiles.filter(file => file.endsWith('.cs') && !file.includes(`${path.sep}Hubs${path.sep}`))) {
  const text = fs.readFileSync(file, 'utf8');
  assert.doesNotMatch(text, /(?:public|internal)\s+(?:sealed\s+|static\s+|abstract\s+)?class\s+\w*Hub\b/, `Hub-named implementation exists outside Hubs: ${path.relative(root, file)}`);
}

const serviceFiles = allFiles.filter(file => file.includes(`${path.sep}Services${path.sep}`) && file.endsWith('.cs'));
for (const file of serviceFiles) {
  const text = fs.readFileSync(file, 'utf8');
  assert.doesNotMatch(text, /PublisherStudio\.(?:Controllers|Hubs|HostedServices|Components)/, `Reusable service depends on an entry/lifecycle/UI root: ${path.relative(root, file)}`);
  assert.doesNotMatch(text, /Microsoft\.AspNetCore\.Mvc/, `Reusable service depends on MVC: ${path.relative(root, file)}`);
}

assert.match(program, /builder\.Services\.AddPublisherStreaming\(\)/);
assert.doesNotMatch(program, /MapPublisherStreamingRuntime|StreamingRuntimeEndpoints/);
assert.doesNotMatch(program, /app\.Map(?:Get|Post|Put|Delete)\(/);
assert.match(composition, /namespace PublisherStudio;/);
assert.match(composition, /AddSingleton<GlobalHotkeyService>/);
assert.match(composition, /AddHostedService<GlobalHotkeyHostedService>/);
assert.match(composition, /AddSingleton<PlatformChatHub>/);
assert.match(composition, /AddSingleton<WebRtcSignalingHub>/);
assert.match(composition, /AddSingleton<StreamingSessionUseCases>/);
assert.match(composition, /AddSingleton<StreamingIngestUseCases>/);
assert.match(mediaHostClient, /StreamingRuntimeUseCases runtime/);
assert.match(mediaHostClient, /StreamingSessionUseCases sessions/);
assert.doesNotMatch(mediaHostClient, /MediaSessionRegistry sessions/);
assert.doesNotMatch(mediaHostClient, /NativeDeviceDiscovery\.DiscoverAsync/);

assert.match(hotkeyService, /namespace PublisherStudio\.Services\.Streaming\.Hotkeys/);
assert.doesNotMatch(hotkeyService, /IHostedService/);
assert.match(hotkeyHostedService, /namespace PublisherStudio\.HostedServices\.Streaming/);
assert.match(hotkeyHostedService, /GlobalHotkeyService hotkeys/);
assert.match(hotkeyHostedService, /hotkeys\.StartAsync/);
assert.match(hotkeyHostedService, /hotkeys\.StopAsync/);
assert.match(chatHub, /namespace PublisherStudio\.Hubs\.Streaming\.Chat/);
assert.match(chatHub, /StreamingChatUseCases/);
assert.match(webRtcHub, /namespace PublisherStudio\.Hubs\.Streaming\.Lan/);
assert.match(webRtcHub, /StreamingIngestUseCases/);
assert.match(chatService, /namespace PublisherStudio\.Services\.Streaming\.Chat/);
assert.match(chatService, /class PlatformChatService/);
assert.match(webRtcService, /namespace PublisherStudio\.Services\.Streaming\.Lan/);
assert.match(webRtcService, /class WebRtcSignalingService/);

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
assert.match(controllerText, /PlatformChatHub/);
assert.match(controllerText, /WebRtcSignalingHub/);

for (const pathName of [
  ['Services', 'Streaming', 'Capture', 'NativeCaptureRegistry.cs'],
  ['Services', 'Streaming', 'Encoding', 'EncoderOrchestrator.cs'],
  ['Services', 'Streaming', 'Encoding', 'FfmpegLocator.cs'],
  ['Services', 'Streaming', 'Lan', 'LanStreamingServer.cs'],
  ['Services', 'Streaming', 'Lan', 'RtspLanServer.cs'],
  ['Services', 'Streaming', 'Metadata', 'NowPlayingReader.cs'],
  ['Services', 'Streaming', 'Sessions', 'MediaSession.cs']
]) assert.ok(fs.existsSync(path.join(web, ...pathName)), `Streaming service is misplaced: ${pathName.join('/')}`);

console.log('PublisherStudio architecture contract checks passed.');
