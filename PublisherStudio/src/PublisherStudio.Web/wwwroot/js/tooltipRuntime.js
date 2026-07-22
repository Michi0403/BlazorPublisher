(() => {
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
    let showTimer = 0;
    let hideTimer = 0;

    function clean(value) {
        return String(value || '')
            .replace(/[✓✔☑＋+…]/g, ' ')
            .replace(/\s+/g, ' ')
            .trim();
    }

    function key(value) {
        return clean(value).toLocaleLowerCase();
    }

    function elementText(element) {
        return clean(element.getAttribute('aria-label')
            || element.getAttribute('data-text')
            || element.innerText
            || element.textContent);
    }

    function labelText(element) {
        const label = element.closest?.('label');
        if (!label) return clean(element.getAttribute('aria-label') || element.getAttribute('placeholder'));
        const clone = label.cloneNode(true);
        clone.querySelectorAll('input,select,textarea,button,small').forEach(node => node.remove());
        return clean(clone.textContent);
    }

    function explicitHelp(element) {
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
    }

    function catalogHelp(text) {
        const normalized = key(text);
        if (!normalized) return '';
        if (catalog.has(normalized)) return catalog.get(normalized);

        for (const [candidate, description] of catalog) {
            if (normalized === candidate || normalized.startsWith(`${candidate} `) || normalized.endsWith(` ${candidate}`))
                return description;
        }

        return '';
    }

    function describeInput(element) {
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
    }

    function describeButton(element) {
        const text = elementText(element);
        const known = catalogHelp(text);
        if (known) return known;

        if (element.getAttribute('role') === 'tab')
            return `Open the “${text || 'selected'}” ribbon tab and show its related commands.`;

        if (!text)
            return 'Run this command for the current selection or open studio.';

        return `Run the “${text}” command. It applies to the current selection, item, page, or open studio.`;
    }

    function descriptionFor(element) {
        const explicit = explicitHelp(element);
        if (explicit) return explicit;

        if (element.matches('input,select,textarea'))
            return describeInput(element);

        return describeButton(element);
    }

    function prepare(element) {
        if (!(element instanceof Element)) return;
        const description = descriptionFor(element);
        if (!description) return;

        const previous = prepared.get(element);
        if (previous === description) return;

        prepared.set(element, description);
        element.dataset.publisherTooltip = description;

        if (!element.hasAttribute('aria-label') && !elementText(element) && element.matches('button,[role="button"]'))
            element.setAttribute('aria-label', description);
    }

    function scan(root) {
        if (!root) return;
        if (root instanceof Element && root.matches(selector)) prepare(root);
        root.querySelectorAll?.(selector).forEach(prepare);
    }

    function ensureTooltip() {
        if (tooltip?.isConnected) return tooltip;
        tooltip = document.createElement('div');
        tooltip.className = 'publisher-help-tooltip';
        tooltip.setAttribute('role', 'tooltip');
        tooltip.hidden = true;
        document.body.appendChild(tooltip);
        return tooltip;
    }

    function overlayZIndex() {
        let highest = 1000;
        document.querySelectorAll('.dx-overlay-wrapper,.dx-popup-wrapper,.streaming-studio-overlay,[role="dialog"]').forEach(element => {
            if (!(element instanceof HTMLElement) || element.hidden) return;
            const style = getComputedStyle(element);
            if (style.display === 'none' || style.visibility === 'hidden') return;
            const value = Number.parseInt(style.zIndex, 10);
            if (Number.isFinite(value)) highest = Math.max(highest, value);
        });
        return Math.min(2147483000, highest + 2);
    }

    function position(target) {
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
    }

    function show(target) {
        const description = target?.dataset?.publisherTooltip;
        if (!description || !target.isConnected) return;

        activeTarget = target;
        const popup = ensureTooltip();
        popup.textContent = description;
        popup.hidden = false;
        requestAnimationFrame(() => {
            if (activeTarget !== target) return;
            popup.classList.add('visible');
            position(target);
        });
    }

    function scheduleShow(target, delay = 420) {
        clearTimeout(hideTimer);
        clearTimeout(showTimer);
        if (activeTarget === target && tooltip?.classList.contains('visible')) return;
        showTimer = window.setTimeout(() => show(target), delay);
    }

    function hide(immediate = false) {
        clearTimeout(showTimer);
        clearTimeout(hideTimer);
        const action = () => {
            activeTarget = null;
            if (!tooltip) return;
            tooltip.classList.remove('visible');
            window.setTimeout(() => {
                if (!tooltip?.classList.contains('visible')) tooltip.hidden = true;
            }, 120);
        };
        if (immediate) action();
        else hideTimer = window.setTimeout(action, 80);
    }

    function targetFrom(event) {
        const target = event.target instanceof Element ? event.target.closest(selector) : null;
        return target?.dataset?.publisherTooltip ? target : null;
    }

    document.addEventListener('pointerover', event => {
        const target = targetFrom(event);
        if (target) scheduleShow(target);
    }, true);

    document.addEventListener('pointerout', event => {
        const leaving = event.target instanceof Element ? event.target.closest(selector) : null;
        const next = event.relatedTarget instanceof Node ? event.relatedTarget : null;
        if (leaving && next && leaving.contains(next)) return;
        hide();
    }, true);

    document.addEventListener('focusin', event => {
        const target = targetFrom(event);
        if (target) scheduleShow(target, 220);
    }, true);

    document.addEventListener('focusout', () => hide(), true);
    document.addEventListener('pointerdown', () => hide(true), true);
    document.addEventListener('contextmenu', () => hide(true), true);
    document.addEventListener('click', () => hide(true), true);
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape') hide(true);
    }, true);
    window.addEventListener('scroll', () => hide(true), true);
    window.addEventListener('resize', () => {
        if (activeTarget) position(activeTarget);
    });

    const observer = new MutationObserver(records => {
        for (const record of records) {
            record.addedNodes.forEach(node => {
                if (node instanceof Element) scan(node);
            });
            if (record.type === 'attributes' && record.target instanceof Element)
                prepare(record.target);
        }
    });

    function start() {
        scan(document);
        observer.observe(document.documentElement, {
            subtree: true,
            childList: true,
            attributes: true,
            attributeFilter: ['title', 'aria-label', 'data-help', 'placeholder', 'disabled']
        });
    }

    if (document.readyState === 'loading')
        document.addEventListener('DOMContentLoaded', start, { once: true });
    else
        start();

    window.PublisherStudioTooltips = {
        refresh(root = document) { scan(root); },
        hide() { hide(true); }
    };
})();
