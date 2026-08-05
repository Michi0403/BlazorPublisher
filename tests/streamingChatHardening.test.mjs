import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');
const model = read('src/PublisherStudio.Web/BusinessObjects/PublicationComponentModels.cs');
const service = read('src/PublisherStudio.Web/Services/PublicationComponentService.cs');
const editor = read('src/PublisherStudio.Web/Components/Editor/DevExtremeComponentEditor.razor');
const panels = read('src/PublisherStudio.Web/Services/Panels/PanelDocumentService.cs');
const runtime = read('src/PublisherStudio.Web/wwwroot/js/componentRuntime.js');
const streaming = read('src/PublisherStudio.Web/wwwroot/js/streamingInterop.js');
const css = read('src/PublisherStudio.Web/wwwroot/css/site.css');
const exporter = read('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js');
const project = read('src/PublisherStudio.Web/PublisherStudio.Web.csproj');
const packageJson = JSON.parse(read('src/PublisherStudio.Web/package.json'));

for (const contract of [
  'PublicationChatDisplayMode', 'Auto', 'Interactive', 'ViewerOnly', 'StreamOverlay',
  'ChatMaxVisibleMessages', 'ChatCompact', 'ChatFadeOlderMessages',
  'ChatShowPlatformBadge', 'ChatBackgroundOpacity', 'ChatMessageOpacity'
]) assert.ok(model.includes(contract), `${contract} is missing from the chat model.`);

assert.match(service, /ChatMaxVisibleMessages = Math\.Clamp/);
assert.match(service, /ChatBackgroundOpacity = Math\.Clamp/);
assert.match(service, /chatDisplayMode = item\.ChatDisplayMode\.ToString\(\)/);
assert.match(editor, /Display mode/);
assert.match(editor, /Maximum visible messages/);
assert.match(editor, /ViewerOnly and StreamOverlay never render a message input/);

assert.match(panels, /ChatDisplayMode = PublicationChatDisplayMode\.StreamOverlay/);
assert.match(panels, /ChatAllowSending = false/);

assert.match(runtime, /function chatDisplayMode\(/);
assert.match(runtime, /if \(chatBroadcastMode\(\)\) return "streamoverlay"/);
assert.match(runtime, /function renderChatOverlayContent\(/);
assert.match(runtime, /aria-live", "polite"/);
assert.match(runtime, /config\.kind === "Chat" && chatDisplayMode\(config\) !== "interactive"/);
assert.match(runtime, /const nativeChatOverlay = config\.kind === "Chat"/);
assert.match(runtime, /chatSafeText/);
assert.match(runtime, /chatMaximumMessages/);
assert.match(runtime, /ViewerOnly|vieweronly/);

assert.match(css, /ps-chat-mode-streamoverlay \.ps-stream-chat \{ pointer-events:none; \}/);
assert.match(css, /ps-stream-chat-message/);
assert.match(css, /overflow-wrap:anywhere/);
assert.match(css, /--ps-chat-background-opacity/);

assert.match(streaming, /function drawBroadcastChatLayer/);
assert.match(streaming, /maxVisibleMessages/);
assert.match(streaming, /showAvatar/);
assert.match(streaming, /showTimestamp/);
assert.match(streaming, /authorColor/);
assert.match(streaming, /for \(const character of token\)/, 'Long unbroken chat tokens must be split safely for canvas rendering.');
assert.match(streaming, /publisherstream-base-capture/);

assert.match(exporter, /componentRuntimeSource/);
assert.match(exporter, /js\/componentRuntime\.js/);
assert.doesNotMatch(project, /DevExpress\.AIIntegration\.Blazor\.Chat|DxAIChat/);
assert.equal(packageJson.scripts['test:streaming-chat-hardening'], 'node ../../tests/streamingChatHardening.test.mjs');

console.log('Streaming chat hardening contracts passed.');
