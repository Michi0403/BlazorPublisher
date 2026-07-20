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
const cssPath = path.join(root, 'src', 'PublisherStudio.Web', 'wwwroot', 'css', 'site.css');

const runtime = fs.readFileSync(runtimePath, 'utf8');
const exporter = fs.readFileSync(exportPath, 'utf8');
const model = fs.readFileSync(modelPath, 'utf8');
const service = fs.readFileSync(servicePath, 'utf8');
const ribbon = fs.readFileSync(ribbonPath, 'utf8');
const editor = fs.readFileSync(editorPath, 'utf8');
const view = fs.readFileSync(viewPath, 'utf8');
const state = fs.readFileSync(statePath, 'utf8');
const devExtreme = fs.readFileSync(devExtremePath, 'utf8');
const css = fs.readFileSync(cssPath, 'utf8');

const dataModel = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Domain', 'PublicationDataModels.cs'), 'utf8');
const dataService = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Services', 'PublicationDataService.cs'), 'utf8');
const dataManager = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Components', 'Editor', 'DataManager.razor'), 'utf8');
const pageSurface = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Components', 'Editor', 'PageSurface.razor'), 'utf8');
const app = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Components', 'App.razor'), 'utf8');
const publicationModel = fs.readFileSync(path.join(root, 'src', 'PublisherStudio.Web', 'Domain', 'PublicationModels.cs'), 'utf8');

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
  Map: 'dxMap',
  VectorMap: 'dxVectorMap',
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
assert.match(editor, /Use live publication pages/);
assert.match(editor, /Lookup dataset/);
assert.match(editor, /Document-wide synchronized/);

