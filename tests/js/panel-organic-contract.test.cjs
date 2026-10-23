const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const base = path.resolve(__dirname,'../../');
const read = file => fs.readFileSync(path.join(base, file),'utf8');
const model = read('src/PublisherStudio.Web/BusinessObjects/AutomationModels.cs');
const executable = read('src/PublisherStudio.Web/Services/OrganicPlugins/OrganicCapabilityAndExecutionServices.cs');
const handlers = read('src/PublisherStudio.Web/Services/OrganicPlugins/PanelModuleOrganicHandlers.cs');
const serviceCollection = read('src/PublisherStudio.Web/PublisherStudioServiceCollectionExtensions.cs');
const descriptors = JSON.parse(read('src/PublisherStudio.Web/Configuration/publisher-dx-functions.json')).functions;
for(const key of ['InspectModules','InvokeModule']) assert(model.includes(key));
assert(executable.includes('kind is BrowserAutomationCommandKind.InspectModules or BrowserAutomationCommandKind.InvokeModule'), 'Generic browser input must not bypass module policy');
assert(executable.includes('requiresFreshModuleConsent || requiresFreshCaptureConsent') || executable.includes('requiresFreshCaptureConsent || requiresFreshModuleConsent'), 'Consequential invocation must force fresh approval');
for(const key of ['publisher.panel.modules.inspect','publisher.panel.module.invoke']) {
 const c = descriptors.find(x=>x.key===key);
 assert(c && c.allowPeerInvocation && c.isExposedToPeer, `${key} is not advertised`);
 assert(handlers.includes(`"${key}"`), `${key} is not implemented`);
}
const invoke = descriptors.find(c=>c.key==='publisher.panel.module.invoke');
assert.equal(invoke.requiresFrontendUserConfirmation,true);
assert.equal(invoke.requiresHumanConfirmation,true);
assert.equal(invoke.isReadOnly,false);
const inspect = descriptors.find(c=>c.key==='publisher.panel.modules.inspect');
assert.equal(inspect.isReadOnly,true);
assert(serviceCollection.includes('PanelModuleInspectOrganicHandler') && serviceCollection.includes('PanelModuleInvokeOrganicHandler'));
assert(read('src/PublisherStudio.Web/wwwroot/js/componentRuntime.js').includes('PublisherStudioOrganicModuleRuntime'));
console.log('Module organic contract: dedicated handlers, explicit fresh approval, typed browser command, catalog, shared export runtime passed.');
