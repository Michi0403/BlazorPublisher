// javascript-diagnostics: guarded
var publisherStudioDiagnostics = globalThis.publisherStudioJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`PublisherStudio JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("PublisherStudio fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`PublisherStudio fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
const canvasStates = new WeakMap();
const boundRulers = new WeakSet();
const wordArtPathStates = new WeakMap();
const PX_PER_MM_AT_96_DPI = 96 / 25.4;
let publisherDocumentDirty = false;
let activeVideoExportCancel = null;

window.addEventListener('beforeunload', event => { try {
    if (!publisherDocumentDirty) return;
    event.preventDefault();
    event.returnValue = '';
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:window.addEventListener@10', __javascriptError); throw __javascriptError; }});

function number(value, fallback = 0) { try {
    const parsed = Number.parseFloat(value);
    return Number.isFinite(parsed) ? parsed : fallback;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:number@16', __javascriptError); throw __javascriptError; }}

function clamp(value, min, max) { try {
    return Math.max(min, Math.min(max, value));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:clamp@21', __javascriptError); throw __javascriptError; }}

function elementMm(element, pxPerMm) { try {
    return {
        x: number(element.style.left) / pxPerMm,
        y: number(element.style.top) / pxPerMm,
        width: number(element.style.width) / pxPerMm,
        height: number(element.style.height) / pxPerMm
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:elementMm@25', __javascriptError); throw __javascriptError; }}

function nextAnimationFrame(state) { try {
    if (!state || state.drawPending) return;
    state.drawPending = true;
    requestAnimationFrame(() => { try {
        state.drawPending = false;
        if (!state.stage?.isConnected || !state.scroll?.isConnected || !state.page?.isConnected) return;
        try { drawRulers(state); } catch (error) { console.warn('Publisher ruler redraw failed.', error); }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:requestAnimationFrame@37', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:nextAnimationFrame@34', __javascriptError); throw __javascriptError; }}

function safeDotNet(state, method, ...args) { try {
    if (!state?.dotnet) return Promise.resolve();
    return state.dotnet.invokeMethodAsync(method, ...args).catch(error => { try {
        const message = String(error?.message || error || '');
        if (/disconnected|disposed|circuit/i.test(message)) return;
        console.warn(`Publisher callback ${method} failed.`, error);
        if (method !== 'ReportCanvasInteractionError')
            state.dotnet.invokeMethodAsync('ReportCanvasInteractionError', method, message).catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@51', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:state.dotnet.invokeMethodAsync(\'ReportCanvasInteractionError\', method,@51', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:state.dotnet.invokeMethodAsync(method, ...args).catch@46', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:safeDotNet@44', __javascriptError); throw __javascriptError; }}

function clearObjectAlignmentFeedback(state) { try {
    if (!state?.page) return;
    state.page.querySelectorAll('.alignment-moving-green,.alignment-moving-orange,.alignment-moving-red,.alignment-target-green,.alignment-target-orange,.alignment-target-red')
        .forEach(element => { try { return (element.classList.remove('alignment-moving-green','alignment-moving-orange','alignment-moving-red','alignment-target-green','alignment-target-orange','alignment-target-red')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:state.page.querySelectorAll(\'.alignment-moving-green,.alignment-moving@58', __javascriptError); throw __javascriptError; } });
    state.page.querySelectorAll('.publisher-object-alignment-overlay').forEach(element => { try { return (element.remove()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:state.page.querySelectorAll(\'.publisher-object-alignment-overlay\').for@59', __javascriptError); throw __javascriptError; } });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:clearObjectAlignmentFeedback@55', __javascriptError); throw __javascriptError; }}

function publicationObjectBounds(state, excludedIds) { try {
    const excluded = excludedIds instanceof Set ? excludedIds : new Set([excludedIds].filter(Boolean));
    return [...state.page.querySelectorAll('[data-publication-element][data-element-id]')]
        .filter(element => { try { return (!excluded.has(element.dataset.elementId) && !element.matches('[data-connector-id]') && !element.classList.contains('locked-hidden')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...state.page.querySelectorAll(\'[data-publication-element][data-eleme@65', __javascriptError); throw __javascriptError; } })
        .map(element => { try { return (({
            element,
            id: element.dataset.elementId,
            zIndex: number(element.style.zIndex),
            ...elementMm(element, state.config.pxPerMm)
        })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...state.page.querySelectorAll(\'[data-publication-element][data-eleme@66', __javascriptError); throw __javascriptError; } });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:publicationObjectBounds@62', __javascriptError); throw __javascriptError; }}

function rectanglesOverlap(a, b) { try {
    return Math.min(a.x + a.width, b.x + b.width) - Math.max(a.x, b.x) > .15
        && Math.min(a.y + a.height, b.y + b.height) - Math.max(a.y, b.y) > .15;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:rectanglesOverlap@74', __javascriptError); throw __javascriptError; }}

function overlapArea(a, b) { try {
    const width = Math.max(0, Math.min(a.x + a.width, b.x + b.width) - Math.max(a.x, b.x));
    const height = Math.max(0, Math.min(a.y + a.height, b.y + b.height) - Math.max(a.y, b.y));
    return width * height;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:overlapArea@79', __javascriptError); throw __javascriptError; }}

function rectangleGap(a, b) { try {
    const horizontal = Math.max(b.x - (a.x + a.width), a.x - (b.x + b.width), 0);
    const vertical = Math.max(b.y - (a.y + a.height), a.y - (b.y + b.height), 0);
    return Math.hypot(horizontal, vertical);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:rectangleGap@85', __javascriptError); throw __javascriptError; }}

function internalSnapFractions(size, pxPerMm) { try {
    const pixels = size * pxPerMm;
    const step = pixels >= 520 ? .05 : pixels >= 260 ? .1 : .25;
    const values = new Set([0, .25, .5, .75, 1]);
    for (let value = 0; value <= 1.0001; value += step) values.add(Math.round(value * 1000) / 1000);
    return [...values].sort((a, b) => { try { return (a - b); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...values].sort@96', __javascriptError); throw __javascriptError; } });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:internalSnapFractions@91', __javascriptError); throw __javascriptError; }}

function chooseInternalTarget(targets, moving, nearTolerance) { try {
    const overlapping = targets
        .map(target => { try { return (({ target, area: overlapArea(moving, target) })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:targets .map@101', __javascriptError); throw __javascriptError; } })
        .filter(item => { try { return (item.area > .15); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:targets .map(target => ({ target, area: overlapArea(moving, target) })@102', __javascriptError); throw __javascriptError; } })
        .sort((a, b) => { try { return (b.target.zIndex - a.target.zIndex || b.area - a.area); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:targets .map(target => ({ target, area: overlapArea(moving, target) })@103', __javascriptError); throw __javascriptError; } });
    if (overlapping.length) return overlapping[0].target;

    return targets
        .map(target => { try { return (({ target, distance: rectangleGap(moving, target) })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:targets .map@107', __javascriptError); throw __javascriptError; } })
        .filter(item => { try { return (item.distance <= nearTolerance); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:targets .map(target => ({ target, distance: rectangleGap(moving, targe@108', __javascriptError); throw __javascriptError; } })
        .sort((a, b) => { try { return (a.distance - b.distance || b.target.zIndex - a.target.zIndex); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:targets .map(target => ({ target, distance: rectangleGap(moving, targe@109', __javascriptError); throw __javascriptError; } })[0]?.target || null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:chooseInternalTarget@99', __javascriptError); throw __javascriptError; }}

function sourceAnchors(size, grab, extraOffsets = []) { try {
    const offsets = new Set([0, size / 2, size, ...extraOffsets].map(value => { try { return (Math.round(value * 10000) / 10000); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[0, size / 2, size, ...extraOffsets].map@113', __javascriptError); throw __javascriptError; } }));
    return [...offsets]
        .filter(offset => { try { return (offset >= -.0001 && offset <= size + .0001); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...offsets] .filter@115', __javascriptError); throw __javascriptError; } })
        .map(offset => { try {
            const fraction = size > 0 ? offset / size : .5;
            return { offset, fraction, penalty: Math.abs(grab - fraction) };
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...offsets] .filter(offset => offset >= -.0001 && offset <= size + .0@116', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:sourceAnchors@112', __javascriptError); throw __javascriptError; }}

function movingAnchorOffsets(operation, axis) { try {
    const bounds = operation.movingBounds;
    if (!bounds || !operation.moving?.length) return [];
    const origin = axis === 'x' ? bounds.x : bounds.y;
    return operation.moving.flatMap(item => { try {
        const start = (axis === 'x' ? item.x : item.y) - origin;
        const size = axis === 'x' ? item.width : item.height;
        return [start, start + size / 2, start + size];
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:operation.moving.flatMap@126', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:movingAnchorOffsets@122', __javascriptError); throw __javascriptError; }}

function snapCandidate(mode, axis, target, destination, source, rawStart, tolerance, percent = null) { try {
    const delta = destination - (rawStart + source.offset);
    if (Math.abs(delta) > tolerance) return null;
    return {
        mode,
        axis,
        delta,
        line: destination,
        target,
        percent,
        sourcePercent: source.fraction,
        score: Math.abs(delta) + source.penalty * tolerance * .7,
        key: `${mode}:${axis}:${target.id}:${destination.toFixed(4)}:${source.fraction}`
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:snapCandidate@133', __javascriptError); throw __javascriptError; }}

function pickSnapCandidate(candidates, previous, tolerance, releaseTolerance) { try {
    const valid = candidates.filter(Boolean);
    if (previous?.key) {
        const locked = valid.find(candidate => { try { return (candidate.key === previous.key && Math.abs(candidate.delta) <= releaseTolerance); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:valid.find@152', __javascriptError); throw __javascriptError; } });
        if (locked) return locked;
    }
    return valid.filter(candidate => { try { return (Math.abs(candidate.delta) <= tolerance); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:valid.filter@155', __javascriptError); throw __javascriptError; } }).sort((a, b) => { try { return (a.score - b.score); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:valid.filter(candidate => Math.abs(candidate.delta) <= tolerance).sort@155', __javascriptError); throw __javascriptError; } })[0] || null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pickSnapCandidate@149', __javascriptError); throw __javascriptError; }}

function objectSnapResult(state, operation, x, y, width, height) { try {
    const targets = publicationObjectBounds(state, operation.movingIds || operation.id);
    const tolerance = 7 / state.config.pxPerMm;
    const releaseTolerance = 11 / state.config.pxPerMm;
    const nearTolerance = tolerance * 2.2;
    const moving = { x, y, width, height };
    const xSources = sourceAnchors(width, operation.grabGroupX ?? .5, movingAnchorOffsets(operation, 'x'));
    const ySources = sourceAnchors(height, operation.grabGroupY ?? .5, movingAnchorOffsets(operation, 'y'));
    const xCandidates = [];
    const yCandidates = [];

    if (state.config.snapToObjects) {
        for (const target of targets) {
            for (const source of xSources) {
                for (const destination of [target.x, target.x + target.width / 2, target.x + target.width])
                    xCandidates.push(snapCandidate('object', 'x', target, destination, source, x, releaseTolerance));
            }
            for (const source of ySources) {
                for (const destination of [target.y, target.y + target.height / 2, target.y + target.height])
                    yCandidates.push(snapCandidate('object', 'y', target, destination, source, y, releaseTolerance));
            }
        }
    }

    const internalTarget = state.config.snapInObjects ? chooseInternalTarget(targets, moving, nearTolerance) : null;
    if (internalTarget) {
        const xFractions = internalSnapFractions(internalTarget.width, state.config.pxPerMm);
        const yFractions = internalSnapFractions(internalTarget.height, state.config.pxPerMm);
        for (const source of xSources) {
            for (const fraction of xFractions) {
                const destination = internalTarget.x + internalTarget.width * fraction;
                xCandidates.push(snapCandidate('inside', 'x', internalTarget, destination, source, x, releaseTolerance, fraction));
            }
        }
        for (const source of ySources) {
            for (const fraction of yFractions) {
                const destination = internalTarget.y + internalTarget.height * fraction;
                yCandidates.push(snapCandidate('inside', 'y', internalTarget, destination, source, y, releaseTolerance, fraction));
            }
        }
    }

    const bestX = pickSnapCandidate(xCandidates, operation.snapLockX, tolerance, releaseTolerance);
    const bestY = pickSnapCandidate(yCandidates, operation.snapLockY, tolerance, releaseTolerance);
    operation.snapLockX = bestX;
    operation.snapLockY = bestY;
    if (bestX) x += bestX.delta;
    if (bestY) y += bestY.delta;

    const moved = { x, y, width, height };
    const intentionalTargetIds = new Set([
        internalTarget?.id,
        ...[bestX, bestY]
            .filter(candidate => { try { return (candidate?.mode === 'inside'); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[bestX, bestY] .filter@211', __javascriptError); throw __javascriptError; } })
            .map(candidate => { try { return (candidate.target.id); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[bestX, bestY] .filter(candidate => candidate?.mode === \'inside\') .map@212', __javascriptError); throw __javascriptError; } })
    ].filter(Boolean));
    const collisions = targets.filter(target => { try { return (!intentionalTargetIds.has(target.id) && rectanglesOverlap(moved, target)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:targets.filter@214', __javascriptError); throw __javascriptError; } });
    const alignedTargets = new Set([bestX?.target, bestY?.target].filter(Boolean));
    let nearTarget = null;
    let nearestDistance = Infinity;
    for (const target of targets) {
        const distance = rectangleGap(moved, target);
        if (distance < nearestDistance) { nearestDistance = distance; nearTarget = target; }
    }
    const status = collisions.length ? 'red' : (bestX || bestY) ? 'green' : nearestDistance <= nearTolerance ? 'orange' : null;
    return { x, y, width, height, bestX, bestY, collisions, alignedTargets, nearTarget, internalTarget, status };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:objectSnapResult@158', __javascriptError); throw __javascriptError; }}

function showObjectAlignmentFeedback(state, operation, result) { try {
    clearObjectAlignmentFeedback(state);
    if (!result?.status) return;
    for (const moving of operation.moving || []) moving.element?.classList.add(`alignment-moving-${result.status}`);
    if (!operation.moving?.length) refreshOperationElement(state, operation)?.classList.add(`alignment-moving-${result.status}`);
    const highlighted = result.status === 'red' ? result.collisions : result.status === 'green' ? [...result.alignedTargets] : [result.nearTarget].filter(Boolean);
    highlighted.forEach(target => { try { return (target?.element?.classList.add(`alignment-target-${result.status}`)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:highlighted.forEach@232', __javascriptError); throw __javascriptError; } });
    const overlay = document.createElement('div');
    overlay.className = `publisher-object-alignment-overlay status-${result.status}`;
    overlay.setAttribute('aria-hidden', 'true');
    if (result.bestX) {
        const line = document.createElement('i');
        line.className = `publisher-object-alignment-line vertical ${result.bestX.mode === 'inside' ? 'inside' : ''}`;
        line.style.left = `${result.bestX.line * state.config.pxPerMm}px`;
        overlay.appendChild(line);
    }
    if (result.bestY) {
        const line = document.createElement('i');
        line.className = `publisher-object-alignment-line horizontal ${result.bestY.mode === 'inside' ? 'inside' : ''}`;
        line.style.top = `${result.bestY.line * state.config.pxPerMm}px`;
        overlay.appendChild(line);
    }
    const crosshair = document.createElement('b');
    crosshair.className = 'publisher-object-alignment-crosshair';
    crosshair.style.left = `${(result.x + result.width / 2) * state.config.pxPerMm}px`;
    crosshair.style.top = `${(result.y + result.height / 2) * state.config.pxPerMm}px`;
    overlay.appendChild(crosshair);

    const internal = [result.bestX, result.bestY].filter(candidate => { try { return (candidate?.mode === 'inside'); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[result.bestX, result.bestY].filter@254', __javascriptError); throw __javascriptError; } });
    if (internal.length) {
        const label = document.createElement('span');
        label.className = 'publisher-object-alignment-label';
        label.textContent = internal.map(candidate => { try { return (`${candidate.axis.toUpperCase()} ${Math.round(candidate.percent * 100)}%`); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:internal.map@258', __javascriptError); throw __javascriptError; } }).join(' · ');
        label.style.left = `${(result.x + result.width / 2) * state.config.pxPerMm}px`;
        label.style.top = `${Math.max(0, result.y * state.config.pxPerMm - 26)}px`;
        overlay.appendChild(label);
    }
    state.page.appendChild(overlay);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:showObjectAlignmentFeedback@226', __javascriptError); throw __javascriptError; }}

function syncEditorElementContentFrame(state, element, widthMm, heightMm) { try {
    const content = element?.querySelector?.(':scope > .publication-element-content');
    if (!content) return;
    const zoom = Math.max(.05, number(state?.page?.dataset?.zoom, state?.config?.pxPerMm / PX_PER_MM_AT_96_DPI || 1));
    const basePixelsPerMm = Math.max(.0001, number(state?.config?.pxPerMm, PX_PER_MM_AT_96_DPI) / zoom);
    content.style.width = `${Math.max(0, number(widthMm)) * basePixelsPerMm}px`;
    content.style.height = `${Math.max(0, number(heightMm)) * basePixelsPerMm}px`;
    content.style.setProperty("--publisher-editor-zoom", String(zoom));
    const requestedMode = String(state?.page?.dataset?.zoomMode || state?.config?.zoomMode || "CssLayout").toLowerCase();
    const strategy = String(content.dataset.editorZoomStrategy || "auto").toLowerCase();
    const useCssLayoutZoom = requestedMode === "csslayout"
        && strategy !== "transform"
        && globalThis.CSS?.supports?.("zoom", "1");
    if (useCssLayoutZoom) {
        content.style.zoom = String(zoom);
        content.style.transform = "none";
    } else {
        content.style.zoom = "1";
        content.style.transform = `scale(${zoom})`;
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:syncEditorElementContentFrame@266', __javascriptError); throw __javascriptError; }}

function syncEditorZoomRendering(state) { try {
    if (!state?.page) return;
    for (const element of state.page.querySelectorAll?.('[data-publication-element]:not([data-connector-id])') || []) {
        const bounds = elementMm(element, state.config.pxPerMm);
        syncEditorElementContentFrame(state, element, bounds.width, bounds.height);
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:syncEditorZoomRendering@288', __javascriptError); throw __javascriptError; }}

function resetPointerOperation(state, restoreDom = false) { try {
    clearObjectAlignmentFeedback(state);
    const operation = state?.operation;
    if (!operation) return;
    state.operation = null;
    try { state.stage.releasePointerCapture(operation.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@301', __caughtJavaScriptError);  }

    if (operation.kind === 'connector-control') {
        const controls = operation.originalControls;
        if (operation.connector && controls) {
            operation.connector.dataset.control1X = String(controls.c1.x);
            operation.connector.dataset.control1Y = String(controls.c1.y);
            operation.connector.dataset.control2X = String(controls.c2.x);
            operation.connector.dataset.control2Y = String(controls.c2.y);
            const path = connectorPath(operation.connector.dataset.pathKind || 'Curved', operation.source, operation.target, controls);
            operation.connector.querySelectorAll('.connector-line,.connector-hit').forEach(item => { try { return (item.setAttribute('d', path)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:operation.connector.querySelectorAll(\'.connector-line,.connector-hit\')@311', __javascriptError); throw __javascriptError; } });
            updateConnectorControlAppearance(operation.connector, operation.source, operation.target, controls);
        }
        return;
    }
    if (operation.kind?.startsWith('connector-')) {
        state.operation = operation;
        clearConnectorOperation(state, true);
        return;
    }
    if (operation.kind === 'marquee') {
        operation.overlay?.remove?.();
        if (restoreDom)
            synchronizeSelectionDom(state, operation.initialSelection || new Set(), operation.initialPrimaryId || null);
        return;
    }
    if (!restoreDom || !operation.id) return;
    const moving = operation.moving?.length ? operation.moving : [{ id: operation.id, x: operation.x, y: operation.y, width: operation.width, height: operation.height }];
    for (const item of moving) {
        const element = state.page?.querySelector?.(`[data-element-id="${CSS.escape(item.id)}"]`);
        if (!element) continue;
        element.style.left = `${item.x * state.config.pxPerMm}px`;
        element.style.top = `${item.y * state.config.pxPerMm}px`;
        element.style.width = `${item.width * state.config.pxPerMm}px`;
        element.style.height = `${item.height * state.config.pxPerMm}px`;
        syncEditorElementContentFrame(state, element, item.width, item.height);
        refreshContentFit(element);
        updateAttachedConnectors(state, item.id);
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:resetPointerOperation@296', __javascriptError); throw __javascriptError; }}

function safeMediaDownloadName(value, fallback = 'media') { try {
    const cleaned = String(value || fallback).normalize('NFKC').replace(/[<>:"/\\|?*\u0000-\u001f]+/g, '-').replace(/\s+/g, ' ').replace(/[. ]+$/g, '').trim();
    return cleaned || fallback;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:safeMediaDownloadName', __javascriptError); return fallback; }}

function mediaDownloadDescriptor(media) { try {
    if (!(media instanceof Element)) return null;
    const source = String(media.currentSrc || media.getAttribute('src') || media.querySelector?.('source')?.getAttribute?.('src') || '');
    if (!source) return null;
    const owner = media.closest?.('[data-element-name], [data-publication-element]');
    const elementName = owner?.getAttribute?.('data-element-name') || media.getAttribute('alt') || media.getAttribute('title') || media.tagName.toLowerCase();
    let mime = String(media.getAttribute('type') || media.querySelector?.('source')?.getAttribute?.('type') || '');
    if (!mime && source.startsWith('data:')) mime = source.slice(5, source.indexOf(';') > 5 ? source.indexOf(';') : source.indexOf(','));
    if (!mime) mime = media instanceof HTMLImageElement ? 'image/png' : media instanceof HTMLVideoElement ? 'video/webm' : 'audio/webm';
    const mimeExtensions = { 'image/png':'png','image/jpeg':'jpg','image/webp':'webp','image/avif':'avif','image/gif':'gif','image/svg+xml':'svg','video/webm':'webm','video/mp4':'mp4','video/ogg':'ogv','audio/webm':'webm','audio/mpeg':'mp3','audio/mp4':'m4a','audio/ogg':'ogg','audio/wav':'wav' };
    let extension = mimeExtensions[mime.toLowerCase()] || '';
    if (!extension) {
        try { extension = (new URL(source, location.href).pathname.split('.').pop() || '').toLowerCase().replace(/[^a-z0-9]/g,'').slice(0,6); } catch { extension = ''; }
    }
    extension ||= media instanceof HTMLImageElement ? 'png' : media instanceof HTMLVideoElement ? 'webm' : 'audio';
    const safeName = safeMediaDownloadName(elementName);
    const extensionPattern = new RegExp(`\.${extension.replace(/[^a-z0-9]/gi, '')}$`, 'i');
    const duplicateSuffixPattern = new RegExp(`^(.*)\.${extension.replace(/[^a-z0-9]/gi, '')}(\s+\d+)$`, 'i');
    const duplicateMatch = safeName.match(duplicateSuffixPattern);
    const filename = duplicateMatch
        ? `${duplicateMatch[1]}${duplicateMatch[2]}.${extension}`
        : extensionPattern.test(safeName) ? safeName : `${safeName}.${extension}`;
    let href = source;
    if (!source.startsWith('data:') && !source.startsWith('blob:')) { try { href = new URL(source, location.href).href; } catch { href = source; } }
    return { filename, mime, href };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:mediaDownloadDescriptor', __javascriptError); return null; }}

function configureStudioDragTransfer(transfer, payload = {}) { try {
    if (!transfer) return false;
    const effectAllowed = String(payload.effectAllowed || 'copy');
    transfer.effectAllowed = effectAllowed;
    const internalKind = String(payload.internalKind || '').trim().toLowerCase();
    if (internalKind) {
        const internal = JSON.stringify({ kind: internalKind, id: String(payload.id || ''), name: String(payload.name || '') });
        transfer.setData('application/x-publisher-studio-drag', internal);
        transfer.setData(`application/x-publisher-${internalKind}`, String(payload.id || ''));
    }
    const href = String(payload.href || '');
    const mime = String(payload.mime || 'application/octet-stream');
    const filename = safeMediaDownloadName(payload.filename || payload.name || 'media');
    if (href) {
        const mediaPayload = JSON.stringify({ href, mime, filename });
        transfer.setData('application/x-publisher-media', mediaPayload);
        transfer.setData('DownloadURL', `${mime}:${filename}:${href}`);
        transfer.setData('text/uri-list', href);
        transfer.setData('text/plain', filename);
    } else if (payload.name) {
        transfer.setData('text/plain', String(payload.name));
    }
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:configureStudioDragTransfer', __javascriptError); return false; }}

function readStudioDragTransfer(transfer, expectedKind = '') { try {
    if (!transfer) return null;
    const expected = String(expectedKind || '').trim().toLowerCase();
    if (expected) {
        const direct = transfer.getData(`application/x-publisher-${expected}`);
        if (direct) return { kind: expected, id: direct };
    }
    const raw = transfer.getData('application/x-publisher-studio-drag');
    if (!raw) return null;
    const parsed = JSON.parse(raw);
    if (!parsed || (expected && String(parsed.kind || '').toLowerCase() !== expected)) return null;
    return parsed;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:readStudioDragTransfer', __javascriptError); return null; }}

function studioMediaDescriptorFromTransfer(transfer) { try {
    if (!transfer) return null;
    const raw = transfer.getData('application/x-publisher-media');
    if (raw) {
        try {
            const parsed = JSON.parse(raw);
            if (parsed?.href) return { href: String(parsed.href), mime: String(parsed.mime || ''), filename: safeMediaDownloadName(parsed.filename || 'media') };
        } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:studioMediaDescriptorFromTransfer:json', __caughtJavaScriptError); }
    }
    const href = String(transfer.getData('text/uri-list') || '').split(/\r?\n/).find(line => line && !line.startsWith('#')) || '';
    if (!href) return null;
    return { href, mime: '', filename: safeMediaDownloadName(transfer.getData('text/plain') || 'media') };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:studioMediaDescriptorFromTransfer', __javascriptError); return null; }}

async function fileFromStudioDragTransfer(transfer) { try {
    const direct = transfer?.files?.[0]
        || [...(transfer?.items || [])].find(candidate => candidate.kind === 'file')?.getAsFile?.();
    if (direct) return direct;
    const descriptor = studioMediaDescriptorFromTransfer(transfer);
    if (!descriptor?.href) return null;
    const source = String(descriptor.href);
    if (!/^(?:data:|blob:)/i.test(source)) {
        try {
            const resolved = new URL(source, location.href);
            if (resolved.origin !== location.origin) return null;
        } catch { return null; }
    }
    const response = await fetch(source);
    if (!response.ok) throw new Error(`Dragged media could not be read (${response.status}).`);
    const blob = await response.blob();
    const mime = String(descriptor.mime || blob.type || 'application/octet-stream');
    let filename = safeMediaDownloadName(descriptor.filename || 'media');
    if (!/\.[a-z0-9]{2,6}$/i.test(filename)) {
        const extensions = { 'image/png':'png','image/jpeg':'jpg','image/webp':'webp','image/gif':'gif','image/svg+xml':'svg','video/webm':'webm','video/mp4':'mp4','audio/webm':'webm','audio/mpeg':'mp3','audio/ogg':'ogg','audio/wav':'wav' };
        const extension = extensions[mime.toLowerCase()] || 'bin';
        filename += `.${extension}`;
    }
    return new File([blob], filename, { type: mime, lastModified: Date.now() });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:fileFromStudioDragTransfer', __javascriptError); throw __javascriptError; }}

function namedMediaDragStart(event) { try {
    const media = event?.target?.closest?.('img,video,audio');
    const transfer = event?.dataTransfer;
    if (!media || !transfer) return false;
    const descriptor = mediaDownloadDescriptor(media);
    if (!descriptor) return false;
    return configureStudioDragTransfer(transfer, {
        effectAllowed: 'copy',
        internalKind: 'media',
        id: media.closest?.('[data-element-id]')?.getAttribute?.('data-element-id') || '',
        name: descriptor.filename,
        filename: descriptor.filename,
        mime: descriptor.mime,
        href: descriptor.href
    });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:namedMediaDragStart', __javascriptError); return false; }}

function namedMediaDragRuntime(root = document) { try {
    root.querySelectorAll?.('img,video,audio').forEach(media => { try {
        if (media.dataset.publisherNamedDragBound === 'true') return;
        media.dataset.publisherNamedDragBound = 'true';
        media.draggable = true;
        media.addEventListener('dragstart', namedMediaDragStart);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:namedMediaDragRuntime:media', __javascriptError); }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:namedMediaDragRuntime', __javascriptError); }}

function insertionKindFromEvent(state, event) { try {
    return event.dataTransfer?.getData('application/x-publisher-insert')
        || event.dataTransfer?.getData('text/x-publisher-insert')
        || String(state?.insertDragSource?.dataset?.publisherInsert || '');
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:insertionKindFromEvent@342', __javascriptError); throw __javascriptError; }}

function insertionDragStart(state, event) { try {
    const source = event.target?.closest?.('[data-publisher-insert]');
    if (!source || !event.dataTransfer) return;
    const kind = String(source.dataset.publisherInsert || '').trim().toLowerCase();
    if (!kind) return;
    event.dataTransfer.effectAllowed = 'copy';
    event.dataTransfer.setData('application/x-publisher-insert', kind);
    event.dataTransfer.setData('text/x-publisher-insert', kind);
    source.classList.add('dragging');
    state.insertDragSource = source;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:insertionDragStart@348', __javascriptError); throw __javascriptError; }}

function createInsertionDropPreview(state, kind) { try {
    state.insertDropPreview?.remove?.();
    const ghost = document.createElement('div');
    ghost.className = `publisher-insert-drag-ghost kind-${kind}`;
    ghost.setAttribute('aria-hidden', 'true');
    if (kind === 'video') {
        ghost.innerHTML = '<span class="publisher-insert-video-play">▶</span><small>Video</small>';
    } else if (kind === 'picture') {
        ghost.innerHTML = '<span class="publisher-insert-picture-mark">▧</span><small>Picture</small>';
    } else {
        ghost.innerHTML = '<strong>New text box</strong><small>Text</small>';
    }
    state.page?.appendChild(ghost);
    state.insertDropPreview = ghost;
    return ghost;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:createInsertionDropPreview@360', __javascriptError); throw __javascriptError; }}

function positionInsertionDropPreview(state, event, kind) { try {
    const rect = state.page.getBoundingClientRect();
    const xPx = clamp(event.clientX - rect.left, 0, rect.width);
    const yPx = clamp(event.clientY - rect.top, 0, rect.height);
    const ghost = state.insertDropPreview?.classList?.contains(`kind-${kind}`)
        ? state.insertDropPreview
        : createInsertionDropPreview(state, kind);
    ghost.style.left = `${xPx}px`;
    ghost.style.top = `${yPx}px`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:positionInsertionDropPreview@377', __javascriptError); throw __javascriptError; }}

function clearInsertionDrag(state) { try {
    state?.insertDragSource?.classList?.remove('dragging');
    state.insertDragSource = null;
    state?.insertDropPreview?.remove?.();
    state.insertDropPreview = null;
    state?.page?.classList?.remove('insert-drop-target');
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:clearInsertionDrag@388', __javascriptError); throw __javascriptError; }}

function insertionDragEnd(state) { try {
    state.suppressInsertClickUntil = performance.now() + 350;
    clearInsertionDrag(state);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:insertionDragEnd@396', __javascriptError); throw __javascriptError; }}

function suppressInsertionClick(state, event) { try {
    if (performance.now() > number(state?.suppressInsertClickUntil)) return;
    if (!event.target?.closest?.('[data-publisher-insert]')) return;
    event.preventDefault();
    event.stopImmediatePropagation();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressInsertionClick@401', __javascriptError); throw __javascriptError; }}

function insertionDragOver(state, event) { try {
    if (externalFileDragOver(state, event)) return;
    const kind = insertionKindFromEvent(state, event);
    if (!kind || !state.page?.isConnected) return;
    const rect = state.page.getBoundingClientRect();
    if (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom) {
        state.page.classList.remove('insert-drop-target');
        return;
    }
    event.preventDefault();
    event.dataTransfer.dropEffect = 'copy';
    state.page.classList.add('insert-drop-target');
    positionInsertionDropPreview(state, event, kind);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:insertionDragOver@408', __javascriptError); throw __javascriptError; }}

async function insertionDrop(state, event) { try {
    if (await externalFileDrop(state, event)) return;
    const kind = insertionKindFromEvent(state, event);
    if (!kind || !state.page?.isConnected) return;
    const rect = state.page.getBoundingClientRect();
    if (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom) return;
    event.preventDefault();
    const x = clamp((event.clientX - rect.left) / state.config.pxPerMm, 0, number(state.page.dataset.pageWidthMm));
    const y = clamp((event.clientY - rect.top) / state.config.pxPerMm, 0, number(state.page.dataset.pageHeightMm));
    state.suppressInsertClickUntil = performance.now() + 350;
    clearInsertionDrag(state);
    if (kind === 'picture') {
        const input = document.getElementById('picture-file-input');
        if (input instanceof HTMLInputElement && input.type === 'file') {
            input.value = '';
            input.dataset.publisherDropX = String(x);
            input.dataset.publisherDropY = String(y);
            input.click();
            return;
        }
    }
    safeDotNet(state, 'DropInsert', kind, x, y);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:insertionDrop@423', __javascriptError); throw __javascriptError; }}


function externalDraggedFile(event) { try {
    const transfer = event?.dataTransfer;
    if (!transfer) return null;
    if (transfer.files?.length) return transfer.files[0];
    const item = [...(transfer.items || [])].find(candidate => { try { return (candidate.kind === 'file'); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...(transfer.items || [])].find@452', __javascriptError); throw __javascriptError; } });
    return item?.getAsFile?.() || null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:externalDraggedFile@448', __javascriptError); throw __javascriptError; }}

function externalDraggedDescriptor(event) { try {
    const file = externalDraggedFile(event);
    if (file) return { file, name: file.name || '', type: file.type || '', size: file.size || 0, lastModified: file.lastModified || 0 };
    const item = [...(event?.dataTransfer?.items || [])].find(candidate => { try { return (candidate.kind === 'file'); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...(event?.dataTransfer?.items || [])].find@459', __javascriptError); throw __javascriptError; } });
    if (!item) return null;
    return { file: null, name: '', type: item.type || '', size: 0, lastModified: 0 };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:externalDraggedDescriptor@456', __javascriptError); throw __javascriptError; }}

function externalDropKind(file) { try {
    const name = String(file?.name || '').toLowerCase();
    const mime = String(file?.type || '').toLowerCase();
    if (mime.startsWith('image/') || /\.(png|jpe?g|gif|webp|svg)$/.test(name)) return 'picture';
    if (mime.startsWith('video/') || /\.(mp4|m4v|webm|ogv|mov)$/.test(name)) return 'video';
    if (mime.startsWith('audio/') || /\.(mp3|wav|oga|ogg|m4a|aac|flac)$/.test(name)) return 'audio';
    if (mime === 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' ||
        mime === 'application/vnd.ms-excel' || /\.(xlsx|xlsm|xls|csv|tsv)$/.test(name)) return 'spreadsheet';
    if (mime === 'text/markdown' || /\.(md|markdown)$/.test(name)) return 'markdown';
    if (mime.startsWith('text/') || /\.(txt|text|log|csv|tsv)$/.test(name)) return 'text';
    if (mime === 'application/vnd.openxmlformats-officedocument.wordprocessingml.document' || /\.docx$/.test(name)) return 'docx';
    return '';
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:externalDropKind@464', __javascriptError); throw __javascriptError; }}

function clearExternalDropPreview(state) { try {
    const preview = state?.externalDropPreview;
    if (!preview) return;
    if (preview.url) URL.revokeObjectURL(preview.url);
    preview.target?.element?.classList?.remove('external-file-component-drop-target');
    preview.ghost?.remove?.();
    preview.overlay?.remove?.();
    state.page?.classList?.remove('external-file-drop-target');
    state.externalDropPreview = null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:clearExternalDropPreview@478', __javascriptError); throw __javascriptError; }}

function externalDropTargetMessage(kind, target) { try {
    if (target?.kind === 'image' && kind === 'picture') return 'Add picture as a new Picture Studio layer';
    if (target?.kind === 'video' && kind === 'video') return 'Add video as a new Video Studio sequence segment';
    if (target?.kind === 'audio' && kind === 'audio') return 'Add audio as a new Audio Studio sequence segment';
    return kind === 'picture' ? 'Drop picture at this position'
        : kind === 'video' ? 'Drop video at this position'
        : kind === 'audio' ? 'Drop audio at this position'
        : kind === 'spreadsheet' ? 'Drop workbook as an editable spreadsheet frame'
        : kind === 'markdown' ? 'Drop Markdown as a text frame'
        : kind === 'text' ? 'Drop text as a text frame'
        : kind === 'docx' ? 'Drop Word document as an editable text frame'
        : 'This file type is not supported yet';
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:externalDropTargetMessage@489', __javascriptError); throw __javascriptError; }}

function compatibleExternalDropTarget(kind, targetKind) { try {
    return (kind === 'picture' && targetKind === 'image')
        || (kind === 'video' && targetKind === 'video')
        || (kind === 'audio' && targetKind === 'audio');
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:compatibleExternalDropTarget@503', __javascriptError); throw __javascriptError; }}

function externalDropTargetAt(state, event, kind, placement) { try {
    const element = event?.target?.closest?.('[data-publication-element]');
    if (!element || !state.page?.contains(element) || element.classList.contains('locked')) return null;
    const targetKind = String(element.dataset.elementKind || '').toLowerCase();
    const targetId = String(element.dataset.elementId || '');
    if (!targetId || !compatibleExternalDropTarget(kind, targetKind)) return null;

    const x = number(element.dataset.elementX);
    const y = number(element.dataset.elementY);
    const width = Math.max(.001, number(element.dataset.elementWidth, 1));
    const height = Math.max(.001, number(element.dataset.elementHeight, 1));
    const radians = number(element.dataset.elementRotation) * Math.PI / 180;
    const dx = number(placement?.x) - (x + width / 2);
    const dy = number(placement?.y) - (y + height / 2);
    const cos = Math.cos(radians);
    const sin = Math.sin(radians);
    const localX = cos * dx + sin * dy + width / 2;
    const localY = -sin * dx + cos * dy + height / 2;
    return {
        element,
        id: targetId,
        kind: targetKind,
        x: clamp(localX / width, 0, 1),
        y: clamp(localY / height, 0, 1)
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:externalDropTargetAt@509', __javascriptError); throw __javascriptError; }}

function setExternalDropTarget(preview, target) { try {
    if (!preview) return;
    if (preview.target?.element !== target?.element)
        preview.target?.element?.classList?.remove('external-file-component-drop-target');
    preview.target = target || null;
    preview.target?.element?.classList?.add('external-file-component-drop-target');
    const message = preview.overlay?.querySelector?.('.publisher-external-drop-message');
    if (message) message.textContent = externalDropTargetMessage(preview.kind, preview.target);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:setExternalDropTarget@536', __javascriptError); throw __javascriptError; }}

function createExternalDropPreview(state, file, kind) { try {
    clearExternalDropPreview(state);
    const overlay = document.createElement('div');
    overlay.className = 'publisher-external-drop-overlay';
    const message = document.createElement('span');
    message.className = 'publisher-external-drop-message';
    message.textContent = externalDropTargetMessage(kind, null);
    overlay.appendChild(message);
    state.page.appendChild(overlay);

    const ghost = document.createElement('div');
    ghost.className = `publisher-external-drop-ghost kind-${kind || 'unsupported'}`;
    ghost.setAttribute('aria-hidden', 'true');
    let url = '';
    const fileKey = `${file?.name || ''}|${file?.size || 0}|${file?.lastModified || 0}|${file?.type || ''}`;
    const preview = { file, fileKey, kind, overlay, ghost, url, target: null, widthPx: 190, heightPx: kind === 'video' ? 108 : 120, pixelWidth: 0, pixelHeight: 0, durationSeconds: 0 };

    if (kind === 'picture' && file instanceof Blob) {
        url = URL.createObjectURL(file);
        preview.url = url;
        const image = document.createElement('img');
        image.src = url;
        image.alt = '';
        image.addEventListener('load', () => { try {
            preview.pixelWidth = image.naturalWidth || 0;
            preview.pixelHeight = image.naturalHeight || 0;
            if (preview.pixelWidth > 0 && preview.pixelHeight > 0) {
                preview.heightPx = clamp(preview.widthPx * preview.pixelHeight / preview.pixelWidth, 54, 220);
                ghost.style.height = `${preview.heightPx}px`;
            }
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:image.addEventListener@569', __javascriptError); throw __javascriptError; }}, { once: true });
        ghost.appendChild(image);
    } else if ((kind === 'video' || kind === 'audio') && file instanceof Blob) {
        url = URL.createObjectURL(file);
        preview.url = url;
        const media = document.createElement(kind === 'video' ? 'video' : 'audio');
        media.src = url;
        media.muted = true;
        media.preload = 'metadata';
        if (kind === 'video') {
            media.playsInline = true;
            media.autoplay = true;
            media.loop = true;
            media.play().catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@589', __promiseError);   } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:media.play().catch@589', __javascriptError); throw __javascriptError; }});
        }
        media.addEventListener('loadedmetadata', () => { try {
            preview.durationSeconds = Number.isFinite(media.duration) ? Math.max(0, media.duration) : 0;
            if (kind === 'video') {
                preview.pixelWidth = media.videoWidth || 0;
                preview.pixelHeight = media.videoHeight || 0;
            }
            if (preview.pixelWidth > 0 && preview.pixelHeight > 0) {
                preview.heightPx = clamp(preview.widthPx * preview.pixelHeight / preview.pixelWidth, 54, 220);
                ghost.style.height = `${preview.heightPx}px`;
            }
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:media.addEventListener@591', __javascriptError); throw __javascriptError; }}, { once: true });
        if (kind === 'video') ghost.appendChild(media);
        else {
            const icon = document.createElement('b');
            icon.textContent = 'AUDIO';
            const label = document.createElement('small');
            label.textContent = file?.name || 'Dropped audio';
            ghost.append(icon, label, media);
        }
    } else {
        const icon = document.createElement('b');
        icon.textContent = kind === 'spreadsheet' ? 'XLSX' : kind === 'markdown' ? 'MD' : kind === 'text' ? 'TXT' : kind === 'docx' ? 'DOCX' : '?';
        const label = document.createElement('small');
        label.textContent = file?.name || (kind ? `Dropped ${kind}` : 'Dropped file');
        ghost.append(icon, label);
        if ((kind === 'text' || kind === 'markdown') && file instanceof Blob) {
            file.slice(0, 4096).text().then(text => { try {
                if (state.externalDropPreview !== preview) return;
                const excerpt = document.createElement('p');
                excerpt.textContent = text.replace(/\s+/g, ' ').trim().slice(0, 180) || '(empty text file)';
                ghost.appendChild(excerpt);
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:file.slice(0, 4096).text().then@617', __javascriptError); throw __javascriptError; }}).catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@617', __promiseError);   } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:file.slice(0, 4096).text().then(text => { if (state.externalDropPrevie@622', __javascriptError); throw __javascriptError; }});
        }
    }
    ghost.style.width = `${preview.widthPx}px`;
    ghost.style.height = `${preview.heightPx}px`;
    state.page.appendChild(ghost);
    state.page.classList.add('external-file-drop-target');
    state.externalDropPreview = preview;
    return preview;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:createExternalDropPreview@546', __javascriptError); throw __javascriptError; }}

function positionExternalDropPreviewAt(state, placement) { try {
    const preview = state.externalDropPreview;
    if (!preview) return null;
    const pageWidth = number(state.page.dataset.pageWidthMm);
    const pageHeight = number(state.page.dataset.pageHeightMm);
    const x = clamp(number(placement?.x, pageWidth / 2), 0, pageWidth);
    const y = clamp(number(placement?.y, pageHeight / 2), 0, pageHeight);
    preview.ghost.style.left = `${x * state.config.pxPerMm}px`;
    preview.ghost.style.top = `${y * state.config.pxPerMm}px`;
    state.lastInsertionPoint = { x, y };
    return { x, y };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:positionExternalDropPreviewAt@633', __javascriptError); throw __javascriptError; }}

function positionExternalDropPreview(state, event) { try {
    const rect = state.page.getBoundingClientRect();
    return positionExternalDropPreviewAt(state, {
        x: (event.clientX - rect.left) / state.config.pxPerMm,
        y: (event.clientY - rect.top) / state.config.pxPerMm
    });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:positionExternalDropPreview@646', __javascriptError); throw __javascriptError; }}

function externalFileDragOver(state, event) { try {
    const descriptor = externalDraggedDescriptor(event);
    if (!descriptor) return false;
    event.preventDefault();
    event.dataTransfer.dropEffect = 'copy';
    const kind = externalDropKind(descriptor);
    const current = state.externalDropPreview;
    const fileKey = `${descriptor.name}|${descriptor.size}|${descriptor.lastModified}|${descriptor.type}`;
    if (!current || current.fileKey !== fileKey || current.kind !== kind)
        createExternalDropPreview(state, descriptor.file || descriptor, kind);
    const placement = positionExternalDropPreview(state, event);
    setExternalDropTarget(state.externalDropPreview, externalDropTargetAt(state, event, kind, placement));
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:externalFileDragOver@654', __javascriptError); throw __javascriptError; }}

async function importExternalFileAt(state, file, placement, existingPreview = null, target = null) { try {
    const kind = externalDropKind(file);
    const preview = existingPreview || createExternalDropPreview(state, file, kind);
    positionExternalDropPreviewAt(state, placement);
    setExternalDropTarget(preview, target);
    if (!kind) {
        clearExternalDropPreview(state);
        await safeDotNet(state, 'ExternalFileDropFailed',
            `The file '${file?.name || 'file'}' is not a supported picture, spreadsheet, video, audio, DOCX, text, or Markdown file.`);
        return false;
    }

    const assetId = crypto.randomUUID();
    try {
        preview?.overlay?.classList?.add('uploading');
        const message = preview?.overlay?.querySelector?.('.publisher-external-drop-message');
        if (message) message.textContent = `Importing ${file?.name || kind}…`;
        const response = await fetch(`/api/assets/drop/${encodeURIComponent(assetId)}`, {
            method: 'POST',
            headers: {
                'Content-Type': file?.type || 'application/octet-stream',
                'X-Publisher-File-Name': encodeURIComponent(file?.name || '')
            },
            body: file
        });
        if (!response.ok) throw new Error((await response.text()) || `Upload failed with status ${response.status}.`);
        await safeDotNet(state, 'CompleteExternalFileDrop', assetId, kind, file?.name || kind,
            file?.type || 'application/octet-stream', file?.size || 0,
            preview?.durationSeconds || 0, preview?.pixelWidth || 0, preview?.pixelHeight || 0,
            placement.x, placement.y,
            target?.id || '', target?.kind || '', target?.x ?? .5, target?.y ?? .5);
        return true;
    } catch (error) {
        await safeDotNet(state, 'ExternalFileDropFailed', error?.message || String(error));
        return false;
    } finally {
        clearExternalDropPreview(state);
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:importExternalFileAt@669', __javascriptError); throw __javascriptError; }}

async function externalFileDrop(state, event) { try {
    const file = externalDraggedFile(event);
    if (!file) return false;
    event.preventDefault();
    event.stopPropagation();
    const preview = state.externalDropPreview || createExternalDropPreview(state, file, externalDropKind(file));
    const placement = positionExternalDropPreview(state, event) || { x: 0, y: 0 };
    const target = externalDropTargetAt(state, event, preview.kind, placement) || preview.target || null;
    await importExternalFileAt(state, file, placement, preview, target);
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:externalFileDrop@709', __javascriptError); throw __javascriptError; }}

function canvasClipboardFiles(event) { try {
    const transfer = event?.clipboardData;
    if (!transfer) return [];
    const files = [...(transfer.files || [])].filter(file => { try { return (file instanceof Blob); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...(transfer.files || [])].filter@724', __javascriptError); throw __javascriptError; } });
    for (const item of [...(transfer.items || [])]) {
        if (item.kind !== 'file') continue;
        const file = item.getAsFile?.();
        if (file && !files.includes(file)) files.push(file);
    }
    return files;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:canvasClipboardFiles@721', __javascriptError); throw __javascriptError; }}

function canvasClipboardPlacement(state, index = 0) { try {
    const pageWidth = number(state.page?.dataset?.pageWidthMm);
    const pageHeight = number(state.page?.dataset?.pageHeightMm);
    const base = state.lastInsertionPoint || { x: pageWidth / 2, y: pageHeight / 2 };
    const offset = index * 4;
    return {
        x: clamp(number(base.x, pageWidth / 2) + offset, 0, pageWidth),
        y: clamp(number(base.y, pageHeight / 2) + offset, 0, pageHeight)
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:canvasClipboardPlacement@733', __javascriptError); throw __javascriptError; }}

function namedClipboardFile(file, index) { try {
    if (file?.name) return file;
    const mime = String(file?.type || '').toLowerCase();
    const extension = mime.startsWith('image/') ? (mime.split('/')[1] || 'png').replace('jpeg', 'jpg')
        : mime.startsWith('video/') ? (mime.split('/')[1] || 'webm').replace('quicktime', 'mov')
        : mime === 'text/markdown' ? 'md'
        : 'txt';
    return new File([file], `Pasted ${mime.startsWith('image/') ? 'image' : mime.startsWith('video/') ? 'video' : 'file'} ${index + 1}.${extension}`,
        { type: file?.type || 'application/octet-stream', lastModified: Date.now() });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:namedClipboardFile@744', __javascriptError); throw __javascriptError; }}

async function canvasDocumentPaste(state, event) { try {
    if (!state?.keyboardActive || !state.stage?.isConnected || event.defaultPrevented || isPublisherEditableTarget(event.target)) return;
    if (state.pendingPasteTimer) clearTimeout(state.pendingPasteTimer);
    state.pendingPasteTimer = 0;

    const files = canvasClipboardFiles(event);
    const types = [...(event.clipboardData?.types || [])].map(value => { try { return (String(value).toLowerCase()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...(event.clipboardData?.types || [])].map@761', __javascriptError); throw __javascriptError; } });
    const plainText = String(event.clipboardData?.getData?.('text/plain') || '');
    const markdownText = String(event.clipboardData?.getData?.('text/markdown') || '');
    const externalText = markdownText || plainText;
    const preferInternal = state.internalClipboardArmed && !state.externalClipboardLikely;
    const useExternalText = Boolean(externalText.trim()) && !preferInternal;

    if (preferInternal || (!files.length && !useExternalText)) {
        event.preventDefault();
        event.stopPropagation();
        resetCanvasTransientState(state, true);
        safeDotNet(state, 'KeyboardPaste');
        state.externalClipboardLikely = false;
        return;
    }

    event.preventDefault();
    event.stopPropagation();
    resetCanvasTransientState(state, true);
    state.internalClipboardArmed = false;
    state.externalClipboardLikely = true;

    if (files.length) {
        for (let index = 0; index < files.length; index++) {
            const file = namedClipboardFile(files[index], index);
            await importExternalFileAt(state, file, canvasClipboardPlacement(state, index));
        }
        return;
    }

    const markdown = Boolean(markdownText) || types.includes('text/markdown');
    const file = new File([externalText], markdown ? 'Pasted Markdown.md' : 'Pasted Text.txt', {
        type: markdown ? 'text/markdown' : 'text/plain',
        lastModified: Date.now()
    });
    await importExternalFileAt(state, file, canvasClipboardPlacement(state, 0));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:canvasDocumentPaste@755', __javascriptError); throw __javascriptError; }}


function measureNaturalContent(source,kind){ try {const ot=source.style.transform,ow=source.style.width,oh=source.style.height;source.style.transform='none';source.style.width=kind==='spreadsheet'?'max-content':'100%';source.style.height='auto';const v={width:Math.max(1,source.scrollWidth,source.getBoundingClientRect().width),height:Math.max(1,source.scrollHeight,source.getBoundingClientRect().height)};source.style.transform=ot;source.style.width=ow;source.style.height=oh;return v; } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:measureNaturalContent@800', __javascriptError); throw __javascriptError; }}
function applyContentViewport(frame, source, fitScaleX = number(frame.dataset.contentFitScaleX, 1), fitScaleY = number(frame.dataset.contentFitScaleY, 1)) { try {
    const offsetX = number(frame.dataset.contentOffsetX, 0);
    const offsetY = number(frame.dataset.contentOffsetY, 0);
    const scale = clamp(number(frame.dataset.contentScale, 1), .1, 12);
    const translateX = offsetX * Math.max(1, frame.clientWidth) / 100;
    const translateY = offsetY * Math.max(1, frame.clientHeight) / 100;
    source.style.transformOrigin = '0 0';
    source.style.transform = `translate(${translateX}px, ${translateY}px) scale(${Math.max(.0001, fitScaleX * scale)}, ${Math.max(.0001, fitScaleY * scale)})`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:applyContentViewport@801', __javascriptError); throw __javascriptError; }}

export function refreshContentFit(root = document) { try {
    const frames = root?.matches?.('[data-content-fit]') ? [root] : [...(root?.querySelectorAll?.('[data-content-fit]') || [])];
    for (const frame of frames) {
        const source = frame.querySelector(':scope > [data-content-fit-source]');
        if (!source) continue;
        const mode = String(frame.dataset.contentFit || 'clip').toLowerCase();
        const kind = String(frame.dataset.contentKind || 'text').toLowerCase();
        source.style.transform = 'none';
        source.style.transformOrigin = '0 0';
        source.style.width = kind === 'spreadsheet' ? 'max-content' : '100%';
        source.style.height = kind === 'component' ? '100%' : 'auto';
        let sx = 1, sy = 1;
        if (mode !== 'clip') {
            const natural = measureNaturalContent(source, kind);
            sx = Math.max(1, frame.clientWidth) / natural.width;
            sy = Math.max(1, frame.clientHeight) / natural.height;
            if (mode === 'fit') sx = sy = Math.min(sx, sy);
            else if (mode === 'fill') sx = sy = Math.max(sx, sy);
        }
        frame.dataset.contentFitScaleX = String(sx);
        frame.dataset.contentFitScaleY = String(sy);
        applyContentViewport(frame, source, sx, sy);
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:refreshContentFit@811', __javascriptError); throw __javascriptError; }}
function resizeCursorFor(handle, rotation) { try {
    // CSS screen coordinates increase downward. 45 degrees therefore follows the
    // NW-SE axis and 135 degrees follows NE-SW; treating them as mathematical
    // Y-up angles swaps the diagonal cursor after an object is rotated.
    const base = { e: 0, w: 0, n: 90, s: 90, nw: 45, se: 45, ne: 135, sw: 135 }[handle] ?? 0;
    const sector = Math.round(((((base + rotation) % 180) + 180) % 180) / 45) % 4;
    return ['ew-resize', 'nwse-resize', 'ns-resize', 'nesw-resize'][sector];
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:resizeCursorFor@835', __javascriptError); throw __javascriptError; }}
function updateResizeHandleCursors(root=document){ try {for(const element of root.querySelectorAll?.('.pub-element')||[]){const rotation=parseRotation(element);for(const handle of element.querySelectorAll('[data-resize-handle]'))handle.style.cursor=resizeCursorFor(handle.dataset.resizeHandle,rotation);} } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:updateResizeHandleCursors@843', __javascriptError); throw __javascriptError; }}


export function elementRelativePoint(pageId, elementId, clientX, clientY) { try {
    const page = typeof pageId === 'string' ? document.getElementById(pageId) : pageId;
    const element = page?.querySelector?.(`[data-element-id="${CSS.escape(String(elementId || ''))}"]`);
    if (!element) return [.5, .5];
    const stage = page.closest?.('.publication-stage') || document.getElementById('publisher-stage');
    const state = stage ? canvasStates.get(stage) : null;
    if (state) {
        const point = relativePointForElement(state, element, { clientX: number(clientX), clientY: number(clientY) });
        return [point.x, point.y];
    }
    const rect = element.getBoundingClientRect();
    return [
        rect.width > 0 ? clamp((number(clientX) - rect.left) / rect.width, 0, 1) : .5,
        rect.height > 0 ? clamp((number(clientY) - rect.top) / rect.height, 0, 1) : .5
    ];
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:elementRelativePoint@846', __javascriptError); throw __javascriptError; }}

export function initializeSignalConnectors(rootId, options = {}) { try {
    const root = typeof rootId === 'string' ? document.getElementById(rootId) : rootId;
    if (!root) return false;
    root.__publisherSignalRuntime?.dispose?.();
    root.__publisherSignalRuntime = signalConnectorRuntime(root, options);
    return Boolean(root.__publisherSignalRuntime);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:initializeSignalConnectors@863', __javascriptError); throw __javascriptError; }}

export function initializeCanvas(stageId, scrollId, pageId, horizontalRulerId, verticalRulerId, dotnet, config) { try {
    const stage = document.getElementById(stageId);
    const scroll = document.getElementById(scrollId);
    const page = document.getElementById(pageId);
    if (!stage || !scroll || !page || !stage.isConnected || !scroll.isConnected || !page.isConnected) return false;
    const normalizedConfig = {
        pxPerMm: Math.max(.0001, number(config?.pxPerMm, PX_PER_MM_AT_96_DPI)),
        zoomMode: String(config?.zoomMode || 'CssLayout'),
        cropMode: Boolean(config?.cropMode),
        contentPanMode: Boolean(config?.contentPanMode),
        unit: String(config?.unit || 'Millimeter'),
        rulersVisible: config?.rulersVisible !== false,
        guidesVisible: config?.guidesVisible !== false,
        snapToGrid: Boolean(config?.snapToGrid),
        snapToGuides: Boolean(config?.snapToGuides),
        snapToPage: Boolean(config?.snapToPage),
        snapToObjects: Boolean(config?.snapToObjects),
        snapInObjects: Boolean(config?.snapInObjects),
        gridSpacingMm: Math.max(.1, number(config?.gridSpacingMm, 2.5)),
        connectorTool: String(config?.connectorTool || 'None'),
        selectedElementIds: new Set((Array.isArray(config?.selectedElementIds) ? config.selectedElementIds : [])
            .map(value => { try { return (String(value || '').toLowerCase()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:(Array.isArray(config?.selectedElementIds) ? config.selectedElementIds@892', __javascriptError); throw __javascriptError; } })
            .filter(Boolean)),
        internalClipboardAvailable: Boolean(config?.internalClipboardAvailable),
        clipboardRevision: number(config?.clipboardRevision, 0)
    };

    let state = canvasStates.get(stage);
    if (!state) {
        state = {
            stage,
            scroll,
            page,
            dotnet,
            config: normalizedConfig,
            operation: null,
            cursorX: null,
            cursorY: null,
            drawPending: false,
            cropTimers: new Map(),
            connectorGhost: null,
            lastCanvasClick: null,
            externalDropPreview: null,
            insertDropPreview: null,
            keyboardActive: false,
            internalClipboardArmed: normalizedConfig.internalClipboardAvailable,
            externalClipboardLikely: false,
            pendingPasteTimer: 0,
            lastInsertionPoint: null,
            pendingComponentAction: null,
            suppressNextComponentClickUntil: 0,
            selectionFramePending: false,
            handlers: {}
        };

        const handlers = state.handlers;
        handlers.stagePointerDown = event => { try { return (pointerDown(state, event)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.stagePointerDown@927', __javascriptError); throw __javascriptError; } };
        handlers.stageDoubleClick = event => { try { return (componentDoubleClick(state, event)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.stageDoubleClick@928', __javascriptError); throw __javascriptError; } };
        handlers.stageContextMenu = event => { try { return (designerContextMenu(state, event)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.stageContextMenu@929', __javascriptError); throw __javascriptError; } };
        handlers.stagePointerMove = event => { try { return (pointerMove(state, event)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.stagePointerMove@930', __javascriptError); throw __javascriptError; } };
        handlers.stagePointerUp = event => { try { return (pointerUp(state, event)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.stagePointerUp@931', __javascriptError); throw __javascriptError; } };
        handlers.stagePointerCancel = event => { try { return (pointerCancel(state, event)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.stagePointerCancel@932', __javascriptError); throw __javascriptError; } };
        handlers.lostPointerCapture = event => { try {
            if (state.operation?.pointerId === event.pointerId) resetPointerOperation(state, true);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.lostPointerCapture@933', __javascriptError); throw __javascriptError; }};
        handlers.windowPointerDown = event => { try {
            if (state.operation && !state.stage.contains(event.target)) resetPointerOperation(state, true);
            if (state.stage.contains(event.target)) state.keyboardActive = true;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.windowPointerDown@936', __javascriptError); throw __javascriptError; }};
        handlers.windowPointerUp = event => { try { return (pointerUp(state, event)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.windowPointerUp@940', __javascriptError); throw __javascriptError; } };
        handlers.windowPointerCancel = event => { try { return (pointerCancel(state, event)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.windowPointerCancel@941', __javascriptError); throw __javascriptError; } };
        handlers.windowBlur = () => { try {
            state.externalClipboardLikely = true;
            resetPointerOperation(state, true);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.windowBlur@942', __javascriptError); throw __javascriptError; }};
        handlers.visibilityChange = () => { try {
            if (document.hidden) {
                state.externalClipboardLikely = true;
                resetPointerOperation(state, true);
            }
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.visibilityChange@946', __javascriptError); throw __javascriptError; }};
        handlers.stagePointerLeave = () => { try {
            state.cursorX = null;
            state.cursorY = null;
            nextAnimationFrame(state);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.stagePointerLeave@952', __javascriptError); throw __javascriptError; }};
        handlers.stageWheel = event => { try { return (cropWheel(state, event)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.stageWheel@957', __javascriptError); throw __javascriptError; } };
        handlers.stageKeyDown = event => { try { return (canvasKeyDown(state, event)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.stageKeyDown@958', __javascriptError); throw __javascriptError; } };
        handlers.documentKeyDown = event => { try { return (canvasDocumentKeyDown(state, event)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.documentKeyDown@959', __javascriptError); throw __javascriptError; } };
        handlers.documentPaste = event => { try { return (canvasDocumentPaste(state, event)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.documentPaste@960', __javascriptError); throw __javascriptError; } };
        handlers.stageDragEnter = event => { try { return (insertionDragOver(state, event)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.stageDragEnter@961', __javascriptError); throw __javascriptError; } };
        handlers.stageDragOver = event => { try { return (insertionDragOver(state, event)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.stageDragOver@962', __javascriptError); throw __javascriptError; } };
        handlers.stageDrop = event => { try { return (insertionDrop(state, event)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.stageDrop@963', __javascriptError); throw __javascriptError; } };
        handlers.stageDragLeave = event => { try {
            const next = event.relatedTarget;
            if (!next || !state.stage.contains(next)) {
                clearExternalDropPreview(state);
                clearInsertionDrag(state);
            }
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.stageDragLeave@964', __javascriptError); throw __javascriptError; }};
        handlers.documentDragStart = event => { try { namedMediaDragStart(event); return insertionDragStart(state, event); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.documentDragStart@971', __javascriptError); throw __javascriptError; } };
        handlers.documentDragEnd = () => { try { return (insertionDragEnd(state)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.documentDragEnd@972', __javascriptError); throw __javascriptError; } };
        handlers.documentClick = event => { try {
            if (performance.now() < number(state.suppressNextComponentClickUntil) && event.target?.closest?.('.devextreme-component-host')) {
                state.suppressNextComponentClickUntil = 0;
                event.preventDefault();
                event.stopImmediatePropagation();
                return;
            }
            suppressInsertionClick(state, event);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.documentClick@973', __javascriptError); throw __javascriptError; }};
        handlers.scroll = () => { try { nextAnimationFrame(state); scheduleSelectionVisualFrame(state);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.scroll@982', __javascriptError); throw __javascriptError; }};
        handlers.publisherNavigate = event => { try { return (scheduleComponentNavigation(state, event.detail)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.publisherNavigate@983', __javascriptError); throw __javascriptError; } };
        handlers.publisherOpenUrl = event => { try { return (scheduleComponentUrl(state, event.detail)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.publisherOpenUrl@984', __javascriptError); throw __javascriptError; } };
        handlers.mapViewportChanged = event => { try { return (commitMapViewportEvent(state, event)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handlers.mapViewportChanged@985', __javascriptError); throw __javascriptError; } };

        // Capture phase keeps selection alive even when nested media, SVG, chart,
        // or animation content stops pointer events in its own component tree.
        stage.addEventListener('pointerdown', handlers.stagePointerDown, true);
        stage.addEventListener('dblclick', handlers.stageDoubleClick, true);
        stage.addEventListener('contextmenu', handlers.stageContextMenu, true);
        stage.addEventListener('pointermove', handlers.stagePointerMove, true);
        stage.addEventListener('pointerup', handlers.stagePointerUp, true);
        stage.addEventListener('pointercancel', handlers.stagePointerCancel, true);
        stage.addEventListener('lostpointercapture', handlers.lostPointerCapture);
        window.addEventListener('pointerdown', handlers.windowPointerDown, true);
        window.addEventListener('pointerup', handlers.windowPointerUp, true);
        window.addEventListener('pointercancel', handlers.windowPointerCancel, true);
        window.addEventListener('publisherstudio:navigate', handlers.publisherNavigate);
        window.addEventListener('publisherstudio:open-url', handlers.publisherOpenUrl);
        window.addEventListener('blur', handlers.windowBlur);
        document.addEventListener('visibilitychange', handlers.visibilityChange);
        stage.addEventListener('pointerleave', handlers.stagePointerLeave);
        stage.addEventListener('wheel', handlers.stageWheel, { passive: false });
        stage.addEventListener('publisherstudio:map-viewport-changed', handlers.mapViewportChanged, true);
        stage.addEventListener('keydown', handlers.stageKeyDown);
        document.addEventListener('keydown', handlers.documentKeyDown, true);
        document.addEventListener('paste', handlers.documentPaste, true);
        stage.addEventListener('dragenter', handlers.stageDragEnter);
        stage.addEventListener('dragover', handlers.stageDragOver);
        stage.addEventListener('drop', handlers.stageDrop);
        stage.addEventListener('dragleave', handlers.stageDragLeave);
        document.addEventListener('dragstart', handlers.documentDragStart, true);
        document.addEventListener('dragend', handlers.documentDragEnd, true);
        document.addEventListener('click', handlers.documentClick, true);
        scroll.addEventListener('scroll', handlers.scroll, { passive: true });

        if (typeof ResizeObserver === 'function') {
            state.resizeObserver = new ResizeObserver(() => { try { nextAnimationFrame(state); refreshContentFit(state.page); updateResizeHandleCursors(state.page); scheduleSelectionVisualFrame(state);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@1019', __javascriptError); throw __javascriptError; }});
            state.resizeObserver.observe(stage);
            state.resizeObserver.observe(scroll);
        }
        canvasStates.set(stage, state);
        namedMediaDragRuntime(page);
        startCanvasGamepad(state);
    }

    const pageChanged = state.page !== page;
    const clipboardChanged = number(state.config?.clipboardRevision, -1) !== normalizedConfig.clipboardRevision;
    if (clipboardChanged && normalizedConfig.internalClipboardAvailable) {
        state.internalClipboardArmed = true;
        state.externalClipboardLikely = false;
    }
    if (pageChanged && state.operation) resetPointerOperation(state, true);
    if (pageChanged) {
        clearInsertionDrag(state);
        clearExternalDropPreview(state);
        try { clearPublicationPreview(state.page?.id || state.page); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@1037', __caughtJavaScriptError);  }
    }
    state.scroll = scroll;
    state.page = page;
    state.dotnet = dotnet;
    state.config = normalizedConfig;
    state.horizontalRuler = document.getElementById(horizontalRulerId);
    state.verticalRuler = document.getElementById(verticalRulerId);

    bindRuler(state.horizontalRuler, 'Horizontal', state);
    bindRuler(state.verticalRuler, 'Vertical', state);
    syncEditorZoomRendering(state);
    refreshContentFit(state.page);
    updateResizeHandleCursors(state.page);
    scheduleSelectionVisualFrame(state);
    nextAnimationFrame(state);
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:initializeCanvas@871', __javascriptError); throw __javascriptError; }}

export function disposeCanvas(stageId) { try {
    const stage = document.getElementById(stageId);
    if (!stage) return;
    const state = canvasStates.get(stage);
    if (!state) return;

    resetPointerOperation(state, true);
    state.selectionFramePending = false;
    cancelPendingComponentAction(state);
    if (state.pendingPasteTimer) clearTimeout(state.pendingPasteTimer);
    state.pendingPasteTimer = 0;
    clearObjectAlignmentFeedback(state);
    clearInsertionDrag(state);
    clearExternalDropPreview(state);
    state.resizeObserver?.disconnect?.();
    if (state.gamepad?.frame) cancelAnimationFrame(state.gamepad.frame);
    state.gamepad = null;
    for (const timer of state.cropTimers?.values?.() || []) clearTimeout(timer);
    state.cropTimers?.clear?.();

    const handlers = state.handlers || {};
    stage.removeEventListener('pointerdown', handlers.stagePointerDown, true);
    stage.removeEventListener('dblclick', handlers.stageDoubleClick, true);
    stage.removeEventListener('contextmenu', handlers.stageContextMenu, true);
    stage.removeEventListener('pointermove', handlers.stagePointerMove, true);
    stage.removeEventListener('pointerup', handlers.stagePointerUp, true);
    stage.removeEventListener('pointercancel', handlers.stagePointerCancel, true);
    stage.removeEventListener('lostpointercapture', handlers.lostPointerCapture);
    stage.removeEventListener('pointerleave', handlers.stagePointerLeave);
    stage.removeEventListener('wheel', handlers.stageWheel);
    stage.removeEventListener('publisherstudio:map-viewport-changed', handlers.mapViewportChanged, true);
    stage.removeEventListener('keydown', handlers.stageKeyDown);
    document.removeEventListener('keydown', handlers.documentKeyDown, true);
    document.removeEventListener('paste', handlers.documentPaste, true);
    stage.removeEventListener('dragenter', handlers.stageDragEnter);
    stage.removeEventListener('dragover', handlers.stageDragOver);
    stage.removeEventListener('drop', handlers.stageDrop);
    stage.removeEventListener('dragleave', handlers.stageDragLeave);
    document.removeEventListener('dragstart', handlers.documentDragStart, true);
    document.removeEventListener('dragend', handlers.documentDragEnd, true);
    document.removeEventListener('click', handlers.documentClick, true);
    state.scroll?.removeEventListener?.('scroll', handlers.scroll);
    window.removeEventListener('pointerdown', handlers.windowPointerDown, true);
    window.removeEventListener('pointerup', handlers.windowPointerUp, true);
    window.removeEventListener('pointercancel', handlers.windowPointerCancel, true);
    window.removeEventListener('publisherstudio:navigate', handlers.publisherNavigate);
    window.removeEventListener('publisherstudio:open-url', handlers.publisherOpenUrl);
    window.removeEventListener('blur', handlers.windowBlur);
    document.removeEventListener('visibilitychange', handlers.visibilityChange);

    state.dotnet = null;
    canvasStates.delete(stage);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:disposeCanvas@1056', __javascriptError); throw __javascriptError; }}

function bindRuler(canvas, orientation, state) { try {
    if (!canvas || boundRulers.has(canvas)) return;
    boundRulers.add(canvas);
    canvas.addEventListener('pointerdown', event => { try { return (rulerPointerDown(state, orientation, canvas, event)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:canvas.addEventListener@1113', __javascriptError); throw __javascriptError; } });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:bindRuler@1110', __javascriptError); throw __javascriptError; }}

function rulerPointerDown(state, orientation, canvas, event) { try {
    if (event.button !== 0) return;
    state.rulerGuide = { orientation, pointerId: event.pointerId, canvas };
    canvas.setPointerCapture(event.pointerId);

    const move = moveEvent => { try {
        if (!state.rulerGuide || moveEvent.pointerId !== state.rulerGuide.pointerId) return;
        updateRulerGuidePreview(state, orientation, moveEvent);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:move@1121', __javascriptError); throw __javascriptError; }};
    const finish = upEvent => { try {
        if (!state.rulerGuide || upEvent.pointerId !== state.rulerGuide.pointerId) return;
        canvas.removeEventListener('pointermove', move);
        canvas.removeEventListener('pointerup', finish);
        canvas.removeEventListener('pointercancel', finish);
        finishRulerGuide(state, orientation, upEvent);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:finish@1125', __javascriptError); throw __javascriptError; }};

    canvas.addEventListener('pointermove', move);
    canvas.addEventListener('pointerup', finish);
    canvas.addEventListener('pointercancel', finish);
    updateRulerGuidePreview(state, orientation, event);
    event.preventDefault();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:rulerPointerDown@1116', __javascriptError); throw __javascriptError; }}

function guidePositionFromPointer(state, orientation, event) { try {
    const pageRect = state.page.getBoundingClientRect();
    return orientation === 'Horizontal'
        ? (event.clientY - pageRect.top) / state.config.pxPerMm
        : (event.clientX - pageRect.left) / state.config.pxPerMm;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:guidePositionFromPointer@1140', __javascriptError); throw __javascriptError; }}

function updateRulerGuidePreview(state, orientation, event) { try {
    const position = guidePositionFromPointer(state, orientation, event);
    let preview = state.guidePreview;
    if (!preview) {
        preview = document.createElement('div');
        preview.className = `guide-line guide-preview ${orientation.toLowerCase()}`;
        state.page.appendChild(preview);
        state.guidePreview = preview;
    }
    if (orientation === 'Horizontal') preview.style.top = `${position * state.config.pxPerMm}px`;
    else preview.style.left = `${position * state.config.pxPerMm}px`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:updateRulerGuidePreview@1147', __javascriptError); throw __javascriptError; }}

function finishRulerGuide(state, orientation, event) { try {
    const position = guidePositionFromPointer(state, orientation, event);
    const max = orientation === 'Horizontal'
        ? number(state.page.dataset.pageHeightMm)
        : number(state.page.dataset.pageWidthMm);

    state.guidePreview?.remove();
    state.guidePreview = null;
    state.rulerGuide = null;
    if (position >= 0 && position <= max)
        state.dotnet.invokeMethodAsync('AddGuideAt', orientation, position);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:finishRulerGuide@1160', __javascriptError); throw __javascriptError; }}


function isPublisherEditableTarget(target) { try {
    if (!(target instanceof Element)) return false;
    return Boolean(target.closest('input,textarea,select,button,a,[role="button"],[role="menuitem"],[contenteditable="true"],[contenteditable=""],[role="textbox"],[role="dialog"],.modal-backdrop,.editor-modal-backdrop'));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:isPublisherEditableTarget@1174', __javascriptError); throw __javascriptError; }}


function isDesignerComponentControlTarget(target, element) { try {
    if (!target?.closest || !element || String(element.dataset?.elementKind || '').toLowerCase() !== 'devextremecomponent') return false;
    const control = target.closest([
        'button', 'a[href]', 'input', 'textarea', 'select',
        '[role="button"]', '[role="tab"]', '[role="menuitem"]', '[role="option"]',
        '.dx-button', '.dx-gallery-nav-button-prev', '.dx-gallery-nav-button-next',
        '.dx-gallery-indicator-item', '.dx-checkbox', '.dx-switch', '.dx-radiobutton',
        '.dx-slider-handle', '.dx-scrollbar', '.dx-scrollable-scroll'
    ].join(','));
    return Boolean(control && element.contains(control));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:isDesignerComponentControlTarget@1180', __javascriptError); throw __javascriptError; }}

function resetCanvasTransientState(state, restoreDom = true) { try {
    resetPointerOperation(state, restoreDom);
    clearConnectorOperation(state, true);
    clearInsertionDrag(state);
    clearExternalDropPreview(state);
    try { clearPublicationPreview(state.page?.id || state.page); }
    catch (error) { console.warn('Publisher transient preview cleanup failed.', error); }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:resetCanvasTransientState@1192', __javascriptError); throw __javascriptError; }}

function invokeCanvasKeyboardCommand(state, event, method, ...args) { try {
    resetCanvasTransientState(state, true);
    safeDotNet(state, method, ...args);
    try { state.stage.focus({ preventScroll: true }); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@1204', __caughtJavaScriptError);  }
    event.preventDefault();
    event.stopPropagation();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:invokeCanvasKeyboardCommand@1201', __javascriptError); throw __javascriptError; }}

function canvasDocumentKeyDown(state, event) { try {
    if (!state?.keyboardActive || !state.stage?.isConnected || event.defaultPrevented || isPublisherEditableTarget(event.target)) return;
    const key = String(event.key || '').toLowerCase();
    const command = event.ctrlKey || event.metaKey;

    if (key === 'escape') {
        resetCanvasTransientState(state, true);
        safeDotNet(state, 'CancelActiveTool');
        event.preventDefault();
        return;
    }
    if (command && key === 'c') {
        state.internalClipboardArmed = true;
        state.externalClipboardLikely = false;
        return invokeCanvasKeyboardCommand(state, event, 'KeyboardCopy');
    }
    if (command && key === 'x') {
        state.internalClipboardArmed = true;
        state.externalClipboardLikely = false;
        return invokeCanvasKeyboardCommand(state, event, 'KeyboardCut');
    }
    if (command && key === 'v') {
        // Do not cancel the native paste event: it is the only standards-based way
        // for the browser to expose files copied from Explorer/Finder or external text.
        if (state.pendingPasteTimer) clearTimeout(state.pendingPasteTimer);
        state.pendingPasteTimer = setTimeout(() => { try {
            state.pendingPasteTimer = 0;
            resetCanvasTransientState(state, true);
            safeDotNet(state, 'KeyboardPaste');
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:setTimeout@1234', __javascriptError); throw __javascriptError; }}, 80);
        return;
    }
    if (command && key === 'd') return invokeCanvasKeyboardCommand(state, event, 'KeyboardDuplicate');
    if (command && key === 'a') return invokeCanvasKeyboardCommand(state, event, 'KeyboardSelectAll');
    if (command && key === 'g' && event.shiftKey) return invokeCanvasKeyboardCommand(state, event, 'KeyboardUngroup');
    if (command && key === 'g') return invokeCanvasKeyboardCommand(state, event, 'KeyboardGroup');
    if (command && key === 'z' && event.shiftKey) return invokeCanvasKeyboardCommand(state, event, 'KeyboardRedo');
    if (command && key === 'z') return invokeCanvasKeyboardCommand(state, event, 'KeyboardUndo');
    if (command && key === 'y') return invokeCanvasKeyboardCommand(state, event, 'KeyboardRedo');
    if (event.altKey && key === 'home') return invokeCanvasKeyboardCommand(state, event, 'KeyboardLayerMove', 'front');
    if (event.altKey && key === 'end') return invokeCanvasKeyboardCommand(state, event, 'KeyboardLayerMove', 'back');
    if (event.altKey && key === 'pageup') return invokeCanvasKeyboardCommand(state, event, 'KeyboardLayerMove', 'forward');
    if (event.altKey && key === 'pagedown') return invokeCanvasKeyboardCommand(state, event, 'KeyboardLayerMove', 'backward');
    if (key === 'delete' || key === 'backspace') return invokeCanvasKeyboardCommand(state, event, 'KeyboardDelete');

    if (key.startsWith('arrow')) {
        const step = event.altKey ? .1 : event.shiftKey ? 5 : 1;
        const dx = key === 'arrowleft' ? -step : key === 'arrowright' ? step : 0;
        const dy = key === 'arrowup' ? -step : key === 'arrowdown' ? step : 0;
        return invokeCanvasKeyboardCommand(state, event, 'KeyboardNudge', dx, dy);
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:canvasDocumentKeyDown@1209', __javascriptError); throw __javascriptError; }}

function startCanvasGamepad(state) { try {
    if (state.gamepad || typeof navigator.getGamepads !== 'function') return;
    const controller = { frame: 0, buttons: [], axisX: 0, axisY: 0, nextRepeat: 0 };
    state.gamepad = controller;
    const pressed = (gamepad, index) => { try { return (Boolean(gamepad?.buttons?.[index]?.pressed || number(gamepad?.buttons?.[index]?.value) > .55)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pressed@1266', __javascriptError); throw __javascriptError; } };
    const edge = (gamepad, index) => { try {
        const value = pressed(gamepad, index);
        const previous = Boolean(controller.buttons[index]);
        controller.buttons[index] = value;
        return value && !previous;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:edge@1267', __javascriptError); throw __javascriptError; }};
    const tick = time => { try {
        if (state.gamepad !== controller || !state.stage?.isConnected) return;
        controller.frame = requestAnimationFrame(tick);
        if (document.hidden || !state.keyboardActive) return;
        const gamepad = [...(navigator.getGamepads?.() || [])].find(Boolean);
        if (!gamepad) return;
        const axisX = Math.abs(number(gamepad.axes?.[0])) > .45 ? Math.sign(number(gamepad.axes?.[0])) : 0;
        const axisY = Math.abs(number(gamepad.axes?.[1])) > .45 ? Math.sign(number(gamepad.axes?.[1])) : 0;
        const x = (pressed(gamepad, 14) ? -1 : pressed(gamepad, 15) ? 1 : 0) || axisX;
        const y = (pressed(gamepad, 12) ? -1 : pressed(gamepad, 13) ? 1 : 0) || axisY;
        const changed = x !== controller.axisX || y !== controller.axisY;
        if (x || y) {
            if (changed || time >= controller.nextRepeat) {
                safeDotNet(state, 'KeyboardNudge', x, y);
                controller.nextRepeat = time + (changed ? 260 : 90);
            }
        } else controller.nextRepeat = 0;
        controller.axisX = x;
        controller.axisY = y;

        if (edge(gamepad, 4)) safeDotNet(state, 'KeyboardLayerMove', 'backward');
        if (edge(gamepad, 5)) safeDotNet(state, 'KeyboardLayerMove', 'forward');
        if (edge(gamepad, 6)) safeDotNet(state, 'KeyboardLayerMove', 'back');
        if (edge(gamepad, 7)) safeDotNet(state, 'KeyboardLayerMove', 'front');
        if (edge(gamepad, 2)) safeDotNet(state, 'KeyboardDuplicate');
        if (edge(gamepad, 1)) safeDotNet(state, 'ClearSelectionFromCanvas');
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:tick@1273', __javascriptError); throw __javascriptError; }};
    controller.frame = requestAnimationFrame(tick);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:startCanvasGamepad@1262', __javascriptError); throw __javascriptError; }}

function canvasKeyDown(state, event) { try {
    canvasDocumentKeyDown(state, event);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:canvasKeyDown@1303', __javascriptError); throw __javascriptError; }}

function connectorToolActive(state) { try {
    return state.config.connectorTool && state.config.connectorTool !== 'None';
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:connectorToolActive@1307', __javascriptError); throw __javascriptError; }}

function signalConnectorToolActive(state) { try {
    return state.config.connectorTool === 'SignalConnector' || state.config.connectorTool === 'SignalArrow';
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:signalConnectorToolActive@1311', __javascriptError); throw __javascriptError; }}

function pagePointMm(state, event) { try {
    const pageRect = state.page.getBoundingClientRect();
    return {
        x: clamp((event.clientX - pageRect.left) / state.config.pxPerMm, 0, number(state.page.dataset.pageWidthMm)),
        y: clamp((event.clientY - pageRect.top) / state.config.pxPerMm, 0, number(state.page.dataset.pageHeightMm))
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pagePointMm@1315', __javascriptError); throw __javascriptError; }}

function defaultConnectorControls(source, target) { try {
    const dx = Math.max(12, Math.abs(target.x - source.x) * .48);
    const direction = target.x >= source.x ? 1 : -1;
    return {
        first: { x: source.x + dx * direction, y: source.y },
        second: { x: target.x - dx * direction, y: target.y }
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:defaultConnectorControls@1323', __javascriptError); throw __javascriptError; }}

function connectorPath(kind, source, target, controls = null) { try {
    if (kind === 'Elbow') {
        const middleX = (source.x + target.x) / 2;
        return `M ${source.x} ${source.y} L ${middleX} ${source.y} L ${middleX} ${target.y} L ${target.x} ${target.y}`;
    }
    if (kind === 'Curved') {
        const value = controls || defaultConnectorControls(source, target);
        const first = value.first || value.c1;
        const second = value.second || value.c2;
        return `M ${source.x} ${source.y} C ${first.x} ${first.y}, ${second.x} ${second.y}, ${target.x} ${target.y}`;
    }
    return `M ${source.x} ${source.y} L ${target.x} ${target.y}`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:connectorPath@1332', __javascriptError); throw __javascriptError; }}

function portPointMm(state, port) { try {
    const owner = port.closest?.('[data-publication-element][data-element-id]');
    if (owner && port.dataset.portId) {
        return pointForElementRelative(owner, {
            x: clamp(number(port.style.left) / 100, 0, 1),
            y: clamp(number(port.style.top) / 100, 0, 1)
        }, state.config.pxPerMm);
    }
    const pageRect = state.page.getBoundingClientRect();
    const rect = port.getBoundingClientRect();
    return {
        x: (rect.left + rect.width / 2 - pageRect.left) / state.config.pxPerMm,
        y: (rect.top + rect.height / 2 - pageRect.top) / state.config.pxPerMm
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:portPointMm@1346', __javascriptError); throw __javascriptError; }}

function relativePointForElement(state, element, event) { try {
    const point = pagePointMm(state, event);
    const bounds = elementMm(element, state.config.pxPerMm);
    const centerX = bounds.x + bounds.width / 2;
    const centerY = bounds.y + bounds.height / 2;
    const radians = -parseRotation(element) * Math.PI / 180;
    const dx = point.x - centerX;
    const dy = point.y - centerY;
    const localX = centerX + dx * Math.cos(radians) - dy * Math.sin(radians);
    const localY = centerY + dx * Math.sin(radians) + dy * Math.cos(radians);
    return {
        x: bounds.width > 0 ? clamp((localX - bounds.x) / bounds.width, 0, 1) : .5,
        y: bounds.height > 0 ? clamp((localY - bounds.y) / bounds.height, 0, 1) : .5
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:relativePointForElement@1362', __javascriptError); throw __javascriptError; }}

function pointForElementRelative(element, relative, pxPerMm) { try {
    const bounds = elementMm(element, pxPerMm);
    const centerX = bounds.x + bounds.width / 2;
    const centerY = bounds.y + bounds.height / 2;
    const rawX = bounds.x + bounds.width * clamp(relative.x, 0, 1);
    const rawY = bounds.y + bounds.height * clamp(relative.y, 0, 1);
    const radians = parseRotation(element) * Math.PI / 180;
    const dx = rawX - centerX;
    const dy = rawY - centerY;
    return {
        x: centerX + dx * Math.cos(radians) - dy * Math.sin(radians),
        y: centerY + dx * Math.sin(radians) + dy * Math.cos(radians)
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pointForElementRelative@1378', __javascriptError); throw __javascriptError; }}

function createConnectorPortPreview(owner, relative, className = '') { try {
    const preview = document.createElement('span');
    preview.className = `connector-port connector-port-custom connector-port-preview ${className}`.trim();
    preview.style.left = `${clamp(relative.x, 0, 1) * 100}%`;
    preview.style.top = `${clamp(relative.y, 0, 1) * 100}%`;
    owner.appendChild(preview);
    return preview;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:createConnectorPortPreview@1393', __javascriptError); throw __javascriptError; }}

function dynamicConnectorTarget(state, event, excluded) { try {
    for (const candidate of document.elementsFromPoint(event.clientX, event.clientY)) {
        const owner = candidate.closest?.('[data-publication-element][data-element-id]');
        if (!owner || !state.page.contains(owner) || owner.matches('[data-connector-id]') || owner.classList.contains('locked')) continue;
        const ownerId = String(owner.dataset.elementId || '');
        if (!ownerId || excluded.has(ownerId)) continue;
        const relative = relativePointForElement(state, owner, event);
        return {
            port: null,
            preview: createConnectorPortPreview(owner, relative, 'connector-port-target'),
            ownerId,
            anchor: 'Center',
            portId: '',
            createPort: true,
            relativeX: relative.x,
            relativeY: relative.y,
            point: pointForElementRelative(owner, relative, state.config.pxPerMm),
            kind: 'Element'
        };
    }
    return null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:dynamicConnectorTarget@1402', __javascriptError); throw __javascriptError; }}

function findConnectorTarget(state, event, excludedIds = []) { try {
    const excluded = new Set(excludedIds.filter(Boolean));
    let best = null;
    let bestDistance = 22;
    for (const port of state.page.querySelectorAll('[data-connector-port]')) {
        if (excluded.has(port.dataset.ownerId)) continue;
        const owner = port.closest('[data-publication-element]');
        if (!owner || owner.classList.contains('locked')) continue;
        const rect = port.getBoundingClientRect();
        const x = rect.left + rect.width / 2;
        const y = rect.top + rect.height / 2;
        const distance = Math.hypot(event.clientX - x, event.clientY - y);
        if (distance <= bestDistance) {
            bestDistance = distance;
            best = {
                port,
                ownerId: port.dataset.ownerId,
                anchor: port.dataset.anchor,
                portId: port.dataset.portId || '',
                createPort: false,
                relativeX: number(port.style.left) / 100 || 0,
                relativeY: number(port.style.top) / 100 || 0,
                point: portPointMm(state, port),
                kind: 'Element'
            };
        }
    }
    return best || dynamicConnectorTarget(state, event, excluded);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:findConnectorTarget@1425', __javascriptError); throw __javascriptError; }}

function ensureConnectorGhost(state, markerEnd) { try {
    if (state.connectorGhost) return state.connectorGhost;
    const ns = 'http://www.w3.org/2000/svg';
    const svg = document.createElementNS(ns, 'svg');
    svg.classList.add('connector-ghost');
    svg.setAttribute('viewBox', `0 0 ${number(state.page.dataset.pageWidthMm)} ${number(state.page.dataset.pageHeightMm)}`);
    svg.setAttribute('preserveAspectRatio', 'none');
    const defs = document.createElementNS(ns, 'defs');
    const marker = document.createElementNS(ns, 'marker');
    marker.setAttribute('id', 'publisher-connector-ghost-arrow');
    marker.setAttribute('markerWidth', '7');
    marker.setAttribute('markerHeight', '7');
    marker.setAttribute('refX', '6');
    marker.setAttribute('refY', '3.5');
    marker.setAttribute('orient', 'auto-start-reverse');
    marker.setAttribute('markerUnits', 'strokeWidth');
    const triangle = document.createElementNS(ns, 'path');
    triangle.setAttribute('d', 'M 0 0 L 7 3.5 L 0 7 z');
    triangle.setAttribute('fill', 'currentColor');
    marker.appendChild(triangle);
    defs.appendChild(marker);
    svg.appendChild(defs);
    const path = document.createElementNS(ns, 'path');
    path.classList.add('connector-ghost-line');
    if (markerEnd) path.setAttribute('marker-end', 'url(#publisher-connector-ghost-arrow)');
    svg.appendChild(path);
    state.page.appendChild(svg);
    state.connectorGhost = { svg, path };
    return state.connectorGhost;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ensureConnectorGhost@1455', __javascriptError); throw __javascriptError; }}

function showConnectorGhost(state, operation, target) { try {
    const ghost = ensureConnectorGhost(state, operation.markerEnd);
    ghost.path.setAttribute('d', connectorPath(operation.pathKind || 'Curved', operation.fixedPoint, target.point));
    ghost.svg.classList.add('visible');
    operation.target = target;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:showConnectorGhost@1486', __javascriptError); throw __javascriptError; }}

function hideConnectorGhost(state) { try {
    state.connectorGhost?.svg.classList.remove('visible');
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:hideConnectorGhost@1493', __javascriptError); throw __javascriptError; }}

function clearConnectorOperation(state, restoreOriginal) { try {
    const operation = state.operation;
    if (operation?.kind === 'connector-reconnect' && operation.connector && restoreOriginal)
        operation.connector.style.visibility = '';
    if (state.connectorGhost) {
        state.connectorGhost.svg.remove();
        state.connectorGhost = null;
    }
    if (operation?.target?.port) operation.target.port.classList.remove('connector-port-target');
    operation?.target?.preview?.remove?.();
    operation?.sourcePreview?.remove?.();
    if (operation?.sourcePort) operation.sourcePort.classList.remove('connector-port-source');
    if (operation?.kind?.startsWith('connector-')) state.operation = null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:clearConnectorOperation@1497', __javascriptError); throw __javascriptError; }}

function updateConnectorDrag(state, event, operation) { try {
    operation.target?.port?.classList.remove('connector-port-target');
    operation.target?.preview?.remove?.();
    let target = findConnectorTarget(state, event, operation.excludedIds);
    if (!target && operation.signal) {
        target = { kind: 'Canvas', ownerId: '', anchor: 'Center', point: pagePointMm(state, event), port: null };
    } else if (target) {
        target.kind = 'Element';
    }
    if (!target) {
        operation.target = null;
        hideConnectorGhost(state);
        return;
    }
    target.port?.classList.add('connector-port-target');
    showConnectorGhost(state, operation, target);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:updateConnectorDrag@1512', __javascriptError); throw __javascriptError; }}

function finishConnectorDrag(state, operation) { try {
    const target = operation.target;
    if (target) {
        const source = operation.sourceEndpoint || {
            kind: 'Element', ownerId: operation.sourceOwnerId || '', anchor: operation.sourceAnchor || 'Center',
            portId: '', createPort: false, relativeX: 0.5, relativeY: 0.5, point: operation.fixedPoint
        };
        if (operation.kind === 'connector-new') {
            safeDotNet(state, 'CommitConnectorAdvanced',
                source.kind || 'Element', source.ownerId || '', source.anchor || 'Center', source.portId || '', Boolean(source.createPort), number(source.relativeX, .5), number(source.relativeY, .5), number(source.point?.x), number(source.point?.y),
                target.kind || 'Element', target.ownerId || '', target.anchor || 'Center', target.portId || '', Boolean(target.createPort), number(target.relativeX, .5), number(target.relativeY, .5), number(target.point?.x), number(target.point?.y),
                operation.tool || (operation.markerEnd ? 'Arrow' : 'Connector'));
        } else {
            safeDotNet(state, 'ReconnectConnectorAdvanced', operation.connectorId, operation.endpoint,
                target.kind || 'Element', target.ownerId || '', target.anchor || 'Center', target.portId || '', Boolean(target.createPort),
                number(target.relativeX, .5), number(target.relativeY, .5), number(target.point?.x), number(target.point?.y));
        }
    }
    clearConnectorOperation(state, true);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:finishConnectorDrag@1530', __javascriptError); throw __javascriptError; }}

function parseRotation(element) { try {
    const match = /rotate\(([-+0-9.]+)deg\)/i.exec(element.style.transform || '');
    return match ? number(match[1]) : 0;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:parseRotation@1551', __javascriptError); throw __javascriptError; }}

function anchorPointForElement(element, anchor, pxPerMm) { try {
    const bounds = elementMm(element, pxPerMm);
    const local = {
        TopLeft: [0, 0], Top: [.5, 0], TopRight: [1, 0], Right: [1, .5],
        BottomRight: [1, 1], Bottom: [.5, 1], BottomLeft: [0, 1], Left: [0, .5], Center: [.5, .5]
    }[anchor] || [.5, .5];
    const centerX = bounds.x + bounds.width / 2;
    const centerY = bounds.y + bounds.height / 2;
    const rawX = bounds.x + bounds.width * local[0];
    const rawY = bounds.y + bounds.height * local[1];
    const radians = parseRotation(element) * Math.PI / 180;
    const dx = rawX - centerX;
    const dy = rawY - centerY;
    return {
        x: centerX + dx * Math.cos(radians) - dy * Math.sin(radians),
        y: centerY + dx * Math.sin(radians) + dy * Math.cos(radians)
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:anchorPointForElement@1556', __javascriptError); throw __javascriptError; }}

function connectorEndpointPoint(state, connector, prefix) { try {
    if (String(connector.dataset[`${prefix}Kind`] || 'Element').toLowerCase() === 'canvas') {
        return { x: number(connector.dataset[`${prefix}X`]), y: number(connector.dataset[`${prefix}Y`]) };
    }
    const id = connector.dataset[`${prefix}ElementId`] || '';
    const element = state.page.querySelector(`[data-element-id="${CSS.escape(id)}"]`);
    if (!element) return null;
    const portId = connector.dataset[`${prefix}PortId`] || '';
    if (portId) {
        const port = element.querySelector(`[data-connector-port][data-port-id="${CSS.escape(portId)}"]`);
        if (port) return portPointMm(state, port);
    }
    return anchorPointForElement(element, connector.dataset[`${prefix}Anchor`] || 'Center', state.config.pxPerMm);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:connectorEndpointPoint@1575', __javascriptError); throw __javascriptError; }}

function connectorControls(connector, source, target) { try {
    const distance = Math.max(16, Math.min(70, Math.hypot(target.x - source.x, target.y - source.y) * .45));
    const anchored = (point, anchor) => { try {
        const value = { ...point };
        const name = String(anchor || 'Center').toLowerCase();
        if (name.includes('top')) value.y -= distance;
        else if (name.includes('bottom')) value.y += distance;
        else if (name === 'left') value.x -= distance;
        else if (name === 'right') value.x += distance;
        return value;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:anchored@1592', __javascriptError); throw __javascriptError; }};
    const defaults = {
        c1: anchored(source, connector.dataset.sourceAnchor),
        c2: anchored(target, connector.dataset.targetAnchor)
    };
    return {
        c1: {
            x: Number.isFinite(Number.parseFloat(connector.dataset.control1X)) ? number(connector.dataset.control1X) : defaults.c1.x,
            y: Number.isFinite(Number.parseFloat(connector.dataset.control1Y)) ? number(connector.dataset.control1Y) : defaults.c1.y
        },
        c2: {
            x: Number.isFinite(Number.parseFloat(connector.dataset.control2X)) ? number(connector.dataset.control2X) : defaults.c2.x,
            y: Number.isFinite(Number.parseFloat(connector.dataset.control2Y)) ? number(connector.dataset.control2Y) : defaults.c2.y
        }
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:connectorControls@1590', __javascriptError); throw __javascriptError; }}

function updateConnectorControlAppearance(connector, source, target, controls) { try {
    const guide = connector.querySelector('.connector-control-guide');
    if (guide) guide.setAttribute('d', `M ${source.x} ${source.y} L ${controls.c1.x} ${controls.c1.y} M ${target.x} ${target.y} L ${controls.c2.x} ${controls.c2.y}`);
    const first = connector.querySelector('[data-connector-control="1"]');
    const second = connector.querySelector('[data-connector-control="2"]');
    const route = connector.querySelector('[data-connector-control="route"]');
    if (first) { first.setAttribute('cx', controls.c1.x); first.setAttribute('cy', controls.c1.y); }
    if (second) { second.setAttribute('cx', controls.c2.x); second.setAttribute('cy', controls.c2.y); }
    if (route) {
        const x = (controls.c1.x + controls.c2.x) / 2;
        const y = (controls.c1.y + controls.c2.y) / 2;
        const width = number(route.getAttribute('width'), 2);
        const height = number(route.getAttribute('height'), 2);
        route.setAttribute('x', x - width / 2);
        route.setAttribute('y', y - height / 2);
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:updateConnectorControlAppearance@1617', __javascriptError); throw __javascriptError; }}

function updateAttachedConnectors(state, movedId) { try {
    for (const connector of state.page.querySelectorAll('[data-connector-id]')) {
        if (connector.dataset.sourceElementId !== movedId && connector.dataset.targetElementId !== movedId) continue;
        const source = connectorEndpointPoint(state, connector, 'source');
        const target = connectorEndpointPoint(state, connector, 'target');
        if (!source || !target) continue;
        const controls = connectorControls(connector, source, target);
        const path = connectorPath(connector.dataset.pathKind || 'Curved', source, target, controls);
        connector.querySelectorAll('.connector-line,.connector-hit').forEach(item => { try { return (item.setAttribute('d', path)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:connector.querySelectorAll(\'.connector-line,.connector-hit\').forEach@1643', __javascriptError); throw __javascriptError; } });
        const ends = connector.querySelectorAll('.connector-endpoint');
        if (ends[0]) { ends[0].setAttribute('cx', source.x); ends[0].setAttribute('cy', source.y); }
        if (ends[1]) { ends[1].setAttribute('cx', target.x); ends[1].setAttribute('cy', target.y); }
        updateConnectorControlAppearance(connector, source, target, controls);
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:updateAttachedConnectors@1635', __javascriptError); throw __javascriptError; }}

function mediaPointerTargetsControls(event) { try {
    const target = event.target?.closest?.('[data-media-control],video,audio');
    if (!target) return false;
    const rect = target.getBoundingClientRect?.();
    if (!rect || rect.height <= 0) return true;
    const tag = String(target.tagName || '').toLowerCase();
    const relativeY = event.clientY - rect.top;
    const controlBand = tag === 'audio' ? rect.height : Math.min(38, rect.height * .3);
    return relativeY >= rect.height - controlBand;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:mediaPointerTargetsControls@1651', __javascriptError); throw __javascriptError; }}

function selectableNodes(state) { try {
    return [...state.page.querySelectorAll('[data-publication-element][data-element-id]')];
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:selectableNodes@1662', __javascriptError); throw __javascriptError; }}

function selectionNodes(state) { try {
    return selectableNodes(state).filter(item => { try { return (!item.matches('[data-connector-id]')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:selectableNodes(state).filter@1667', __javascriptError); throw __javascriptError; } });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:selectionNodes@1666', __javascriptError); throw __javascriptError; }}

function selectedElementIdSet(state) { try {
    const ids = new Set(state.config?.selectedElementIds || []);
    for (const item of selectableNodes(state)) {
        if (item.dataset.selected === 'true' || item.classList.contains('selected'))
            ids.add(String(item.dataset.elementId || '').toLowerCase());
    }
    ids.delete('');
    return ids;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:selectedElementIdSet@1670', __javascriptError); throw __javascriptError; }}

function selectionVisualFrame(state) { try {
    return state.page?.querySelector?.(':scope > [data-selection-visual-frame]') || null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:selectionVisualFrame@1680', __javascriptError); throw __javascriptError; }}

function renderedSelectionTarget(element) { try {
    if (!element) return null;
    const content = element.querySelector(':scope > .publication-element-content');
    if (!content) return element;
    const kind = String(element.dataset.elementKind || '').toLowerCase();
    const selectors = {
        panel: '[data-panel-root]',
        htmlembed: '.publication-html-embed-shell',
        devextremecomponent: '.devextreme-publication-component,.devextreme-component-host',
        datavisual: '.data-visual-view,[data-visual-root]',
        livesource: '.live-source-view',
        video: '.publication-video-renderer,video',
        audio: '.publication-audio-frame,audio'
    };
    const selector = selectors[kind];
    return (selector ? content.querySelector(selector) : null) || content;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:renderedSelectionTarget@1684', __javascriptError); throw __javascriptError; }}

function finiteVisibleRect(node) { try {
    if (!node?.isConnected) return null;
    const style = getComputedStyle(node);
    if (style.display === 'none' || style.visibility === 'hidden') return null;
    const rect = node.getBoundingClientRect();
    if (![rect.left, rect.top, rect.right, rect.bottom, rect.width, rect.height].every(Number.isFinite)) return null;
    if (rect.width < .5 || rect.height < .5) return null;
    return rect;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:finiteVisibleRect@1702', __javascriptError); throw __javascriptError; }}

function renderedSelectionBounds(element) { try {
    const target = renderedSelectionTarget(element);
    const first = finiteVisibleRect(target) || finiteVisibleRect(element);
    if (!first) return null;

    // Responsive panels and embedded component runtimes can paint outside their root box.
    // Union only their major visible descendants, not popups or selection chrome.
    const kind = String(element.dataset.elementKind || '').toLowerCase();
    if (!['panel', 'htmlembed', 'devextremecomponent', 'datavisual', 'livesource'].includes(kind)) return first;
    let left = first.left, top = first.top, right = first.right, bottom = first.bottom;
    const candidates = target?.querySelectorAll?.(
        ':scope > *,[data-panel-view]:not([hidden]),[data-panel-element],.dx-widget,.dashboard-card,.publication-html-embed-content'
    ) || [];
    let inspected = 0;
    for (const node of candidates) {
        if (inspected++ > 160 || node.closest?.('.dx-overlay-wrapper,.dx-popup-wrapper,[data-selection-visual-frame]')) continue;
        const rect = finiteVisibleRect(node);
        if (!rect) continue;
        left = Math.min(left, rect.left);
        top = Math.min(top, rect.top);
        right = Math.max(right, rect.right);
        bottom = Math.max(bottom, rect.bottom);
    }
    return { left, top, right, bottom, width: right - left, height: bottom - top };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:renderedSelectionBounds@1712', __javascriptError); throw __javascriptError; }}

function updateSelectionVisualFrame(state) { try {
    state.selectionFramePending = false;
    const frame = selectionVisualFrame(state);
    if (!frame || !state.page?.isConnected) return;
    const selected = [...state.page.querySelectorAll('[data-publication-element].selected')]
        .filter(item => { try { return (!item.matches('[data-connector-id]')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...state.page.querySelectorAll(\'[data-publication-element].selected\')@1743', __javascriptError); throw __javascriptError; } });
    const primary = selected.find(item => { try { return (item.classList.contains('selection-primary')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:selected.find@1744', __javascriptError); throw __javascriptError; } }) || selected.at(-1) || null;
    const rotation = Math.abs(number(primary?.dataset?.elementRotation));
    if (!primary || selected.length !== 1 || rotation > .01 || state.config?.cropMode || state.config?.contentPanMode) {
        frame.hidden = true;
        frame.removeAttribute('data-element-id');
        state.page.classList.remove('selection-visual-active');
        return;
    }

    const bounds = renderedSelectionBounds(primary);
    const pageRect = state.page.getBoundingClientRect();
    if (!bounds || pageRect.width < .5 || pageRect.height < .5) {
        frame.hidden = true;
        state.page.classList.remove('selection-visual-active');
        return;
    }
    const scaleX = state.page.clientWidth / pageRect.width;
    const scaleY = state.page.clientHeight / pageRect.height;
    const left = (bounds.left - pageRect.left) * scaleX;
    const top = (bounds.top - pageRect.top) * scaleY;
    const width = bounds.width * scaleX;
    const height = bounds.height * scaleY;
    if (![left, top, width, height].every(Number.isFinite) || width < 1 || height < 1) {
        frame.hidden = true;
        state.page.classList.remove('selection-visual-active');
        return;
    }

    frame.style.left = `${left}px`;
    frame.style.top = `${top}px`;
    frame.style.width = `${width}px`;
    frame.style.height = `${height}px`;
    frame.dataset.elementId = primary.dataset.elementId || '';
    frame.classList.toggle('locked', primary.classList.contains('locked'));
    frame.hidden = false;
    state.page.classList.add('selection-visual-active');
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:updateSelectionVisualFrame@1738', __javascriptError); throw __javascriptError; }}

function scheduleSelectionVisualFrame(state) { try {
    if (!state || state.selectionFramePending) return;
    state.selectionFramePending = true;
    requestAnimationFrame(() => { try { return (updateSelectionVisualFrame(state)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:requestAnimationFrame@1785', __javascriptError); throw __javascriptError; } });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:scheduleSelectionVisualFrame@1782', __javascriptError); throw __javascriptError; }}

function synchronizeSelectionDom(state, ids, primaryId = null) { try {
    const normalized = new Set([...ids].map(value => { try { return (String(value || '').toLowerCase()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...ids].map@1789', __javascriptError); throw __javascriptError; } }).filter(Boolean));
    const primary = String(primaryId || '').toLowerCase();
    const previousPrimary = String(state.page?.querySelector?.('[data-publication-element].selection-primary')?.dataset?.elementId || '').toLowerCase();
    if (state.config?.contentPanMode && (normalized.size !== 1 || previousPrimary !== primary)) {
        state.config.contentPanMode = false;
        state.page?.classList?.remove?.('content-pan-mode');
    }
    state.config.selectedElementIds = normalized;
    for (const item of selectableNodes(state)) {
        const id = String(item.dataset.elementId || '').toLowerCase();
        const selected = normalized.has(id);
        const contentPanTarget = Boolean(state.config?.contentPanMode && selected && id === primary);
        item.dataset.selected = selected ? 'true' : 'false';
        item.classList.toggle('selected', selected);
        item.classList.toggle('selection-primary', selected && id === primary);
        item.classList.toggle('content-pan-target', contentPanTarget);
    }
    scheduleSelectionVisualFrame(state);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:synchronizeSelectionDom@1788', __javascriptError); throw __javascriptError; }}

function createSelectionMarquee(state) { try {
    const overlay = document.createElement('div');
    overlay.className = 'publisher-selection-marquee';
    overlay.setAttribute('aria-hidden', 'true');
    state.page.appendChild(overlay);
    return overlay;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:createSelectionMarquee@1809', __javascriptError); throw __javascriptError; }}

function marqueeViewportRect(operation, event, pageRect) { try {
    const startX = clamp(operation.startClientX, pageRect.left, pageRect.right);
    const startY = clamp(operation.startClientY, pageRect.top, pageRect.bottom);
    const endX = clamp(event.clientX, pageRect.left, pageRect.right);
    const endY = clamp(event.clientY, pageRect.top, pageRect.bottom);
    return {
        left: Math.min(startX, endX),
        top: Math.min(startY, endY),
        right: Math.max(startX, endX),
        bottom: Math.max(startY, endY)
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:marqueeViewportRect@1817', __javascriptError); throw __javascriptError; }}

function marqueeSelectionIds(state, viewportRect, initialSelection, additive) { try {
    const hits = [];
    for (const item of selectionNodes(state)) {
        const rect = item.getBoundingClientRect();
        const intersects = rect.right > viewportRect.left
            && rect.left < viewportRect.right
            && rect.bottom > viewportRect.top
            && rect.top < viewportRect.bottom;
        if (intersects) hits.push(item);
    }

    const expanded = new Set(additive ? initialSelection : []);
    for (const item of hits) {
        for (const member of selectionUnitNodes(state, item)) {
            const id = String(member.dataset.elementId || '').toLowerCase();
            if (id) expanded.add(id);
        }
    }
    return expanded;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:marqueeSelectionIds@1830', __javascriptError); throw __javascriptError; }}

function updateSelectionMarquee(state, operation, event) { try {
    const pageRect = state.page.getBoundingClientRect();
    const viewportRect = marqueeViewportRect(operation, event, pageRect);
    const left = viewportRect.left - pageRect.left;
    const top = viewportRect.top - pageRect.top;
    operation.overlay.style.left = `${left}px`;
    operation.overlay.style.top = `${top}px`;
    operation.overlay.style.width = `${viewportRect.right - viewportRect.left}px`;
    operation.overlay.style.height = `${viewportRect.bottom - viewportRect.top}px`;
    operation.currentSelection = marqueeSelectionIds(
        state,
        viewportRect,
        operation.initialSelection,
        operation.additive);
    const primary = [...operation.currentSelection].at(-1) || null;
    synchronizeSelectionDom(state, operation.currentSelection, primary);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:updateSelectionMarquee@1851', __javascriptError); throw __javascriptError; }}

function selectionUnitNodes(state, element) { try {
    const groupId = String(element.dataset.groupId || '').trim();
    if (!groupId) return [element];
    return selectionNodes(state).filter(item => { try { return (item.dataset.groupId === groupId); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:selectionNodes(state).filter@1872', __javascriptError); throw __javascriptError; } });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:selectionUnitNodes@1869', __javascriptError); throw __javascriptError; }}

function optimisticSelectElement(state, element, additive) { try {
    const unitIds = selectionUnitNodes(state, element)
        .map(item => { try { return (String(item.dataset.elementId || '').toLowerCase()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:selectionUnitNodes(state, element) .map@1877', __javascriptError); throw __javascriptError; } })
        .filter(Boolean);
    let ids = selectedElementIdSet(state);
    if (additive) {
        const remove = unitIds.length > 0 && unitIds.every(id => { try { return (ids.has(id)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:unitIds.every@1881', __javascriptError); throw __javascriptError; } });
        for (const id of unitIds) remove ? ids.delete(id) : ids.add(id);
    } else {
        ids = new Set(unitIds);
    }
    synchronizeSelectionDom(state, ids, element.dataset.elementId);
    return ids;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:optimisticSelectElement@1875', __javascriptError); throw __javascriptError; }}

function movingNodesForPointer(state, element, additive, wasSelected) { try {
    const selectedIds = selectedElementIdSet(state);
    const selected = selectionNodes(state).filter(item => { try { return (selectedIds.has(String(item.dataset.elementId || '').toLowerCase())); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:selectionNodes(state).filter@1892', __javascriptError); throw __javascriptError; } });
    const unit = selectionUnitNodes(state, element);
    if (!additive) return wasSelected && selected.length ? selected : unit;
    if (wasSelected) return selected.length ? selected : unit;
    const result = [...selected];
    for (const item of unit) if (!result.includes(item)) result.push(item);
    return result;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:movingNodesForPointer@1890', __javascriptError); throw __javascriptError; }}

function movingBounds(items) { try {
    if (!items.length) return { x: 0, y: 0, width: 0, height: 0 };
    const left = Math.min(...items.map(item => { try { return (item.x); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:items.map@1903', __javascriptError); throw __javascriptError; } }));
    const top = Math.min(...items.map(item => { try { return (item.y); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:items.map@1904', __javascriptError); throw __javascriptError; } }));
    const right = Math.max(...items.map(item => { try { return (item.x + item.width); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:items.map@1905', __javascriptError); throw __javascriptError; } }));
    const bottom = Math.max(...items.map(item => { try { return (item.y + item.height); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:items.map@1906', __javascriptError); throw __javascriptError; } }));
    return { x: left, y: top, width: right - left, height: bottom - top };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:movingBounds@1901', __javascriptError); throw __javascriptError; }}

function refreshMovingElements(state, operation) { try {
    if (!operation?.moving) return;
    for (const item of operation.moving) {
        const current = state.page.querySelector(`[data-element-id="${CSS.escape(item.id)}"]`);
        if (current) item.element = current;
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:refreshMovingElements@1910', __javascriptError); throw __javascriptError; }}

function cancelPendingComponentAction(state) { try {
    const pending = state?.pendingComponentAction;
    if (!pending) return;
    if (pending.timer) clearTimeout(pending.timer);
    try { pending.popup?.close?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@1922', __caughtJavaScriptError);  }
    state.pendingComponentAction = null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:cancelPendingComponentAction@1918', __javascriptError); throw __javascriptError; }}

function scheduleComponentNavigation(state, detail) { try {
    if (detail?.editorSurface === false) return;
    cancelPendingComponentAction(state);
    const target = detail?.pageId;
    state.pendingComponentAction = {
        timer: setTimeout(() => { try {
            state.pendingComponentAction = null;
            safeDotNet(state, 'NavigateToPage', String(target ?? ''));
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:setTimeout@1931', __javascriptError); throw __javascriptError; }}, 420)
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:scheduleComponentNavigation@1926', __javascriptError); throw __javascriptError; }}

function scheduleComponentUrl(state, detail) { try {
    if (detail?.editorSurface === false) return;
    cancelPendingComponentAction(state);
    const url = String(detail?.url || '').trim();
    if (!/^(https?:|mailto:)/i.test(url)) return;
    const newWindow = detail?.openInNewWindow !== false;
    let popup = null;
    if (newWindow) {
        try { popup = window.open('about:blank', '_blank', 'noopener'); } catch { popup = null; }
    }
    state.pendingComponentAction = {
        popup,
        timer: setTimeout(() => { try {
            state.pendingComponentAction = null;
            if (newWindow && popup) { try { popup.location.href = url; } catch { window.open(url, '_blank', 'noopener'); } }
            else if (newWindow) window.open(url, '_blank', 'noopener');
            else location.href = url;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:setTimeout@1950', __javascriptError); throw __javascriptError; }}, 420)
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:scheduleComponentUrl@1938', __javascriptError); throw __javascriptError; }}

function registerCanvasClick(state, operation, event) { try {
    if (!operation?.id || operation.kind === 'resize' || operation.kind?.startsWith('connector-')) return;
    const now = performance.now();
    const previous = state.lastCanvasClick;
    const sameElement = previous?.id === operation.id;
    const closeInTime = previous && now - previous.time <= 520;
    const closeInSpace = previous && Math.hypot(event.clientX - previous.x, event.clientY - previous.y) <= 10;
    if (sameElement && closeInTime && closeInSpace) {
        cancelPendingComponentAction(state);
        state.lastCanvasClick = null;
        resetPointerOperation(state, false);
        safeDotNet(state, 'ActivateElement', operation.id);
        event.preventDefault();
        event.stopPropagation();
        return;
    }
    state.lastCanvasClick = { id: operation.id, time: now, x: event.clientX, y: event.clientY };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:registerCanvasClick@1959', __javascriptError); throw __javascriptError; }}

function designerInteractionOwner(state, event) { try {
    const target = event?.target instanceof Element ? event.target : null;
    const owner = target?.closest?.('[data-publication-element][data-element-id]');
    return owner && state.page.contains(owner) ? owner : null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:designerInteractionOwner@1978', __javascriptError); throw __javascriptError; }}

function componentInteractionOwner(state, event) { try {
    const target = event?.target instanceof Element ? event.target : null;
    if (!target?.closest?.('.devextreme-component-host')) return null;
    return designerInteractionOwner(state, event);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:componentInteractionOwner@1984', __javascriptError); throw __javascriptError; }}

function mapComponentHost(owner) { try {
    const host = owner?.querySelector?.('.devextreme-component-host[data-ps-component-kind]');
    const kind = String(host?.dataset?.psComponentKind || '').replace(/[^a-z0-9]/gi, '').toLowerCase();
    return ['map', 'vectormap'].includes(kind) ? host : null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:mapComponentHost@1990', __javascriptError); throw __javascriptError; }}

function componentMapContentInteractionActive(state, owner) { try {
    if (!state?.config?.contentPanMode || !owner) return false;
    const id = String(owner.dataset?.elementId || '').toLowerCase();
    const selected = id && selectedElementIdSet(state).has(id);
    const host = mapComponentHost(owner);
    return Boolean(selected && host && String(host.dataset.psDesignerInteraction || '').toLowerCase() === 'content');
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:componentMapContentInteractionActive@1996', __javascriptError); throw __javascriptError; }}

function commitMapViewportEvent(state, event) { try {
    const owner = designerInteractionOwner(state, event);
    if (!componentMapContentInteractionActive(state, owner)) return;
    const detail = event?.detail || {};
    const componentId = String(detail.componentId || owner?.dataset?.elementId || '');
    if (!componentId || componentId.toLowerCase() !== String(owner.dataset.elementId || '').toLowerCase()) return;
    const longitude = Number(detail.longitude);
    const latitude = Number(detail.latitude);
    const zoom = Number(detail.zoom);
    if (![longitude, latitude, zoom].every(Number.isFinite)) return;
    safeDotNet(state, 'CommitMapViewport', componentId, longitude, latitude, zoom);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:commitMapViewportEvent@2004', __javascriptError); throw __javascriptError; }}

function componentDoubleClick(state, event) { try {
    const owner = designerInteractionOwner(state, event);
    if (!owner || componentMapContentInteractionActive(state, owner)) return;
    cancelPendingComponentAction(state);
    state.lastCanvasClick = null;
    safeDotNet(state, 'ActivateElement', String(owner.dataset.elementId || ''));
    event.preventDefault();
    event.stopImmediatePropagation();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:componentDoubleClick@2017', __javascriptError); throw __javascriptError; }}

function designerContextMenu(state, event) { try {
    const owner = designerInteractionOwner(state, event);
    if (!owner) return;
    const id = String(owner.dataset.elementId || '');
    if (!id) return;

    cancelPendingComponentAction(state);
    state.lastCanvasClick = null;
    event.preventDefault();
    event.stopImmediatePropagation();
    safeDotNet(state, 'OpenElementContextMenu', id, number(event.clientX), number(event.clientY), number(event.pageX), number(event.pageY), number(event.screenX), number(event.screenY));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:designerContextMenu@2027', __javascriptError); throw __javascriptError; }}

function pointerDown(state, event) { try {
    if (event.button !== 0 || event.target.closest('.ruler-canvas,.corner-ruler')) return;
    state.keyboardActive = true;
    try { clearPublicationPreview(state.page?.id || state.page); }
    catch (error) { console.warn('Publisher animation preview cleanup failed.', error); }
    if (state.operation) resetPointerOperation(state, true);
    state.stage.focus({ preventScroll: true });

    const insertionRect = state.page.getBoundingClientRect();
    if (event.clientX >= insertionRect.left && event.clientX <= insertionRect.right &&
        event.clientY >= insertionRect.top && event.clientY <= insertionRect.bottom) {
        const insertionX = clamp((event.clientX - insertionRect.left) / state.config.pxPerMm, 0, number(state.page.dataset.pageWidthMm));
        const insertionY = clamp((event.clientY - insertionRect.top) / state.config.pxPerMm, 0, number(state.page.dataset.pageHeightMm));
        state.lastInsertionPoint = { x: insertionX, y: insertionY };
        safeDotNet(state, 'SetInsertionPoint', insertionX, insertionY);
    }

    if (mediaPointerTargetsControls(event)) {
        const owner = event.target.closest('[data-publication-element][data-element-id]');
        if (owner && state.page.contains(owner)) {
            const id = String(owner.dataset.elementId || '');
            if (id && !selectedElementIdSet(state).has(id.toLowerCase())) {
                optimisticSelectElement(state, owner, false);
                safeDotNet(state, 'SelectElement', id, false);
            }
        }
        return;
    }

    const componentOwner = componentInteractionOwner(state, event);
    if (componentOwner && !connectorToolActive(state)) {
        if (componentMapContentInteractionActive(state, componentOwner)) {
            state.lastCanvasClick = null;
            cancelPendingComponentAction(state);
            return;
        }
        const id = String(componentOwner.dataset.elementId || '');
        const wasSelected = selectedElementIdSet(state).has(id.toLowerCase());
        if (id && !wasSelected) {
            // Keep selection optimistic while the pointer is down. Updating Blazor here
            // recreates the DevExtreme host before the drag threshold is reached and
            // makes the component look resize-only. Commit the selection on pointerup,
            // or let CommitMove select the object after an actual drag.
            optimisticSelectElement(state, componentOwner, false);
        }
        if (id && !componentOwner.classList.contains('locked')) {
            const bounds = elementMm(componentOwner, state.config.pxPerMm);
            const moving = movingNodesForPointer(state, componentOwner, false, wasSelected)
                .filter(item => { try { return (!item.classList.contains('locked') && !item.matches('[data-connector-id]')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:movingNodesForPointer(state, componentOwner, false, wasSelected) .filt@2088', __javascriptError); throw __javascriptError; } })
                .map(item => { try { return (({ id: item.dataset.elementId, element: item, ...elementMm(item, state.config.pxPerMm) })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:movingNodesForPointer(state, componentOwner, false, wasSelected) .filt@2089', __javascriptError); throw __javascriptError; } });
            if (!moving.some(item => { try { return (item.id === id); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:moving.some@2090', __javascriptError); throw __javascriptError; } })) moving.unshift({ id, element: componentOwner, ...bounds });
            const groupBounds = movingBounds(moving);
            state.operation = {
                kind: 'component-pending', pointerId: event.pointerId, id, element: componentOwner,
                startX: event.clientX, startY: event.clientY, moved: false, wasSelected, additive: false, pendingToggle: false,
                selectionCommitPending: !wasSelected,
                moving, movingIds: new Set(moving.map(item => { try { return (item.id); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:moving.map@2096', __javascriptError); throw __javascriptError; } })), movingBounds: groupBounds,
                x: bounds.x, y: bounds.y, width: bounds.width, height: bounds.height
            };
        }
        return;
    }

    const connectorControl = event.target.closest('[data-connector-control]');
    if (connectorControl && state.page.contains(connectorControl)) {
        const connector = connectorControl.closest('[data-connector-id]');
        if (!connector || connector.classList.contains('locked')) return;
        const source = connectorEndpointPoint(state, connector, 'source');
        const target = connectorEndpointPoint(state, connector, 'target');
        if (!source || !target) return;
        const controls = connectorControls(connector, source, target);
        state.operation = {
            kind: 'connector-control', pointerId: event.pointerId, connector,
            connectorId: connector.dataset.connectorId, control: connectorControl.dataset.connectorControl,
            startPoint: pagePointMm(state, event), source, target,
            originalControls: { c1: { ...controls.c1 }, c2: { ...controls.c2 } },
            currentControls: { c1: { ...controls.c1 }, c2: { ...controls.c2 } }
        };
        try { state.stage.setPointerCapture(event.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@2118', __caughtJavaScriptError);  }
        event.preventDefault();
        event.stopPropagation();
        return;
    }

    const endpoint = event.target.closest('[data-connector-end]');
    if (endpoint && state.page.contains(endpoint)) {
        const connector = endpoint.closest('[data-connector-id]');
        if (!connector || connector.classList.contains('locked')) return;
        const endpointName = endpoint.dataset.connectorEnd;
        const otherPrefix = endpointName === 'source' ? 'target' : 'source';
        const otherId = connector.dataset[`${otherPrefix}ElementId`] || '';
        const fixedPoint = connectorEndpointPoint(state, connector, otherPrefix);
        if (!fixedPoint) return;
        const signal = connector.dataset.signalEnabled === 'true';
        connector.style.visibility = 'hidden';
        state.operation = {
            kind: 'connector-reconnect', pointerId: event.pointerId, connector, connectorId: connector.dataset.connectorId,
            endpoint: endpointName, fixedPoint,
            pathKind: connector.dataset.pathKind || 'Curved', markerEnd: endpointName !== 'source', excludedIds: otherId ? [otherId] : [], signal
        };
        try { state.stage.setPointerCapture(event.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@2140', __caughtJavaScriptError);  }
        event.preventDefault();
        event.stopPropagation();
        return;
    }

    const connectorBody = event.target.closest('[data-connector-id]');
    if (connectorBody && state.page.contains(connectorBody) &&
        !connectorBody.classList.contains('locked') &&
        (connectorBody.classList.contains('selected') || connectorBody.classList.contains('selection-primary')) &&
        event.target.closest('.connector-line,.connector-hit')) {
        const source = connectorEndpointPoint(state, connectorBody, 'source');
        const target = connectorEndpointPoint(state, connectorBody, 'target');
        if (source && target) {
            const controls = connectorControls(connectorBody, source, target);
            state.operation = {
                kind: 'connector-control', pointerId: event.pointerId, connector: connectorBody,
                connectorId: connectorBody.dataset.connectorId, control: 'route',
                startPoint: pagePointMm(state, event), source, target,
                originalControls: { c1: { ...controls.c1 }, c2: { ...controls.c2 } },
                currentControls: { c1: { ...controls.c1 }, c2: { ...controls.c2 } }
            };
            try { state.stage.setPointerCapture(event.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@2162', __caughtJavaScriptError);  }
            event.preventDefault();
            event.stopPropagation();
            return;
        }
    }

    const connectorPort = event.target.closest('[data-connector-port]');
    if (connectorPort && state.page.contains(connectorPort) && connectorToolActive(state)) {
        state.lastCanvasClick = null;
        const sourceOwnerId = connectorPort.dataset.ownerId;
        connectorPort.classList.add('connector-port-source');
        const sourcePoint = portPointMm(state, connectorPort);
        state.operation = {
            kind: 'connector-new', pointerId: event.pointerId, sourcePort: connectorPort,
            sourceOwnerId, sourceAnchor: connectorPort.dataset.anchor, fixedPoint: sourcePoint,
            sourceEndpoint: {
                kind: 'Element', ownerId: sourceOwnerId, anchor: connectorPort.dataset.anchor || 'Center',
                portId: connectorPort.dataset.portId || '', createPort: false,
                relativeX: number(connectorPort.style.left) / 100 || .5, relativeY: number(connectorPort.style.top) / 100 || .5,
                point: sourcePoint
            },
            pathKind: 'Curved', markerEnd: state.config.connectorTool === 'Arrow' || state.config.connectorTool === 'SignalArrow', tool: state.config.connectorTool,
            signal: signalConnectorToolActive(state), excludedIds: [sourceOwnerId]
        };
        try { state.stage.setPointerCapture(event.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@2187', __caughtJavaScriptError);  }
        event.preventDefault();
        event.stopPropagation();
        return;
    }

    const guide = event.target.closest('[data-guide-id]');
    if (guide && state.page.contains(guide)) {
        const orientation = guide.dataset.guideOrientation;
        const position = orientation === 'Horizontal'
            ? number(guide.style.top) / state.config.pxPerMm
            : number(guide.style.left) / state.config.pxPerMm;
        state.operation = {
            kind: 'guide',
            guide,
            id: guide.dataset.guideId,
            orientation,
            position,
            pointerId: event.pointerId
        };
        guide.setPointerCapture(event.pointerId);
        event.preventDefault();
        return;
    }

    const visualHandle = event.target.closest('[data-selection-visual-frame] [data-resize-handle]');
    const visualElementId = visualHandle?.closest('[data-selection-visual-frame]')?.dataset?.elementId || '';
    const element = visualElementId
        ? state.page.querySelector(`[data-publication-element][data-element-id="${CSS.escape(visualElementId)}"]`)
        : event.target.closest('[data-publication-element]');
    if ((!element || !state.page.contains(element)) && state.page.contains(event.target) && signalConnectorToolActive(state)) {
        state.lastCanvasClick = null;
        const sourcePoint = pagePointMm(state, event);
        state.operation = {
            kind: 'connector-new', pointerId: event.pointerId,
            sourceOwnerId: '', sourceAnchor: 'Center', fixedPoint: sourcePoint,
            sourceEndpoint: { kind: 'Canvas', ownerId: '', anchor: 'Center', portId: '', createPort: false, relativeX: .5, relativeY: .5, point: sourcePoint },
            pathKind: 'Curved', markerEnd: state.config.connectorTool === 'SignalArrow', tool: state.config.connectorTool,
            signal: true, excludedIds: []
        };
        try { state.stage.setPointerCapture(event.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@2227', __caughtJavaScriptError);  }
        event.preventDefault();
        event.stopPropagation();
        return;
    }
    if (!element || !state.page.contains(element)) {
        if (state.page.contains(event.target) && !connectorToolActive(state)) {
            state.lastCanvasClick = null;
            const additive = Boolean(event.ctrlKey || event.metaKey || event.shiftKey);
            const initialSelection = selectedElementIdSet(state);
            const initialPrimary = [...initialSelection].at(-1) || null;
            if (!additive) synchronizeSelectionDom(state, new Set(), null);
            state.operation = {
                kind: 'marquee',
                pointerId: event.pointerId,
                startClientX: event.clientX,
                startClientY: event.clientY,
                moved: false,
                additive,
                initialSelection,
                initialPrimaryId: initialPrimary,
                currentSelection: additive ? new Set(initialSelection) : new Set(),
                overlay: createSelectionMarquee(state)
            };
            try { state.stage.setPointerCapture(event.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@2251', __caughtJavaScriptError);  }
            event.preventDefault();
        } else if (state.scroll.contains(event.target)) {
            state.lastCanvasClick = null;
            if (!connectorToolActive(state)) {
                synchronizeSelectionDom(state, new Set(), null);
                safeDotNet(state, 'ClearSelectionFromCanvas');
            }
            event.preventDefault();
        }
        return;
    }

    const id = element.dataset.elementId;
    const wasSelected = selectedElementIdSet(state).has(String(id || '').toLowerCase());
    const additive = Boolean(event.ctrlKey || event.metaKey || event.shiftKey);
    const activeConnectorTool = connectorToolActive(state);
    if (!activeConnectorTool && isDesignerComponentControlTarget(event.target, element)) {
        state.lastCanvasClick = null;
        // A selected component's native controls own the full pointer sequence. This
        // avoids the canvas taking pointer capture from Gallery navigation buttons
        // (and other DevExtreme controls), which could otherwise combine one click
        // with a canvas drag/swipe and advance more than one item. The first click on
        // an unselected object remains selection-only, as in other design tools.
        if (!wasSelected || additive) {
            optimisticSelectElement(state, element, additive);
            safeDotNet(state, 'SelectElement', id, additive);
            event.preventDefault();
            event.stopPropagation();
        }
        return;
    }
    if (!activeConnectorTool && componentMapContentInteractionActive(state, element)) {
        state.lastCanvasClick = null;
        event.preventDefault();
        return;
    }
    if (activeConnectorTool) {
        // Dropping directly on an object creates a persistent custom attachment point.
        const relative = relativePointForElement(state, element, event);
        const sourcePoint = pointForElementRelative(element, relative, state.config.pxPerMm);
        const sourcePreview = createConnectorPortPreview(element, relative, 'connector-port-source');
        state.operation = {
            kind: 'connector-new', pointerId: event.pointerId, sourcePreview,
            sourceOwnerId: id, sourceAnchor: 'Center', fixedPoint: sourcePoint,
            sourceEndpoint: {
                kind: 'Element', ownerId: id, anchor: 'Center', portId: '', createPort: true,
                relativeX: relative.x, relativeY: relative.y, point: sourcePoint
            },
            pathKind: 'Curved', markerEnd: state.config.connectorTool === 'Arrow' || state.config.connectorTool === 'SignalArrow',
            tool: state.config.connectorTool, signal: signalConnectorToolActive(state), excludedIds: [id]
        };
        try { state.stage.setPointerCapture(event.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@2303', __caughtJavaScriptError);  }
        event.preventDefault();
        event.stopPropagation();
        return;
    }
    const pendingToggle = additive && wasSelected;
    if (!pendingToggle && (!wasSelected || additive)) {
        optimisticSelectElement(state, element, additive);
        safeDotNet(state, 'SelectElement', id, additive);
    }
    if (element.classList.contains('locked')) return;
    if (element.matches('[data-connector-id]')) return;

    const handle = visualHandle || event.target.closest('[data-resize-handle]');
    const image = element.querySelector('img');
    const bounds = elementMm(element, state.config.pxPerMm);
    const moving = movingNodesForPointer(state, element, additive, wasSelected)
        .filter(item => { try { return (!item.classList.contains('locked') && !item.matches('[data-connector-id]')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:movingNodesForPointer(state, element, additive, wasSelected) .filter@2320', __javascriptError); throw __javascriptError; } })
        .map(item => { try { return (({ id: item.dataset.elementId, element: item, ...elementMm(item, state.config.pxPerMm) })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:movingNodesForPointer(state, element, additive, wasSelected) .filter(i@2321', __javascriptError); throw __javascriptError; } });
    if (!moving.some(item => { try { return (item.id === id); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:moving.some@2322', __javascriptError); throw __javascriptError; } })) moving.unshift({ id, element, ...bounds });
    const groupBounds = movingBounds(moving);
    const pageRect = state.page.getBoundingClientRect();
    const pointerX = (event.clientX - pageRect.left) / state.config.pxPerMm;
    const pointerY = (event.clientY - pageRect.top) / state.config.pxPerMm;
    const base = {
        id,
        element,
        pointerId: event.pointerId,
        startX: event.clientX,
        startY: event.clientY,
        moved: false,
        wasSelected,
        additive,
        pendingToggle,
        moving,
        movingIds: new Set(moving.map(item => { try { return (item.id); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:moving.map@2338', __javascriptError); throw __javascriptError; } })),
        movingBounds: groupBounds,
        grabGroupX: groupBounds.width > 0 ? clamp((pointerX - groupBounds.x) / groupBounds.width, 0, 1) : .5,
        grabGroupY: groupBounds.height > 0 ? clamp((pointerY - groupBounds.y) / groupBounds.height, 0, 1) : .5,
        ...bounds
    };

    const contentViewport = element.querySelector('[data-content-viewport]');
    const contentSource = contentViewport?.querySelector(':scope > [data-content-fit-source]');
    if (state.config.contentPanMode && contentViewport && contentSource && !handle) {
        state.operation = {
            ...base, kind: 'content-pan', moving: [{ id, element, ...bounds }], movingIds: new Set([id]),
            viewport: contentViewport, source: contentSource,
            contentOffsetX: number(contentViewport.dataset.contentOffsetX),
            contentOffsetY: number(contentViewport.dataset.contentOffsetY),
            contentScale: number(contentViewport.dataset.contentScale, 1)
        };
    } else if (state.config.cropMode && image && !handle) {
        state.operation = {
            ...base,
            kind: 'crop',
            moving: [{ id, element, ...bounds }],
            movingIds: new Set([id]),
            image,
            cropX: number(image.dataset.cropX),
            cropY: number(image.dataset.cropY),
            cropScale: number(image.dataset.cropScale, 1),
            imageRotation: number(image.dataset.imageRotation),
            flipX: number(image.dataset.flipX, 1),
            flipY: number(image.dataset.flipY, 1)
        };
    } else if (handle) {
        state.operation = { ...base, kind: 'resize', moving: [{ id, element, ...bounds }], movingIds: new Set([id]), handle: handle.dataset.resizeHandle, rotation: parseRotation(element), centerX: bounds.x + bounds.width / 2, centerY: bounds.y + bounds.height / 2 };
    } else {
        state.operation = { ...base, kind: 'move' };
    }

    try { state.stage.setPointerCapture(event.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@2375', __caughtJavaScriptError);  }
    if (handle || state.config.cropMode || state.config.contentPanMode) event.preventDefault();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pointerDown@2040', __javascriptError); throw __javascriptError; }}

function refreshOperationElement(state, operation) { try {
    if (!operation?.id) return operation?.element || null;
    const current = state.page.querySelector(`[data-element-id="${CSS.escape(operation.id)}"]`);
    if (current) {
        operation.element = current;
        if (operation.kind === 'crop') operation.image = current.querySelector('img') || operation.image;
        if (operation.kind === 'content-pan') { operation.viewport = current.querySelector('[data-content-viewport]') || operation.viewport; operation.source = operation.viewport?.querySelector(':scope > [data-content-fit-source]') || operation.source; }
    }
    return operation.element;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:refreshOperationElement@2379', __javascriptError); throw __javascriptError; }}

function pointerMove(state, event) { try {
    const stageRect = state.stage.getBoundingClientRect();
    state.cursorX = event.clientX - stageRect.left;
    state.cursorY = event.clientY - stageRect.top;
    nextAnimationFrame(state);

    const operation = state.operation;
    if (!operation || operation.pointerId !== event.pointerId) return;
    if (event.pointerType === 'mouse' && (event.buttons & 1) === 0) {
        pointerUp(state, event);
        return;
    }

    if (operation.kind === 'marquee') {
        const movementPixels = Math.hypot(event.clientX - operation.startClientX, event.clientY - operation.startClientY);
        if (!operation.moved && movementPixels < 2) return;
        operation.moved = true;
        updateSelectionMarquee(state, operation, event);
        event.preventDefault();
        return;
    }

    if (operation.kind === 'connector-new' || operation.kind === 'connector-reconnect') {
        state.lastCanvasClick = null;
        updateConnectorDrag(state, event, operation);
        event.preventDefault();
        return;
    }

    if (operation.kind === 'connector-control') {
        const point = pagePointMm(state, event);
        const controls = {
            c1: { ...operation.originalControls.c1 },
            c2: { ...operation.originalControls.c2 }
        };
        if (operation.control === 'route') {
            const dx = point.x - operation.startPoint.x;
            const dy = point.y - operation.startPoint.y;
            controls.c1.x += dx; controls.c1.y += dy;
            controls.c2.x += dx; controls.c2.y += dy;
        } else if (operation.control === '1') controls.c1 = point;
        else controls.c2 = point;
        controls.c1.x = clamp(controls.c1.x, 0, number(state.page.dataset.pageWidthMm));
        controls.c1.y = clamp(controls.c1.y, 0, number(state.page.dataset.pageHeightMm));
        controls.c2.x = clamp(controls.c2.x, 0, number(state.page.dataset.pageWidthMm));
        controls.c2.y = clamp(controls.c2.y, 0, number(state.page.dataset.pageHeightMm));
        operation.currentControls = controls;
        operation.connector.dataset.pathKind = 'Curved';
        operation.connector.dataset.control1X = String(controls.c1.x);
        operation.connector.dataset.control1Y = String(controls.c1.y);
        operation.connector.dataset.control2X = String(controls.c2.x);
        operation.connector.dataset.control2Y = String(controls.c2.y);
        const path = connectorPath('Curved', operation.source, operation.target, controls);
        operation.connector.querySelectorAll('.connector-line,.connector-hit').forEach(item => { try { return (item.setAttribute('d', path)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:operation.connector.querySelectorAll(\'.connector-line,.connector-hit\')@2443', __javascriptError); throw __javascriptError; } });
        updateConnectorControlAppearance(operation.connector, operation.source, operation.target, controls);
        event.preventDefault();
        return;
    }

    if (operation.kind === 'guide') {
        const position = guidePositionFromPointer(state, operation.orientation, event);
        operation.currentPosition = position;
        if (operation.orientation === 'Horizontal') operation.guide.style.top = `${position * state.config.pxPerMm}px`;
        else operation.guide.style.left = `${position * state.config.pxPerMm}px`;
        event.preventDefault();
        return;
    }

    const movementPixels = Math.hypot(event.clientX - operation.startX, event.clientY - operation.startY);
    if (operation.kind === 'component-pending') {
        if (movementPixels < 5) return;
        operation.kind = 'move';
        operation.moved = true;
        state.lastCanvasClick = null;
        cancelPendingComponentAction(state);
        state.suppressNextComponentClickUntil = performance.now() + 350;
        try { state.stage.setPointerCapture(event.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@2466', __caughtJavaScriptError);  }
    }
    if (!operation.moved && movementPixels < (operation.kind === 'resize' ? 1.5 : 3)) return;
    operation.moved = true;
    const dx = (event.clientX - operation.startX) / state.config.pxPerMm;
    const dy = (event.clientY - operation.startY) / state.config.pxPerMm;

    if (operation.kind === 'content-pan') {
        refreshOperationElement(state, operation);
        const offsetX = clamp(operation.contentOffsetX + dx / Math.max(operation.width, 1) * 100, -500, 500);
        const offsetY = clamp(operation.contentOffsetY + dy / Math.max(operation.height, 1) * 100, -500, 500);
        operation.currentContentOffsetX = offsetX;
        operation.currentContentOffsetY = offsetY;
        operation.viewport.dataset.contentOffsetX = String(offsetX);
        operation.viewport.dataset.contentOffsetY = String(offsetY);
        applyContentViewport(operation.viewport, operation.source);
        event.preventDefault();
        return;
    }

    if (operation.kind === 'crop') {
        refreshOperationElement(state, operation);
        const cropX = clamp(operation.cropX + dx / Math.max(operation.width, 1) * 100, -100, 100);
        const cropY = clamp(operation.cropY + dy / Math.max(operation.height, 1) * 100, -100, 100);
        operation.currentCropX = cropX;
        operation.currentCropY = cropY;
        applyImageTransform(operation.image, cropX, cropY, operation.cropScale, operation.imageRotation, operation.flipX, operation.flipY);
        event.preventDefault();
        return;
    }

    const pageWidth = state.page.clientWidth / state.config.pxPerMm;
    const pageHeight = state.page.clientHeight / state.config.pxPerMm;
    const verticalGuides = state.config.snapToGuides
        ? [...state.page.querySelectorAll('.guide-line.vertical:not(.guide-preview)')].map(line => { try { return (number(line.style.left) / state.config.pxPerMm); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...state.page.querySelectorAll(\'.guide-line.vertical:not(.guide-previ@2500', __javascriptError); throw __javascriptError; } })
        : [];
    const horizontalGuides = state.config.snapToGuides
        ? [...state.page.querySelectorAll('.guide-line.horizontal:not(.guide-preview)')].map(line => { try { return (number(line.style.top) / state.config.pxPerMm); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...state.page.querySelectorAll(\'.guide-line.horizontal:not(.guide-pre@2503', __javascriptError); throw __javascriptError; } })
        : [];

    let x = operation.x;
    let y = operation.y;
    let width = operation.width;
    let height = operation.height;

    if (operation.kind === 'move') {
        const initialBounds = operation.movingBounds ?? { x: operation.x, y: operation.y, width: operation.width, height: operation.height };
        let groupX = snapAxis(initialBounds.x + dx, initialBounds.width, pageWidth, verticalGuides, state.config);
        let groupY = snapAxis(initialBounds.y + dy, initialBounds.height, pageHeight, horizontalGuides, state.config);
        if (state.config.snapToObjects || state.config.snapInObjects) {
            const snapped = objectSnapResult(state, operation, groupX, groupY, initialBounds.width, initialBounds.height);
            groupX = snapped.x;
            groupY = snapped.y;
            showObjectAlignmentFeedback(state, operation, snapped);
        } else {
            clearObjectAlignmentFeedback(state);
        }

        const translateX = groupX - initialBounds.x;
        const translateY = groupY - initialBounds.y;
        x = operation.x + translateX;
        y = operation.y + translateY;
        operation.current = { x, y, width, height };
        operation.currentDelta = { x: translateX, y: translateY };
        refreshMovingElements(state, operation);
        for (const item of operation.moving || []) {
            if (!item.element) continue;
            item.element.style.left = `${(item.x + translateX) * state.config.pxPerMm}px`;
            item.element.style.top = `${(item.y + translateY) * state.config.pxPerMm}px`;
            updateAttachedConnectors(state, item.id);
        }
        scheduleSelectionVisualFrame(state);
        event.preventDefault();
        return;
    }

    const handle = operation.handle;
    const radians=number(operation.rotation)*Math.PI/180,cos=Math.cos(radians),sin=Math.sin(radians),localDx=dx*cos+dy*sin,localDy=-dx*sin+dy*cos;
    if(handle.includes('e'))width=Math.max(2,snapSize(operation.width+localDx,state.config));if(handle.includes('w'))width=Math.max(2,snapSize(operation.width-localDx,state.config));if(handle.includes('s'))height=Math.max(2,snapSize(operation.height+localDy,state.config));if(handle.includes('n'))height=Math.max(2,snapSize(operation.height-localDy,state.config));
    const wd=width-operation.width,hd=height-operation.height;let sx=0,sy=0;if(handle.includes('e'))sx+=wd/2;if(handle.includes('w'))sx-=wd/2;if(handle.includes('s'))sy+=hd/2;if(handle.includes('n'))sy-=hd/2;x=operation.centerX+(sx*cos-sy*sin)-width/2;y=operation.centerY+(sx*sin+sy*cos)-height/2;
    operation.current = { x, y, width, height };
    const operationElement = refreshOperationElement(state, operation);
    if (!operationElement) return;
    operationElement.style.left = `${x * state.config.pxPerMm}px`;
    operationElement.style.top = `${y * state.config.pxPerMm}px`;
    operationElement.style.width = `${width * state.config.pxPerMm}px`;
    operationElement.style.height = `${height * state.config.pxPerMm}px`;
    syncEditorElementContentFrame(state, operationElement, width, height);
    refreshContentFit(operationElement);
    updateResizeHandleCursors(operationElement.parentElement || state.page);
    updateAttachedConnectors(state, operation.id);
    scheduleSelectionVisualFrame(state);
    event.preventDefault();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pointerMove@2390', __javascriptError); throw __javascriptError; }}

function snapAxis(value, size, pageSize, guides, config) { try {
    let result = value;
    if (config.snapToGrid && config.gridSpacingMm > 0)
        result = Math.round(result / config.gridSpacingMm) * config.gridSpacingMm;

    const candidates = [];
    if (config.snapToPage) candidates.push(0, pageSize / 2 - size / 2, pageSize - size);
    if (config.snapToGuides) {
        for (const guide of guides) candidates.push(guide, guide - size / 2, guide - size);
    }
    return nearestCandidate(result, candidates, 6 / config.pxPerMm);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:snapAxis@2561', __javascriptError); throw __javascriptError; }}

function snapCoordinate(value, guides, config) { try {
    let result = value;
    if (config.snapToGrid && config.gridSpacingMm > 0)
        result = Math.round(result / config.gridSpacingMm) * config.gridSpacingMm;
    return config.snapToGuides ? nearestCandidate(result, guides, 6 / config.pxPerMm) : result;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:snapCoordinate@2574', __javascriptError); throw __javascriptError; }}

function snapSize(value, config) { try {
    if (!config.snapToGrid || config.gridSpacingMm <= 0) return value;
    return Math.round(value / config.gridSpacingMm) * config.gridSpacingMm;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:snapSize@2581', __javascriptError); throw __javascriptError; }}

function nearestCandidate(value, candidates, tolerance) { try {
    let result = value;
    let distance = tolerance;
    for (const candidate of candidates) {
        const current = Math.abs(value - candidate);
        if (current < distance) {
            result = candidate;
            distance = current;
        }
    }
    return result;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:nearestCandidate@2586', __javascriptError); throw __javascriptError; }}

function pointerUp(state, event) { try {
    const operation = state.operation;
    if (!operation || operation.pointerId !== event.pointerId) return;
    state.operation = null;
    clearObjectAlignmentFeedback(state);
    scheduleSelectionVisualFrame(state);
    try { state.stage.releasePointerCapture(event.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@2605', __caughtJavaScriptError);  }

    if (operation.kind === 'marquee') {
        operation.overlay?.remove?.();
        const selected = operation.moved
            ? operation.currentSelection
            : (operation.additive ? operation.initialSelection : new Set());
        const primary = [...selected].at(-1) || null;
        synchronizeSelectionDom(state, selected, primary);
        safeDotNet(state, 'SetSelectionFromCanvas', [...selected]);
        state.lastCanvasClick = null;
        event.preventDefault();
        return;
    }

    if (operation.kind === 'connector-new' || operation.kind === 'connector-reconnect') {
        state.lastCanvasClick = null;
        // Pointerup can be the first event that reaches the destination during a fast drag.
        updateConnectorDrag(state, event, operation);
        state.operation = operation;
        finishConnectorDrag(state, operation);
        return;
    }

    if (operation.kind === 'connector-control') {
        const controls = operation.currentControls || operation.originalControls;
        if (operation.control === 'route') {
            safeDotNet(state, 'CommitConnectorRoute', operation.connectorId,
                controls.c1.x, controls.c1.y, controls.c2.x, controls.c2.y);
        } else {
            const control = operation.control === '1' ? controls.c1 : controls.c2;
            safeDotNet(state, 'CommitConnectorControl', operation.connectorId, operation.control === '1' ? 1 : 2, control.x, control.y);
        }
        state.lastCanvasClick = null;
        event.preventDefault();
        return;
    }

    if (operation.kind === 'component-pending') {
        // No drag occurred: let the native DevExtreme click finish before a Blazor
        // selection render can replace the widget DOM.
        if (operation.selectionCommitPending)
            setTimeout(() => { try { return (safeDotNet(state, 'SelectElement', operation.id, false)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:setTimeout@2647', __javascriptError); throw __javascriptError; } }, 0);
        return;
    }

    if (operation.kind === 'guide') {
        state.lastCanvasClick = null;
        const max = operation.orientation === 'Horizontal'
            ? number(state.page.dataset.pageHeightMm)
            : number(state.page.dataset.pageWidthMm);
        const position = operation.currentPosition ?? operation.position;
        if (position < -10 || position > max + 10)
            safeDotNet(state, 'DeleteGuide', operation.id);
        else
            safeDotNet(state, 'CommitGuide', operation.id, clamp(position, 0, max));
        return;
    }

    if (!operation.moved) {
        if (operation.additive) {
            if (operation.pendingToggle) {
                optimisticSelectElement(state, operation.element, true);
                safeDotNet(state, 'SelectElement', operation.id, true);
            }
            state.lastCanvasClick = null;
            return;
        }
        if (operation.wasSelected && (operation.moving?.length || 0) > 1) {
            optimisticSelectElement(state, operation.element, false);
            safeDotNet(state, 'SelectElement', operation.id, false);
        }
        registerCanvasClick(state, operation, event);
        return;
    }
    state.lastCanvasClick = null;
    if (operation.kind === 'content-pan') {
        safeDotNet(state, 'CommitContentViewport', operation.id,
            operation.currentContentOffsetX ?? operation.contentOffsetX,
            operation.currentContentOffsetY ?? operation.contentOffsetY, operation.contentScale);
    } else if (operation.kind === 'crop') {
        safeDotNet(
            state,
            'CommitCrop',
            operation.id,
            operation.currentCropX ?? operation.cropX,
            operation.currentCropY ?? operation.cropY,
            operation.cropScale);
    } else if (operation.kind === 'move') {
        const value = operation.current ?? { x: operation.x, y: operation.y };
        safeDotNet(state, 'CommitMove', operation.id, value.x, value.y, [...(operation.movingIds || [])]);
    } else {
        const value = operation.current ?? { x: operation.x, y: operation.y, width: operation.width, height: operation.height };
        safeDotNet(state, 'CommitBounds', operation.id, value.x, value.y, value.width, value.height);
        const resized = state.page.querySelector(`[data-element-id="${CSS.escape(operation.id)}"]`);
        if (resized?.classList.contains('kind-datavisual')) {
            requestAnimationFrame(() => { try { return (window.dispatchEvent(new Event('resize'))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:requestAnimationFrame@2701', __javascriptError); throw __javascriptError; } });
            setTimeout(() => { try { return (window.dispatchEvent(new Event('resize'))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:setTimeout@2702', __javascriptError); throw __javascriptError; } }, 120);
        }
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pointerUp@2599', __javascriptError); throw __javascriptError; }}

function pointerCancel(state, event) { try {
    if (state.operation?.pointerId !== event.pointerId) return;
    resetPointerOperation(state, true);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pointerCancel@2707', __javascriptError); throw __javascriptError; }}

function cropWheel(state, event) { try {
    const mapOwner = event.target?.closest?.('[data-publication-element][data-element-id]');
    if (componentMapContentInteractionActive(state, mapOwner)) return;
    if (state.config.contentPanMode) {
        const element = event.target.closest('[data-publication-element].selected');
        const viewport = element?.querySelector('[data-content-viewport]');
        const source = viewport?.querySelector(':scope > [data-content-fit-source]');
        if (element && viewport && source && state.page.contains(element)) {
            event.preventDefault();
            const id = element.dataset.elementId;
            const offsetX = number(viewport.dataset.contentOffsetX);
            const offsetY = number(viewport.dataset.contentOffsetY);
            const nextScale = clamp(number(viewport.dataset.contentScale, 1) * Math.exp(-event.deltaY * .0015), .1, 12);
            viewport.dataset.contentScale = String(nextScale);
            applyContentViewport(viewport, source);
            const key = `content-${id}`;
            const previous = state.cropTimers.get(key);
            if (previous) clearTimeout(previous);
            state.cropTimers.set(key, setTimeout(() => { try { state.cropTimers.delete(key); safeDotNet(state, 'CommitContentViewport', id, offsetX, offsetY, nextScale);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:setTimeout@2730', __javascriptError); throw __javascriptError; }}, 140));
            return;
        }
    }
    if (!state.config.cropMode) return;
    const element = event.target.closest('[data-publication-element].selected.kind-image');
    if (!element || !state.page.contains(element)) return;
    const image = element.querySelector('img');
    if (!image) return;

    event.preventDefault();
    const id = element.dataset.elementId;
    const cropX = number(image.dataset.cropX);
    const cropY = number(image.dataset.cropY);
    const currentScale = number(image.dataset.cropScale, 1);
    const nextScale = clamp(currentScale * Math.exp(-event.deltaY * 0.0015), .2, 8);
    const rotation = number(image.dataset.imageRotation);
    const flipX = number(image.dataset.flipX, 1);
    const flipY = number(image.dataset.flipY, 1);

    image.dataset.cropScale = String(nextScale);
    applyImageTransform(image, cropX, cropY, nextScale, rotation, flipX, flipY);

    const previous = state.cropTimers.get(id);
    if (previous) clearTimeout(previous);
    state.cropTimers.set(id, setTimeout(() => { try {
        state.cropTimers.delete(id);
        safeDotNet(state, 'CommitCrop', id, cropX, cropY, nextScale);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:setTimeout@2755', __javascriptError); throw __javascriptError; }}, 140));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:cropWheel@2712', __javascriptError); throw __javascriptError; }}

function applyImageTransform(image, cropX, cropY, scale, rotation, flipX, flipY) { try {
    image.dataset.cropX = String(cropX);
    image.dataset.cropY = String(cropY);
    image.dataset.cropScale = String(scale);
    image.style.transform = `translate(${cropX}%, ${cropY}%) rotate(${rotation}deg) scale(${scale * flipX}, ${scale * flipY})`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:applyImageTransform@2761', __javascriptError); throw __javascriptError; }}

function drawRulers(state) { try {
    if (!state.config.rulersVisible || !state.horizontalRuler || !state.verticalRuler) return;
    drawRuler(state, state.horizontalRuler, true);
    drawRuler(state, state.verticalRuler, false);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:drawRulers@2768', __javascriptError); throw __javascriptError; }}

function unitDefinition(unit) { try {
    switch (unit) {
        case 'Centimeter': return { mmPerUnit: 10, suffix: 'cm' };
        case 'Inch': return { mmPerUnit: 25.4, suffix: 'in' };
        case 'Pixel': return { mmPerUnit: 25.4 / 96, suffix: 'px' };
        default: return { mmPerUnit: 1, suffix: 'mm' };
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:unitDefinition@2774', __javascriptError); throw __javascriptError; }}

function niceStep(minimum) { try {
    if (!Number.isFinite(minimum) || minimum <= 0) return 1;
    const power = Math.pow(10, Math.floor(Math.log10(minimum)));
    const normalized = minimum / power;
    const factor = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10;
    return factor * power;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:niceStep@2783', __javascriptError); throw __javascriptError; }}

function configureCanvas(canvas) { try {
    const rect = canvas.getBoundingClientRect();
    const ratio = window.devicePixelRatio || 1;
    const width = Math.max(1, Math.round(rect.width * ratio));
    const height = Math.max(1, Math.round(rect.height * ratio));
    if (canvas.width !== width || canvas.height !== height) {
        canvas.width = width;
        canvas.height = height;
    }
    const context = canvas.getContext('2d');
    if (!context) return { context: null, rect };
    context.setTransform(ratio, 0, 0, ratio, 0, 0);
    return { context, rect };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:configureCanvas@2791', __javascriptError); throw __javascriptError; }}

function drawRuler(state, canvas, horizontal) { try {
    const { context, rect } = configureCanvas(canvas);
    if (!context) return;
    const pageRect = state.page.getBoundingClientRect();
    const unit = unitDefinition(state.config.unit);
    const pixelsPerUnit = state.config.pxPerMm * unit.mmPerUnit;
    const startPixel = horizontal ? pageRect.left - rect.left : pageRect.top - rect.top;
    const endPixel = startPixel + (horizontal ? pageRect.width : pageRect.height);
    const length = horizontal ? rect.width : rect.height;
    const thickness = horizontal ? rect.height : rect.width;

    context.clearRect(0, 0, rect.width, rect.height);
    context.fillStyle = '#eef0f3';
    context.fillRect(0, 0, rect.width, rect.height);
    context.fillStyle = '#ffffff';
    if (horizontal) context.fillRect(startPixel, 0, endPixel - startPixel, thickness);
    else context.fillRect(0, startPixel, thickness, endPixel - startPixel);

    const lower = -startPixel / pixelsPerUnit;
    const upper = (length - startPixel) / pixelsPerUnit;
    const major = niceStep(58 / pixelsPerUnit);
    const minor = major / (major / Math.pow(10, Math.floor(Math.log10(major))) === 2 ? 4 : 5);
    const first = Math.floor(lower / minor) * minor;
    const decimals = major < 1 ? Math.min(3, Math.ceil(-Math.log10(major)) + 1) : 0;

    context.strokeStyle = '#68707b';
    context.fillStyle = '#4b5563';
    context.lineWidth = 1;
    context.font = '9px Segoe UI, sans-serif';
    context.textBaseline = 'top';

    const maxTicks = 2000;
    let tickCount = 0;
    for (let value = first; value <= upper + minor / 2 && tickCount < maxTicks; value += minor, tickCount++) {
        const pixel = startPixel + value * pixelsPerUnit;
        if (pixel < -1 || pixel > length + 1) continue;
        const majorIndex = Math.round(value / major);
        const isMajor = Math.abs(value - majorIndex * major) < minor * .15;
        const halfIndex = Math.round(value / (major / 2));
        const isHalf = !isMajor && Math.abs(value - halfIndex * (major / 2)) < minor * .15;
        const tickLength = isMajor ? thickness - 13 : isHalf ? Math.max(8, thickness * .48) : Math.max(4, thickness * .27);

        context.beginPath();
        if (horizontal) {
            context.moveTo(Math.round(pixel) + .5, thickness);
            context.lineTo(Math.round(pixel) + .5, thickness - tickLength);
        } else {
            context.moveTo(thickness, Math.round(pixel) + .5);
            context.lineTo(thickness - tickLength, Math.round(pixel) + .5);
        }
        context.stroke();

        if (isMajor) {
            const label = (majorIndex * major).toFixed(decimals).replace(/\.0+$/, '');
            if (horizontal) context.fillText(label, pixel + 3, 2);
            else {
                context.save();
                context.translate(2, pixel + 3);
                context.rotate(-Math.PI / 2);
                context.fillText(label, 0, 0);
                context.restore();
            }
        }
    }

    context.strokeStyle = '#9299a3';
    context.beginPath();
    if (horizontal) {
        context.moveTo(0, thickness - .5);
        context.lineTo(length, thickness - .5);
    } else {
        context.moveTo(thickness - .5, 0);
        context.lineTo(thickness - .5, length);
    }
    context.stroke();

    const cursor = horizontal ? state.cursorX : state.cursorY;
    if (cursor == null) return;
    const stageRect = state.stage.getBoundingClientRect();
    const marker = horizontal
        ? stageRect.left + cursor - rect.left
        : stageRect.top + cursor - rect.top;
    if (Number.isFinite(marker) && marker >= 0 && marker <= length) {
        context.strokeStyle = '#d12c2c';
        context.beginPath();
        if (horizontal) {
            context.moveTo(marker + .5, 0);
            context.lineTo(marker + .5, thickness);
        } else {
            context.moveTo(0, marker + .5);
            context.lineTo(thickness, marker + .5);
        }
        context.stroke();
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:drawRuler@2806', __javascriptError); throw __javascriptError; }}

export function calculateFitZoom(stageId, widthMm, heightMm, rulersVisible) { try {
    const stage = document.getElementById(stageId);
    if (!stage) return .8;
    const ruler = rulersVisible ? 28 : 0;
    const availableWidth = Math.max(100, stage.clientWidth - ruler - 84);
    const availableHeight = Math.max(100, stage.clientHeight - ruler - 84);
    const zoom = Math.min(
        availableWidth / (widthMm * PX_PER_MM_AT_96_DPI),
        availableHeight / (heightMm * PX_PER_MM_AT_96_DPI));
    return clamp(zoom, .2, 4);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:calculateFitZoom@2902', __javascriptError); throw __javascriptError; }}

function collectExportCss() { try {
    let css = '';
    for (const sheet of document.styleSheets) {
        try {
            for (const rule of sheet.cssRules) {
                if (rule.type === CSSRule.PAGE_RULE) continue;
                css += `${rule.cssText}\n`;
            }
        } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@2922', __caughtJavaScriptError); 
            // Cross-origin component styles are not required for publication page export.
        }
    }
    return css;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:collectExportCss@2914', __javascriptError); throw __javascriptError; }}

function waitForImages(root) { try {
    return Promise.all([...root.querySelectorAll('img')].map(async image => { try {
        if (image.complete && image.naturalWidth > 0) return;
        try {
            if (typeof image.decode === 'function') await image.decode();
            else await new Promise((resolve, reject) => { try {
                image.addEventListener('load', resolve, { once: true });
                image.addEventListener('error', reject, { once: true });
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@2934', __javascriptError); throw __javascriptError; }});
        } catch {
            throw new Error(`Picture '${image.alt || image.src.slice(0, 48)}' could not be decoded for export.`);
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...root.querySelectorAll(\'img\')].map@2930', __javascriptError); throw __javascriptError; }}));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:waitForImages@2929', __javascriptError); throw __javascriptError; }}

let cssColorProbeContext = null;

function cssColorFunctionToRgba(value) { try {
    try {
        if (!cssColorProbeContext) {
            const canvas = document.createElement('canvas');
            canvas.width = 1;
            canvas.height = 1;
            cssColorProbeContext = canvas.getContext('2d', { willReadFrequently: true });
        }
        const context = cssColorProbeContext;
        if (!context) return value;
        context.clearRect(0, 0, 1, 1);
        context.fillStyle = '#010203';
        context.fillStyle = value;
        context.fillRect(0, 0, 1, 1);
        const pixel = context.getImageData(0, 0, 1, 1).data;
        const alpha = Math.round((pixel[3] / 255) * 10000) / 10000;
        return `rgba(${pixel[0]}, ${pixel[1]}, ${pixel[2]}, ${alpha})`;
    } catch {
        return value;
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:cssColorFunctionToRgba@2946', __javascriptError); throw __javascriptError; }}

function normalizeCssColorFunctions(value) { try {
    if (!value || !/(?:^|\W)(?:color|lab|lch|oklab|oklch)\(/i.test(value)) return value;
    return String(value).replace(/(?:color|lab|lch|oklab|oklch)\([^()]*\)/gi, match => { try { return (cssColorFunctionToRgba(match)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:String(value).replace@2970', __javascriptError); throw __javascriptError; } });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:normalizeCssColorFunctions@2968', __javascriptError); throw __javascriptError; }}

function sanitizeInlineColorFunctions(root) { try {
    const elements = [root, ...root.querySelectorAll('*')];
    for (const element of elements) {
        const style = element.getAttribute?.('style');
        if (style) element.setAttribute('style', normalizeCssColorFunctions(style));
        for (const attribute of ['fill', 'stroke', 'color', 'flood-color', 'stop-color']) {
            const value = element.getAttribute?.(attribute);
            if (value) element.setAttribute(attribute, normalizeCssColorFunctions(value));
        }
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:sanitizeInlineColorFunctions@2973', __javascriptError); throw __javascriptError; }}

function copyComputedStyles(source, clone) { try {
    if (!(source instanceof Element) || !(clone instanceof Element)) return;
    const computed = getComputedStyle(source);
    const important = [
        'position','display','left','top','right','bottom','width','height','min-width','max-width','min-height','max-height','box-sizing','overflow',
        'background','background-color','background-image','background-size','background-position','background-repeat','border','border-top','border-right','border-bottom','border-left',
        'border-color','border-radius','box-shadow','text-shadow','opacity','filter',
        'transform','transform-origin','object-fit','object-position','color','font','font-family',
        'font-size','font-weight','font-style','font-variant','font-feature-settings','line-height','letter-spacing','word-spacing','text-indent','text-rendering','text-align','text-decoration',
        'white-space','word-break','overflow-wrap','text-overflow','text-transform','vertical-align','tab-size',
        'padding','margin','z-index','clip-path','isolation','mix-blend-mode',
        'align-items','align-content','align-self','justify-content','justify-items','justify-self','place-items','gap','row-gap','column-gap',
        'flex','flex-basis','flex-direction','flex-flow','flex-grow','flex-shrink','flex-wrap','order',
        'grid','grid-area','grid-template','grid-template-columns','grid-template-rows','grid-auto-flow','grid-auto-columns','grid-auto-rows',
        'list-style','columns','column-count','column-gap','table-layout','border-collapse','border-spacing',
        'paint-order','stroke','stroke-width','stroke-linecap','stroke-linejoin','fill'
    ];
    let inline = normalizeCssColorFunctions(clone.getAttribute('style') || '').trim();
    if (inline && !inline.endsWith(';')) inline += ';';
    for (const property of important) {
        const value = normalizeCssColorFunctions(computed.getPropertyValue(property));
        if (value) inline += `${property}:${value};`;
    }
    clone.setAttribute('style', inline);
    const sourceChildren = [...source.children];
    const cloneChildren = [...clone.children];
    for (let index = 0; index < Math.min(sourceChildren.length, cloneChildren.length); index++)
        copyComputedStyles(sourceChildren[index], cloneChildren[index]);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:copyComputedStyles@2985', __javascriptError); throw __javascriptError; }}

function cleanPageClone(page) { try {
    const clone = page.cloneNode(true);
    copyComputedStyles(page, clone);
    clone.removeAttribute('id');
    clone.classList.remove('crop-mode');
    clone.style.margin = '0';
    clone.style.boxShadow = 'none';
    clone.style.backgroundImage = 'none';
    clone.querySelectorAll('.selection-handle,.guide-line,.crop-thirds,.crop-help,.connector-port,.connector-endpoint,.connector-control-point,.connector-route-handle,.connector-control-guide,.connector-hit,.connector-ghost,.spreadsheet-sheet-badge').forEach(item => { try { return (item.remove()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:clone.querySelectorAll(\'.selection-handle,.guide-line,.crop-thirds,.cr@3023', __javascriptError); throw __javascriptError; } });
    clone.querySelectorAll('.selected').forEach(item => { try {
        item.classList.remove('selected');
        item.style.outline = 'none';
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:clone.querySelectorAll(\'.selected\').forEach@3024', __javascriptError); throw __javascriptError; }});
    sanitizeInlineColorFunctions(clone);
    return clone;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:cleanPageClone@3015', __javascriptError); throw __javascriptError; }}

function normalizeObjectFitImages(root) { try {
    for (const image of root.querySelectorAll('.image-frame > img')) {
        const source = image.currentSrc || image.getAttribute('src') || '';
        if (!source) continue;

        // html2canvas' computed renderer stretches replaced images in some Chromium builds
        // even when object-fit is present. Inline SVG preserveAspectRatio gives both the DOM
        // renderer and the SVG fallback an explicit, deterministic cover/contain instruction.
        const fit = String(image.style.objectFit || 'fill').toLowerCase();
        const preserveAspectRatio = fit === 'contain'
            ? 'xMidYMid meet'
            : fit === 'cover'
                ? 'xMidYMid slice'
                : 'none';
        const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        svg.setAttribute('xmlns', 'http://www.w3.org/2000/svg');
        svg.setAttribute('xmlns:xlink', 'http://www.w3.org/1999/xlink');
        svg.setAttribute('viewBox', '0 0 100 100');
        svg.setAttribute('preserveAspectRatio', 'none');
        svg.setAttribute('aria-label', image.getAttribute('alt') || 'Publication picture');
        svg.style.cssText = image.getAttribute('style') || '';
        svg.style.objectFit = '';
        svg.style.objectPosition = '';
        svg.style.display = 'block';
        svg.style.width = '100%';
        svg.style.height = '100%';
        svg.style.maxWidth = 'none';
        svg.style.overflow = 'visible';

        const svgImage = document.createElementNS('http://www.w3.org/2000/svg', 'image');
        svgImage.setAttribute('x', '0');
        svgImage.setAttribute('y', '0');
        svgImage.setAttribute('width', '100');
        svgImage.setAttribute('height', '100');
        svgImage.setAttribute('preserveAspectRatio', preserveAspectRatio);
        svgImage.setAttribute('href', source);
        svgImage.setAttributeNS('http://www.w3.org/1999/xlink', 'xlink:href', source);
        svg.appendChild(svgImage);
        image.replaceWith(svg);
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:normalizeObjectFitImages@3032', __javascriptError); throw __javascriptError; }}

function pageExportMetrics(page) { try {
    const rect = page.getBoundingClientRect();
    const widthMm = number(page.dataset.pageWidthMm, 0);
    const heightMm = number(page.dataset.pageHeightMm, 0);
    const canonicalWidth = widthMm > 0 ? widthMm * PX_PER_MM_AT_96_DPI : Math.max(1, rect.width);
    const canonicalHeight = heightMm > 0 ? heightMm * PX_PER_MM_AT_96_DPI : Math.max(1, rect.height);
    return {
        rect,
        widthMm,
        heightMm,
        sourceWidth: Math.max(1, rect.width),
        sourceHeight: Math.max(1, rect.height),
        width: Math.max(1, canonicalWidth),
        height: Math.max(1, canonicalHeight)
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pageExportMetrics@3074', __javascriptError); throw __javascriptError; }}

function canonicalizePageClone(clone, metrics) { try {
    clone.style.position = 'absolute';
    clone.style.left = '0';
    clone.style.top = '0';
    clone.style.width = `${metrics.sourceWidth}px`;
    clone.style.height = `${metrics.sourceHeight}px`;
    clone.style.margin = '0';
    clone.style.transformOrigin = '0 0';
    clone.style.transform = `scale(${metrics.width / metrics.sourceWidth}, ${metrics.height / metrics.sourceHeight})`;
    clone.style.translate = 'none';
    return clone;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:canonicalizePageClone@3091', __javascriptError); throw __javascriptError; }}

function normalizePublicationPageSizes(publication) { try {
    for (const page of publication.querySelectorAll(':scope > .print-page')) {
        const widthMm = number(page.dataset.pageWidthMm, 0);
        const heightMm = number(page.dataset.pageHeightMm, 0);
        let width = widthMm > 0 ? widthMm * PX_PER_MM_AT_96_DPI : 0;
        let height = heightMm > 0 ? heightMm * PX_PER_MM_AT_96_DPI : 0;
        if (!(width > 0)) {
            const match = /^([0-9.]+)mm$/i.exec(page.style.width || '');
            width = match ? number(match[1]) * PX_PER_MM_AT_96_DPI : number(page.style.width, 800);
        }
        if (!(height > 0)) {
            const match = /^([0-9.]+)mm$/i.exec(page.style.height || '');
            height = match ? number(match[1]) * PX_PER_MM_AT_96_DPI : number(page.style.height, 600);
        }
        width = Math.max(1, width);
        height = Math.max(1, height);
        page.dataset.exportWidthPx = String(width);
        page.dataset.exportHeightPx = String(height);
        page.style.width = `${width}px`;
        page.style.height = `${height}px`;
        page.style.minWidth = `${width}px`;
        page.style.minHeight = `${height}px`;
        page.style.maxWidth = 'none';
        page.style.maxHeight = 'none';
        page.style.transform = 'none';
        page.style.translate = 'none';
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:normalizePublicationPageSizes@3104', __javascriptError); throw __javascriptError; }}

function waitForVideoFrame(video, timeoutMs = 8000) { try {
    if (video.readyState >= 2 && video.videoWidth > 0 && video.videoHeight > 0) return Promise.resolve();
    return new Promise((resolve, reject) => { try {
        const timer = setTimeout(() => { try { return (finish(new Error('Video frame loading timed out.'))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:setTimeout@3136', __javascriptError); throw __javascriptError; } }, timeoutMs);
        const finish = error => { try {
            clearTimeout(timer);
            video.removeEventListener('loadeddata', loaded);
            video.removeEventListener('error', failed);
            error ? reject(error) : resolve();
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:finish@3137', __javascriptError); throw __javascriptError; }};
        const loaded = () => { try { return (finish()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:loaded@3143', __javascriptError); throw __javascriptError; } };
        const failed = () => { try { return (finish(new Error('The video frame could not be decoded.'))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:failed@3144', __javascriptError); throw __javascriptError; } };
        video.addEventListener('loadeddata', loaded, { once: true });
        video.addEventListener('error', failed, { once: true });
        video.load();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@3135', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:waitForVideoFrame@3133', __javascriptError); throw __javascriptError; }}

function drawVideoFrameDataUrl(video) { try {
    if (!(video instanceof HTMLVideoElement) || video.videoWidth <= 0 || video.videoHeight <= 0) return '';
    try {
        const canvas = document.createElement('canvas');
        canvas.width = Math.max(1, video.videoWidth);
        canvas.height = Math.max(1, video.videoHeight);
        const context = canvas.getContext('2d');
        if (!context) return '';
        context.drawImage(video, 0, 0, canvas.width, canvas.height);
        return canvas.toDataURL('image/jpeg', .9);
    } catch {
        return '';
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:drawVideoFrameDataUrl@3151', __javascriptError); throw __javascriptError; }}

async function snapshotVideoForRaster(video, owner) { try {
    if (!(video instanceof HTMLVideoElement)) return '';
    if (video.readyState >= 2) {
        const current = drawVideoFrameDataUrl(video);
        if (current) return current;
    }
    const poster = video.getAttribute('poster') || '';
    if (poster.startsWith('data:image/')) return poster;
    const source = video.currentSrc || video.getAttribute('src') || '';
    if (!source) return poster;
    const temporary = document.createElement('video');
    temporary.muted = true;
    temporary.playsInline = true;
    temporary.preload = 'auto';
    temporary.src = source;
    try {
        await waitForVideoFrame(temporary);
        const requested = Number(owner?.dataset?.mediaTrimStart);
        const target = Number.isFinite(requested) ? Math.max(0, requested) : 0;
        if (target > .001 && Number.isFinite(temporary.duration) && target < temporary.duration) {
            await new Promise(resolve => { try {
                const timer = setTimeout(done, 3500);
                function done() { try {
                    clearTimeout(timer);
                    temporary.removeEventListener('seeked', done);
                    resolve();
                 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:done@3188', __javascriptError); throw __javascriptError; }}
                temporary.addEventListener('seeked', done, { once: true });
                temporary.currentTime = target;
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@3186', __javascriptError); throw __javascriptError; }});
        }
        return drawVideoFrameDataUrl(temporary) || poster;
    } catch {
        return poster;
    } finally {
        temporary.pause();
        temporary.removeAttribute('src');
        temporary.load();
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:snapshotVideoForRaster@3166', __javascriptError); throw __javascriptError; }}

function createFrozenRasterImage(source, clone, dataUrl, fallbackLabel) { try {
    const image = document.createElement('img');
    image.alt = source?.getAttribute?.('aria-label') || source?.getAttribute?.('title') || fallbackLabel;
    image.draggable = false;
    image.style.cssText = clone?.getAttribute?.('style') || 'width:100%;height:100%;object-fit:contain;';
    image.style.display = getComputedStyle(source || clone).display === 'none' ? 'none' : 'block';
    image.style.width = image.style.width || '100%';
    image.style.height = image.style.height || '100%';
    image.style.maxWidth = 'none';
    image.style.objectFit = image.style.objectFit || 'contain';
    image.src = dataUrl;
    return image;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:createFrozenRasterImage@3207', __javascriptError); throw __javascriptError; }}

function snapshotCanvasForRaster(canvas) { try {
    if (!(canvas instanceof HTMLCanvasElement) || canvas.width < 1 || canvas.height < 1) return '';
    try { return canvas.toDataURL('image/png'); }
    catch (error) {
        console.warn('PublisherStudio could not snapshot a canvas for render export.', error);
        return '';
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:snapshotCanvasForRaster@3221', __javascriptError); throw __javascriptError; }}

function shouldIgnoreHtml2CanvasCloneElement(element) { try {
    const tag = String(element?.tagName || '').toUpperCase();
    if (tag.startsWith('DXBL-')) return true;
    if (element?.classList?.contains('dxbl-toast-portal')) return true;
    if (element?.matches?.('[data-permanent], .publisher-component-error')) return true;
    return false;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:shouldIgnoreHtml2CanvasCloneElement', __javascriptError); return false; }}

async function snapshotIframeForRaster(frame) { try {
    if (!(frame instanceof HTMLIFrameElement)) return '';
    try {
        const body = frame.contentDocument?.body;
        if (!body || typeof window.html2canvas !== 'function') return '';
        const canvas = await window.html2canvas(body, { backgroundColor: null, scale: 1, logging: false, useCORS: true, allowTaint: false, ignoreElements: shouldIgnoreHtml2CanvasCloneElement });
        return canvas.toDataURL('image/png');
    } catch {
        return '';
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:snapshotIframeForRaster@3230', __javascriptError); throw __javascriptError; }}

async function freezeMediaForRaster(sourcePage, clonePage) { try {
    // cloneNode() copies canvas elements but never their pixel buffers. Snapshot every source
    // canvas before rasterization so Video Studio effects, Mainframe 3D layers, charts and
    // plugin canvases remain visible in PNG/JPEG/SVG render exports.
    const sourceCanvases = [...sourcePage.querySelectorAll('canvas')];
    const cloneCanvases = [...clonePage.querySelectorAll('canvas')];
    for (let index = 0; index < cloneCanvases.length; index++) {
        const sourceCanvas = sourceCanvases[index];
        const cloneCanvas = cloneCanvases[index];
        const snapshot = snapshotCanvasForRaster(sourceCanvas);
        if (!snapshot) continue;
        cloneCanvas.replaceWith(createFrozenRasterImage(sourceCanvas, cloneCanvas, snapshot, 'Rendered canvas effect'));
    }

    const sourceVideos = [...sourcePage.querySelectorAll('video')];
    const cloneVideos = [...clonePage.querySelectorAll('video')];
    for (let index = 0; index < cloneVideos.length; index++) {
        const cloneVideo = cloneVideos[index];
        const sourceVideo = sourceVideos[index];
        const sourceOwner = sourceVideo?.closest?.('[data-media-kind]');
        const snapshot = await snapshotVideoForRaster(sourceVideo, sourceOwner);
        const fallback = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent('<svg xmlns="http://www.w3.org/2000/svg" width="640" height="360"><rect width="100%" height="100%" fill="#111827"/><text x="50%" y="50%" fill="#e5e7eb" font-family="Segoe UI,Arial" font-size="26" text-anchor="middle" dominant-baseline="middle">Video frame unavailable</text></svg>');
        cloneVideo.replaceWith(createFrozenRasterImage(sourceVideo, cloneVideo, snapshot || fallback, 'Frozen video frame'));
    }

    const sourceFrames = [...sourcePage.querySelectorAll('iframe')];
    const cloneFrames = [...clonePage.querySelectorAll('iframe')];
    for (let index = 0; index < cloneFrames.length; index++) {
        const snapshot = await snapshotIframeForRaster(sourceFrames[index]);
        if (!snapshot) continue;
        cloneFrames[index].replaceWith(createFrozenRasterImage(sourceFrames[index], cloneFrames[index], snapshot, 'Rendered embedded component'));
    }

    clonePage.querySelectorAll('audio').forEach(audio => { try { return (audio.remove()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:clonePage.querySelectorAll(\'audio\').forEach@3275', __javascriptError); throw __javascriptError; } });
    clonePage.querySelectorAll('.media-object-badge').forEach(badge => { try { return (badge.remove()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:clonePage.querySelectorAll(\'.media-object-badge\').forEach@3276', __javascriptError); throw __javascriptError; } });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:freezeMediaForRaster@3242', __javascriptError); throw __javascriptError; }}

function blobAsDataUrl(blob) { try {
    return new Promise((resolve, reject) => { try {
        const reader = new FileReader();
        reader.onload = () => { try { return (resolve(String(reader.result || ''))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:reader.onload@3282', __javascriptError); throw __javascriptError; } };
        reader.onerror = () => { try { return (reject(reader.error || new Error('The media asset could not be embedded.'))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:reader.onerror@3283', __javascriptError); throw __javascriptError; } };
        reader.readAsDataURL(blob);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@3280', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:blobAsDataUrl@3279', __javascriptError); throw __javascriptError; }}

async function inlineLocalMediaSources(root) { try {
    const nodes = [...root.querySelectorAll('video[src],audio[src],source[src]')];
    for (const node of nodes) {
        const source = node.getAttribute('src') || '';
        if (!source || source.startsWith('data:') || source.startsWith('blob:')) continue;
        let url;
        try { url = new URL(source, location.href); } catch { continue; }
        if (url.origin !== location.origin || !url.pathname.startsWith('/api/assets/media/')) continue;
        const response = await fetch(url.href, { cache: 'force-cache' });
        if (!response.ok) throw new Error(`Media asset ${url.pathname} could not be embedded (${response.status}).`);
        node.setAttribute('src', await blobAsDataUrl(await response.blob()));
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:inlineLocalMediaSources@3288', __javascriptError); throw __javascriptError; }}

async function pageSvg(page, options = {}) { try {
    await document.fonts?.ready;
    await waitForImages(page);
    const metrics = pageExportMetrics(page);
    const clone = cleanPageClone(page);
    normalizeObjectFitImages(clone);
    if (options.freezeMedia) await freezeMediaForRaster(page, clone);
    else await inlineLocalMediaSources(clone);
    sanitizeInlineColorFunctions(clone);
    canonicalizePageClone(clone, metrics);

    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg.setAttribute('xmlns', 'http://www.w3.org/2000/svg');
    svg.setAttribute('xmlns:xlink', 'http://www.w3.org/1999/xlink');
    svg.setAttribute('width', metrics.widthMm > 0 ? `${metrics.widthMm}mm` : `${metrics.width}px`);
    svg.setAttribute('height', metrics.heightMm > 0 ? `${metrics.heightMm}mm` : `${metrics.height}px`);
    svg.setAttribute('viewBox', `0 0 ${metrics.width} ${metrics.height}`);
    svg.setAttribute('preserveAspectRatio', 'xMidYMid meet');

    const foreignObject = document.createElementNS('http://www.w3.org/2000/svg', 'foreignObject');
    foreignObject.setAttribute('x', '0');
    foreignObject.setAttribute('y', '0');
    foreignObject.setAttribute('width', String(metrics.width));
    foreignObject.setAttribute('height', String(metrics.height));

    const host = document.createElement('div');
    host.setAttribute('xmlns', 'http://www.w3.org/1999/xhtml');
    host.style.position = 'relative';
    host.style.width = `${metrics.width}px`;
    host.style.height = `${metrics.height}px`;
    host.style.margin = '0';
    host.style.padding = '0';
    host.style.overflow = 'hidden';
    host.appendChild(clone);
    foreignObject.appendChild(host);
    svg.appendChild(foreignObject);
    return { text: new XMLSerializer().serializeToString(svg), width: metrics.width, height: metrics.height };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pageSvg@3302', __javascriptError); throw __javascriptError; }}

async function loadSvgImage(svgText) { try {
    const attempts = [];
    const blob = new Blob([svgText], { type: 'image/svg+xml;charset=utf-8' });

    // Chromium's ImageBitmap path is the most reliable way to rasterize an SVG that contains
    // XHTML foreignObject content. Keep two Image fallbacks for browsers that reject it.
    if (typeof createImageBitmap === 'function') {
        try {
            const bitmap = await createImageBitmap(blob);
            return { image: bitmap, cleanup: () => { try { return (bitmap.close()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:cleanup@3350', __javascriptError); throw __javascriptError; } } };
        } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@3351', __caughtJavaScriptError); 
            // Continue with object/data URL fallbacks.
        }
    }

    const objectUrl = URL.createObjectURL(blob);
    attempts.push({ url: objectUrl, revoke: () => { try { return (URL.revokeObjectURL(objectUrl)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:revoke@3357', __javascriptError); throw __javascriptError; } } });
    attempts.push({ url: `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svgText)}`, revoke: null });

    let lastError = null;
    for (const attempt of attempts) {
        const image = new Image();
        image.decoding = 'sync';
        try {
            await new Promise((resolve, reject) => { try {
                const timer = setTimeout(() => { try { return (reject(new Error('SVG rasterization timed out.'))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:setTimeout@3366', __javascriptError); throw __javascriptError; } }, 15000);
                image.onload = () => { try { clearTimeout(timer); resolve();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:image.onload@3367', __javascriptError); throw __javascriptError; }};
                image.onerror = () => { try { clearTimeout(timer); reject(new Error('The browser could not render the SVG export surface.'));  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:image.onerror@3368', __javascriptError); throw __javascriptError; }};
                image.src = attempt.url;
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@3365', __javascriptError); throw __javascriptError; }});
            return { image, cleanup: attempt.revoke };
        } catch (error) {
            lastError = error;
            if (attempt.revoke) attempt.revoke();
        }
    }
    throw lastError || new Error('The browser could not prepare the publication for raster export.');
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:loadSvgImage@3341', __javascriptError); throw __javascriptError; }}

function canvasLooksBlank(canvas) { try {
    if (!canvas || canvas.width < 1 || canvas.height < 1) return true;
    const probe = document.createElement('canvas');
    probe.width = Math.min(128, canvas.width);
    probe.height = Math.min(128, canvas.height);
    const context = probe.getContext('2d', { willReadFrequently: true });
    if (!context) return false;
    context.clearRect(0, 0, probe.width, probe.height);
    context.drawImage(canvas, 0, 0, probe.width, probe.height);
    const pixels = context.getImageData(0, 0, probe.width, probe.height).data;
    for (let index = 3; index < pixels.length; index += 4) {
        if (pixels[index] > 4) return false;
    }
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:canvasLooksBlank@3380', __javascriptError); throw __javascriptError; }}

function freezePageEffectsForRaster(root) { try {
    root?.querySelectorAll?.('.publication-page-effect.page-effect-animated').forEach(layer => { try {
        const from = layer.querySelector('.page-effect-from');
        const to = layer.querySelector('.page-effect-to');
        if (from instanceof HTMLElement) { from.style.animation = 'none'; from.style.opacity = '0'; }
        if (to instanceof HTMLElement) { to.style.animation = 'none'; }
        layer.classList.remove('page-effect-animated');
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:freezePageEffectsForRaster:item', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:freezePageEffectsForRaster', __javascriptError); throw __javascriptError; }}

function cappedRasterScale(width, height, requestedScale) { try {
    const scale = Math.max(.1, number(requestedScale, 1));
    const requestedPixels = Math.max(1, width * scale) * Math.max(1, height * scale);
    const maxPixels = 80_000_000;
    return requestedPixels > maxPixels ? scale * Math.sqrt(maxPixels / requestedPixels) : scale;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:cappedRasterScale@3396', __javascriptError); throw __javascriptError; }}

async function rasterizePageElement(page, scale) { try {
    await document.fonts?.ready;
    await waitForImages(page);
    const metrics = pageExportMetrics(page);
    if (metrics.width <= 0 || metrics.height <= 0) throw new Error('The publication page has no measurable export size.');
    const effectiveScale = cappedRasterScale(metrics.width, metrics.height, scale);
    let domError = null;

    if (typeof window.html2canvas === 'function') {
        const clone = cleanPageClone(page);
        normalizeObjectFitImages(clone);
        await freezeMediaForRaster(page, clone);
        freezePageEffectsForRaster(clone);
        sanitizeInlineColorFunctions(clone);
        clone.style.visibility = 'visible';
        clone.style.opacity = '1';
        canonicalizePageClone(clone, metrics);
        const frame = document.createElement('div');
        const rasterId = `publisher-raster-${Date.now()}-${Math.random().toString(36).slice(2)}`;
        frame.dataset.publisherRasterRoot = rasterId;
        frame.style.cssText = `position:relative;left:0;top:0;width:${metrics.width}px;height:${metrics.height}px;overflow:hidden;visibility:visible;opacity:1;pointer-events:none;background:transparent`;
        frame.appendChild(clone);
        const stage = document.createElement('div');
        stage.setAttribute('aria-hidden', 'true');
        stage.style.cssText = `position:fixed;left:-100000px;top:0;width:${metrics.width}px;height:${metrics.height}px;overflow:hidden;visibility:visible;opacity:1;pointer-events:none;z-index:0;background:transparent`;
        stage.appendChild(frame);
        document.body.appendChild(stage);
        try {
            await waitForImages(frame);
            await new Promise(resolve => { try { return (requestAnimationFrame(() => { try { return (requestAnimationFrame(resolve)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:requestAnimationFrame@3431', __javascriptError); throw __javascriptError; } })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@3431', __javascriptError); throw __javascriptError; } });
            const options = {
                backgroundColor: null, scale: effectiveScale, logging: false, useCORS: true, allowTaint: false,
            ignoreElements: shouldIgnoreHtml2CanvasCloneElement,
                imageTimeout: 20000, removeContainer: true, width: metrics.width, height: metrics.height,
                windowWidth: Math.max(document.documentElement.clientWidth, Math.ceil(metrics.width)),
                windowHeight: Math.max(document.documentElement.clientHeight, Math.ceil(metrics.height)), scrollX: 0, scrollY: 0,
                onclone: documentClone => { try {
                    const root = documentClone.querySelector(`[data-publisher-raster-root="${rasterId}"]`);
                    if (root) sanitizeInlineColorFunctions(root);
                 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:onclone@3437', __javascriptError); throw __javascriptError; }}
            };
            let canvas = null;
            let firstError = null;
            try {
                canvas = await window.html2canvas(frame, { ...options, foreignObjectRendering: false });
            } catch (error) {
                firstError = error;
            }
            if (!canvas || (page.querySelector('[data-publication-element]') && canvasLooksBlank(canvas))) {
                try {
                    canvas = await window.html2canvas(frame, { ...options, foreignObjectRendering: true });
                } catch (error) {
                    throw firstError || error;
                }
            }
            if (page.querySelector('[data-publication-element]') && canvasLooksBlank(canvas))
                throw new Error('The DOM rasterizer returned a transparent image.');
            return canvas;
        } catch (error) {
            domError = error;
            console.warn('DOM rasterization failed; trying the SVG fallback.', error);
        } finally {
            stage.remove();
        }
    }

    try {
        const serialized = await pageSvg(page, { freezeMedia: true });
        const canvas = await svgToCanvas(serialized.text, serialized.width, serialized.height, effectiveScale, false);
        if (page.querySelector('[data-publication-element]') && canvasLooksBlank(canvas))
            throw new Error('The browser returned a transparent SVG raster.');
        return canvas;
    } catch (svgError) {
        throw new Error(`Raster export failed. DOM renderer: ${domError?.message || 'not available'}. SVG renderer: ${svgError?.message || svgError}`);
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:rasterizePageElement@3403', __javascriptError); throw __javascriptError; }}

function prepareOutputCanvas(canvas, jpeg) { try {
    if (!jpeg) return canvas;
    const output = document.createElement('canvas');
    output.width = canvas.width;
    output.height = canvas.height;
    const context = output.getContext('2d', { alpha: false });
    if (!context) throw new Error('The browser did not provide a JPEG canvas context.');
    context.fillStyle = '#ffffff';
    context.fillRect(0, 0, output.width, output.height);
    context.drawImage(canvas, 0, 0);
    return output;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:prepareOutputCanvas@3478', __javascriptError); throw __javascriptError; }}

function canvasToEmbeddedSvg(canvas, widthMm = 0, heightMm = 0) { try {
    const dataUrl = canvas.toDataURL('image/png');
    const width = Math.max(1, canvas.width);
    const height = Math.max(1, canvas.height);
    const widthAttribute = widthMm > 0 ? `${widthMm}mm` : String(width);
    const heightAttribute = heightMm > 0 ? `${heightMm}mm` : String(height);
    return `<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="${widthAttribute}" height="${heightAttribute}" viewBox="0 0 ${width} ${height}" preserveAspectRatio="xMidYMid meet"><image x="0" y="0" width="${width}" height="${height}" href="${dataUrl}" xlink:href="${dataUrl}"/></svg>`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:canvasToEmbeddedSvg@3491', __javascriptError); throw __javascriptError; }}

async function rasterizeIsolatedPublicationElement(page, element, scale) { try {
    const hidden = [];
    const pageStyle = page.getAttribute('style');
    for (const node of page.querySelectorAll('[data-publication-element], .publication-page-effect')) {
        if (node === element) continue;
        hidden.push({
            node,
            value: node.style.getPropertyValue('visibility'),
            priority: node.style.getPropertyPriority('visibility')
        });
        node.style.setProperty('visibility', 'hidden', 'important');
    }
    page.style.setProperty('background', 'transparent', 'important');
    page.style.setProperty('background-color', 'transparent', 'important');
    page.style.setProperty('background-image', 'none', 'important');
    try {
        return await rasterizePageElement(page, scale);
    } finally {
        for (const item of hidden) {
            if (item.value) item.node.style.setProperty('visibility', item.value, item.priority);
            else item.node.style.removeProperty('visibility');
        }
        if (pageStyle === null) page.removeAttribute('style');
        else page.setAttribute('style', pageStyle);
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:rasterizeIsolatedPublicationElement@3500', __javascriptError); throw __javascriptError; }}

function cropCanvasToElement(canvas, page, element, paddingPixels = 2) { try {
    // The selected object is rasterized in isolation, so its painted alpha is a more
    // reliable export boundary than getBoundingClientRect(). The latter only describes
    // the CSS frame and used to crop rotated content, shadows, filters, and Picture Studio
    // pixels extending beyond that frame.
    const context = canvas.getContext('2d', { willReadFrequently: true });
    if (!context) throw new Error('The browser did not provide an object export canvas.');
    const image = context.getImageData(0, 0, canvas.width, canvas.height);
    let left = canvas.width, top = canvas.height, right = -1, bottom = -1;
    for (let y = 0; y < canvas.height; y++) {
        const row = y * canvas.width * 4;
        for (let x = 0; x < canvas.width; x++) {
            if (image.data[row + x * 4 + 3] <= 1) continue;
            if (x < left) left = x;
            if (x > right) right = x;
            if (y < top) top = y;
            if (y > bottom) bottom = y;
        }
    }
    if (right < left || bottom < top) {
        const pageRect = page.getBoundingClientRect();
        const elementRect = element.getBoundingClientRect();
        if (pageRect.width <= 0 || pageRect.height <= 0 || elementRect.width <= 0 || elementRect.height <= 0)
            throw new Error('The selected object has no measurable export area.');
        const scaleX = canvas.width / pageRect.width;
        const scaleY = canvas.height / pageRect.height;
        left = Math.floor((elementRect.left - pageRect.left) * scaleX);
        top = Math.floor((elementRect.top - pageRect.top) * scaleY);
        right = Math.ceil((elementRect.right - pageRect.left) * scaleX) - 1;
        bottom = Math.ceil((elementRect.bottom - pageRect.top) * scaleY) - 1;
    }
    const padding = Math.max(0, Math.round(paddingPixels));
    left = Math.max(0, left - padding);
    top = Math.max(0, top - padding);
    right = Math.min(canvas.width - 1, right + padding);
    bottom = Math.min(canvas.height - 1, bottom + padding);
    if (right < left || bottom < top) throw new Error('The selected object is outside the page export area.');
    const output = document.createElement('canvas');
    output.width = right - left + 1;
    output.height = bottom - top + 1;
    const outputContext = output.getContext('2d');
    if (!outputContext) throw new Error('The browser did not provide an object export canvas.');
    outputContext.drawImage(canvas, left, top, output.width, output.height, 0, 0, output.width, output.height);
    return output;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:cropCanvasToElement@3527', __javascriptError); throw __javascriptError; }}

async function svgToCanvas(svgText, width, height, scale, jpeg) { try {
    const loaded = await loadSvgImage(svgText);
    try {
        const requestedWidth = Math.max(1, Math.round(width * scale));
        const requestedHeight = Math.max(1, Math.round(height * scale));
        const maxPixels = 80_000_000;
        const reduction = requestedWidth * requestedHeight > maxPixels
            ? Math.sqrt(maxPixels / (requestedWidth * requestedHeight))
            : 1;
        const effectiveScale = scale * reduction;

        const canvas = document.createElement('canvas');
        canvas.width = Math.max(1, Math.round(width * effectiveScale));
        canvas.height = Math.max(1, Math.round(height * effectiveScale));
        const context = canvas.getContext('2d', { alpha: !jpeg });
        if (!context) throw new Error('The browser did not provide a 2D canvas context.');
        context.clearRect(0, 0, canvas.width, canvas.height);
        if (jpeg) {
            context.fillStyle = '#ffffff';
            context.fillRect(0, 0, canvas.width, canvas.height);
        }
        context.setTransform(effectiveScale, 0, 0, effectiveScale, 0, 0);
        context.drawImage(loaded.image, 0, 0, width, height);
        return canvas;
    } finally {
        if (loaded.cleanup) loaded.cleanup();
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:svgToCanvas@3549', __javascriptError); throw __javascriptError; }}

async function canvasBlob(canvas, mimeType, quality) { try {
    const blob = await new Promise(resolve => { try { return (canvas.toBlob(resolve, mimeType, quality)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@3579', __javascriptError); throw __javascriptError; } });
    if (blob) return blob;
    try {
        const dataUrl = canvas.toDataURL(mimeType, quality);
        const response = await fetch(dataUrl);
        return await response.blob();
    } catch {
        throw new Error('The browser could not create the raster image. Try SVG export or a lower DPI.');
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:canvasBlob@3578', __javascriptError); throw __javascriptError; }}

function downloadBlob(fileName, blob) { try {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    setTimeout(() => { try { return (URL.revokeObjectURL(url)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:setTimeout@3598', __javascriptError); throw __javascriptError; } }, 1500);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:downloadBlob@3590', __javascriptError); throw __javascriptError; }}

let zipCrcTable;
function crc32(bytes) { try {
    if (!zipCrcTable) {
        zipCrcTable = new Uint32Array(256);
        for (let index = 0; index < 256; index++) {
            let value = index;
            for (let bit = 0; bit < 8; bit++) value = (value & 1) ? (0xedb88320 ^ (value >>> 1)) : (value >>> 1);
            zipCrcTable[index] = value >>> 0;
        }
    }
    let crc = 0xffffffff;
    for (const value of bytes) crc = zipCrcTable[(crc ^ value) & 0xff] ^ (crc >>> 8);
    return (crc ^ 0xffffffff) >>> 0;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:crc32@3602', __javascriptError); throw __javascriptError; }}

function dosDateTime(date = new Date()) { try {
    const year = Math.max(1980, date.getFullYear());
    return {
        time: ((date.getHours() & 31) << 11) | ((date.getMinutes() & 63) << 5) | ((Math.floor(date.getSeconds() / 2)) & 31),
        date: (((year - 1980) & 127) << 9) | (((date.getMonth() + 1) & 15) << 5) | (date.getDate() & 31)
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:dosDateTime@3616', __javascriptError); throw __javascriptError; }}

async function deflateRawZipBytes(bytes) { try {
    if (typeof CompressionStream !== 'function') return null;
    try {
        const stream = new Blob([bytes]).stream().pipeThrough(new CompressionStream('deflate-raw'));
        return new Uint8Array(await new Response(stream).arrayBuffer());
    } catch {
        return null;
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:deflateRawZipBytes@3624', __javascriptError); throw __javascriptError; }}

async function createZip(files, options = {}) { try {
    const encoder = new TextEncoder();
    const localParts = [];
    const centralParts = [];
    let offset = 0;
    const allowCompression = options.compress !== false;
    for (const file of files) {
        const name = encoder.encode(file.name);
        const bytes = new Uint8Array(await file.blob.arrayBuffer());
        const compressed = allowCompression && file.compress !== false ? await deflateRawZipBytes(bytes) : null;
        const useDeflate = Boolean(compressed && compressed.length + 8 < bytes.length);
        const payload = useDeflate ? compressed : bytes;
        const method = useDeflate ? 8 : 0;
        const crc = crc32(bytes);
        const stamp = dosDateTime(file.modified || new Date());
        const local = new Uint8Array(30 + name.length);
        const localView = new DataView(local.buffer);
        localView.setUint32(0, 0x04034b50, true);
        localView.setUint16(4, 20, true);
        localView.setUint16(6, 0x0800, true);
        localView.setUint16(8, method, true);
        localView.setUint16(10, stamp.time, true);
        localView.setUint16(12, stamp.date, true);
        localView.setUint32(14, crc, true);
        localView.setUint32(18, payload.length, true);
        localView.setUint32(22, bytes.length, true);
        localView.setUint16(26, name.length, true);
        localView.setUint16(28, 0, true);
        local.set(name, 30);
        localParts.push(local, payload);

        const central = new Uint8Array(46 + name.length);
        const centralView = new DataView(central.buffer);
        centralView.setUint32(0, 0x02014b50, true);
        centralView.setUint16(4, 20, true);
        centralView.setUint16(6, 20, true);
        centralView.setUint16(8, 0x0800, true);
        centralView.setUint16(10, method, true);
        centralView.setUint16(12, stamp.time, true);
        centralView.setUint16(14, stamp.date, true);
        centralView.setUint32(16, crc, true);
        centralView.setUint32(20, payload.length, true);
        centralView.setUint32(24, bytes.length, true);
        centralView.setUint16(28, name.length, true);
        centralView.setUint16(30, 0, true);
        centralView.setUint16(32, 0, true);
        centralView.setUint16(34, 0, true);
        centralView.setUint16(36, 0, true);
        centralView.setUint32(38, 0, true);
        centralView.setUint32(42, offset, true);
        central.set(name, 46);
        centralParts.push(central);
        offset += local.length + payload.length;
    }
    const centralOffset = offset;
    const centralSize = centralParts.reduce((sum, part) => { try { return (sum + part.length); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:centralParts.reduce@3689', __javascriptError); throw __javascriptError; } }, 0);
    const end = new Uint8Array(22);
    const endView = new DataView(end.buffer);
    endView.setUint32(0, 0x06054b50, true);
    endView.setUint16(4, 0, true);
    endView.setUint16(6, 0, true);
    endView.setUint16(8, files.length, true);
    endView.setUint16(10, files.length, true);
    endView.setUint32(12, centralSize, true);
    endView.setUint32(16, centralOffset, true);
    endView.setUint16(20, 0, true);
    return new Blob([...localParts, ...centralParts, end], { type: 'application/zip' });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:createZip@3634', __javascriptError); throw __javascriptError; }}

async function createStoredZip(files) { try {
    return createZip(files, { compress: false });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:createStoredZip@3703', __javascriptError); throw __javascriptError; }}


function escapeHtml(value) { try {
    return String(value ?? '')
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;');
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:escapeHtml@3708', __javascriptError); throw __javascriptError; }}

function parseHexColor(value) { try {
    const text = String(value || '#ffffff').trim().replace('#', '');
    const normalized = text.length === 3 ? [...text].map(x => { try { return (x + x); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...text].map@3718', __javascriptError); throw __javascriptError; } }).join('') : text.padEnd(6, 'f').slice(0, 6);
    return {
        r: Number.parseInt(normalized.slice(0, 2), 16),
        g: Number.parseInt(normalized.slice(2, 4), 16),
        b: Number.parseInt(normalized.slice(4, 6), 16)
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:parseHexColor@3716', __javascriptError); throw __javascriptError; }}

async function imageFromDataUrl(dataUrl) { try {
    const image = new Image();
    image.decoding = 'async';
    await new Promise((resolve, reject) => { try {
        image.onload = resolve;
        image.onerror = () => { try { return (reject(new Error('The selected picture could not be decoded.'))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:image.onerror@3731', __javascriptError); throw __javascriptError; } };
        image.src = dataUrl;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@3729', __javascriptError); throw __javascriptError; }});
    return image;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:imageFromDataUrl@3726', __javascriptError); throw __javascriptError; }}

const workspaceStates = new WeakMap();

function setWorkspaceColumns(workspace, state) { try {
    workspace.style.setProperty('--pages-pane-width', state.leftCollapsed ? '0px' : `${state.left}px`);
    workspace.style.setProperty('--inspector-pane-width', state.rightCollapsed ? '0px' : `${state.right}px`);
    workspace.classList.toggle('pages-collapsed', state.leftCollapsed);
    workspace.classList.toggle('inspector-collapsed', state.rightCollapsed);
    localStorage.setItem('blazorPublisher.workspace', JSON.stringify(state));
    window.dispatchEvent(new Event('resize'));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:setWorkspaceColumns@3739', __javascriptError); throw __javascriptError; }}

function createWorkspaceState(workspace) { try {
    let stored = {};
    try { stored = JSON.parse(localStorage.getItem('blazorPublisher.workspace') || '{}'); } catch { stored = {}; }
    const state = {
        left: clamp(number(stored.left, 172), 120, 420),
        right: clamp(number(stored.right, 292), 220, 560),
        leftCollapsed: !!stored.leftCollapsed,
        rightCollapsed: !!stored.rightCollapsed
    };
    if (!localStorage.getItem('blazorPublisher.workspace')) {
        if (workspace.clientWidth < 920) state.rightCollapsed = true;
        if (workspace.clientWidth < 680) state.leftCollapsed = true;
    }
    workspaceStates.set(workspace, state);
    setWorkspaceColumns(workspace, state);
    return state;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:createWorkspaceState@3748', __javascriptError); throw __javascriptError; }}

function bindWorkspaceSplitter(workspace, splitter, side) { try {
    if (!splitter || splitter.dataset.bound === 'true') return;
    splitter.dataset.bound = 'true';
    splitter.addEventListener('dblclick', () => { try { return (window.publisherStudio.toggleWorkspacePane(workspace.id, side)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:splitter.addEventListener@3769', __javascriptError); throw __javascriptError; } });
    splitter.addEventListener('pointerdown', event => { try {
        if (event.button !== 0) return;
        const state = workspaceStates.get(workspace) || createWorkspaceState(workspace);
        const startX = event.clientX;
        const initial = side === 'left' ? state.left : state.right;
        splitter.classList.add('dragging');
        splitter.setPointerCapture(event.pointerId);
        const move = moveEvent => { try {
            const delta = moveEvent.clientX - startX;
            if (side === 'left') {
                state.leftCollapsed = false;
                state.left = clamp(initial + delta, 120, Math.max(120, workspace.clientWidth * .42));
            } else {
                state.rightCollapsed = false;
                state.right = clamp(initial - delta, 220, Math.max(220, workspace.clientWidth * .48));
            }
            setWorkspaceColumns(workspace, state);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:move@3777', __javascriptError); throw __javascriptError; }};
        const up = upEvent => { try {
            splitter.classList.remove('dragging');
            splitter.removeEventListener('pointermove', move);
            splitter.removeEventListener('pointerup', up);
            splitter.removeEventListener('pointercancel', up);
            try { splitter.releasePointerCapture(upEvent.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@3793', __caughtJavaScriptError);  }
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:up@3788', __javascriptError); throw __javascriptError; }};
        splitter.addEventListener('pointermove', move);
        splitter.addEventListener('pointerup', up);
        splitter.addEventListener('pointercancel', up);
        event.preventDefault();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:splitter.addEventListener@3770', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:bindWorkspaceSplitter@3766', __javascriptError); throw __javascriptError; }}


function normalizeWordArtPoints(points) { try {
    const normalized = Array.isArray(points)
        ? points
            .map(point => { try { return (({ x: clamp(number(point?.x ?? point?.X), 0, 1000), y: clamp(number(point?.y ?? point?.Y), 0, 300) })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:points .map@3806', __javascriptError); throw __javascriptError; } })
            .filter(point => { try { return (Number.isFinite(point.x) && Number.isFinite(point.y)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:points .map(point => ({ x: clamp(number(point?.x ?? point?.X), 0, 1000@3807', __javascriptError); throw __javascriptError; } })
            .slice(0, 32)
        : [];
    return normalized.length >= 2 ? normalized : [{ x: 60, y: 150 }, { x: 940, y: 150 }];
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:normalizeWordArtPoints@3803', __javascriptError); throw __javascriptError; }}

function wordArtPathFromPoints(points) { try {
    const safe = normalizeWordArtPoints(points);
    if (safe.length === 2)
        return `M ${safe[0].x} ${safe[0].y} L ${safe[1].x} ${safe[1].y}`;

    let path = `M ${safe[0].x} ${safe[0].y}`;
    for (let index = 0; index < safe.length - 1; index++) {
        const previous = index === 0 ? safe[index] : safe[index - 1];
        const current = safe[index];
        const next = safe[index + 1];
        const following = index + 2 < safe.length ? safe[index + 2] : next;
        const control1 = {
            x: current.x + (next.x - previous.x) / 6,
            y: current.y + (next.y - previous.y) / 6
        };
        const control2 = {
            x: next.x - (following.x - current.x) / 6,
            y: next.y - (following.y - current.y) / 6
        };
        path += ` C ${control1.x} ${control1.y} ${control2.x} ${control2.y} ${next.x} ${next.y}`;
    }
    return path;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:wordArtPathFromPoints@3813', __javascriptError); throw __javascriptError; }}

function wordArtEditorPoint(svg, event) { try {
    const matrix = svg.getScreenCTM();
    if (!matrix) return { x: 0, y: 0 };
    const point = new DOMPoint(event.clientX, event.clientY).matrixTransform(matrix.inverse());
    return { x: clamp(point.x, 0, 1000), y: clamp(point.y, 0, 300) };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:wordArtEditorPoint@3837', __javascriptError); throw __javascriptError; }}

function wordArtDistance(left, right) { try {
    return Math.hypot(left.x - right.x, left.y - right.y);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:wordArtDistance@3844', __javascriptError); throw __javascriptError; }}

function perpendicularDistance(point, start, end) { try {
    const dx = end.x - start.x;
    const dy = end.y - start.y;
    if (dx === 0 && dy === 0) return wordArtDistance(point, start);
    const t = clamp(((point.x - start.x) * dx + (point.y - start.y) * dy) / (dx * dx + dy * dy), 0, 1);
    return wordArtDistance(point, { x: start.x + t * dx, y: start.y + t * dy });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:perpendicularDistance@3848', __javascriptError); throw __javascriptError; }}

function simplifyWordArtPoints(points, tolerance = 8) { try {
    if (points.length <= 2) return points.slice();
    let maximumDistance = 0;
    let splitIndex = 0;
    for (let index = 1; index < points.length - 1; index++) {
        const distance = perpendicularDistance(points[index], points[0], points[points.length - 1]);
        if (distance > maximumDistance) {
            maximumDistance = distance;
            splitIndex = index;
        }
    }
    if (maximumDistance <= tolerance) return [points[0], points[points.length - 1]];
    const left = simplifyWordArtPoints(points.slice(0, splitIndex + 1), tolerance);
    const right = simplifyWordArtPoints(points.slice(splitIndex), tolerance);
    return [...left.slice(0, -1), ...right];
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:simplifyWordArtPoints@3856', __javascriptError); throw __javascriptError; }}

function limitWordArtPoints(points, maximum = 18) { try {
    if (points.length <= maximum) return points;
    const result = [points[0]];
    for (let index = 1; index < maximum - 1; index++) {
        const sourceIndex = Math.round(index * (points.length - 1) / (maximum - 1));
        result.push(points[sourceIndex]);
    }
    result.push(points[points.length - 1]);
    return result;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:limitWordArtPoints@3873', __javascriptError); throw __javascriptError; }}

function renderWordArtPathEditor(state) { try {
    state.path?.setAttribute('d', wordArtPathFromPoints(state.points));
    if (!state.pointLayer) return;
    state.pointLayer.replaceChildren();
    state.points.forEach((point, index) => { try {
        const circle = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
        circle.setAttribute('cx', String(point.x));
        circle.setAttribute('cy', String(point.y));
        circle.setAttribute('r', index === 0 || index === state.points.length - 1 ? '11' : '8');
        circle.classList.add('wordart-path-point');
        if (index === 0) circle.classList.add('start');
        if (index === state.points.length - 1) circle.classList.add('end');
        circle.dataset.wordartPointIndex = String(index);
        state.pointLayer.appendChild(circle);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:state.points.forEach@3888', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:renderWordArtPathEditor@3884', __javascriptError); throw __javascriptError; }}

function commitWordArtPath(state) { try {
    return state.dotnet.invokeMethodAsync('CommitWordArtPath', state.points.map(point => { try { return (({ x: point.x, y: point.y })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:state.points.map@3902', __javascriptError); throw __javascriptError; } }));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:commitWordArtPath@3901', __javascriptError); throw __javascriptError; }}

function wordArtPathPointerDown(state, event) { try {
    if (event.button !== 0) return;
    const pointIndex = event.target?.dataset?.wordartPointIndex;
    if (pointIndex !== undefined) {
        state.operation = { kind: 'point', index: Number.parseInt(pointIndex, 10), pointerId: event.pointerId };
    } else if (state.drawMode) {
        state.operation = { kind: 'draw', pointerId: event.pointerId };
        state.points = [wordArtEditorPoint(state.svg, event)];
        renderWordArtPathEditor(state);
    } else {
        return;
    }
    state.svg.setPointerCapture(event.pointerId);
    event.preventDefault();
    event.stopPropagation();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:wordArtPathPointerDown@3905', __javascriptError); throw __javascriptError; }}

function wordArtPathPointerMove(state, event) { try {
    if (!state.operation || state.operation.pointerId !== event.pointerId) return;
    const point = wordArtEditorPoint(state.svg, event);
    if (state.operation.kind === 'point') {
        state.points[state.operation.index] = point;
    } else {
        const previous = state.points[state.points.length - 1];
        if (state.points.length < 512 && (!previous || wordArtDistance(previous, point) >= 7)) state.points.push(point);
    }
    renderWordArtPathEditor(state);
    event.preventDefault();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:wordArtPathPointerMove@3922', __javascriptError); throw __javascriptError; }}

async function wordArtPathPointerUp(state, event) { try {
    if (!state.operation || state.operation.pointerId !== event.pointerId) return;
    if (state.operation.kind === 'draw') {
        if (state.points.length < 2) {
            const start = state.points[0] || { x: 60, y: 150 };
            state.points = [start, { x: clamp(start.x + 220, 0, 1000), y: start.y }];
        }
        state.points = limitWordArtPoints(simplifyWordArtPoints(state.points, 7));
        state.drawMode = false;
        state.svg.classList.remove('drawing-armed');
    }
    state.operation = null;
    renderWordArtPathEditor(state);
    try { state.svg.releasePointerCapture(event.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@3948', __caughtJavaScriptError);  }
    await commitWordArtPath(state);
    event.preventDefault();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:wordArtPathPointerUp@3935', __javascriptError); throw __javascriptError; }}

export function initializeWordArtPathEditor(editorId, dotnet, points) { try {
    const svg = document.getElementById(editorId);
    if (!svg) return;
    let state = wordArtPathStates.get(svg);
    if (!state) {
        state = {
            svg,
            dotnet,
            points: normalizeWordArtPoints(points),
            path: svg.querySelector('[data-wordart-editor-path]'),
            pointLayer: svg.querySelector('[data-wordart-editor-points]'),
            drawMode: false,
            operation: null
        };
        state.pointerDown = event => { try { return (wordArtPathPointerDown(state, event)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:state.pointerDown@3967', __javascriptError); throw __javascriptError; } };
        state.pointerMove = event => { try { return (wordArtPathPointerMove(state, event)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:state.pointerMove@3968', __javascriptError); throw __javascriptError; } };
        state.pointerUp = event => { try { return (wordArtPathPointerUp(state, event)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:state.pointerUp@3969', __javascriptError); throw __javascriptError; } };
        svg.addEventListener('pointerdown', state.pointerDown);
        svg.addEventListener('pointermove', state.pointerMove);
        svg.addEventListener('pointerup', state.pointerUp);
        svg.addEventListener('pointercancel', state.pointerUp);
        wordArtPathStates.set(svg, state);
    }
    state.dotnet = dotnet;
    state.points = normalizeWordArtPoints(points);
    state.path = svg.querySelector('[data-wordart-editor-path]');
    state.pointLayer = svg.querySelector('[data-wordart-editor-points]');
    renderWordArtPathEditor(state);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:initializeWordArtPathEditor@3953', __javascriptError); throw __javascriptError; }}

export function updateWordArtPathEditor(editorId, points) { try {
    const svg = document.getElementById(editorId);
    const state = svg ? wordArtPathStates.get(svg) : null;
    if (!state || state.operation) return;
    state.points = normalizeWordArtPoints(points);
    state.path = svg.querySelector('[data-wordart-editor-path]');
    state.pointLayer = svg.querySelector('[data-wordart-editor-points]');
    renderWordArtPathEditor(state);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:updateWordArtPathEditor@3983', __javascriptError); throw __javascriptError; }}

export function setWordArtPathDrawMode(editorId, enabled) { try {
    const svg = document.getElementById(editorId);
    const state = svg ? wordArtPathStates.get(svg) : null;
    if (!state) return;
    state.drawMode = Boolean(enabled);
    svg.classList.toggle('drawing-armed', state.drawMode);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:setWordArtPathDrawMode@3993', __javascriptError); throw __javascriptError; }}

export function disposeWordArtPathEditor(editorId) { try {
    const svg = document.getElementById(editorId);
    const state = svg ? wordArtPathStates.get(svg) : null;
    if (!state) return;
    svg.removeEventListener('pointerdown', state.pointerDown);
    svg.removeEventListener('pointermove', state.pointerMove);
    svg.removeEventListener('pointerup', state.pointerUp);
    svg.removeEventListener('pointercancel', state.pointerUp);
    wordArtPathStates.delete(svg);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:disposeWordArtPathEditor@4001', __javascriptError); throw __javascriptError; }}


const publicationAnimationPreviews = new Map();

function publicationPreviewAttribute(node, name) { try {
    return node?.hasAttribute?.(name) ? node.getAttribute(name) : null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:publicationPreviewAttribute@4015', __javascriptError); throw __javascriptError; }}
function publicationPreviewSnapshot(node) { try {
    if (!node) return null;
    const media = node.matches?.('video,audio') ? node : null;
    return {
        node,
        style: publicationPreviewAttribute(node, 'style'),
        className: publicationPreviewAttribute(node, 'class'),
        hidden: publicationPreviewAttribute(node, 'hidden'),
        signalOpacity: publicationPreviewAttribute(node, 'data-publisher-signal-opacity'),
        media: media ? {
            currentTime: Number.isFinite(media.currentTime) ? media.currentTime : 0,
            paused: media.paused,
            volume: media.volume,
            playbackRate: media.playbackRate,
            muted: media.muted,
            loop: media.loop,
            timeHandler: media.__publisherTimeHandler || null
        } : null
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:publicationPreviewSnapshot@4018', __javascriptError); throw __javascriptError; }}
function restorePublicationPreviewAttribute(node, name, value) { try {
    if (!node) return;
    if (value === null) node.removeAttribute(name); else node.setAttribute(name, value);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:restorePublicationPreviewAttribute@4038', __javascriptError); throw __javascriptError; }}
function restorePublicationPreviewSnapshot(snapshot) { try {
    const node = snapshot?.node;
    if (!node?.isConnected) return;
    restorePublicationPreviewAttribute(node, 'style', snapshot.style);
    restorePublicationPreviewAttribute(node, 'class', snapshot.className);
    restorePublicationPreviewAttribute(node, 'hidden', snapshot.hidden);
    restorePublicationPreviewAttribute(node, 'data-publisher-signal-opacity', snapshot.signalOpacity);
    if (!snapshot.media || !node.matches?.('video,audio')) return;
    const currentHandler = node.__publisherTimeHandler;
    if (currentHandler) node.removeEventListener('timeupdate', currentHandler);
    node.__publisherTimeHandler = snapshot.media.timeHandler;
    if (snapshot.media.timeHandler) node.addEventListener('timeupdate', snapshot.media.timeHandler);
    node.pause();
    node.volume = snapshot.media.volume;
    node.playbackRate = snapshot.media.playbackRate;
    node.muted = snapshot.media.muted;
    node.loop = snapshot.media.loop;
    try { node.currentTime = snapshot.media.currentTime; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@4059', __caughtJavaScriptError);  }
    if (!snapshot.media.paused) node.play().catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@4060', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:node.play().catch@4060', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:restorePublicationPreviewSnapshot@4042', __javascriptError); throw __javascriptError; }}
function capturePublicationPreviewNode(state, node) { try {
    if (!state?.snapshots || !node || state.snapshots.has(node)) return;
    state.snapshots.set(node, publicationPreviewSnapshot(node));
    state.baseTransforms?.set?.(node, baseTransform(node));
    if (!node.matches?.('video,audio'))
        node.querySelectorAll?.('video,audio').forEach(media => { try { return (capturePublicationPreviewNode(state, media)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:node.querySelectorAll?.(\'video,audio\').forEach@4067', __javascriptError); throw __javascriptError; } });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:capturePublicationPreviewNode@4062', __javascriptError); throw __javascriptError; }}

function parsePublicationData(value, fallback) { try {
    if (!value) return fallback;
    try { return JSON.parse(value); } catch { return fallback; }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:parsePublicationData@4070', __javascriptError); throw __javascriptError; }}

function animationName(value) { try { return String(value || '').replace(/[^a-z0-9]/gi, '').toLowerCase();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:animationName@4075', __javascriptError); throw __javascriptError; }}
function isMediaAnimationEffect(value) { try { return ['playmedia', 'pausemedia', 'stopmedia'].includes(animationName(value));  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:isMediaAnimationEffect@4076', __javascriptError); throw __javascriptError; }}
function publicationReducedMotion() { try { return typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:publicationReducedMotion@4077', __javascriptError); throw __javascriptError; }}
function publicationAnimationSpan(animation) { try {
    if (publicationReducedMotion()) return .001;
    if (isMediaAnimationEffect(animation.effect)) return .05;
    return Math.max(.05, animationNumber(animation.durationSeconds, .6))
        * Math.max(1, animationNumber(animation.repeatCount, 1))
        * (animation.autoReverse ? 2 : 1);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:publicationAnimationSpan@4078', __javascriptError); throw __javascriptError; }}
function animationNumber(value, fallback) { try {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : fallback;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:animationNumber@4085', __javascriptError); throw __javascriptError; }}
function animationEasing(value) { try {
    switch (animationName(value)) {
        case 'linear': return 'linear';
        case 'easein': return 'cubic-bezier(.42,0,1,1)';
        case 'easeout': return 'cubic-bezier(0,0,.2,1)';
        case 'backout': return 'cubic-bezier(.18,.89,.32,1.28)';
        case 'bounceout': return 'cubic-bezier(.22,1.3,.36,1)';
        default: return 'cubic-bezier(.4,0,.2,1)';
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:animationEasing@4089', __javascriptError); throw __javascriptError; }}
function animationDirectionVector(direction, distance) { try {
    const amount = animationNumber(distance, 18);
    switch (animationName(direction)) {
        case 'right': return { x: amount, y: 0 };
        case 'up': return { x: 0, y: -amount };
        case 'down': return { x: 0, y: amount };
        default: return { x: -amount, y: 0 };
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:animationDirectionVector@4099', __javascriptError); throw __javascriptError; }}
function baseTransform(node) { try {
    const inline = String(node?.style?.transform || '').trim();
    if (inline) return inline === 'none' ? '' : inline;
    const value = getComputedStyle(node).transform;
    return !value || value === 'none' ? '' : value;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:baseTransform@4108', __javascriptError); throw __javascriptError; }}
function withBase(base, transform) { try { return `${transform} ${base}`.trim();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:withBase@4114', __javascriptError); throw __javascriptError; }}
function publicationAnimationFrames(node, animation, baseOverride = null) { try {
    const effect = animationName(animation.effect);
    const phase = animationName(animation.phase);
    const base = baseOverride ?? baseTransform(node);
    const vector = animationDirectionVector(animation.direction, animation.distancePercent);
    const scaleAmount = Math.max(0.01, animationNumber(animation.scalePercent, 20) / 100);
    const rotation = animationNumber(animation.rotationDegrees, 360);
    const translated = withBase(base, `translate(${vector.x}%,${vector.y}%)`);
    const reverse = frames => { try { return (phase === 'exit' ? [...frames].reverse() : frames); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:reverse@4123', __javascriptError); throw __javascriptError; } };

    switch (effect) {
        case 'fade':
            return reverse([{ opacity: 0 }, { opacity: 1 }]);
        case 'fly':
            return reverse([{ opacity: 0, transform: translated }, { opacity: 1, transform: base || 'none' }]);
        case 'float':
            return reverse([{ opacity: 0, filter: 'blur(6px)', transform: withBase(base, `translate(${vector.x / 2}%,${vector.y / 2}%)`) }, { opacity: 1, filter: 'blur(0)', transform: base || 'none' }]);
        case 'zoom':
            return reverse([{ opacity: 0, transform: withBase(base, `scale(${Math.max(.02, 1 - scaleAmount)})`) }, { opacity: 1, transform: base || 'none' }]);
        case 'wipe': {
            const direction = animationName(animation.direction);
            const start = direction === 'right' ? 'inset(0 100% 0 0)' : direction === 'up' ? 'inset(100% 0 0 0)' : direction === 'down' ? 'inset(0 0 100% 0)' : 'inset(0 0 0 100%)';
            return reverse([{ opacity: 0, clipPath: start }, { opacity: 1, clipPath: 'inset(0 0 0 0)' }]);
        }
        case 'bounce':
            if (phase === 'entrance' || phase === 'exit') return reverse([
                { opacity: 0, transform: withBase(base, `translate(${vector.x}%,${vector.y}%) scale(${Math.max(.05, 1 - scaleAmount)})`) },
                { opacity: 1, offset: .62, transform: withBase(base, 'scale(1.08)') },
                { opacity: 1, transform: base || 'none' }
            ]);
            return [
                { transform: base || 'none' },
                { offset: .35, transform: withBase(base, `translateY(${-Math.max(8, animationNumber(animation.distancePercent, 18))}%) scale(${1 + scaleAmount / 2})`) },
                { offset: .7, transform: withBase(base, 'translateY(3%) scale(.98)') },
                { transform: base || 'none' }
            ];
        case 'pulse':
            return [{ transform: base || 'none' }, { transform: withBase(base, `scale(${1 + scaleAmount})`), offset: .5 }, { transform: base || 'none' }];
        case 'growshrink':
            return [{ transform: base || 'none' }, { transform: withBase(base, `scale(${1 + scaleAmount})`), offset: .5 }, { transform: base || 'none' }];
        case 'spin':
            return [{ transform: base || 'none' }, { transform: withBase(base, `rotate(${rotation}deg)`) }];
        case 'shake': {
            const amount = Math.max(2, animationNumber(animation.distancePercent, 18) / 4);
            return [0, -.2, .2, -.16, .16, -.08, .08, 0].map((factor, index, values) => { try { return (({
                offset: index / (values.length - 1), transform: withBase(base, `translateX(${amount * factor * 10}%)`)
            })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[0, -.2, .2, -.16, .16, -.08, .08, 0].map@4159', __javascriptError); throw __javascriptError; } });
        }
        case 'move':
            return [{ transform: base || 'none' }, { transform: translated }];
        default:
            return [{ opacity: 1 }, { opacity: 1 }];
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:publicationAnimationFrames@4115', __javascriptError); throw __javascriptError; }}
function runPublicationMediaAnimation(node, animation, delaySeconds = 0) { try {
    const effect = animationName(animation.effect);
    let timer = 0;
    let cancelled = false;
    let resolveFinished;
    const finished = new Promise(resolve => { try { resolveFinished = resolve;  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@4174', __javascriptError); throw __javascriptError; }});
    const execute = () => { try {
        if (cancelled) return;
        if (effect === 'playmedia') playPublicationMediaNode(node);
        else if (effect === 'pausemedia') pausePublicationMediaNode(node, false);
        else if (effect === 'stopmedia') pausePublicationMediaNode(node, true);
        resolveFinished();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:execute@4175', __javascriptError); throw __javascriptError; }};
    timer = setTimeout(execute, Math.max(0, delaySeconds) * 1000);
    return {
        finished,
        cancel() { try {
            cancelled = true;
            clearTimeout(timer);
            resolveFinished();
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:cancel@4185', __javascriptError); throw __javascriptError; }}
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:runPublicationMediaAnimation@4169', __javascriptError); throw __javascriptError; }}

function publicationAnimationGroupNodes(node) { try {
    const groupId = String(node?.dataset?.groupId || '').trim();
    const root = node?.closest?.('.publication-page,.print-page') || node?.parentElement;
    if (!groupId || !root) return [node];
    const peers = [...root.querySelectorAll('[data-publication-element][data-group-id]')]
        .filter(candidate => { try { return (String(candidate.dataset.groupId || '') === groupId); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...root.querySelectorAll(\'[data-publication-element][data-group-id]\')@4198', __javascriptError); throw __javascriptError; } });
    return peers.length ? peers : [node];
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:publicationAnimationGroupNodes@4193', __javascriptError); throw __javascriptError; }}

function publicationGroupTransformOrigins(nodes) { try {
    const rectangles = nodes.map(node => { try { return (({ node, rect: node.getBoundingClientRect(), previous: node.style.transformOrigin })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:nodes.map@4203', __javascriptError); throw __javascriptError; } });
    const left = Math.min(...rectangles.map(item => { try { return (item.rect.left); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:rectangles.map@4204', __javascriptError); throw __javascriptError; } }));
    const top = Math.min(...rectangles.map(item => { try { return (item.rect.top); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:rectangles.map@4205', __javascriptError); throw __javascriptError; } }));
    const right = Math.max(...rectangles.map(item => { try { return (item.rect.right); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:rectangles.map@4206', __javascriptError); throw __javascriptError; } }));
    const bottom = Math.max(...rectangles.map(item => { try { return (item.rect.bottom); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:rectangles.map@4207', __javascriptError); throw __javascriptError; } }));
    const centerX = (left + right) / 2;
    const centerY = (top + bottom) / 2;
    for (const item of rectangles)
        item.node.style.transformOrigin = `${centerX - item.rect.left}px ${centerY - item.rect.top}px`;
    return () => { try { return (rectangles.forEach(item => { try { return (item.node.style.transformOrigin = item.previous); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:rectangles.forEach@4212', __javascriptError); throw __javascriptError; } })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@4212', __javascriptError); throw __javascriptError; } };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:publicationGroupTransformOrigins@4202', __javascriptError); throw __javascriptError; }}

function publicationAnimationComposite(animations, restore) { try {
    let restored = false;
    const restoreOnce = () => { try {
        if (restored) return;
        restored = true;
        restore?.();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:restoreOnce@4217', __javascriptError); throw __javascriptError; }};
    return {
        finished: Promise.all(animations.map(animation => { try { return (animation.finished.catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@4223', __promiseError);  return (undefined); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:animation.finished.catch@4223', __javascriptError); throw __javascriptError; } })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:animations.map@4223', __javascriptError); throw __javascriptError; } })),
        cancel() { try { animations.forEach(animation => { try { try { animation.cancel(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@4224', __caughtJavaScriptError);  }  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:animations.forEach@4224', __javascriptError); throw __javascriptError; }}); restoreOnce();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:cancel@4224', __javascriptError); throw __javascriptError; }},
        pause() { try { animations.forEach(animation => { try { try { animation.pause(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@4225', __caughtJavaScriptError);  }  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:animations.forEach@4225', __javascriptError); throw __javascriptError; }});  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pause@4225', __javascriptError); throw __javascriptError; }},
        play() { try { animations.forEach(animation => { try { try { animation.play(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@4226', __caughtJavaScriptError);  }  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:animations.forEach@4226', __javascriptError); throw __javascriptError; }});  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:play@4226', __javascriptError); throw __javascriptError; }}
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:publicationAnimationComposite@4215', __javascriptError); throw __javascriptError; }}

function runPublicationAnimation(node, animation, delaySeconds = 0, baseTransforms = null) { try {
    if (isMediaAnimationEffect(animation.effect)) return runPublicationMediaAnimation(node, animation, delaySeconds);
    const reducedMotion = publicationReducedMotion();
    const duration = (reducedMotion ? .001 : Math.max(.05, animationNumber(animation.durationSeconds, .6))) * 1000;
    const repeat = Math.max(1, Math.round(animationNumber(animation.repeatCount, 1)));
    const iterations = reducedMotion ? 1 : repeat * (animation.autoReverse ? 2 : 1);
    const nodes = publicationAnimationGroupNodes(node);
    const restore = nodes.length > 1 ? publicationGroupTransformOrigins(nodes) : null;
    const animations = nodes.map(member => { try { return (member.animate(publicationAnimationFrames(member, animation, baseTransforms?.get?.(member) ?? null), {
            duration,
            delay: (reducedMotion ? 0 : Math.max(0, delaySeconds)) * 1000,
            easing: animationEasing(animation.easing),
            iterations,
            direction: animation.autoReverse ? 'alternate' : 'normal',
            fill: animationName(animation.phase) === 'entrance' ? 'both' : 'forwards'
        })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:nodes.map@4238', __javascriptError); throw __javascriptError; } });
    return animations.length === 1 ? animations[0] : publicationAnimationComposite(animations, restore);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:runPublicationAnimation@4230', __javascriptError); throw __javascriptError; }}
function publicationPageTransitionFrames(page, entering = true) { try {
    const kind = animationName(page.dataset.transitionKind);
    const direction = animationName(page.dataset.transitionDirection);
    const vector = animationDirectionVector(direction, 12);
    let frames;
    switch (kind) {
        case 'push': frames = [{ opacity: .4, transform: `translate(${vector.x}%,${vector.y}%)` }, { opacity: 1, transform: 'translate(0,0)' }]; break;
        case 'wipe': {
            const start = direction === 'right' ? 'inset(0 100% 0 0)' : direction === 'up' ? 'inset(100% 0 0 0)' : direction === 'down' ? 'inset(0 0 100% 0)' : 'inset(0 0 0 100%)';
            frames = [{ clipPath: start, opacity: .3 }, { clipPath: 'inset(0 0 0 0)', opacity: 1 }];
            break;
        }
        case 'zoom': frames = [{ opacity: 0, transform: 'scale(.86)' }, { opacity: 1, transform: 'scale(1)' }]; break;
        case 'flip': {
            const axis = direction === 'up' || direction === 'down' ? 'X' : 'Y';
            const sign = direction === 'right' || direction === 'down' ? 1 : -1;
            frames = [{ opacity: 0, transform: `perspective(1200px) rotate${axis}(${sign * 75}deg)` }, { opacity: 1, transform: `perspective(1200px) rotate${axis}(0deg)` }];
            break;
        }
        case 'none': frames = [{ opacity: 1 }, { opacity: 1 }]; break;
        default: frames = [{ opacity: 0 }, { opacity: 1 }]; break;
    }
    return entering ? frames : [...frames].reverse();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:publicationPageTransitionFrames@4248', __javascriptError); throw __javascriptError; }}
function runPublicationPageTransition(page, entering = true, target = page) { try {
    const duration = (publicationReducedMotion() ? .001 : Math.max(.1, animationNumber(page.dataset.transitionDuration, .55))) * 1000;
    return target.animate(publicationPageTransitionFrames(page, entering), {
        duration,
        easing: animationEasing(page.dataset.transitionEasing),
        fill: 'both'
    });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:runPublicationPageTransition@4272', __javascriptError); throw __javascriptError; }}
function animationItems(root) { try {
    return [...root.querySelectorAll('[data-publication-element]')].flatMap(node => { try {
        const animations = parsePublicationData(node.dataset.animations, []);
        return animations.map(animation => { try { return (({ node, animation })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:animations.map@4283', __javascriptError); throw __javascriptError; } });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...root.querySelectorAll(\'[data-publication-element]\')].flatMap@4281', __javascriptError); throw __javascriptError; }}).sort((left, right) => { try { return (animationNumber(left.animation.order, 0) - animationNumber(right.animation.order, 0)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...root.querySelectorAll(\'[data-publication-element]\')].flatMap(node @4284', __javascriptError); throw __javascriptError; } });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:animationItems@4280', __javascriptError); throw __javascriptError; }}
function clearPublicationPreview(key) { try {
    const state = publicationAnimationPreviews.get(key);
    if (!state) return;
    if (state.cleanupTimer) clearTimeout(state.cleanupTimer);
    for (const animation of state.animations) {
        try { animation.cancel(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@4291', __caughtJavaScriptError);  }
    }
    for (const timer of state.mediaTimers || []) clearTimeout(timer);
    for (const node of state.mediaNodes || []) pausePublicationMediaNode(node, true);
    if (state.clickTarget && state.clickHandler) state.clickTarget.removeEventListener('click', state.clickHandler, true);
    state.root?.classList.remove('pub-animation-previewing', 'pub-animation-click-hint');
    [...(state.snapshots?.values?.() || [])].reverse().forEach(restorePublicationPreviewSnapshot);
    state.snapshots?.clear?.();
    publicationAnimationPreviews.delete(key);
    if (state.root?.querySelector?.('.data-visual-view')) refreshDataVisualLayout(state.root.id || 'publisher-page');
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:clearPublicationPreview@4286', __javascriptError); throw __javascriptError; }}

function armPublicationPreviewCleanup(state) { try {
    if (!state || state.clickGroups?.length || state.root?.classList?.contains('pub-animation-click-hint')) return;
    const key = state.root.id || state.root;
    const animations = [...state.animations];
    Promise.all(animations.map(animation => { try { return (animation.finished?.catch?.((__promiseError) => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@4307', __promiseError);  return (undefined); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:animation.finished?.catch@4307', __javascriptError); throw __javascriptError; } }) ?? Promise.resolve()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:animations.map@4307', __javascriptError); throw __javascriptError; } }))
        .then(() => { try {
            if (publicationAnimationPreviews.get(key) !== state || state.clickGroups?.length) return;
            if (state.cleanupTimer) clearTimeout(state.cleanupTimer);
            state.cleanupTimer = setTimeout(() => { try {
                if (publicationAnimationPreviews.get(key) === state) clearPublicationPreview(key);
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:setTimeout@4311', __javascriptError); throw __javascriptError; }}, 120);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:Promise.all(animations.map(animation => animation.finished?.catch?.(()@4308', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:armPublicationPreviewCleanup@4303', __javascriptError); throw __javascriptError; }}
function schedulePublicationPreviewGroup(state, items, initialOffset = 0) { try {
    let previousStart = initialOffset;
    let previousEnd = initialOffset;
    for (const item of items) {
        const trigger = animationName(item.animation.trigger);
        const ownDelay = publicationReducedMotion() ? 0 : Math.max(0, animationNumber(item.animation.delaySeconds, 0));
        let start = initialOffset + ownDelay;
        const explicitStart = Number(item.animation.timelineStartSeconds);
        if (Number.isFinite(explicitStart)) start = Math.max(0, explicitStart);
        else if (trigger === 'withprevious') start = previousStart + ownDelay;
        else if (trigger === 'afterprevious') start = previousEnd + ownDelay;
        publicationAnimationGroupNodes(item.node).forEach(node => { try { return (capturePublicationPreviewNode(state, node)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:publicationAnimationGroupNodes(item.node).forEach@4327', __javascriptError); throw __javascriptError; } });
        if (isMediaAnimationEffect(item.animation.effect)) state.mediaNodes.add(item.node);
        state.animations.push(runPublicationAnimation(item.node, item.animation, start, state.baseTransforms));
        previousStart = start;
        previousEnd = start + publicationAnimationSpan(item.animation);
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:schedulePublicationPreviewGroup@4316', __javascriptError); throw __javascriptError; }}
function previewPublicationItems(root, items, includeTransition, transitionTarget = root) { try {
    clearPublicationPreview(root.id || root);
    const state = { root, animations: [], clickTarget: root, clickHandler: null, clickGroups: [], mediaTimers: [], mediaNodes: new Set(), cleanupTimer: 0, snapshots: new Map(), baseTransforms: new Map() };
    publicationAnimationPreviews.set(root.id || root, state);
    capturePublicationPreviewNode(state, root);
    capturePublicationPreviewNode(state, transitionTarget);
    root.classList.add('pub-animation-previewing');
    if (includeTransition) state.animations.push(runPublicationPageTransition(root, true, transitionTarget));

    const automatic = [];
    let currentClickGroup = null;
    for (const item of items) {
        const trigger = animationName(item.animation.trigger);
        if (trigger === 'onclick') {
            currentClickGroup = [item];
            state.clickGroups.push(currentClickGroup);
        } else if (trigger === 'onpageenter') {
            automatic.push(item);
            currentClickGroup = null;
        } else if (currentClickGroup) {
            currentClickGroup.push(item);
        } else {
            automatic.push(item);
        }
    }
    const transitionOffset = includeTransition && !publicationReducedMotion() ? animationNumber(root.dataset.transitionDuration, .55) : 0;
    schedulePublicationPreviewGroup(state, automatic, transitionOffset);
    if (includeTransition) schedulePublicationPreviewMedia(state, root, transitionOffset);

    const hasClickMedia = includeTransition && [...root.querySelectorAll('[data-media-kind]')]
        .some(node => { try { return (animationName(node.dataset.mediaTrigger) === 'onclick'); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...root.querySelectorAll(\'[data-media-kind]\')] .some@4364', __javascriptError); throw __javascriptError; } });
    if (state.clickGroups.length || hasClickMedia) {
        root.classList.add('pub-animation-click-hint');
        state.clickHandler = event => { try {
            const mediaNode = event.target.closest?.('[data-media-kind]');
            if (mediaNode && root.contains(mediaNode)) {
                if (animationName(mediaNode.dataset.mediaTrigger) === 'onclick') {
                    event.preventDefault();
                    event.stopImmediatePropagation();
                    capturePublicationPreviewNode(state, mediaNode);
                    state.mediaNodes.add(mediaNode);
                    togglePublicationMediaNode(mediaNode);
                    return;
                }
                if (event.target.closest?.('[data-media-control],video,audio')) return;
            }
            if (!state.clickGroups.length) return;
            event.preventDefault();
            event.stopImmediatePropagation();
            schedulePublicationPreviewGroup(state, state.clickGroups.shift(), 0);
            if (!state.clickGroups.length && !hasClickMedia) {
                root.classList.remove('pub-animation-click-hint');
                armPublicationPreviewCleanup(state);
            }
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:state.clickHandler@4367', __javascriptError); throw __javascriptError; }};
        root.addEventListener('click', state.clickHandler, true);
    }
    armPublicationPreviewCleanup(state);
    return state;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:previewPublicationItems@4334', __javascriptError); throw __javascriptError; }}
function previewPageAnimations(pageId) { try {
    const page = document.getElementById(pageId);
    if (!page) return;
    if (!page.__publisherSignalRuntime)
        page.__publisherSignalRuntime = signalConnectorRuntime(page, { autoStart: false, editor: true });
    page.__publisherSignalRuntime?.reset?.();
    previewPublicationItems(page, animationItems(page), true);
    page.__publisherSignalRuntime?.startPage?.(page);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:previewPageAnimations@4394', __javascriptError); throw __javascriptError; }}
function previewElementAnimations(elementId) { try {
    const node = document.getElementById(elementId);
    if (!node) return;
    const page = node.closest('.publication-page') || node;
    const animations = parsePublicationData(node.dataset.animations, []);
    previewPublicationItems(page, animations.map(animation => { try { return (({ node, animation })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:animations.map@4408', __javascriptError); throw __javascriptError; } }), false);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:previewElementAnimations@4403', __javascriptError); throw __javascriptError; }}
function previewAnimationStep(pageId, animationId) { try {
    const page = document.getElementById(pageId);
    if (!page) return;
    const item = animationItems(page).find(entry => { try { return (String(entry.animation.id).toLowerCase() === String(animationId).toLowerCase()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:animationItems(page).find@4413', __javascriptError); throw __javascriptError; } });
    if (item) previewPublicationItems(page, [item], false);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:previewAnimationStep@4410', __javascriptError); throw __javascriptError; }}
function stopAnimationPreview(pageId) { try {
    const page = document.getElementById(pageId);
    clearPublicationPreview(pageId);
    if (page) {
        clearPublicationPreview(page);
        page.__publisherSignalRuntime?.reset?.();
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:stopAnimationPreview@4416', __javascriptError); throw __javascriptError; }}


function publicationMediaElement(elementId) { try {
    const node = document.getElementById(elementId);
    return node?.querySelector('video,audio') || null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:publicationMediaElement@4426', __javascriptError); throw __javascriptError; }}
function publicationMediaSegments(node, media, number = animationNumber) { try {
    const sources = [...(media?.querySelectorAll?.('source[data-media-segment]') || [])]
        .map(source => { try {
            const src = source.getAttribute('src') || '';
            const start = Math.max(0, number(source.dataset.mediaTrimStart, 0));
            const end = Math.max(start + .01, number(source.dataset.mediaTrimEnd, start + 1));
            return { src, start, end, poster: source.dataset.mediaPoster || '', name: source.dataset.mediaName || '', originalSrc: source.dataset.publisherOriginalSrc || '' };
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...(media?.querySelectorAll?.(\'source[data-media-segment]\') || [])] .@4432', __javascriptError); throw __javascriptError; }})
        .filter(segment => { try { return (segment.src); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...(media?.querySelectorAll?.(\'source[data-media-segment]\') || [])] .@4438', __javascriptError); throw __javascriptError; } });
    if (sources.length) return sources;
    if (!media) return [];
    const src = media.getAttribute('src') || media.currentSrc || '';
    const start = Math.max(0, number(node?.dataset?.mediaTrimStart, 0));
    const end = Math.max(start + .01, number(node?.dataset?.mediaTrimEnd, media.duration || start + 1));
    return src ? [{ src, start, end, poster: media.getAttribute('poster') || '', name: '', originalSrc: media.dataset.publisherOriginalSrc || '' }] : [];
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:publicationMediaSegments@4430', __javascriptError); throw __javascriptError; }}
function publicationMediaSourceEquals(media, source) { try {
    if (!media || !source) return false;
    try { return new URL(media.currentSrc || media.getAttribute('src') || '', location.href).href === new URL(source, location.href).href; }
    catch { return (media.currentSrc || media.getAttribute('src') || '') === source; }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:publicationMediaSourceEquals@4446', __javascriptError); throw __javascriptError; }}
function waitForPublicationMediaMetadata(media) { try {
    if (!media || media.readyState >= 1) return Promise.resolve();
    return new Promise(resolve => { try {
        const timer = setTimeout(done, 5000);
        function done() { try {
            clearTimeout(timer);
            media.removeEventListener('loadedmetadata', done);
            media.removeEventListener('error', done);
            resolve();
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:done@4455', __javascriptError); throw __javascriptError; }}
        media.addEventListener('loadedmetadata', done, { once: true });
        media.addEventListener('error', done, { once: true });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@4453', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:waitForPublicationMediaMetadata@4451', __javascriptError); throw __javascriptError; }}
function clearPublicationMediaSequence(media) { try {
    if (!media) return;
    const state = media.__publisherSequenceState;
    if (media.__publisherTimeHandler) media.removeEventListener('timeupdate', media.__publisherTimeHandler);
    media.__publisherTimeHandler = null;
    if (state) {
        state.token = (state.token || 0) + 1;
        state.advancing = false;
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:clearPublicationMediaSequence@4465', __javascriptError); throw __javascriptError; }}
function configurePublicationMedia(node, media, requestedIndex = 0, autoPlay = false) { try {
    if (!node || !media) return null;
    clearPublicationMediaSequence(media);
    const segments = publicationMediaSegments(node, media);
    if (!segments.length) return null;
    const index = Math.max(0, Math.min(segments.length - 1, Number(requestedIndex) || 0));
    const segment = segments[index];
    const rate = Math.max(.1, animationNumber(node.dataset.mediaRate, 1));
    const baseVolume = Math.max(0, Math.min(1, animationNumber(node.dataset.mediaVolume, 1)));
    const fadeIn = Math.max(0, animationNumber(node.dataset.mediaFadeIn, 0));
    const fadeOut = Math.max(0, animationNumber(node.dataset.mediaFadeOut, 0));
    const elapsedBefore = segments.slice(0, index).reduce((total, item) => { try { return (total + Math.max(.01, item.end - item.start) / rate); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:segments.slice(0, index).reduce@4486', __javascriptError); throw __javascriptError; } }, 0);
    const totalDuration = segments.reduce((total, item) => { try { return (total + Math.max(.01, item.end - item.start) / rate); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:segments.reduce@4487', __javascriptError); throw __javascriptError; } }, 0);
    const state = media.__publisherSequenceState || { token: 0, index: 0, advancing: false };
    state.index = index;
    state.segments = segments;
    state.token = (state.token || 0) + 1;
    state.advancing = false;
    media.__publisherSequenceState = state;
    const token = state.token;
    media.playbackRate = rate;
    media.muted = node.dataset.mediaMuted === 'true';
    media.loop = false;
    media.volume = fadeIn > 0 && index === 0 ? 0 : baseVolume;
    if (media instanceof HTMLVideoElement && segment.poster) media.poster = segment.poster;

    const prepare = async () => { try {
        if (media.__publisherFallbackHandler) media.removeEventListener('error', media.__publisherFallbackHandler);
        media.__publisherFallbackHandler = null;
        if (segment.originalSrc) {
            const fallbackHandler = async () => { try {
                if (state.token !== token || publicationMediaSourceEquals(media, segment.originalSrc)) return;
                media.removeEventListener('error', fallbackHandler);
                media.__publisherFallbackHandler = null;
                media.pause();
                media.src = segment.originalSrc;
                media.load();
                await waitForPublicationMediaMetadata(media);
                if (state.token !== token) return;
                try { media.currentTime = segment.start; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@4514', __caughtJavaScriptError);  }
                if (autoPlay) media.play().catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@4515', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:media.play().catch@4515', __javascriptError); throw __javascriptError; }});
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:fallbackHandler@4505', __javascriptError); throw __javascriptError; }};
            media.__publisherFallbackHandler = fallbackHandler;
            media.addEventListener('error', fallbackHandler, { once: true });
        }
        if (!publicationMediaSourceEquals(media, segment.src)) {
            media.pause();
            media.src = segment.src;
            media.load();
            await waitForPublicationMediaMetadata(media);
        }
        if (state.token !== token) return;
        try { media.currentTime = segment.start; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@4527', __caughtJavaScriptError);  }
        const handler = () => { try {
            if (state.token !== token || state.advancing) return;
            const presentationPosition = elapsedBefore + Math.max(0, media.currentTime - segment.start) / rate;
            const presentationRemaining = Math.max(0, totalDuration - presentationPosition);
            let gain = baseVolume;
            if (fadeIn > 0) gain *= Math.max(0, Math.min(1, presentationPosition / fadeIn));
            if (fadeOut > 0) gain *= Math.max(0, Math.min(1, presentationRemaining / fadeOut));
            if (!media.muted) media.volume = Math.max(0, Math.min(1, gain));
            if (media.currentTime < segment.end - .02) return;
            state.advancing = true;
            if (index + 1 < segments.length) configurePublicationMedia(node, media, index + 1, true);
            else if (node.dataset.mediaLoop === 'true') configurePublicationMedia(node, media, 0, true);
            else {
                media.pause();
                state.advancing = false;
            }
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handler@4528', __javascriptError); throw __javascriptError; }};
        media.__publisherTimeHandler = handler;
        media.addEventListener('timeupdate', handler);
        if (autoPlay) media.play().catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@4547', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:media.play().catch@4547', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:prepare@4501', __javascriptError); throw __javascriptError; }};
    void prepare();
    return { start: segment.start, end: segment.end, index, segments };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:configurePublicationMedia@4475', __javascriptError); throw __javascriptError; }}
function playPublicationMediaNode(node) { try {
    const media = node?.querySelector('video,audio');
    if (!node || !media) return;
    configurePublicationMedia(node, media, 0, true);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:playPublicationMediaNode@4552', __javascriptError); throw __javascriptError; }}
function pausePublicationMediaNode(node, rewind = false) { try {
    const media = node?.querySelector('video,audio');
    if (!media) return;
    media.pause();
    if (rewind) configurePublicationMedia(node, media, 0, false);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pausePublicationMediaNode@4557', __javascriptError); throw __javascriptError; }}
function togglePublicationMediaNode(node) { try {
    const media = node?.querySelector('video,audio');
    if (!media) return;
    if (media.paused) {
        const state = media.__publisherSequenceState;
        if (state?.segments?.length) media.play().catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@4568', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:media.play().catch@4568', __javascriptError); throw __javascriptError; }});
        else playPublicationMediaNode(node);
    } else media.pause();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:togglePublicationMediaNode@4563', __javascriptError); throw __javascriptError; }}
function schedulePublicationPreviewMedia(state, root, initialOffset = 0) { try {
    for (const node of root.querySelectorAll('[data-media-kind]')) {
        const trigger = animationName(node.dataset.mediaTrigger);
        if (node.dataset.mediaAutoplay === 'false' || trigger === 'onclick') continue;
        const delay = Math.max(0, initialOffset + animationNumber(node.dataset.mediaStart, 0));
        capturePublicationPreviewNode(state, node);
        state.mediaNodes.add(node);
        if (publicationReducedMotion() || delay <= 0) playPublicationMediaNode(node);
        else state.mediaTimers.push(setTimeout(() => { try { return (playPublicationMediaNode(node)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:setTimeout@4580', __javascriptError); throw __javascriptError; } }, delay * 1000));
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:schedulePublicationPreviewMedia@4572', __javascriptError); throw __javascriptError; }}
function playPublicationMedia(elementId) { try {
    playPublicationMediaNode(document.getElementById(elementId));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:playPublicationMedia@4583', __javascriptError); throw __javascriptError; }}
function pausePublicationMedia(elementId) { try {
    pausePublicationMediaNode(document.getElementById(elementId));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pausePublicationMedia@4586', __javascriptError); throw __javascriptError; }}

function signalConnectorRuntime(root = document, options = {}) { try {
    const host = typeof root === 'string' ? document.getElementById(root) : (root || document);
    if (!host) return null;
    const lower = value => { try { return (String(value || '').replace(/[^a-z0-9]/gi, '').toLowerCase()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:lower@4593', __javascriptError); throw __javascriptError; } };
    const num = (value, fallback = 0) => { try { const parsed = Number(value); return Number.isFinite(parsed) ? parsed : fallback;  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:num@4594', __javascriptError); throw __javascriptError; }};
    const wait = milliseconds => { try { return (new Promise(resolve => { try { return (setTimeout(resolve, Math.max(0, milliseconds))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@4595', __javascriptError); throw __javascriptError; } })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:wait@4595', __javascriptError); throw __javascriptError; } };
    const parse = value => { try { try { return JSON.parse(value || '{}'); } catch { return {}; }  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:parse@4596', __javascriptError); throw __javascriptError; }};
    const bool = value => { try { return (value === true || String(value).toLowerCase() === 'true'); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:bool@4597', __javascriptError); throw __javascriptError; } };
    const reducedMotion = () => { try { return (typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:reducedMotion@4598', __javascriptError); throw __javascriptError; } };
    const cssEscape = value => { try { return (globalThis.CSS?.escape ? CSS.escape(String(value)) : String(value).replace(/["\\]/g, '\\$&')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:cssEscape@4599', __javascriptError); throw __javascriptError; } };
    const controllers = new Map();
    const bindings = [];
    const snapshots = new Map();
    const effects = new Map();
    const timers = new Set();
    const pointerStates = new Map();
    let observer = null;
    let disposed = false;

    const pageFor = connector => { try { return (connector?.closest?.('[data-page-id],.print-page,.publication-page') || host); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pageFor@4609', __javascriptError); throw __javascriptError; } };
    const connectorId = connector => { try { return (String(connector?.dataset?.connectorId || connector?.dataset?.elementId || connector?.id || '').replace(/^element-/, '')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:connectorId@4610', __javascriptError); throw __javascriptError; } };
    const elementById = (page, id) => { try { return (id ? page.querySelector(`[data-element-id="${cssEscape(id)}"]`) || document.getElementById(`element-${id}`) : null); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:elementById@4611', __javascriptError); throw __javascriptError; } };
    const selectInside = (owner, selector) => { try {
        if (!owner || !String(selector || '').trim()) return owner;
        try { return owner.matches?.(selector) ? owner : owner.querySelector(selector); } catch { return owner; }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:selectInside@4612', __javascriptError); throw __javascriptError; }};
    const pointTarget = (connector, prefix) => { try {
        const page = pageFor(connector);
        const kind = lower(connector.dataset[`${prefix}Kind`] || 'element');
        const selector = connector.dataset[`${prefix}Selector`] || '';
        if (kind !== 'canvas') {
            const owner = elementById(page, connector.dataset[`${prefix}ElementId`]);
            return selectInside(owner, selector);
        }
        const svg = connector;
        const viewBox = svg.viewBox?.baseVal;
        const rect = svg.getBoundingClientRect();
        if (!rect.width || !rect.height) return page;
        const x = num(connector.dataset[`${prefix}X`]);
        const y = num(connector.dataset[`${prefix}Y`]);
        const clientX = rect.left + (x - (viewBox?.x || 0)) / Math.max(.001, viewBox?.width || rect.width) * rect.width;
        const clientY = rect.top + (y - (viewBox?.y || 0)) / Math.max(.001, viewBox?.height || rect.height) * rect.height;
        const previous = connector.style.pointerEvents;
        connector.style.pointerEvents = 'none';
        let target = document.elementFromPoint(clientX, clientY);
        connector.style.pointerEvents = previous;
        if (target && !page.contains(target)) target = page;
        return selectInside(target || page, selector);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pointTarget@4616', __javascriptError); throw __javascriptError; }};
    const configuredTarget = (connector, settings, mode) => { try {
        const page = pageFor(connector);
        const id = mode === 'motion' ? settings.motionTargetElementId : settings.completionTargetElementId;
        const selector = mode === 'motion' ? settings.motionTargetSelector : settings.completionTargetSelector;
        const owner = elementById(page, id) || (mode === 'completion' ? pointTarget(connector, 'target') : null);
        if (mode === 'motion' && owner && !String(selector || '').trim()) {
            const viewport = owner.matches?.('[data-content-viewport]') ? owner : owner.querySelector?.('[data-content-viewport]');
            const source = viewport?.querySelector?.(':scope > [data-content-fit-source]');
            if (source) return source;
        }
        return selectInside(owner, selector);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:configuredTarget@4639', __javascriptError); throw __javascriptError; }};
    const waitForTarget = async (resolve, signal, timeout = num(options.targetWaitMilliseconds, 2000)) => { try {
        const started = performance.now();
        let target = resolve();
        while (!target && !signal?.aborted && performance.now() - started < Math.max(0, timeout)) {
            await wait(40);
            target = resolve();
        }
        return target;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:waitForTarget@4651', __javascriptError); throw __javascriptError; }};
    const attribute = (node, name) => { try { return (node?.hasAttribute?.(name) ? node.getAttribute(name) : null); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:attribute@4660', __javascriptError); throw __javascriptError; } };
    const capture = node => { try {
        if (!node || snapshots.has(node)) return;
        const media = node.matches?.('video,audio') ? node : null;
        snapshots.set(node, {
            node,
            style: attribute(node, 'style'),
            className: attribute(node, 'class'),
            hidden: attribute(node, 'hidden'),
            signalOpacity: attribute(node, 'data-publisher-signal-opacity'),
            media: media ? {
                currentTime: Number.isFinite(media.currentTime) ? media.currentTime : 0,
                paused: media.paused,
                volume: media.volume,
                playbackRate: media.playbackRate,
                muted: media.muted,
                loop: media.loop
            } : null
        });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:capture@4661', __javascriptError); throw __javascriptError; }};
    const restoreAttribute = (node, name, value) => { try {
        if (value === null) node.removeAttribute(name); else node.setAttribute(name, value);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:restoreAttribute@4680', __javascriptError); throw __javascriptError; }};
    const restoreSnapshot = snapshot => { try {
        const node = snapshot?.node;
        if (!node?.isConnected) return;
        restoreAttribute(node, 'style', snapshot.style);
        restoreAttribute(node, 'class', snapshot.className);
        restoreAttribute(node, 'hidden', snapshot.hidden);
        restoreAttribute(node, 'data-publisher-signal-opacity', snapshot.signalOpacity);
        if (!snapshot.media || !node.matches?.('video,audio')) return;
        node.pause();
        node.volume = snapshot.media.volume;
        node.playbackRate = snapshot.media.playbackRate;
        node.muted = snapshot.media.muted;
        node.loop = snapshot.media.loop;
        try { node.currentTime = snapshot.media.currentTime; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@4696', __caughtJavaScriptError);  }
        if (!snapshot.media.paused) node.play().catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@4697', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:node.play().catch@4697', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:restoreSnapshot@4683', __javascriptError); throw __javascriptError; }};
    const trackEffect = (animation, target, persistent = false) => { try {
        if (!animation) return animation;
        effects.set(animation, target || null);
        animation.finished?.catch?.((__promiseError) => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@4702', __promiseError);  return (undefined); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:animation.finished?.catch@4702', __javascriptError); throw __javascriptError; } }).finally?.(() => { try {
            if (!persistent) effects.delete(animation);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:animation.finished?.catch?.(() => undefined).finally@4702', __javascriptError); throw __javascriptError; }});
        return animation;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:trackEffect@4699', __javascriptError); throw __javascriptError; }};
    const schedule = (callback, milliseconds) => { try {
        const timer = setTimeout(() => { try { timers.delete(timer); callback();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:setTimeout@4708', __javascriptError); throw __javascriptError; }}, Math.max(0, milliseconds));
        timers.add(timer);
        return timer;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:schedule@4707', __javascriptError); throw __javascriptError; }};
    const dispatchGesture = (target, gesture) => { try {
        if (!target) return;
        capture(target);
        const kind = lower(gesture);
        const init = { bubbles: true, cancelable: true, composed: true, view: window };
        const dispatch = type => { try {
            let event;
            try { event = type.startsWith('pointer') ? new PointerEvent(type, init) : new MouseEvent(type, init); }
            catch { event = new MouseEvent(type, init); }
            try { Object.defineProperty(event, 'publisherSignalSynthetic', { value: true }); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@4721', __caughtJavaScriptError);  }
            target.dispatchEvent(event);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:dispatch@4717', __javascriptError); throw __javascriptError; }};
        if (kind === 'click') {
            for (const type of ['pointerdown', 'mousedown', 'pointerup', 'mouseup', 'click']) dispatch(type);
        } else if (kind === 'hover') {
            for (const type of ['pointerover', 'mouseover', 'pointerenter', 'mouseenter']) dispatch(type);
            target.classList.add('ps-signal-hover');
            schedule(() => { try {
                for (const type of ['pointerout', 'mouseout', 'pointerleave', 'mouseleave']) dispatch(type);
                target.classList.remove('ps-signal-hover');
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:schedule@4729', __javascriptError); throw __javascriptError; }}, 800);
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:dispatchGesture@4712', __javascriptError); throw __javascriptError; }};
    const replayAnimations = target => { try {
        if (!target) return;
        capture(target);
        try {
            target.getAnimations?.({ subtree: true }).forEach(animation => { try { animation.cancel(); animation.play();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:target.getAnimations?.({ subtree: true }).forEach@4739', __javascriptError); throw __javascriptError; }});
            target.dispatchEvent(new CustomEvent('publisher:replay-animation', { bubbles: true, detail: { target } }));
        } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@4741', __caughtJavaScriptError);  }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:replayAnimations@4735', __javascriptError); throw __javascriptError; }};
    const animateOpacity = async (target, from, to, duration, signal) => { try {
        if (!target) return;
        capture(target);
        if (!target.animate || reducedMotion()) { target.style.opacity = String(to); return; }
        const animation = trackEffect(target.animate([{ opacity: from }, { opacity: to }], {
            duration: Math.max(10, duration * 1000), easing: 'cubic-bezier(.4,0,.2,1)', fill: 'forwards'
        }), target, false);
        signal?.addEventListener?.('abort', () => { try { return (animation.cancel()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:signal?.addEventListener@4750', __javascriptError); throw __javascriptError; } }, { once: true });
        try { await animation.finished; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@4751', __caughtJavaScriptError);  }
        if (!signal?.aborted) target.style.opacity = String(to);
        animation.cancel();
        effects.delete(animation);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:animateOpacity@4743', __javascriptError); throw __javascriptError; }};
    const setVisibility = async (target, visible, duration, signal) => { try {
        if (!target) return;
        capture(target);
        if (visible) {
            target.classList.remove('ps-action-hidden');
            target.hidden = false;
            target.style.visibility = '';
            const destination = Math.max(0, Math.min(1, num(target.dataset.publisherSignalOpacity, 1)));
            target.style.opacity = '0';
            await animateOpacity(target, 0, destination, duration, signal);
        } else {
            const start = Math.max(0, Math.min(1, num(getComputedStyle(target).opacity, 1)));
            target.dataset.publisherSignalOpacity = String(start || 1);
            await animateOpacity(target, start, 0, duration, signal);
            if (!signal?.aborted) target.classList.add('ps-action-hidden');
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:setVisibility@4756', __javascriptError); throw __javascriptError; }};
    const applyCompletion = async (connector, settings, signal, chain) => { try {
        const action = lower(settings.completionAction);
        if (!action || action === 'none') return;
        if (action === 'runsignal') {
            const next = settings.completionValue || settings.nextConnectorId;
            if (next) await run(next, { chained: true, signal, chain });
            return;
        }
        const target = await waitForTarget(() => { try { return (configuredTarget(connector, settings, 'completion')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:waitForTarget@4781', __javascriptError); throw __javascriptError; } }, signal);
        if (!target) return;
        capture(target);
        const duration = Math.max(.01, num(settings.completionDurationSeconds, .8));
        const value = String(settings.completionValue || '').trim();
        if (action === 'click' || action === 'hover') dispatchGesture(target, action);
        else if (action === 'show') await setVisibility(target, true, duration, signal);
        else if (action === 'hide') await setVisibility(target, false, duration, signal);
        else if (action === 'togglevisibility') await setVisibility(target, target.classList.contains('ps-action-hidden') || target.hidden, duration, signal);
        else if (action === 'setopacity') {
            const start = Math.max(0, Math.min(1, num(getComputedStyle(target).opacity, 1)));
            const destination = Math.max(0, Math.min(1, num(value, 1)));
            await animateOpacity(target, start, destination, duration, signal);
        }
        else if (action === 'replayanimation') replayAnimations(target);
        else if (action === 'playmedia') {
            const media = target.matches?.('video,audio') ? target : target.querySelector?.('video,audio');
            if (media) { capture(media); media.play?.().catch?.((__promiseError) => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@4798', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:media.play?.().catch@4798', __javascriptError); throw __javascriptError; }}); }
        }
        else if (action === 'pausemedia') {
            const media = target.matches?.('video,audio') ? target : target.querySelector?.('video,audio');
            if (media) { capture(media); media.pause?.(); }
        }
        else if (action === 'togglemediaplayback') {
            const media = target.matches?.('video,audio') ? target : target.querySelector?.('video,audio');
            if (media) { capture(media); media.paused ? media.play().catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@4806', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:media.play().catch@4806', __javascriptError); throw __javascriptError; }}) : media.pause(); }
        } else if (action === 'highlight') {
            const color = value || '#facc15';
            const animation = trackEffect(target.animate?.([
                { outline: `0 solid ${color}`, boxShadow: `0 0 0 0 ${color}00` },
                { outline: `4px solid ${color}`, boxShadow: `0 0 0 8px ${color}55`, offset: .35 },
                { outline: `0 solid ${color}`, boxShadow: `0 0 0 0 ${color}00` }
            ], { duration: duration * 1000, easing: 'ease-in-out' }), target, false);
            try { await animation?.finished; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@4814', __caughtJavaScriptError);  }
        } else if (action === 'addcssclass' && value) target.classList.add(...value.split(/\s+/).filter(Boolean));
        else if (action === 'removecssclass' && value) target.classList.remove(...value.split(/\s+/).filter(Boolean));
        else if (action === 'togglecssclass' && value) value.split(/\s+/).filter(Boolean).forEach(name => { try { return (target.classList.toggle(name)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:value.split(/\\s+/).filter(Boolean).forEach@4817', __javascriptError); throw __javascriptError; } });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:applyCompletion@4773', __javascriptError); throw __javascriptError; }};
    const animateMotion = (connector, settings, signal, resolvedTarget = null) => { try {
        const target = resolvedTarget || configuredTarget(connector, settings, 'motion');
        if (!target?.animate) return null;
        capture(target);
        const x = num(settings.translateXPercent);
        const y = num(settings.translateYPercent);
        const scale = Math.max(.01, num(settings.scale, 1));
        const resizeWidth = Math.max(.01, num(settings.resizeWidthPercent, 100));
        const resizeHeight = Math.max(.01, num(settings.resizeHeightPercent, 100));
        const rotation = num(settings.rotationDegrees);
        const opacity = Math.max(0, Math.min(1, num(settings.opacity, 1)));
        const resizeRequested = Math.abs(resizeWidth - 100) >= .001 || Math.abs(resizeHeight - 100) >= .001;
        if (Math.abs(x) < .001 && Math.abs(y) < .001 && Math.abs(scale - 1) < .001 && !resizeRequested && Math.abs(rotation) < .001 && Math.abs(opacity - 1) < .001) return null;
        const computed = getComputedStyle(target);
        const base = computed.transform === 'none' ? '' : computed.transform;
        const destination = `${base} translate(${x}%,${y}%) scale(${scale}) rotate(${rotation}deg)`.trim();
        const startFrame = { transform: base || 'none', opacity: computed.opacity };
        const endFrame = { transform: destination, opacity };
        if (resizeRequested) {
            const rect = target.getBoundingClientRect();
            const width = Math.max(.01, rect.width);
            const height = Math.max(.01, rect.height);
            startFrame.width = `${width}px`;
            startFrame.height = `${height}px`;
            endFrame.width = `${width * resizeWidth / 100}px`;
            endFrame.height = `${height * resizeHeight / 100}px`;
        }
        const duration = Math.max(.05, num(settings.durationSeconds, 1.5)) * 1000;
        const loop = bool(settings.loop) && options.finiteLoops !== true;
        const repeatCount = Math.max(1, Math.round(num(settings.repeatCount, 1)));
        const iterations = loop ? Infinity : repeatCount * (bool(settings.autoReverse) ? 2 : 1);
        const restoreAfterRun = bool(settings.restoreMotionAfterRun);
        const animation = trackEffect(target.animate([startFrame, endFrame], {
            duration,
            iterations,
            direction: bool(settings.autoReverse) ? 'alternate' : 'normal',
            easing: 'cubic-bezier(.4,0,.2,1)',
            fill: restoreAfterRun ? 'none' : 'forwards'
        }), target, !restoreAfterRun);
        signal?.addEventListener?.('abort', () => { try { return (animation.cancel()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:signal?.addEventListener@4858', __javascriptError); throw __javascriptError; } }, { once: true });
        return animation;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:animateMotion@4819', __javascriptError); throw __javascriptError; }};
    const animateVisual = async (connector, settings, signal) => { try {
        const visible = settings.lineVisible !== false && connector.dataset.signalLineVisible !== 'false';
        const visual = lower(settings.visual || 'flyingarrow');
        const path = connector.querySelector('.connector-line');
        const duration = Math.max(.05, num(settings.durationSeconds, 1.5)) * 1000;
        const repeatCount = Math.max(1, Math.round(num(settings.repeatCount, 1)));
        const iterations = bool(settings.loop) && options.finiteLoops !== true ? Infinity : repeatCount * (bool(settings.autoReverse) ? 2 : 1);
        const direction = bool(settings.autoReverse) ? 'alternate' : 'normal';
        if (!visible || visual === 'none' || !path) { await wait(duration * (Number.isFinite(iterations) ? iterations : 1)); return; }
        if (visual === 'drawpath' && path.animate) {
            capture(path);
            const length = path.getTotalLength?.() || 100;
            const previousDash = path.style.strokeDasharray;
            const previousOffset = path.style.strokeDashoffset;
            const animation = trackEffect(path.animate([
                { strokeDasharray: `${length} ${length}`, strokeDashoffset: length },
                { strokeDasharray: `${length} ${length}`, strokeDashoffset: 0 }
            ], { duration, iterations, direction, easing: 'linear' }), path, false);
            signal?.addEventListener?.('abort', () => { try { return (animation.cancel()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:signal?.addEventListener@4879', __javascriptError); throw __javascriptError; } }, { once: true });
            try { await animation.finished; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@4880', __caughtJavaScriptError);  }
            path.style.strokeDasharray = previousDash; path.style.strokeDashoffset = previousOffset;
            return;
        }
        if (visual === 'pulse' && path.animate) {
            capture(path);
            const animation = trackEffect(path.animate([
                { opacity: .3, filter: 'drop-shadow(0 0 0 currentColor)' },
                { opacity: 1, filter: 'drop-shadow(0 0 5px currentColor)' },
                { opacity: .3, filter: 'drop-shadow(0 0 0 currentColor)' }
            ], { duration, iterations, direction, easing: 'ease-in-out' }), path, false);
            signal?.addEventListener?.('abort', () => { try { return (animation.cancel()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:signal?.addEventListener@4891', __javascriptError); throw __javascriptError; } }, { once: true });
            try { await animation.finished; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@4892', __caughtJavaScriptError);  }
            return;
        }
        const ns = 'http://www.w3.org/2000/svg';
        const runner = document.createElementNS(ns, 'g');
        runner.classList.add('publisher-signal-runner');
        const halo = document.createElementNS(ns, 'circle'); halo.setAttribute('r', '2.4'); halo.setAttribute('fill', 'none'); halo.setAttribute('stroke', connector.dataset.stroke || '#0ea5e9'); halo.setAttribute('stroke-width', '1.1');
        const arrow = document.createElementNS(ns, 'path'); arrow.setAttribute('d', 'M -4 -2.7 L 3.5 0 L -4 2.7 L -2.2 0 Z'); arrow.setAttribute('fill', connector.dataset.stroke || '#0ea5e9');
        runner.append(halo, arrow); connector.appendChild(runner);
        const length = Math.max(.001, path.getTotalLength?.() || 1);
        const started = performance.now();
        const finiteIterations = Number.isFinite(iterations) ? iterations : Number.MAX_SAFE_INTEGER;
        await new Promise(resolve => { try {
            const tick = now => { try {
                if (signal?.aborted || disposed) { resolve(); return; }
                const elapsed = now - started;
                const iteration = Math.floor(elapsed / duration);
                if (iteration >= finiteIterations) { resolve(); return; }
                let progress = (elapsed % duration) / duration;
                if (direction === 'alternate' && iteration % 2 === 1) progress = 1 - progress;
                const point = path.getPointAtLength(progress * length);
                const ahead = path.getPointAtLength(Math.min(length, progress * length + Math.max(.1, length / 200)));
                const angle = Math.atan2(ahead.y - point.y, ahead.x - point.x) * 180 / Math.PI;
                runner.setAttribute('transform', `translate(${point.x} ${point.y}) rotate(${angle})`);
                requestAnimationFrame(tick);
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:tick@4905', __javascriptError); throw __javascriptError; }};
            requestAnimationFrame(tick);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@4904', __javascriptError); throw __javascriptError; }});
        runner.remove();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:animateVisual@4861', __javascriptError); throw __javascriptError; }};
    async function run(idOrNode, runOptions = {}) { try {
        if (disposed) return false;
        const connector = typeof idOrNode === 'string'
            ? host.querySelector(`[data-connector-id="${cssEscape(String(idOrNode).replace(/^element-/, ''))}"]`) || document.getElementById(String(idOrNode))
            : idOrNode;
        if (!connector) return false;
        const settings = parse(connector.dataset.signal);
        if (!bool(settings.enabled) && connector.dataset.signalEnabled !== 'true') return false;
        const id = connectorId(connector);
        const chain = runOptions.chain instanceof Set ? new Set(runOptions.chain) : new Set();
        if (chain.has(id)) {
            console.warn(`PublisherStudio stopped a circular signal chain at ${id}.`);
            return false;
        }
        chain.add(id);
        controllers.get(id)?.abort();
        const controller = new AbortController();
        runOptions.signal?.addEventListener?.('abort', () => { try { return (controller.abort()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:runOptions.signal?.addEventListener@4939', __javascriptError); throw __javascriptError; } }, { once: true });
        controllers.set(id, controller);
        const signal = controller.signal;
        capture(connector);
        connector.classList.add('ps-signal-running');
        try {
            await wait(Math.max(0, num(settings.delaySeconds)) * 1000);
            if (signal.aborted) return false;
            const startTarget = lower(settings.startGesture) !== 'none'
                ? await waitForTarget(() => { try { return (pointTarget(connector, 'source')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:waitForTarget@4948', __javascriptError); throw __javascriptError; } }, signal)
                : null;
            dispatchGesture(startTarget, settings.startGesture);
            const motionTarget = settings.motionTargetElementId
                ? await waitForTarget(() => { try { return (configuredTarget(connector, settings, 'motion')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:waitForTarget@4952', __javascriptError); throw __javascriptError; } }, signal)
                : null;
            const motion = animateMotion(connector, settings, signal, motionTarget);
            await animateVisual(connector, settings, signal);
            try { await motion?.finished; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@4956', __caughtJavaScriptError);  }
            if (signal.aborted) return false;
            const endTarget = lower(settings.endGesture) !== 'none'
                ? await waitForTarget(() => { try { return (pointTarget(connector, 'target')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:waitForTarget@4959', __javascriptError); throw __javascriptError; } }, signal)
                : null;
            dispatchGesture(endTarget, settings.endGesture);
            await applyCompletion(connector, settings, signal, chain);
            const next = settings.nextConnectorId;
            if (next && lower(settings.completionAction) !== 'runsignal') await run(next, { chained: true, signal, chain });
            connector.dispatchEvent(new CustomEvent('publisher:signal-complete', { bubbles: true, detail: { connectorId: id } }));
            return true;
        } finally {
            connector.classList.remove('ps-signal-running');
            if (controllers.get(id) === controller) controllers.delete(id);
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:run@4922', __javascriptError); throw __javascriptError; }}
    const abort = id => { try {
        if (id) { controllers.get(String(id).replace(/^element-/, ''))?.abort(); return; }
        controllers.forEach(controller => { try { return (controller.abort()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:controllers.forEach@4974', __javascriptError); throw __javascriptError; } });
        controllers.clear();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:abort@4972', __javascriptError); throw __javascriptError; }};
    const reset = () => { try {
        abort();
        effects.forEach((_, animation) => { try { try { animation.cancel(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@4979', __caughtJavaScriptError);  }  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:effects.forEach@4979', __javascriptError); throw __javascriptError; }});
        effects.clear();
        timers.forEach(timer => { try { return (clearTimeout(timer)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:timers.forEach@4981', __javascriptError); throw __javascriptError; } });
        timers.clear();
        host.querySelectorAll('.publisher-signal-runner').forEach(node => { try { return (node.remove()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:host.querySelectorAll(\'.publisher-signal-runner\').forEach@4983', __javascriptError); throw __javascriptError; } });
        [...snapshots.values()].reverse().forEach(restoreSnapshot);
        snapshots.clear();
        host.querySelectorAll('.ps-signal-running,.ps-signal-hover').forEach(node => { try { return (node.classList.remove('ps-signal-running', 'ps-signal-hover')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:host.querySelectorAll(\'.ps-signal-running,.ps-signal-hover\').forEach@4986', __javascriptError); throw __javascriptError; } });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:reset@4977', __javascriptError); throw __javascriptError; }};
    const stop = id => { try { if (id) abort(id); else reset();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:stop@4988', __javascriptError); throw __javascriptError; }};
    const signalsIn = page => { try { return ([...(page || host).querySelectorAll('[data-signal-enabled="true"][data-connector-id]')]); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:signalsIn@4989', __javascriptError); throw __javascriptError; } };
    const startPage = page => { try {
        const current = typeof page === 'string' ? host.querySelector(`[data-page-id="${cssEscape(page)}"]`) : page;
        reset();
        signalsIn(current || host).forEach(connector => { try {
            const settings = parse(connector.dataset.signal);
            if (lower(settings.trigger) === 'onpageenter') void run(connector);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:signalsIn(current || host).forEach@4993', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:startPage@4990', __javascriptError); throw __javascriptError; }};
    const sourceContainsEvent = (connector, event) => { try {
        const settings = parse(connector.dataset.signal);
        const source = pointTarget(connector, 'source') || connector;
        const path = event.composedPath?.() || [];
        const onSource = source === event.target || source.contains?.(event.target) || path.includes(source);
        const onVisibleLine = settings.lineVisible !== false && connector.dataset.signalLineVisible !== 'false'
            && (connector === event.target || connector.contains?.(event.target) || path.includes(connector));
        return onSource || onVisibleLine ? source : null;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:sourceContainsEvent@4998', __javascriptError); throw __javascriptError; }};
    const handleTrigger = (event, trigger) => { try {
        if (event?.publisherSignalSynthetic) return;
        signalsIn(host).forEach(connector => { try {
            const settings = parse(connector.dataset.signal);
            if (lower(settings.trigger) !== trigger) return;
            const source = sourceContainsEvent(connector, event);
            if (!source) return;
            if (trigger === 'onhover' && event.relatedTarget && source.contains?.(event.relatedTarget)) return;
            void run(connector);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:signalsIn(host).forEach@5009', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:handleTrigger@5007', __javascriptError); throw __javascriptError; }};
    const refreshConnectorHitTesting = () => { try {
        const current = new Set(signalsIn(host));
        current.forEach(connector => { try {
            const settings = parse(connector.dataset.signal);
            const needsPointerEvents = ['onclick', 'onhover'].includes(lower(settings.trigger))
                && settings.lineVisible !== false && connector.dataset.signalLineVisible !== 'false';
            if (needsPointerEvents && !pointerStates.has(connector)) {
                pointerStates.set(connector, connector.style.pointerEvents);
                connector.style.pointerEvents = 'auto';
            } else if (!needsPointerEvents && pointerStates.has(connector)) {
                connector.style.pointerEvents = pointerStates.get(connector);
                pointerStates.delete(connector);
            }
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:current.forEach@5020', __javascriptError); throw __javascriptError; }});
        [...pointerStates.keys()].filter(connector => { try { return (!current.has(connector) || !connector.isConnected); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...pointerStates.keys()].filter@5032', __javascriptError); throw __javascriptError; } }).forEach(connector => { try {
            if (connector.isConnected) connector.style.pointerEvents = pointerStates.get(connector);
            pointerStates.delete(connector);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...pointerStates.keys()].filter(connector => !current.has(connector) @5032', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:refreshConnectorHitTesting@5018', __javascriptError); throw __javascriptError; }};
    const bind = () => { try {
        const click = event => { try { return (handleTrigger(event, 'onclick')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:click@5038', __javascriptError); throw __javascriptError; } };
        const hover = event => { try { return (handleTrigger(event, 'onhover')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:hover@5039', __javascriptError); throw __javascriptError; } };
        const pageEnter = event => { try { return (startPage(event.target?.closest?.('[data-page-id]') || event.target)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pageEnter@5040', __javascriptError); throw __javascriptError; } };
        host.addEventListener('click', click, true);
        host.addEventListener('pointerover', hover, true);
        host.addEventListener('publisher:page-enter', pageEnter);
        bindings.push([host, 'click', click, true], [host, 'pointerover', hover, true], [host, 'publisher:page-enter', pageEnter, false]);
        refreshConnectorHitTesting();
        if (typeof MutationObserver === 'function') {
            observer = new MutationObserver(refreshConnectorHitTesting);
            observer.observe(host, { subtree: true, childList: true, attributes: true, attributeFilter: ['data-signal', 'data-signal-enabled', 'data-signal-line-visible'] });
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:bind@5037', __javascriptError); throw __javascriptError; }};
    const dispose = () => { try {
        disposed = true;
        reset();
        observer?.disconnect();
        observer = null;
        bindings.splice(0).forEach(([node, eventName, handler, capturePhase]) => { try { return (node.removeEventListener(eventName, handler, capturePhase)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:bindings.splice(0).forEach@5056', __javascriptError); throw __javascriptError; } });
        pointerStates.forEach((value, connector) => { try { if (connector.isConnected) connector.style.pointerEvents = value;  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:pointerStates.forEach@5057', __javascriptError); throw __javascriptError; }});
        pointerStates.clear();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:dispose@5051', __javascriptError); throw __javascriptError; }};
    bind();
    const api = { run, stop, reset, startPage, dispose, root: host };
    if (options.expose !== false) window.PublisherStudioSignals = api;
    if (options.autoStart !== false) {
        queueMicrotask(() => { try {
            const visiblePage = [...host.querySelectorAll('[data-page-id]')].find(page => { try { return (!page.hidden && getComputedStyle(page).display !== 'none'); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...host.querySelectorAll(\'[data-page-id]\')].find@5065', __javascriptError); throw __javascriptError; } }) || (host.matches?.('[data-page-id]') ? host : null);
            startPage(visiblePage || host);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:queueMicrotask@5064', __javascriptError); throw __javascriptError; }});
    }
    return api;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:signalConnectorRuntime@4590', __javascriptError); throw __javascriptError; }}

function websitePresentationRuntime() { try {
    const publication = document.querySelector('.website-publication');
    if (!publication) return;
    const pages = [...publication.querySelectorAll(':scope > .print-page')];
    if (!pages.length) return;
    const lower = value => { try { return (String(value || '').replace(/[^a-z0-9]/gi, '').toLowerCase()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:lower@5077', __javascriptError); throw __javascriptError; } };
    const num = (value, fallback) => { try { const parsed = Number(value); return Number.isFinite(parsed) ? parsed : fallback;  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:num@5078', __javascriptError); throw __javascriptError; }};
    const pageSizes = pages.map(page => { try { return (({
        width: Math.max(1, num(page.dataset.exportWidthPx, page.offsetWidth || 1)),
        height: Math.max(1, num(page.dataset.exportHeightPx, page.offsetHeight || 1))
    })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:pages.map@5079', __javascriptError); throw __javascriptError; } });
    const frameWidth = Math.max(1, num(publication.dataset.frameWidthPx, Math.max(...pageSizes.map(size => { try { return (size.width); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:pageSizes.map@5083', __javascriptError); throw __javascriptError; } }))));
    const frameHeight = Math.max(1, num(publication.dataset.frameHeightPx, Math.max(...pageSizes.map(size => { try { return (size.height); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:pageSizes.map@5084', __javascriptError); throw __javascriptError; } }))));
    const bool = value => { try { return (String(value).toLowerCase() === 'true'); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:bool@5085', __javascriptError); throw __javascriptError; } };
    const parse = (value, fallback) => { try { try { return JSON.parse(value || ''); } catch { return fallback; }  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:parse@5086', __javascriptError); throw __javascriptError; }};
    const reducedMotion = typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;
    const animationSpan = animation => { try { return (reducedMotion ? .001 : ['playmedia','pausemedia','stopmedia'].includes(lower(animation.effect))
        ? .05
        : Math.max(.05, num(animation.durationSeconds, .6)) * Math.max(1, num(animation.repeatCount, 1)) * (animation.autoReverse ? 2 : 1)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:animationSpan@5088', __javascriptError); throw __javascriptError; } };
    const easing = value => { try {
        switch (lower(value)) {
            case 'linear': return 'linear';
            case 'easein': return 'cubic-bezier(.42,0,1,1)';
            case 'easeout': return 'cubic-bezier(0,0,.2,1)';
            case 'backout': return 'cubic-bezier(.18,.89,.32,1.28)';
            case 'bounceout': return 'cubic-bezier(.22,1.3,.36,1)';
            default: return 'cubic-bezier(.4,0,.2,1)';
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:easing@5091', __javascriptError); throw __javascriptError; }};
    const vector = (direction, distance) => { try {
        const amount = num(distance, 18);
        switch (lower(direction)) {
            case 'right': return { x: amount, y: 0 };
            case 'up': return { x: 0, y: -amount };
            case 'down': return { x: 0, y: amount };
            default: return { x: -amount, y: 0 };
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:vector@5101', __javascriptError); throw __javascriptError; }};
    const baseTransform = node => { try { const inline = String(node?.style?.transform || '').trim(); if (inline) return inline === 'none' ? '' : inline; const value = getComputedStyle(node).transform; return !value || value === 'none' ? '' : value;  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:baseTransform@5110', __javascriptError); throw __javascriptError; }};
    const compose = (base, extra) => { try { return (`${extra} ${base}`.trim()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:compose@5111', __javascriptError); throw __javascriptError; } };
    const frames = (node, animation) => { try {
        const effect = lower(animation.effect);
        const phase = lower(animation.phase);
        const base = baseTransform(node);
        const move = vector(animation.direction, animation.distancePercent);
        const scale = Math.max(.01, num(animation.scalePercent, 20) / 100);
        const rotation = num(animation.rotationDegrees, 360);
        const translated = compose(base, `translate(${move.x}%,${move.y}%)`);
        const reverse = value => { try { return (phase === 'exit' ? [...value].reverse() : value); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:reverse@5120', __javascriptError); throw __javascriptError; } };
        switch (effect) {
            case 'fade': return reverse([{ opacity: 0 }, { opacity: 1 }]);
            case 'fly': return reverse([{ opacity: 0, transform: translated }, { opacity: 1, transform: base || 'none' }]);
            case 'float': return reverse([{ opacity: 0, filter: 'blur(6px)', transform: compose(base, `translate(${move.x / 2}%,${move.y / 2}%)`) }, { opacity: 1, filter: 'blur(0)', transform: base || 'none' }]);
            case 'zoom': return reverse([{ opacity: 0, transform: compose(base, `scale(${Math.max(.02, 1 - scale)})`) }, { opacity: 1, transform: base || 'none' }]);
            case 'wipe': {
                const direction = lower(animation.direction);
                const start = direction === 'right' ? 'inset(0 100% 0 0)' : direction === 'up' ? 'inset(100% 0 0 0)' : direction === 'down' ? 'inset(0 0 100% 0)' : 'inset(0 0 0 100%)';
                return reverse([{ opacity: 0, clipPath: start }, { opacity: 1, clipPath: 'inset(0 0 0 0)' }]);
            }
            case 'bounce':
                if (phase === 'entrance' || phase === 'exit') return reverse([
                    { opacity: 0, transform: compose(base, `translate(${move.x}%,${move.y}%) scale(${Math.max(.05, 1 - scale)})`) },
                    { opacity: 1, offset: .62, transform: compose(base, 'scale(1.08)') },
                    { opacity: 1, transform: base || 'none' }
                ]);
                return [{ transform: base || 'none' }, { offset: .35, transform: compose(base, `translateY(${-Math.max(8, num(animation.distancePercent, 18))}%) scale(${1 + scale / 2})`) }, { offset: .7, transform: compose(base, 'translateY(3%) scale(.98)') }, { transform: base || 'none' }];
            case 'pulse':
            case 'growshrink': return [{ transform: base || 'none' }, { transform: compose(base, `scale(${1 + scale})`), offset: .5 }, { transform: base || 'none' }];
            case 'spin': return [{ transform: base || 'none' }, { transform: compose(base, `rotate(${rotation}deg)`) }];
            case 'shake': {
                const amount = Math.max(2, num(animation.distancePercent, 18) / 4);
                const positions = [0, -2, 2, -1.6, 1.6, -.8, .8, 0];
                return positions.map((factor, index) => { try { return (({ offset: index / (positions.length - 1), transform: compose(base, `translateX(${amount * factor}%)`) })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:positions.map@5144', __javascriptError); throw __javascriptError; } });
            }
            case 'move': return [{ transform: base || 'none' }, { transform: translated }];
            default: return [{ opacity: 1 }, { opacity: 1 }];
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:frames@5112', __javascriptError); throw __javascriptError; }};
    const groupNodes = node => { try {
        const groupId = String(node?.dataset?.groupId || '').trim();
        const page = node?.closest?.('.print-page');
        if (!groupId || !page) return [node];
        const peers = [...page.querySelectorAll('[data-publication-element][data-group-id]')]
            .filter(candidate => { try { return (String(candidate.dataset.groupId || '') === groupId); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...page.querySelectorAll(\'[data-publication-element][data-group-id]\')@5155', __javascriptError); throw __javascriptError; } });
        return peers.length ? peers : [node];
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:groupNodes@5150', __javascriptError); throw __javascriptError; }};
    const setGroupOrigins = nodes => { try {
        if (nodes.length < 2) return () => { try { } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@5159', __javascriptError); throw __javascriptError; }};
        const entries = nodes.map(node => { try { return (({ node, rect: node.getBoundingClientRect(), previous: node.style.transformOrigin })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:nodes.map@5160', __javascriptError); throw __javascriptError; } });
        const left = Math.min(...entries.map(item => { try { return (item.rect.left); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:entries.map@5161', __javascriptError); throw __javascriptError; } }));
        const top = Math.min(...entries.map(item => { try { return (item.rect.top); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:entries.map@5162', __javascriptError); throw __javascriptError; } }));
        const right = Math.max(...entries.map(item => { try { return (item.rect.right); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:entries.map@5163', __javascriptError); throw __javascriptError; } }));
        const bottom = Math.max(...entries.map(item => { try { return (item.rect.bottom); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:entries.map@5164', __javascriptError); throw __javascriptError; } }));
        const centerX = (left + right) / 2;
        const centerY = (top + bottom) / 2;
        entries.forEach(item => { try { return (item.node.style.transformOrigin = `${centerX - item.rect.left}px ${centerY - item.rect.top}px`); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:entries.forEach@5167', __javascriptError); throw __javascriptError; } });
        return () => { try { return (entries.forEach(item => { try { return (item.node.style.transformOrigin = item.previous); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:entries.forEach@5168', __javascriptError); throw __javascriptError; } })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@5168', __javascriptError); throw __javascriptError; } };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:setGroupOrigins@5158', __javascriptError); throw __javascriptError; }};
    const compositeAnimation = (animations, restore) => { try { return (({
        finished: Promise.all(animations.map(animation => { try { return (animation.finished.catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@5171', __promiseError);  return (undefined); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:animation.finished.catch@5171', __javascriptError); throw __javascriptError; } })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:animations.map@5171', __javascriptError); throw __javascriptError; } })),
        cancel() { try { animations.forEach(animation => { try { try { animation.cancel(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@5172', __caughtJavaScriptError);  }  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:animations.forEach@5172', __javascriptError); throw __javascriptError; }}); restore();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:cancel@5172', __javascriptError); throw __javascriptError; }},
        pause() { try { animations.forEach(animation => { try { try { animation.pause(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@5173', __caughtJavaScriptError);  }  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:animations.forEach@5173', __javascriptError); throw __javascriptError; }});  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pause@5173', __javascriptError); throw __javascriptError; }},
        play() { try { animations.forEach(animation => { try { try { animation.play(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@5174', __caughtJavaScriptError);  }  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:animations.forEach@5174', __javascriptError); throw __javascriptError; }});  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:play@5174', __javascriptError); throw __javascriptError; }}
    })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:compositeAnimation@5170', __javascriptError); throw __javascriptError; } };
    const playItem = (item, delay = 0) => { try {
        item.prestate?.cancel();
        item.prestate = null;
        const effect = lower(item.animation.effect);
        if (['playmedia','pausemedia','stopmedia'].includes(effect)) {
            let timer = 0;
            let cancelled = false;
            let resolveFinished;
            const finished = new Promise(resolve => { try { resolveFinished = resolve;  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@5184', __javascriptError); throw __javascriptError; }});
            const execute = () => { try {
                if (cancelled) return;
                if (effect === 'playmedia') playMediaNode(item.node);
                else if (effect === 'pausemedia') pauseMediaNode(item.node);
                else stopMediaNode(item.node, true);
                resolveFinished();
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:execute@5185', __javascriptError); throw __javascriptError; }};
            timer = setTimeout(execute, Math.max(0, delay) * 1000);
            activeMediaTimers.push(timer);
            const handle = { finished, cancel() { try { cancelled = true; clearTimeout(timer); resolveFinished();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:cancel@5194', __javascriptError); throw __javascriptError; }} };
            activeAnimations.push(handle);
            return handle;
        }
        const nodes = groupNodes(item.node);
        if (lower(item.animation.phase) === 'entrance') nodes.forEach(node => { try { return (node.classList.remove('ps-action-hidden')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:nodes.forEach@5199', __javascriptError); throw __javascriptError; } });
        const repeat = Math.max(1, Math.round(num(item.animation.repeatCount, 1)));
        const restore = setGroupOrigins(nodes);
        const members = nodes.map(node => { try { return (node.animate(frames(node, item.animation), {
            duration: (reducedMotion ? .001 : Math.max(.05, num(item.animation.durationSeconds, .6))) * 1000,
            delay: (reducedMotion ? 0 : Math.max(0, delay)) * 1000,
            easing: easing(item.animation.easing),
            iterations: reducedMotion ? 1 : repeat * (item.animation.autoReverse ? 2 : 1),
            direction: item.animation.autoReverse ? 'alternate' : 'normal',
            fill: lower(item.animation.phase) === 'entrance' ? 'both' : 'forwards'
        })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:nodes.map@5202', __javascriptError); throw __javascriptError; } });
        const animation = members.length === 1 ? members[0] : compositeAnimation(members, restore);
        activeAnimations.push(animation);
        return animation;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:playItem@5176', __javascriptError); throw __javascriptError; }};
    const transitionFrames = (page, entering) => { try {
        const kind = lower(page.dataset.transitionKind);
        const direction = lower(page.dataset.transitionDirection);
        const move = vector(direction, 12);
        let value;
        switch (kind) {
            case 'push': value = [{ opacity: .35, transform: `translate(${move.x}%,${move.y}%)` }, { opacity: 1, transform: 'translate(0,0)' }]; break;
            case 'wipe': {
                const start = direction === 'right' ? 'inset(0 100% 0 0)' : direction === 'up' ? 'inset(100% 0 0 0)' : direction === 'down' ? 'inset(0 0 100% 0)' : 'inset(0 0 0 100%)';
                value = [{ opacity: .3, clipPath: start }, { opacity: 1, clipPath: 'inset(0 0 0 0)' }];
                break;
            }
            case 'zoom': value = [{ opacity: 0, transform: 'scale(.86)' }, { opacity: 1, transform: 'scale(1)' }]; break;
            case 'flip': {
                const axis = direction === 'up' || direction === 'down' ? 'X' : 'Y';
                const sign = direction === 'right' || direction === 'down' ? 1 : -1;
                value = [{ opacity: 0, transform: `perspective(1200px) rotate${axis}(${sign * 75}deg)` }, { opacity: 1, transform: `perspective(1200px) rotate${axis}(0deg)` }];
                break;
            }
            case 'none': value = [{ opacity: 1 }, { opacity: 1 }]; break;
            default: value = [{ opacity: 0 }, { opacity: 1 }]; break;
        }
        return entering ? value : [...value].reverse();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:transitionFrames@5214', __javascriptError); throw __javascriptError; }};
    const pageItems = page => { try { return ([...page.querySelectorAll('[data-publication-element]')].flatMap(node => { try { return (parse(node.dataset.animations, []).map(animation => { try { return (({ node, animation, prestate: null })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:parse(node.dataset.animations, []).map@5238', __javascriptError); throw __javascriptError; } })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...page.querySelectorAll(\'[data-publication-element]\')].flatMap@5238', __javascriptError); throw __javascriptError; } }).sort((a, b) => { try { return (num(a.animation.order, 0) - num(b.animation.order, 0)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...page.querySelectorAll(\'[data-publication-element]\')].flatMap(node @5238', __javascriptError); throw __javascriptError; } })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pageItems@5238', __javascriptError); throw __javascriptError; } };
    const splitTimeline = items => { try {
        const automatic = [];
        const clickGroups = [];
        let group = null;
        for (const item of items) {
            const trigger = lower(item.animation.trigger);
            if (trigger === 'onclick') {
                group = [item];
                clickGroups.push(group);
            } else if (trigger === 'onpageenter') {
                automatic.push(item);
                group = null;
            } else if (group) {
                group.push(item);
            } else {
                automatic.push(item);
            }
        }
        return { automatic, clickGroups };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:splitTimeline@5239', __javascriptError); throw __javascriptError; }};
    const scheduleGroup = items => { try {
        let previousStart = 0;
        let previousEnd = 0;
        let groupEnd = 0;
        for (const item of items) {
            const trigger = lower(item.animation.trigger);
            const ownDelay = reducedMotion ? 0 : Math.max(0, num(item.animation.delaySeconds, 0));
            let start = ownDelay;
            const explicitStart = Number(item.animation.timelineStartSeconds);
            if (Number.isFinite(explicitStart)) start = Math.max(0, explicitStart);
            else if (trigger === 'withprevious') start = previousStart + ownDelay;
            else if (trigger === 'afterprevious') start = previousEnd + ownDelay;
            playItem(item, start);
            const end = start + animationSpan(item.animation);
            previousStart = start;
            previousEnd = end;
            groupEnd = Math.max(groupEnd, end);
        }
        return groupEnd;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:scheduleGroup@5259', __javascriptError); throw __javascriptError; }};
    const primeClickEntrances = groups => { try {
        for (const item of groups.flat()) {
            if (lower(item.animation.phase) !== 'entrance') continue;
            const members = groupNodes(item.node).map(node => { try {
                const animation = node.animate(frames(node, item.animation), { duration: 1, fill: 'both' });
                animation.pause();
                animation.currentTime = 0;
                return animation;
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:groupNodes(item.node).map@5282', __javascriptError); throw __javascriptError; }});
            item.prestate = members.length === 1 ? members[0] : compositeAnimation(members, () => { try { } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:compositeAnimation@5288', __javascriptError); throw __javascriptError; }});
            activeAnimations.push(item.prestate);
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:primeClickEntrances@5279', __javascriptError); throw __javascriptError; }};
    const mediaFromNode = node => { try { return (node?.querySelector('video,audio') || null); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:mediaFromNode@5292', __javascriptError); throw __javascriptError; } };
    const mediaSegments = (node, media) => { try {
        const sources = [...(media?.querySelectorAll?.('source[data-media-segment]') || [])]
            .map(source => { try {
                const src = source.getAttribute('src') || '';
                const start = Math.max(0, num(source.dataset.mediaTrimStart, 0));
                const end = Math.max(start + .01, num(source.dataset.mediaTrimEnd, start + 1));
                return { src, start, end, poster: source.dataset.mediaPoster || '', originalSrc: source.dataset.publisherOriginalSrc || '' };
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...(media?.querySelectorAll?.(\'source[data-media-segment]\') || [])] .@5295', __javascriptError); throw __javascriptError; }}).filter(segment => { try { return (segment.src); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...(media?.querySelectorAll?.(\'source[data-media-segment]\') || [])] .@5300', __javascriptError); throw __javascriptError; } });
        if (sources.length) return sources;
        const src = media?.getAttribute('src') || media?.currentSrc || '';
        const start = Math.max(0, num(node?.dataset?.mediaTrimStart, 0));
        const end = Math.max(start + .01, num(node?.dataset?.mediaTrimEnd, media?.duration || start + 1));
        return src ? [{ src, start, end, poster: media?.getAttribute('poster') || '', originalSrc: media?.dataset?.publisherOriginalSrc || '' }] : [];
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:mediaSegments@5293', __javascriptError); throw __javascriptError; }};
    const sameSource = (media, source) => { try {
        try { return new URL(media.currentSrc || media.getAttribute('src') || '', location.href).href === new URL(source, location.href).href; }
        catch { return (media.currentSrc || media.getAttribute('src') || '') === source; }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:sameSource@5307', __javascriptError); throw __javascriptError; }};
    const waitMetadata = media => { try { return (media.readyState >= 1 ? Promise.resolve() : new Promise(resolve => { try {
        const timer = setTimeout(done, 5000);
        function done() { try { clearTimeout(timer); media.removeEventListener('loadedmetadata', done); media.removeEventListener('error', done); resolve();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:done@5313', __javascriptError); throw __javascriptError; }}
        media.addEventListener('loadedmetadata', done, { once: true });
        media.addEventListener('error', done, { once: true });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@5311', __javascriptError); throw __javascriptError; }})); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:waitMetadata@5311', __javascriptError); throw __javascriptError; } };
    const clearMediaSequence = media => { try {
        if (!media) return;
        if (media.__psTimeHandler) media.removeEventListener('timeupdate', media.__psTimeHandler);
        media.__psTimeHandler = null;
        if (media.__psSequenceState) {
            media.__psSequenceState.token = (media.__psSequenceState.token || 0) + 1;
            media.__psSequenceState.advancing = false;
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:clearMediaSequence@5317', __javascriptError); throw __javascriptError; }};
    const configureMediaSegment = (node, media, requestedIndex = 0, autoPlay = false) => { try {
        if (!node || !media) return;
        clearMediaSequence(media);
        const segments = mediaSegments(node, media);
        if (!segments.length) return;
        const index = Math.max(0, Math.min(segments.length - 1, Number(requestedIndex) || 0));
        const segment = segments[index];
        const rate = Math.max(.1, num(node.dataset.mediaRate, 1));
        const baseVolume = Math.max(0, Math.min(1, num(node.dataset.mediaVolume, 1)));
        const fadeIn = Math.max(0, num(node.dataset.mediaFadeIn, 0));
        const fadeOut = Math.max(0, num(node.dataset.mediaFadeOut, 0));
        const elapsedBefore = segments.slice(0, index).reduce((total, item) => { try { return (total + Math.max(.01, item.end - item.start) / rate); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:segments.slice(0, index).reduce@5337', __javascriptError); throw __javascriptError; } }, 0);
        const totalDuration = segments.reduce((total, item) => { try { return (total + Math.max(.01, item.end - item.start) / rate); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:segments.reduce@5338', __javascriptError); throw __javascriptError; } }, 0);
        const state = media.__psSequenceState || { token: 0, index: 0, advancing: false };
        state.index = index; state.segments = segments; state.token = (state.token || 0) + 1; state.advancing = false;
        media.__psSequenceState = state;
        const token = state.token;
        media.playbackRate = rate;
        media.muted = bool(node.dataset.mediaMuted);
        media.loop = false;
        media.volume = fadeIn > 0 && index === 0 ? 0 : baseVolume;
        if (media instanceof HTMLVideoElement && segment.poster) media.poster = segment.poster;
        const prepare = async () => { try {
            if (media.__psFallbackHandler) media.removeEventListener('error', media.__psFallbackHandler);
            media.__psFallbackHandler = null;
            if (segment.originalSrc) {
                const fallbackHandler = async () => { try {
                    if (state.token !== token || sameSource(media, segment.originalSrc)) return;
                    media.removeEventListener('error', fallbackHandler);
                    media.__psFallbackHandler = null;
                    media.pause(); media.src = segment.originalSrc; media.load(); await waitMetadata(media);
                    if (state.token !== token) return;
                    try { media.currentTime = segment.start; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@5358', __caughtJavaScriptError);  }
                    if (autoPlay) media.play().catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@5359', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:media.play().catch@5359', __javascriptError); throw __javascriptError; }});
                 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:fallbackHandler@5352', __javascriptError); throw __javascriptError; }};
                media.__psFallbackHandler = fallbackHandler;
                media.addEventListener('error', fallbackHandler, { once: true });
            }
            if (!sameSource(media, segment.src)) {
                media.pause(); media.src = segment.src; media.load(); await waitMetadata(media);
            }
            if (state.token !== token) return;
            try { media.currentTime = segment.start; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@5368', __caughtJavaScriptError);  }
            const onTime = () => { try {
                if (state.token !== token || state.advancing) return;
                const timelinePosition = elapsedBefore + Math.max(0, media.currentTime - segment.start) / rate;
                const timelineRemaining = Math.max(0, totalDuration - timelinePosition);
                let volume = baseVolume;
                if (fadeIn > 0) volume *= Math.max(0, Math.min(1, timelinePosition / fadeIn));
                if (fadeOut > 0) volume *= Math.max(0, Math.min(1, timelineRemaining / fadeOut));
                if (!media.muted) media.volume = Math.max(0, Math.min(1, volume));
                if (media.currentTime < segment.end - .02) return;
                state.advancing = true;
                if (index + 1 < segments.length) configureMediaSegment(node, media, index + 1, true);
                else if (bool(node.dataset.mediaLoop)) configureMediaSegment(node, media, 0, true);
                else { media.pause(); state.advancing = false; }
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:onTime@5369', __javascriptError); throw __javascriptError; }};
            media.__psTimeHandler = onTime;
            media.addEventListener('timeupdate', onTime);
            if (autoPlay) media.play().catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@5385', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:media.play().catch@5385', __javascriptError); throw __javascriptError; }});
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:prepare@5348', __javascriptError); throw __javascriptError; }};
        void prepare();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:configureMediaSegment@5326', __javascriptError); throw __javascriptError; }};
    const pauseMediaNode = node => { try {
        const media = mediaFromNode(node);
        if (media) media.pause();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pauseMediaNode@5389', __javascriptError); throw __javascriptError; }};
    const stopMediaNode = (node, rewind = false) => { try {
        const media = mediaFromNode(node);
        if (!media) return;
        media.pause();
        clearMediaSequence(media);
        if (rewind) configureMediaSegment(node, media, 0, false);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:stopMediaNode@5393', __javascriptError); throw __javascriptError; }};
    const playMediaNode = (node, delay = 0) => { try {
        const media = mediaFromNode(node);
        if (!media) return;
        const run = () => { try { return (configureMediaSegment(node, media, 0, true)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:run@5403', __javascriptError); throw __javascriptError; } };
        if (delay > 0) activeMediaTimers.push(setTimeout(run, delay * 1000)); else run();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:playMediaNode@5400', __javascriptError); throw __javascriptError; }};
    const toggleMediaNode = node => { try {
        const media = mediaFromNode(node);
        if (!media) return;
        if (media.paused) {
            if (media.__psSequenceState?.segments?.length) media.play().catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@5410', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:media.play().catch@5410', __javascriptError); throw __javascriptError; }});
            else playMediaNode(node);
        } else media.pause();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:toggleMediaNode@5406', __javascriptError); throw __javascriptError; }};
    const startPageMedia = page => { try {
        for (const node of page.querySelectorAll('[data-media-kind]')) {
            const trigger = lower(node.dataset.mediaTrigger);
            if (!bool(node.dataset.mediaAutoplay) || trigger === 'onclick') continue;
            playMediaNode(node, Math.max(0, num(node.dataset.mediaStart, 0)));
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:startPageMedia@5414', __javascriptError); throw __javascriptError; }};
    const resetPageVisibility = page => { try {
        for (const node of page.querySelectorAll('[data-publication-element]'))
            node.classList.toggle('ps-action-hidden', bool(node.dataset.playbackHidden));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:resetPageVisibility@5421', __javascriptError); throw __javascriptError; }};
    const cancelPlayback = () => { try {
        clearTimeout(autoTimer);
        autoTimer = 0;
        for (const animation of activeAnimations) { try { animation.cancel(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@5428', __caughtJavaScriptError);  } }
        activeAnimations = [];
        for (const timer of activeMediaTimers) clearTimeout(timer);
        activeMediaTimers = [];
        for (const node of publication.querySelectorAll('[data-media-kind]')) stopMediaNode(node, true);
        clickGroups = [];
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:cancelPlayback@5425', __javascriptError); throw __javascriptError; }};
    const fitPages = () => { try {
        const controlsHeight = controls && !controls.hidden ? 62 : 18;
        const scale = Math.min(
            (innerWidth - 32) / frameWidth,
            (innerHeight - controlsHeight - 24) / frameHeight,
            1.75);
        stage.style.transform = `scale(${Math.max(.05, scale)})`;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:fitPages@5435', __javascriptError); throw __javascriptError; }};
    const updateControls = () => { try {
        if (!counter) return;
        counter.textContent = `${current + 1} / ${pages.length}`;
        previousButton.disabled = current <= 0 && !loop;
        nextButton.disabled = current >= pages.length - 1 && !loop;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:updateControls@5443', __javascriptError); throw __javascriptError; }};
    const normalizeIndex = value => { try {
        if (loop) return (value % pages.length + pages.length) % pages.length;
        return Math.max(0, Math.min(pages.length - 1, value));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:normalizeIndex@5449', __javascriptError); throw __javascriptError; }};
    const showPage = async (requested, direction = 1, animate = true) => { try {
        const next = normalizeIndex(requested);
        if (next === current && shells[current].classList.contains('active')) return;
        cancelPlayback();
        const previous = shells[current];
        const target = shells[next];
        const page = pages[next];
        target.hidden = false;
        target.classList.add('active');
        if (animate) {
            const duration = (reducedMotion ? .001 : Math.max(.1, num(page.dataset.transitionDuration, .55))) * 1000;
            const incoming = target.animate(transitionFrames(page, true), { duration, easing: easing(page.dataset.transitionEasing), fill: 'both' });
            activeAnimations.push(incoming);
            if (previous && previous !== target && !previous.hidden) {
                const outgoing = previous.animate(transitionFrames(page, false), { duration, easing: easing(page.dataset.transitionEasing), fill: 'both' });
                activeAnimations.push(outgoing);
                try { await Promise.allSettled([incoming.finished, outgoing.finished]); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@5469', __caughtJavaScriptError);  }
            }
        }
        shells.forEach((shell, index) => { try {
            shell.hidden = index !== next;
            shell.classList.toggle('active', index === next);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:shells.forEach@5472', __javascriptError); throw __javascriptError; }});
        current = next;
        updateControls();
        runCurrentPage(startAutomatically || animate);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:showPage@5453', __javascriptError); throw __javascriptError; }};
    const runCurrentPage = shouldRun => { try {
        cancelPlayback();
        const page = pages[current];
        window.PublisherStudioComponentRuntime?.refreshPanels?.(page);
        resetPageVisibility(page);
        page.dispatchEvent(new CustomEvent('publisher:page-enter', { bubbles: true, detail: { pageId: page.dataset.pageId || '', pageName: page.dataset.pageName || '' } }));
        const timeline = splitTimeline(pageItems(page));
        clickGroups = timeline.clickGroups;
        if (shouldRun) {
            scheduleGroup(timeline.automatic);
            startPageMedia(page);
        } else primeClickEntrances([timeline.automatic]);
        primeClickEntrances(clickGroups);
        if (bool(page.dataset.autoAdvance)) {
            const seconds = Math.max(.25, num(page.dataset.autoAdvanceSeconds, 5));
            autoTimer = setTimeout(() => { try { return (showPage(current + 1, 1, true)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:setTimeout@5494', __javascriptError); throw __javascriptError; } }, seconds * 1000);
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:runCurrentPage@5480', __javascriptError); throw __javascriptError; }};
    const runNextClickGroup = () => { try {
        const group = clickGroups.shift();
        if (!group) return false;
        scheduleGroup(group);
        return true;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:runNextClickGroup@5497', __javascriptError); throw __javascriptError; }};
    const replayCurrent = () => { try { return (runCurrentPage(true)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:replayCurrent@5503', __javascriptError); throw __javascriptError; } };
    const goNext = () => { try { return (showPage(current + 1, 1, true)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:goNext@5504', __javascriptError); throw __javascriptError; } };
    const goPrevious = () => { try { return (showPage(current - 1, -1, true)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:goPrevious@5505', __javascriptError); throw __javascriptError; } };

    const stage = document.createElement('div');
    stage.className = 'ps-stage';
    stage.style.width = `${frameWidth}px`;
    stage.style.height = `${frameHeight}px`;
    publication.appendChild(stage);

    const shells = pages.map((page, index) => { try {
        const shell = document.createElement('div');
        shell.className = 'ps-slide';
        const size = pageSizes[index];
        const pageScale = Math.min(frameWidth / size.width, frameHeight / size.height);
        page.style.position = 'absolute';
        page.style.left = '50%';
        page.style.top = '50%';
        page.style.width = `${size.width}px`;
        page.style.height = `${size.height}px`;
        page.style.minWidth = `${size.width}px`;
        page.style.minHeight = `${size.height}px`;
        page.style.maxWidth = 'none';
        page.style.maxHeight = 'none';
        page.style.margin = '0';
        page.style.translate = 'none';
        page.style.transformOrigin = 'center center';
        page.style.transform = `translate(-50%, -50%) scale(${Math.max(.01, pageScale)})`;
        stage.appendChild(shell);
        shell.appendChild(page);
        shell.hidden = true;
        return shell;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:pages.map@5513', __javascriptError); throw __javascriptError; }});
    const showControls = bool(publication.dataset.playbackControls);
    const loop = bool(publication.dataset.playbackLoop);
    const startAutomatically = publication.dataset.playbackStart !== 'false';
    let current = 0;
    let activeAnimations = [];
    let activeMediaTimers = [];
    let clickGroups = [];
    let autoTimer = 0;

    const controls = document.createElement('nav');
    controls.className = 'ps-controls';
    controls.hidden = !showControls;
    controls.innerHTML = '<button type="button" data-ps-previous title="Previous page">‹</button><button type="button" data-ps-replay title="Replay page">↻</button><span data-ps-counter></span><button type="button" data-ps-next title="Next page">›</button><button type="button" data-ps-fullscreen title="Full screen">⛶</button>';
    document.body.appendChild(controls);
    const previousButton = controls.querySelector('[data-ps-previous]');
    const nextButton = controls.querySelector('[data-ps-next]');
    const counter = controls.querySelector('[data-ps-counter]');
    previousButton.addEventListener('click', event => { try { event.stopPropagation(); goPrevious();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:previousButton.addEventListener@5553', __javascriptError); throw __javascriptError; }});
    nextButton.addEventListener('click', event => { try { event.stopPropagation(); goNext();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:nextButton.addEventListener@5554', __javascriptError); throw __javascriptError; }});
    controls.querySelector('[data-ps-replay]').addEventListener('click', event => { try { event.stopPropagation(); replayCurrent();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:controls.querySelector(\'[data-ps-replay]\').addEventListener@5555', __javascriptError); throw __javascriptError; }});
    controls.querySelector('[data-ps-fullscreen]').addEventListener('click', async event => { try {
        event.stopPropagation();
        try { if (!document.fullscreenElement) await document.documentElement.requestFullscreen(); else await document.exitFullscreen(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@5558', __caughtJavaScriptError);  }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:controls.querySelector(\'[data-ps-fullscreen]\').addEventListener@5556', __javascriptError); throw __javascriptError; }});

    pages.forEach((page, pageIndex) => { try {
        page.addEventListener('click', event => { try {
            if (event.defaultPrevented) return;
            if (event.target?.closest?.('.ps-pointer-owner,[data-panel-root]')) return;
            if (runNextClickGroup()) return;
            if (bool(page.dataset.advanceOnClick)) goNext();
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:page.addEventListener@5562', __javascriptError); throw __javascriptError; }});
        const signalSourceIds = new Set([...page.querySelectorAll('[data-signal-enabled="true"][data-connector-id]')].flatMap(connector => { try {
            const settings = parse(connector.dataset.signal, {});
            const trigger = lower(settings.trigger);
            const sourceId = String(connector.dataset.sourceElementId || '').trim();
            return sourceId && ['onclick', 'onhover'].includes(trigger) ? [sourceId] : [];
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...page.querySelectorAll(\'[data-signal-enabled="true"][data-connector@5567', __javascriptError); throw __javascriptError; }}));
        for (const node of page.querySelectorAll('[data-publication-element]')) {
            const interaction = parse(node.dataset.interaction, {});
            const interactionAction = lower(interaction.action);
            const kind = lower(node.dataset.elementKind);
            const nativeInteractive = Boolean(node.dataset.mediaKind)
                || ['datavisual', 'devextremecomponent', 'livesource'].includes(kind)
                || Boolean(node.querySelector('video,audio,[data-ps-visual-config],[data-ps-component-config],button,a[href],input,select,textarea,[contenteditable="true"]'));
            const signalSource = signalSourceIds.has(String(node.dataset.elementId || ''));
            if (nativeInteractive || signalSource) node.classList.add('ps-pointer-owner');
            if (!nativeInteractive && !signalSource && (!interactionAction || interactionAction === 'none')
                && ['shape', 'wordart', 'barcode'].includes(kind))
                node.classList.add('ps-pointer-passive');
            if (node.dataset.mediaKind && lower(node.dataset.mediaTrigger) === 'onclick' && (!interactionAction || interactionAction === 'none')) {
                node.classList.add('ps-interactive');
                node.addEventListener('click', event => { try {
                    event.preventDefault();
                    event.stopPropagation();
                    toggleMediaNode(node);
                 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:node.addEventListener@5587', __javascriptError); throw __javascriptError; }});
            }
            if (interactionAction === 'none' || !interaction.action) continue;
            node.classList.add('ps-interactive', 'ps-pointer-owner');
            node.classList.remove('ps-pointer-passive');
            node.addEventListener('click', event => { try {
                event.preventDefault();
                event.stopPropagation();
                const action = lower(interaction.action);
                if (action === 'nextpage') goNext();
                else if (action === 'previouspage') goPrevious();
                else if (action === 'gotopage') {
                    const target = pages.findIndex(item => { try { return (String(item.dataset.pageId).toLowerCase() === String(interaction.targetPageId || '').toLowerCase()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:pages.findIndex@5603', __javascriptError); throw __javascriptError; } });
                    if (target >= 0) showPage(target, target >= current ? 1 : -1, true);
                } else if (action === 'openurl') {
                    const url = String(interaction.url || '').trim();
                    if (/^(https?:|mailto:)/i.test(url)) window.open(url, interaction.openInNewWindow === false ? '_self' : '_blank', 'noopener');
                } else {
                    const targetId = interaction.targetElementId || node.dataset.elementId;
                    const target = page.querySelector(`[data-element-id="${CSS.escape(String(targetId))}"]`);
                    if (!target) return;
                    if (action === 'togglevisibility') target.classList.toggle('ps-action-hidden');
                    else if (action === 'show') target.classList.remove('ps-action-hidden');
                    else if (action === 'hide') target.classList.add('ps-action-hidden');
                    else if (action === 'replayanimation') {
                        const items = parse(target.dataset.animations, []).map(animation => { try { return (({ node: target, animation, prestate: null })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:parse(target.dataset.animations, []).map@5616', __javascriptError); throw __javascriptError; } });
                        scheduleGroup(items);
                    } else if (action === 'playmedia') playMediaNode(target);
                    else if (action === 'pausemedia') pauseMediaNode(target);
                    else if (action === 'togglemediaplayback') toggleMediaNode(target);
                }
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:node.addEventListener@5596', __javascriptError); throw __javascriptError; }});
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:pages.forEach@5561', __javascriptError); throw __javascriptError; }});

    window.PublisherStudioNavigation = window.PublisherStudioPresentation = {
        next: goNext,
        previous: goPrevious,
        replay: replayCurrent,
        currentPageId: () => { try { return (pages[current]?.dataset.pageId || null); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:currentPageId@5630', __javascriptError); throw __javascriptError; } },
        goToPage(target) { try {
            const value = String(target ?? '').replace(/^#/, '').toLowerCase();
            const targetIndex = pages.findIndex((page, index) =>
                { try { return (String(page.dataset.pageId || '').toLowerCase() === value ||
                String(page.dataset.pageName || '').trim().toLowerCase() === value ||
                String(index + 1) === value); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:pages.findIndex@5633', __javascriptError); throw __javascriptError; } });
            if (targetIndex >= 0) return showPage(targetIndex, targetIndex >= current ? 1 : -1, true);
            return false;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:goToPage@5631', __javascriptError); throw __javascriptError; }}
    };

    addEventListener('resize', fitPages);
    addEventListener('keydown', event => { try {
        if (event.key === 'ArrowRight' || event.key === 'PageDown') { event.preventDefault(); if (!runNextClickGroup()) goNext(); }
        else if (event.key === 'ArrowLeft' || event.key === 'PageUp') { event.preventDefault(); goPrevious(); }
        else if (event.key === ' ' || event.key === 'Enter') { event.preventDefault(); if (!runNextClickGroup()) goNext(); }
        else if (event.key.toLowerCase() === 'r') replayCurrent();
        else if (event.key === 'Home') showPage(0, -1, true);
        else if (event.key === 'End') showPage(pages.length - 1, 1, true);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:addEventListener@5643', __javascriptError); throw __javascriptError; }});
    fitPages();
    shells[0].hidden = false;
    shells[0].classList.add('active');
    updateControls();
    const startFirstPage = async () => { try {
        if (startAutomatically && lower(pages[0].dataset.transitionKind) !== 'none') {
            const duration = (reducedMotion ? .001 : Math.max(.1, num(pages[0].dataset.transitionDuration, .55))) * 1000;
            const initial = shells[0].animate(transitionFrames(pages[0], true), {
                duration,
                easing: easing(pages[0].dataset.transitionEasing),
                fill: 'both'
            });
            activeAnimations.push(initial);
            try { await initial.finished; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@5664', __caughtJavaScriptError);  }
        }
        runCurrentPage(startAutomatically);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:startFirstPage@5655', __javascriptError); throw __javascriptError; }};
    startFirstPage();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:websitePresentationRuntime@5072', __javascriptError); throw __javascriptError; }}

function websiteSiteRuntime() { try {
    const publication = document.querySelector('.website-publication');
    if (!publication) return;
    const pages = [...publication.querySelectorAll(':scope > .print-page')];
    if (!pages.length) return;
    const numberValue = (value, fallback = 0) => { try { const parsed = Number(value); return Number.isFinite(parsed) ? parsed : fallback;  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:numberValue@5676', __javascriptError); throw __javascriptError; }};
    const lower = value => { try { return (String(value ?? '').replace(/[^a-z0-9]/gi, '').toLowerCase()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:lower@5677', __javascriptError); throw __javascriptError; } };
    const slugify = (value, fallback) => { try { return (String(value || fallback).trim().toLowerCase()
        .normalize('NFKD').replace(/[\u0300-\u036f]/g, '').replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '') || fallback); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:slugify@5678', __javascriptError); throw __javascriptError; } };
    const reducedMotion = typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;
    const usedSlugs = new Map();
    const routes = pages.map((page, index) => { try {
        const baseSlug = slugify(page.dataset.pageName, `page-${index + 1}`);
        const occurrence = (usedSlugs.get(baseSlug) || 0) + 1;
        usedSlugs.set(baseSlug, occurrence);
        return {
            page,
            id: String(page.dataset.pageId || index + 1),
            slug: occurrence === 1 ? baseSlug : `${baseSlug}-${occurrence}`,
            index
        };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:pages.map@5682', __javascriptError); throw __javascriptError; }});
    let current = 0;
    let activeTransition = null;

    publication.classList.add('ps-site');
    pages.forEach((page, index) => { try {
        page.classList.add('ps-site-page');
        page.hidden = index !== 0;
        page.setAttribute('aria-hidden', index === 0 ? 'false' : 'true');
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:pages.forEach@5697', __javascriptError); throw __javascriptError; }});

    const easing = value => { try {
        switch (lower(value)) {
            case 'linear': return 'linear';
            case 'easein': return 'cubic-bezier(.42,0,1,1)';
            case 'easeout': return 'cubic-bezier(0,0,.2,1)';
            case 'backout': return 'cubic-bezier(.18,.89,.32,1.28)';
            default: return 'cubic-bezier(.4,0,.2,1)';
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:easing@5703', __javascriptError); throw __javascriptError; }};
    const transitionFrames = (page, entering, direction) => { try {
        const kind = lower(page.dataset.transitionKind);
        const pageDirection = lower(page.dataset.transitionDirection);
        const sign = direction >= 0 ? 1 : -1;
        const horizontal = pageDirection === 'right' ? -1 : pageDirection === 'up' || pageDirection === 'down' ? 0 : 1;
        const vertical = pageDirection === 'down' ? -1 : pageDirection === 'up' ? 1 : 0;
        const x = 14 * sign * horizontal;
        const y = 14 * sign * vertical;
        if (kind === 'none') return [{ opacity: 1 }, { opacity: 1 }];
        if (kind === 'zoom') return entering
            ? [{ opacity: 0, transform: 'scale(.94)' }, { opacity: 1, transform: 'scale(1)' }]
            : [{ opacity: 1, transform: 'scale(1)' }, { opacity: 0, transform: 'scale(1.04)' }];
        if (kind === 'wipe') return entering
            ? [{ opacity: 1, clipPath: x < 0 ? 'inset(0 0 0 100%)' : y < 0 ? 'inset(100% 0 0 0)' : y > 0 ? 'inset(0 0 100% 0)' : 'inset(0 100% 0 0)' }, { opacity: 1, clipPath: 'inset(0)' }]
            : [{ opacity: 1 }, { opacity: 0 }];
        if (kind === 'push' || kind === 'slide' || kind === 'flip') return entering
            ? [{ opacity: .35, transform: `translate(${x}%,${y}%)` }, { opacity: 1, transform: 'translate(0,0)' }]
            : [{ opacity: 1, transform: 'translate(0,0)' }, { opacity: .25, transform: `translate(${-x / 2}%,${-y / 2}%)` }];
        return entering ? [{ opacity: 0 }, { opacity: 1 }] : [{ opacity: 1 }, { opacity: 0 }];
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:transitionFrames@5712', __javascriptError); throw __javascriptError; }};
    const fitPage = page => { try {
        const width = Math.max(1, numberValue(page.dataset.exportWidthPx, page.offsetWidth || 1));
        const height = Math.max(1, numberValue(page.dataset.exportHeightPx, page.offsetHeight || 1));
        const scale = Math.min(innerWidth / width, innerHeight / height);
        page.style.setProperty('--ps-site-scale', String(Math.max(.05, scale)));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:fitPage@5732', __javascriptError); throw __javascriptError; }};
    const fit = () => { try { return (pages.forEach(fitPage)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:fit@5738', __javascriptError); throw __javascriptError; } };
    const pausePageMedia = page => { try { return (page?.querySelectorAll?.('video,audio').forEach(media => { try { try { media.pause(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@5739', __caughtJavaScriptError);  }  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:page?.querySelectorAll?.(\'video,audio\').forEach@5739', __javascriptError); throw __javascriptError; }})); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pausePageMedia@5739', __javascriptError); throw __javascriptError; } };
    const routeFor = value => { try {
        const normalized = decodeURIComponent(String(value || '').replace(/^#\/?/, '')).toLowerCase();
        if (!normalized) return routes[0];
        return routes.find(route => { try { return (route.slug === normalized || route.id.toLowerCase() === normalized || String(route.index + 1) === normalized); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:routes.find@5743', __javascriptError); throw __javascriptError; } }) || routes[0];
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:routeFor@5740', __javascriptError); throw __javascriptError; }};
    const setHash = (route, replace) => { try {
        const next = `#/${route.slug}`;
        if (location.hash === next) return;
        if (replace) history.replaceState({ publisherPage: route.id }, '', next);
        else history.pushState({ publisherPage: route.id }, '', next);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:setHash@5745', __javascriptError); throw __javascriptError; }};
    const showRoute = async (route, options = {}) => { try {
        if (!route) return false;
        const nextIndex = route.index;
        const previous = pages[current];
        const next = pages[nextIndex];
        if (!next) return false;
        if (activeTransition) { try { activeTransition.cancel(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@5757', __caughtJavaScriptError);  } activeTransition = null; }
        if (nextIndex === current) {
            next.hidden = false;
            next.setAttribute('aria-hidden', 'false');
            fitPage(next);
            if (options.updateHistory !== false) setHash(route, options.replace === true);
            return true;
        }
        const direction = nextIndex >= current ? 1 : -1;
        pausePageMedia(previous);
        next.hidden = false;
        next.setAttribute('aria-hidden', 'false');
        next.style.zIndex = '2';
        previous.style.zIndex = '1';
        fitPage(next);
        const duration = reducedMotion ? 1 : Math.max(80, numberValue(next.dataset.transitionDuration, .4) * 1000);
        const incoming = next.animate(transitionFrames(next, true, direction), { duration, easing: easing(next.dataset.transitionEasing), fill: 'both' });
        const outgoing = previous.animate(transitionFrames(next, false, direction), { duration, easing: easing(next.dataset.transitionEasing), fill: 'both' });
        activeTransition = incoming;
        try { await Promise.all([incoming.finished, outgoing.finished]); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@5776', __caughtJavaScriptError);  }
        previous.hidden = true;
        previous.setAttribute('aria-hidden', 'true');
        previous.style.zIndex = '';
        next.style.zIndex = '';
        try { incoming.cancel(); outgoing.cancel(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@5781', __caughtJavaScriptError);  }
        activeTransition = null;
        current = nextIndex;
        document.title = next.dataset.pageName ? `${next.dataset.pageName} · ${publication.dataset.publicationTitle || document.title}` : document.title;
        if (options.updateHistory !== false) setHash(route, options.replace === true);
        next.dispatchEvent(new CustomEvent('publisher:page-enter', { bubbles: true, detail: { pageId: route.id, pageName: next.dataset.pageName || '' } }));
        return true;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:showRoute@5751', __javascriptError); throw __javascriptError; }};
    const goToPage = (target, options = {}) => { try {
        const value = String(target ?? '').toLowerCase();
        const route = routes.find(item => { try { return (item.id.toLowerCase() === value || item.slug === value.replace(/^#\/?/, '') || String(item.index + 1) === value); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:routes.find@5791', __javascriptError); throw __javascriptError; } }) || routeFor(value);
        return showRoute(route, options);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:goToPage@5789', __javascriptError); throw __javascriptError; }};
    const next = () => { try { return (showRoute(routes[Math.min(routes.length - 1, current + 1)])); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:next@5794', __javascriptError); throw __javascriptError; } };
    const previous = () => { try { return (showRoute(routes[Math.max(0, current - 1)])); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:previous@5795', __javascriptError); throw __javascriptError; } };

    window.PublisherStudioNavigation = window.PublisherStudioSite = {
        goToPage,
        next,
        previous,
        currentPageId: () => { try { return (routes[current]?.id || null); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:currentPageId@5801', __javascriptError); throw __javascriptError; } },
        currentPageName: () => { try { return (pages[current]?.dataset.pageName || ''); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:currentPageName@5802', __javascriptError); throw __javascriptError; } },
        routes: routes.map(route => { try { return (({ id: route.id, slug: route.slug, name: route.page.dataset.pageName || `Page ${route.index + 1}` })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:routes.map@5803', __javascriptError); throw __javascriptError; } })
    };

    for (const page of pages) {
        const signalSourceIds = new Set([...page.querySelectorAll('[data-signal-enabled="true"][data-connector-id]')].flatMap(connector => { try {
            let settings;
            try { settings = JSON.parse(connector.dataset.signal || '{}'); } catch { settings = {}; }
            const trigger = lower(settings.trigger);
            const sourceId = String(connector.dataset.sourceElementId || '').trim();
            return sourceId && ['onclick', 'onhover'].includes(trigger) ? [sourceId] : [];
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...page.querySelectorAll(\'[data-signal-enabled="true"][data-connector@5807', __javascriptError); throw __javascriptError; }}));
        for (const node of page.querySelectorAll('[data-publication-element]')) {
            let interaction;
            try { interaction = JSON.parse(node.dataset.interaction || '{}'); } catch { interaction = {}; }
            const action = lower(interaction.action || node.dataset.interactionAction);
            const kind = lower(node.dataset.elementKind);
            const nativeInteractive = Boolean(node.dataset.mediaKind)
                || ['datavisual', 'devextremecomponent', 'livesource'].includes(kind)
                || Boolean(node.querySelector('video,audio,[data-ps-visual-config],[data-ps-component-config],button,a[href],input,select,textarea,[contenteditable="true"]'));
            const signalSource = signalSourceIds.has(String(node.dataset.elementId || ''));
            if (nativeInteractive || signalSource) node.classList.add('ps-pointer-owner');
            if (!nativeInteractive && !signalSource && (!action || action === 'none')
                && ['shape', 'wordart', 'barcode'].includes(kind))
                node.classList.add('ps-pointer-passive');
            if (!action || action === 'none') continue;
            node.classList.add('ps-interactive', 'ps-pointer-owner');
            node.classList.remove('ps-pointer-passive');
            node.addEventListener('click', event => { try {
                event.preventDefault();
                event.stopPropagation();
                if (action === 'nextpage') next();
                else if (action === 'previouspage') previous();
                else if (action === 'gotopage') goToPage(interaction.targetPageId);
                else if (action === 'openurl') {
                    const url = String(interaction.url || '').trim();
                    if (/^(https?:|mailto:)/i.test(url)) window.open(url, interaction.openInNewWindow === false ? '_self' : '_blank', 'noopener');
                } else {
                    const targetId = interaction.targetElementId || node.dataset.elementId;
                    const target = publication.querySelector(`[data-element-id="${CSS.escape(String(targetId || ''))}"]`);
                    if (!target) return;
                    if (action === 'togglevisibility') target.classList.toggle('ps-action-hidden');
                    else if (action === 'show') target.classList.remove('ps-action-hidden');
                    else if (action === 'hide') target.classList.add('ps-action-hidden');
                    else if (action === 'playmedia') target.querySelector('video,audio')?.play?.().catch?.((__promiseError) => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@5846', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:target.querySelector(\'video,audio\')?.play?.().catch@5846', __javascriptError); throw __javascriptError; }});
                    else if (action === 'pausemedia') target.querySelector('video,audio')?.pause?.();
                }
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:node.addEventListener@5830', __javascriptError); throw __javascriptError; }});
        }
    }

    addEventListener('resize', fit);
    addEventListener('hashchange', () => { try { return (showRoute(routeFor(location.hash), { updateHistory: false })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:addEventListener@5854', __javascriptError); throw __javascriptError; } });
    addEventListener('popstate', () => { try { return (showRoute(routeFor(location.hash), { updateHistory: false })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:addEventListener@5855', __javascriptError); throw __javascriptError; } });
    addEventListener('keydown', event => { try {
        if (event.key === 'PageDown' || event.key === 'ArrowRight') next();
        else if (event.key === 'PageUp' || event.key === 'ArrowLeft') previous();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:addEventListener@5856', __javascriptError); throw __javascriptError; }});
    fit();
    const initial = routeFor(location.hash);
    pages[0].hidden = initial.index !== 0;
    pages[0].setAttribute('aria-hidden', initial.index === 0 ? 'false' : 'true');
    current = initial.index;
    pages.forEach((page, index) => { try { page.hidden = index !== current; page.setAttribute('aria-hidden', index === current ? 'false' : 'true');  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:pages.forEach@5865', __javascriptError); throw __javascriptError; }});
    setHash(initial, true);
    fitPage(pages[current]);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:websiteSiteRuntime@5671', __javascriptError); throw __javascriptError; }}


function barcodeColor(value, fallback) { try {
    const text = String(value || '').trim();
    return /^#[0-9a-f]{3,8}$/i.test(text) || /^(rgb|hsl)a?\(/i.test(text) || /^[a-z]+$/i.test(text) ? text : fallback;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:barcodeColor@5871', __javascriptError); throw __javascriptError; }}

function barcodeEnumName(value, names, fallback) { try {
    if (Number.isInteger(value) && value >= 0 && value < names.length) return names[value];
    const numeric = Number(value);
    if (Number.isInteger(numeric) && String(value).trim() !== '' && numeric >= 0 && numeric < names.length) return names[numeric];
    const normalized = String(value ?? '').replace(/[^a-z0-9]/gi, '').toLowerCase();
    return names.find(name => { try { return (name.replace(/[^a-z0-9]/gi, '').toLowerCase() === normalized); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:names.find@5881', __javascriptError); throw __javascriptError; } }) || fallback;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:barcodeEnumName@5876', __javascriptError); throw __javascriptError; }}

function barcodeFormatToken(value) { try {
    return barcodeEnumName(value, ['QrCode', 'Code128', 'Code39', 'Ean13', 'UpcA', 'Itf14', 'Codabar'], 'Code128');
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:barcodeFormatToken@5884', __javascriptError); throw __javascriptError; }}

function barcodeFormatName(value) { try {
    const normalized = barcodeFormatToken(value).toLowerCase();
    return ({ qrcode: 'QR', code128: 'CODE128', code39: 'CODE39', ean13: 'EAN13', upca: 'UPC', itf14: 'ITF14', codabar: 'codabar' })[normalized] || 'CODE128';
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:barcodeFormatName@5888', __javascriptError); throw __javascriptError; }}

function barcodeCorrectionName(value) { try {
    return barcodeEnumName(value, ['L', 'M', 'Q', 'H'], 'M').toUpperCase();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:barcodeCorrectionName@5893', __javascriptError); throw __javascriptError; }}

function barcodeShapeName(value) { try {
    return barcodeEnumName(value, ['Square', 'Rounded', 'Dots'], 'Square').toLowerCase();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:barcodeShapeName@5897', __javascriptError); throw __javascriptError; }}

function generateQrSvg(options) { try {
    if (typeof window.qrcode !== 'function') throw new Error('QR-code generator did not load.');
    const value = String(options?.value || '').trim();
    if (!value) throw new Error('Barcode value cannot be empty.');
    const correction = barcodeCorrectionName(options?.errorCorrection);
    const qr = window.qrcode(0, correction);
    qr.addData(value, 'Byte');
    qr.make();
    const count = qr.getModuleCount();
    const margin = Math.max(0, Math.min(32, Number(options?.margin) || 0));
    const size = count + margin * 2;
    const foreground = barcodeColor(options?.foregroundColor, '#111827');
    const transparent = options?.transparentBackground === true;
    const background = barcodeColor(options?.backgroundColor, '#ffffff');
    const shape = barcodeShapeName(options?.moduleShape);
    const cells = [];
    for (let row = 0; row < count; row++) {
        for (let column = 0; column < count; column++) {
            if (!qr.isDark(row, column)) continue;
            const x = column + margin;
            const y = row + margin;
            if (shape === 'dots') cells.push(`<circle cx="${x + .5}" cy="${y + .5}" r=".39"/>`);
            else if (shape === 'rounded') cells.push(`<rect x="${x + .04}" y="${y + .04}" width=".92" height=".92" rx=".22" ry=".22"/>`);
            else cells.push(`<rect x="${x + .02}" y="${y + .02}" width=".96" height=".96"/>`);
        }
    }
    const backgroundMarkup = transparent ? '' : `<rect width="100%" height="100%" fill="${background}"/>`;
    const rendering = shape === 'square' ? 'crispEdges' : 'geometricPrecision';
    return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${size} ${size}" preserveAspectRatio="xMidYMid meet" shape-rendering="${rendering}" role="img" aria-label="QR code, ${correction} error correction" data-error-correction="${correction}" data-module-count="${count}" data-module-shape="${shape}" data-transparent-background="${transparent}" style="background:transparent"><title>QR code · correction ${correction} · ${shape} modules</title>${backgroundMarkup}<g fill="${foreground}">${cells.join('')}</g></svg>`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:generateQrSvg@5901', __javascriptError); throw __javascriptError; }}

function generateLinearBarcodeSvg(options) { try {
    if (typeof window.JsBarcode !== 'function') throw new Error('Barcode generator did not load.');
    const value = String(options?.value || '').trim();
    if (!value) throw new Error('Barcode value cannot be empty.');
    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    const transparent = options?.transparentBackground === true;
    let valid = true;
    const formatToken = barcodeFormatToken(options?.format);
    try {
        window.JsBarcode(svg, value, {
            format: barcodeFormatName(options?.format),
            lineColor: barcodeColor(options?.foregroundColor, '#111827'),
            background: transparent ? 'transparent' : barcodeColor(options?.backgroundColor, '#ffffff'),
            displayValue: options?.showText !== false,
            margin: Math.max(0, Math.min(64, Number(options?.margin) || 0)),
            width: Math.max(1, Math.min(8, Number(options?.lineWidth) || 2)),
            height: Math.max(24, Math.min(400, Number(options?.barHeight) || 90)),
            fontSize: Math.max(8, Math.min(72, Number(options?.fontSize) || 16)),
            textMargin: 4,
            valid: result => { try { valid = result;  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:valid@5951', __javascriptError); throw __javascriptError; }}
        });
    } catch (error) {
        throw new Error(`${formatToken} could not encode "${value}": ${error?.message || error}`);
    }
    if (!valid) throw new Error(`The value "${value}" is invalid for ${formatToken}.`);
    const width = Number(svg.getAttribute('width')) || 320;
    const height = Number(svg.getAttribute('height')) || 120;
    if (transparent) {
        svg.querySelectorAll('rect').forEach(rect => { try {
            const fill = String(rect.getAttribute('fill') || rect.style.fill || '').replace(/\s/g,'').toLowerCase();
            const widthAttribute = rect.getAttribute('width') || '';
            const heightAttribute = rect.getAttribute('height') || '';
            const rectWidth = number(widthAttribute, 0);
            const rectHeight = number(heightAttribute, 0);
            const x = number(rect.getAttribute('x'), 0);
            const y = number(rect.getAttribute('y'), 0);
            const percentageBackground = widthAttribute.includes('%') && heightAttribute.includes('%');
            const isCanvasBackground = x === 0 && y === 0 && (percentageBackground || (rectWidth >= width * .98 && rectHeight >= height * .98));
            if (isCanvasBackground || fill === 'transparent' || fill === 'rgba(0,0,0,0)' || fill === '#00000000') rect.remove();
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:svg.querySelectorAll(\'rect\').forEach@5960', __javascriptError); throw __javascriptError; }});
        svg.style.background = 'transparent';
    }
    svg.setAttribute('viewBox', `0 0 ${width} ${height}`);
    svg.setAttribute('width', '100%');
    svg.setAttribute('height', '100%');
    svg.setAttribute('preserveAspectRatio', 'xMidYMid meet');
    svg.setAttribute('role', 'img');
    svg.setAttribute('aria-label', `${barcodeFormatToken(options?.format)}: ${value}`);
    svg.setAttribute('data-transparent-background', String(transparent));
    if (barcodeShapeName(options?.moduleShape) === 'rounded')
        svg.querySelectorAll('rect').forEach(rect => { try {
            const barWidth = number(rect.getAttribute('width'), 0);
            if (barWidth > 0 && barWidth < width * .5) {
                const radius = Math.min(2, barWidth / 2);
                rect.setAttribute('rx', String(radius));
                rect.setAttribute('ry', String(radius));
            }
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:svg.querySelectorAll(\'rect\').forEach@5982', __javascriptError); throw __javascriptError; }});
    return svg.outerHTML;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:generateLinearBarcodeSvg@5932', __javascriptError); throw __javascriptError; }}

const barcodeLibraryLoads = new Map();

function loadBarcodeLibrary(src, available, errorMessage, timeoutMilliseconds = 5000) { try {
    if (available()) return Promise.resolve();
    if (barcodeLibraryLoads.has(src)) return barcodeLibraryLoads.get(src);

    const promise = new Promise((resolve, reject) => { try {
        let script = [...document.scripts].find(item => { try {
            try { return new URL(item.src, document.baseURI).pathname.endsWith(src); }
            catch { return false; }
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...document.scripts].find@6000', __javascriptError); throw __javascriptError; }});
        let settled = false;
        const finish = error => { try {
            if (settled) return;
            settled = true;
            clearTimeout(timer);
            script?.removeEventListener('load', loaded);
            script?.removeEventListener('error', failed);
            if (error || !available()) reject(error || new Error(errorMessage));
            else resolve();
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:finish@6005', __javascriptError); throw __javascriptError; }};
        const loaded = () => { try { return (finish()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:loaded@6014', __javascriptError); throw __javascriptError; } };
        const failed = () => { try { return (finish(new Error(errorMessage))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:failed@6015', __javascriptError); throw __javascriptError; } };
        const timer = setTimeout(() => { try { return (finish(new Error(errorMessage))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:setTimeout@6016', __javascriptError); throw __javascriptError; } }, timeoutMilliseconds);

        if (!script) {
            script = document.createElement('script');
            script.src = new URL(src, document.baseURI).href;
            script.async = true;
            document.head.appendChild(script);
        }
        script.addEventListener('load', loaded, { once: true });
        script.addEventListener('error', failed, { once: true });

        // The normal application path preloads the libraries before Blazor starts. Resolve
        // immediately in that case instead of waiting for a load event that already fired.
        queueMicrotask(() => { try { if (available()) finish();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:queueMicrotask@6029', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@5999', __javascriptError); throw __javascriptError; }});
    barcodeLibraryLoads.set(src, promise);
    promise.catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@6032', __promiseError);  return (barcodeLibraryLoads.delete(src)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:promise.catch@6032', __javascriptError); throw __javascriptError; } });
    return promise;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:loadBarcodeLibrary@5995', __javascriptError); throw __javascriptError; }}

async function waitForBarcodeGenerator(format) { try {
    const qr = format === 'qrcode';
    if (qr) {
        await loadBarcodeLibrary('js/vendor/qrcode-generator.js',
            () => { try { return (typeof window.qrcode === 'function'); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:loadBarcodeLibrary@6040', __javascriptError); throw __javascriptError; } }, 'QR-code generator did not load.');
        return;
    }
    await loadBarcodeLibrary('js/vendor/JsBarcode.all.min.js',
        () => { try { return (typeof window.JsBarcode === 'function'); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:loadBarcodeLibrary@6044', __javascriptError); throw __javascriptError; } }, 'Barcode generator did not load.');
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:waitForBarcodeGenerator@6036', __javascriptError); throw __javascriptError; }}

export async function generateBarcodeSvg(options) { try {
    const format = barcodeFormatToken(options?.format).toLowerCase();
    await waitForBarcodeGenerator(format);
    return format === 'qrcode' ? generateQrSvg(options) : generateLinearBarcodeSvg(options);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:generateBarcodeSvg@6047', __javascriptError); throw __javascriptError; }}

function sleep(milliseconds) { try { return new Promise(resolve => { try { return (setTimeout(resolve, milliseconds)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@6053', __javascriptError); throw __javascriptError; } });  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:sleep@6053', __javascriptError); throw __javascriptError; }}

function chooseVideoRecordingMimeType() { try {
    if (typeof MediaRecorder === 'undefined') return '';
    const probe = document.createElement('video');
    const candidates = ['video/webm;codecs=vp8,opus', 'video/webm', 'video/webm;codecs=vp9,opus'];
    return candidates.find(type => { try { return (MediaRecorder.isTypeSupported(type) && probe.canPlayType(type) !== ''); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:candidates.find@6059', __javascriptError); throw __javascriptError; } })
        || candidates.find(type => { try { return (MediaRecorder.isTypeSupported(type)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:candidates.find@6060', __javascriptError); throw __javascriptError; } })
        || '';
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:chooseVideoRecordingMimeType@6055', __javascriptError); throw __javascriptError; }}

function exportedPageDuration(page) { try {
    const transitionDuration = Math.max(.1, animationNumber(page.dataset.transitionDuration, .55));
    const signalNodes = [...page.querySelectorAll('[data-signal-enabled="true"][data-signal][data-connector-id]')];
    const signalSettings = new Map(signalNodes.map(node => { try {
        let settings = {}; try { settings = JSON.parse(node.dataset.signal || '{}'); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@6068', __caughtJavaScriptError);  }
        return [String(node.dataset.connectorId || ''), settings];
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:signalNodes.map@6067', __javascriptError); throw __javascriptError; }}));
    const signalOwnDuration = settings => { try {
        const repeats = Math.max(1, animationNumber(settings?.repeatCount, 1)) * (settings?.autoReverse ? 2 : 1);
        return Math.max(0, animationNumber(settings?.delaySeconds, 0))
            + Math.max(.05, animationNumber(settings?.durationSeconds, 1.5)) * repeats
            + Math.max(0, animationNumber(settings?.completionDurationSeconds, 0));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:signalOwnDuration@6071', __javascriptError); throw __javascriptError; }};
    const signalChainDuration = (id, seen = new Set()) => { try {
        if (!id || seen.has(id)) return 0;
        const settings = signalSettings.get(String(id));
        if (!settings) return 0;
        const nextSeen = new Set(seen); nextSeen.add(String(id));
        const action = animationName(settings.completionAction);
        const next = action === 'runsignal' ? (settings.completionValue || settings.nextConnectorId) : settings.nextConnectorId;
        return signalOwnDuration(settings) + signalChainDuration(String(next || ''), nextSeen);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:signalChainDuration@6077', __javascriptError); throw __javascriptError; }};
    const autoSignalIds = signalNodes
        .filter(node => { try { const settings = signalSettings.get(String(node.dataset.connectorId || '')); return animationName(settings?.trigger) === 'onpageenter';  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:signalNodes .filter@6087', __javascriptError); throw __javascriptError; }})
        .map(node => { try { return (String(node.dataset.connectorId || '')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:signalNodes .filter(node => { const settings = signalSettings.get(Stri@6088', __javascriptError); throw __javascriptError; } });
    const signalDuration = Math.max(0, ...(autoSignalIds.length ? autoSignalIds.map(id => { try { return (signalChainDuration(id)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:autoSignalIds.map@6089', __javascriptError); throw __javascriptError; } }) : [...signalSettings.values()].map(signalOwnDuration)));
    let duration = Math.max(2.5, transitionDuration + .3, signalDuration + .3, animationNumber(page.dataset.timelineDuration, 0), animationNumber(page.dataset.autoAdvanceSeconds, 0));
    let cursor = 0;
    for (const item of animationItems(page)) {
        const animation = item.animation || {};
        const explicit = Number(animation.timelineStartSeconds);
        const delay = Math.max(0, animationNumber(animation.delaySeconds, 0));
        const start = Number.isFinite(explicit) ? Math.max(0, explicit) : cursor + delay;
        const span = publicationAnimationSpan(animation);
        duration = Math.max(duration, start + span + .3);
        cursor = Math.max(cursor, start + span);
    }
    for (const node of page.querySelectorAll('[data-media-kind]')) {
        const start = Math.max(0, animationNumber(node.dataset.mediaStart, 0));
        const trimStart = Math.max(0, animationNumber(node.dataset.mediaTrimStart, 0));
        const trimEnd = Math.max(trimStart, animationNumber(node.dataset.mediaTrimEnd, trimStart));
        const rate = Math.max(.1, animationNumber(node.dataset.mediaRate, 1));
        duration = Math.max(duration, start + (trimEnd - trimStart) / rate + .3);
    }
    return duration;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:exportedPageDuration@6064', __javascriptError); throw __javascriptError; }}

function prepareVideoExportPage(page) { try {
    page.querySelectorAll('[data-media-kind]').forEach(node => { try {
        if (animationName(node.dataset.mediaTrigger) === 'onclick') node.dataset.mediaTrigger = 'OnPageEnter';
        node.dataset.mediaAutoplay = 'true';
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:page.querySelectorAll(\'[data-media-kind]\').forEach@6112', __javascriptError); throw __javascriptError; }});
    return animationItems(page).map(item => { try { return (({
        node: item.node,
        animation: animationName(item.animation.trigger) === 'onclick'
            ? { ...item.animation, trigger: 'AfterPrevious', timelineStartSeconds: null }
            : item.animation
    })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:animationItems(page).map@6116', __javascriptError); throw __javascriptError; } });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:prepareVideoExportPage@6111', __javascriptError); throw __javascriptError; }}

async function requestPresentationCapture() { try {
    if (!navigator.mediaDevices?.getDisplayMedia)
        throw new Error('This browser does not support tab/screen capture video export.');
    try {
        return await navigator.mediaDevices.getDisplayMedia({
            video: { frameRate: { ideal: 30, max: 60 } },
            audio: true,
            preferCurrentTab: true,
            selfBrowserSurface: 'include',
            surfaceSwitching: 'exclude',
            systemAudio: 'include'
        });
    } catch (error) {
        if (error?.name === 'TypeError')
            return await navigator.mediaDevices.getDisplayMedia({ video: true, audio: true });
        throw error;
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:requestPresentationCapture@6124', __javascriptError); throw __javascriptError; }}

function evenVideoDimension(value, fallback) { try {
    const rounded = Math.max(2, Math.round(Number(value) || fallback || 2));
    return rounded % 2 === 0 ? rounded : rounded + 1;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:evenVideoDimension@6143', __javascriptError); throw __javascriptError; }}

function pagePresentationSize(page) { try {
    const widthMm = number(page.dataset.pageWidthMm, 0);
    const heightMm = number(page.dataset.pageHeightMm, 0);
    const exportWidth = number(page.dataset.exportWidthPx, 0);
    const exportHeight = number(page.dataset.exportHeightPx, 0);
    if (exportWidth > 0 && exportHeight > 0)
        return { width: exportWidth, height: exportHeight, area: exportWidth * exportHeight };
    if (widthMm > 0 && heightMm > 0) {
        const width = widthMm * PX_PER_MM_AT_96_DPI;
        const height = heightMm * PX_PER_MM_AT_96_DPI;
        return { width, height, area: width * height };
    }

    const previousTransform = page.style.transform;
    const previousTranslate = page.style.translate;
    const wasHidden = page.hidden;
    page.hidden = false;
    page.style.transform = 'none';
    page.style.translate = 'none';
    const bounds = page.getBoundingClientRect();
    const computed = getComputedStyle(page);
    const width = Math.max(1, bounds.width || parseFloat(computed.width) || number(page.style.width, 800));
    const height = Math.max(1, bounds.height || parseFloat(computed.height) || number(page.style.height, 600));
    page.style.transform = previousTransform;
    page.style.translate = previousTranslate;
    page.hidden = wasHidden;
    return { width, height, area: width * height };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pagePresentationSize@6148', __javascriptError); throw __javascriptError; }}

function publicationFrameDefinition(pages, evenDimensions = false) { try {
    const measured = pages.map(pagePresentationSize);
    const fallback = { width: 1280, height: 720, area: 1280 * 720 };
    if (!measured.length)
        return { width: fallback.width, height: fallback.height, pageSizes: [] };

    // A publication containing any landscape page exports to a landscape frame.
    // Every page still participates in the maximum-size calculation: mixed portrait
    // pages contribute their long side to frame width and their short side to frame
    // height. Portrait-only publications keep their native portrait orientation.
    const landscape = measured.some(size => { try { return (size.width > size.height + .5); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:measured.some@6187', __javascriptError); throw __javascriptError; } });
    let width = landscape
        ? Math.max(...measured.map(size => { try { return (Math.max(size.width, size.height)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:measured.map@6189', __javascriptError); throw __javascriptError; } }))
        : Math.max(...measured.map(size => { try { return (size.width); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:measured.map@6190', __javascriptError); throw __javascriptError; } }));
    let height = landscape
        ? Math.max(...measured.map(size => { try { return (Math.min(size.width, size.height)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:measured.map@6192', __javascriptError); throw __javascriptError; } }))
        : Math.max(...measured.map(size => { try { return (size.height); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:measured.map@6193', __javascriptError); throw __javascriptError; } }));
    if (!(width > 0) || !(height > 0)) {
        width = fallback.width;
        height = fallback.height;
    }

    return {
        width: evenDimensions ? evenVideoDimension(width, fallback.width) : Math.max(1, Math.round(width)),
        height: evenDimensions ? evenVideoDimension(height, fallback.height) : Math.max(1, Math.round(height)),
        pageSizes: measured,
        landscape
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:publicationFrameDefinition@6177', __javascriptError); throw __javascriptError; }}

async function restrictPresentationCapture(capture, target, targetWidth, targetHeight) { try {
    const videoTrack = capture.getVideoTracks()[0];
    if (!videoTrack) throw new Error('The selected capture surface did not provide a video track.');
    let restricted = false;
    try {
        if (typeof RestrictionTarget !== 'undefined' && typeof RestrictionTarget.fromElement === 'function' && typeof videoTrack.restrictTo === 'function') {
            const restrictionTarget = await RestrictionTarget.fromElement(target);
            await videoTrack.restrictTo(restrictionTarget);
            restricted = true;
        } else if (typeof CropTarget !== 'undefined' && typeof CropTarget.fromElement === 'function' && typeof videoTrack.cropTo === 'function') {
            const cropTarget = await CropTarget.fromElement(target);
            await videoTrack.cropTo(cropTarget);
            restricted = true;
        }
    } catch (error) {
        console.warn('Publisher video export could not crop the capture to the publication frame. Falling back to full-tab capture.', error);
    }

    if (restricted && typeof videoTrack.applyConstraints === 'function') {
        try {
            await videoTrack.applyConstraints({
                width: { ideal: targetWidth },
                height: { ideal: targetHeight },
                frameRate: { ideal: 30, max: 60 }
            });
        } catch (error) {
            console.warn('Publisher video export could not request the publication frame resolution.', error);
        }
    }
    return restricted;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:restrictPresentationCapture@6207', __javascriptError); throw __javascriptError; }}

function waitForVideoMetadata(video, timeoutMilliseconds = 12000) { try {
    if (video.readyState >= HTMLMediaElement.HAVE_METADATA && video.videoWidth > 0 && video.videoHeight > 0)
        return Promise.resolve();
    return new Promise((resolve, reject) => { try {
        const timeout = setTimeout(() => { try { return (finish(new Error('The selected tab capture did not produce video frames.'))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:setTimeout@6243', __javascriptError); throw __javascriptError; } }, timeoutMilliseconds);
        const finish = error => { try {
            clearTimeout(timeout);
            video.removeEventListener('loadedmetadata', loaded);
            video.removeEventListener('canplay', loaded);
            video.removeEventListener('error', failed);
            if (error) reject(error); else resolve();
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:finish@6244', __javascriptError); throw __javascriptError; }};
        const loaded = () => { try { return (video.videoWidth > 0 && video.videoHeight > 0 && finish()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:loaded@6251', __javascriptError); throw __javascriptError; } };
        const failed = () => { try { return (finish(video.error || new Error('The selected tab capture could not be decoded.'))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:failed@6252', __javascriptError); throw __javascriptError; } };
        video.addEventListener('loadedmetadata', loaded);
        video.addEventListener('canplay', loaded);
        video.addEventListener('error', failed);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@6242', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:waitForVideoMetadata@6239', __javascriptError); throw __javascriptError; }}

async function createPageFrameRecordingStream(capture, frame, targetWidth, targetHeight) { try {
    const captureVideoTrack = capture.getVideoTracks()[0];
    if (!captureVideoTrack) throw new Error('The selected capture surface did not provide a video track.');
    const sourceVideo = document.createElement('video');
    sourceVideo.muted = true;
    sourceVideo.playsInline = true;
    sourceVideo.autoplay = true;
    sourceVideo.srcObject = new MediaStream([captureVideoTrack]);
    await sourceVideo.play();
    await waitForVideoMetadata(sourceVideo);

    const canvas = document.createElement('canvas');
    canvas.width = evenVideoDimension(targetWidth, 1280);
    canvas.height = evenVideoDimension(targetHeight, 720);
    const context = canvas.getContext('2d', { alpha: false, desynchronized: true });
    if (!context) throw new Error('The browser could not create the video compositor canvas.');
    const canvasStream = canvas.captureStream(30);
    const output = new MediaStream();
    const canvasTrack = canvasStream.getVideoTracks()[0];
    if (!canvasTrack) throw new Error('The browser could not create a page-sized video track.');
    output.addTrack(canvasTrack);
    capture.getAudioTracks().forEach(track => { try { return (output.addTrack(track)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:capture.getAudioTracks().forEach@6280', __javascriptError); throw __javascriptError; } });

    let animationFrame = 0;
    let stopped = false;
    const draw = () => { try {
        if (stopped) return;
        const sourceWidth = sourceVideo.videoWidth;
        const sourceHeight = sourceVideo.videoHeight;
        const viewportWidth = Math.max(1, window.visualViewport?.width || window.innerWidth);
        const viewportHeight = Math.max(1, window.visualViewport?.height || window.innerHeight);
        const frameBounds = frame.getBoundingClientRect();
        const scaleX = sourceWidth / viewportWidth;
        const scaleY = sourceHeight / viewportHeight;
        let sourceX = frameBounds.left * scaleX;
        let sourceY = frameBounds.top * scaleY;
        let sourceCropWidth = frameBounds.width * scaleX;
        let sourceCropHeight = frameBounds.height * scaleY;

        sourceX = clamp(sourceX, 0, Math.max(0, sourceWidth - 1));
        sourceY = clamp(sourceY, 0, Math.max(0, sourceHeight - 1));
        sourceCropWidth = clamp(sourceCropWidth, 1, sourceWidth - sourceX);
        sourceCropHeight = clamp(sourceCropHeight, 1, sourceHeight - sourceY);
        context.fillStyle = '#090d14';
        context.fillRect(0, 0, canvas.width, canvas.height);
        try {
            context.drawImage(sourceVideo, sourceX, sourceY, sourceCropWidth, sourceCropHeight, 0, 0, canvas.width, canvas.height);
        } catch (error) {
            console.warn('Publisher video compositor skipped one frame.', error);
        }
        animationFrame = requestAnimationFrame(draw);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:draw@6284', __javascriptError); throw __javascriptError; }};
    draw();

    return {
        stream: output,
        displaySurface: captureVideoTrack.getSettings?.().displaySurface || '',
        stop() { try {
            stopped = true;
            if (animationFrame) cancelAnimationFrame(animationFrame);
            try { sourceVideo.pause(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@6319', __caughtJavaScriptError);  }
            sourceVideo.srcObject = null;
            canvasTrack.stop();
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:stop@6316', __javascriptError); throw __javascriptError; }}
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:createPageFrameRecordingStream@6259', __javascriptError); throw __javascriptError; }}

async function exportPresentationVideo(containerSelector, fileName, title) { try {
    let step = 'initializing';
    if (typeof MediaRecorder === 'undefined') throw new Error('This browser does not support MediaRecorder video export.');
    if (typeof HTMLCanvasElement.prototype.captureStream !== 'function')
        throw new Error('This browser cannot record the page-sized compositor canvas.');
    const source = document.querySelector(containerSelector);
    if (!source) throw new Error('The publication export surface is not available.');
    const sourcePages = [...source.querySelectorAll(':scope > .print-page')];
    if (!sourcePages.length) throw new Error('The publication does not contain any pages.');
    if (window.PublisherStudioLiveDataRuntime) {
        await window.PublisherStudioLiveDataRuntime.refreshAll(source, { polling: false });
    }
    refreshContentFit(source);
    await new Promise(resolve => { try { return (requestAnimationFrame(resolve)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@6339', __javascriptError); throw __javascriptError; } });

    const overlay = document.createElement('div');
    overlay.className = 'publisher-video-export-overlay';
    overlay.setAttribute('aria-label', `${title || 'Publication'} video export`);

    const frame = document.createElement('div');
    frame.className = 'publisher-video-export-frame';
    const publication = source.cloneNode(true);
    publication.removeAttribute('aria-hidden');
    publication.className = 'publisher-video-export-publication';
    const pages = [...publication.querySelectorAll(':scope > .print-page')];
    pages.forEach((page, index) => { try {
        page.id = `publisher-video-export-page-${index}-${Date.now()}`;
        page.querySelectorAll('video,audio').forEach(media => { try { media.controls = false; media.preload = 'auto';  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:page.querySelectorAll(\'video,audio\').forEach@6353', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:pages.forEach@6351', __javascriptError); throw __javascriptError; }});
    const pageShells = pages.map(page => { try {
        const shell = document.createElement('div');
        shell.className = 'publisher-video-page-shell';
        page.before(shell);
        shell.appendChild(page);
        shell.hidden = true;
        return shell;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:pages.map@6355', __javascriptError); throw __javascriptError; }});
    frame.appendChild(publication);

    const countdown = document.createElement('div');
    countdown.className = 'publisher-video-export-countdown';
    countdown.textContent = 'Select This Tab and enable tab audio when needed.';
    const cancelButton = document.createElement('button');
    cancelButton.type = 'button';
    cancelButton.className = 'publisher-video-export-cancel';
    cancelButton.textContent = 'Cancel export and return';
    let cancelled = false;
    const cancelExport = () => { try {
        cancelled = true;
        if (recorder && recorder.state !== 'inactive') { try { recorder.stop(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@6375', __caughtJavaScriptError);  } }
        if (capture) capture.getTracks().forEach(track => { try { try { track.stop(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@6376', __caughtJavaScriptError);  }  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:capture.getTracks().forEach@6376', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:cancelExport@6373', __javascriptError); throw __javascriptError; }};
    const waitForExport = async milliseconds => { try {
        const end = performance.now() + Math.max(0, milliseconds);
        while (performance.now() < end) {
            if (cancelled) throw new Error('Video export was cancelled.');
            await sleep(Math.min(120, Math.max(1, end - performance.now())));
        }
        if (cancelled) throw new Error('Video export was cancelled.');
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:waitForExport@6378', __javascriptError); throw __javascriptError; }};
    const cancelOnEscape = event => { try { if (event.key === 'Escape') cancelExport();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:cancelOnEscape@6386', __javascriptError); throw __javascriptError; }};
    cancelButton.addEventListener('click', cancelExport);
    window.addEventListener('keydown', cancelOnEscape, true);
    activeVideoExportCancel?.();
    activeVideoExportCancel = cancelExport;
    overlay.append(frame, countdown, cancelButton);
    document.body.appendChild(overlay);

    const frameDefinition = publicationFrameDefinition(pages, true);
    pageShells.forEach((shell, index) => { try { return (shell.hidden = index !== 0); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:pageShells.forEach@6395', __javascriptError); throw __javascriptError; } });
    const frameWidth = frameDefinition.width;
    const frameHeight = frameDefinition.height;
    frame.style.width = `${frameWidth}px`;
    frame.style.height = `${frameHeight}px`;
    frame.style.setProperty('--publisher-video-frame-width', `${frameWidth}px`);
    frame.style.setProperty('--publisher-video-frame-height', `${frameHeight}px`);

    const fitPage = (page, pageIndex = pages.indexOf(page)) => { try {
        const measured = frameDefinition.pageSizes[pageIndex] || pagePresentationSize(page);
        const scale = Math.min(frameWidth / measured.width, frameHeight / measured.height);
        page.style.width = `${measured.width}px`;
        page.style.height = `${measured.height}px`;
        page.style.transform = `translate(-50%, -50%) scale(${Math.max(.01, scale)})`;
        page.style.transformOrigin = 'center center';
        page.style.translate = 'none';
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:fitPage@6403', __javascriptError); throw __javascriptError; }};
    const fitFrameToViewport = () => { try {
        const viewportWidth = window.visualViewport?.width || innerWidth;
        const viewportHeight = window.visualViewport?.height || innerHeight;
        const scale = Math.min((viewportWidth - 32) / frameWidth, (viewportHeight - 32) / frameHeight, 1);
        frame.style.transform = `scale(${Math.max(.05, scale)})`;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:fitFrameToViewport@6412', __javascriptError); throw __javascriptError; }};
    pages.forEach(fitPage);
    const videoSignals = signalConnectorRuntime(publication, { autoStart: false, expose: false, finiteLoops: true });
    refreshContentFit(publication);
    fitFrameToViewport();
    window.addEventListener('resize', fitFrameToViewport);

    let capture = null;
    let compositor = null;
    let recorder = null;
    let stopped = null;
    const chunks = [];
    let totalDuration = 0;
    try {
        step = 'requesting tab capture';
        await waitForExport(120);
        capture = await requestPresentationCapture();
        if (cancelled) throw new Error('Video export was cancelled.');
        capture.getVideoTracks()[0]?.addEventListener('ended', cancelExport, { once: true });
        step = 'creating the page-sized compositor';
        compositor = await createPageFrameRecordingStream(capture, frame, frameWidth, frameHeight);

        const mimeType = chooseVideoRecordingMimeType();
        const pixels = frameWidth * frameHeight;
        const videoBitsPerSecond = Math.max(4_000_000, Math.min(20_000_000, Math.round(pixels * 8)));
        step = 'starting MediaRecorder';
        recorder = new MediaRecorder(compositor.stream, mimeType
            ? { mimeType, videoBitsPerSecond }
            : { videoBitsPerSecond });
        recorder.addEventListener('dataavailable', event => { try { if (event.data?.size) chunks.push(event.data);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:recorder.addEventListener@6446', __javascriptError); throw __javascriptError; }});
        stopped = new Promise((resolve, reject) => { try {
            recorder.addEventListener('stop', resolve, { once: true });
            recorder.addEventListener('error', event => { try { return (reject(event.error || new Error('Video recording failed.'))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:recorder.addEventListener@6449', __javascriptError); throw __javascriptError; } }, { once: true });
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@6447', __javascriptError); throw __javascriptError; }});

        for (let count = 3; count > 0; count--) {
            if (cancelled) throw new Error('Video export was cancelled.');
            const sourceLabel = compositor.displaySurface && compositor.displaySurface !== 'browser'
                ? `Selected ${compositor.displaySurface}; This Tab is recommended`
                : 'Page-sized tab recording';
            countdown.textContent = `${sourceLabel} starts in ${count}`;
            await waitForExport(700);
        }
        countdown.remove();
        recorder.start(500);
        step = 'recording publication pages';
        for (let index = 0; index < pages.length; index++) {
            if (cancelled) throw new Error('Video export was cancelled.');
            const page = pages[index];
            const shell = pageShells[index];
            pageShells.forEach((candidate, candidateIndex) => { try { return (candidate.hidden = candidateIndex !== index); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:pageShells.forEach@6467', __javascriptError); throw __javascriptError; } });
            fitPage(page, index);
            if (window.PublisherStudioLiveDataRuntime) {
                await window.PublisherStudioLiveDataRuntime.refreshAll(page, { polling: false });
                fitPage(page, index);
            }
            await waitForExport(120);
            const duration = exportedPageDuration(page);
            totalDuration += duration;
            // Animate the fixed-size shell rather than the fitted page. Page transition
            // transforms therefore no longer overwrite the page's centering/scale.
            previewPublicationItems(page, prepareVideoExportPage(page), true, shell);
            videoSignals?.stop();
            videoSignals?.startPage(page);
            await waitForExport(duration * 1000);
            videoSignals?.stop();
            if (cancelled) throw new Error('Video export was cancelled.');
            clearPublicationPreview(page.id || page);
            page.querySelectorAll('video,audio').forEach(media => { try {
                try { media.pause(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@6486', __caughtJavaScriptError);  }
                try { media.currentTime = 0; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@6487', __caughtJavaScriptError);  }
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:page.querySelectorAll(\'video,audio\').forEach@6485', __javascriptError); throw __javascriptError; }});
        }

        step = 'finalizing WebM';
        if (recorder.state === 'recording') {
            try { recorder.requestData(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@6493', __caughtJavaScriptError);  }
            await waitForExport(120);
            recorder.stop();
        }
        await Promise.race([
            stopped,
            new Promise((_, reject) => { try { return (setTimeout(() => { try { return (reject(new Error('MediaRecorder did not finish the WebM file.'))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:setTimeout@6499', __javascriptError); throw __javascriptError; } }, 15000)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@6499', __javascriptError); throw __javascriptError; } })
        ]);
        const blobType = String(recorder.mimeType || 'video/webm').split(';', 1)[0] || 'video/webm';
        const blob = new Blob(chunks, { type: blobType });
        if (!blob.size) throw new Error('The browser completed the capture but produced an empty video.');
        step = 'downloading WebM';
        downloadBlob(fileName || 'publication.webm', blob);
        let assetId = '';
        try {
            assetId = crypto.randomUUID();
            const response = await fetch(`/api/assets/drop/${assetId}`, {
                method: 'POST',
                headers: { 'Content-Type': blobType },
                body: blob
            });
            if (!response.ok) assetId = '';
        } catch (error) {
            assetId = '';
            console.warn('The exported video was downloaded but could not be retained for Media Converter Studio.', error);
        }
        return {
            fileName: fileName || 'publication.webm',
            durationSeconds: totalDuration,
            width: frameWidth,
            height: frameHeight,
            pageSizedCapture: true,
            assetId,
            mimeType: blobType,
            sizeBytes: blob.size
        };
    } catch (error) {
        const message = error?.message || String(error);
        console.error(`Publisher video export failed while ${step}.`, error);
        throw new Error(`Video export failed while ${step}: ${message}`);
    } finally {
        if (recorder && recorder.state !== 'inactive') { try { recorder.stop(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@6534', __caughtJavaScriptError);  } }
        try { compositor?.stop(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@6535', __caughtJavaScriptError);  }
        if (capture) capture.getTracks().forEach(track => { try { try { track.stop(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@6536', __caughtJavaScriptError);  }  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:capture.getTracks().forEach@6536', __javascriptError); throw __javascriptError; }});
        window.removeEventListener('resize', fitFrameToViewport);
        window.removeEventListener('keydown', cancelOnEscape, true);
        if (activeVideoExportCancel === cancelExport) activeVideoExportCancel = null;
        try { window.PublisherStudioLiveDataRuntime?.dispose(overlay); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@6540', __caughtJavaScriptError);  }
        try { videoSignals?.dispose(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@6541', __caughtJavaScriptError);  }
        overlay.remove();
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:exportPresentationVideo@6326', __javascriptError); throw __javascriptError; }}

const storyEditorLayouts = new WeakMap();

function initializeStoryEditorLayout(shellId, hostId, dotNetReference = null) { try {
    const shell = document.getElementById(shellId);
    const host = document.getElementById(hostId);
    if (!shell || !host) return;
    let state = storyEditorLayouts.get(shell);
    if (state) {
        state.host = host;
        if (dotNetReference) state.dotNet = dotNetReference;
        state.schedule();
        return;
    }
    let timer = 0;
    const refresh = () => { try {
        timer = 0;
        if (!shell.isConnected || !host.isConnected) return;
        host.style.maxWidth = `${Math.max(1, shell.clientWidth)}px`;
        host.scrollLeft = 0;
        const richRoot = host.firstElementChild;
        if (richRoot instanceof HTMLElement) {
            richRoot.style.width = '100%';
            richRoot.style.maxWidth = '100%';
            richRoot.style.minWidth = '0';
        }
        window.dispatchEvent(new Event('resize'));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:refresh@6560', __javascriptError); throw __javascriptError; }};
    const schedule = () => { try {
        if (timer) clearTimeout(timer);
        timer = window.setTimeout(refresh, 40);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:schedule@6573', __javascriptError); throw __javascriptError; }};
    const click = event => { try {
        const printCommand = reserveStoryPrintPreviewFromEvent(event, host);
        if (printCommand === 'rich-edit') {
            event.preventDefault();
            event.stopPropagation();
            event.stopImmediatePropagation();
            state?.dotNet?.invokeMethodAsync('PrintStoryFromClient').catch(error =>
                { try { return (console.error('Story print preview could not be started.', error)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:state?.dotNet?.invokeMethodAsync(\'PrintStoryFromClient\').catch@6583', __javascriptError); throw __javascriptError; } });
            return;
        }
        if (!event.target.closest('button,[role="tab"],[role="button"]')) return;
        schedule();
        window.setTimeout(schedule, 120);
        window.setTimeout(schedule, 320);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:click@6577', __javascriptError); throw __javascriptError; }};
    const keydown = event => { try {
        if (!(event.ctrlKey || event.metaKey) || event.altKey || String(event.key || '').toLowerCase() !== 'p') return;
        if (!host.contains(event.target)) return;
        const current = storyPrintPreviews.get(reservedStoryPrintPreviewId);
        if (!current?.previewWindow || current.previewWindow.closed)
            reservedStoryPrintPreviewId = openStoryPrintPreview('Story print preview');
        event.preventDefault();
        event.stopPropagation();
        event.stopImmediatePropagation();
        state?.dotNet?.invokeMethodAsync('PrintStoryFromClient').catch(error =>
            { try { return (console.error('Story print preview could not be started.', error)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:state?.dotNet?.invokeMethodAsync(\'PrintStoryFromClient\').catch@6601', __javascriptError); throw __javascriptError; } });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:keydown@6592', __javascriptError); throw __javascriptError; }};
    shell.addEventListener('click', click, true);
    shell.addEventListener('keydown', keydown, true);
    const resizeObserver = typeof ResizeObserver === 'function' ? new ResizeObserver(schedule) : null;
    resizeObserver?.observe(shell);
    resizeObserver?.observe(host);
    state = { host, schedule, resizeObserver, click, keydown, dotNet: dotNetReference };
    storyEditorLayouts.set(shell, state);
    schedule();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:initializeStoryEditorLayout@6548', __javascriptError); throw __javascriptError; }}


let dataVisualLayoutTimer = 0;
export function refreshDataVisualLayout(pageId = 'publisher-page') { try {
    const page = document.getElementById(pageId);
    if (!page?.querySelector?.('.data-visual-view')) return;
    if (dataVisualLayoutTimer) clearTimeout(dataVisualLayoutTimer);
    requestAnimationFrame(() => { try {
        window.dispatchEvent(new Event('resize'));
        dataVisualLayoutTimer = window.setTimeout(() => { try {
            dataVisualLayoutTimer = 0;
            if (page.isConnected) window.dispatchEvent(new Event('resize'));
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:window.setTimeout@6622', __javascriptError); throw __javascriptError; }}, 120);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:requestAnimationFrame@6620', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:refreshDataVisualLayout@6616', __javascriptError); throw __javascriptError; }}

export function cancelCanvasInteraction(stageId = 'publisher-stage') { try {
    const stage = document.getElementById(stageId);
    const state = stage ? canvasStates.get(stage) : null;
    if (!state) return;
    resetCanvasTransientState(state, true);
    state.lastCanvasClick = null;
    try { state.stage.focus({ preventScroll: true }); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@6635', __caughtJavaScriptError);  }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:cancelCanvasInteraction@6629', __javascriptError); throw __javascriptError; }}

function panelStudioCoordinateSurface(element) { try {
    if (!(element instanceof HTMLElement)) return null;
    const viewport = element.querySelector('.publication-panel > .publication-panel-viewport[data-panel-authoring-viewport="true"]');
    const canvasRegion = viewport?.querySelector?.(':scope > [data-panel-canvas-region]');
    if (canvasRegion instanceof HTMLElement) return canvasRegion;
    return viewport instanceof HTMLElement ? viewport : element;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:panelStudioCoordinateSurface@6638', __javascriptError); throw __javascriptError; }}

function syncPanelStudioDesignSurface(element) { try {
    if (!(element instanceof HTMLElement)) return;
    const width = Math.max(1, Number.parseFloat(element.dataset.panelStudioDesignWidth || '') || 1);
    const height = Math.max(1, Number.parseFloat(element.dataset.panelStudioDesignHeight || '') || 1);
    const bounds = element.getBoundingClientRect();
    const availableWidth = Math.max(1, bounds.width - 56);
    const availableHeight = Math.max(1, bounds.height - 56);
    // Panel Studio is an authoring viewport, not a physical-size preview. Preserve the selected
    // panel's own aspect ratio and coordinate system, but zoom it uniformly so the editable panel
    // remains human-usable on large monitors and still shrinks on constrained viewports.
    const scale = Math.max(.05, Math.min(8, availableWidth / width, availableHeight / height));
    element.style.setProperty('--panel-studio-fit-scale', String(scale));
    element.dataset.panelStudioFitScale = String(scale);
    const frame = element.querySelector('[data-panel-studio-design-frame]');
    if (frame instanceof HTMLElement) frame.dataset.panelStudioFitScale = String(scale);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:syncPanelStudioDesignSurface@6645', __javascriptError); throw __javascriptError; }}

export function refreshPanelStudioDesignSurface(element) { try {
    if (!(element instanceof HTMLElement)) return false;
    syncPanelStudioDesignSurface(element);
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:refreshPanelStudioDesignSurface', __javascriptError); throw __javascriptError; }}

export function panelStudioPoint(element, clientX, clientY) { try {
    if (!(element instanceof HTMLElement)) return { x: 0.5, y: 0.5 };
    const coordinateSurface = panelStudioCoordinateSurface(element) || element;
    const bounds = coordinateSurface.getBoundingClientRect();
    const width = Math.max(1, bounds.width);
    const height = Math.max(1, bounds.height);
    return {
        x: clamp((Number(clientX) - bounds.left) / width, 0, 1),
        y: clamp((Number(clientY) - bounds.top) / height, 0, 1)
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:panelStudioPoint@6645', __javascriptError); throw __javascriptError; }}

const panelStudioDropBindings = new WeakMap();

function panelStudioExpectedShutdown(error) { try {
    const message = error instanceof Error ? error.message : String(error || '');
    return /cancel(?:led|ed)?|disposed|disconnect|no longer|cannot send data|circuit|abort/i.test(message);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:panelStudioExpectedShutdown@6651', __javascriptError); throw __javascriptError; }}

function reportPanelStudioError(binding, error, operation = 'unknown') { try {
    if (binding?.disposed) return;
    const message = error instanceof Error ? error.message : String(error || 'Unknown panel interaction error.');
    const detail = `operation=${operation}; binding=${binding?.bindingId || 'unknown'}; reason=${message}`;
    if (panelStudioExpectedShutdown(error)) {
        console.debug('Panel Studio interaction was cancelled or ended.', detail, error);
        if (!binding?.dotNetReference || binding.reportingCancellation) return;
        binding.reportingCancellation = true;
        binding.dotNetReference.invokeMethodAsync('ReportPanelInteractionError', detail)
            .catch(reportError => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@6664', reportError); 
                if (!panelStudioExpectedShutdown(reportError)) console.debug('Panel Studio cancellation reporting ended.', reportError);
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:binding.dotNetReference.invokeMethodAsync(\'ReportPanelInteractionError@6665', __javascriptError); throw __javascriptError; }})
            .finally(() => { try { binding.reportingCancellation = false;  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:binding.dotNetReference.invokeMethodAsync(\'ReportPanelInteractionError@6668', __javascriptError); throw __javascriptError; }});
        return;
    }
    console.warn('Panel Studio interaction failed.', detail, error);
    if (!binding?.dotNetReference || binding.reportingError) return;
    binding.reportingError = true;
    binding.dotNetReference.invokeMethodAsync('ReportPanelInteractionError', detail)
        .catch(reportError => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@6674', reportError); 
            if (!panelStudioExpectedShutdown(reportError)) console.debug('Panel Studio error reporting ended.', reportError);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:binding.dotNetReference.invokeMethodAsync(\'ReportPanelInteractionError@6675', __javascriptError); throw __javascriptError; }})
        .finally(() => { try { binding.reportingError = false;  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:binding.dotNetReference.invokeMethodAsync(\'ReportPanelInteractionError@6678', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:reportPanelStudioError@6656', __javascriptError); throw __javascriptError; }}

function panelStudioQueueInvoke(binding, method, ...args) { try {
    if (!binding || binding.disposed || !binding.element?.isConnected) return Promise.resolve();
    binding.invokeQueue = (binding.invokeQueue || Promise.resolve())
        .catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@6683', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:(binding.invokeQueue || Promise.resolve()) .catch@6684', __javascriptError); throw __javascriptError; }})
        .then(() => { try {
            if (binding.disposed || !binding.element?.isConnected) return;
            if (!binding.dotNetReference) throw new Error('Panel Studio .NET interaction reference is unavailable.');
            return binding.dotNetReference.invokeMethodAsync(method, ...args);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:(binding.invokeQueue || Promise.resolve()) .catch(() => {}) .then@6685', __javascriptError); throw __javascriptError; }})
        .catch(error => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@6683', error);  return (reportPanelStudioError(binding, error, method)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:(binding.invokeQueue || Promise.resolve()) .catch(() => {}) .then(() =@6690', __javascriptError); throw __javascriptError; } });
    return binding.invokeQueue;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:panelStudioQueueInvoke@6681', __javascriptError); throw __javascriptError; }}

function panelStudioEditableTarget(target) { try {
    return Boolean(target?.closest?.('input,textarea,select,[contenteditable="true"],[role="textbox"]'));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:panelStudioEditableTarget@6694', __javascriptError); throw __javascriptError; }}

function panelStudioInvoke(binding, command, amount = 1) { try {
    return panelStudioQueueInvoke(binding, 'PanelStudioCommand', command, amount);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:panelStudioInvoke@6698', __javascriptError); throw __javascriptError; }}

function startPanelStudioGamepad(binding) { try {
    if (typeof navigator.getGamepads !== 'function') return;
    const state = { frame: 0, buttons: [], nextRepeat: 0, axisX: 0, axisY: 0 };
    const pressed = (gamepad, index) => { try { return (Boolean(gamepad?.buttons?.[index]?.pressed || number(gamepad?.buttons?.[index]?.value) > .55)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pressed@6705', __javascriptError); throw __javascriptError; } };
    const edge = (gamepad, index) => { try {
        const value = pressed(gamepad, index);
        const previous = Boolean(state.buttons[index]);
        state.buttons[index] = value;
        return value && !previous;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:edge@6706', __javascriptError); throw __javascriptError; }};
    const tick = time => { try {
        if (binding.disposed || !binding.element?.isConnected) return;
        state.frame = requestAnimationFrame(tick);
        if (document.hidden || binding.element.dataset.panelStudioArrange !== 'true') return;
        const active = document.activeElement === binding.element || binding.element.contains(document.activeElement);
        if (!active) return;
        const gamepad = [...(navigator.getGamepads?.() || [])].find(Boolean);
        if (!gamepad) return;

        const axisX = Math.abs(number(gamepad.axes?.[0])) > .45 ? Math.sign(number(gamepad.axes?.[0])) : 0;
        const axisY = Math.abs(number(gamepad.axes?.[1])) > .45 ? Math.sign(number(gamepad.axes?.[1])) : 0;
        const dpadX = pressed(gamepad, 14) ? -1 : pressed(gamepad, 15) ? 1 : 0;
        const dpadY = pressed(gamepad, 12) ? -1 : pressed(gamepad, 13) ? 1 : 0;
        const x = dpadX || axisX;
        const y = dpadY || axisY;
        const changed = x !== state.axisX || y !== state.axisY;
        if (x || y) {
            if (changed || time >= state.nextRepeat) {
                if (x < 0) panelStudioInvoke(binding, 'left', 1);
                if (x > 0) panelStudioInvoke(binding, 'right', 1);
                if (y < 0) panelStudioInvoke(binding, 'up', 1);
                if (y > 0) panelStudioInvoke(binding, 'down', 1);
                state.nextRepeat = time + (changed ? 260 : 90);
            }
        } else state.nextRepeat = 0;
        state.axisX = x;
        state.axisY = y;

        // Steam Deck / standard gamepad: bumpers move one layer, triggers move to edge,
        // X duplicates. Interaction mode remains an explicit UI choice. Destructive delete remains
        // keyboard/context-menu only to avoid accidental controller data loss.
        if (edge(gamepad, 4)) panelStudioInvoke(binding, 'backward');
        if (edge(gamepad, 5)) panelStudioInvoke(binding, 'forward');
        if (edge(gamepad, 6)) panelStudioInvoke(binding, 'back');
        if (edge(gamepad, 7)) panelStudioInvoke(binding, 'front');
        if (edge(gamepad, 2)) panelStudioInvoke(binding, 'duplicate');
        // Interaction mode is intentionally changed only by explicit UI controls.
        // A connected or noisy gamepad must never switch the editor out of arrange mode.
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:tick@6712', __javascriptError); throw __javascriptError; }};
    state.frame = requestAnimationFrame(tick);
    binding.gamepad = state;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:startPanelStudioGamepad@6702', __javascriptError); throw __javascriptError; }}

export function unbindPanelStudioDropSurface(element) { try {
    if (!(element instanceof HTMLElement)) return;
    const binding = panelStudioDropBindings.get(element);
    if (!binding) return;
    binding.disposed = true;
    element.dataset.panelStudioBindingState = 'disposed';
    binding.cancelPointer?.();
    binding.controller?.abort?.();
    try { binding.layoutObserver?.disconnect?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@6782', __caughtJavaScriptError); }
    if (binding.gamepad?.frame) cancelAnimationFrame(binding.gamepad.frame);
    panelStudioDropBindings.delete(element);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:unbindPanelStudioDropSurface@6755', __javascriptError); throw __javascriptError; }}

export function cancelPanelStudioPointer(element, restore = true) { try {
    if (!(element instanceof HTMLElement)) return;
    const binding = panelStudioDropBindings.get(element);
    binding?.cancelPointer?.(restore !== false);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:cancelPanelStudioPointer@6767', __javascriptError); throw __javascriptError; }}

export async function flushPanelStudioInteractions(element) { try {
    if (!(element instanceof HTMLElement)) return false;
    const binding = panelStudioDropBindings.get(element);
    if (!binding || binding.disposed) return false;
    await (binding.invokeQueue || Promise.resolve());
    return !binding.disposed;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:flushPanelStudioInteractions', __javascriptError); throw __javascriptError; }}

export function bindPanelStudioDropSurface(element, dotNetReference, bindingId = '') { try {
    if (!(element instanceof HTMLElement)) return false;
    const normalizedBindingId = String(bindingId || element.dataset.panelStudioBindingId || '').trim();
    const existing = panelStudioDropBindings.get(element);
    if (existing && !existing.disposed && existing.bindingId === normalizedBindingId) {
        existing.dotNetReference = dotNetReference || existing.dotNetReference;
        element.dataset.panelStudioBindingState = 'active';
        syncPanelStudioDesignSurface(element);
        return true;
    }
    unbindPanelStudioDropSurface(element);
    const controller = new AbortController();
    const binding = { element, dotNetReference, bindingId: normalizedBindingId, controller, disposed: false, pointer: null, gamepad: null, layoutObserver: null, reportingError: false, reportingCancellation: false, invokeQueue: Promise.resolve() };
    const options = { signal: controller.signal };
    const activeOptions = { signal: controller.signal, passive: false };
    panelStudioDropBindings.set(element, binding);
    element.dataset.panelStudioBindingState = 'active';
    syncPanelStudioDesignSurface(element);
    if (typeof ResizeObserver !== 'undefined') {
        binding.layoutObserver = new ResizeObserver(() => { try {
            syncPanelStudioDesignSurface(element);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:ResizeObserver@6811', __javascriptError); throw __javascriptError; }});
        binding.layoutObserver.observe(element);
    }

    const setActive = active => { try { return (element.querySelector('.panel-studio-drop-layer')?.classList.toggle('active', active)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:setActive@6790', __javascriptError); throw __javascriptError; } };
    const updateGhost = event => { try {
        event.preventDefault();
        const ghost = element.querySelector('.panel-studio-drag-ghost');
        if (!(ghost instanceof HTMLElement)) return;
        const point = panelStudioPoint(element, event.clientX, event.clientY);
        ghost.style.left = `${point.x * 100}%`;
        ghost.style.top = `${point.y * 100}%`;
        ghost.style.transform = 'translate(-50%, -50%)';
        setActive(true);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:updateGhost@6791', __javascriptError); throw __javascriptError; }};
    element.addEventListener('dragenter', updateGhost, activeOptions);
    element.addEventListener('dragover', updateGhost, activeOptions);
    element.addEventListener('dragleave', event => { try {
        if (event.relatedTarget instanceof Node && element.contains(event.relatedTarget)) return;
        setActive(false);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:element.addEventListener@6803', __javascriptError); throw __javascriptError; }}, options);
    element.addEventListener('drop', () => { try { return (setActive(false)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:element.addEventListener@6807', __javascriptError); throw __javascriptError; } }, options);

    const applyPanelBounds = (operation, bounds) => { try {
        if (!operation || !bounds) return;
        operation.hitbox.style.left = `${bounds.x * 100}%`;
        operation.hitbox.style.top = `${bounds.y * 100}%`;
        operation.hitbox.style.width = `${bounds.width * 100}%`;
        operation.hitbox.style.height = `${bounds.height * 100}%`;
        if (operation.liveElement instanceof HTMLElement) {
            operation.liveElement.style.left = `${bounds.x * 100}%`;
            operation.liveElement.style.top = `${bounds.y * 100}%`;
            operation.liveElement.style.width = `${bounds.width * 100}%`;
            operation.liveElement.style.height = `${bounds.height * 100}%`;
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:applyPanelBounds@6809', __javascriptError); throw __javascriptError; }};
    const cancelPointer = (restore = true) => { try {
        const operation = binding.pointer;
        if (!operation) return;
        try { operation.hitbox.releasePointerCapture(operation.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@6825', __caughtJavaScriptError);  }
        if (restore && operation.moved) applyPanelBounds(operation, operation.initial);
        binding.pointer = null;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:cancelPointer@6822', __javascriptError); throw __javascriptError; }};
    binding.cancelPointer = cancelPointer;

    const commit = operation => { try {
        const bounds = operation.current || operation.initial;
        operation.hitbox.dataset.panelElementX = String(bounds.x);
        operation.hitbox.dataset.panelElementY = String(bounds.y);
        operation.hitbox.dataset.panelElementWidth = String(bounds.width);
        operation.hitbox.dataset.panelElementHeight = String(bounds.height);
        panelStudioQueueInvoke(binding, 'CommitPanelElementBounds', operation.id, bounds.x, bounds.y, bounds.width, bounds.height);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:commit@6831', __javascriptError); throw __javascriptError; }};

    element.addEventListener('pointerdown', event => { try {
        if (event.button !== 0 || element.dataset.panelStudioArrange !== 'true') return;
        const target = event.target instanceof Element ? event.target : null;
        const hitbox = target?.closest?.('.panel-studio-hitbox[data-panel-element-id]');
        if (!(hitbox instanceof HTMLElement) || !element.contains(hitbox)) return;
        const handle = target.closest('i[data-resize]');
        event.preventDefault();
        event.stopPropagation();
        try { element.focus({ preventScroll: true }); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@6848', __caughtJavaScriptError);  }
        cancelPointer();

        const coordinateSurface = hitbox.closest('.panel-studio-hit-layer') || element;
        const canvasBounds = coordinateSurface.getBoundingClientRect();
        const readNormalized = (name, fallback) => { try {
            const value = Number.parseFloat(hitbox.dataset[name] || '');
            return Number.isFinite(value) ? value : fallback;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:readNormalized@6853', __javascriptError); throw __javascriptError; }};
        const boxBounds = hitbox.getBoundingClientRect();
        const fallback = {
            x: clamp((boxBounds.left - canvasBounds.left) / Math.max(1, canvasBounds.width), 0, 1),
            y: clamp((boxBounds.top - canvasBounds.top) / Math.max(1, canvasBounds.height), 0, 1),
            width: clamp(boxBounds.width / Math.max(1, canvasBounds.width), .005, 1),
            height: clamp(boxBounds.height / Math.max(1, canvasBounds.height), .005, 1)
        };
        const initial = {
            x: clamp(readNormalized('panelElementX', fallback.x), 0, 1),
            y: clamp(readNormalized('panelElementY', fallback.y), 0, 1),
            width: clamp(readNormalized('panelElementWidth', fallback.width), .005, 1),
            height: clamp(readNormalized('panelElementHeight', fallback.height), .005, 1)
        };
        const elementId = hitbox.dataset.panelElementId || '';
        const liveElement = Array.from(element.querySelectorAll('.publication-panel-element[data-element-id]'))
            .find(node => { try { return (node instanceof HTMLElement && node.dataset.elementId === elementId); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:Array.from(element.querySelectorAll(\'.publication-panel-element[data-e@6872', __javascriptError); throw __javascriptError; } }) || null;
        const operation = {
            id: elementId, hitbox, liveElement, handle: handle?.dataset?.resize || '',
            pointerId: event.pointerId, originX: event.clientX, originY: event.clientY,
            canvasBounds, initial, current: initial, moved: false
        };
        binding.pointer = operation;
        try { hitbox.setPointerCapture(event.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@6879', __caughtJavaScriptError);  }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:element.addEventListener@6840', __javascriptError); throw __javascriptError; }}, activeOptions);

    element.addEventListener('pointermove', event => { try {
        const operation = binding.pointer;
        if (!operation || operation.pointerId !== event.pointerId) return;
        event.preventDefault();
        const dx = (event.clientX - operation.originX) / Math.max(1, operation.canvasBounds.width);
        const dy = (event.clientY - operation.originY) / Math.max(1, operation.canvasBounds.height);
        if (!operation.moved && Math.hypot(dx * operation.canvasBounds.width, dy * operation.canvasBounds.height) < 2) return;
        operation.moved = true;
        let { x, y, width, height } = operation.initial;
        if (!operation.handle) {
            x = clamp(x + dx, 0, Math.max(0, 1 - width));
            y = clamp(y + dy, 0, Math.max(0, 1 - height));
        } else {
            const edge = operation.handle;
            if (edge.includes('w')) { x += dx; width -= dx; }
            if (edge.includes('e')) width += dx;
            if (edge.includes('n')) { y += dy; height -= dy; }
            if (edge.includes('s')) height += dy;
            const minimum = .01;
            if (width < minimum) { if (edge.includes('w')) x -= minimum - width; width = minimum; }
            if (height < minimum) { if (edge.includes('n')) y -= minimum - height; height = minimum; }
            x = clamp(x, 0, Math.max(0, 1 - minimum));
            y = clamp(y, 0, Math.max(0, 1 - minimum));
            width = clamp(width, minimum, 1 - x);
            height = clamp(height, minimum, 1 - y);
        }
        operation.current = { x, y, width, height };
        // Keep the preview attached to its selection rectangle while dragging.
        // The final normalized values are committed through the C# layout service on pointer-up.
        applyPanelBounds(operation, operation.current);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:element.addEventListener@6882', __javascriptError); throw __javascriptError; }}, activeOptions);

    const finishPointer = event => { try {
        const operation = binding.pointer;
        if (!operation || operation.pointerId !== event.pointerId) return;
        event.preventDefault();
        cancelPointer(false);
        if (operation.moved) commit(operation);
        else panelStudioQueueInvoke(binding, 'SelectPanelElement', operation.id);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:finishPointer@6914', __javascriptError); throw __javascriptError; }};
    element.addEventListener('pointerup', finishPointer, activeOptions);
    element.addEventListener('pointercancel', cancelPointer, options);

    element.addEventListener('dblclick', event => { try {
        if (element.dataset.panelStudioArrange !== 'true') return;
        const target = event.target instanceof Element ? event.target : null;
        const hitbox = target?.closest?.('.panel-studio-hitbox[data-panel-element-id]');
        if (!(hitbox instanceof HTMLElement) || !element.contains(hitbox)) return;
        event.preventDefault();
        event.stopPropagation();
        cancelPointer(true);
        panelStudioQueueInvoke(binding, 'ActivatePanelElement', hitbox.dataset.panelElementId || '');
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:element.addEventListener@6925', __javascriptError); throw __javascriptError; }}, activeOptions);

    element.addEventListener('contextmenu', event => { try {
        const target = event.target instanceof Element ? event.target : null;
        if (target?.closest?.('.panel-studio-hitbox[data-panel-element-id]')) cancelPointer(true);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:element.addEventListener@6936', __javascriptError); throw __javascriptError; }}, options);

    element.addEventListener('keydown', event => { try {
        if (event.defaultPrevented || panelStudioEditableTarget(event.target) || element.dataset.panelStudioArrange !== 'true') return;
        const key = String(event.key || '').toLowerCase();
        const command = event.ctrlKey || event.metaKey;
        let handled = true;
        const amount = event.altKey ? .25 : event.shiftKey ? 10 : 1;
        if (key === 'arrowleft') panelStudioInvoke(binding, 'left', amount);
        else if (key === 'arrowright') panelStudioInvoke(binding, 'right', amount);
        else if (key === 'arrowup') panelStudioInvoke(binding, 'up', amount);
        else if (key === 'arrowdown') panelStudioInvoke(binding, 'down', amount);
        else if (key === 'delete' || key === 'backspace') panelStudioInvoke(binding, 'delete');
        else if (command && key === 'd') panelStudioInvoke(binding, 'duplicate');
        else if (event.altKey && key === 'pageup') panelStudioInvoke(binding, 'forward');
        else if (event.altKey && key === 'pagedown') panelStudioInvoke(binding, 'backward');
        else if (event.altKey && key === 'home') panelStudioInvoke(binding, 'front');
        else if (event.altKey && key === 'end') panelStudioInvoke(binding, 'back');
        else if (key === 'enter') handled = false;
        else handled = false;
        if (handled) { event.preventDefault(); event.stopPropagation(); }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:element.addEventListener@6941', __javascriptError); throw __javascriptError; }}, options);

    startPanelStudioGamepad(binding);
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:bindPanelStudioDropSurface@6773', __javascriptError); throw __javascriptError; }}

export function clickElementById(id) { try {
    const element = document.getElementById(id);
    if (!element) throw new Error(`Element '${id}' is not available.`);
    if (element instanceof HTMLInputElement && element.type === 'file') {
        element.value = '';
        delete element.dataset.publisherDropX;
        delete element.dataset.publisherDropY;
    }
    element.click();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:clickElementById@6966', __javascriptError); throw __javascriptError; }}

export function consumeCanvasInsertPlacement(id) { try {
    const element = document.getElementById(id);
    if (!(element instanceof HTMLInputElement)) return null;
    const x = Number.parseFloat(element.dataset.publisherDropX || '');
    const y = Number.parseFloat(element.dataset.publisherDropY || '');
    delete element.dataset.publisherDropX;
    delete element.dataset.publisherDropY;
    return Number.isFinite(x) && Number.isFinite(y) ? [x, y] : null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:consumeCanvasInsertPlacement@6977', __javascriptError); throw __javascriptError; }}


function storyBackgroundIsVisible(value) { try {
    const normalized = String(value || '').trim().toLowerCase();
    if (!normalized || normalized === 'transparent' || normalized === 'none') return false;
    if (normalized === 'rgba(0, 0, 0, 0)' || normalized === 'rgba(0,0,0,0)') return false;
    if (/^rgba?\([^)]*,\s*0(?:\.0+)?\s*\)$/.test(normalized)) return false;
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:storyBackgroundIsVisible@6988', __javascriptError); throw __javascriptError; }}

function storyStyleBackgroundColor(style) { try {
    if (!style) return '';
    const color = style.getPropertyValue?.('background-color') || style.backgroundColor || '';
    return storyBackgroundIsVisible(color) ? color.trim() : '';
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:storyStyleBackgroundColor@6996', __javascriptError); throw __javascriptError; }}

function storyPageRuleBackground(doc) { try {
    const visit = rules => { try {
        for (const rule of rules || []) {
            try {
                const text = String(rule.cssText || '').trim().toLowerCase();
                if (text.startsWith('@page') && rule.style) {
                    const color = storyStyleBackgroundColor(rule.style);
                    if (color) return color;
                }
                if (rule.cssRules) {
                    const nested = visit(rule.cssRules);
                    if (nested) return nested;
                }
            } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@7015', __caughtJavaScriptError);  }
        }
        return '';
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:visit@7003', __javascriptError); throw __javascriptError; }};
    for (const sheet of doc.styleSheets || []) {
        try {
            const color = visit(sheet.cssRules);
            if (color) return color;
        } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@7023', __caughtJavaScriptError);  }
    }
    return '';
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:storyPageRuleBackground@7002', __javascriptError); throw __javascriptError; }}

function storyDocumentBackground(doc, view) { try {
    const pageRule = storyPageRuleBackground(doc);
    if (pageRule) return pageRule;

    const roots = [doc.documentElement, doc.body];
    for (const root of roots) {
        if (!root) continue;
        const color = storyStyleBackgroundColor(view.getComputedStyle(root));
        if (color) return color;
    }

    const bodyRect = doc.body.getBoundingClientRect();
    const referenceArea = Math.max(1,
        Math.max(bodyRect.width, doc.documentElement.scrollWidth, doc.body.scrollWidth)
        * Math.max(bodyRect.height, doc.documentElement.scrollHeight, doc.body.scrollHeight));
    let best = null;
    for (const element of doc.body.querySelectorAll('*')) {
        const rect = element.getBoundingClientRect();
        const area = Math.max(0, rect.width) * Math.max(0, rect.height);
        if (area < referenceArea * .45) continue;
        const color = storyStyleBackgroundColor(view.getComputedStyle(element));
        if (!color) continue;
        if (!best || area > best.area) best = { color, area };
    }
    return best?.color || 'transparent';
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:storyDocumentBackground@7028', __javascriptError); throw __javascriptError; }}

async function waitForStoryImages(doc) { try {
    const images = [...(doc?.images || [])];
    if (!images.length) return;
    await Promise.all(images.map(image => { try {
        if (image.complete) return Promise.resolve();
        return new Promise(resolve => { try {
            const finish = () => { try { return (resolve()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:finish@7061', __javascriptError); throw __javascriptError; } };
            image.addEventListener('load', finish, { once: true });
            image.addEventListener('error', finish, { once: true });
            setTimeout(finish, 4000);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@7060', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:images.map@7058', __javascriptError); throw __javascriptError; }}));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:waitForStoryImages@7055', __javascriptError); throw __javascriptError; }}

async function prepareStoryPreviewHtml(html, preferredBackground = '') { try {
    const source = String(html || '');
    if (!source.trim()) return {
        html: '<div class="publisher-story-document"><p></p></div>',
        background: 'transparent'
    };

    const parsed = new DOMParser().parseFromString(source, 'text/html');
    parsed.querySelectorAll('script,iframe,object,embed,form,input,button,meta,link').forEach(node => { try { return (node.remove()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:parsed.querySelectorAll(\'script,iframe,object,embed,form,input,button,@7077', __javascriptError); throw __javascriptError; } });
    parsed.querySelectorAll('*').forEach(node => { try {
        for (const attribute of [...node.attributes]) {
            if (/^on/i.test(attribute.name)) node.removeAttribute(attribute.name);
            else if ((attribute.name === 'href' || attribute.name === 'src') && /^\s*javascript:/i.test(attribute.value))
                node.setAttribute(attribute.name, '#');
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:parsed.querySelectorAll(\'*\').forEach@7078', __javascriptError); throw __javascriptError; }});

    const frame = document.createElement('iframe');
    frame.setAttribute('aria-hidden', 'true');
    frame.tabIndex = -1;
    frame.style.cssText = 'position:fixed;left:-100000px;top:0;width:1200px;height:1600px;visibility:hidden;pointer-events:none;border:0;';
    document.body.appendChild(frame);

    try {
        const loaded = new Promise(resolve => { try {
            const timeout = setTimeout(resolve, 2500);
            frame.addEventListener('load', () => { try {
                clearTimeout(timeout);
                resolve();
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:frame.addEventListener@7095', __javascriptError); throw __javascriptError; }}, { once: true });
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@7093', __javascriptError); throw __javascriptError; }});
        frame.srcdoc = '<!doctype html>' + parsed.documentElement.outerHTML;
        await loaded;
        const doc = frame.contentDocument;
        const view = frame.contentWindow;
        if (!doc?.body || !view) throw new Error('The story HTML preview document could not be created.');
        try { await doc.fonts?.ready; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@7105', __caughtJavaScriptError);  }
        await waitForStoryImages(doc);

        const documentBackground = storyBackgroundIsVisible(preferredBackground)
            ? String(preferredBackground).trim()
            : storyDocumentBackground(doc, view);
        const originalNodes = [doc.body, ...doc.body.querySelectorAll('*')];
        const cloneBody = doc.body.cloneNode(true);
        const cloneNodes = [cloneBody, ...cloneBody.querySelectorAll('*')];
        const properties = [
            'display','position','float','clear','box-sizing',
            'color','background-color','background-image','background-repeat','background-position','background-size',
            'font-family','font-size','font-weight','font-style','font-variant','font-stretch','line-height',
            'letter-spacing','word-spacing','text-align','text-indent','text-transform','text-shadow',
            'text-decoration-line','text-decoration-style','text-decoration-color','text-decoration-thickness',
            'white-space','overflow-wrap','word-break','hyphens','vertical-align','direction','unicode-bidi',
            'list-style-type','list-style-position','list-style-image',
            'margin-top','margin-right','margin-bottom','margin-left',
            'padding-top','padding-right','padding-bottom','padding-left',
            'border-top-width','border-right-width','border-bottom-width','border-left-width',
            'border-top-style','border-right-style','border-bottom-style','border-left-style',
            'border-top-color','border-right-color','border-bottom-color','border-left-color',
            'border-radius','border-collapse','border-spacing','table-layout',
            'break-before','break-after','break-inside','page-break-before','page-break-after','page-break-inside',
            'opacity'
        ];

        const bodyProperties = new Set([
            'color','font-family','font-size','font-weight','font-style','font-variant','font-stretch','line-height',
            'letter-spacing','word-spacing','text-align','text-indent','text-transform','text-shadow',
            'text-decoration-line','text-decoration-style','text-decoration-color','text-decoration-thickness',
            'white-space','overflow-wrap','word-break','hyphens','vertical-align','direction','unicode-bidi'
        ]);

        for (let index = 0; index < Math.min(originalNodes.length, cloneNodes.length); index++) {
            const original = originalNodes[index];
            const clone = cloneNodes[index];
            const computed = view.getComputedStyle(original);
            const inline = [];
            for (const property of properties) {
                // RichEdit's exported BODY can contain browser/page-preview layout such as a
                // fixed/max width and auto margins. Those values are not document formatting:
                // the DOCX section page size and margins are applied by the Story preview shell.
                // Retain only inherited text properties on BODY and keep complete computed
                // formatting on the actual document nodes below it.
                if (index === 0 && !bodyProperties.has(property)) continue;
                const value = computed.getPropertyValue(property);
                if (value) inline.push(`${property}:${value}`);
            }
            const existing = index === 0 ? '' : clone.getAttribute?.('style');
            clone.setAttribute?.('style', `${existing ? existing + ';' : ''}${inline.join(';')}`);
            const printFill = storyStyleBackgroundColor(computed);
            if (printFill && clone?.nodeType === 1) {
                clone.setAttribute('data-publisher-print-fill', 'true');
                clone.style.setProperty('--publisher-print-fill', printFill);
            }
            clone.removeAttribute?.('class');
            clone.removeAttribute?.('id');
        }

        cloneBody.querySelectorAll('script,iframe,object,embed,form,input,button,meta,link').forEach(node => { try { return (node.remove()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:cloneBody.querySelectorAll(\'script,iframe,object,embed,form,input,butt@7165', __javascriptError); throw __javascriptError; } });
        const bodyStyle = cloneBody.getAttribute('style') || '';
        const wrapper = document.createElement('div');
        wrapper.className = 'publisher-story-document';
        wrapper.style.cssText = `${bodyStyle};display:block;position:static;float:none;clear:both;box-sizing:border-box;`+
            'width:100%;max-width:none;min-width:0;min-height:0;height:auto;margin:0;padding:0;overflow:visible';
        if (storyBackgroundIsVisible(documentBackground)) {
            wrapper.style.setProperty('--publisher-story-page-background', documentBackground);
            wrapper.style.setProperty('--publisher-print-fill', documentBackground);
            wrapper.style.backgroundColor = documentBackground;
            wrapper.setAttribute('data-publisher-print-fill', 'true');
        }
        wrapper.innerHTML = cloneBody.innerHTML;
        return { html: wrapper.outerHTML, background: documentBackground };
    } finally {
        frame.remove();
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:prepareStoryPreviewHtml@7069', __javascriptError); throw __javascriptError; }}

const STORY_PREVIEW_TRANSFER_CHUNK_SIZE = 6 * 1024;

async function prepareStoryPreviewHtmlInChunks(htmlStream, preferredBackground, dotNetReference) { try {
    if (!htmlStream?.arrayBuffer || !dotNetReference)
        throw new Error('The story preview stream could not be initialized.');
    const buffer = await htmlStream.arrayBuffer();
    const source = new TextDecoder('utf-8').decode(buffer);
    const prepared = await prepareStoryPreviewHtml(source, preferredBackground);
    const html = String(prepared?.html || '<div class="publisher-story-document"><p></p></div>');
    const background = String(prepared?.background || 'transparent');
    const transferId = globalThis.crypto?.randomUUID?.() || `story-${Date.now()}-${Math.random().toString(16).slice(2)}`;
    const chunkCount = Math.max(1, Math.ceil(html.length / STORY_PREVIEW_TRANSFER_CHUNK_SIZE));
    const accepted = await dotNetReference.invokeMethodAsync(
        'BeginStoryPreviewTransfer', transferId, html.length, chunkCount, background);
    if (!accepted) throw new Error('The application rejected the formatted story preview transfer.');

    for (let index = 0; index < chunkCount; index++) {
        const start = index * STORY_PREVIEW_TRANSFER_CHUNK_SIZE;
        const chunk = html.slice(start, start + STORY_PREVIEW_TRANSFER_CHUNK_SIZE);
        const appended = await dotNetReference.invokeMethodAsync(
            'AppendStoryPreviewChunk', transferId, index, chunk);
        if (!appended) throw new Error(`The application rejected story preview chunk ${index + 1}.`);
    }

    const completed = await dotNetReference.invokeMethodAsync('CompleteStoryPreviewTransfer', transferId);
    if (!completed) throw new Error('The formatted story preview transfer did not complete.');
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:prepareStoryPreviewHtmlInChunks@7186', __javascriptError); throw __javascriptError; }}


const storyPrintPreviews = new Map();
let reservedStoryPrintPreviewId = '';

function storyPrintPreviewLoadingHtml(title) { try {
    const safeTitle = String(title || 'Story print preview')
        .replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;').replaceAll("'", '&#39;');
    return `<!doctype html><html><head><meta charset="utf-8"><title>${safeTitle}</title>
        <style>body{margin:0;display:grid;place-items:center;min-height:100vh;font:14px Segoe UI,Arial,sans-serif;background:#f3f4f6;color:#172033}.card{padding:22px 28px;border:1px solid #d5d9de;border-radius:6px;background:white;box-shadow:0 8px 28px #0002}</style>
        </head><body><div class="card"><strong>Preparing print preview…</strong><div>${safeTitle}</div></div></body></html>`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:storyPrintPreviewLoadingHtml@7216', __javascriptError); throw __javascriptError; }}

function openStoryPrintPreview(title) { try {
    const id = globalThis.crypto?.randomUUID?.() || `story-print-${Date.now()}-${Math.random().toString(16).slice(2)}`;
    const previewWindow = window.open('', `_publisher_story_print_${id}`);
    if (!previewWindow) return '';
    try {
        previewWindow.document.open();
        previewWindow.document.write(storyPrintPreviewLoadingHtml(title));
        previewWindow.document.close();
        previewWindow.focus();
    } catch {
        try { previewWindow.close(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@7235', __caughtJavaScriptError);  }
        return '';
    }
    storyPrintPreviews.set(id, { previewWindow, objectUrl: '' });
    return id;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:openStoryPrintPreview@7225', __javascriptError); throw __javascriptError; }}


function storyPrintCommandFromEvent(event, richEditHost = null) { try {
    const target = event?.target;
    const command = target?.closest?.('button,[role="button"],[role="menuitem"],.dxbl-btn,.dxbl-ribbon-item,.dxbl-ribbon-item-content');
    if (!command) return '';
    const pathHasPrintIcon = [...(event?.composedPath?.() || [])]
        .some(node => { try { return (node?.classList?.contains?.('pub-icon-print')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...(event?.composedPath?.() || [])] .some@7248', __javascriptError); throw __javascriptError; } });
    const commandLabel = [command.textContent, command.getAttribute?.('aria-label'), command.getAttribute?.('title')]
        .filter(Boolean).join(' ').trim().toLowerCase();
    const publisherPrint = pathHasPrintIcon
        || Boolean(target?.closest?.('.pub-icon-print'))
        || Boolean(command?.querySelector?.('.pub-icon-print'))
        || (command?.closest?.('.story-editor-ribbon') && commandLabel === 'print');
    if (publisherPrint) return 'publisher';
    if (richEditHost?.contains?.(command) && /(^|\s)print(\s|$)/.test(commandLabel)) return 'rich-edit';
    return '';
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:storyPrintCommandFromEvent@7243', __javascriptError); throw __javascriptError; }}

function reserveStoryPrintPreviewFromEvent(event, richEditHost = null) { try {
    const commandKind = storyPrintCommandFromEvent(event, richEditHost);
    if (!commandKind) return '';
    const current = storyPrintPreviews.get(reservedStoryPrintPreviewId);
    if (!current?.previewWindow || current.previewWindow.closed)
        reservedStoryPrintPreviewId = openStoryPrintPreview('Story print preview');
    return commandKind;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:reserveStoryPrintPreviewFromEvent@7260', __javascriptError); throw __javascriptError; }}

function claimStoryPrintPreview(title) { try {
    const reservedId = reservedStoryPrintPreviewId;
    reservedStoryPrintPreviewId = '';
    const entry = storyPrintPreviews.get(reservedId);
    if (entry?.previewWindow && !entry.previewWindow.closed) {
        try {
            entry.previewWindow.document.title = String(title || 'Story print preview');
            entry.previewWindow.focus();
        } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@7277', __caughtJavaScriptError);  }
        return reservedId;
    }
    return openStoryPrintPreview(title);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:claimStoryPrintPreview@7269', __javascriptError); throw __javascriptError; }}

function completeStoryPrintPreview(id, html) { try {
    const entry = storyPrintPreviews.get(String(id || ''));
    if (!entry?.previewWindow || entry.previewWindow.closed)
        throw new Error('The story print-preview window is no longer available.');

    if (entry.objectUrl) {
        try { URL.revokeObjectURL(entry.objectUrl); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@7289', __caughtJavaScriptError);  }
    }
    const rendererUrl = new URL('js/vendor/html2canvas.min.js', document.baseURI).href
        .replaceAll('&', '&amp;').replaceAll('"', '&quot;').replaceAll("'", '&#39;');
    const hydratedHtml = String(html || '').replaceAll('__PUBLISHER_HTML2CANVAS_URL__', rendererUrl);
    const objectUrl = URL.createObjectURL(new Blob([hydratedHtml], { type: 'text/html;charset=utf-8' }));
    entry.objectUrl = objectUrl;
    entry.previewWindow.location.replace(objectUrl);
    try { entry.previewWindow.focus(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@7297', __caughtJavaScriptError);  }

    // The loaded blob is independent from the Blazor circuit. Revoking the URL later
    // does not close the already-loaded preview, but avoids keeping the export forever.
    setTimeout(() => { try {
        const current = storyPrintPreviews.get(String(id || ''));
        if (current?.objectUrl !== objectUrl) return;
        try { URL.revokeObjectURL(objectUrl); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@7304', __caughtJavaScriptError);  }
        current.objectUrl = '';
        if (current.previewWindow?.closed) storyPrintPreviews.delete(String(id || ''));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:setTimeout@7301', __javascriptError); throw __javascriptError; }}, 10 * 60 * 1000);
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:completeStoryPrintPreview@7283', __javascriptError); throw __javascriptError; }}

function failStoryPrintPreview(id, message) { try {
    const key = String(id || '');
    const entry = storyPrintPreviews.get(key);
    if (!entry?.previewWindow || entry.previewWindow.closed) {
        storyPrintPreviews.delete(key);
        return;
    }
    const safeMessage = String(message || 'The story print preview could not be prepared.')
        .replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');
    try {
        entry.previewWindow.document.open();
        entry.previewWindow.document.write(`<!doctype html><html><head><meta charset="utf-8"><title>Print preview error</title></head><body style="font:14px Segoe UI,Arial,sans-serif;padding:32px"><h1>Print preview could not be prepared</h1><p>${safeMessage}</p><button onclick="window.close()">Close</button></body></html>`);
        entry.previewWindow.document.close();
        entry.previewWindow.focus();
    } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@7325', __caughtJavaScriptError);  }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:failStoryPrintPreview@7311', __javascriptError); throw __javascriptError; }}

async function buildPublisherSingleHtml(mode, title, exportOptions = {}) { try {
    const source = document.querySelector('.print-publication');
    if (!source) throw new Error('The publication export surface is not available.');
    if (window.PublisherStudioLiveDataRuntime) {
        await window.PublisherStudioLiveDataRuntime.refreshAll(source, { polling: false });
    }
    if (window.PublisherStudioComponentRuntime) {
        await window.PublisherStudioComponentRuntime.refreshAll(source, { polling: false, fetchNow: true });
    }
    const fetchExportAsset = async (url, description = 'offline export asset') => { try {
        const response = await fetch(url, { cache: 'force-cache' });
        if (!response.ok) {
            throw new Error(`The ${description} ${url} is missing (${response.status}). Run Prepare-DevExpressAssets.cmd on the licensed build machine before building or publishing PublisherStudio.`);
        }
        return await response.text();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:fetchExportAsset@7337', __javascriptError); throw __javascriptError; }};
    const [devExtremeCss, jquerySource, devExtremeSource, worldMapSource, europeMapSource, eurasiaMapSource, africaMapSource, usaMapSource, canadaMapSource, devExtremeLicenseSource, devExtremeLicenseVersion, liveDataSource, componentRuntimeSource, tooltipRuntimeSource] = await Promise.all([
        fetchExportAsset('vendor/devextreme-dist/css/dx.light.css'),
        fetchExportAsset('vendor/jquery/jquery.min.js'),
        fetchExportAsset('vendor/devextreme-dist/js/dx.all.js'),
        fetchExportAsset('vendor/devextreme-dist/js/vectormap-data/world.js'),
        fetchExportAsset('vendor/devextreme-dist/js/vectormap-data/europe.js'),
        fetchExportAsset('vendor/devextreme-dist/js/vectormap-data/eurasia.js'),
        fetchExportAsset('vendor/devextreme-dist/js/vectormap-data/africa.js'),
        fetchExportAsset('vendor/devextreme-dist/js/vectormap-data/usa.js'),
        fetchExportAsset('vendor/devextreme-dist/js/vectormap-data/canada.js'),
        fetchExportAsset('vendor/devextreme-license.js', 'generated DevExtreme runtime license'),
        fetchExportAsset('vendor/devextreme-license.version', 'DevExtreme runtime-license version marker'),
        fetchExportAsset('js/liveDataInterop.js'),
        fetchExportAsset('js/componentRuntime.js'),
        fetchExportAsset('js/tooltipRuntime.js')
    ]);
    if (!/DevExpress\s*\.\s*config\s*\(/.test(devExtremeLicenseSource) || !/licenseKey\s*:/.test(devExtremeLicenseSource)) {
        throw new Error('The generated DevExtreme runtime license file is invalid. Run Prepare-DevExpressAssets.cmd again on the licensed build machine.');
    }
    const bundledDevExtremeVersion = /Version:\s*([0-9]+(?:\.[0-9]+){2})/.exec(devExtremeSource)?.[1] || '';
    const licensedDevExtremeVersion = String(devExtremeLicenseVersion || '').trim();
    if (!bundledDevExtremeVersion || licensedDevExtremeVersion !== bundledDevExtremeVersion) {
        throw new Error(`The DevExtreme runtime license targets ${licensedDevExtremeVersion || 'an unknown version'}, but the bundled browser runtime is ${bundledDevExtremeVersion || 'unknown'}. Run Prepare-DevExpressAssets.cmd again.`);
    }
    const safeScript = value => { try { return (String(value).replace(/<\/script/gi, '<\\/script')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:safeScript@7368', __javascriptError); throw __javascriptError; } };
    await document.fonts?.ready;
    await waitForImages(source);
    refreshContentFit(source);
    await new Promise(resolve => { try { return (requestAnimationFrame(resolve)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@7372', __javascriptError); throw __javascriptError; } });
    const publication = source.cloneNode(true);
    copyComputedStyles(source, publication);
    publication.removeAttribute('aria-hidden');
    publication.removeAttribute('style');
    publication.className = 'website-publication';
    publication.dataset.publicationTitle = String(title || 'Publication');
    normalizePublicationPageSizes(publication);
    const websitePages = [...publication.querySelectorAll(':scope > .print-page')];
    const websiteFrame = publicationFrameDefinition(websitePages, false);
    publication.dataset.frameWidthPx = String(websiteFrame.width);
    publication.dataset.frameHeightPx = String(websiteFrame.height);
    await inlineLocalMediaSources(publication);
    const singleMediaStats = await optimizeSingleFileMedia(publication, exportOptions);
    window.__publisherSingleExportStats = singleMediaStats;
    publication.querySelectorAll('img').forEach(image => { try {
        image.draggable = true;
        image.removeAttribute('aria-hidden');
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:publication.querySelectorAll(\'img\').forEach@7385', __javascriptError); throw __javascriptError; }});
    const css = collectExportCss();
    const defaultPublisherApi = /^https?:$/.test(location.protocol) ? location.origin : '';
    const isSite = mode === 'site';
    const runtimeFunction = isSite ? websiteSiteRuntime : websitePresentationRuntime;
    const outputContextRuntime = `(()=>{const p=new URLSearchParams(location.search);const platform=p.get('publisherChatPlatform')||p.get('publisherOutputPlatform')||'Preview';const channel=p.get('publisherChatChannel')||'';const mode=p.get('publisherOutputMode')||(platform==='Preview'?'operator':'broadcast');window.PublisherStudioOutputContext={mode,platform,channel,outputId:p.get('publisherOutputId')||''};window.PublisherStudioChatPlatform=platform;window.PublisherStudioChatChannel=channel;})();`;
    const runtime = `${outputContextRuntime}${safeMediaDownloadName.toString()}${mediaDownloadDescriptor.toString()}${namedMediaDragStart.toString()}${namedMediaDragRuntime.toString()}window.PublisherStudioDataBaseUrl=${JSON.stringify(defaultPublisherApi)};window.__publisherSignalRuntime=(${signalConnectorRuntime.toString()})(document,{autoStart:false,expose:true});(()=>{let booted=false;const boot=()=>{try{if(!booted){booted=true;(${runtimeFunction.toString()})();}window.PublisherStudioLiveDataRuntime?.start(document,{polling:true,fetchNow:true});window.PublisherStudioComponentRuntime?.start(document,{polling:true,fetchNow:true});(${namedMediaDragRuntime.toString()})(document);window.__publisherSignalRuntime?.startPage?.(document.querySelector('.ps-slide:not([hidden]) .print-page,.ps-site-page:not([hidden]),.print-page')||document);window.PublisherStudioTooltips?.refresh(document);}catch(error){console.error('PublisherStudio standalone runtime boot failed.',error);}};const refresh=()=>{window.PublisherStudioTooltips?.refresh(document);window.__publisherSignalRuntime?.refresh?.();};if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',boot,{once:true});else boot();requestAnimationFrame(()=>{boot();refresh();requestAnimationFrame(refresh);});setTimeout(refresh,250);setTimeout(refresh,1000);new MutationObserver(()=>requestAnimationFrame(refresh)).observe(document.body,{subtree:true,childList:true,attributes:true});})();`;
    const modeCss = isSite ? `
:root{color-scheme:light dark}
html,body{width:100%;height:100%;overflow:hidden!important;background:#111827!important}
body{margin:0;font-family:Segoe UI,system-ui,sans-serif;user-select:text}
.website-publication.ps-site{position:fixed;inset:0;display:block!important;overflow:hidden;visibility:visible!important;pointer-events:auto!important;background:#111827}
.website-publication.ps-site .ps-site-page{position:absolute!important;left:50%!important;top:50%!important;margin:0!important;box-shadow:0 12px 56px #0009;transform:translate(-50%,-50%) scale(var(--ps-site-scale,1))!important;transform-origin:center center!important;will-change:transform,opacity,clip-path}
.website-publication.ps-site .ps-site-page[hidden]{display:none!important}
.website-publication .print-element{position:absolute;transform-origin:center}
.website-publication .print-connector{position:absolute;inset:0;width:100%;height:100%;overflow:visible;transform-box:fill-box;transform-origin:center}
.website-publication .text-frame-content,.website-publication .image-frame,.website-publication .shape,.website-publication .wordart-svg{width:100%;height:100%;overflow:hidden}
.website-publication img{display:block;width:100%;height:100%;max-width:none;transform-origin:center;pointer-events:auto;-webkit-user-drag:auto;user-select:auto}
.website-publication video,.website-publication audio{pointer-events:auto;user-select:auto}
.website-publication .text-frame-content{user-select:text}
.ps-pointer-passive{pointer-events:none!important}.ps-interactive{cursor:pointer}.ps-interactive:hover{outline:2px solid #48a7e8aa;outline-offset:2px}.ps-action-hidden{visibility:hidden!important;pointer-events:none!important}
@media (prefers-reduced-motion:reduce){.ps-site-page,.website-publication [data-publication-element]{animation-duration:.001ms!important;animation-delay:0ms!important}}
@media print{html,body{width:auto;height:auto;overflow:visible!important;background:#fff!important}.website-publication.ps-site{position:static;display:block!important;overflow:visible;background:#fff}.website-publication.ps-site .ps-site-page,.website-publication.ps-site .ps-site-page[hidden]{position:relative!important;display:block!important;left:auto!important;top:auto!important;margin:0 auto!important;transform:none!important;box-shadow:none;break-after:page}}
` : `
:root{color-scheme:dark}
html,body{width:100%;height:100%;overflow:hidden!important;background:#20242b!important}
body{margin:0;font-family:Segoe UI,system-ui,sans-serif;user-select:text}
.website-publication{position:fixed;inset:0;display:grid!important;place-items:center;overflow:hidden;visibility:visible!important;pointer-events:auto!important}
.ps-stage{position:relative;overflow:hidden;background:#090d14;transform-origin:center center;box-shadow:0 10px 48px #000a}
.ps-slide{position:absolute;inset:0;display:block;overflow:hidden;transform-origin:center;will-change:transform,opacity,clip-path}
.ps-slide[hidden]{display:none!important}
.website-publication .print-page{position:absolute;left:50%;top:50%;overflow:hidden;margin:0;box-shadow:none;background-color:#fff;transform-origin:center;will-change:transform}
.website-publication .print-element{position:absolute;transform-origin:center}
.website-publication .print-connector{position:absolute;inset:0;width:100%;height:100%;overflow:visible;transform-box:fill-box;transform-origin:center}
.website-publication .print-connector.ps-interactive{pointer-events:none}
.website-publication .print-connector.ps-interactive .connector-hit{pointer-events:stroke;cursor:pointer}
.website-publication .print-connector.ps-interactive:hover{outline:none}
.website-publication .print-connector.ps-interactive:hover .connector-line{filter:drop-shadow(0 0 2px #48a7e8)}
.website-publication .text-frame-content,.website-publication .image-frame,.website-publication .shape,.website-publication .wordart-svg{width:100%;height:100%;overflow:hidden}
.website-publication img{display:block;width:100%;height:100%;max-width:none;transform-origin:center;pointer-events:auto;-webkit-user-drag:auto;user-select:auto}
.website-publication video,.website-publication audio{pointer-events:auto;user-select:auto}
.website-publication .text-frame-content{user-select:text}
.ps-pointer-passive{pointer-events:none!important}.ps-interactive{cursor:pointer}.ps-interactive:hover{outline:2px solid #48a7e8aa;outline-offset:2px}.ps-action-hidden{visibility:hidden!important;pointer-events:none!important}
.ps-controls{position:fixed;z-index:20;left:50%;bottom:14px;display:flex;align-items:center;gap:7px;min-height:38px;padding:6px 9px;border:1px solid #ffffff38;border-radius:999px;background:#111827dd;box-shadow:0 6px 24px #0009;transform:translateX(-50%);backdrop-filter:blur(10px)}
.ps-controls[hidden]{display:none!important}.ps-controls button{display:grid;place-items:center;width:31px;height:31px;padding:0;border:1px solid #ffffff38;border-radius:50%;color:#fff;background:#ffffff12;font:600 18px/1 Segoe UI,system-ui,sans-serif;cursor:pointer}.ps-controls button:hover{background:#ffffff2c}.ps-controls button:disabled{opacity:.35;cursor:default}.ps-controls span{min-width:58px;color:#e5e7eb;text-align:center;font-size:12px}
@media (prefers-reduced-motion:reduce){.ps-slide,.website-publication [data-publication-element]{animation-duration:.001ms!important;animation-delay:0ms!important}}
@media print{html,body{width:auto;height:auto;overflow:visible!important;background:#fff!important}.website-publication{position:static;display:block!important;overflow:visible}.ps-stage{position:static;width:auto!important;height:auto!important;overflow:visible;transform:none!important;box-shadow:none}.ps-slide,.ps-slide[hidden]{position:relative;display:block!important;inset:auto;overflow:hidden;break-after:page}.website-publication .print-page{position:relative;left:auto;top:auto;margin:0 auto;box-shadow:none;transform:none!important}.ps-controls{display:none!important}}
`;
    const exportCulture = String(source.dataset.publicationCulture || document.documentElement.lang || 'en-US');
    return `<!doctype html>
<html lang="${escapeHtml(exportCulture)}">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>${escapeHtml(title)}</title>
<style>${devExtremeCss}
${css}
${modeCss}
</style>
</head>
<body>${publication.outerHTML}<script>${safeScript(jquerySource)}</script><script>${safeScript(devExtremeSource)}</script><script>${safeScript(worldMapSource)}</script><script>${safeScript(europeMapSource)}</script><script>${safeScript(eurasiaMapSource)}</script><script>${safeScript(africaMapSource)}</script><script>${safeScript(usaMapSource)}</script><script>${safeScript(canadaMapSource)}</script><script>${safeScript(devExtremeLicenseSource)}</script><script>${safeScript(liveDataSource)}</script><script>${safeScript(componentRuntimeSource)}</script><script>${safeScript(tooltipRuntimeSource)}</script><script>${safeScript(runtime)}</script></body>
</html>`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:buildPublisherSingleHtml@7328', __javascriptError); throw __javascriptError; }}

const structuredExportScriptPaths = [
    'js/vendor/jquery.min.js',
    'js/vendor/devextreme.js',
    'js/vendor/vectormap-world.js',
    'js/vendor/vectormap-europe.js',
    'js/vendor/vectormap-eurasia.js',
    'js/vendor/vectormap-africa.js',
    'js/vendor/vectormap-usa.js',
    'js/vendor/vectormap-canada.js',
    'js/vendor/devextreme-license.js',
    'js/live-data-runtime.js',
    'js/component-runtime.js',
    'js/publisher-runtime.js'
];

function structuredWebsiteOptions(options = {}) { try {
    const imageMode = ['preserve', 'png', 'webp', 'avif'].includes(String(options.imageMode || '').toLowerCase())
        ? String(options.imageMode).toLowerCase()
        : 'preserve';
    const videoMode = String(options.videoMode || '').toLowerCase() === 'webm' ? 'webm' : 'preserve';
    return {
        mode: String(options.mode || '').toLowerCase() === 'presentation' ? 'presentation' : 'site',
        imageMode,
        imageQuality: clamp(number(options.imageQuality, .82), .35, 1),
        videoMode,
        videoQuality: clamp(number(options.videoQuality, .78), .35, 1),
        keepVideoFallback: options.keepVideoFallback !== false,
        compressArchive: options.compressArchive !== false
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:structuredWebsiteOptions@7467', __javascriptError); throw __javascriptError; }}

function structuredMimeExtension(mimeType) { try {
    const mime = String(mimeType || '').split(';', 1)[0].trim().toLowerCase();
    return ({
        'image/png': 'png', 'image/jpeg': 'jpg', 'image/jpg': 'jpg', 'image/webp': 'webp',
        'image/avif': 'avif', 'image/gif': 'gif', 'image/svg+xml': 'svg', 'image/bmp': 'bmp',
        'video/webm': 'webm', 'video/mp4': 'mp4', 'video/ogg': 'ogv', 'video/quicktime': 'mov',
        'audio/webm': 'webm', 'audio/ogg': 'ogg', 'audio/mpeg': 'mp3', 'audio/mp4': 'm4a',
        'audio/wav': 'wav', 'audio/x-wav': 'wav', 'audio/flac': 'flac',
        'font/woff2': 'woff2', 'font/woff': 'woff', 'application/json': 'json'
    })[mime] || 'bin';
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:structuredMimeExtension@7483', __javascriptError); throw __javascriptError; }}

async function optimizeSingleFileMedia(root, rawOptions = {}) { try {
    const options = structuredWebsiteOptions({ ...rawOptions, compressArchive: false });
    const warnings = [];
    let sourceBytes = 0;
    let outputBytes = 0;
    const cache = new Map();

    const convertDataUrl = async dataUrl => { try {
        if (cache.has(dataUrl)) return cache.get(dataUrl);
        const task = (async () => { try {
            let original;
            try { original = await (await fetch(dataUrl)).blob(); }
            catch { return { dataUrl, originalSize: 0, outputSize: 0, sourceMime: '', outputMime: '' }; }
            const mime = String(original.type || '').toLowerCase();
            let selected = original;
            if (mime.startsWith('image/') && !['image/svg+xml', 'image/gif'].includes(mime) && options.imageMode !== 'preserve') {
                try {
                    let requestedMime = options.imageMode === 'png' ? 'image/png' : options.imageMode === 'avif' ? 'image/avif' : 'image/webp';
                    let converted = await structuredEncodeImage(original, requestedMime, options.imageQuality);
                    if (!converted && requestedMime === 'image/avif') {
                        warnings.push('This browser cannot encode AVIF; WebP was attempted for affected pictures.');
                        requestedMime = 'image/webp';
                        converted = await structuredEncodeImage(original, requestedMime, options.imageQuality);
                    }
                    if (converted) {
                        if (options.imageMode === 'png' || converted.size < original.size) selected = converted;
                        else warnings.push('A picture conversion was skipped because it would have increased the single-file website.');
                    } else warnings.push('A picture was preserved because this browser could not encode the selected format.');
                } catch { warnings.push('A picture was preserved because browser-side conversion failed.'); }
            }
            if (mime.startsWith('video/') && options.videoMode === 'webm') {
                try {
                    const converted = await structuredTranscodeVideo(original, options.videoQuality);
                    if (converted && (converted.size < original.size || !options.keepVideoFallback)) {
                        selected = converted;
                        if (converted.size >= original.size) warnings.push('The requested WebM conversion was used even though it was not smaller than its source.');
                    } else if (converted) warnings.push('A video was preserved because the WebM result was not smaller than its source.');
                    else warnings.push('A video was preserved because this browser cannot perform the requested WebM conversion.');
                } catch { warnings.push('A video was preserved because browser-side WebM conversion failed.'); }
            }
            const selectedUrl = selected === original ? dataUrl : await blobAsDataUrl(selected);
            return { dataUrl: selectedUrl, originalSize: original.size || 0, outputSize: selected.size || 0, sourceMime: original.type || '', outputMime: selected.type || original.type || '' };
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:optimizeSingleFileMedia:convert', __javascriptError); throw __javascriptError; }})();
        cache.set(dataUrl, task);
        return task;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:optimizeSingleFileMedia:cache', __javascriptError); throw __javascriptError; }};

    const nodes = [...root.querySelectorAll('img[src],video[src],source[src]')];
    for (const node of nodes) {
        const value = String(node.getAttribute('src') || '');
        if (!value.startsWith('data:')) continue;
        const result = await convertDataUrl(value);
        if (result.originalSize > 0) sourceBytes += result.originalSize;
        if (result.outputSize > 0) outputBytes += result.outputSize;
        if (result.dataUrl !== value) {
            node.setAttribute('src', result.dataUrl);
            if (node.tagName === 'SOURCE' && result.outputMime) node.setAttribute('type', result.outputMime);
        }
    }
    return { sourceBytes, outputBytes, warnings };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:optimizeSingleFileMedia', __javascriptError); throw __javascriptError; }}

function structuredAssetFolder(mimeType) { try {
    const mime = String(mimeType || '').toLowerCase();
    if (mime.startsWith('image/')) return 'assets/images';
    if (mime.startsWith('video/')) return 'assets/video';
    if (mime.startsWith('audio/')) return 'assets/audio';
    if (mime.startsWith('font/')) return 'assets/fonts';
    return 'assets/files';
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:structuredAssetFolder@7495', __javascriptError); throw __javascriptError; }}

async function structuredBlobHash(blob) { try {
    const bytes = await blob.arrayBuffer();
    if (globalThis.crypto?.subtle) {
        const digest = new Uint8Array(await globalThis.crypto.subtle.digest('SHA-256', bytes));
        return [...digest.slice(0, 10)].map(value => { try { return (value.toString(16).padStart(2, '0')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[...digest.slice(0, 10)].map@7508', __javascriptError); throw __javascriptError; } }).join('');
    }
    return crc32(new Uint8Array(bytes)).toString(16).padStart(8, '0');
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:structuredBlobHash@7504', __javascriptError); throw __javascriptError; }}

async function structuredImageCanvas(blob) { try {
    const url = URL.createObjectURL(blob);
    try {
        const image = new Image();
        image.decoding = 'async';
        await new Promise((resolve, reject) => { try {
            const timer = setTimeout(() => { try { return (reject(new Error('Image decoding timed out.'))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:setTimeout@7519', __javascriptError); throw __javascriptError; } }, 20000);
            image.onload = () => { try { clearTimeout(timer); resolve();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:image.onload@7520', __javascriptError); throw __javascriptError; }};
            image.onerror = () => { try { clearTimeout(timer); reject(new Error('The browser could not decode the image.'));  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:image.onerror@7521', __javascriptError); throw __javascriptError; }};
            image.src = url;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@7518', __javascriptError); throw __javascriptError; }});
        const canvas = document.createElement('canvas');
        canvas.width = Math.max(1, image.naturalWidth);
        canvas.height = Math.max(1, image.naturalHeight);
        const context = canvas.getContext('2d');
        if (!context) throw new Error('The browser did not provide an image conversion canvas.');
        context.drawImage(image, 0, 0);
        return canvas;
    } finally {
        URL.revokeObjectURL(url);
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:structuredImageCanvas@7513', __javascriptError); throw __javascriptError; }}

async function structuredEncodeImage(blob, mimeType, quality) { try {
    const canvas = await structuredImageCanvas(blob);
    const encoded = await new Promise(resolve => { try { return (canvas.toBlob(resolve, mimeType, quality)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@7538', __javascriptError); throw __javascriptError; } });
    if (!encoded || encoded.type.toLowerCase() !== mimeType.toLowerCase()) return null;
    return encoded;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:structuredEncodeImage@7536', __javascriptError); throw __javascriptError; }}

function structuredRecorderMimeType() { try {
    if (typeof MediaRecorder !== 'function' || typeof MediaRecorder.isTypeSupported !== 'function') return '';
    return [
        'video/webm;codecs=vp9,opus',
        'video/webm;codecs=vp8,opus',
        'video/webm'
    ].find(type => { try { return (MediaRecorder.isTypeSupported(type)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:[ \'video/webm;codecs=vp9,opus\', \'video/webm;codecs=vp8,opus\', \'video/w@7549', __javascriptError); throw __javascriptError; } }) || '';
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:structuredRecorderMimeType@7543', __javascriptError); throw __javascriptError; }}

function waitForStructuredMedia(media, eventName, timeoutMs) { try {
    return new Promise((resolve, reject) => { try {
        const timer = setTimeout(() => { try { return (done(new Error(`Media ${eventName} timed out.`))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:setTimeout@7554', __javascriptError); throw __javascriptError; } }, timeoutMs);
        const onSuccess = () => { try { return (done()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:onSuccess@7555', __javascriptError); throw __javascriptError; } };
        const onError = () => { try { return (done(new Error('The browser could not decode this media source.'))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:onError@7556', __javascriptError); throw __javascriptError; } };
        function done(error) { try {
            clearTimeout(timer);
            media.removeEventListener(eventName, onSuccess);
            media.removeEventListener('error', onError);
            error ? reject(error) : resolve();
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:done@7557', __javascriptError); throw __javascriptError; }}
        media.addEventListener(eventName, onSuccess, { once: true });
        media.addEventListener('error', onError, { once: true });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@7553', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:waitForStructuredMedia@7552', __javascriptError); throw __javascriptError; }}

async function structuredTranscodeVideo(blob, quality) { try {
    const mimeType = structuredRecorderMimeType();
    if (!mimeType) return null;
    const video = document.createElement('video');
    const url = URL.createObjectURL(blob);
    let stream = null;
    try {
        video.preload = 'auto';
        video.playsInline = true;
        video.muted = true;
        video.src = url;
        await waitForStructuredMedia(video, 'loadedmetadata', 20000);
        if (!Number.isFinite(video.duration) || video.duration <= 0) return null;
        if (video.readyState < 3) await waitForStructuredMedia(video, 'canplay', 20000);
        const capture = video.captureStream || video.mozCaptureStream;
        if (typeof capture !== 'function') return null;
        stream = capture.call(video);
        if (!stream?.getVideoTracks?.().length) return null;
        const pixels = Math.max(1, video.videoWidth * video.videoHeight);
        const resolutionScale = Math.max(.55, Math.min(2, Math.sqrt(pixels / (1280 * 720))));
        const qualityCurve = Math.max(.01, Math.min(1, quality)) ** 2;
        const videoBitsPerSecond = Math.round((350_000 + qualityCurve * 2_850_000) * resolutionScale);
        const audioBitsPerSecond = Math.round(80_000 + Math.max(0, Math.min(1, quality)) * 96_000);
        const chunks = [];
        const recorder = new MediaRecorder(stream, { mimeType, videoBitsPerSecond, audioBitsPerSecond });
        recorder.addEventListener('dataavailable', event => { try { if (event.data?.size) chunks.push(event.data);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:recorder.addEventListener@7591', __javascriptError); throw __javascriptError; }});
        const stopped = new Promise((resolve, reject) => { try {
            recorder.addEventListener('stop', resolve, { once: true });
            recorder.addEventListener('error', event => { try { return (reject(event.error || new Error('WebM recording failed.'))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:recorder.addEventListener@7594', __javascriptError); throw __javascriptError; } }, { once: true });
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@7592', __javascriptError); throw __javascriptError; }});
        recorder.start(1000);
        await video.play();
        await waitForStructuredMedia(video, 'ended', Math.max(30000, Math.ceil(video.duration * 2000 + 30000)));
        if (recorder.state !== 'inactive') recorder.stop();
        await stopped;
        const output = new Blob(chunks, { type: mimeType.split(';', 1)[0] });
        return output.size > 0 ? output : null;
    } finally {
        try { video.pause(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@7604', __caughtJavaScriptError);  }
        try { stream?.getTracks?.().forEach(track => { try { return (track.stop()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:stream?.getTracks?.().forEach@7605', __javascriptError); throw __javascriptError; } }); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@7605', __caughtJavaScriptError);  }
        video.removeAttribute('src');
        try { video.load(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@7607', __caughtJavaScriptError);  }
        URL.revokeObjectURL(url);
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:structuredTranscodeVideo@7568', __javascriptError); throw __javascriptError; }}

function structuredDataUrlMatches(value) { try {
    return [...String(value || '').matchAll(/data:[a-zA-Z0-9.+-]+\/[a-zA-Z0-9.+-]+(?:;[^,"'\s<>)]*)?,[^"'\s<>)]*/g)];
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:structuredDataUrlMatches@7612', __javascriptError); throw __javascriptError; }}

async function replaceStructuredDataUrls(value, prefix, resolveAsset) { try {
    const text = String(value || '');
    const matches = structuredDataUrlMatches(text);
    if (!matches.length) return text;
    let output = '';
    let cursor = 0;
    for (const match of matches) {
        output += text.slice(cursor, match.index);
        const asset = await resolveAsset(match[0]);
        output += `${prefix}${asset.path}`;
        cursor = match.index + match[0].length;
    }
    return output + text.slice(cursor);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:replaceStructuredDataUrls@7616', __javascriptError); throw __javascriptError; }}

async function buildPublisherStructuredSite(title, rawOptions = {}) { try {
    const options = structuredWebsiteOptions(rawOptions);
    const standaloneHtml = await buildPublisherSingleHtml(options.mode, title);
    const parser = new DOMParser();
    const exportedDocument = parser.parseFromString(standaloneHtml, 'text/html');
    if (!exportedDocument.documentElement || exportedDocument.querySelector('parsererror'))
        throw new Error('The browser could not parse the generated website document.');

    const files = [];
    const warnings = [];
    const assetCache = new Map();
    const pathCache = new Map();
    let assetCount = 0;

    const addFile = (name, blob, compress = true) => { try {
        files.push({ name, blob, compress });
        return name;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:addFile@7645', __javascriptError); throw __javascriptError; }};
    const storeAsset = async blob => { try {
        const mimeType = blob.type || 'application/octet-stream';
        const hash = await structuredBlobHash(blob);
        const extension = structuredMimeExtension(mimeType);
        const key = `${hash}.${extension}`;
        if (pathCache.has(key)) return pathCache.get(key);
        const path = `${structuredAssetFolder(mimeType)}/${key}`;
        addFile(path, blob, false);
        pathCache.set(key, path);
        assetCount++;
        return path;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:storeAsset@7649', __javascriptError); throw __javascriptError; }};
    const resolveAsset = async dataUrl => { try {
        if (assetCache.has(dataUrl)) return assetCache.get(dataUrl);
        const task = (async () => { try {
            let original;
            try { original = await (await fetch(dataUrl)).blob(); }
            catch { throw new Error('An embedded media asset could not be decoded for structured export.'); }
            const mime = String(original.type || '').toLowerCase();
            let selected = original;
            let selectedMime = original.type || 'application/octet-stream';
            let keepOriginalFallback = false;

            if (mime.startsWith('image/') && !['image/svg+xml', 'image/gif'].includes(mime) && options.imageMode !== 'preserve') {
                try {
                    let requestedMime = options.imageMode === 'png' ? 'image/png'
                        : options.imageMode === 'avif' ? 'image/avif'
                            : 'image/webp';
                    let converted = await structuredEncodeImage(original, requestedMime, options.imageQuality);
                    if (!converted && requestedMime === 'image/avif') {
                        warnings.push('This browser cannot encode AVIF; WebP was used for affected pictures.');
                        requestedMime = 'image/webp';
                        converted = await structuredEncodeImage(original, requestedMime, options.imageQuality);
                    }
                    if (converted) {
                        const shouldUse = options.imageMode === 'png' || converted.size < original.size;
                        if (shouldUse) {
                            selected = converted;
                            selectedMime = converted.type;
                        } else {
                            warnings.push('A picture conversion was skipped because it would have increased the exported file size.');
                        }
                    } else {
                        warnings.push('A picture was preserved because this browser could not encode the selected format.');
                    }
                } catch {
                    warnings.push('A picture was preserved because browser-side conversion failed.');
                }
            }

            if (mime.startsWith('video/') && options.videoMode === 'webm') {
                try {
                    const converted = await structuredTranscodeVideo(original, options.videoQuality);
                    if (converted && (converted.size < original.size || !options.keepVideoFallback)) {
                        selected = converted;
                        selectedMime = converted.type;
                        // The source is not duplicated after a successful conversion. The checkbox means
                        // "keep the source when conversion fails or is not smaller", not "embed both".
                        keepOriginalFallback = false;
                        if (converted.size >= original.size)
                            warnings.push('The requested WebM conversion was used even though it was not smaller than its source.');
                    } else if (converted) {
                        warnings.push('A video was preserved because the WebM result was not smaller than its source.');
                    } else {
                        warnings.push('A video was preserved because this browser cannot perform the requested WebM conversion.');
                    }
                } catch {
                    warnings.push('A video was preserved because browser-side WebM conversion failed.');
                }
            }

            const selectedPath = await storeAsset(selected);
            const fallbackPath = keepOriginalFallback && selected !== original ? await storeAsset(original) : '';
            return { path: selectedPath, originalPath: fallbackPath, mimeType: selectedMime, sourceMimeType: original.type || '' };
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@7663', __javascriptError); throw __javascriptError; }})();
        assetCache.set(dataUrl, task);
        return task;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:resolveAsset@7661', __javascriptError); throw __javascriptError; }};

    for (const node of exportedDocument.querySelectorAll('video[src],audio[src],source[src]')) {
        const value = node.getAttribute('src') || '';
        if (!value.startsWith('data:')) continue;
        const asset = await resolveAsset(value);
        node.setAttribute('src', asset.path);
        if (asset.originalPath) node.setAttribute('data-publisher-original-src', asset.originalPath);
        if (node instanceof HTMLSourceElement || node.tagName.toLowerCase() === 'source') node.setAttribute('type', asset.mimeType);
    }

    for (const element of exportedDocument.querySelectorAll('*')) {
        for (const attribute of [...element.attributes]) {
            if (attribute.name === 'src' && /^(video|audio|source)$/i.test(element.tagName)) continue;
            if (!attribute.value.includes('data:')) continue;
            const replaced = await replaceStructuredDataUrls(attribute.value, '', resolveAsset);
            if (replaced !== attribute.value) element.setAttribute(attribute.name, replaced);
        }
    }

    const style = exportedDocument.head.querySelector('style');
    if (style) {
        const cssText = await replaceStructuredDataUrls(style.textContent || '', '../', resolveAsset);
        addFile('css/site.css', new Blob([cssText], { type: 'text/css;charset=utf-8' }), true);
        const link = exportedDocument.createElement('link');
        link.rel = 'stylesheet';
        link.setAttribute('href', 'css/site.css');
        style.replaceWith(link);
    }

    const scripts = [...exportedDocument.querySelectorAll('script')];
    for (let index = 0; index < scripts.length; index++) {
        const script = scripts[index];
        const path = structuredExportScriptPaths[index] || `js/runtime-${String(index + 1).padStart(2, '0')}.js`;
        addFile(path, new Blob([script.textContent || ''], { type: 'text/javascript;charset=utf-8' }), true);
        script.textContent = '';
        script.setAttribute('src', path);
    }

    const uniqueWarnings = [...new Set(warnings)];
    const manifest = {
        publisherStudioVersion: '2.2.4',
        kind: options.mode,
        generatedUtc: new Date().toISOString(),
        assetCount,
        options,
        warnings: uniqueWarnings
    };
    addFile('publisherstudio-export.json', new Blob([JSON.stringify(manifest, null, 2)], { type: 'application/json;charset=utf-8' }), true);
    addFile('README.txt', new Blob([
        'PublisherStudio structured website export\n',
        'Open index.html in a modern browser or upload this folder to any static web host.\n',
        'The site is offline-capable. Live REST/OData bindings still require browser access to their configured endpoints.\n',
        'Files under assets/ are content-addressed and may be shared by several publication objects.\n'
    ], { type: 'text/plain;charset=utf-8' }), true);

    const html = `<!doctype html>\n${exportedDocument.documentElement.outerHTML}`;
    files.unshift({ name: 'index.html', blob: new Blob([html], { type: 'text/html;charset=utf-8' }), compress: true });
    return {
        files,
        assetCount,
        sourceBytes: new TextEncoder().encode(standaloneHtml).length,
        warnings: uniqueWarnings,
        options
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:buildPublisherStructuredSite@7631', __javascriptError); throw __javascriptError; }}



    const mediaConverterDropBindings = new WeakMap();

    function unbindMediaConverterDrop(element) { try {
        if (!element) return;
        const binding = mediaConverterDropBindings.get(element);
        if (!binding) return;
        element.removeEventListener("dragenter", binding.dragenter);
        element.removeEventListener("dragover", binding.dragover);
        element.removeEventListener("dragleave", binding.dragleave);
        element.removeEventListener("drop", binding.drop);
        element.classList.remove("drag-active");
        mediaConverterDropBindings.delete(element);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:unbindMediaConverterDrop@7793', __javascriptError); throw __javascriptError; }}

    function bindMediaConverterDrop(element, dotNetReference) { try {
        if (!element || !dotNetReference) return false;
        unbindMediaConverterDrop(element);
        let depth = 0;
        const dragenter = event => { try {
            if (!event.dataTransfer?.types?.includes("Files")) return;
            event.preventDefault();
            depth += 1;
            element.classList.add("drag-active");
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:dragenter@7809', __javascriptError); throw __javascriptError; }};
        const dragover = event => { try {
            if (!event.dataTransfer?.types?.includes("Files")) return;
            event.preventDefault();
            event.dataTransfer.dropEffect = "copy";
            element.classList.add("drag-active");
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:dragover@7815', __javascriptError); throw __javascriptError; }};
        const dragleave = event => { try {
            event.preventDefault();
            depth = Math.max(0, depth - 1);
            if (depth === 0) element.classList.remove("drag-active");
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:dragleave@7821', __javascriptError); throw __javascriptError; }};
        const drop = async event => { try {
            event.preventDefault();
            depth = 0;
            element.classList.remove("drag-active");
            const file = event.dataTransfer?.files?.[0];
            if (!file) return;
            try {
                const assetId = crypto.randomUUID();
                const response = await fetch(`/api/assets/drop/${assetId}`, {
                    method: "POST",
                    headers: { "Content-Type": file.type || "application/octet-stream" },
                    body: file
                });
                if (!response.ok) throw new Error(await response.text() || `Media upload failed (${response.status}).`);
                await dotNetReference.invokeMethodAsync("ReceiveDroppedMedia", assetId, file.name || "dropped-media", file.type || "application/octet-stream");
            } catch (error) {
                await dotNetReference.invokeMethodAsync("ReceiveMediaDropError", String(error?.message || error || "The dropped media could not be loaded.")).catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/publisherInterop.js:promise-catch@7842', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:dotNetReference.invokeMethodAsync("ReceiveMediaDropError", String(erro@7842', __javascriptError); throw __javascriptError; }});
            }
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:drop@7826', __javascriptError); throw __javascriptError; }};
        const binding = { dragenter, dragover, dragleave, drop };
        mediaConverterDropBindings.set(element, binding);
        element.addEventListener("dragenter", dragenter);
        element.addEventListener("dragover", dragover);
        element.addEventListener("dragleave", dragleave);
        element.addEventListener("drop", drop);
        return true;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:bindMediaConverterDrop@7805', __javascriptError); throw __javascriptError; }}

window.publisherStudio = {
    setDocumentDirty(value) { try { publisherDocumentDirty = Boolean(value);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:setDocumentDirty@7855', __javascriptError); throw __javascriptError; }},
    restorePublisherWorkspaceAfterExport(stageId = 'publisher-stage') { try {
        activeVideoExportCancel?.();
        document.querySelectorAll('.publisher-video-export-overlay').forEach(element => { try { return (element.remove()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:document.querySelectorAll(\'.publisher-video-export-overlay\').forEach@7858', __javascriptError); throw __javascriptError; } });
        const stage = document.getElementById(stageId);
        if (stage) {
            try { stage.focus({ preventScroll: true }); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@7861', __caughtJavaScriptError);  }
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:restorePublisherWorkspaceAfterExport@7856', __javascriptError); throw __javascriptError; }},
    cancelCanvasInteraction(stageId = 'publisher-stage') { try { cancelCanvasInteraction(stageId);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:cancelCanvasInteraction@7864', __javascriptError); throw __javascriptError; }},
    initializeStoryEditorLayout(shellId, hostId, dotNetReference = null) { try { initializeStoryEditorLayout(shellId, hostId, dotNetReference);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:initializeStoryEditorLayout@7865', __javascriptError); throw __javascriptError; }},
    prepareStoryPreviewHtml(html, preferredBackground = '') { try { return prepareStoryPreviewHtml(html, preferredBackground);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:prepareStoryPreviewHtml@7866', __javascriptError); throw __javascriptError; }},
    prepareStoryPreviewHtmlInChunks(htmlStream, preferredBackground = '', dotNetReference) { try { return prepareStoryPreviewHtmlInChunks(htmlStream, preferredBackground, dotNetReference);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:prepareStoryPreviewHtmlInChunks@7867', __javascriptError); throw __javascriptError; }},
    generateBarcodeSvg(options) { try { return generateBarcodeSvg(options);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:generateBarcodeSvg@7868', __javascriptError); throw __javascriptError; }},
    exportPresentationVideo(containerSelector, fileName, title) { try { return exportPresentationVideo(containerSelector, fileName, title);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:exportPresentationVideo@7869', __javascriptError); throw __javascriptError; }},
    initializeSignalConnectors(rootId, options = {}) { try {
        const root = typeof rootId === 'string' ? document.getElementById(rootId) : rootId;
        if (!root) return false;
        root.__publisherSignalRuntime?.dispose?.();
        root.__publisherSignalRuntime = signalConnectorRuntime(root, options);
        return Boolean(root.__publisherSignalRuntime);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:initializeSignalConnectors@7870', __javascriptError); throw __javascriptError; }},
    runSignalConnector(elementId) { try {
        const id = String(elementId || '').replace(/^element-/, '');
        const root = document.getElementById('publisher-page') || document;
        if (!root.__publisherSignalRuntime) root.__publisherSignalRuntime = signalConnectorRuntime(root, { autoStart: false, editor: true });
        return root.__publisherSignalRuntime?.run(id);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:runSignalConnector@7877', __javascriptError); throw __javascriptError; }},
    stopSignalConnectors(rootId) { try {
        const root = typeof rootId === 'string' ? document.getElementById(rootId) : rootId;
        root?.__publisherSignalRuntime?.reset?.();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:stopSignalConnectors@7883', __javascriptError); throw __javascriptError; }},

    refreshPanelStudioDesignSurface(element) { try { return refreshPanelStudioDesignSurface(element);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:refreshPanelStudioDesignSurface@7888', __javascriptError); throw __javascriptError; }},
    panelStudioPoint(element, clientX, clientY) { try { return panelStudioPoint(element, clientX, clientY);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:panelStudioPoint@7889', __javascriptError); throw __javascriptError; }},
    bindPanelStudioDropSurface(element, dotNetReference, bindingId = '') { try { return bindPanelStudioDropSurface(element, dotNetReference, bindingId);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:bindPanelStudioDropSurface@7890', __javascriptError); throw __javascriptError; }},
    flushPanelStudioInteractions(element) { try { return flushPanelStudioInteractions(element);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:flushPanelStudioInteractions', __javascriptError); throw __javascriptError; }},
    cancelPanelStudioPointer(element, restore = true) { try { cancelPanelStudioPointer(element, restore);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:cancelPanelStudioPointer@7890', __javascriptError); throw __javascriptError; }},
    unbindPanelStudioDropSurface(element) { try { unbindPanelStudioDropSurface(element);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:unbindPanelStudioDropSurface@7891', __javascriptError); throw __javascriptError; }},
    clickElement(id) { try { clickElementById(id);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:clickElement@7892', __javascriptError); throw __javascriptError; }},
    focusElement(id) { try {
        const element = document.getElementById(id);
        if (!element) return;
        element.scrollIntoView({ block: 'nearest', inline: 'nearest', behavior: 'smooth' });
        const focusable = element.querySelector('input,select,textarea,button,[tabindex]:not([tabindex="-1"])');
        setTimeout(() => { try { try { focusable?.focus({ preventScroll: true }); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@7898', __caughtJavaScriptError);  }  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:setTimeout@7898', __javascriptError); throw __javascriptError; }}, 180);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:focusElement@7893', __javascriptError); throw __javascriptError; }},
    reserveStoryPrintPreviewFromEvent(event) { try { reserveStoryPrintPreviewFromEvent(event);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:reserveStoryPrintPreviewFromEvent@7900', __javascriptError); throw __javascriptError; }},
    claimStoryPrintPreview(title) { try { return claimStoryPrintPreview(title);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:claimStoryPrintPreview@7901', __javascriptError); throw __javascriptError; }},
    openStoryPrintPreview(title) { try { return openStoryPrintPreview(title);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:openStoryPrintPreview@7902', __javascriptError); throw __javascriptError; }},
    completeStoryPrintPreview(id, html) { try { return completeStoryPrintPreview(id, html);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:completeStoryPrintPreview@7903', __javascriptError); throw __javascriptError; }},
    failStoryPrintPreview(id, message) { try { failStoryPrintPreview(id, message);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:failStoryPrintPreview@7904', __javascriptError); throw __javascriptError; }},
    printStoryHtml(html) { try {
        const id = openStoryPrintPreview('Story print preview');
        if (!id) throw new Error('The browser blocked the story print-preview window.');
        return completeStoryPrintPreview(id, html);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:printStoryHtml@7905', __javascriptError); throw __javascriptError; }},
    consumeCanvasInsertPlacement(id) { try { return consumeCanvasInsertPlacement(id);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:consumeCanvasInsertPlacement@7910', __javascriptError); throw __javascriptError; }},

    initializeWorkspace(id) { try {
        const workspace = document.getElementById(id);
        if (!workspace) return;
        if (!workspaceStates.has(workspace)) createWorkspaceState(workspace);
        bindWorkspaceSplitter(workspace, workspace.querySelector('[data-workspace-splitter="left"]'), 'left');
        bindWorkspaceSplitter(workspace, workspace.querySelector('[data-workspace-splitter="right"]'), 'right');
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:initializeWorkspace@7912', __javascriptError); throw __javascriptError; }},

    toggleWorkspacePane(id, side) { try {
        const workspace = document.getElementById(id);
        if (!workspace) return;
        const state = workspaceStates.get(workspace) || createWorkspaceState(workspace);
        if (side === 'left') state.leftCollapsed = !state.leftCollapsed;
        else state.rightCollapsed = !state.rightCollapsed;
        setWorkspaceColumns(workspace, state);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:toggleWorkspacePane@7920', __javascriptError); throw __javascriptError; }},

    resetWorkspaceLayout(id) { try {
        const workspace = document.getElementById(id);
        if (!workspace) return;
        const state = { left: 172, right: 292, leftCollapsed: false, rightCollapsed: false };
        workspaceStates.set(workspace, state);
        setWorkspaceColumns(workspace, state);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:resetWorkspaceLayout@7929', __javascriptError); throw __javascriptError; }},

    previewPageAnimations(pageId) { try { previewPageAnimations(pageId);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:previewPageAnimations@7937', __javascriptError); throw __javascriptError; }},
    previewElementAnimations(elementId) { try { previewElementAnimations(elementId);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:previewElementAnimations@7938', __javascriptError); throw __javascriptError; }},
    previewAnimationStep(pageId, animationId) { try { previewAnimationStep(pageId, animationId);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:previewAnimationStep@7939', __javascriptError); throw __javascriptError; }},
    stopAnimationPreview(pageId) { try { stopAnimationPreview(pageId);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:stopAnimationPreview@7940', __javascriptError); throw __javascriptError; }},
    playPublicationMedia(elementId) { try { playPublicationMedia(elementId);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:playPublicationMedia@7941', __javascriptError); throw __javascriptError; }},
    pausePublicationMedia(elementId) { try { pausePublicationMedia(elementId);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:pausePublicationMedia@7942', __javascriptError); throw __javascriptError; }},

    async downloadStream(fileName, streamReference, mimeType) { try {
        const buffer = await streamReference.arrayBuffer();
        downloadBlob(fileName, new Blob([buffer], { type: mimeType || 'application/octet-stream' }));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:downloadStream@7944', __javascriptError); throw __javascriptError; }},

    downloadTextFile(fileName, text, mimeType = 'text/plain;charset=utf-8') { try {
        downloadBlob(fileName || 'publisherstudio.txt', new Blob([String(text ?? '')], { type: mimeType }));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:downloadTextFile@7949', __javascriptError); throw __javascriptError; }},

    async exportPage(pageId, fileName, format, dpi, zoom, jpegQuality = .92) { try {
        const page = document.getElementById(pageId);
        if (!page) throw new Error('The publication page is not available.');
        const pageKey = page.dataset.pageId || '';
        const exportSource = pageKey
            ? document.querySelector(`.print-publication > .print-page[data-page-id="${CSS.escape(pageKey)}"]`) || page
            : page;
        refreshContentFit(exportSource);
        await new Promise(resolve => { try { return (requestAnimationFrame(resolve)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@7961', __javascriptError); throw __javascriptError; } });
        const normalized = String(format).toLowerCase();
        const scale = clamp(number(dpi, 150) / 96, .5, 12);
        const canvas = await rasterizePageElement(exportSource, scale);
        if (normalized === 'svg') {
            const metrics = pageExportMetrics(exportSource);
            const svg = canvasToEmbeddedSvg(canvas, metrics.widthMm, metrics.heightMm);
            downloadBlob(fileName, new Blob([svg], { type: 'image/svg+xml;charset=utf-8' }));
            return;
        }

        const jpeg = normalized === 'jpeg' || normalized === 'jpg';
        if (!jpeg && normalized !== 'png') throw new Error('Only PNG, JPEG, and SVG page export are supported.');
        const output = prepareOutputCanvas(canvas, jpeg);
        const blob = await canvasBlob(output, jpeg ? 'image/jpeg' : 'image/png', jpeg ? clamp(number(jpegQuality, .92), .35, 1) : undefined);
        downloadBlob(fileName, blob);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:exportPage@7953', __javascriptError); throw __javascriptError; }},

    async exportPublicationElement(elementId, fileName, format, dpi) { try {
        const id = String(elementId || '');
        if (!id) throw new Error('No publication object was selected.');
        const element = document.querySelector(`.print-publication [data-element-id="${CSS.escape(id)}"]`);
        if (!element) throw new Error('The selected object is not available on the export surface.');
        if (element.classList.contains('print-connector')) throw new Error('Connector-only export is not supported yet.');
        const page = element.closest('.print-page');
        if (!page) throw new Error('The selected object is not attached to a publication page.');
        refreshContentFit(page);
        await new Promise(resolve => { try { return (requestAnimationFrame(resolve)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@7988', __javascriptError); throw __javascriptError; } });
        const scale = clamp(number(dpi, 150) / 96, .5, 12);
        const pageCanvas = await rasterizeIsolatedPublicationElement(page, element, scale);
        const objectCanvas = cropCanvasToElement(pageCanvas, page, element, Math.max(2, Math.ceil(scale * 1.5)));
        const normalized = String(format).toLowerCase();
        if (normalized === 'svg') {
            const svg = canvasToEmbeddedSvg(objectCanvas);
            downloadBlob(fileName, new Blob([svg], { type: 'image/svg+xml;charset=utf-8' }));
            return;
        }
        if (normalized !== 'png') throw new Error('Selected objects can be exported as PNG or SVG.');
        downloadBlob(fileName, await canvasBlob(objectCanvas, 'image/png'));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:exportPublicationElement@7979', __javascriptError); throw __javascriptError; }},

    async exportPublicationPages(containerSelector, baseName, format, dpi, compressArchive = true, jpegQuality = .92) { try {
        const container = document.querySelector(containerSelector);
        if (!container) throw new Error('The publication export surface is not available.');
        const pages = [...container.querySelectorAll(':scope > .print-page')];
        if (!pages.length) throw new Error('The publication does not contain any pages.');
        refreshContentFit(container);
        await new Promise(resolve => { try { return (requestAnimationFrame(resolve)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@8008', __javascriptError); throw __javascriptError; } });
        const normalized = String(format).toLowerCase();
        const jpeg = normalized === 'jpeg' || normalized === 'jpg';
        if (!jpeg && normalized !== 'png') throw new Error('Only PNG and JPEG page export are supported here.');
        const extension = jpeg ? 'jpg' : 'png';
        const mimeType = jpeg ? 'image/jpeg' : 'image/png';
        const scale = clamp(number(dpi, 150) / 96, .5, 12);
        const safeBase = String(baseName || 'publication').replace(/[<>:"/\\|?*\u0000-\u001f]+/g, '-').replace(/[. ]+$/g, '') || 'publication';
        const files = [];
        for (let index = 0; index < pages.length; index++) {
            try {
                if (index > 0) await new Promise(resolve => { try { return (requestAnimationFrame(resolve)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@8019', __javascriptError); throw __javascriptError; } });
                const canvas = await rasterizePageElement(pages[index], scale);
                const output = prepareOutputCanvas(canvas, jpeg);
                const blob = await canvasBlob(output, mimeType, jpeg ? clamp(number(jpegQuality, .92), .35, 1) : undefined);
                files.push({ name: `${safeBase}-page-${index + 1}.${extension}`, blob });
            } catch (error) {
                console.error(`Page ${index + 1} raster export failed.`, error);
                throw new Error(`Page ${index + 1} could not be exported: ${error?.message || error}`);
            }
        }
        if (files.length === 1) {
            downloadBlob(files[0].name, files[0].blob);
            return { count: 1, fileName: files[0].name };
        }
        const archiveName = `${safeBase}-${extension}-pages.zip`;
        downloadBlob(archiveName, await createZip(files, { compress: compressArchive !== false }));
        return { count: files.length, fileName: archiveName };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:exportPublicationPages@8002', __javascriptError); throw __javascriptError; }},

    async verifyPageRaster(pageId) { try {
        const page = document.getElementById(pageId);
        if (!page) throw new Error('The publication page is not available.');
        const canvas = await rasterizePageElement(page, 1, false);
        return { width: canvas.width, height: canvas.height, prefix: canvas.toDataURL('image/png').slice(0, 22) };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:verifyPageRaster@8038', __javascriptError); throw __javascriptError; }},

    async makeColorTransparent(dataUrl, color, tolerance) { try {
        const image = await imageFromDataUrl(dataUrl);
        const canvas = document.createElement('canvas');
        canvas.width = image.naturalWidth;
        canvas.height = image.naturalHeight;
        const context = canvas.getContext('2d', { willReadFrequently: true });
        if (!context) throw new Error('The browser did not provide an image editing canvas.');
        context.drawImage(image, 0, 0);
        const pixels = context.getImageData(0, 0, canvas.width, canvas.height);
        const target = parseHexColor(color);
        const threshold = clamp(number(tolerance, 24), 0, 255);
        const thresholdSquared = threshold * threshold * 3;
        for (let index = 0; index < pixels.data.length; index += 4) {
            const dr = pixels.data[index] - target.r;
            const dg = pixels.data[index + 1] - target.g;
            const db = pixels.data[index + 2] - target.b;
            if (dr * dr + dg * dg + db * db <= thresholdSquared) pixels.data[index + 3] = 0;
        }
        context.putImageData(pixels, 0, 0);
        return canvas.toDataURL('image/png');
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:makeColorTransparent@8045', __javascriptError); throw __javascriptError; }},

    async exportWebsite(fileName, title, options = {}) { try {
        const html = await buildPublisherSingleHtml('presentation', title, options);
        const blob = new Blob([html], { type: 'text/html;charset=utf-8' });
        downloadBlob(fileName, blob);
        const stats = window.__publisherSingleExportStats || {};
        return { fileName, sourceBytes: Number(stats.sourceBytes || 0), outputBytes: Number(stats.outputBytes || 0), warnings: Array.isArray(stats.warnings) ? stats.warnings : [] };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:exportWebsite@8067', __javascriptError); throw __javascriptError; }},

    async exportSite(fileName, title, options = {}) { try {
        const html = await buildPublisherSingleHtml('site', title, options);
        const blob = new Blob([html], { type: 'text/html;charset=utf-8' });
        downloadBlob(fileName, blob);
        const stats = window.__publisherSingleExportStats || {};
        return { fileName, sourceBytes: Number(stats.sourceBytes || 0), outputBytes: Number(stats.outputBytes || 0), warnings: Array.isArray(stats.warnings) ? stats.warnings : [] };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:exportSite@8072', __javascriptError); throw __javascriptError; }},

    async exportStructuredWebsite(fileName, title, options = {}) { try {
        const result = await buildPublisherStructuredSite(title, options);
        const archive = await createZip(result.files, { compress: result.options.compressArchive });
        downloadBlob(fileName, archive);
        return {
            fileName,
            assetCount: result.assetCount,
            sourceBytes: result.sourceBytes,
            archiveBytes: archive.size,
            warnings: result.warnings
        };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:exportStructuredWebsite@8077', __javascriptError); throw __javascriptError; }},

    async printPublication() { try {
        const active = document.activeElement;
        try { active?.blur?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:suppressed-catch@8092', __caughtJavaScriptError);  }
        document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
        document.body.classList.add('publisher-printing');
        await new Promise(resolve => { try { return (requestAnimationFrame(() => { try { return (requestAnimationFrame(resolve)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:requestAnimationFrame@8095', __javascriptError); throw __javascriptError; } })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@8095', __javascriptError); throw __javascriptError; } });
        refreshContentFit(document.querySelector('.print-publication') || document);
        await new Promise(resolve => { try { return (requestAnimationFrame(() => { try { return (requestAnimationFrame(resolve)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:callback:requestAnimationFrame@8097', __javascriptError); throw __javascriptError; } })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:ArrowFunction@8097', __javascriptError); throw __javascriptError; } });
        const cleanup = () => { try { return (document.body.classList.remove('publisher-printing')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:cleanup@8098', __javascriptError); throw __javascriptError; } };
        window.addEventListener('afterprint', cleanup, { once: true });
        try { window.print(); } finally { setTimeout(cleanup, 1500); }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/publisherInterop.js:printPublication@8090', __javascriptError); throw __javascriptError; }}
};

window.publisherStudio.bindMediaConverterDrop = bindMediaConverterDrop;
window.publisherStudio.unbindMediaConverterDrop = unbindMediaConverterDrop;
window.publisherStudio.configureStudioDragTransfer = configureStudioDragTransfer;
window.publisherStudio.readStudioDragTransfer = readStudioDragTransfer;
window.publisherStudio.studioMediaDescriptorFromTransfer = studioMediaDescriptorFromTransfer;
window.publisherStudio.fileFromStudioDragTransfer = fileFromStudioDragTransfer;

// Guard exported browser namespaces after the file has initialized.
publisherStudioDiagnostics.guardObject("publisherStudio", window.publisherStudio);
publisherStudioDiagnostics.guardObject("PublisherStudioNavigation", window.PublisherStudioNavigation);
publisherStudioDiagnostics.guardObject("PublisherStudioPresentation", window.PublisherStudioPresentation);
publisherStudioDiagnostics.guardObject("PublisherStudioSite", window.PublisherStudioSite);
