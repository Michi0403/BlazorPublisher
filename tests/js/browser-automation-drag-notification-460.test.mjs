import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';

const source = fs.readFileSync(new URL('../../src/PublisherStudio.Web/wwwroot/js/automationInterop.js', import.meta.url), 'utf8');
const diagnosticsSource = fs.readFileSync(new URL('../../src/PublisherStudio.Web/wwwroot/js/javascript-diagnostics.js', import.meta.url), 'utf8');
const bridge = fs.readFileSync(new URL('../../src/PublisherStudio.Web/Components/Layout/JavaScriptDiagnosticsBridge.razor', import.meta.url), 'utf8');
const events = [];
let pointer = null;
class Element { constructor(tag='CANVAS') { this.tagName=tag; this.id='drawing'; this.style={}; this.bounds={ left:10,top:10,width:200,height:100 }; } dispatchEvent(ev){events.push(ev);} getBoundingClientRect(){return this.bounds;} scrollIntoView(){} }
const canvas = new Element();
const document = { body: new Element('BODY'), querySelector: () => canvas, elementFromPoint: () => canvas, createElement: () => { pointer = new Element('DIV'); pointer.setAttribute=()=>{}; pointer.remove=()=>{};return pointer; }, addEventListener(){} };
document.body.appendChild = () => {};
const messages=[];
const sandbox = { document, console, Element, Number, String, Promise, Date, Math, JSON,
    MouseEvent: class { constructor(type, opt) { this.type=type;Object.assign(this,opt); } },
    PointerEvent: class { constructor(type, opt) { this.type=type;Object.assign(this,opt); } },
    Window: class {},
    sessionStorage: {getItem:()=>null, setItem(){}, removeItem(){}},
    location: {pathname:'/'},
    window: {addEventListener(){},setTimeout:()=>1,clearTimeout(){},location:{origin:'http://localhost'}},
    publisherStudioJavaScriptDiagnostics: {report(){},guardObject(_name,value){return value;},notifyAutomationError:(ctx,msg)=>messages.push({ctx,msg})}
};
sandbox.globalThis=sandbox;
vm.runInNewContext(source,sandbox);
const execute=sandbox.window.publisherAutomation.execute;
await execute({kind:'mousedown',selector:'canvas',x:15,y:25});
await execute({kind:'mousemove',selector:'canvas',x:70,y:55});
await execute({kind:'mouseup',selector:'canvas',x:90,y:60});
assert.deepEqual(events.map(x=>x.type),['pointerdown','mousedown','pointermove','mousemove','pointerup','mouseup']);
assert.equal(events[2].buttons,1);
assert.equal(events[4].buttons,0);
assert(pointer?.style?.left === '90px' && pointer?.style?.top === '60px');
assert(source.includes('showAutomationError(\'transport\', error)'));
assert(source.includes('if (!inputResponse.ok) throw'));
assert(diagIncludes());
// Notifier bridge may mount after transport polling starts: preserve bounded pre-bind errors.
assert(diagnosticsSource.includes('pendingAutomationErrors.push({ context: source, message: detail })'));
assert(diagnosticsSource.includes('pendingAutomationErrors.splice(0)'));
assert(source.includes("if (document.visibilityState === 'prerender')"));
assert(source.includes('state.timer = window.setTimeout(poll, 2000)'));
function diagIncludes() {return diagnosticsSource.includes('notifyAutomationError') && bridge.includes('OperationalNotifications.Error(safeMessage');}
console.log('PublisherStudio browser drag state, visible pointer, and native notifier contract: PASS');
