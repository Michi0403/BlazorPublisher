// javascript-diagnostics: guarded
var publisherStudioDiagnostics = globalThis.publisherStudioJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`PublisherStudio JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("PublisherStudio fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`PublisherStudio fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
(() => { try {
    const storageKey = 'publisherstudio.automation.enabled';
    const state = { running: false, timer: 0, dragging: false, pointer: null, lastWarning: '', lastWarningTime: 0 };

    function showAutomationError(context, error) {
        const message = String(error?.message || error || 'Automation failed.');
        const fingerprint = `${context}:${message}`;
        const now = Date.now();
        if (state.lastWarning === fingerprint && now - state.lastWarningTime < 20000) return;
        state.lastWarning = fingerprint;
        state.lastWarningTime = now;
        publisherStudioDiagnostics.report(`Browser automation ${context}`, error);
        globalThis.publisherStudioJavaScriptDiagnostics?.notifyAutomationError?.(context, message);
    }

    function moveVisiblePointer(options, active) {
        if (!state.pointer) {
            state.pointer = document.createElement('div');
            state.pointer.setAttribute('data-publisher-automation-pointer', 'true');
            state.pointer.setAttribute('aria-hidden', 'true');
            Object.assign(state.pointer.style, {
                position: 'fixed', zIndex: '2147483645', pointerEvents: 'none',
                width: '17px', height: '17px', borderRadius: '50%',
                border: '2px solid #75d6ff', background: 'rgba(110,79,218,.18)',
                boxShadow: '0 0 0 5px rgba(117,214,255,.24), 0 0 9px rgba(110,79,218,.45)',
                transition: 'left 120ms linear, top 120ms linear',
                transform: 'translate(-50%,-50%)'
            });
            document.body.appendChild(state.pointer);
        }
        state.pointer.style.left = `${options.clientX}px`;
        state.pointer.style.top = `${options.clientY}px`;
        state.pointer.style.borderColor = active ? '#ff76ce' : '#75d6ff';
    }

    function dispatchPointerAndMouse(target, name, options) {
        target.dispatchEvent(new PointerEvent(`pointer${name}`, {
            ...options, pointerId: 1, pointerType: 'mouse', isPrimary: true,
            buttons: name === 'up' ? 0 : name === 'down' || state.dragging ? 1 : 0
        }));
        target.dispatchEvent(new MouseEvent(`mouse${name}`, {
            ...options, buttons: name === 'up' ? 0 : name === 'down' || state.dragging ? 1 : 0
        }));
    }


    function resolveTarget(selector, x, y) { try {
        if (selector) {
            try {
                const target = document.querySelector(selector);
                if (target) return target;
            } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:suppressed-catch@11', __caughtJavaScriptError);  }
        }
        if (Number.isFinite(Number(x)) && Number.isFinite(Number(y)))
            return document.elementFromPoint(Number(x), Number(y));
        return document.activeElement || document.body;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:resolveTarget@6', __javascriptError); throw __javascriptError; }}

    function eventOptions(command, target) { try {
        const rect = target?.getBoundingClientRect?.();
        const x = Number.isFinite(Number(command.x)) ? Number(command.x) : (rect ? rect.left + rect.width / 2 : 0);
        const y = Number.isFinite(Number(command.y)) ? Number(command.y) : (rect ? rect.top + rect.height / 2 : 0);
        return {
            bubbles: true, cancelable: true, composed: true,
            clientX: x, clientY: y, screenX: x, screenY: y,
            button: Number(command.button) || 0, buttons: state.dragging ? 1 : 0,
            ctrlKey: !!command.ctrlKey, shiftKey: !!command.shiftKey,
            altKey: !!command.altKey, metaKey: !!command.metaKey,
            key: String(command.key || ''), code: String(command.code || ''),
            deltaX: Number(command.deltaX) || 0, deltaY: Number(command.deltaY) || 0
        };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:eventOptions@18', __javascriptError); throw __javascriptError; }}

    function setNativeValue(target, value) { try {
        const prototype = target instanceof HTMLTextAreaElement ? HTMLTextAreaElement.prototype : target instanceof HTMLInputElement ? HTMLInputElement.prototype : null;
        const setter = prototype && Object.getOwnPropertyDescriptor(prototype, 'value')?.set;
        if (setter) setter.call(target, value);
        else if ('value' in target) target.value = value;
        else target.textContent = value;
        target.dispatchEvent(new InputEvent('input', { bubbles: true, composed: true, inputType: 'insertText', data: value }));
        target.dispatchEvent(new Event('change', { bubbles: true, composed: true }));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:setNativeValue@33', __javascriptError); throw __javascriptError; }}

    // Read-only eyes: visible viewport structure, not credentials, form values, or hidden DOM.
    // The existing session-scoped browser automation consent and 1-Wire permission matrix
    // govern when this may be requested by a linked peer.
    function inspectView(selector) { try {
        if (sessionStorage.getItem(storageKey) !== 'true' || !state.running)
            throw new Error('Browser automation is not enabled for this PublisherStudio session.');
        const root = selector ? document.querySelector(selector) : document.body;
        if (!(root instanceof Element)) throw new Error('No visible browser view matches the selector.');
        const maxElements = 100, maxBytes = 18000;
        const width = window.innerWidth, height = window.innerHeight;
        const candidates = root.querySelectorAll('button,a,input,textarea,select,[role="button"],[role="tab"],[role="menuitem"],[role="dialog"],[role="textbox"],[data-publication-element],h1,h2,h3,h4,label,[aria-label],.dxbl-popup');
        const nodes = [];
        const compact = (value, length = 100) => String(value || '').replace(/\s+/g, ' ').trim().slice(0, length);
        for (const element of candidates) {
            if (nodes.length >= maxElements) break;
            if (element.closest('[data-sensitive],[data-private],[aria-hidden="true"],input[type="password"],input[type="hidden"]')) continue;
            if (element.matches('[contenteditable], [contenteditable="true"], [type="password"], [type="hidden"]')) continue;
            const style = window.getComputedStyle(element);
            if (style.display === 'none' || style.visibility === 'hidden' || Number(style.opacity) === 0) continue;
            const bounds = element.getBoundingClientRect();
            if (!bounds.width || !bounds.height || bounds.right <= 0 || bounds.bottom <= 0 || bounds.left >= width || bounds.top >= height) continue;
            const tag = element.tagName.toLowerCase();
            const permittedText = (tag === 'button' || tag === 'a' || /^h[1-4]$/.test(tag) || tag === 'label')
                ? [...element.childNodes].filter(node => node.nodeType === Node.TEXT_NODE).map(node => node.textContent).join(' ') : '';
            const label = compact(element.getAttribute('aria-label') || element.getAttribute('title') || permittedText || element.getAttribute('placeholder'));
            const id = compact(element.id, 90);
            const elementId = compact(element.getAttribute('data-element-id'), 90);
            if (!label && !id && !elementId && !element.getAttribute('role')) continue;
            nodes.push({ tag, role: compact(element.getAttribute('role'), 35), label, id, elementId,
                kind: compact(element.getAttribute('data-element-kind'), 45),
                selected: element.getAttribute('data-selected') === 'true',
                disabled: element.matches(':disabled, [aria-disabled="true"]'),
                rect: [Math.round(bounds.x),Math.round(bounds.y),Math.round(bounds.width),Math.round(bounds.height)] });
        }
        const documentNode = document.querySelector('[data-page-id]');
        const snapshot = { path: location.pathname, title: compact(document.title),
            viewport: { width, height },
            publication: documentNode ? { pageId: compact(documentNode.getAttribute('data-page-id'), 90),
                widthMm: compact(documentNode.getAttribute('data-page-width-mm'), 16),
                heightMm: compact(documentNode.getAttribute('data-page-height-mm'), 16) } : null,
            elements: nodes, truncated: nodes.length >= maxElements };
        let result = JSON.stringify(snapshot);
        while (result.length > maxBytes && snapshot.elements.length) {
            snapshot.elements.pop(); snapshot.truncated = true; result = JSON.stringify(snapshot);
        }
        return result;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:inspectView', __javascriptError); throw __javascriptError; }}

    // Panel/Div modules share PublisherStudioPublicationRuntime with standalone website exports.
    // Introspection does not return component configuration, data rows, tokens, or field values.
    function inspectModules() { try {
        if (sessionStorage.getItem(storageKey) !== 'true' || !state.running)
            throw new Error('PublisherStudio browser automation consent is required.');
        const runtime = window.PublisherStudioOrganicModuleRuntime;
        if (!runtime?.list) throw new Error('The module runtime has not initialized.');
        const modules = runtime.list().slice(0, 48).map(module => {
            const target = document.querySelector(`[data-element-id="${module.elementId}"]`);
            const rect = target?.getBoundingClientRect?.();
            return { elementId: module.elementId, kind: module.kind, fields: module.fields,
                methods: module.functions.map(item => item.name), functions: module.functions,
                rect: rect ? [Math.round(rect.x), Math.round(rect.y), Math.round(rect.width), Math.round(rect.height)] : [] };
        });
        const result = { modules, truncated: modules.length >= 64,
            invokeCapability: 'publisher.panel.module.invoke', resultCapability: 'publisher.input.result' };
        while (JSON.stringify(result).length > 16000 && result.modules.length) {
            result.modules.pop(); result.truncated = true;
        }
        return JSON.stringify(result);
    } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:inspectModules', __javascriptError); throw __javascriptError; }}

    async function invokeModule(command) { try {
        if (sessionStorage.getItem(storageKey) !== 'true' || !state.running)
            throw new Error('PublisherStudio browser automation consent is required.');
        const runtime = window.PublisherStudioOrganicModuleRuntime;
        if (!runtime?.describe || !runtime?.invoke) throw new Error('The module runtime is unavailable.');
        const elementId = String(command.selector || '').trim();
        if (!/^[0-9a-f-]{36}$/i.test(elementId)) throw new Error('Invalid module identifier.');
        const method = String(command.key || '').trim();
        const available = runtime.describe(elementId)?.methods || [];
        if (!available.some(name => name.toLowerCase() === method.toLowerCase()))
            throw new Error('The requested function is not available on the current module.');
        let value = null;
        if (command.text) value = JSON.parse(String(command.text));
        const applied = await runtime.invoke({ elementId, method, value });
        if (applied !== true) throw new Error('The module operation did not confirm success.');
        return JSON.stringify({ elementId, method, applied: true });
    } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:invokeModule', __javascriptError); throw __javascriptError; }}

    async function execute(command) { try {
        if (String(command.kind || '').toLowerCase() === 'inspectview') return inspectView(command.selector || 'body');
        if (String(command.kind || '').toLowerCase() === 'inspectmodules') return inspectModules();
        if (String(command.kind || '').toLowerCase() === 'invokemodule') return await invokeModule(command);
        const target = resolveTarget(command.selector, command.x, command.y);
        if (!(target instanceof Element)) throw new Error('No browser element matched the automation command.');
        const options = eventOptions(command, target);
        const kind = String(command.kind || '').toLowerCase();
        if (kind === 'click') target.dispatchEvent(new MouseEvent('click', options));
        else if (kind === 'doubleclick') target.dispatchEvent(new MouseEvent('dblclick', { ...options, detail: 2 }));
        else if (kind === 'contextmenu') target.dispatchEvent(new MouseEvent('contextmenu', { ...options, button: 2 }));
        else if (kind === 'mousemove') dispatchPointerAndMouse(target, 'move', options);
        else if (kind === 'mousedown') {
            state.dragging = true;
            dispatchPointerAndMouse(target, 'down', options);
        }
        else if (kind === 'mouseup') {
            dispatchPointerAndMouse(target, 'up', options);
            state.dragging = false;
        }
        else if (kind === 'wheel') target.dispatchEvent(new WheelEvent('wheel', options));
        else if (kind === 'focus') target.focus?.({ preventScroll: false });
        else if (kind === 'blur') target.blur?.();
        else if (kind === 'typetext') {
            target.focus?.();
            const current = 'value' in target ? String(target.value || '') : String(target.textContent || '');
            setNativeValue(target, current + String(command.text || ''));
        }
        else if (kind === 'setvalue') { target.focus?.(); setNativeValue(target, String(command.text || '')); }
        else if (kind === 'keydown') target.dispatchEvent(new KeyboardEvent('keydown', options));
        else if (kind === 'keyup') target.dispatchEvent(new KeyboardEvent('keyup', options));
        else if (kind === 'keypress') target.dispatchEvent(new KeyboardEvent('keypress', options));
        else throw new Error(`Unsupported browser automation command: ${command.kind}`);
        if (['click', 'doubleclick', 'mousemove', 'mousedown', 'mouseup'].includes(kind))
            moveVisiblePointer(options, state.dragging);
        // A positioned canvas must never be scrolled after each drawing stroke.
        if (!['mousemove', 'mousedown', 'mouseup'].includes(kind))
            target.scrollIntoView?.({ block: 'nearest', inline: 'nearest' });
        return `${command.kind} executed on ${target.tagName.toLowerCase()}${target.id ? '#' + target.id : ''}`;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:execute@43', __javascriptError); throw __javascriptError; }}

    function shouldIgnoreCaptureCloneElement(element) { try {
        const tag = String(element?.tagName || '').toUpperCase();
        return tag.startsWith('DXBL-') || !!element?.classList?.contains('dxbl-toast-portal') || !!element?.matches?.('[data-permanent]');
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:shouldIgnoreCaptureCloneElement', __javascriptError); return false; }}

    async function capture(request) { try {
        const target = resolveTarget(request.selector || 'body');
        if (!(target instanceof Element)) throw new Error('No element matched the screenshot selector.');
        if (typeof window.html2canvas !== 'function') throw new Error('html2canvas is unavailable.');
        const scale = Math.max(.1, Math.min(4, Number(request.scale) || 1));
        const canvas = await window.html2canvas(target, {
            backgroundColor: null, scale, useCORS: true, allowTaint: false,
            logging: false, imageTimeout: 20000, foreignObjectRendering: false,
            ignoreElements: shouldIgnoreCaptureCloneElement
        });
        const jpeg = String(request.format || '').toLowerCase().includes('jp');
        const dataUrl = canvas.toDataURL(jpeg ? 'image/jpeg' : 'image/png', Math.max(.1, Math.min(1, Number(request.quality) || .92)));
        return { dataUrl, pixelWidth: canvas.width, pixelHeight: canvas.height, error: '' };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:capture@71', __javascriptError); throw __javascriptError; }}

    async function completeInput(id, result, error = '') { try {
        const response = await fetch(`/api/automation/input/${encodeURIComponent(id)}/complete`, {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ result, error })
        });
        if (!response.ok) throw new Error(`Automation input completion failed (HTTP ${response.status}).`);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:completeInput@85', __javascriptError); throw __javascriptError; }}

    async function completeScreenshot(id, completion) { try {
        const response = await fetch(`/api/automation/screenshots/${encodeURIComponent(id)}/complete`, {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(completion)
        });
        if (!response.ok) throw new Error(`Automation screenshot completion failed (HTTP ${response.status}).`);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:completeScreenshot@92', __javascriptError); throw __javascriptError; }}

    async function poll() { try {
        if (!state.running) return;
        if (document.visibilityState === 'prerender') {
            state.timer = window.setTimeout(poll, 2000);
            return;
        }
        try {
            const runtimeResponse = await fetch('/api/automation/runtime/status', { cache: 'no-store' });
            if (!runtimeResponse.ok) throw new Error(`Automation runtime status failed (HTTP ${runtimeResponse.status}).`);
            const runtime = await runtimeResponse.json();
            if (!runtime.linked) {
                showAutomationError('connection', new Error('LocalGPT connection is not trusted or linked. Automation was paused; review both peer trust settings.'));
                window.publisherAutomation.disableForSession();
                return;
            }
            const inputResponse = await fetch('/api/automation/input/pending?maximum=20', { cache: 'no-store' });
            if (!inputResponse.ok) throw new Error(`Automation input polling failed (HTTP ${inputResponse.status}).`);
            for (const command of await inputResponse.json()) {
                try { await completeInput(command.id, await execute(command)); }
                catch (error) {
                    showAutomationError(`command ${command.kind || 'unknown'}`, error);
                    try { await completeInput(command.id, '', error?.message || String(error)); }
                    catch (completionError) { showAutomationError('input completion', completionError); }
                }
            }
            const screenshotResponse = await fetch('/api/automation/screenshots/pending?maximum=3', { cache: 'no-store' });
            if (!screenshotResponse.ok) throw new Error(`Automation screenshot polling failed (HTTP ${screenshotResponse.status}).`);
            for (const request of await screenshotResponse.json()) {
                try { await completeScreenshot(request.id, await capture(request)); }
                catch (error) {
                    showAutomationError('screenshot', error);
                    try { await completeScreenshot(request.id, { dataUrl: '', pixelWidth: 0, pixelHeight: 0, error: error?.message || String(error) }); }
                    catch (completionError) { showAutomationError('screenshot completion', completionError); }
                }
            }
        } catch (error) {
            showAutomationError('transport', error);
        } finally {
            if (state.running) state.timer = window.setTimeout(poll, 2000);
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:poll@99', __javascriptError); throw __javascriptError; }}

    window.publisherAutomation = {
        start() { try {
            if (sessionStorage.getItem(storageKey) !== 'true' || state.running) return false;
            state.running = true;
            void poll();
            return true;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:start@130', __javascriptError); throw __javascriptError; }},
        stop() { try {
            state.running = false;
            state.dragging = false;
            state.pointer?.remove();
            state.pointer = null;
            clearTimeout(state.timer);
        } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:stop@136', __javascriptError); throw __javascriptError; }},
        enableForSession() { try {
            sessionStorage.setItem(storageKey, 'true');
            return this.start();
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:enableForSession@137', __javascriptError); throw __javascriptError; }},
        disableForSession() { try {
            sessionStorage.removeItem(storageKey);
            this.stop();
            return false;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:disableForSession@141', __javascriptError); throw __javascriptError; }},
        isEnabled() { try { return sessionStorage.getItem(storageKey) === 'true' && state.running;  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:isEnabled@146', __javascriptError); throw __javascriptError; }},
        execute,
        capture
    };
    window.addEventListener('DOMContentLoaded', () => { try {
        if (sessionStorage.getItem(storageKey) === 'true') window.publisherAutomation.start();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:callback:window.addEventListener@150', __javascriptError); throw __javascriptError; }}, { once: true });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:ArrowFunction@2', __javascriptError); throw __javascriptError; }})();

// Guard exported browser namespaces after the file has initialized.
publisherStudioDiagnostics.guardObject("publisherAutomation", window.publisherAutomation);
