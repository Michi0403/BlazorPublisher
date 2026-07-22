import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const runtimePath = path.join(root, 'src', 'PublisherStudio.Web', 'wwwroot', 'js', 'streamingInterop.js');
const runtime = fs.readFileSync(runtimePath, 'utf8');
const model = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Domain', 'PublicationStreamingModels.cs'), 'utf8');
const editor = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Components', 'Pages', 'Editor.razor'), 'utf8');
const studio = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Components', 'Editor', 'StreamingStudio.razor'), 'utf8');
const mediaHost = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Services', 'StreamingRuntime', 'StreamingRuntimeEndpoints.cs'), 'utf8');
const encoder = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Services', 'StreamingRuntime', 'EncoderOrchestrator.cs'), 'utf8');
const lanServer = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Services', 'StreamingRuntime', 'LanStreamingServer.cs'), 'utf8');
const nativeCapture = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Services', 'StreamingRuntime', 'NativeCaptureRegistry.cs'), 'utf8');
const nativeDevices = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Services', 'StreamingRuntime', 'NativeDeviceDiscovery.cs'), 'utf8');
const platformChat = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Services', 'StreamingRuntime', 'PlatformChatHub.cs'), 'utf8');
const rtspServer = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Services', 'StreamingRuntime', 'RtspLanServer.cs'), 'utf8');
const webRtcHub = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Services', 'StreamingRuntime', 'WebRtcSignalingHub.cs'), 'utf8');
const processLoopback = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Services', 'StreamingRuntime', 'WindowsProcessLoopbackCapture.cs'), 'utf8');
const webProgram = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Program.cs'), 'utf8');
const inspector = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Components', 'Editor', 'InspectorPanel.razor'), 'utf8');
const profileStore = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Services', 'StreamingProfileStore.cs'), 'utf8');
const twitchOAuth = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Services', 'TwitchOAuthService.cs'), 'utf8');
const componentRuntime = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'wwwroot', 'js', 'componentRuntime.js'), 'utf8');
const exporter = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'wwwroot', 'js', 'publisherInterop.js'), 'utf8');
const streamingClient = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Services', 'StreamingMediaHostClient.cs'), 'utf8');
const ffmpegLocator = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Services', 'StreamingRuntime', 'FfmpegLocator.cs'), 'utf8');
const ffmpegProvisioner = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.InstallerConsole', 'FfmpegProvisioner.cs'), 'utf8');
const installerProgram = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.InstallerConsole', 'Program.cs'), 'utf8');
const buildRelease = fs.readFileSync(path.join(root, 'Build-Release.ps1'), 'utf8');
const solution = fs.readFileSync(path.join(root, 'PublisherStudio.sln'), 'utf8');

for (const contract of [
  'PublicationStreamingSettings', 'PublicationStreamOutput', 'PublicationRecordingSettings',
  'PublicationLanStreamingSettings', 'PublicationStreamingHotkey', 'LiveSourceElement',
  'ChromaResidualOpacity', 'PreferDeviceTimestamps', 'CaptureWidth', 'CaptureFrameRate', 'StreamingHardwareEncoderPreference',
  'ChatEnabled', 'ChatSecret', 'HasStoredChatSecret', 'StreamingProviderAuthenticationMode',
  'HasStoredOAuthSession', 'OAuthClientId', 'OAuthScopes', 'AutoSelectIngest', 'TwitchIngestCandidate'
]) assert.ok(model.includes(contract), `${contract} is missing from the streaming model.`);

