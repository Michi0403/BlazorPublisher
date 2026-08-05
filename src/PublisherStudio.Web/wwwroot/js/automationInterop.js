// javascript-diagnostics: guarded
var publisherStudioDiagnostics = globalThis.publisherStudioJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`PublisherStudio JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("PublisherStudio fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`PublisherStudio fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
(() => { try {
    const storageKey = 'publisherstudio.automation.enabled';
    const state = { running: false, timer: 0 };

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
            button: Number(command.button) || 0, buttons: 1,
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

    async function execute(command) { try {
        const target = resolveTarget(command.selector, command.x, command.y);
        if (!(target instanceof Element)) throw new Error('No browser element matched the automation command.');
        const options = eventOptions(command, target);
        const kind = String(command.kind || '').toLowerCase();
        if (kind === 'click') target.dispatchEvent(new MouseEvent('click', options));
        else if (kind === 'doubleclick') target.dispatchEvent(new MouseEvent('dblclick', { ...options, detail: 2 }));
        else if (kind === 'contextmenu') target.dispatchEvent(new MouseEvent('contextmenu', { ...options, button: 2 }));
        else if (kind === 'mousemove') target.dispatchEvent(new PointerEvent('pointermove', options));
        else if (kind === 'mousedown') target.dispatchEvent(new PointerEvent('pointerdown', options));
        else if (kind === 'mouseup') target.dispatchEvent(new PointerEvent('pointerup', options));
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
        target.scrollIntoView?.({ block: 'nearest', inline: 'nearest' });
        return `${command.kind} executed on ${target.tagName.toLowerCase()}${target.id ? '#' + target.id : ''}`;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:execute@43', __javascriptError); throw __javascriptError; }}

    async function capture(request) { try {
        const target = resolveTarget(request.selector || 'body');
        if (!(target instanceof Element)) throw new Error('No element matched the screenshot selector.');
        if (typeof window.html2canvas !== 'function') throw new Error('html2canvas is unavailable.');
        const scale = Math.max(.1, Math.min(4, Number(request.scale) || 1));
        const canvas = await window.html2canvas(target, {
            backgroundColor: null, scale, useCORS: true, allowTaint: false,
            logging: false, imageTimeout: 20000, foreignObjectRendering: false
        });
        const jpeg = String(request.format || '').toLowerCase().includes('jp');
        const dataUrl = canvas.toDataURL(jpeg ? 'image/jpeg' : 'image/png', Math.max(.1, Math.min(1, Number(request.quality) || .92)));
        return { dataUrl, pixelWidth: canvas.width, pixelHeight: canvas.height, error: '' };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:capture@71', __javascriptError); throw __javascriptError; }}

    async function completeInput(id, result, error = '') { try {
        await fetch(`/api/automation/input/${encodeURIComponent(id)}/complete`, {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ result, error })
        });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:completeInput@85', __javascriptError); throw __javascriptError; }}

    async function completeScreenshot(id, completion) { try {
        await fetch(`/api/automation/screenshots/${encodeURIComponent(id)}/complete`, {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(completion)
        });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:completeScreenshot@92', __javascriptError); throw __javascriptError; }}

    async function poll() { try {
        if (!state.running || document.visibilityState === 'prerender') return;
        try {
            const runtimeResponse = await fetch('/api/automation/runtime/status', { cache: 'no-store' });
            const runtime = runtimeResponse.ok ? await runtimeResponse.json() : { linked: false };
            if (!runtime.linked) {
                window.publisherAutomation.disableForSession();
                return;
            }
            const inputResponse = await fetch('/api/automation/input/pending?maximum=20', { cache: 'no-store' });
            if (inputResponse.ok) {
                for (const command of await inputResponse.json()) {
                    try { await completeInput(command.id, await execute(command)); }
                    catch (error) { await completeInput(command.id, '', error?.message || String(error)); }
                }
            }
            const screenshotResponse = await fetch('/api/automation/screenshots/pending?maximum=3', { cache: 'no-store' });
            if (screenshotResponse.ok) {
                for (const request of await screenshotResponse.json()) {
                    try { await completeScreenshot(request.id, await capture(request)); }
                    catch (error) { await completeScreenshot(request.id, { dataUrl: '', pixelWidth: 0, pixelHeight: 0, error: error?.message || String(error) }); }
                }
            }
        } catch (error) {
            console.debug('PublisherStudio automation polling paused.', error);
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
        stop() { try { state.running = false; clearTimeout(state.timer);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/automationInterop.js:stop@136', __javascriptError); throw __javascriptError; }},
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
