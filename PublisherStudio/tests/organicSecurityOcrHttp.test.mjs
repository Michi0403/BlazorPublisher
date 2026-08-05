import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = rel => fs.readFileSync(path.join(root, rel), 'utf8');

assert.equal(fs.existsSync(path.join(root, 'src', 'LocalGPT.WireProtocolVersion')), false, 'PublisherStudio must not revision protocol source.');
const csproj = read('src/PublisherStudio.Web/PublisherStudio.Web.csproj');
assert.match(csproj, /PackageReference Include="LocalGPT\.WireProtocolVersion"/);
assert.doesNotMatch(csproj, /ProjectReference[^>]+WireProtocol/);
assert.ok(read('Directory.Build.props').includes('<LocalGptWireProtocolVersion>2.1.1</LocalGptWireProtocolVersion>'));
const localBuild = read('Build-LocalDevelopment.ps1');
for (const token of ['--force-evaluate', '-maxcpucount:1', 'Ensure-WireProtocolPackage.ps1']) assert.ok(localBuild.includes(token), token);
const packageBootstrap = read('build/Ensure-WireProtocolPackage.ps1');
for (const token of ['System.Threading.Mutex', 'Test-WireProtocolPackage', 'lib/net10.0/LocalGPT.WireProtocolVersion.dll']) assert.ok(packageBootstrap.includes(token), token);
const buildCmd = read('Build-LocalDevelopment.cmd');
assert.ok(buildCmd.includes('pushd "%~dp0"'));
assert.ok(buildCmd.includes('pause'));

const security = read('src/PublisherStudio.Web/Services/OrganicPlugins/OrganicRuntimeSecurityService.cs');
for (const token of ['onewire-secret.json', 'RandomNumberGenerator.GetBytes', 'RegenerateAsync', 'DeleteAsync', 'AesGcm', 'ECDiffieHellman', 'VerifyTotp', 'HkdfSha256', 'orderedPeers', 'orderedFingerprints']) assert.ok(security.includes(token), token);
const panel = read('src/PublisherStudio.Web/Components/OrganicPlugins/OrganicSecurityPanel.razor');
for (const token of ['Create identity', 'Regenerate identity', 'Delete identity', 'Authenticator enrollment', 'Public pairing ticket']) assert.ok(panel.includes(token), token);

const connection = read('src/PublisherStudio.Web/Services/OrganicPlugins/LocalGptConnectionService.cs');
assert.match(connection, /return envelope\.CorrelationId;/, 'Callers must wait on the exact correlation ID.');
assert.doesNotMatch(connection, /responseCache\[envelope\.CorrelationId\].*TrySetResult/s, 'An active waiter must own an intermediate response instead of receiving a stale cached approval twice.');

const coordinator = read('src/PublisherStudio.Web/Services/OrganicPlugins/OrganicCapabilityAndExecutionServices.cs');
assert.ok(coordinator.includes('requiresFreshCaptureConsent'));
assert.ok(coordinator.includes('OrganicApprovalMode.AskEveryTime'));
const capture = read('src/PublisherStudio.Web/wwwroot/js/secureCaptureInterop.js');
assert.ok(capture.includes('getDisplayMedia'));

const picture = read('src/PublisherStudio.Web/Components/Editor/PictureEditor.razor.cs');
for (const token of ['localgpt.vision.ocr', 'WaitForResultAsync(correlationId', 'ApprovalRequired', 'InsertOcrTextLayer']) assert.ok(picture.includes(token), token);

assert.match(security, /ProtectOutgoingAsync\(OrganicWireEnvelope envelope/);
assert.match(security, /UnprotectIncomingAsync\(OrganicWireEnvelope envelope/);
assert.match(security, /IsSecurityBootstrap\(OrganicWireMessageType type/);
assert.doesNotMatch(security, /\bOneWireEnvelope envelope/);
assert.doesNotMatch(security, /IsSecurityBootstrap\(OneWireMessageType type/);
assert.ok(picture.includes('using PublisherStudio.Services.UserExperience;'));
const razorImports = read('src/PublisherStudio.Web/Components/_Imports.razor');
assert.ok(razorImports.includes('@using PublisherStudio.Components.OrganicPlugins'));
for (const launcher of ['Build-Release.cmd', 'Build-AllRuntimes.cmd']) {
  const text = read(launcher);
  assert.ok(text.includes('pushd "%~dp0"'), launcher);
  assert.ok(text.includes('pause'), launcher);
}

const pictureRazor = read('src/PublisherStudio.Web/Components/Editor/PictureEditor.razor');
assert.ok(pictureRazor.includes('CanUseLocalGptOcr'));
assert.ok(pictureRazor.includes('LocalGPT AI'));

const http = read('src/PublisherStudio.Web/Controllers/OrganicWireHttpController.cs');
const envelopeFactory = read('src/PublisherStudio.Web/Services/OrganicPlugins/OrganicWireEnvelopeFactory.cs');
assert.ok(http.includes('api/organic/onewire/http-json'));
assert.ok(http.includes('CreateWorkEnvelope'));
assert.ok(envelopeFactory.includes('OrganicWireMessageType.ApprovalRequired'));
assert.ok(envelopeFactory.includes('OrganicWorkStatus.PendingApproval'));
assert.ok(http.includes('MaximumMessageBytes'));
assert.ok(http.includes('PollWork'));
assert.ok(coordinator.includes('publisher.website.content.request'));
const ribbon = read('src/PublisherStudio.Web/Components/Editor/PublicationRibbon.razor');
assert.ok(ribbon.includes('CanShowAiCouncil'));
assert.ok(ribbon.includes('LocalGptConnection.Changed += OnLocalGptChanged'));

const docs = read('docs/articles/localgpt-and-onewire.md');
assert.ok(docs.includes('ESP32'));
assert.ok(docs.includes('generated at runtime') || docs.includes('generate their own random'));
console.log('PASS organic runtime security, correlation, fresh capture consent, OCR and HTTP/JSON contracts.');
