// Read-only PublisherStudio eye capability regression: no inputs/hidden DOM are disclosed.
import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';

const Element = class Element {};
function element(tag, attributes, bounds, { text = '', value = '', visible = true, sensitive = false } = {}) {
  const instance = new Element();
  instance.tagName = tag.toUpperCase(); instance.id = attributes.id || '';
  instance.textContent = text; instance.value = value;
  instance.childNodes = [{ nodeType: 3, textContent: text }];
  instance.getAttribute = name => attributes[name] ?? null;
  instance.getBoundingClientRect = () => ({ ...bounds, right: bounds.x + bounds.width, bottom: bounds.y + bounds.height, left: bounds.x, top: bounds.y });
  instance.matches = selector => (selector.includes('password') && attributes.type === 'password') || false;
  instance.closest = () => sensitive ? {} : null;
  instance.styleVisible = visible;
  return instance;
}
const button = element('button', { id: 'save', 'aria-label': 'Save publication' }, { x: 20, y: 10, width: 150, height: 35 }, { text: 'Save' });
const field = element('input', { id: 'author', type: 'text' }, { x: 20, y: 70, width: 200, height: 30 }, { value: 'SECRET-CUSTOMER-VALUE' });
const secret = element('input', { id: 'password', type: 'password' }, { x: 20, y: 110, width: 200, height: 30 }, { value: 'PASSWORD-SECRET' });
const publication = element('main', { 'data-page-id': 'page-123', 'data-page-width-mm': '210', 'data-page-height-mm': '297' }, { x: 20, y: 150, width: 300, height: 400 });
const artwork = element('div', { 'data-element-id': 'element-456', 'data-element-kind': 'picture', 'aria-label': 'Cover image', 'data-selected': 'true' }, { x: 25, y: 155, width: 90, height: 75 }, { text: 'PRIVATE-DOCUMENT-TEXT' });
const body = new Element(); body.querySelectorAll = () => [button,field,secret,artwork];
const session = new Map();
const sandbox = {
  console, Element, Node: { TEXT_NODE: 3 }, Promise, Number, String,
  globalThis: null, location: { pathname: '/' },
  document: { body, title: 'PublisherStudio', querySelector: selector => selector === 'body' ? body : null, querySelectorAll: () => [],
              addEventListener() {} },
  sessionStorage: { getItem: key => session.get(key), setItem: (key, v) => session.set(key,v), removeItem: key => session.delete(key) },
  window: { innerWidth: 1280, innerHeight: 800, getComputedStyle: el => ({ display: el.styleVisible === false ? 'none' : 'block', visibility:'visible',opacity:'1' }),
            setTimeout: () => 1, clearTimeout() {}, addEventListener() {}, location:{ origin:'http://localhost' } },
  fetch: () => new Promise(() => {}), // keep the existing UI polling operation pending
  setTimeout: () => 1, clearTimeout() {}
};
sandbox.globalThis = sandbox;
const source = fs.readFileSync(new URL('../../src/PublisherStudio.Web/wwwroot/js/automationInterop.js', import.meta.url), 'utf8');
vm.runInNewContext(source, sandbox, { filename: 'automationInterop.js' });
assert.equal(sandbox.window.publisherAutomation.enableForSession(), true);
const raw = await sandbox.window.publisherAutomation.execute({ kind: 'InspectView', selector: 'body' });
const snapshot = JSON.parse(raw);
assert.equal(snapshot.path, '/');
assert.equal(snapshot.viewport.width, 1280);
assert(snapshot.elements.some(el => el.label === 'Save publication'));
assert(snapshot.elements.some(el => el.elementId === 'element-456' && el.selected === true));
assert(!raw.includes('SECRET-CUSTOMER-VALUE'));
assert(!raw.includes('PASSWORD-SECRET'));
assert(!raw.includes('PRIVATE-DOCUMENT-TEXT'));
sandbox.window.publisherAutomation.disableForSession();
await assert.rejects(() => sandbox.window.publisherAutomation.execute({ kind: 'InspectView' }), /not enabled/);
console.log('PublisherStudio bounded read-only browser view and sensitive-value filtering: passed.');
