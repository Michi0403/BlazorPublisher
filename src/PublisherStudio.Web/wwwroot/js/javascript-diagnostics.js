(() => { try {
    "use strict";

    // javascript-diagnostics: guarded-runtime
    let dotNetReference = null;
    let reporting = false;
    const pendingReports = new Map();
    const wrappedCallbacks = new WeakMap();
    const lifecycleTrace = [];
    const lifecycleTraceLimit = 48;
    let lifecycleObserver = null;

    function summarizeLifecycleElement(value) { try {
        try {
            if (!(value instanceof Element)) return null;
            const classes = [...value.classList].slice(0, 6);
            return {
                tag: value.localName || value.tagName?.toLowerCase?.() || '',
                id: value.id || '',
                classes,
                connected: Boolean(value.isConnected),
                loaded: value.hasAttribute('data-qa-dxbl-loaded'),
                defined: Boolean(value.localName && globalThis.customElements?.get?.(value.localName)),
                shadowRoot: Boolean(value.shadowRoot),
                childElementCount: value.childElementCount,
                branchId: value.getAttribute('branch-id') || '',
                open: value.hasAttribute('x-is-open') || value.hasAttribute('is-shown'),
                parent: value.parentElement?.localName || ''
            };
        } catch (error) {
            console.error('PublisherStudio lifecycle diagnostics could not summarize an element.', error);
            return null;
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:summarizeLifecycleElement.', __javascriptError); throw __javascriptError; }}

    function recordLifecycle(kind, value, detail = '') { try {
        try {
            const element = value instanceof Element ? value : null;
            const panelOpen = Boolean(document.querySelector?.('.panel-studio-dialog'));
            const panelRelated = Boolean(element?.closest?.('.panel-studio-dialog'))
                || Boolean(element?.matches?.('.panel-studio-dialog,.panel-studio-canvas,dxbl-modal,dxbl-modal-root,dxbl-modal-dialog,dxbl-popup-portal,dxbl-popup-cell'))
                || (panelOpen && Boolean(element?.localName?.startsWith?.('dxbl-')));
            if (!panelRelated && !String(kind || '').startsWith('panel-studio')) return;
            lifecycleTrace.push({
                at: new Date().toISOString(),
                kind: String(kind || 'unknown'),
                detail: String(detail || '').slice(0, 240),
                element: summarizeLifecycleElement(element)
            });
            if (lifecycleTrace.length > lifecycleTraceLimit) lifecycleTrace.splice(0, lifecycleTrace.length - lifecycleTraceLimit);
        } catch (error) {
            console.error('PublisherStudio lifecycle diagnostics could not record a transition.', error);
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:recordLifecycle.', __javascriptError); throw __javascriptError; }}

    function captureLifecycleSnapshot(reason = 'browser-error') { try {
        try {
            const dialog = document.querySelector?.('.panel-studio-dialog');
            const canvas = dialog?.querySelector?.('.panel-studio-canvas');
            const modalDialog = dialog?.closest?.('dxbl-modal-dialog');
            const modalRoot = dialog?.closest?.('dxbl-modal-root');
            const modal = dialog?.closest?.('dxbl-modal');
            const dxElements = dialog ? [...dialog.querySelectorAll('*')].filter(element => element.localName?.startsWith?.('dxbl-')) : [];
            const pendingElements = dxElements.filter(element => !element.hasAttribute('data-qa-dxbl-loaded'));
            const pending = pendingElements.slice(0, 24).map(summarizeLifecycleElement);
            const portals = [...document.querySelectorAll?.('dxbl-popup-portal') || []].slice(-16).map(summarizeLifecycleElement);
            return {
                reason: String(reason || 'browser-error'),
                at: new Date().toISOString(),
                readyState: document.readyState,
                panelStudio: {
                    dialogCount: document.querySelectorAll?.('.panel-studio-dialog')?.length || 0,
                    dialogConnected: Boolean(dialog?.isConnected),
                    source: dialog?.getAttribute?.('data-panel-studio-source') || '',
                    bindingId: dialog?.getAttribute?.('data-panel-studio-binding-id') || canvas?.dataset?.panelStudioBindingId || '',
                    viewId: dialog?.getAttribute?.('data-panel-studio-view-id') || '',
                    authoringRevision: dialog?.getAttribute?.('data-panel-studio-authoring-revision') || '',
                    previewRevision: dialog?.getAttribute?.('data-panel-studio-preview-revision') || '',
                    canvasConnected: Boolean(canvas?.isConnected),
                    canvasBindingState: canvas?.dataset?.panelStudioBindingState || '',
                    canvasArrange: canvas?.dataset?.panelStudioArrange || '',
                    panelRootPresent: Boolean(canvas?.querySelector?.('[data-panel-root]'))
                },
                devExpress: {
                    modal: summarizeLifecycleElement(modal),
                    modalRoot: summarizeLifecycleElement(modalRoot),
                    modalDialog: summarizeLifecycleElement(modalDialog),
                    descendantCount: dxElements.length,
                    loadedDescendantCount: dxElements.length - pendingElements.length,
                    pendingDescendantCount: pendingElements.length,
                    pendingDescendants: pending,
                    popupPortals: portals
                },
                activeElement: summarizeLifecycleElement(document.activeElement),
                recentLifecycle: lifecycleTrace.slice(-24)
            };
        } catch (error) {
            console.error('PublisherStudio lifecycle diagnostics could not capture a snapshot.', error);
            return { reason: String(reason || 'browser-error'), captureError: String(error?.message || error) };
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:captureLifecycleSnapshot.', __javascriptError); throw __javascriptError; }}

    function shouldAttachLifecycleDetails(source, details) { try {
        try {
            const text = `${String(source || '')}\n${String(details?.message || '')}\n${String(details?.stack || '')}`;
            return /DevExpress\.Blazor|dx-blazor|addEventListener|componentContentChanged|initializeComponent/i.test(text)
                && Boolean(document.querySelector?.('.panel-studio-dialog'));
        } catch (error) {
            console.error('PublisherStudio lifecycle diagnostics could not classify an error.', error);
            return false;
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:shouldAttachLifecycleDetails.', __javascriptError); throw __javascriptError; }}

    function attachLifecycleDetails(source, details) { try {
        try {
            if (!shouldAttachLifecycleDetails(source, details)) return details;
            const snapshot = captureLifecycleSnapshot(`error:${String(source || 'browser-runtime')}`);
            console.error('PublisherStudio Panel/DevExpress lifecycle snapshot:', snapshot);
            return {
                message: details.message,
                stack: `${details.stack || ''}\nPublisherStudio Panel/DevExpress lifecycle snapshot: ${JSON.stringify(snapshot)}`
            };
        } catch (error) {
            console.error('PublisherStudio lifecycle diagnostics could not enrich an error.', error);
            return details;
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:attachLifecycleDetails.', __javascriptError); throw __javascriptError; }}

    function startLifecycleObserver() { try {
        try {
            if (lifecycleObserver || typeof MutationObserver !== 'function' || !document.documentElement) return;
            lifecycleObserver = new MutationObserver(records => { try {
                try {
                    for (const record of records) {
                        if (record.type === 'attributes') {
                            recordLifecycle(`attribute:${record.attributeName || ''}`, record.target);
                            continue;
                        }
                        for (const node of record.addedNodes || []) {
                            if (!(node instanceof Element)) continue;
                            recordLifecycle('added', node);
                            for (const child of [...node.querySelectorAll?.('dxbl-modal,dxbl-modal-root,dxbl-modal-dialog,dxbl-popup-portal,.panel-studio-dialog,.panel-studio-canvas') || []].slice(0, 8)) recordLifecycle('added-descendant', child);
                        }
                        for (const node of record.removedNodes || []) {
                            if (!(node instanceof Element)) continue;
                            recordLifecycle('removed', node);
                            for (const child of [...node.querySelectorAll?.('dxbl-modal,dxbl-modal-root,dxbl-modal-dialog,dxbl-popup-portal,.panel-studio-dialog,.panel-studio-canvas') || []].slice(0, 8)) recordLifecycle('removed-descendant', child);
                        }
                    }
                } catch (error) {
                    console.error('PublisherStudio lifecycle mutation diagnostics failed.', error);
                }
             } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:lifecycleObserver.callback.', __javascriptError); throw __javascriptError; }});
            lifecycleObserver.observe(document.documentElement, {
                subtree: true,
                childList: true,
                attributes: true,
                attributeFilter: ['data-qa-dxbl-loaded', 'branch-id', 'x-is-open', 'is-shown']
            });
        } catch (error) {
            console.error('PublisherStudio lifecycle diagnostics could not attach its observer.', error);
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:startLifecycleObserver.', __javascriptError); throw __javascriptError; }}

    function markLifecycle(kind, value, detail = '') { try {
        try {
            recordLifecycle(kind, value, detail);
        } catch (error) {
            console.error('PublisherStudio lifecycle diagnostics could not mark a transition.', error);
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:markLifecycle.', __javascriptError); throw __javascriptError; }}

    function errorDetails(error) {
        try {
            const normalized = error instanceof Error ? error : new Error(String(error ?? "Unknown JavaScript error."));
            return {
                message: normalized.message || String(error ?? "Unknown JavaScript error."),
                stack: normalized.stack || ""
            };
        } catch (detailsError) {
            console.error("PublisherStudio JavaScript diagnostics could not normalize an error.", detailsError);
            return { message: String(error ?? "Unknown JavaScript error."), stack: "" };
        }
    }

    function queueReport(source, details) { try {
        try {
            pendingReports.set(`${source}\n${details.message}`, { source, message: details.message, stack: details.stack });
        } catch (queueError) {
            console.error("PublisherStudio JavaScript diagnostics could not buffer an error before the application logger attached.", queueError);
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:queueReport.', __javascriptError); throw __javascriptError; }}

    function flushPendingReports() { try {
        try {
            if (!dotNetReference || reporting || pendingReports.size === 0) return;
            const first = pendingReports.entries().next();
            if (first.done) return;
            pendingReports.delete(first.value[0]);
            forward(first.value[1].source, first.value[1]);
        } catch (flushError) {
            console.error("PublisherStudio JavaScript diagnostics could not flush a buffered error.", flushError);
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:flushPendingReports.', __javascriptError); throw __javascriptError; }}

    function forward(source, details) { try {
        try {
            if (!dotNetReference || reporting) {
                queueReport(source, details);
                return;
            }

            reporting = true;
            dotNetReference.invokeMethodAsync("ReportJavaScriptErrorAsync", source, details.message, details.stack)
                .catch(reportError => { try {
                    console.error("PublisherStudio JavaScript diagnostics could not forward an error to the application logger.", reportError);
                 } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:forward.catch.', __javascriptError); throw __javascriptError; }})
                .finally(() => { try {
                    reporting = false;
                    flushPendingReports();
                 } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:forward.finally.', __javascriptError); throw __javascriptError; }});
        } catch (forwardError) {
            reporting = false;
            console.error("PublisherStudio JavaScript diagnostics failed while forwarding an error.", forwardError);
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:forward.', __javascriptError); throw __javascriptError; }}

    function report(context, error) { try {
        try {
            const source = String(context || "browser-runtime");
            const details = attachLifecycleDetails(source, errorDetails(error));
            console.error(`PublisherStudio JavaScript error in ${source}: ${details.message}`, error);
            if (!dotNetReference || reporting) {
                queueReport(source, details);
                return;
            }
            forward(source, details);
        } catch (reportError) {
            reporting = false;
            console.error("PublisherStudio JavaScript diagnostics failed while reporting an error.", reportError);
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:report.', __javascriptError); throw __javascriptError; }}

    function guard(context, callback) { try {
        try {
            if (typeof callback !== "function") return callback;
            if (wrappedCallbacks.has(callback)) return wrappedCallbacks.get(callback);

            const guarded = function (...args) { try {
                try {
                    const result = callback.apply(this, args);
                    if (result && typeof result.then === "function") {
                        return result.catch(error => { try {
                            report(context, error);
                            throw error;
                         } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:callback:result.catch@52.', __javascriptError); throw __javascriptError; }});
                    }
                    return result;
                } catch (error) {
                    report(context, error);
                    throw error;
                }
             } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:guarded@48.', __javascriptError); throw __javascriptError; }};
            wrappedCallbacks.set(callback, guarded);
            return guarded;
        } catch (error) {
            report("javascript-diagnostics.guard", error);
            return callback;
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:guard@43.', __javascriptError); throw __javascriptError; }}

    function guardObject(context, value) { try {
        try {
            if (!value || typeof value !== "object") return value;
            for (const key of Reflect.ownKeys(value)) {
                try {
                    const member = value[key];
                    if (typeof member === "function") value[key] = guard(`${context}.${String(key)}`, member);
                    else if (member && typeof member === "object") guardObject(`${context}.${String(key)}`, member);
                } catch (error) {
                    report(`${context}.${String(key)}`, error);
                }
            }
            return value;
        } catch (error) {
            report("javascript-diagnostics.guardObject", error);
            return value;
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:guardObject@71.', __javascriptError); throw __javascriptError; }}

    function guardClass(context, classType) { try {
        try {
            if (typeof classType !== "function") return classType;
            const protect = (target, prefix) => { try {
                try {
                    for (const key of Reflect.ownKeys(target)) {
                        if (key === "constructor" || key === "prototype" || key === "name" || key === "length") continue;
                        const descriptor = Object.getOwnPropertyDescriptor(target, key);
                        if (!descriptor || descriptor.configurable === false) continue;
                        if (typeof descriptor.value === "function") descriptor.value = guard(`${prefix}.${String(key)}`, descriptor.value);
                        if (typeof descriptor.get === "function") descriptor.get = guard(`${prefix}.get:${String(key)}`, descriptor.get);
                        if (typeof descriptor.set === "function") descriptor.set = guard(`${prefix}.set:${String(key)}`, descriptor.set);
                        Object.defineProperty(target, key, descriptor);
                    }
                } catch (error) {
                    report(`${prefix}.prototype`, error);
                }
             } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:protect@93.', __javascriptError); throw __javascriptError; }};
            protect(classType, context);
            if (classType.prototype) protect(classType.prototype, context);
            return classType;
        } catch (error) {
            report("javascript-diagnostics.guardClass", error);
            return classType;
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:guardClass@90.', __javascriptError); throw __javascriptError; }}

    function bindDotNet(reference) { try {
        try {
            dotNetReference = reference || null;
            flushPendingReports();
        } catch (error) {
            report("javascript-diagnostics.bindDotNet", error);
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:bindDotNet@117.', __javascriptError); throw __javascriptError; }}

    function unbindDotNet() { try {
        try {
            dotNetReference = null;
        } catch (error) {
            report("javascript-diagnostics.unbindDotNet", error);
        }
     } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:unbindDotNet@125.', __javascriptError); throw __javascriptError; }}

    window.addEventListener("error", event => {
        try {
            report(event?.filename ? `${event.filename}:${event.lineno || 0}:${event.colno || 0}` : "window.error", event?.error || event?.message);
        } catch (error) {
            console.error("PublisherStudio window error diagnostics failed.", error);
        }
    });

    window.addEventListener("unhandledrejection", event => {
        try {
            report("window.unhandledrejection", event?.reason);
        } catch (error) {
            console.error("PublisherStudio promise-rejection diagnostics failed.", error);
        }
    });

    // Vendor/runtime diagnostics are deliberately observational. Do not monkey-patch EventTarget,
    // timers, animation frames, microtasks, or observer constructors: DevExpress owns those browser
    // lifecycle primitives. Window error/unhandled-rejection capture plus explicit PublisherStudio
    // guards provide diagnostics without changing third-party callback identity or slot timing.
    startLifecycleObserver();

    window.publisherStudioJavaScriptDiagnostics = {
        report,
        guard,
        guardObject,
        guardClass,
        bindDotNet,
        unbindDotNet,
        captureLifecycleSnapshot,
        markLifecycle
    };
 } catch (__javascriptError) { console.error('JavaScript diagnostics runtime error in js/javascript-diagnostics.js:ArrowFunction@1.', __javascriptError); throw __javascriptError; }})();