assert.match(studio, /Providers/);
assert.match(studio, /Company \/ LAN/);
assert.match(studio, /Streaming hotkeys/);
assert.match(studio, /Twitch web login \(OAuth\)/);
assert.match(studio, /ConnectTwitchOAuth/);
assert.match(studio, /Test Twitch endpoints/);
assert.match(studio, /Automatically choose the best Twitch ingest/);
assert.match(studio, /Global hotkeys are registered by PublisherStudio on Windows/);
assert.match(editor, /prepareProgramCapture/);
assert.match(editor, /startProgramIngest/);
assert.match(editor, /masterQualityBitrate/);
assert.match(runtime, /connectAllProgramAudio/);
assert.match(runtime, /windowAudio/);
assert.match(runtime, /captureFrameRate/);
assert.match(runtime, /configureChatBridge/);
assert.match(runtime, /PublisherStudioChatBridge/);
assert.match(runtime, /reserveExternalAuthorizationWindow/);
assert.match(runtime, /navigateExternalAuthorizationWindow/);
assert.match(runtime, /closeExternalAuthorizationWindow/);
assert.match(runtime, /publisherstream-base-capture/);
assert.match(runtime, /captureRequired !== false/);
assert.match(runtime, /getBroadcastLayers/);
assert.match(editor, /captureRequired/);
assert.match(editor, /SelectedOutputIds\.Contains/);
assert.match(editor, /HandleStreamingHotkey/);
assert.match(mediaHost, /RegisterHotKey/);
assert.match(mediaHost, /ingest\/websocket/);
assert.match(mediaHost, /NowPlayingReader/);
assert.match(mediaHost, /LanStreamingServer/);
assert.match(mediaHost, /enabledProperty/);
assert.match(mediaHost, /chat\/\{outputId:guid\}\/websocket/);
assert.match(mediaHost, /chat\/\{outputId:guid\}\/send/);
assert.match(mediaHost, /PlatformChatHub/);
assert.match(platformChat, /TwitchIrcChatAdapter/);
assert.match(platformChat, /YouTubeLiveChatAdapter/);
assert.match(platformChat, /PRIVMSG/);
assert.match(platformChat, /liveChat\/messages/);
assert.equal((platformChat.match(/public async ValueTask DisposeAsync\(\)/g) || []).length, 3, 'Each Chat hub/adapter must expose one disposal method.');
assert.match(nativeCapture, /wasapi-process-loopback/);
assert.match(nativeCapture, /WindowsProcessLoopbackCapture/);
assert.match(nativeDevices, /DiscoverWindowsProcesses/);
assert.match(nativeDevices, /record DiscoveredNativeMediaDeviceInfo/);
assert.doesNotMatch(nativeDevices, /record NativeMediaDeviceInfo/);
assert.match(streamingClient, /Task<List<PublisherStudio\.Domain\.NativeMediaDeviceInfo>>/);
assert.match(streamingClient, /Select\(device => new PublisherStudio\.Domain\.NativeMediaDeviceInfo/);
assert.match(processLoopback, /ActivateAudioInterfaceAsync/);
assert.ok(processLoopback.includes('VAD\\\\Process_Loopback'));
assert.match(rtspServer, /RTP\/AVP\/TCP/);
assert.match(rtspServer, /rtpmap:33 MP2T\/90000/);
assert.match(webRtcHub, /viewer-offer/);
assert.match(webRtcHub, /publisher-answer/);
assert.match(encoder, /bandwidthtest=true/);
assert.match(encoder, /\{stream_key\}/);
assert.match(encoder, /\{streamKey\}/);
assert.match(encoder, /-f", "segment/);
assert.match(encoder, /delete_segments\+append_list/);
assert.match(encoder, /LanDefinition\.EnableHls/);
assert.match(encoder, /RecommendedRecordingBitrateKbps/);
assert.match(encoder, /libvpx-vp9/);
assert.match(encoder, /FfmpegEncoderResolver/);
assert.match(encoder, /h264_nvenc/);
assert.match(encoder, /ScheduleRecordingRemux/);
assert.match(encoder, /ScheduleRestart/);
assert.match(encoder, /StandardInput\.Close/);
assert.equal((encoder.match(/var known = new\[\]/g) || []).length, 1, 'The FFmpeg encoder probe must declare its known encoder list exactly once.');
assert.match(studio, /HardwareEncoder/);
assert.match(studio, /Reusable device profiles/);
assert.match(studio, /SaveBrowserDevice/);
assert.match(studio, /_draft\.Lan\.VideoBitrateKbps/);
assert.match(studio, /EstimatedRecordingStorageText/);
assert.match(inspector, /ChangeLiveDeviceProfile/);
assert.match(profileStore, /UseDeviceTimestamps/);
assert.match(profileStore, /ProtectedChatSecret/);
assert.match(profileStore, /ResolveChatSecretAsync/);
assert.match(profileStore, /ProtectedOAuthAccessToken/);
assert.match(profileStore, /ProtectedOAuthRefreshToken/);
assert.match(profileStore, /SaveTwitchOAuthConnectionAsync/);
assert.match(profileStore, /retainOAuthSession/);
assert.match(lanServer, /MediaSource/);
assert.match(lanServer, /SubscribeIngest/);
assert.match(lanServer, /RequireAccessToken/);
assert.match(lanServer, /href=\\"/);
assert.match(lanServer, /try \{ sourceBuffer\.mode/);
assert.match(webProgram, /ProtectKeysWithDpapi/);
assert.match(webProgram, /AddPublisherStreamingRuntime/);
assert.match(webProgram, /MapPublisherStreamingRuntime/);
assert.match(webProgram, /AddHostedService<TwitchOAuthMaintenanceService>/);
assert.match(mediaHost, /public static class PublisherStreamingRuntimeExtensions/);
assert.match(mediaHost, /version = "1\.0\.53"/);
assert.match(streamingClient, /In-process facade/);
assert.match(streamingClient, /EnsureValidAccessTokenAsync/);
assert.doesNotMatch(streamingClient, /HttpClient/);
assert.match(runtime, /return value \|\| window\.location\.origin/);
assert.match(ffmpegLocator, /AppContext\.BaseDirectory/);
assert.match(ffmpegProvisioner, /Gyan\.FFmpeg/);
assert.match(ffmpegProvisioner, /brew/);
assert.match(ffmpegProvisioner, /apt-get/);
assert.match(installerProgram, /RemoveLegacyMediaHostPayload\(targetPath, logger\)/);
assert.match(installerProgram, /PublisherStudio\.MediaHost\*/);
assert.doesNotMatch(buildRelease, /PublisherStudio\.MediaHost/);
assert.doesNotMatch(solution, /PublisherStudio\.MediaHost|PublisherStudio\.StreamingRuntime/);
assert.match(componentRuntime, /PublisherStudioOutputContext/);
assert.match(componentRuntime, /PublisherStudioOutputContext\?\.mode/);
assert.match(componentRuntime, /chatBroadcastMode/);
assert.match(exporter, /publisherOutputMode/);
assert.match(exporter, /publisherChatPlatform/);

assert.match(twitchOAuth, /https:\/\/id\.twitch\.tv\/oauth2\/device/);
assert.match(twitchOAuth, /urn:ietf:params:oauth:grant-type:device_code/);
assert.match(twitchOAuth, /channel:read:stream_key/);
assert.match(twitchOAuth, /chat:read chat:edit/);
assert.match(twitchOAuth, /https:\/\/api\.twitch\.tv\/helix\/streams\/key/);
assert.match(twitchOAuth, /https:\/\/ingest\.twitch\.tv\/ingests/);
assert.match(twitchOAuth, /https:\/\/id\.twitch\.tv\/oauth2\/validate/);
assert.match(twitchOAuth, /https:\/\/id\.twitch\.tv\/oauth2\/revoke/);
assert.match(twitchOAuth, /grant_type.*refresh_token/s);
assert.match(twitchOAuth, /TimeSpan\.FromHours\(1\)/);
assert.match(twitchOAuth, /MeasureTcpLatencyAsync/);
assert.match(twitchOAuth, /1935/);
assert.match(twitchOAuth, /forceValidation: true/);

const listeners = new Map();
const dispatched = [];
class MockElement { closest() { return null; } }
const popupWindows = new Map();
const windowObject = {
  open(url = '', name = '') {
    const popup = {
      closed: false,
      document: { title: '', body: { textContent: '' } },
      location: { href: String(url) },
      focus() {},
      close() { this.closed = true; }
    };
    popupWindows.set(String(name), popup);
    return popup;
  },
  addEventListener(type, callback) { listeners.set(type, callback); },
  removeEventListener(type, callback) { if (listeners.get(type) === callback) listeners.delete(type); },
  dispatchEvent(event) { dispatched.push(event); },
  setInterval,
  clearInterval,
  PublisherStudioOutputContext: null
};
const context = {
  window: windowObject,
  document: {
    getElementById() { return null; },
    documentElement: { clientWidth: 1920, clientHeight: 1080 }
  },
  navigator: {},
  Element: MockElement,
  CustomEvent: class { constructor(type, options = {}) { this.type = type; this.detail = options.detail; } },
  WebSocket: class {},
  MediaRecorder: class {},
  AudioContext: class {},
  requestAnimationFrame() { return 1; },
  cancelAnimationFrame() {},
  fetch,
  console,
  setInterval,
  clearInterval,
  setTimeout,
  clearTimeout
};
context.globalThis = context;
vm.runInNewContext(runtime, context, { filename: runtimePath });

assert.equal(context.window.publisherStreaming.reserveExternalAuthorizationWindow('twitch-oauth'), true);
assert.equal(context.window.publisherStreaming.navigateExternalAuthorizationWindow('twitch-oauth', 'https://www.twitch.tv/activate'), true);
assert.equal(popupWindows.get('twitch-oauth').location.href, 'https://www.twitch.tv/activate');
assert.equal(context.window.publisherStreaming.closeExternalAuthorizationWindow('twitch-oauth'), true);
assert.equal(popupWindows.get('twitch-oauth').closed, true);

context.window.publisherStreaming.setOutputContext({ mode: 'broadcast', platform: 'Twitch', channel: 'channel-a', outputId: 'out-a' });
assert.equal(context.window.PublisherStudioOutputContext.platform, 'Twitch');
assert.equal(context.window.PublisherStudioOutputContext.mode, 'broadcast');
assert.equal(dispatched.at(-1).type, 'publisherstudio:output-context-changed');

const calls = [];
const dotnet = { invokeMethodAsync(name, command, targetId) { calls.push({ name, command, targetId }); return Promise.resolve(); } };
context.window.publisherStreaming.bindHotkeys([
  { gesture: 'F9', command: 'ToggleStreaming', targetId: null, global: false },
  { gesture: 'F10', command: 'ToggleRecording', targetId: null, global: true }
], dotnet);
const keydown = listeners.get('keydown');
assert.equal(typeof keydown, 'function');
let prevented = false;
keydown({ key: 'F9', repeat: false, isComposing: false, ctrlKey: false, altKey: false, shiftKey: false, metaKey: false, target: null, preventDefault() { prevented = true; }, stopPropagation() {} });
await Promise.resolve();
assert.equal(prevented, true);
assert.deepEqual(calls, [{ name: 'HandleStreamingHotkey', command: 'ToggleStreaming', targetId: null }]);
keydown({ key: 'F10', repeat: false, isComposing: false, ctrlKey: false, altKey: false, shiftKey: false, metaKey: false, target: null, preventDefault() {}, stopPropagation() {} });
await Promise.resolve();
assert.equal(calls.length, 1, 'Global hotkeys must be owned by the integrated runtime, not double-fired by the browser.');
context.window.publisherStreaming.unbindHotkeys();
assert.equal(listeners.has('keydown'), false);

console.log('streaming model, Twitch OAuth/ingest, per-output Chat compositor, native capture, FFmpeg orchestration, WebRTC/RTSP LAN delivery, hotkey, and Now Playing contract tests passed');
