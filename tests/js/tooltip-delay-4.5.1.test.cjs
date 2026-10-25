'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const root = path.resolve(__dirname, '../..');
const source = fs.readFileSync(path.join(root, 'src/PublisherStudio.Web/wwwroot/js/tooltipRuntime.js'), 'utf8');
const listeners = new Map();
const timers = [];
class FakeElement {
    constructor(title) {
        this.attributes = { title };
        this.dataset = {};
        this.innerText = '';
        this.textContent = '';
        this.isConnected = true;
    }
    matches(selector) { return selector.includes('[title]') && selector.includes('button'); }
    getAttribute(name) { return this.attributes[name] ?? null; }
    hasAttribute(name) { return Object.hasOwn(this.attributes, name); }
    setAttribute(name, value) { this.attributes[name] = value; }
    removeAttribute(name) { delete this.attributes[name]; }
    closest() { return null; }
}
const document = {
    readyState: 'complete',
    documentElement: {},
    querySelectorAll() { return []; },
    addEventListener(name, callback) { listeners.set(name, callback); },
};
const window = {
    setTimeout(callback, delay) { timers.push({ callback, delay }); return timers.length; },
    addEventListener() {},
};
class FakeMutationObserver { observe() {} }
const context = vm.createContext({
    document, window, Element: FakeElement, MutationObserver: FakeMutationObserver,
    WeakMap, Map, String, Number, console,
    clearTimeout() {},
});
vm.runInContext(source, context, { filename: 'tooltipRuntime.js' });
assert.equal(typeof window.PublisherStudioTooltips?.hide, 'function');

const button = new FakeElement('Create a blank publication');
const event = { target: button, composedPath() { return [button]; } };
listeners.get('pointerover')(event);
assert.equal(timers.at(-1).delay, 1260, 'Mouse-hover tooltip should take 3 × 420ms');
assert.equal(button.getAttribute('title'), null, 'Native browser title should be suppressed until the custom tooltip is ready');
assert.equal(button.dataset.publisherTooltip, 'Create a blank publication');
listeners.get('pointerdown')(event);
listeners.get('focusin')(event);
assert.equal(timers.at(-1).delay, 660, 'Keyboard-focus tooltip should take 3 × 220ms');
console.log('PASS: 4.5.1 tooltip delays tripled for mouse and keyboard with native title suppression');
