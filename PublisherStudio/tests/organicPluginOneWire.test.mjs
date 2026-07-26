import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = (...parts) => fs.readFileSync(path.join(root, ...parts), 'utf8');

const models = read('src','PublisherStudio.Web','Domain','OrganicPluginModels.cs');
const interfaces = read('src','PublisherStudio.Web','Services','OrganicPlugins','IOrganicPluginServices.cs');
const codec = read('src','PublisherStudio.Web','Services','OrganicPlugins','OrganicPluginProtocolCodec.cs');
const state = read('src','PublisherStudio.Web','Services','OrganicPlugins','OrganicPluginStateServices.cs');
const execution = read('src','PublisherStudio.Web','Services','OrganicPlugins','OrganicCapabilityAndExecutionServices.cs');
const connection = read('src','PublisherStudio.Web','Services','OrganicPlugins','LocalGptConnectionService.cs');
const discovery = read('src','PublisherStudio.Web','HostedServices','OrganicPlugins','LocalGptDiscoveryHostedService.cs');
const controller = read('src','PublisherStudio.Web','Controllers','OrganicPluginController.cs');
const services = read('src','PublisherStudio.Web','PublisherStudioServiceCollectionExtensions.cs');
const page = read('src','PublisherStudio.Web','Components','Pages','OrganicPlugins.razor');
const ribbon = read('src','PublisherStudio.Web','Components','Editor','PublicationRibbon.razor');
const settings = JSON.parse(read('src','PublisherStudio.Web','appsettings.json'));
const packageJson = JSON.parse(read('src','PublisherStudio.Web','package.json'));
const webProject = read('src','PublisherStudio.Web','PublisherStudio.Web.csproj');
const installerProject = read('src','PublisherStudio.InstallerConsole','PublisherStudio.InstallerConsole.csproj');

assert.match(models, /DefaultLocalGptServicePort = 51140/);
assert.match(models, /DefaultDiscoveryPort = 51141/);
for (const field of ['Properties', 'EncryptedPayload', 'Signature', 'Hash', 'ErrorCheck', 'ExecutionMode', 'NotBeforeUtc', 'Organs', 'Skills'])
  assert.match(models, new RegExp(`\\b${field}\\b`), `${field} missing from protocol envelope.`);
assert.match(models, /enum OrganicApprovalMode \{ AskEveryTime, SameCapability, CurrentWorkOrder, AlwaysAllow, Deny \}/);

assert.match(codec, /SHA256\.HashData/);
assert.match(codec, /ComputeCrc32/);
assert.match(codec, /EncryptedPayload and public Properties are mutually exclusive/);
assert.match(codec, /MaximumMessageBytes/);
assert.match(codec, /FixedTimeEquals/);

for (const contract of ['IOrganicPluginProtocolCodec','ILocalGptDiscoveryRegistry','IOrganicCapabilityCatalog','IOrganicPermissionStore','IOrganicWorkCoordinator','IOrganicWorkExecutor','ILocalGptConnectionService'])
  assert.match(interfaces, new RegExp(`interface ${contract}`));
for (const registration of [
  'AddSingleton<IOrganicPluginProtocolCodec, OrganicPluginProtocolCodec>',
  'AddSingleton<ILocalGptDiscoveryRegistry, LocalGptDiscoveryRegistry>',
  'AddSingleton<IOrganicPermissionStore, OrganicPermissionStore>',
  'AddSingleton<IOrganicWorkCoordinator, OrganicWorkCoordinator>',
  'AddSingleton<ILocalGptConnectionService, LocalGptConnectionService>',
  'AddHostedService<LocalGptDiscoveryHostedService>'
]) assert.ok(services.includes(registration), `${registration} missing.`);

