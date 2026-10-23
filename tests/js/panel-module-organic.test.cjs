const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const script = fs.readFileSync(path.join(__dirname, '../../src/PublisherStudio.Web/wwwroot/js/automationInterop.js'), 'utf8');
const id = 'a7c604bf-441a-4db6-ad7e-2eb7897bad42';
let applied = false;
const store = new Map();
class Element {}
const element = new Element();
element.dataset = { elementId: id, elementKind: 'DevExtremeComponent' };
element.getBoundingClientRect = () => ({x: 2, y: 4, width: 400, height: 300});
element.closest = () => null;
const runtime = {
  list: () => [{elementId:id,kind:'DataGrid',fields:['Name'],functions:[{name:'refreshData',input:'none'}, {name:'sortByColumn',input:'object'}]}],
  describe: value => value===id ? {methods:['refreshData','sortByColumn']} : null,
  invoke: async command => { applied = command.elementId===id && command.method==='sortByColumn' && command.value.field==='Name'; return applied; }
};
const window = {
  PublisherStudioOrganicModuleRuntime: runtime,
  innerWidth: 1200, innerHeight: 800,
  setTimeout: () => 1,
  addEventListener: () => {}
};
const document = {querySelector: selector => selector.includes(id) ? element : null, visibilityState:'visible'};
const storage = {getItem: key => store.get(key), setItem: (key,val) => store.set(key,val),removeItem: key => store.delete(key)};
const context = {window,document,Element,sessionStorage:storage,console,clearTimeout:()=>{},fetch:async()=>({ok:true,json:async()=>({linked:true})}),globalThis:null};
context.globalThis = context;
vm.createContext(context);
vm.runInContext(script, context);
(async () => {
    assert.equal(window.publisherAutomation.enableForSession(), true);
    const inventory = JSON.parse(await window.publisherAutomation.execute({kind:'InspectModules'}));
    assert.equal(inventory.modules.length,1);
    assert.equal(inventory.modules[0].elementId,id);
    assert.deepEqual(Array.from(inventory.modules[0].methods),['refreshData','sortByColumn']);
    assert.equal(inventory.modules[0].functions[1].input,'object');
    const result = JSON.parse(await window.publisherAutomation.execute({kind:'InvokeModule',selector:id,key:'sortByColumn',text:JSON.stringify({field:'Name',direction:'asc'})}));
    assert.equal(result.applied,true);
    assert.equal(applied,true);
    await assert.rejects(()=>window.publisherAutomation.execute({kind:'InvokeModule',selector:id,key:'arbitraryJavaScript',text:'null'}),/not available/);
    window.publisherAutomation.disableForSession();
    await assert.rejects(()=>window.publisherAutomation.execute({kind:'InspectModules'}),/consent/);
    await assert.rejects(()=>window.publisherAutomation.execute({kind:'InvokeModule',selector:id,key:'refreshData',text:'null'}),/consent/);
    console.log('Panel module organic browser boundary: opted in, discovery, typed functions, invocation, arbitrary function denial, opt-out: passed');
})().catch(error=>{console.error(error);process.exitCode=1});