assert.match(model, /PublicationMenuDestinationKind/);
assert.match(model, /ExternalUrl/);
assert.match(service, /menuItem\.Destination == PublicationMenuDestinationKind\.Page/);
assert.match(service, /menuItem\.Destination == PublicationMenuDestinationKind\.ExternalUrl/);
assert.match(runtime, /kind === "navigate"/);
assert.match(runtime, /publisherstudio:navigate/);
assert.match(runtime, /publisherstudio:open-url/);
assert.match(runtime, /itemWindow/);
assert.match(pageSurface, /NavigateToPage\(string target\)/);
assert.match(editor, /External-only menus are supported/);
assert.match(editor, /Build direction/);
assert.match(editor, /SetFieldDataProperty/);
assert.match(editor, /Select source property/);
assert.match(editor, /@foreach \(var column in SourceColumns\)/);
assert.match(editor, /This menu uses its editable item list/);
assert.match(runtime, /trigger === "ItemClick" && \["menu", "contextmenu"\]/);
assert.match(runtime, /action: "Navigate"/);
assert.match(app, /Unregister\(CommonResources\.DevExtremeJS\)/);
assert.equal((app.match(/vendor\/devextreme-dist\/js\/dx\.all\.js/g) || []).length, 1, 'App must contain one pinned manual DevExtreme bundle.');
assert.doesNotMatch(app, /RegisterScripts\(\)/, 'Default DevExtreme registration would duplicate the pinned bundle.');
assert.ok(app.indexOf('vendor/jquery/jquery.min.js') < app.indexOf('vendor/devextreme-dist/js/dx.all.js'));
assert.ok(app.indexOf('vendor/devextreme-dist/js/dx.all.js') < app.indexOf('DxResourceManager.RegisterScripts'), 'Pinned jQuery/DevExtreme must load before dependent DevExpress resource scripts.');
assert.match(publicationModel, /FormatVersion \{ get; set; \} = "1\.41"/);
assert.match(editor, /Only options supported by the selected component are shown here/);
assert.doesNotMatch(editor.slice(editor.indexOf('else if (_section == "behavior")'), editor.indexOf('else if (_section == "map")')), /Scheduler view[\s\S]*without-kind-guard/);
assert.match(dataModel, /PublicationPages/);
assert.match(dataModel, /PublicationDocument/);
assert.match(dataService, /EnsureBuiltInObjects/);
assert.match(dataService, /BuildPublicationPageRows/);
assert.match(dataService, /BuildPublicationDocumentRows/);
assert.match(dataManager, /Self-updating publication pages/);
assert.match(dataManager, /Self-updating publication document/);
assert.match(dataManager, /Self-updating publication objects/);
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
assert.match(exporter, /vectormap-data\/world\.js/);
assert.match(editor, /Vector map designer/);
assert.match(editor, /Import GeoJSON/);
assert.match(model, /PublicationVectorMapFeature/);
assert.match(state, /CommitContentViewport/);
assert.match(view, /data-content-viewport/);
assert.match(runtime, /CheckBox", "Map", "VectorMap"\]\.includes/);
assert.match(exporter, /contentFitScaleX/);
assert.match(runtime, /fetchDataObjectLive\(state\.config\.connection\.dataObjectLive\)/);
assert.match(runtime, /clearInterval\(state\.timer\)/);
assert.match(ribbon, /Position visible text/);
assert.match(ribbon, /Position visible worksheet/);
assert.match(ribbon, /Position visible map/);
assert.match(css, /dxbl-btn-dropdown-toggle/);
assert.match(exporter, /translate\(\$\{translateX\}px, \$\{translateY\}px\)/);
assert.match(exporter, /websiteSiteRuntime/);
assert.match(exporter, /PublisherStudioNavigation/);
assert.match(exporter, /componentInteractionOwner/);
assert.match(exporter, /stage\.addEventListener\('dblclick', handlers\.stageDoubleClick, true\)/);
assert.match(exporter, /Let DevExtreme finish its native click first/);
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

// Exercise the actual Menu event path, not only the source contract. This catches
// regressions where the configured item data renders but ItemClick never reaches
// stable page navigation or external URL handling.
let menuOptions;
let navigatedPage = '';
let openedUrl = null;
const menuHost = { closest: () => null };
const menuInstance = {
  element: () => [menuHost],
  getDataSource: () => null,
  dispose: () => undefined
};
const jquery = () => ({
  dxMenu(argument) {
    if (argument === 'instance') return menuInstance;
    menuOptions = argument;
    return this;
  }
});
jquery.fn = { dxMenu() {} };
context.jQuery = jquery;
context.DevExpress = { data: {} };
context.PublisherStudioNavigation = {
  goToPage(value) {
    navigatedPage = String(value);
    return true;
  }
};
context.open = (url, target) => {
  openedUrl = { url: String(url), target: String(target) };
  return null;
};
const menuElement = {
  dataset: {},
  innerHTML: '',
  classList: { add() {} },
  querySelectorAll: () => [],
  replaceChildren() {},
  matches: () => false
};
const pageId = '11111111-2222-3333-4444-555555555555';
await context.PublisherStudioComponentRuntime.render(menuElement, {
  kind: 'Menu',
  id: 'menu-test',
  menuSourceMode: 'ManualItems',
  menuItems: [
    { id: 'page', text: 'Page', targetPageId: pageId, visible: true, enabled: true },
    { id: 'external', text: 'External', url: 'https://example.test/help', openInNewWindow: false, visible: true, enabled: true },
    { id: 'none', text: 'No action', visible: true, enabled: true }
  ],
  rows: [],
  fields: [],
  actions: [],
  orientation: 'vertical',
  designerMode: false,
  connection: {}
}, { polling: false, fetchNow: false });
assert.equal(menuOptions.orientation, 'vertical');
assert.equal(menuOptions.items.length, 3);
menuOptions.onItemClick({ component: menuInstance, itemData: menuOptions.items[0] });
await new Promise(resolve => setTimeout(resolve, 0));
assert.equal(navigatedPage, pageId);
menuOptions.onItemClick({ component: menuInstance, itemData: menuOptions.items[1] });
await new Promise(resolve => setTimeout(resolve, 0));
assert.deepEqual(openedUrl, { url: 'https://example.test/help', target: '_self' });
openedUrl = null;
menuOptions.onItemClick({ component: menuInstance, itemData: menuOptions.items[2] });
await new Promise(resolve => setTimeout(resolve, 0));
assert.equal(openedUrl, null);

console.log('component catalog, REST/OData probe, menu navigation, smart connections, and single-file export contract tests passed');