assert.match(discovery, /UdpClient/);
assert.match(discovery, /options\.Value\.DiscoveryPort/);
assert.match(discovery, /Optional LocalGPT discovery could not bind/);
assert.match(discovery, /DiscoveryReceivePollSeconds/);
assert.match(discovery, /receiveCancellation\.CancelAfter\(receivePoll\)/);
assert.match(discovery, /Ignored malformed LocalGPT discovery data.*listening continues/);
assert.match(discovery, /Transient LocalGPT discovery receive failure.*listening continues/);
assert.match(connection, /TcpClient/);
assert.match(connection, /OrganicWireMessageType\.Hello/);
assert.match(connection, /ProcessInvokeAsync/);
assert.match(connection, /new CancellationTokenSource\(\)/, 'Connection lifetime must not be tied to a single HTTP request token.');
assert.match(connection, /ConcurrentDictionary<Guid, Task> activeInvocations/, 'Incoming invocations must be tracked.');
assert.match(connection, /StartInvoke\(envelope, connectedId, connectedWriter, cancellationToken\)/, 'Incoming work must use the task tracker.');
assert.doesNotMatch(connection, /_\s*=\s*ProcessInvokeAsync/, 'Incoming work must not be discarded as fire-and-forget.');
assert.match(connection, /ReadLoopAsync\(connectedId, requestedPeerId, connectedReader, connectedWriter/, 'The read loop must bind to one connection generation.');
assert.match(connection, /if \(connectionId == connectedId\)/, 'A stale read loop must not overwrite a replacement connection.');
assert.match(connection, /connectionId = Guid\.Empty;/, 'Disconnect must invalidate the current connection generation before cancellation.');

for (const capability of [
  'publisher.screen.capture','publisher.screen.capture.result','publisher.input.execute','publisher.input.result','publisher.openscad.generate',
  'publisher.spreadsheet.inspect','publisher.text.insert.propose','publisher.business-context','publisher.media.capabilities'
]) assert.ok(execution.includes(`"${capability}"`), `${capability} missing.`);
assert.match(execution, /IOpenScadDocumentService openScad/);
assert.match(execution, /openScad\.Generate\(document\)/);
assert.match(execution, /SpreadsheetSessionStore spreadsheetSessions/);
assert.match(execution, /ReadOnlyInspection = true/);
assert.match(execution, /ReadScreenshotResult\(parameters\)/);
assert.match(execution, /ReadInputResult\(parameters\)/);
assert.match(execution, /ReadyForNextHeartbeat/);
assert.match(execution, /700_000/);
assert.match(execution, /ConcurrentDictionary<string, SemaphoreSlim> workflowGates/);
assert.match(execution, /ExecutionMode == OrganicExecutionMode\.Scheduled/);
assert.match(execution, /await workflowGate\.WaitAsync/);
assert.doesNotMatch(execution, /class\s+OrganicOpenScad(Document|Node)/, 'Do not introduce a second OpenSCAD object graph.');

assert.match(state, /permissions\.json/);
assert.match(state, /AskEveryTime remains the safe default/);
assert.match(state, /OrganicApprovalMode\.CurrentWorkOrder/);
assert.match(state, /public bool IsDenied\(OrganicWireEnvelope envelope\)/);
assert.match(state, /OrganicApprovalMode\.Deny/);
assert.match(state, /string\.IsNullOrWhiteSpace\(rule\.WorkOrderKey\)/);
assert.match(state, /string\.IsNullOrWhiteSpace\(envelope\.WorkOrderKey\)/);
assert.match(state, /OrganicWireMessageType\.ApprovalRequired => OrganicWorkStatus\.PendingApproval/);
assert.match(state, /OrganicWireMessageType\.WorkAccepted => OrganicWorkStatus\.Queued/);
assert.match(state, /private static OrganicWorkStatus ResolveStatus/);
assert.match(state, /Enum\.TryParse<OrganicWorkStatus>/);
assert.match(execution, /permissions\.IsDenied\(envelope\)/);
assert.match(execution, /OrganicWorkStatus\.Declined/);
assert.doesNotMatch(state, /public OrganicPermissionStore\(\)/, 'Permission store should use DI logging, not a fallback constructor.');

assert.match(controller, /Route\("api\/organic"\)/);
for (const route of ['status','peers','capabilities','permissions','work','results','text-proposals','connect/{peerId}','disconnect','council','work/{id:guid}/approve','work/{id:guid}/decline'])
  assert.ok(controller.includes(`"${route}"`), `${route} controller route missing.`);
assert.match(page, /@page "\/organic-plugins"/);
assert.match(page, /OpenSCAD Team/);
assert.match(page, /Spreadsheet Team/);
assert.match(page, /Permission matrix/);
assert.match(page, /Approve once/);
assert.match(page, /IUserNotificationService Notifications/);
assert.match(page, /ILogger<OrganicPlugins> Logger/);
assert.match(ribbon, /<DxRibbonTab Text="AI Council">/);
assert.match(ribbon, /Navigation\.NavigateTo\("\/organic-plugins"\)/);

assert.equal(settings.OrganicPlugins.DiscoveryPort, 51141);
assert.equal(settings.OrganicPlugins.DiscoveryReceivePollSeconds, 5);
assert.equal(settings.OrganicPlugins.Enabled, true);
assert.equal(packageJson.version, '1.0.89');
assert.match(webProject, /<Version>1\.0\.89<\/Version>/);
assert.match(installerProject, /<Version>1\.0\.89<\/Version>/);

console.log('PublisherStudio LocalGPT organic 1-Wire, permission, OpenSCAD and spreadsheet workflow source contracts passed.');
