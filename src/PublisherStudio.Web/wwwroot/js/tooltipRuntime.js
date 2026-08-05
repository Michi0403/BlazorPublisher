// javascript-diagnostics: guarded
var publisherStudioDiagnostics = globalThis.publisherStudioJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`PublisherStudio JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("PublisherStudio fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`PublisherStudio fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
(() => { try {
    'use strict';

    const selector = 'button,input,select,textarea,a[href],[role="button"],[role="tab"],[role="menuitem"],[role="option"],[role="checkbox"],[role="switch"],[data-help]';
    const prepared = new WeakMap();
    const catalog = new Map(Object.entries({
        'new publication': 'Create a new blank publication. Unsaved work in the current publication should be saved first.',
        'open publication': 'Open an existing PublisherStudio publication from a local file.',
        'save publication': 'Save the complete publication, including pages, objects, data, media settings, and streaming setup.',
        'undo': 'Undo the most recent editable action in the current studio or publication.',
        'redo': 'Restore the most recently undone action.',
        'copy': 'Copy the selected publication object or objects to the internal clipboard.',
        'paste': 'Paste the last copied publication object or objects onto the current page.',
        'duplicate': 'Create an independent copy of the current selection or item.',
        'delete': 'Remove the current selection or item. Use Undo when the command is part of the publication history.',
        'streaming setup': 'Open Streaming Studio to configure providers, outputs, recording, LAN delivery, devices, and hotkeys.',
        'dry run': 'Start the complete streaming pipeline without sending provider outputs live.',
        'start streaming': 'Start a live session using the enabled outputs, recording, and LAN settings.',
        'stop session': 'Stop the active streaming or dry-run session and finalize encoders and recordings.',
        'new provider': 'Create a reusable provider profile stored on this machine.',
        'save provider': 'Encrypt and save the provider profile for reuse by this and other publications.',
        'duplicate provider': 'Create a new provider profile from the current settings without copying stored secrets.',
        'delete provider': 'Delete the selected machine provider profile and remove references from the current streaming draft.',
        'add output': 'Add an independently scaled and encoded publication output.',
        'recommended settings': 'Apply provider-oriented resolution, frame-rate, bitrate, and keyframe defaults to the selected output.',
        'duplicate output': 'Create another publication output with the same provider and encoding settings.',
        'remove output': 'Remove the selected publication output and any recording selection that references it.',
        'enable recording': 'Turn recording on for future live and dry-run sessions.',
        'disable recording': 'Turn recording off while preserving the configured recording settings.',
        'use clean master': 'Record the shared publication render before provider-specific scaling and encoding.',
        'use enabled outputs': 'Record a separate file for every currently enabled publication output.',
        'enable lan output': 'Allow PublisherStudio to start the configured LAN listener during a session.',
        'disable lan output': 'Prevent PublisherStudio from exposing any LAN playback listener.',
        'local computer only': 'Bind LAN playback to 127.0.0.1 so only this computer can connect.',
        'browser + hls': 'Enable low-latency browser playback and HLS delivery for browsers or VLC.',
        'add device profile': 'Create a reusable camera, microphone, capture-device, application, or window profile.',
        'refresh browser devices': 'Ask the browser for available cameras and microphones. Permission may be required before labels appear.',
        'refresh native devices': 'Ask PublisherStudio and FFmpeg to discover native capture devices, audio devices, and applications.',
        'save profiles': 'Save the current reusable device profiles to the machine profile store.',
        'save device profiles': 'Save the current reusable device profiles to the machine profile store.',
        'add hotkey': 'Add a streaming command shortcut. Global shortcuts are active only while the PublisherStudio streaming session is running.',
        'save machine options': 'Save FFmpeg, encoder, recording-directory, provider, and device settings on this machine.',
        'apply streaming setup': 'Apply publication-specific output, recording, LAN, page, and hotkey settings and close Streaming Studio.',
        'cancel': 'Close the current studio without applying unsaved changes made in that studio.',
        'close': 'Close the current window or studio.',
        'edit story': 'Open Story Editor for the selected text frame.',
        'edit in picture studio': 'Open the selected picture in Picture Studio for non-destructive editing.',
        'edit in media studio': 'Open the selected audio or video object in its media studio.',
        'edit spreadsheet': 'Open the selected spreadsheet object in Spreadsheet Studio.',
        'edit data visual': 'Open the selected chart, gauge, table, or KPI in Data Visual Studio.',
        'open component studio': 'Open Component Studio for the selected interactive DevExtreme component.',
        'edit barcode / qr': 'Open Barcode Studio for the selected barcode or QR object.',
        'play range': 'Play the currently selected media range in the studio preview.',
        'pause': 'Pause the current preview without changing trim or playback settings.',
        'reset trim': 'Restore the full source duration as the selected media range.',
        'download recording': 'Download the completed browser recording in its retained original form.',
        'refresh data': 'Reload the selected object from its configured publication data source or endpoint.',
        'manage publication data': 'Open the publication data manager to create, inspect, or refresh reusable datasets.',
        'fit page': 'Choose a zoom level that fits the full publication page inside the current workspace.',
        'zoom in': 'Increase publication page magnification.',
        'zoom out': 'Decrease publication page magnification.'
    }));

    let tooltip = null;
    let activeTarget = null;
    let pendingTarget = null;
    let showTimer = 0;
    let hideTimer = 0;

    function clean(value) { try {
        return String(value || '')
            .replace(/[✓✔☑＋+…]/g, ' ')
            .replace(/\s+/g, ' ')
            .trim();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:clean@71', __javascriptError); throw __javascriptError; }}

    function key(value) { try {
        return clean(value).toLocaleLowerCase();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:key@78', __javascriptError); throw __javascriptError; }}

    function elementText(element) { try {
        return clean(element.getAttribute('aria-label')
            || element.getAttribute('data-text')
            || element.innerText
            || element.textContent);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:elementText@82', __javascriptError); throw __javascriptError; }}

    function labelText(element) { try {
        const label = element.closest?.('label');
        if (!label) return clean(element.getAttribute('aria-label') || element.getAttribute('placeholder'));
        const clone = label.cloneNode(true);
        clone.querySelectorAll('input,select,textarea,button,small').forEach(node => { try { return (node.remove()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:callback:clone.querySelectorAll(\'input,select,textarea,button,small\').forEach@93', __javascriptError); throw __javascriptError; } });
        return clean(clone.textContent);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:labelText@89', __javascriptError); throw __javascriptError; }}

    function explicitHelp(element) { try {
        const dataHelp = clean(element.getAttribute('data-help'));
        if (dataHelp) return dataHelp;

        const helpOwner = element.closest?.('[data-help]');
        const inheritedHelp = helpOwner && helpOwner !== element
            ? clean(helpOwner.getAttribute('data-help'))
            : '';
        if (inheritedHelp) return inheritedHelp;

        const storedTitle = clean(element.dataset.publisherNativeTitle);
        if (storedTitle) return storedTitle;

        const title = clean(element.getAttribute('title'));
        if (title) {
            element.dataset.publisherNativeTitle = title;
            element.removeAttribute('title');
            return title;
        }

        return '';
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:explicitHelp@97', __javascriptError); throw __javascriptError; }}

    function catalogHelp(text) { try {
        const normalized = key(text);
        if (!normalized) return '';
        if (catalog.has(normalized)) return catalog.get(normalized);

        for (const [candidate, description] of catalog) {
            if (normalized === candidate || normalized.startsWith(`${candidate} `) || normalized.endsWith(` ${candidate}`))
                return description;
        }

        return '';
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:catalogHelp@120', __javascriptError); throw __javascriptError; }}

    function describeInput(element) { try {
        const label = labelText(element) || 'this value';
        const placeholder = clean(element.getAttribute('placeholder'));
        const type = key(element.getAttribute('type') || element.tagName);

        if (type === 'checkbox')
            return `Turn “${label}” on or off. The setting is kept with the current publication or machine profile according to this studio.`;
        if (type === 'range')
            return `Adjust “${label}”. Drag the slider or use the arrow keys for precise changes.`;
        if (type === 'number')
            return `Set “${label}”. Type a value or use the step buttons; invalid values are constrained by the field limits.`;
        if (type === 'password')
            return `Enter “${label}”. The value stays masked; streaming secrets are stored only through the machine profile workflow.`;
        if (element.tagName === 'SELECT')
            return `Choose “${label}” from the available options. The selection updates the current studio draft.`;
        if (element.tagName === 'TEXTAREA')
            return `Enter the detailed value for “${label}”. Changes apply to the current studio draft.`;

        const hint = placeholder ? ` Suggested format: ${placeholder}.` : '';
        return `Enter or edit “${label}”.${hint}`;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:describeInput@133', __javascriptError); throw __javascriptError; }}

    function describeButton(element) { try {
        const text = elementText(element);
        const known = catalogHelp(text);
        if (known) return known;

        if (element.getAttribute('role') === 'tab')
            return `Open the “${text || 'selected'}” ribbon tab and show its related commands.`;

        if (!text)
            return 'Run this command for the current selection or open studio.';

        return `Run the “${text}” command. It applies to the current selection, item, page, or open studio.`;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:describeButton@155', __javascriptError); throw __javascriptError; }}

    function descriptionFor(element) { try {
        const explicit = explicitHelp(element);
        if (explicit) return explicit;

        if (element.matches('input,select,textarea'))
            return describeInput(element);

        return describeButton(element);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:descriptionFor@169', __javascriptError); throw __javascriptError; }}

    function prepare(element) { try {
        if (!(element instanceof Element)) return;
        const description = descriptionFor(element);
        if (!description) return;

        const previous = prepared.get(element);
        if (previous === description) return;

        prepared.set(element, description);
        element.dataset.publisherTooltip = description;

        if (!element.hasAttribute('aria-label') && !elementText(element) && element.matches('button,[role="button"]'))
            element.setAttribute('aria-label', description);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:prepare@179', __javascriptError); throw __javascriptError; }}

    function scan(root) { try {
        if (!root) return;
        if (root instanceof Element && root.matches(selector)) prepare(root);
        root.querySelectorAll?.(selector).forEach(prepare);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:scan@194', __javascriptError); throw __javascriptError; }}

    function supportsTopLayer(popup) { try {
        return typeof popup?.showPopover === 'function' && typeof popup?.hidePopover === 'function';
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:supportsTopLayer@200', __javascriptError); throw __javascriptError; }}

    function isPopoverOpen(popup) { try {
        if (!supportsTopLayer(popup)) return false;
        try { return popup.matches(':popover-open'); } catch { return false; }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:isPopoverOpen@204', __javascriptError); throw __javascriptError; }}

    function ensureTooltip() { try {
        if (tooltip?.isConnected) return tooltip;
        tooltip = document.createElement('div');
        tooltip.className = 'publisher-help-tooltip';
        tooltip.setAttribute('role', 'tooltip');
        tooltip.setAttribute('popover', 'manual');
        document.body.appendChild(tooltip);
        tooltip.hidden = !supportsTopLayer(tooltip);
        return tooltip;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:ensureTooltip@209', __javascriptError); throw __javascriptError; }}

    function overlayZIndex() { try {
        let highest = 1000;
        document.querySelectorAll('.dx-overlay-wrapper,.dx-popup-wrapper,.dx-tooltip-wrapper,.streaming-studio-overlay,[role="dialog"],[data-publication-element]').forEach(element => { try {
            if (!(element instanceof HTMLElement || element instanceof SVGElement) || element.hidden) return;
            const style = getComputedStyle(element);
            if (style.display === 'none' || style.visibility === 'hidden' || Number.parseFloat(style.opacity || '1') <= 0) return;
            const value = Number.parseInt(style.zIndex, 10);
            if (Number.isFinite(value)) highest = Math.max(highest, value);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:callback:document.querySelectorAll(\'.dx-overlay-wrapper,.dx-popup-wrapper,.dx-t@222', __javascriptError); throw __javascriptError; }});
        return Math.min(2147483000, highest + 2);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:overlayZIndex@220', __javascriptError); throw __javascriptError; }}

    function position(target) { try {
        const popup = ensureTooltip();
        popup.style.zIndex = String(overlayZIndex());
        const rect = target.getBoundingClientRect();
        const popupRect = popup.getBoundingClientRect();
        const margin = 10;
        let left = rect.left + Math.min(rect.width / 2, 120) - popupRect.width / 2;
        left = Math.max(margin, Math.min(left, window.innerWidth - popupRect.width - margin));

        let top = rect.bottom + 8;
        if (top + popupRect.height > window.innerHeight - margin)
            top = Math.max(margin, rect.top - popupRect.height - 8);

        popup.style.left = `${Math.round(left)}px`;
        popup.style.top = `${Math.round(top)}px`;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:position@232', __javascriptError); throw __javascriptError; }}

    function openTooltip(popup) { try {
        if (supportsTopLayer(popup)) {
            popup.hidden = false;
            if (!isPopoverOpen(popup)) {
                try { popup.showPopover(); }
                catch { popup.hidden = false; }
            }
        } else popup.hidden = false;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:openTooltip@249', __javascriptError); throw __javascriptError; }}

    function closeTooltip(popup) { try {
        if (!popup) return;
        if (supportsTopLayer(popup) && isPopoverOpen(popup)) {
            try { popup.hidePopover(); return; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:suppressed-catch@262', __caughtJavaScriptError);  }
        }
        popup.hidden = true;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:closeTooltip@259', __javascriptError); throw __javascriptError; }}

    function show(target) { try {
        const description = target?.dataset?.publisherTooltip;
        if (!description || !target.isConnected) return;

        pendingTarget = null;
        activeTarget = target;
        const popup = ensureTooltip();
        popup.textContent = description;
        openTooltip(popup);
        requestAnimationFrame(() => { try {
            if (activeTarget !== target) return;
            popup.classList.add('visible');
            position(target);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:callback:requestAnimationFrame@276', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:show@267', __javascriptError); throw __javascriptError; }}

    function scheduleShow(target, delay = 420) { try {
        clearTimeout(hideTimer);
        if (activeTarget === target && tooltip?.classList.contains('visible')) return;
        if (pendingTarget === target && showTimer) return;
        clearTimeout(showTimer);
        pendingTarget = target;
        showTimer = window.setTimeout(() => { try {
            showTimer = 0;
            if (pendingTarget === target) show(target);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:callback:window.setTimeout@289', __javascriptError); throw __javascriptError; }}, delay);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:scheduleShow@283', __javascriptError); throw __javascriptError; }}

    function hide(immediate = false) { try {
        clearTimeout(showTimer);
        clearTimeout(hideTimer);
        showTimer = 0;
        pendingTarget = null;
        const action = () => { try {
            activeTarget = null;
            if (!tooltip) return;
            tooltip.classList.remove('visible');
            window.setTimeout(() => { try {
                if (!tooltip?.classList.contains('visible')) closeTooltip(tooltip);
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:callback:window.setTimeout@304', __javascriptError); throw __javascriptError; }}, 120);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:action@300', __javascriptError); throw __javascriptError; }};
        if (immediate) action();
        else hideTimer = window.setTimeout(action, 80);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:hide@295', __javascriptError); throw __javascriptError; }}

    function candidateFromNode(node) { try {
        if (!(node instanceof Element)) return null;
        const candidate = node.matches(selector) ? node : node.closest?.(selector);
        if (!candidate) return null;
        if (!candidate.dataset.publisherTooltip) prepare(candidate);
        return candidate.dataset.publisherTooltip ? candidate : null;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:candidateFromNode@312', __javascriptError); throw __javascriptError; }}

    function targetFrom(event) { try {
        const path = typeof event?.composedPath === 'function' ? event.composedPath() : [event?.target];
        for (const node of path) {
            const candidate = candidateFromNode(node);
            if (candidate) return candidate;
        }
        return candidateFromNode(event?.target);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:targetFrom@320', __javascriptError); throw __javascriptError; }}

    document.addEventListener('pointerover', event => { try {
        const target = targetFrom(event);
        if (!target) return;
        if (target === activeTarget || target === pendingTarget) return;
        scheduleShow(target);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:callback:document.addEventListener@329', __javascriptError); throw __javascriptError; }}, true);

    document.addEventListener('pointerout', event => { try {
        const leaving = targetFrom(event);
        const next = candidateFromNode(event.relatedTarget);
        const current = activeTarget || pendingTarget;
        if (leaving && next === leaving) return;
        if (current && next === current) return;
        hide();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:callback:document.addEventListener@336', __javascriptError); throw __javascriptError; }}, true);

    document.addEventListener('focusin', event => { try {
        const target = targetFrom(event);
        if (target) scheduleShow(target, 220);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:callback:document.addEventListener@345', __javascriptError); throw __javascriptError; }}, true);

    document.addEventListener('focusout', () => { try { return (hide()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:callback:document.addEventListener@350', __javascriptError); throw __javascriptError; } }, true);
    document.addEventListener('pointerdown', () => { try { return (hide(true)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:callback:document.addEventListener@351', __javascriptError); throw __javascriptError; } }, true);
    document.addEventListener('contextmenu', () => { try { return (hide(true)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:callback:document.addEventListener@352', __javascriptError); throw __javascriptError; } }, true);
    document.addEventListener('click', () => { try { return (hide(true)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:callback:document.addEventListener@353', __javascriptError); throw __javascriptError; } }, true);
    document.addEventListener('keydown', event => { try {
        if (event.key === 'Escape') hide(true);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:callback:document.addEventListener@354', __javascriptError); throw __javascriptError; }}, true);
    window.addEventListener('scroll', () => { try { return (hide(true)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:callback:window.addEventListener@357', __javascriptError); throw __javascriptError; } }, true);
    window.addEventListener('resize', () => { try {
        if (activeTarget) position(activeTarget);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:callback:window.addEventListener@358', __javascriptError); throw __javascriptError; }});
    document.addEventListener('fullscreenchange', () => { try {
        if (!activeTarget || !tooltip) return;
        const target = activeTarget;
        tooltip.classList.remove('visible');
        closeTooltip(tooltip);
        requestAnimationFrame(() => { try { return (show(target)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:callback:requestAnimationFrame@366', __javascriptError); throw __javascriptError; } });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:callback:document.addEventListener@361', __javascriptError); throw __javascriptError; }});

    const observer = new MutationObserver(records => { try {
        for (const record of records) {
            record.addedNodes.forEach(node => { try {
                if (node instanceof Element) scan(node);
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:callback:record.addedNodes.forEach@371', __javascriptError); throw __javascriptError; }});
            if (record.type === 'attributes' && record.target instanceof Element)
                prepare(record.target);
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:ArrowFunction@369', __javascriptError); throw __javascriptError; }});

    function start() { try {
        scan(document);
        observer.observe(document.documentElement, {
            subtree: true,
            childList: true,
            attributes: true,
            attributeFilter: ['title', 'aria-label', 'data-help', 'placeholder', 'disabled']
        });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:start@379', __javascriptError); throw __javascriptError; }}

    if (document.readyState === 'loading')
        document.addEventListener('DOMContentLoaded', start, { once: true });
    else
        start();

    window.PublisherStudioTooltips = {
        refresh(root = document) { try { scan(root);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:refresh@395', __javascriptError); throw __javascriptError; }},
        hide() { try { hide(true);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:hide@396', __javascriptError); throw __javascriptError; }}
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/tooltipRuntime.js:ArrowFunction@2', __javascriptError); throw __javascriptError; }})();

// Guard exported browser namespaces after the file has initialized.
publisherStudioDiagnostics.guardObject("PublisherStudioTooltips", window.PublisherStudioTooltips);
