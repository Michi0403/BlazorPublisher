import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const runtimePath = path.join(root, 'src', 'PublisherStudio.Web', 'wwwroot', 'js', 'componentRuntime.js');
const exportPath = path.join(root, 'src', 'PublisherStudio.Web', 'wwwroot', 'js', 'publisherInterop.js');
const modelPath = path.join(root, 'src', 'PublisherStudio.Web', 'Domain', 'PublicationComponentModels.cs');
const servicePath = path.join(root, 'src', 'PublisherStudio.Web', 'Services', 'PublicationComponentService.cs');
const ribbonPath = path.join(root, 'src', 'PublisherStudio.Web', 'Components', 'Editor', 'PublicationRibbon.razor');
const editorPath = path.join(root, 'src', 'PublisherStudio.Web', 'Components', 'Editor', 'DevExtremeComponentEditor.razor');
const viewPath = path.join(root, 'src', 'PublisherStudio.Web', 'Components', 'Editor', 'DevExtremeComponentView.razor');
const statePath = path.join(root, 'src', 'PublisherStudio.Web', 'Services', 'EditorStateService.cs');
const devExtremePath = path.join(root, 'src', 'PublisherStudio.Web', 'wwwroot', 'vendor', 'devextreme-dist', 'js', 'dx.all.js');

const runtime = fs.readFileSync(runtimePath, 'utf8');
const exporter = fs.readFileSync(exportPath, 'utf8');
const model = fs.readFileSync(modelPath, 'utf8');
const service = fs.readFileSync(servicePath, 'utf8');
const ribbon = fs.readFileSync(ribbonPath, 'utf8');
const editor = fs.readFileSync(editorPath, 'utf8');
const view = fs.readFileSync(viewPath, 'utf8');
const state = fs.readFileSync(statePath, 'utf8');
const devExtreme = fs.readFileSync(devExtremePath, 'utf8');

const components = {
  DataGrid: 'dxDataGrid',
  TreeList: 'dxTreeList',
  Scheduler: 'dxScheduler',
  Form: 'dxForm',
  TextBox: 'dxTextBox',
  TextArea: 'dxTextArea',
  NumberBox: 'dxNumberBox',
  DateBox: 'dxDateBox',
  CheckBox: 'dxCheckBox',
  SelectBox: 'dxSelectBox',
  TagBox: 'dxTagBox',
  Gallery: 'dxGallery',
  TileView: 'dxTileView',
  Menu: 'dxMenu',
  ContextMenu: 'dxContextMenu',
  TabPanel: 'dxTabPanel',
  MultiView: 'dxMultiView',
  Splitter: 'dxSplitter',
  ScrollView: 'dxScrollView',
  PivotGrid: 'dxPivotGrid',
  Button: 'dxButton'
};

for (const [kind, plugin] of Object.entries(components)) {
  assert.match(model, new RegExp(`\\b${kind}\\b`), `${kind} is missing from the publication model.`);
  assert.match(runtime, new RegExp(`${kind}:\\s*"${plugin}"`), `${kind} is missing from the browser runtime.`);
  assert.match(ribbon, new RegExp(`AddComponent${kind}`), `${kind} is missing from the Insert ribbon.`);
  assert.ok(devExtreme.includes(plugin), `${plugin} is missing from the bundled DevExtreme runtime.`);
}

for (const contract of [
  'new DevExpress.data.CustomStore',
  'new DevExpress.data.ODataStore',
  'storeOptions.insert',
  'storeOptions.update',
  'storeOptions.remove',
  'SubmitRest',
  'MailTo',
  'ApplyFilter',
  'SetValue',
  'CustomScript',
  'TargetSharedComponentId'
]) {
  assert.ok(runtime.includes(contract) || model.includes(contract) || service.includes(contract), `${contract} is missing from the component contract.`);
}

assert.match(editor, /Test endpoint and discover fields/);
assert.match(editor, /Use publication pages as menu/);
assert.match(editor, /Lookup dataset/);
assert.match(editor, /Document-wide synchronized/);
assert.match(service, /BuildFields\(/);
assert.match(service, /BuildActions\(/);
assert.match(service, /PublicationComponentDataMode\.PublicationDataObject \? live : null/);

assert.match(runtime, /function configuredValue\(/);
assert.match(runtime, /selectedRowsData\?\.\[0\]/);
assert.match(runtime, /typeof context\.dataSource\?\.insert === "function"/);
assert.match(runtime, /typeof context\.dataSource\?\.update === "function"/);
assert.match(runtime, /connection\.allowUpdate && hasKey/);
assert.match(runtime, /connection\.allowLoad === false/);
assert.match(runtime, /element\.replaceChildren\(\);/);
assert.match(state, /SetSelectedComponentScope\(/);
assert.match(state, /RemoveAll\(element => element is DevExtremeComponentElement component && component\.SharedComponentId == sharedId\)/);
assert.match(state, /removedSharedIds/);
assert.match(state, /sharedComponentMap/);
assert.match(state, /SynchronizeDocumentComponent\(component\)/);
assert.match(state, /if \(item is DevExtremeComponentElement component\)[\s\S]*action\.TargetElementId = mappedActionTarget/);
assert.match(view, /_lastRenderedConfig/);
assert.match(view, /string\.Equals\(config, _lastRenderedConfig, StringComparison\.Ordinal\)/);

assert.match(exporter, /buildPublisherSingleHtml\('presentation'/);
assert.match(exporter, /buildPublisherSingleHtml\('site'/);
assert.match(exporter, /fetchExportAsset\('js\/componentRuntime\.js'\)/);
assert.match(exporter, /websiteSiteRuntime/);
assert.match(exporter, /PublisherStudioNavigation/);
assert.match(exporter, /<script>\$\{safeScript\(componentRuntimeSource\)\}<\/script>/);

const requests = [];
const context = {
  console,
  URL,
  URLSearchParams,
  TextDecoder,
  Uint8Array,
  Response,
  Headers,
  setInterval,
  clearInterval,
  CustomEvent: class CustomEvent {},
  Element: class Element {},
  CSS: { escape: value => String(value).replace(/"/g, '\\"') },
  document: {
    querySelector: () => null,
    querySelectorAll: () => []
  },
  location: {
    protocol: 'https:',
    origin: 'https://publisher.local',
    search: '',
    href: 'https://publisher.local/'
  },
  localStorage: {
    getItem: () => '',
    setItem: () => undefined
  },
  atob: value => Buffer.from(value, 'base64').toString('binary'),
  fetch: async (url, options = {}) => {
    requests.push({ url: String(url), options });
    return new Response(JSON.stringify({ data: [{ id: 1, title: 'Working', completed: false }] }), {
      status: 200,
      headers: { 'content-type': 'application/json' }
    });
  }
};
context.window = context;
vm.runInNewContext(runtime, context, { filename: runtimePath });

const restRows = JSON.parse(await context.PublisherStudioComponentRuntime.probeConnection({
  mode: 'Rest',
  url: 'https://api.example.test/items',
  loadMethod: 'Get',
  jsonPath: 'data',
  headers: [{ name: 'X-Test', value: 'yes' }]
}));
assert.deepEqual(restRows, [{ id: 1, title: 'Working', completed: false }]);
assert.equal(requests.at(-1).options.headers['X-Test'], 'yes');

await context.PublisherStudioComponentRuntime.probeConnection({
  mode: 'OData',
  url: 'https://api.example.test/odata/Orders',
  oDataVersion: 4
});
assert.match(requests.at(-1).url, /%24top=10|\$top=10/);

console.log('component catalog, REST/OData probe, smart connections, and single-file export contract tests passed');
