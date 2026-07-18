const canvasStates = new WeakMap();
const boundRulers = new WeakSet();
const wordArtPathStates = new WeakMap();
const PX_PER_MM_AT_96_DPI = 96 / 25.4;
let publisherDocumentDirty = false;
let activeVideoExportCancel = null;

window.addEventListener('beforeunload', event => {
    if (!publisherDocumentDirty) return;
    event.preventDefault();
    event.returnValue = '';
});

function number(value, fallback = 0) {
    const parsed = Number.parseFloat(value);
    return Number.isFinite(parsed) ? parsed : fallback;
}

function clamp(value, min, max) {
    return Math.max(min, Math.min(max, value));
}

function elementMm(element, pxPerMm) {
    return {
        x: number(element.style.left) / pxPerMm,
        y: number(element.style.top) / pxPerMm,
        width: number(element.style.width) / pxPerMm,
        height: number(element.style.height) / pxPerMm
    };
}

function nextAnimationFrame(state) {
    if (!state || state.drawPending) return;
    state.drawPending = true;
    requestAnimationFrame(() => {
        state.drawPending = false;
        if (!state.stage?.isConnected || !state.scroll?.isConnected || !state.page?.isConnected) return;
        try { drawRulers(state); } catch (error) { console.warn('Publisher ruler redraw failed.', error); }
    });
}

function safeDotNet(state, method, ...args) {
    if (!state?.dotnet) return Promise.resolve();
    return state.dotnet.invokeMethodAsync(method, ...args).catch(error => {
        const message = String(error?.message || error || '');
        if (!/disconnected|disposed|circuit/i.test(message))
            console.warn(`Publisher callback ${method} failed.`, error);
    });
}

function clearObjectAlignmentFeedback(state) {
    if (!state?.page) return;
    state.page.querySelectorAll('.alignment-moving-green,.alignment-moving-orange,.alignment-moving-red,.alignment-target-green,.alignment-target-orange,.alignment-target-red')
        .forEach(element => element.classList.remove('alignment-moving-green','alignment-moving-orange','alignment-moving-red','alignment-target-green','alignment-target-orange','alignment-target-red'));
    state.page.querySelectorAll('.publisher-object-alignment-overlay').forEach(element => element.remove());
}

function publicationObjectBounds(state, excludedIds) {
    const excluded = excludedIds instanceof Set ? excludedIds : new Set([excludedIds].filter(Boolean));
    return [...state.page.querySelectorAll('[data-publication-element][data-element-id]')]
        .filter(element => !excluded.has(element.dataset.elementId) && !element.matches('[data-connector-id]') && !element.classList.contains('locked-hidden'))
        .map(element => ({
            element,
            id: element.dataset.elementId,
            zIndex: number(element.style.zIndex),
            ...elementMm(element, state.config.pxPerMm)
        }));
}

function rectanglesOverlap(a, b) {
    return Math.min(a.x + a.width, b.x + b.width) - Math.max(a.x, b.x) > .15
        && Math.min(a.y + a.height, b.y + b.height) - Math.max(a.y, b.y) > .15;
}

function overlapArea(a, b) {
    const width = Math.max(0, Math.min(a.x + a.width, b.x + b.width) - Math.max(a.x, b.x));
    const height = Math.max(0, Math.min(a.y + a.height, b.y + b.height) - Math.max(a.y, b.y));
    return width * height;
}

function rectangleGap(a, b) {
    const horizontal = Math.max(b.x - (a.x + a.width), a.x - (b.x + b.width), 0);
    const vertical = Math.max(b.y - (a.y + a.height), a.y - (b.y + b.height), 0);
    return Math.hypot(horizontal, vertical);
}

function internalSnapFractions(size, pxPerMm) {
    const pixels = size * pxPerMm;
    const step = pixels >= 520 ? .05 : pixels >= 260 ? .1 : .25;
    const values = new Set([0, .25, .5, .75, 1]);
    for (let value = 0; value <= 1.0001; value += step) values.add(Math.round(value * 1000) / 1000);
    return [...values].sort((a, b) => a - b);
}

function chooseInternalTarget(targets, moving, nearTolerance) {
    const overlapping = targets
        .map(target => ({ target, area: overlapArea(moving, target) }))
        .filter(item => item.area > .15)
        .sort((a, b) => b.target.zIndex - a.target.zIndex || b.area - a.area);
    if (overlapping.length) return overlapping[0].target;

    return targets
        .map(target => ({ target, distance: rectangleGap(moving, target) }))
        .filter(item => item.distance <= nearTolerance)
        .sort((a, b) => a.distance - b.distance || b.target.zIndex - a.target.zIndex)[0]?.target || null;
}

function sourceAnchors(size, grab, extraOffsets = []) {
    const offsets = new Set([0, size / 2, size, ...extraOffsets].map(value => Math.round(value * 10000) / 10000));
    return [...offsets]
        .filter(offset => offset >= -.0001 && offset <= size + .0001)
        .map(offset => {
            const fraction = size > 0 ? offset / size : .5;
            return { offset, fraction, penalty: Math.abs(grab - fraction) };
        });
}

function movingAnchorOffsets(operation, axis) {
    const bounds = operation.movingBounds;
    if (!bounds || !operation.moving?.length) return [];
    const origin = axis === 'x' ? bounds.x : bounds.y;
    return operation.moving.flatMap(item => {
        const start = (axis === 'x' ? item.x : item.y) - origin;
        const size = axis === 'x' ? item.width : item.height;
        return [start, start + size / 2, start + size];
    });
}

function snapCandidate(mode, axis, target, destination, source, rawStart, tolerance, percent = null) {
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
}

function pickSnapCandidate(candidates, previous, tolerance, releaseTolerance) {
    const valid = candidates.filter(Boolean);
    if (previous?.key) {
        const locked = valid.find(candidate => candidate.key === previous.key && Math.abs(candidate.delta) <= releaseTolerance);
        if (locked) return locked;
    }
    return valid.filter(candidate => Math.abs(candidate.delta) <= tolerance).sort((a, b) => a.score - b.score)[0] || null;
}

function objectSnapResult(state, operation, x, y, width, height) {
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
            .filter(candidate => candidate?.mode === 'inside')
            .map(candidate => candidate.target.id)
    ].filter(Boolean));
    const collisions = targets.filter(target => !intentionalTargetIds.has(target.id) && rectanglesOverlap(moved, target));
    const alignedTargets = new Set([bestX?.target, bestY?.target].filter(Boolean));
    let nearTarget = null;
    let nearestDistance = Infinity;
    for (const target of targets) {
        const distance = rectangleGap(moved, target);
        if (distance < nearestDistance) { nearestDistance = distance; nearTarget = target; }
    }
    const status = collisions.length ? 'red' : (bestX || bestY) ? 'green' : nearestDistance <= nearTolerance ? 'orange' : null;
    return { x, y, width, height, bestX, bestY, collisions, alignedTargets, nearTarget, internalTarget, status };
}

function showObjectAlignmentFeedback(state, operation, result) {
    clearObjectAlignmentFeedback(state);
    if (!result?.status) return;
    for (const moving of operation.moving || []) moving.element?.classList.add(`alignment-moving-${result.status}`);
    if (!operation.moving?.length) refreshOperationElement(state, operation)?.classList.add(`alignment-moving-${result.status}`);
    const highlighted = result.status === 'red' ? result.collisions : result.status === 'green' ? [...result.alignedTargets] : [result.nearTarget].filter(Boolean);
    highlighted.forEach(target => target?.element?.classList.add(`alignment-target-${result.status}`));
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

    const internal = [result.bestX, result.bestY].filter(candidate => candidate?.mode === 'inside');
    if (internal.length) {
        const label = document.createElement('span');
        label.className = 'publisher-object-alignment-label';
        label.textContent = internal.map(candidate => `${candidate.axis.toUpperCase()} ${Math.round(candidate.percent * 100)}%`).join(' · ');
        label.style.left = `${(result.x + result.width / 2) * state.config.pxPerMm}px`;
        label.style.top = `${Math.max(0, result.y * state.config.pxPerMm - 26)}px`;
        overlay.appendChild(label);
    }
    state.page.appendChild(overlay);
}

function resetPointerOperation(state, restoreDom = false) {
    clearObjectAlignmentFeedback(state);
    const operation = state?.operation;
    if (!operation) return;
    state.operation = null;
    try { state.stage.releasePointerCapture(operation.pointerId); } catch { }

    if (operation.kind?.startsWith('connector-')) {
        state.operation = operation;
        clearConnectorOperation(state, true);
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
        updateAttachedConnectors(state, item.id);
    }
}

function insertionKindFromEvent(state, event) {
    return event.dataTransfer?.getData('application/x-publisher-insert')
        || event.dataTransfer?.getData('text/x-publisher-insert')
        || String(state?.insertDragSource?.dataset?.publisherInsert || '');
}

function insertionDragStart(state, event) {
    const source = event.target?.closest?.('[data-publisher-insert]');
    if (!source || !event.dataTransfer) return;
    const kind = String(source.dataset.publisherInsert || '').trim().toLowerCase();
    if (!kind) return;
    event.dataTransfer.effectAllowed = 'copy';
    event.dataTransfer.setData('application/x-publisher-insert', kind);
    event.dataTransfer.setData('text/x-publisher-insert', kind);
    source.classList.add('dragging');
    state.insertDragSource = source;
}

function clearInsertionDrag(state) {
    state?.insertDragSource?.classList?.remove('dragging');
    state.insertDragSource = null;
    state?.page?.classList?.remove('insert-drop-target');
}

function insertionDragEnd(state) {
    state.suppressInsertClickUntil = performance.now() + 350;
    clearInsertionDrag(state);
}

function suppressInsertionClick(state, event) {
    if (performance.now() > number(state?.suppressInsertClickUntil)) return;
    if (!event.target?.closest?.('[data-publisher-insert]')) return;
    event.preventDefault();
    event.stopImmediatePropagation();
}

function insertionDragOver(state, event) {
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
}

async function insertionDrop(state, event) {
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
}


function externalDraggedFile(event) {
    const transfer = event?.dataTransfer;
    if (!transfer) return null;
    if (transfer.files?.length) return transfer.files[0];
    const item = [...(transfer.items || [])].find(candidate => candidate.kind === 'file');
    return item?.getAsFile?.() || null;
}

function externalDropKind(file) {
    const name = String(file?.name || '').toLowerCase();
    const mime = String(file?.type || '').toLowerCase();
    if (mime.startsWith('image/') || /\.(png|jpe?g|gif|webp|svg)$/.test(name)) return 'picture';
    if (mime.startsWith('video/') || /\.(mp4|m4v|webm|ogv|ogg|mov)$/.test(name)) return 'video';
    if (mime === 'text/markdown' || /\.(md|markdown)$/.test(name)) return 'markdown';
    if (mime.startsWith('text/') || /\.(txt|text|log|csv|tsv)$/.test(name)) return 'text';
    if (/\.docx$/.test(name)) return 'docx';
    return '';
}

function clearExternalDropPreview(state) {
    const preview = state?.externalDropPreview;
    if (!preview) return;
    if (preview.url) URL.revokeObjectURL(preview.url);
    preview.ghost?.remove?.();
    preview.overlay?.remove?.();
    state.page?.classList?.remove('external-file-drop-target');
    state.externalDropPreview = null;
}

function createExternalDropPreview(state, file, kind) {
    clearExternalDropPreview(state);
    const overlay = document.createElement('div');
    overlay.className = 'publisher-external-drop-overlay';
    const message = document.createElement('span');
    message.className = 'publisher-external-drop-message';
    message.textContent = kind === 'picture' ? 'Drop picture at this position'
        : kind === 'video' ? 'Drop video at this position'
        : kind === 'markdown' ? 'Drop Markdown as a text frame'
        : kind === 'text' ? 'Drop text as a text frame'
        : 'This file type is not supported yet';
    overlay.appendChild(message);
    state.page.appendChild(overlay);

    const ghost = document.createElement('div');
    ghost.className = `publisher-external-drop-ghost kind-${kind || 'unsupported'}`;
    ghost.setAttribute('aria-hidden', 'true');
    let url = '';
    const fileKey = `${file.name}|${file.size}|${file.lastModified}|${file.type}`;
    const preview = { file, fileKey, kind, overlay, ghost, url, widthPx: 190, heightPx: kind === 'video' ? 108 : 120, pixelWidth: 0, pixelHeight: 0, durationSeconds: 0 };

    if (kind === 'picture') {
        url = URL.createObjectURL(file);
        preview.url = url;
        const image = document.createElement('img');
        image.src = url;
        image.alt = '';
        image.addEventListener('load', () => {
            preview.pixelWidth = image.naturalWidth || 0;
            preview.pixelHeight = image.naturalHeight || 0;
            if (preview.pixelWidth > 0 && preview.pixelHeight > 0) {
                preview.heightPx = clamp(preview.widthPx * preview.pixelHeight / preview.pixelWidth, 54, 220);
                ghost.style.height = `${preview.heightPx}px`;
            }
        }, { once: true });
        ghost.appendChild(image);
    } else if (kind === 'video') {
        url = URL.createObjectURL(file);
        preview.url = url;
        const video = document.createElement('video');
        video.src = url;
        video.muted = true;
        video.playsInline = true;
        video.autoplay = true;
        video.loop = true;
        video.preload = 'metadata';
        video.play().catch(() => { });
        video.addEventListener('loadedmetadata', () => {
            preview.pixelWidth = video.videoWidth || 0;
            preview.pixelHeight = video.videoHeight || 0;
            preview.durationSeconds = Number.isFinite(video.duration) ? Math.max(0, video.duration) : 0;
            if (preview.pixelWidth > 0 && preview.pixelHeight > 0) {
                preview.heightPx = clamp(preview.widthPx * preview.pixelHeight / preview.pixelWidth, 54, 220);
                ghost.style.height = `${preview.heightPx}px`;
            }
        }, { once: true });
        ghost.appendChild(video);
    } else {
        const icon = document.createElement('b');
        icon.textContent = kind === 'markdown' ? 'MD' : kind === 'text' ? 'TXT' : '?';
        const label = document.createElement('small');
        label.textContent = file.name || 'Dropped file';
        ghost.append(icon, label);
        if (kind === 'text' || kind === 'markdown') {
            file.slice(0, 4096).text().then(text => {
                if (state.externalDropPreview !== preview) return;
                const excerpt = document.createElement('p');
                excerpt.textContent = text.replace(/\s+/g, ' ').trim().slice(0, 180) || '(empty text file)';
                ghost.appendChild(excerpt);
            }).catch(() => { });
        }
    }
    ghost.style.width = `${preview.widthPx}px`;
    ghost.style.height = `${preview.heightPx}px`;
    state.page.appendChild(ghost);
    state.page.classList.add('external-file-drop-target');
    state.externalDropPreview = preview;
    return preview;
}

function positionExternalDropPreview(state, event) {
    const preview = state.externalDropPreview;
    if (!preview) return null;
    const rect = state.page.getBoundingClientRect();
    const xPx = clamp(event.clientX - rect.left, 0, rect.width);
    const yPx = clamp(event.clientY - rect.top, 0, rect.height);
    preview.ghost.style.left = `${xPx}px`;
    preview.ghost.style.top = `${yPx}px`;
    const x = clamp(xPx / state.config.pxPerMm, 0, number(state.page.dataset.pageWidthMm));
    const y = clamp(yPx / state.config.pxPerMm, 0, number(state.page.dataset.pageHeightMm));
    return { x, y };
}

function externalFileDragOver(state, event) {
    const file = externalDraggedFile(event);
    if (!file) return false;
    event.preventDefault();
    event.dataTransfer.dropEffect = 'copy';
    const kind = externalDropKind(file);
    const current = state.externalDropPreview;
    const fileKey = `${file.name}|${file.size}|${file.lastModified}|${file.type}`;
    if (!current || current.fileKey !== fileKey || current.kind !== kind)
        createExternalDropPreview(state, file, kind);
    positionExternalDropPreview(state, event);
    return true;
}

async function externalFileDrop(state, event) {
    const file = externalDraggedFile(event);
    if (!file) return false;
    event.preventDefault();
    event.stopPropagation();
    const preview = state.externalDropPreview || createExternalDropPreview(state, file, externalDropKind(file));
    const placement = positionExternalDropPreview(state, event) || { x: 0, y: 0 };
    const kind = preview?.kind || externalDropKind(file);
    if (!kind || kind === 'docx') {
        clearExternalDropPreview(state);
        await safeDotNet(state, 'ExternalFileDropFailed', kind === 'docx'
            ? 'DOCX drag and drop is not available yet. Drop a picture, video, text, or Markdown file.'
            : `The file '${file.name || 'file'}' is not a supported picture, video, text, or Markdown file.`);
        return true;
    }

    const assetId = crypto.randomUUID();
    try {
        preview?.overlay?.classList?.add('uploading');
        const message = preview?.overlay?.querySelector?.('.publisher-external-drop-message');
        if (message) message.textContent = `Importing ${file.name || kind}…`;
        const response = await fetch(`/api/assets/drop/${encodeURIComponent(assetId)}`, {
            method: 'POST',
            headers: { 'Content-Type': file.type || 'application/octet-stream', 'X-Publisher-File-Name': encodeURIComponent(file.name || '') },
            body: file
        });
        if (!response.ok) throw new Error((await response.text()) || `Upload failed with status ${response.status}.`);
        await safeDotNet(state, 'CompleteExternalFileDrop', assetId, kind, file.name || kind,
            file.type || 'application/octet-stream', file.size || 0,
            preview?.durationSeconds || 0, preview?.pixelWidth || 0, preview?.pixelHeight || 0,
            placement.x, placement.y);
    } catch (error) {
        await safeDotNet(state, 'ExternalFileDropFailed', error?.message || String(error));
    } finally {
        clearExternalDropPreview(state);
    }
    return true;
}

export function initializeCanvas(stageId, scrollId, pageId, horizontalRulerId, verticalRulerId, dotnet, config) {
    const stage = document.getElementById(stageId);
    const scroll = document.getElementById(scrollId);
    const page = document.getElementById(pageId);
    if (!stage || !scroll || !page || !stage.isConnected || !scroll.isConnected || !page.isConnected) return false;
    const normalizedConfig = {
        pxPerMm: Math.max(.0001, number(config?.pxPerMm, PX_PER_MM_AT_96_DPI)),
        cropMode: Boolean(config?.cropMode),
        unit: String(config?.unit || 'Millimeter'),
        rulersVisible: config?.rulersVisible !== false,
        guidesVisible: config?.guidesVisible !== false,
        snapToGrid: Boolean(config?.snapToGrid),
        snapToGuides: Boolean(config?.snapToGuides),
        snapToPage: Boolean(config?.snapToPage),
        snapToObjects: Boolean(config?.snapToObjects),
        snapInObjects: Boolean(config?.snapInObjects),
        gridSpacingMm: Math.max(.1, number(config?.gridSpacingMm, 2.5)),
        connectorTool: String(config?.connectorTool || 'None')
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
            handlers: {}
        };

        const handlers = state.handlers;
        handlers.stagePointerDown = event => pointerDown(state, event);
        handlers.stagePointerMove = event => pointerMove(state, event);
        handlers.stagePointerUp = event => pointerUp(state, event);
        handlers.stagePointerCancel = event => pointerCancel(state, event);
        handlers.lostPointerCapture = event => {
            if (state.operation?.pointerId === event.pointerId) resetPointerOperation(state, true);
        };
        handlers.windowPointerDown = event => {
            if (state.operation && !state.stage.contains(event.target)) resetPointerOperation(state, true);
        };
        handlers.windowPointerUp = event => pointerUp(state, event);
        handlers.windowPointerCancel = event => pointerCancel(state, event);
        handlers.windowBlur = () => resetPointerOperation(state, true);
        handlers.visibilityChange = () => {
            if (document.hidden) resetPointerOperation(state, true);
        };
        handlers.stagePointerLeave = () => {
            state.cursorX = null;
            state.cursorY = null;
            nextAnimationFrame(state);
        };
        handlers.stageWheel = event => cropWheel(state, event);
        handlers.stageKeyDown = event => canvasKeyDown(state, event);
        handlers.stageDragOver = event => insertionDragOver(state, event);
        handlers.stageDrop = event => insertionDrop(state, event);
        handlers.stageDragLeave = event => {
            if (!state.externalDropPreview) return;
            const next = event.relatedTarget;
            if (!next || !state.stage.contains(next)) clearExternalDropPreview(state);
        };
        handlers.documentDragStart = event => insertionDragStart(state, event);
        handlers.documentDragEnd = () => insertionDragEnd(state);
        handlers.documentClick = event => suppressInsertionClick(state, event);
        handlers.scroll = () => nextAnimationFrame(state);

        stage.addEventListener('pointerdown', handlers.stagePointerDown);
        stage.addEventListener('pointermove', handlers.stagePointerMove);
        stage.addEventListener('pointerup', handlers.stagePointerUp);
        stage.addEventListener('pointercancel', handlers.stagePointerCancel);
        stage.addEventListener('lostpointercapture', handlers.lostPointerCapture);
        window.addEventListener('pointerdown', handlers.windowPointerDown, true);
        window.addEventListener('pointerup', handlers.windowPointerUp, true);
        window.addEventListener('pointercancel', handlers.windowPointerCancel, true);
        window.addEventListener('blur', handlers.windowBlur);
        document.addEventListener('visibilitychange', handlers.visibilityChange);
        stage.addEventListener('pointerleave', handlers.stagePointerLeave);
        stage.addEventListener('wheel', handlers.stageWheel, { passive: false });
        stage.addEventListener('keydown', handlers.stageKeyDown);
        stage.addEventListener('dragover', handlers.stageDragOver);
        stage.addEventListener('drop', handlers.stageDrop);
        stage.addEventListener('dragleave', handlers.stageDragLeave);
        document.addEventListener('dragstart', handlers.documentDragStart, true);
        document.addEventListener('dragend', handlers.documentDragEnd, true);
        document.addEventListener('click', handlers.documentClick, true);
        scroll.addEventListener('scroll', handlers.scroll, { passive: true });

        if (typeof ResizeObserver === 'function') {
            state.resizeObserver = new ResizeObserver(() => nextAnimationFrame(state));
            state.resizeObserver.observe(stage);
            state.resizeObserver.observe(scroll);
        }
        canvasStates.set(stage, state);
    }

    if (state.page !== page && state.operation) resetPointerOperation(state, true);
    state.scroll = scroll;
    state.page = page;
    state.dotnet = dotnet;
    state.config = normalizedConfig;
    state.horizontalRuler = document.getElementById(horizontalRulerId);
    state.verticalRuler = document.getElementById(verticalRulerId);

    bindRuler(state.horizontalRuler, 'Horizontal', state);
    bindRuler(state.verticalRuler, 'Vertical', state);
    nextAnimationFrame(state);
    return true;
}

export function disposeCanvas(stageId) {
    const stage = document.getElementById(stageId);
    if (!stage) return;
    const state = canvasStates.get(stage);
    if (!state) return;

    resetPointerOperation(state, true);
    clearObjectAlignmentFeedback(state);
    clearInsertionDrag(state);
    clearExternalDropPreview(state);
    state.resizeObserver?.disconnect?.();
    for (const timer of state.cropTimers?.values?.() || []) clearTimeout(timer);
    state.cropTimers?.clear?.();

    const handlers = state.handlers || {};
    stage.removeEventListener('pointerdown', handlers.stagePointerDown);
    stage.removeEventListener('pointermove', handlers.stagePointerMove);
    stage.removeEventListener('pointerup', handlers.stagePointerUp);
    stage.removeEventListener('pointercancel', handlers.stagePointerCancel);
    stage.removeEventListener('lostpointercapture', handlers.lostPointerCapture);
    stage.removeEventListener('pointerleave', handlers.stagePointerLeave);
    stage.removeEventListener('wheel', handlers.stageWheel);
    stage.removeEventListener('keydown', handlers.stageKeyDown);
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
    window.removeEventListener('blur', handlers.windowBlur);
    document.removeEventListener('visibilitychange', handlers.visibilityChange);

    state.dotnet = null;
    canvasStates.delete(stage);
}

function bindRuler(canvas, orientation, state) {
    if (!canvas || boundRulers.has(canvas)) return;
    boundRulers.add(canvas);
    canvas.addEventListener('pointerdown', event => rulerPointerDown(state, orientation, canvas, event));
}

function rulerPointerDown(state, orientation, canvas, event) {
    if (event.button !== 0) return;
    state.rulerGuide = { orientation, pointerId: event.pointerId, canvas };
    canvas.setPointerCapture(event.pointerId);

    const move = moveEvent => {
        if (!state.rulerGuide || moveEvent.pointerId !== state.rulerGuide.pointerId) return;
        updateRulerGuidePreview(state, orientation, moveEvent);
    };
    const finish = upEvent => {
        if (!state.rulerGuide || upEvent.pointerId !== state.rulerGuide.pointerId) return;
        canvas.removeEventListener('pointermove', move);
        canvas.removeEventListener('pointerup', finish);
        canvas.removeEventListener('pointercancel', finish);
        finishRulerGuide(state, orientation, upEvent);
    };

    canvas.addEventListener('pointermove', move);
    canvas.addEventListener('pointerup', finish);
    canvas.addEventListener('pointercancel', finish);
    updateRulerGuidePreview(state, orientation, event);
    event.preventDefault();
}

function guidePositionFromPointer(state, orientation, event) {
    const pageRect = state.page.getBoundingClientRect();
    return orientation === 'Horizontal'
        ? (event.clientY - pageRect.top) / state.config.pxPerMm
        : (event.clientX - pageRect.left) / state.config.pxPerMm;
}

function updateRulerGuidePreview(state, orientation, event) {
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
}

function finishRulerGuide(state, orientation, event) {
    const position = guidePositionFromPointer(state, orientation, event);
    const max = orientation === 'Horizontal'
        ? number(state.page.dataset.pageHeightMm)
        : number(state.page.dataset.pageWidthMm);

    state.guidePreview?.remove();
    state.guidePreview = null;
    state.rulerGuide = null;
    if (position >= 0 && position <= max)
        state.dotnet.invokeMethodAsync('AddGuideAt', orientation, position);
}


function canvasKeyDown(state, event) {
    if (event.key !== 'Escape') return;
    clearConnectorOperation(state, true);
    safeDotNet(state, 'CancelActiveTool');
    event.preventDefault();
}

function connectorToolActive(state) {
    return state.config.connectorTool && state.config.connectorTool !== 'None';
}

function connectorPath(kind, source, target) {
    if (kind === 'Elbow') {
        const middleX = (source.x + target.x) / 2;
        return `M ${source.x} ${source.y} L ${middleX} ${source.y} L ${middleX} ${target.y} L ${target.x} ${target.y}`;
    }
    if (kind === 'Curved') {
        const dx = Math.max(12, Math.abs(target.x - source.x) * .48);
        const direction = target.x >= source.x ? 1 : -1;
        return `M ${source.x} ${source.y} C ${source.x + dx * direction} ${source.y}, ${target.x - dx * direction} ${target.y}, ${target.x} ${target.y}`;
    }
    return `M ${source.x} ${source.y} L ${target.x} ${target.y}`;
}

function portPointMm(state, port) {
    const pageRect = state.page.getBoundingClientRect();
    const rect = port.getBoundingClientRect();
    return {
        x: (rect.left + rect.width / 2 - pageRect.left) / state.config.pxPerMm,
        y: (rect.top + rect.height / 2 - pageRect.top) / state.config.pxPerMm
    };
}

function findConnectorTarget(state, event, excludedIds = []) {
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
                point: portPointMm(state, port)
            };
        }
    }
    return best;
}

function ensureConnectorGhost(state, markerEnd) {
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
}

function showConnectorGhost(state, operation, target) {
    const ghost = ensureConnectorGhost(state, operation.markerEnd);
    ghost.path.setAttribute('d', connectorPath(operation.pathKind || 'Curved', operation.fixedPoint, target.point));
    ghost.svg.classList.add('visible');
    operation.target = target;
}

function hideConnectorGhost(state) {
    state.connectorGhost?.svg.classList.remove('visible');
}

function clearConnectorOperation(state, restoreOriginal) {
    const operation = state.operation;
    if (operation?.kind === 'connector-reconnect' && operation.connector && restoreOriginal)
        operation.connector.style.visibility = '';
    if (state.connectorGhost) {
        state.connectorGhost.svg.remove();
        state.connectorGhost = null;
    }
    if (operation?.target?.port) operation.target.port.classList.remove('connector-port-target');
    if (operation?.sourcePort) operation.sourcePort.classList.remove('connector-port-source');
    if (operation?.kind?.startsWith('connector-')) state.operation = null;
}

function updateConnectorDrag(state, event, operation) {
    operation.target?.port?.classList.remove('connector-port-target');
    const target = findConnectorTarget(state, event, operation.excludedIds);
    if (!target) {
        operation.target = null;
        hideConnectorGhost(state);
        return;
    }
    target.port.classList.add('connector-port-target');
    showConnectorGhost(state, operation, target);
}

function finishConnectorDrag(state, operation) {
    const target = operation.target;
    if (target) {
        if (operation.kind === 'connector-new') {
            safeDotNet(state,
                'CommitConnector',
                operation.sourceOwnerId, operation.sourceAnchor,
                target.ownerId, target.anchor, operation.tool);
        } else {
            safeDotNet(state,
                'ReconnectConnector', operation.connectorId, operation.endpoint, target.ownerId, target.anchor);
        }
    }
    clearConnectorOperation(state, true);
}

function parseRotation(element) {
    const match = /rotate\(([-+0-9.]+)deg\)/i.exec(element.style.transform || '');
    return match ? number(match[1]) : 0;
}

function anchorPointForElement(element, anchor, pxPerMm) {
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
}

function updateAttachedConnectors(state, movedId) {
    for (const connector of state.page.querySelectorAll('[data-connector-id]')) {
        if (connector.dataset.sourceElementId !== movedId && connector.dataset.targetElementId !== movedId) continue;
        const sourceElement = state.page.querySelector(`[data-element-id="${CSS.escape(connector.dataset.sourceElementId)}"]`);
        const targetElement = state.page.querySelector(`[data-element-id="${CSS.escape(connector.dataset.targetElementId)}"]`);
        if (!sourceElement || !targetElement) continue;
        const source = anchorPointForElement(sourceElement, connector.dataset.sourceAnchor, state.config.pxPerMm);
        const target = anchorPointForElement(targetElement, connector.dataset.targetAnchor, state.config.pxPerMm);
        const path = connectorPath(connector.dataset.pathKind || 'Curved', source, target);
        connector.querySelectorAll('.connector-line,.connector-hit').forEach(item => item.setAttribute('d', path));
        const ends = connector.querySelectorAll('.connector-endpoint');
        if (ends[0]) { ends[0].setAttribute('cx', source.x); ends[0].setAttribute('cy', source.y); }
        if (ends[1]) { ends[1].setAttribute('cx', target.x); ends[1].setAttribute('cy', target.y); }
    }
}

function mediaPointerTargetsControls(event) {
    const target = event.target?.closest?.('[data-media-control],video,audio');
    if (!target) return false;
    const rect = target.getBoundingClientRect?.();
    if (!rect || rect.height <= 0) return true;
    const tag = String(target.tagName || '').toLowerCase();
    const relativeY = event.clientY - rect.top;
    const controlBand = tag === 'audio' ? rect.height : Math.min(38, rect.height * .3);
    return relativeY >= rect.height - controlBand;
}

function selectionNodes(state) {
    return [...state.page.querySelectorAll('[data-publication-element][data-element-id]:not([data-connector-id])')];
}

function selectionUnitNodes(state, element) {
    const groupId = String(element.dataset.groupId || '').trim();
    if (!groupId) return [element];
    return selectionNodes(state).filter(item => item.dataset.groupId === groupId);
}

function movingNodesForPointer(state, element, additive, wasSelected) {
    const selected = selectionNodes(state).filter(item => item.dataset.selected === 'true' || item.classList.contains('selected'));
    const unit = selectionUnitNodes(state, element);
    if (!additive) return wasSelected && selected.length ? selected : unit;
    if (wasSelected) return selected.length ? selected : unit;
    const result = [...selected];
    for (const item of unit) if (!result.includes(item)) result.push(item);
    return result;
}

function movingBounds(items) {
    if (!items.length) return { x: 0, y: 0, width: 0, height: 0 };
    const left = Math.min(...items.map(item => item.x));
    const top = Math.min(...items.map(item => item.y));
    const right = Math.max(...items.map(item => item.x + item.width));
    const bottom = Math.max(...items.map(item => item.y + item.height));
    return { x: left, y: top, width: right - left, height: bottom - top };
}

function refreshMovingElements(state, operation) {
    if (!operation?.moving) return;
    for (const item of operation.moving) {
        const current = state.page.querySelector(`[data-element-id="${CSS.escape(item.id)}"]`);
        if (current) item.element = current;
    }
}

function registerCanvasClick(state, operation, event) {
    if (!operation?.id || operation.kind === 'resize' || operation.kind?.startsWith('connector-')) return;
    const now = performance.now();
    const previous = state.lastCanvasClick;
    const sameElement = previous?.id === operation.id;
    const closeInTime = previous && now - previous.time <= 520;
    const closeInSpace = previous && Math.hypot(event.clientX - previous.x, event.clientY - previous.y) <= 10;
    if (sameElement && closeInTime && closeInSpace) {
        state.lastCanvasClick = null;
        resetPointerOperation(state, false);
        safeDotNet(state, 'ActivateElement', operation.id);
        event.preventDefault();
        event.stopPropagation();
        return;
    }
    state.lastCanvasClick = { id: operation.id, time: now, x: event.clientX, y: event.clientY };
}

function pointerDown(state, event) {
    if (event.button !== 0 || event.target.closest('.ruler-canvas,.corner-ruler')) return;
    if (state.operation) resetPointerOperation(state, true);
    if (mediaPointerTargetsControls(event)) return;
    state.stage.focus({ preventScroll: true });

    const insertionRect = state.page.getBoundingClientRect();
    if (event.clientX >= insertionRect.left && event.clientX <= insertionRect.right &&
        event.clientY >= insertionRect.top && event.clientY <= insertionRect.bottom) {
        const insertionX = clamp((event.clientX - insertionRect.left) / state.config.pxPerMm, 0, number(state.page.dataset.pageWidthMm));
        const insertionY = clamp((event.clientY - insertionRect.top) / state.config.pxPerMm, 0, number(state.page.dataset.pageHeightMm));
        safeDotNet(state, 'SetInsertionPoint', insertionX, insertionY);
    }

    const endpoint = event.target.closest('[data-connector-end]');
    if (endpoint && state.page.contains(endpoint)) {
        const connector = endpoint.closest('[data-connector-id]');
        if (!connector || connector.classList.contains('locked')) return;
        const endpointName = endpoint.dataset.connectorEnd;
        const otherId = endpointName === 'source' ? connector.dataset.targetElementId : connector.dataset.sourceElementId;
        const fixedElement = state.page.querySelector(`[data-element-id="${CSS.escape(otherId)}"]`);
        const fixedAnchor = endpointName === 'source' ? connector.dataset.targetAnchor : connector.dataset.sourceAnchor;
        if (!fixedElement) return;
        connector.style.visibility = 'hidden';
        state.operation = {
            kind: 'connector-reconnect', pointerId: event.pointerId, connector, connectorId: connector.dataset.connectorId,
            endpoint: endpointName, fixedPoint: anchorPointForElement(fixedElement, fixedAnchor, state.config.pxPerMm),
            pathKind: connector.dataset.pathKind || 'Curved', markerEnd: endpointName !== 'source', excludedIds: [otherId]
        };
        try { state.stage.setPointerCapture(event.pointerId); } catch { }
        event.preventDefault();
        event.stopPropagation();
        return;
    }

    const connectorPort = event.target.closest('[data-connector-port]');
    if (connectorPort && state.page.contains(connectorPort) && connectorToolActive(state)) {
        state.lastCanvasClick = null;
        const sourceOwnerId = connectorPort.dataset.ownerId;
        connectorPort.classList.add('connector-port-source');
        state.operation = {
            kind: 'connector-new', pointerId: event.pointerId, sourcePort: connectorPort,
            sourceOwnerId, sourceAnchor: connectorPort.dataset.anchor, fixedPoint: portPointMm(state, connectorPort),
            pathKind: 'Curved', markerEnd: state.config.connectorTool === 'Arrow', tool: state.config.connectorTool,
            excludedIds: [sourceOwnerId]
        };
        try { state.stage.setPointerCapture(event.pointerId); } catch { }
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

    const element = event.target.closest('[data-publication-element]');
    if (!element || !state.page.contains(element)) {
        if (state.page.contains(event.target) || state.scroll.contains(event.target)) {
            state.lastCanvasClick = null;
            if (!connectorToolActive(state)) safeDotNet(state, 'ClearSelectionFromCanvas');
            event.preventDefault();
        }
        return;
    }

    const id = element.dataset.elementId;
    const wasSelected = element.dataset.selected === 'true' || element.classList.contains('selected');
    const additive = Boolean(event.ctrlKey || event.metaKey || event.shiftKey);
    const activeConnectorTool = connectorToolActive(state);
    if (activeConnectorTool) {
        // Connector mode owns the canvas until the user completes it or presses Esc.
        // A slightly missed port must not silently switch back to selection mode.
        event.preventDefault();
        return;
    }
    const pendingToggle = additive && wasSelected;
    if (!pendingToggle && (!wasSelected || additive)) safeDotNet(state, 'SelectElement', id, additive);
    if (element.classList.contains('locked')) return;
    if (element.matches('[data-connector-id]')) return;

    const handle = event.target.closest('[data-resize-handle]');
    const image = element.querySelector('img');
    const bounds = elementMm(element, state.config.pxPerMm);
    const moving = movingNodesForPointer(state, element, additive, wasSelected)
        .filter(item => !item.classList.contains('locked') && !item.matches('[data-connector-id]'))
        .map(item => ({ id: item.dataset.elementId, element: item, ...elementMm(item, state.config.pxPerMm) }));
    if (!moving.some(item => item.id === id)) moving.unshift({ id, element, ...bounds });
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
        movingIds: new Set(moving.map(item => item.id)),
        movingBounds: groupBounds,
        grabGroupX: groupBounds.width > 0 ? clamp((pointerX - groupBounds.x) / groupBounds.width, 0, 1) : .5,
        grabGroupY: groupBounds.height > 0 ? clamp((pointerY - groupBounds.y) / groupBounds.height, 0, 1) : .5,
        ...bounds
    };

    if (state.config.cropMode && image && !handle) {
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
        state.operation = { ...base, kind: 'resize', moving: [{ id, element, ...bounds }], movingIds: new Set([id]), handle: handle.dataset.resizeHandle };
    } else {
        state.operation = { ...base, kind: 'move' };
    }

    try { state.stage.setPointerCapture(event.pointerId); } catch { }
    if (handle || state.config.cropMode) event.preventDefault();
}

function refreshOperationElement(state, operation) {
    if (!operation?.id) return operation?.element || null;
    const current = state.page.querySelector(`[data-element-id="${CSS.escape(operation.id)}"]`);
    if (current) {
        operation.element = current;
        if (operation.kind === 'crop') operation.image = current.querySelector('img') || operation.image;
    }
    return operation.element;
}

function pointerMove(state, event) {
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

    if (operation.kind === 'connector-new' || operation.kind === 'connector-reconnect') {
        state.lastCanvasClick = null;
        updateConnectorDrag(state, event, operation);
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
    if (!operation.moved && movementPixels < (operation.kind === 'resize' ? 1.5 : 3)) return;
    operation.moved = true;
    const dx = (event.clientX - operation.startX) / state.config.pxPerMm;
    const dy = (event.clientY - operation.startY) / state.config.pxPerMm;

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
        ? [...state.page.querySelectorAll('.guide-line.vertical:not(.guide-preview)')].map(line => number(line.style.left) / state.config.pxPerMm)
        : [];
    const horizontalGuides = state.config.snapToGuides
        ? [...state.page.querySelectorAll('.guide-line.horizontal:not(.guide-preview)')].map(line => number(line.style.top) / state.config.pxPerMm)
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
        event.preventDefault();
        return;
    }

    const handle = operation.handle;
    if (handle.includes('e')) width = Math.max(2, snapSize(operation.width + dx, state.config));
    if (handle.includes('s')) height = Math.max(2, snapSize(operation.height + dy, state.config));
    if (handle.includes('w')) {
        x = snapCoordinate(operation.x + dx, verticalGuides, state.config);
        width = Math.max(2, operation.width - (x - operation.x));
    }
    if (handle.includes('n')) {
        y = snapCoordinate(operation.y + dy, horizontalGuides, state.config);
        height = Math.max(2, operation.height - (y - operation.y));
    }

    operation.current = { x, y, width, height };
    const operationElement = refreshOperationElement(state, operation);
    if (!operationElement) return;
    operationElement.style.left = `${x * state.config.pxPerMm}px`;
    operationElement.style.top = `${y * state.config.pxPerMm}px`;
    operationElement.style.width = `${width * state.config.pxPerMm}px`;
    operationElement.style.height = `${height * state.config.pxPerMm}px`;
    updateAttachedConnectors(state, operation.id);
    event.preventDefault();
}

function snapAxis(value, size, pageSize, guides, config) {
    let result = value;
    if (config.snapToGrid && config.gridSpacingMm > 0)
        result = Math.round(result / config.gridSpacingMm) * config.gridSpacingMm;

    const candidates = [];
    if (config.snapToPage) candidates.push(0, pageSize / 2 - size / 2, pageSize - size);
    if (config.snapToGuides) {
        for (const guide of guides) candidates.push(guide, guide - size / 2, guide - size);
    }
    return nearestCandidate(result, candidates, 6 / config.pxPerMm);
}

function snapCoordinate(value, guides, config) {
    let result = value;
    if (config.snapToGrid && config.gridSpacingMm > 0)
        result = Math.round(result / config.gridSpacingMm) * config.gridSpacingMm;
    return config.snapToGuides ? nearestCandidate(result, guides, 6 / config.pxPerMm) : result;
}

function snapSize(value, config) {
    if (!config.snapToGrid || config.gridSpacingMm <= 0) return value;
    return Math.round(value / config.gridSpacingMm) * config.gridSpacingMm;
}

function nearestCandidate(value, candidates, tolerance) {
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
}

function pointerUp(state, event) {
    const operation = state.operation;
    if (!operation || operation.pointerId !== event.pointerId) return;
    state.operation = null;
    clearObjectAlignmentFeedback(state);
    try { state.stage.releasePointerCapture(event.pointerId); } catch { }

    if (operation.kind === 'connector-new' || operation.kind === 'connector-reconnect') {
        state.lastCanvasClick = null;
        // Pointerup can be the first event that reaches the destination port during
        // a fast drag, so resolve the target once more before committing.
        updateConnectorDrag(state, event, operation);
        state.operation = operation;
        finishConnectorDrag(state, operation);
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
            if (operation.pendingToggle) safeDotNet(state, 'SelectElement', operation.id, true);
            state.lastCanvasClick = null;
            return;
        }
        if (operation.wasSelected && (operation.moving?.length || 0) > 1)
            safeDotNet(state, 'SelectElement', operation.id, false);
        registerCanvasClick(state, operation, event);
        return;
    }
    state.lastCanvasClick = null;
    if (operation.kind === 'crop') {
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
            requestAnimationFrame(() => window.dispatchEvent(new Event('resize')));
            setTimeout(() => window.dispatchEvent(new Event('resize')), 120);
        }
    }
}

function pointerCancel(state, event) {
    if (state.operation?.pointerId !== event.pointerId) return;
    resetPointerOperation(state, true);
}

function cropWheel(state, event) {
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
    state.cropTimers.set(id, setTimeout(() => {
        state.cropTimers.delete(id);
        safeDotNet(state, 'CommitCrop', id, cropX, cropY, nextScale);
    }, 140));
}

function applyImageTransform(image, cropX, cropY, scale, rotation, flipX, flipY) {
    image.dataset.cropX = String(cropX);
    image.dataset.cropY = String(cropY);
    image.dataset.cropScale = String(scale);
    image.style.transform = `translate(${cropX}%, ${cropY}%) rotate(${rotation}deg) scale(${scale * flipX}, ${scale * flipY})`;
}

function drawRulers(state) {
    if (!state.config.rulersVisible || !state.horizontalRuler || !state.verticalRuler) return;
    drawRuler(state, state.horizontalRuler, true);
    drawRuler(state, state.verticalRuler, false);
}

function unitDefinition(unit) {
    switch (unit) {
        case 'Centimeter': return { mmPerUnit: 10, suffix: 'cm' };
        case 'Inch': return { mmPerUnit: 25.4, suffix: 'in' };
        case 'Pixel': return { mmPerUnit: 25.4 / 96, suffix: 'px' };
        default: return { mmPerUnit: 1, suffix: 'mm' };
    }
}

function niceStep(minimum) {
    if (!Number.isFinite(minimum) || minimum <= 0) return 1;
    const power = Math.pow(10, Math.floor(Math.log10(minimum)));
    const normalized = minimum / power;
    const factor = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10;
    return factor * power;
}

function configureCanvas(canvas) {
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
}

function drawRuler(state, canvas, horizontal) {
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
}

export function calculateFitZoom(stageId, widthMm, heightMm, rulersVisible) {
    const stage = document.getElementById(stageId);
    if (!stage) return .8;
    const ruler = rulersVisible ? 28 : 0;
    const availableWidth = Math.max(100, stage.clientWidth - ruler - 84);
    const availableHeight = Math.max(100, stage.clientHeight - ruler - 84);
    const zoom = Math.min(
        availableWidth / (widthMm * PX_PER_MM_AT_96_DPI),
        availableHeight / (heightMm * PX_PER_MM_AT_96_DPI));
    return clamp(zoom, .2, 4);
}

function collectExportCss() {
    let css = '';
    for (const sheet of document.styleSheets) {
        try {
            for (const rule of sheet.cssRules) {
                if (rule.type === CSSRule.PAGE_RULE) continue;
                css += `${rule.cssText}\n`;
            }
        } catch {
            // Cross-origin component styles are not required for publication page export.
        }
    }
    return css;
}

function waitForImages(root) {
    return Promise.all([...root.querySelectorAll('img')].map(async image => {
        if (image.complete && image.naturalWidth > 0) return;
        try {
            if (typeof image.decode === 'function') await image.decode();
            else await new Promise((resolve, reject) => {
                image.addEventListener('load', resolve, { once: true });
                image.addEventListener('error', reject, { once: true });
            });
        } catch {
            throw new Error(`Picture '${image.alt || image.src.slice(0, 48)}' could not be decoded for export.`);
        }
    }));
}

let cssColorProbeContext = null;

function cssColorFunctionToRgba(value) {
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
}

function normalizeCssColorFunctions(value) {
    if (!value || !/(?:^|\W)(?:color|lab|lch|oklab|oklch)\(/i.test(value)) return value;
    return String(value).replace(/(?:color|lab|lch|oklab|oklch)\([^()]*\)/gi, match => cssColorFunctionToRgba(match));
}

function sanitizeInlineColorFunctions(root) {
    const elements = [root, ...root.querySelectorAll('*')];
    for (const element of elements) {
        const style = element.getAttribute?.('style');
        if (style) element.setAttribute('style', normalizeCssColorFunctions(style));
        for (const attribute of ['fill', 'stroke', 'color', 'flood-color', 'stop-color']) {
            const value = element.getAttribute?.(attribute);
            if (value) element.setAttribute(attribute, normalizeCssColorFunctions(value));
        }
    }
}

function copyComputedStyles(source, clone) {
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
}

function cleanPageClone(page) {
    const clone = page.cloneNode(true);
    copyComputedStyles(page, clone);
    clone.removeAttribute('id');
    clone.classList.remove('crop-mode');
    clone.style.margin = '0';
    clone.style.boxShadow = 'none';
    clone.style.backgroundImage = 'none';
    clone.querySelectorAll('.selection-handle,.guide-line,.crop-thirds,.crop-help,.connector-port,.connector-endpoint,.connector-hit,.connector-ghost').forEach(item => item.remove());
    clone.querySelectorAll('.selected').forEach(item => {
        item.classList.remove('selected');
        item.style.outline = 'none';
    });
    sanitizeInlineColorFunctions(clone);
    return clone;
}

function normalizeObjectFitImages(root) {
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
}

function pageExportMetrics(page) {
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
}

function canonicalizePageClone(clone, metrics) {
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
}

function normalizePublicationPageSizes(publication) {
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
}

function waitForVideoFrame(video, timeoutMs = 8000) {
    if (video.readyState >= 2 && video.videoWidth > 0 && video.videoHeight > 0) return Promise.resolve();
    return new Promise((resolve, reject) => {
        const timer = setTimeout(() => finish(new Error('Video frame loading timed out.')), timeoutMs);
        const finish = error => {
            clearTimeout(timer);
            video.removeEventListener('loadeddata', loaded);
            video.removeEventListener('error', failed);
            error ? reject(error) : resolve();
        };
        const loaded = () => finish();
        const failed = () => finish(new Error('The video frame could not be decoded.'));
        video.addEventListener('loadeddata', loaded, { once: true });
        video.addEventListener('error', failed, { once: true });
        video.load();
    });
}

function drawVideoFrameDataUrl(video) {
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
}

async function snapshotVideoForRaster(video, owner) {
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
            await new Promise(resolve => {
                const timer = setTimeout(done, 3500);
                function done() {
                    clearTimeout(timer);
                    temporary.removeEventListener('seeked', done);
                    resolve();
                }
                temporary.addEventListener('seeked', done, { once: true });
                temporary.currentTime = target;
            });
        }
        return drawVideoFrameDataUrl(temporary) || poster;
    } catch {
        return poster;
    } finally {
        temporary.pause();
        temporary.removeAttribute('src');
        temporary.load();
    }
}

async function freezeMediaForRaster(sourcePage, clonePage) {
    const sourceVideos = [...sourcePage.querySelectorAll('video')];
    const cloneVideos = [...clonePage.querySelectorAll('video')];
    for (let index = 0; index < cloneVideos.length; index++) {
        const cloneVideo = cloneVideos[index];
        const sourceVideo = sourceVideos[index];
        const sourceOwner = sourceVideo?.closest?.('[data-media-kind]');
        const snapshot = await snapshotVideoForRaster(sourceVideo, sourceOwner);
        const image = document.createElement('img');
        image.alt = sourceVideo?.getAttribute('aria-label') || sourceVideo?.getAttribute('title') || 'Frozen video frame';
        image.draggable = false;
        image.style.cssText = cloneVideo.getAttribute('style') || 'width:100%;height:100%;object-fit:contain;';
        image.style.display = 'block';
        image.style.width = '100%';
        image.style.height = '100%';
        image.style.maxWidth = 'none';
        image.src = snapshot || 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent('<svg xmlns="http://www.w3.org/2000/svg" width="640" height="360"><rect width="100%" height="100%" fill="#111827"/><text x="50%" y="50%" fill="#e5e7eb" font-family="Segoe UI,Arial" font-size="26" text-anchor="middle" dominant-baseline="middle">Video frame unavailable</text></svg>');
        cloneVideo.replaceWith(image);
    }
    clonePage.querySelectorAll('audio').forEach(audio => audio.remove());
    clonePage.querySelectorAll('.media-object-badge').forEach(badge => badge.remove());
}

function blobAsDataUrl(blob) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(String(reader.result || ''));
        reader.onerror = () => reject(reader.error || new Error('The media asset could not be embedded.'));
        reader.readAsDataURL(blob);
    });
}

async function inlineLocalMediaSources(root) {
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
}

async function pageSvg(page, options = {}) {
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
}

async function loadSvgImage(svgText) {
    const attempts = [];
    const blob = new Blob([svgText], { type: 'image/svg+xml;charset=utf-8' });

    // Chromium's ImageBitmap path is the most reliable way to rasterize an SVG that contains
    // XHTML foreignObject content. Keep two Image fallbacks for browsers that reject it.
    if (typeof createImageBitmap === 'function') {
        try {
            const bitmap = await createImageBitmap(blob);
            return { image: bitmap, cleanup: () => bitmap.close() };
        } catch {
            // Continue with object/data URL fallbacks.
        }
    }

    const objectUrl = URL.createObjectURL(blob);
    attempts.push({ url: objectUrl, revoke: () => URL.revokeObjectURL(objectUrl) });
    attempts.push({ url: `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svgText)}`, revoke: null });

    let lastError = null;
    for (const attempt of attempts) {
        const image = new Image();
        image.decoding = 'sync';
        try {
            await new Promise((resolve, reject) => {
                const timer = setTimeout(() => reject(new Error('SVG rasterization timed out.')), 15000);
                image.onload = () => { clearTimeout(timer); resolve(); };
                image.onerror = () => { clearTimeout(timer); reject(new Error('The browser could not render the SVG export surface.')); };
                image.src = attempt.url;
            });
            return { image, cleanup: attempt.revoke };
        } catch (error) {
            lastError = error;
            if (attempt.revoke) attempt.revoke();
        }
    }
    throw lastError || new Error('The browser could not prepare the publication for raster export.');
}

function canvasLooksBlank(canvas) {
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
}

function cappedRasterScale(width, height, requestedScale) {
    const scale = Math.max(.1, number(requestedScale, 1));
    const requestedPixels = Math.max(1, width * scale) * Math.max(1, height * scale);
    const maxPixels = 80_000_000;
    return requestedPixels > maxPixels ? scale * Math.sqrt(maxPixels / requestedPixels) : scale;
}

async function rasterizePageElement(page, scale) {
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
            await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
            const options = {
                backgroundColor: null, scale: effectiveScale, logging: false, useCORS: true, allowTaint: false,
                imageTimeout: 20000, removeContainer: true, width: metrics.width, height: metrics.height,
                windowWidth: Math.max(document.documentElement.clientWidth, Math.ceil(metrics.width)),
                windowHeight: Math.max(document.documentElement.clientHeight, Math.ceil(metrics.height)), scrollX: 0, scrollY: 0,
                onclone: documentClone => {
                    const root = documentClone.querySelector(`[data-publisher-raster-root="${rasterId}"]`);
                    if (root) sanitizeInlineColorFunctions(root);
                }
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
}

function prepareOutputCanvas(canvas, jpeg) {
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
}

function canvasToEmbeddedSvg(canvas, widthMm = 0, heightMm = 0) {
    const dataUrl = canvas.toDataURL('image/png');
    const width = Math.max(1, canvas.width);
    const height = Math.max(1, canvas.height);
    const widthAttribute = widthMm > 0 ? `${widthMm}mm` : String(width);
    const heightAttribute = heightMm > 0 ? `${heightMm}mm` : String(height);
    return `<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="${widthAttribute}" height="${heightAttribute}" viewBox="0 0 ${width} ${height}" preserveAspectRatio="xMidYMid meet"><image x="0" y="0" width="${width}" height="${height}" href="${dataUrl}" xlink:href="${dataUrl}"/></svg>`;
}

async function rasterizeIsolatedPublicationElement(page, element, scale) {
    const hidden = [];
    const pageStyle = page.getAttribute('style');
    for (const node of page.querySelectorAll('[data-publication-element]')) {
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
}

function cropCanvasToElement(canvas, page, element, paddingPixels = 2) {
    const pageRect = page.getBoundingClientRect();
    const elementRect = element.getBoundingClientRect();
    if (pageRect.width <= 0 || pageRect.height <= 0 || elementRect.width <= 0 || elementRect.height <= 0)
        throw new Error('The selected object has no measurable export area.');
    const scaleX = canvas.width / pageRect.width;
    const scaleY = canvas.height / pageRect.height;
    const padding = Math.max(0, Math.round(paddingPixels));
    const left = Math.max(0, Math.floor((elementRect.left - pageRect.left) * scaleX) - padding);
    const top = Math.max(0, Math.floor((elementRect.top - pageRect.top) * scaleY) - padding);
    const right = Math.min(canvas.width, Math.ceil((elementRect.right - pageRect.left) * scaleX) + padding);
    const bottom = Math.min(canvas.height, Math.ceil((elementRect.bottom - pageRect.top) * scaleY) + padding);
    if (right <= left || bottom <= top) throw new Error('The selected object is outside the page export area.');
    const output = document.createElement('canvas');
    output.width = right - left;
    output.height = bottom - top;
    const context = output.getContext('2d');
    if (!context) throw new Error('The browser did not provide an object export canvas.');
    context.drawImage(canvas, left, top, output.width, output.height, 0, 0, output.width, output.height);
    return output;
}

async function svgToCanvas(svgText, width, height, scale, jpeg) {
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
}

async function canvasBlob(canvas, mimeType, quality) {
    const blob = await new Promise(resolve => canvas.toBlob(resolve, mimeType, quality));
    if (blob) return blob;
    try {
        const dataUrl = canvas.toDataURL(mimeType, quality);
        const response = await fetch(dataUrl);
        return await response.blob();
    } catch {
        throw new Error('The browser could not create the raster image. Try SVG export or a lower DPI.');
    }
}

function downloadBlob(fileName, blob) {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1500);
}

let zipCrcTable;
function crc32(bytes) {
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
}

function dosDateTime(date = new Date()) {
    const year = Math.max(1980, date.getFullYear());
    return {
        time: ((date.getHours() & 31) << 11) | ((date.getMinutes() & 63) << 5) | ((Math.floor(date.getSeconds() / 2)) & 31),
        date: (((year - 1980) & 127) << 9) | (((date.getMonth() + 1) & 15) << 5) | (date.getDate() & 31)
    };
}

async function createStoredZip(files) {
    const encoder = new TextEncoder();
    const localParts = [];
    const centralParts = [];
    let offset = 0;
    for (const file of files) {
        const name = encoder.encode(file.name);
        const bytes = new Uint8Array(await file.blob.arrayBuffer());
        const crc = crc32(bytes);
        const stamp = dosDateTime(file.modified || new Date());
        const local = new Uint8Array(30 + name.length);
        const localView = new DataView(local.buffer);
        localView.setUint32(0, 0x04034b50, true);
        localView.setUint16(4, 20, true);
        localView.setUint16(6, 0x0800, true);
        localView.setUint16(8, 0, true);
        localView.setUint16(10, stamp.time, true);
        localView.setUint16(12, stamp.date, true);
        localView.setUint32(14, crc, true);
        localView.setUint32(18, bytes.length, true);
        localView.setUint32(22, bytes.length, true);
        localView.setUint16(26, name.length, true);
        localView.setUint16(28, 0, true);
        local.set(name, 30);
        localParts.push(local, bytes);

        const central = new Uint8Array(46 + name.length);
        const centralView = new DataView(central.buffer);
        centralView.setUint32(0, 0x02014b50, true);
        centralView.setUint16(4, 20, true);
        centralView.setUint16(6, 20, true);
        centralView.setUint16(8, 0x0800, true);
        centralView.setUint16(10, 0, true);
        centralView.setUint16(12, stamp.time, true);
        centralView.setUint16(14, stamp.date, true);
        centralView.setUint32(16, crc, true);
        centralView.setUint32(20, bytes.length, true);
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
        offset += local.length + bytes.length;
    }
    const centralOffset = offset;
    const centralSize = centralParts.reduce((sum, part) => sum + part.length, 0);
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
}


function escapeHtml(value) {
    return String(value ?? '')
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;');
}

function parseHexColor(value) {
    const text = String(value || '#ffffff').trim().replace('#', '');
    const normalized = text.length === 3 ? [...text].map(x => x + x).join('') : text.padEnd(6, 'f').slice(0, 6);
    return {
        r: Number.parseInt(normalized.slice(0, 2), 16),
        g: Number.parseInt(normalized.slice(2, 4), 16),
        b: Number.parseInt(normalized.slice(4, 6), 16)
    };
}

async function imageFromDataUrl(dataUrl) {
    const image = new Image();
    image.decoding = 'async';
    await new Promise((resolve, reject) => {
        image.onload = resolve;
        image.onerror = () => reject(new Error('The selected picture could not be decoded.'));
        image.src = dataUrl;
    });
    return image;
}

const workspaceStates = new WeakMap();

function setWorkspaceColumns(workspace, state) {
    workspace.style.setProperty('--pages-pane-width', state.leftCollapsed ? '0px' : `${state.left}px`);
    workspace.style.setProperty('--inspector-pane-width', state.rightCollapsed ? '0px' : `${state.right}px`);
    workspace.classList.toggle('pages-collapsed', state.leftCollapsed);
    workspace.classList.toggle('inspector-collapsed', state.rightCollapsed);
    localStorage.setItem('blazorPublisher.workspace', JSON.stringify(state));
    window.dispatchEvent(new Event('resize'));
}

function createWorkspaceState(workspace) {
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
}

function bindWorkspaceSplitter(workspace, splitter, side) {
    if (!splitter || splitter.dataset.bound === 'true') return;
    splitter.dataset.bound = 'true';
    splitter.addEventListener('dblclick', () => window.publisherStudio.toggleWorkspacePane(workspace.id, side));
    splitter.addEventListener('pointerdown', event => {
        if (event.button !== 0) return;
        const state = workspaceStates.get(workspace) || createWorkspaceState(workspace);
        const startX = event.clientX;
        const initial = side === 'left' ? state.left : state.right;
        splitter.classList.add('dragging');
        splitter.setPointerCapture(event.pointerId);
        const move = moveEvent => {
            const delta = moveEvent.clientX - startX;
            if (side === 'left') {
                state.leftCollapsed = false;
                state.left = clamp(initial + delta, 120, Math.max(120, workspace.clientWidth * .42));
            } else {
                state.rightCollapsed = false;
                state.right = clamp(initial - delta, 220, Math.max(220, workspace.clientWidth * .48));
            }
            setWorkspaceColumns(workspace, state);
        };
        const up = upEvent => {
            splitter.classList.remove('dragging');
            splitter.removeEventListener('pointermove', move);
            splitter.removeEventListener('pointerup', up);
            splitter.removeEventListener('pointercancel', up);
            try { splitter.releasePointerCapture(upEvent.pointerId); } catch { }
        };
        splitter.addEventListener('pointermove', move);
        splitter.addEventListener('pointerup', up);
        splitter.addEventListener('pointercancel', up);
        event.preventDefault();
    });
}


function normalizeWordArtPoints(points) {
    const normalized = Array.isArray(points)
        ? points
            .map(point => ({ x: clamp(number(point?.x ?? point?.X), 0, 1000), y: clamp(number(point?.y ?? point?.Y), 0, 300) }))
            .filter(point => Number.isFinite(point.x) && Number.isFinite(point.y))
            .slice(0, 32)
        : [];
    return normalized.length >= 2 ? normalized : [{ x: 60, y: 150 }, { x: 940, y: 150 }];
}

function wordArtPathFromPoints(points) {
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
}

function wordArtEditorPoint(svg, event) {
    const matrix = svg.getScreenCTM();
    if (!matrix) return { x: 0, y: 0 };
    const point = new DOMPoint(event.clientX, event.clientY).matrixTransform(matrix.inverse());
    return { x: clamp(point.x, 0, 1000), y: clamp(point.y, 0, 300) };
}

function wordArtDistance(left, right) {
    return Math.hypot(left.x - right.x, left.y - right.y);
}

function perpendicularDistance(point, start, end) {
    const dx = end.x - start.x;
    const dy = end.y - start.y;
    if (dx === 0 && dy === 0) return wordArtDistance(point, start);
    const t = clamp(((point.x - start.x) * dx + (point.y - start.y) * dy) / (dx * dx + dy * dy), 0, 1);
    return wordArtDistance(point, { x: start.x + t * dx, y: start.y + t * dy });
}

function simplifyWordArtPoints(points, tolerance = 8) {
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
}

function limitWordArtPoints(points, maximum = 18) {
    if (points.length <= maximum) return points;
    const result = [points[0]];
    for (let index = 1; index < maximum - 1; index++) {
        const sourceIndex = Math.round(index * (points.length - 1) / (maximum - 1));
        result.push(points[sourceIndex]);
    }
    result.push(points[points.length - 1]);
    return result;
}

function renderWordArtPathEditor(state) {
    state.path?.setAttribute('d', wordArtPathFromPoints(state.points));
    if (!state.pointLayer) return;
    state.pointLayer.replaceChildren();
    state.points.forEach((point, index) => {
        const circle = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
        circle.setAttribute('cx', String(point.x));
        circle.setAttribute('cy', String(point.y));
        circle.setAttribute('r', index === 0 || index === state.points.length - 1 ? '11' : '8');
        circle.classList.add('wordart-path-point');
        if (index === 0) circle.classList.add('start');
        if (index === state.points.length - 1) circle.classList.add('end');
        circle.dataset.wordartPointIndex = String(index);
        state.pointLayer.appendChild(circle);
    });
}

function commitWordArtPath(state) {
    return state.dotnet.invokeMethodAsync('CommitWordArtPath', state.points.map(point => ({ x: point.x, y: point.y })));
}

function wordArtPathPointerDown(state, event) {
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
}

function wordArtPathPointerMove(state, event) {
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
}

async function wordArtPathPointerUp(state, event) {
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
    try { state.svg.releasePointerCapture(event.pointerId); } catch { }
    await commitWordArtPath(state);
    event.preventDefault();
}

export function initializeWordArtPathEditor(editorId, dotnet, points) {
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
        state.pointerDown = event => wordArtPathPointerDown(state, event);
        state.pointerMove = event => wordArtPathPointerMove(state, event);
        state.pointerUp = event => wordArtPathPointerUp(state, event);
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
}

export function updateWordArtPathEditor(editorId, points) {
    const svg = document.getElementById(editorId);
    const state = svg ? wordArtPathStates.get(svg) : null;
    if (!state || state.operation) return;
    state.points = normalizeWordArtPoints(points);
    state.path = svg.querySelector('[data-wordart-editor-path]');
    state.pointLayer = svg.querySelector('[data-wordart-editor-points]');
    renderWordArtPathEditor(state);
}

export function setWordArtPathDrawMode(editorId, enabled) {
    const svg = document.getElementById(editorId);
    const state = svg ? wordArtPathStates.get(svg) : null;
    if (!state) return;
    state.drawMode = Boolean(enabled);
    svg.classList.toggle('drawing-armed', state.drawMode);
}

export function disposeWordArtPathEditor(editorId) {
    const svg = document.getElementById(editorId);
    const state = svg ? wordArtPathStates.get(svg) : null;
    if (!state) return;
    svg.removeEventListener('pointerdown', state.pointerDown);
    svg.removeEventListener('pointermove', state.pointerMove);
    svg.removeEventListener('pointerup', state.pointerUp);
    svg.removeEventListener('pointercancel', state.pointerUp);
    wordArtPathStates.delete(svg);
}


const publicationAnimationPreviews = new Map();

function parsePublicationData(value, fallback) {
    if (!value) return fallback;
    try { return JSON.parse(value); } catch { return fallback; }
}

function animationName(value) { return String(value || '').replace(/[^a-z0-9]/gi, '').toLowerCase(); }
function isMediaAnimationEffect(value) { return ['playmedia', 'pausemedia', 'stopmedia'].includes(animationName(value)); }
function publicationReducedMotion() { return typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches; }
function publicationAnimationSpan(animation) {
    if (publicationReducedMotion()) return .001;
    if (isMediaAnimationEffect(animation.effect)) return .05;
    return Math.max(.05, animationNumber(animation.durationSeconds, .6))
        * Math.max(1, animationNumber(animation.repeatCount, 1))
        * (animation.autoReverse ? 2 : 1);
}
function animationNumber(value, fallback) {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : fallback;
}
function animationEasing(value) {
    switch (animationName(value)) {
        case 'linear': return 'linear';
        case 'easein': return 'cubic-bezier(.42,0,1,1)';
        case 'easeout': return 'cubic-bezier(0,0,.2,1)';
        case 'backout': return 'cubic-bezier(.18,.89,.32,1.28)';
        case 'bounceout': return 'cubic-bezier(.22,1.3,.36,1)';
        default: return 'cubic-bezier(.4,0,.2,1)';
    }
}
function animationDirectionVector(direction, distance) {
    const amount = animationNumber(distance, 18);
    switch (animationName(direction)) {
        case 'right': return { x: amount, y: 0 };
        case 'up': return { x: 0, y: -amount };
        case 'down': return { x: 0, y: amount };
        default: return { x: -amount, y: 0 };
    }
}
function baseTransform(node) {
    const value = getComputedStyle(node).transform;
    return !value || value === 'none' ? '' : value;
}
function withBase(base, transform) { return `${transform} ${base}`.trim(); }
function publicationAnimationFrames(node, animation) {
    const effect = animationName(animation.effect);
    const phase = animationName(animation.phase);
    const base = baseTransform(node);
    const vector = animationDirectionVector(animation.direction, animation.distancePercent);
    const scaleAmount = Math.max(0.01, animationNumber(animation.scalePercent, 20) / 100);
    const rotation = animationNumber(animation.rotationDegrees, 360);
    const translated = withBase(base, `translate(${vector.x}%,${vector.y}%)`);
    const reverse = frames => phase === 'exit' ? [...frames].reverse() : frames;

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
            return [0, -.2, .2, -.16, .16, -.08, .08, 0].map((factor, index, values) => ({
                offset: index / (values.length - 1), transform: withBase(base, `translateX(${amount * factor * 10}%)`)
            }));
        }
        case 'move':
            return [{ transform: base || 'none' }, { transform: translated }];
        default:
            return [{ opacity: 1 }, { opacity: 1 }];
    }
}
function runPublicationMediaAnimation(node, animation, delaySeconds = 0) {
    const effect = animationName(animation.effect);
    let timer = 0;
    let cancelled = false;
    let resolveFinished;
    const finished = new Promise(resolve => { resolveFinished = resolve; });
    const execute = () => {
        if (cancelled) return;
        if (effect === 'playmedia') playPublicationMediaNode(node);
        else if (effect === 'pausemedia') pausePublicationMediaNode(node, false);
        else if (effect === 'stopmedia') pausePublicationMediaNode(node, true);
        resolveFinished();
    };
    timer = setTimeout(execute, Math.max(0, delaySeconds) * 1000);
    return {
        finished,
        cancel() {
            cancelled = true;
            clearTimeout(timer);
            resolveFinished();
        }
    };
}

function publicationAnimationGroupNodes(node) {
    const groupId = String(node?.dataset?.groupId || '').trim();
    const root = node?.closest?.('.publication-page,.print-page') || node?.parentElement;
    if (!groupId || !root) return [node];
    const peers = [...root.querySelectorAll('[data-publication-element][data-group-id]')]
        .filter(candidate => String(candidate.dataset.groupId || '') === groupId);
    return peers.length ? peers : [node];
}

function publicationGroupTransformOrigins(nodes) {
    const rectangles = nodes.map(node => ({ node, rect: node.getBoundingClientRect(), previous: node.style.transformOrigin }));
    const left = Math.min(...rectangles.map(item => item.rect.left));
    const top = Math.min(...rectangles.map(item => item.rect.top));
    const right = Math.max(...rectangles.map(item => item.rect.right));
    const bottom = Math.max(...rectangles.map(item => item.rect.bottom));
    const centerX = (left + right) / 2;
    const centerY = (top + bottom) / 2;
    for (const item of rectangles)
        item.node.style.transformOrigin = `${centerX - item.rect.left}px ${centerY - item.rect.top}px`;
    return () => rectangles.forEach(item => item.node.style.transformOrigin = item.previous);
}

function publicationAnimationComposite(animations, restore) {
    let restored = false;
    const restoreOnce = () => {
        if (restored) return;
        restored = true;
        restore?.();
    };
    return {
        finished: Promise.all(animations.map(animation => animation.finished.catch(() => undefined))),
        cancel() { animations.forEach(animation => { try { animation.cancel(); } catch { } }); restoreOnce(); },
        pause() { animations.forEach(animation => { try { animation.pause(); } catch { } }); },
        play() { animations.forEach(animation => { try { animation.play(); } catch { } }); }
    };
}

function runPublicationAnimation(node, animation, delaySeconds = 0) {
    if (isMediaAnimationEffect(animation.effect)) return runPublicationMediaAnimation(node, animation, delaySeconds);
    const reducedMotion = publicationReducedMotion();
    const duration = (reducedMotion ? .001 : Math.max(.05, animationNumber(animation.durationSeconds, .6))) * 1000;
    const repeat = Math.max(1, Math.round(animationNumber(animation.repeatCount, 1)));
    const iterations = reducedMotion ? 1 : repeat * (animation.autoReverse ? 2 : 1);
    const nodes = publicationAnimationGroupNodes(node);
    const restore = nodes.length > 1 ? publicationGroupTransformOrigins(nodes) : null;
    const animations = nodes.map(member => member.animate(publicationAnimationFrames(member, animation), {
            duration,
            delay: (reducedMotion ? 0 : Math.max(0, delaySeconds)) * 1000,
            easing: animationEasing(animation.easing),
            iterations,
            direction: animation.autoReverse ? 'alternate' : 'normal',
            fill: animationName(animation.phase) === 'entrance' ? 'both' : 'forwards'
        }));
    return animations.length === 1 ? animations[0] : publicationAnimationComposite(animations, restore);
}
function publicationPageTransitionFrames(page, entering = true) {
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
}
function runPublicationPageTransition(page, entering = true, target = page) {
    const duration = (publicationReducedMotion() ? .001 : Math.max(.1, animationNumber(page.dataset.transitionDuration, .55))) * 1000;
    return target.animate(publicationPageTransitionFrames(page, entering), {
        duration,
        easing: animationEasing(page.dataset.transitionEasing),
        fill: 'both'
    });
}
function animationItems(root) {
    return [...root.querySelectorAll('[data-publication-element]')].flatMap(node => {
        const animations = parsePublicationData(node.dataset.animations, []);
        return animations.map(animation => ({ node, animation }));
    }).sort((left, right) => animationNumber(left.animation.order, 0) - animationNumber(right.animation.order, 0));
}
function clearPublicationPreview(key) {
    const state = publicationAnimationPreviews.get(key);
    if (!state) return;
    for (const animation of state.animations) {
        try { animation.cancel(); } catch { }
    }
    for (const timer of state.mediaTimers || []) clearTimeout(timer);
    for (const node of state.mediaNodes || []) pausePublicationMediaNode(node, true);
    if (state.clickTarget && state.clickHandler) state.clickTarget.removeEventListener('click', state.clickHandler, true);
    state.root?.classList.remove('pub-animation-previewing', 'pub-animation-click-hint');
    publicationAnimationPreviews.delete(key);
}
function schedulePublicationPreviewGroup(state, items, initialOffset = 0) {
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
        if (isMediaAnimationEffect(item.animation.effect)) state.mediaNodes.add(item.node);
        state.animations.push(runPublicationAnimation(item.node, item.animation, start));
        previousStart = start;
        previousEnd = start + publicationAnimationSpan(item.animation);
    }
}
function previewPublicationItems(root, items, includeTransition, transitionTarget = root) {
    clearPublicationPreview(root.id || root);
    const state = { root, animations: [], clickTarget: root, clickHandler: null, clickGroups: [], mediaTimers: [], mediaNodes: new Set() };
    publicationAnimationPreviews.set(root.id || root, state);
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
        .some(node => animationName(node.dataset.mediaTrigger) === 'onclick');
    if (state.clickGroups.length || hasClickMedia) {
        root.classList.add('pub-animation-click-hint');
        state.clickHandler = event => {
            const mediaNode = event.target.closest?.('[data-media-kind]');
            if (mediaNode && root.contains(mediaNode)) {
                if (animationName(mediaNode.dataset.mediaTrigger) === 'onclick') {
                    event.preventDefault();
                    event.stopImmediatePropagation();
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
            if (!state.clickGroups.length && !hasClickMedia) root.classList.remove('pub-animation-click-hint');
        };
        root.addEventListener('click', state.clickHandler, true);
    }
    return state;
}
function previewPageAnimations(pageId) {
    const page = document.getElementById(pageId);
    if (!page) return;
    previewPublicationItems(page, animationItems(page), true);
}
function previewElementAnimations(elementId) {
    const node = document.getElementById(elementId);
    if (!node) return;
    const page = node.closest('.publication-page') || node;
    const animations = parsePublicationData(node.dataset.animations, []);
    previewPublicationItems(page, animations.map(animation => ({ node, animation })), false);
}
function previewAnimationStep(pageId, animationId) {
    const page = document.getElementById(pageId);
    if (!page) return;
    const item = animationItems(page).find(entry => String(entry.animation.id).toLowerCase() === String(animationId).toLowerCase());
    if (item) previewPublicationItems(page, [item], false);
}
function stopAnimationPreview(pageId) {
    const page = document.getElementById(pageId);
    clearPublicationPreview(pageId);
    if (page) clearPublicationPreview(page);
}


function publicationMediaElement(elementId) {
    const node = document.getElementById(elementId);
    return node?.querySelector('video,audio') || null;
}
function configurePublicationMedia(node, media) {
    if (!node || !media) return null;
    const start = Math.max(0, animationNumber(node.dataset.mediaTrimStart, 0));
    const end = Math.max(start + .01, animationNumber(node.dataset.mediaTrimEnd, media.duration || start + 1));
    const baseVolume = Math.max(0, Math.min(1, animationNumber(node.dataset.mediaVolume, 1)));
    const rate = Math.max(.1, animationNumber(node.dataset.mediaRate, 1));
    const fadeIn = Math.max(0, animationNumber(node.dataset.mediaFadeIn, 0));
    const fadeOut = Math.max(0, animationNumber(node.dataset.mediaFadeOut, 0));
    media.volume = fadeIn > 0 ? 0 : baseVolume;
    media.playbackRate = rate;
    media.muted = node.dataset.mediaMuted === 'true';
    media.loop = false;
    try { media.currentTime = start; } catch { }
    const previous = media.__publisherTimeHandler;
    if (previous) media.removeEventListener('timeupdate', previous);
    const handler = () => {
        const presentationPosition = (media.currentTime - start) / rate;
        const presentationRemaining = (end - media.currentTime) / rate;
        let gain = baseVolume;
        if (fadeIn > 0) gain *= Math.max(0, Math.min(1, presentationPosition / fadeIn));
        if (fadeOut > 0) gain *= Math.max(0, Math.min(1, presentationRemaining / fadeOut));
        if (!media.muted) media.volume = Math.max(0, Math.min(1, gain));
        if (media.currentTime < end - .02) return;
        if (node.dataset.mediaLoop === 'true') {
            media.currentTime = start;
            media.play().catch(() => {});
        } else media.pause();
    };
    media.__publisherTimeHandler = handler;
    media.addEventListener('timeupdate', handler);
    return { start, end };
}
function playPublicationMediaNode(node) {
    const media = node?.querySelector('video,audio');
    if (!node || !media) return;
    configurePublicationMedia(node, media);
    media.play().catch(() => {});
}
function pausePublicationMediaNode(node, rewind = false) {
    const media = node?.querySelector('video,audio');
    if (!media) return;
    media.pause();
    if (rewind) {
        const start = Math.max(0, animationNumber(node.dataset.mediaTrimStart, 0));
        try { media.currentTime = start; } catch { }
    }
}
function togglePublicationMediaNode(node) {
    const media = node?.querySelector('video,audio');
    if (!media) return;
    if (media.paused) playPublicationMediaNode(node); else media.pause();
}
function schedulePublicationPreviewMedia(state, root, initialOffset = 0) {
    for (const node of root.querySelectorAll('[data-media-kind]')) {
        const trigger = animationName(node.dataset.mediaTrigger);
        if (node.dataset.mediaAutoplay === 'false' || trigger === 'onclick') continue;
        const delay = Math.max(0, initialOffset + animationNumber(node.dataset.mediaStart, 0));
        state.mediaNodes.add(node);
        if (publicationReducedMotion() || delay <= 0) playPublicationMediaNode(node);
        else state.mediaTimers.push(setTimeout(() => playPublicationMediaNode(node), delay * 1000));
    }
}
function playPublicationMedia(elementId) {
    playPublicationMediaNode(document.getElementById(elementId));
}
function pausePublicationMedia(elementId) {
    pausePublicationMediaNode(document.getElementById(elementId));
}

function websitePresentationRuntime() {
    const publication = document.querySelector('.website-publication');
    if (!publication) return;
    const pages = [...publication.querySelectorAll(':scope > .print-page')];
    if (!pages.length) return;
    const lower = value => String(value || '').replace(/[^a-z0-9]/gi, '').toLowerCase();
    const num = (value, fallback) => { const parsed = Number(value); return Number.isFinite(parsed) ? parsed : fallback; };
    const pageSizes = pages.map(page => ({
        width: Math.max(1, num(page.dataset.exportWidthPx, page.offsetWidth || 1)),
        height: Math.max(1, num(page.dataset.exportHeightPx, page.offsetHeight || 1))
    }));
    const frameWidth = Math.max(1, num(publication.dataset.frameWidthPx, Math.max(...pageSizes.map(size => size.width))));
    const frameHeight = Math.max(1, num(publication.dataset.frameHeightPx, Math.max(...pageSizes.map(size => size.height))));
    const bool = value => String(value).toLowerCase() === 'true';
    const parse = (value, fallback) => { try { return JSON.parse(value || ''); } catch { return fallback; } };
    const reducedMotion = typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;
    const animationSpan = animation => reducedMotion ? .001 : ['playmedia','pausemedia','stopmedia'].includes(lower(animation.effect))
        ? .05
        : Math.max(.05, num(animation.durationSeconds, .6)) * Math.max(1, num(animation.repeatCount, 1)) * (animation.autoReverse ? 2 : 1);
    const easing = value => {
        switch (lower(value)) {
            case 'linear': return 'linear';
            case 'easein': return 'cubic-bezier(.42,0,1,1)';
            case 'easeout': return 'cubic-bezier(0,0,.2,1)';
            case 'backout': return 'cubic-bezier(.18,.89,.32,1.28)';
            case 'bounceout': return 'cubic-bezier(.22,1.3,.36,1)';
            default: return 'cubic-bezier(.4,0,.2,1)';
        }
    };
    const vector = (direction, distance) => {
        const amount = num(distance, 18);
        switch (lower(direction)) {
            case 'right': return { x: amount, y: 0 };
            case 'up': return { x: 0, y: -amount };
            case 'down': return { x: 0, y: amount };
            default: return { x: -amount, y: 0 };
        }
    };
    const baseTransform = node => { const value = getComputedStyle(node).transform; return !value || value === 'none' ? '' : value; };
    const compose = (base, extra) => `${extra} ${base}`.trim();
    const frames = (node, animation) => {
        const effect = lower(animation.effect);
        const phase = lower(animation.phase);
        const base = baseTransform(node);
        const move = vector(animation.direction, animation.distancePercent);
        const scale = Math.max(.01, num(animation.scalePercent, 20) / 100);
        const rotation = num(animation.rotationDegrees, 360);
        const translated = compose(base, `translate(${move.x}%,${move.y}%)`);
        const reverse = value => phase === 'exit' ? [...value].reverse() : value;
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
                return positions.map((factor, index) => ({ offset: index / (positions.length - 1), transform: compose(base, `translateX(${amount * factor}%)`) }));
            }
            case 'move': return [{ transform: base || 'none' }, { transform: translated }];
            default: return [{ opacity: 1 }, { opacity: 1 }];
        }
    };
    const groupNodes = node => {
        const groupId = String(node?.dataset?.groupId || '').trim();
        const page = node?.closest?.('.print-page');
        if (!groupId || !page) return [node];
        const peers = [...page.querySelectorAll('[data-publication-element][data-group-id]')]
            .filter(candidate => String(candidate.dataset.groupId || '') === groupId);
        return peers.length ? peers : [node];
    };
    const setGroupOrigins = nodes => {
        if (nodes.length < 2) return () => {};
        const entries = nodes.map(node => ({ node, rect: node.getBoundingClientRect(), previous: node.style.transformOrigin }));
        const left = Math.min(...entries.map(item => item.rect.left));
        const top = Math.min(...entries.map(item => item.rect.top));
        const right = Math.max(...entries.map(item => item.rect.right));
        const bottom = Math.max(...entries.map(item => item.rect.bottom));
        const centerX = (left + right) / 2;
        const centerY = (top + bottom) / 2;
        entries.forEach(item => item.node.style.transformOrigin = `${centerX - item.rect.left}px ${centerY - item.rect.top}px`);
        return () => entries.forEach(item => item.node.style.transformOrigin = item.previous);
    };
    const compositeAnimation = (animations, restore) => ({
        finished: Promise.all(animations.map(animation => animation.finished.catch(() => undefined))),
        cancel() { animations.forEach(animation => { try { animation.cancel(); } catch { } }); restore(); },
        pause() { animations.forEach(animation => { try { animation.pause(); } catch { } }); },
        play() { animations.forEach(animation => { try { animation.play(); } catch { } }); }
    });
    const playItem = (item, delay = 0) => {
        item.prestate?.cancel();
        item.prestate = null;
        const effect = lower(item.animation.effect);
        if (['playmedia','pausemedia','stopmedia'].includes(effect)) {
            let timer = 0;
            let cancelled = false;
            let resolveFinished;
            const finished = new Promise(resolve => { resolveFinished = resolve; });
            const execute = () => {
                if (cancelled) return;
                if (effect === 'playmedia') playMediaNode(item.node);
                else if (effect === 'pausemedia') pauseMediaNode(item.node);
                else stopMediaNode(item.node, true);
                resolveFinished();
            };
            timer = setTimeout(execute, Math.max(0, delay) * 1000);
            activeMediaTimers.push(timer);
            const handle = { finished, cancel() { cancelled = true; clearTimeout(timer); resolveFinished(); } };
            activeAnimations.push(handle);
            return handle;
        }
        const nodes = groupNodes(item.node);
        if (lower(item.animation.phase) === 'entrance') nodes.forEach(node => node.classList.remove('ps-action-hidden'));
        const repeat = Math.max(1, Math.round(num(item.animation.repeatCount, 1)));
        const restore = setGroupOrigins(nodes);
        const members = nodes.map(node => node.animate(frames(node, item.animation), {
            duration: (reducedMotion ? .001 : Math.max(.05, num(item.animation.durationSeconds, .6))) * 1000,
            delay: (reducedMotion ? 0 : Math.max(0, delay)) * 1000,
            easing: easing(item.animation.easing),
            iterations: reducedMotion ? 1 : repeat * (item.animation.autoReverse ? 2 : 1),
            direction: item.animation.autoReverse ? 'alternate' : 'normal',
            fill: lower(item.animation.phase) === 'entrance' ? 'both' : 'forwards'
        }));
        const animation = members.length === 1 ? members[0] : compositeAnimation(members, restore);
        activeAnimations.push(animation);
        return animation;
    };
    const transitionFrames = (page, entering) => {
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
    };
    const pageItems = page => [...page.querySelectorAll('[data-publication-element]')].flatMap(node => parse(node.dataset.animations, []).map(animation => ({ node, animation, prestate: null }))).sort((a, b) => num(a.animation.order, 0) - num(b.animation.order, 0));
    const splitTimeline = items => {
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
    };
    const scheduleGroup = items => {
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
    };
    const primeClickEntrances = groups => {
        for (const item of groups.flat()) {
            if (lower(item.animation.phase) !== 'entrance') continue;
            const members = groupNodes(item.node).map(node => {
                const animation = node.animate(frames(node, item.animation), { duration: 1, fill: 'both' });
                animation.pause();
                animation.currentTime = 0;
                return animation;
            });
            item.prestate = members.length === 1 ? members[0] : compositeAnimation(members, () => {});
            activeAnimations.push(item.prestate);
        }
    };
    const mediaFromNode = node => node?.querySelector('video,audio') || null;
    const pauseMediaNode = node => {
        const media = mediaFromNode(node);
        if (media) media.pause();
    };
    const stopMediaNode = (node, rewind = false) => {
        const media = mediaFromNode(node);
        if (!media) return;
        media.pause();
        if (media.__psTimeHandler) media.removeEventListener('timeupdate', media.__psTimeHandler);
        media.__psTimeHandler = null;
        if (rewind) {
            const start = Math.max(0, num(node.dataset.mediaTrimStart, 0));
            try { media.currentTime = start; } catch { }
        }
    };
    const playMediaNode = (node, delay = 0) => {
        const media = mediaFromNode(node);
        if (!media) return;
        const run = () => {
            const start = Math.max(0, num(node.dataset.mediaTrimStart, 0));
            const end = Math.max(start + .01, num(node.dataset.mediaTrimEnd, media.duration || start + 1));
            const baseVolume = Math.max(0, Math.min(1, num(node.dataset.mediaVolume, 1)));
            const fadeIn = Math.max(0, num(node.dataset.mediaFadeIn, 0));
            const fadeOut = Math.max(0, num(node.dataset.mediaFadeOut, 0));
            media.playbackRate = Math.max(.1, num(node.dataset.mediaRate, 1));
            media.muted = bool(node.dataset.mediaMuted);
            media.loop = false;
            media.volume = fadeIn > 0 ? 0 : baseVolume;
            try { media.currentTime = start; } catch { }
            if (media.__psTimeHandler) media.removeEventListener('timeupdate', media.__psTimeHandler);
            const onTime = () => {
                const position = media.currentTime;
                const timelinePosition = (position - start) / Math.max(.1, media.playbackRate);
                const timelineRemaining = (end - position) / Math.max(.1, media.playbackRate);
                let volume = baseVolume;
                if (fadeIn > 0) volume *= Math.max(0, Math.min(1, timelinePosition / fadeIn));
                if (fadeOut > 0) volume *= Math.max(0, Math.min(1, timelineRemaining / fadeOut));
                if (!media.muted) media.volume = Math.max(0, Math.min(1, volume));
                if (position < end - .02) return;
                if (bool(node.dataset.mediaLoop)) {
                    media.currentTime = start;
                    media.play().catch(() => {});
                } else media.pause();
            };
            media.__psTimeHandler = onTime;
            media.addEventListener('timeupdate', onTime);
            media.play().catch(() => {});
        };
        if (delay > 0) activeMediaTimers.push(setTimeout(run, delay * 1000)); else run();
    };
    const toggleMediaNode = node => {
        const media = mediaFromNode(node);
        if (!media) return;
        if (media.paused) playMediaNode(node); else media.pause();
    };
    const startPageMedia = page => {
        for (const node of page.querySelectorAll('[data-media-kind]')) {
            const trigger = lower(node.dataset.mediaTrigger);
            if (!bool(node.dataset.mediaAutoplay) || trigger === 'onclick') continue;
            playMediaNode(node, Math.max(0, num(node.dataset.mediaStart, 0)));
        }
    };
    const resetPageVisibility = page => {
        for (const node of page.querySelectorAll('[data-publication-element]'))
            node.classList.toggle('ps-action-hidden', bool(node.dataset.playbackHidden));
    };
    const cancelPlayback = () => {
        clearTimeout(autoTimer);
        autoTimer = 0;
        for (const animation of activeAnimations) { try { animation.cancel(); } catch { } }
        activeAnimations = [];
        for (const timer of activeMediaTimers) clearTimeout(timer);
        activeMediaTimers = [];
        for (const node of publication.querySelectorAll('[data-media-kind]')) stopMediaNode(node, true);
        clickGroups = [];
    };
    const fitPages = () => {
        const controlsHeight = controls && !controls.hidden ? 62 : 18;
        const scale = Math.min(
            (innerWidth - 32) / frameWidth,
            (innerHeight - controlsHeight - 24) / frameHeight,
            1.75);
        stage.style.transform = `scale(${Math.max(.05, scale)})`;
    };
    const updateControls = () => {
        if (!counter) return;
        counter.textContent = `${current + 1} / ${pages.length}`;
        previousButton.disabled = current <= 0 && !loop;
        nextButton.disabled = current >= pages.length - 1 && !loop;
    };
    const normalizeIndex = value => {
        if (loop) return (value % pages.length + pages.length) % pages.length;
        return Math.max(0, Math.min(pages.length - 1, value));
    };
    const showPage = async (requested, direction = 1, animate = true) => {
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
                try { await Promise.allSettled([incoming.finished, outgoing.finished]); } catch { }
            }
        }
        shells.forEach((shell, index) => {
            shell.hidden = index !== next;
            shell.classList.toggle('active', index === next);
        });
        current = next;
        updateControls();
        runCurrentPage(startAutomatically || animate);
    };
    const runCurrentPage = shouldRun => {
        cancelPlayback();
        const page = pages[current];
        resetPageVisibility(page);
        const timeline = splitTimeline(pageItems(page));
        clickGroups = timeline.clickGroups;
        if (shouldRun) {
            scheduleGroup(timeline.automatic);
            startPageMedia(page);
        } else primeClickEntrances([timeline.automatic]);
        primeClickEntrances(clickGroups);
        if (bool(page.dataset.autoAdvance)) {
            const seconds = Math.max(.25, num(page.dataset.autoAdvanceSeconds, 5));
            autoTimer = setTimeout(() => showPage(current + 1, 1, true), seconds * 1000);
        }
    };
    const runNextClickGroup = () => {
        const group = clickGroups.shift();
        if (!group) return false;
        scheduleGroup(group);
        return true;
    };
    const replayCurrent = () => runCurrentPage(true);
    const goNext = () => showPage(current + 1, 1, true);
    const goPrevious = () => showPage(current - 1, -1, true);

    const stage = document.createElement('div');
    stage.className = 'ps-stage';
    stage.style.width = `${frameWidth}px`;
    stage.style.height = `${frameHeight}px`;
    publication.appendChild(stage);

    const shells = pages.map((page, index) => {
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
    });
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
    previousButton.addEventListener('click', event => { event.stopPropagation(); goPrevious(); });
    nextButton.addEventListener('click', event => { event.stopPropagation(); goNext(); });
    controls.querySelector('[data-ps-replay]').addEventListener('click', event => { event.stopPropagation(); replayCurrent(); });
    controls.querySelector('[data-ps-fullscreen]').addEventListener('click', async event => {
        event.stopPropagation();
        try { if (!document.fullscreenElement) await document.documentElement.requestFullscreen(); else await document.exitFullscreen(); } catch { }
    });

    pages.forEach((page, pageIndex) => {
        page.addEventListener('click', event => {
            if (event.defaultPrevented) return;
            if (runNextClickGroup()) return;
            if (bool(page.dataset.advanceOnClick)) goNext();
        });
        for (const node of page.querySelectorAll('[data-publication-element]')) {
            const interaction = parse(node.dataset.interaction, {});
            const interactionAction = lower(interaction.action);
            if (node.dataset.mediaKind && lower(node.dataset.mediaTrigger) === 'onclick' && (!interactionAction || interactionAction === 'none')) {
                node.classList.add('ps-interactive');
                node.addEventListener('click', event => {
                    event.preventDefault();
                    event.stopPropagation();
                    toggleMediaNode(node);
                });
            }
            if (interactionAction === 'none' || !interaction.action) continue;
            node.classList.add('ps-interactive');
            node.addEventListener('click', event => {
                event.preventDefault();
                event.stopPropagation();
                const action = lower(interaction.action);
                if (action === 'nextpage') goNext();
                else if (action === 'previouspage') goPrevious();
                else if (action === 'gotopage') {
                    const target = pages.findIndex(item => String(item.dataset.pageId).toLowerCase() === String(interaction.targetPageId || '').toLowerCase());
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
                        const items = parse(target.dataset.animations, []).map(animation => ({ node: target, animation, prestate: null }));
                        scheduleGroup(items);
                    } else if (action === 'playmedia') playMediaNode(target);
                    else if (action === 'pausemedia') pauseMediaNode(target);
                    else if (action === 'togglemediaplayback') toggleMediaNode(target);
                }
            });
        }
    });

    addEventListener('resize', fitPages);
    addEventListener('keydown', event => {
        if (event.key === 'ArrowRight' || event.key === 'PageDown') { event.preventDefault(); if (!runNextClickGroup()) goNext(); }
        else if (event.key === 'ArrowLeft' || event.key === 'PageUp') { event.preventDefault(); goPrevious(); }
        else if (event.key === ' ' || event.key === 'Enter') { event.preventDefault(); if (!runNextClickGroup()) goNext(); }
        else if (event.key.toLowerCase() === 'r') replayCurrent();
        else if (event.key === 'Home') showPage(0, -1, true);
        else if (event.key === 'End') showPage(pages.length - 1, 1, true);
    });
    fitPages();
    shells[0].hidden = false;
    shells[0].classList.add('active');
    updateControls();
    const startFirstPage = async () => {
        if (startAutomatically && lower(pages[0].dataset.transitionKind) !== 'none') {
            const duration = (reducedMotion ? .001 : Math.max(.1, num(pages[0].dataset.transitionDuration, .55))) * 1000;
            const initial = shells[0].animate(transitionFrames(pages[0], true), {
                duration,
                easing: easing(pages[0].dataset.transitionEasing),
                fill: 'both'
            });
            activeAnimations.push(initial);
            try { await initial.finished; } catch { }
        }
        runCurrentPage(startAutomatically);
    };
    startFirstPage();
}

function barcodeColor(value, fallback) {
    const text = String(value || '').trim();
    return /^#[0-9a-f]{3,8}$/i.test(text) || /^(rgb|hsl)a?\(/i.test(text) || /^[a-z]+$/i.test(text) ? text : fallback;
}

function barcodeEnumName(value, names, fallback) {
    if (Number.isInteger(value) && value >= 0 && value < names.length) return names[value];
    const numeric = Number(value);
    if (Number.isInteger(numeric) && String(value).trim() !== '' && numeric >= 0 && numeric < names.length) return names[numeric];
    const normalized = String(value ?? '').replace(/[^a-z0-9]/gi, '').toLowerCase();
    return names.find(name => name.replace(/[^a-z0-9]/gi, '').toLowerCase() === normalized) || fallback;
}

function barcodeFormatToken(value) {
    return barcodeEnumName(value, ['QrCode', 'Code128', 'Code39', 'Ean13', 'UpcA', 'Itf14', 'Codabar'], 'Code128');
}

function barcodeFormatName(value) {
    const normalized = barcodeFormatToken(value).toLowerCase();
    return ({ qrcode: 'QR', code128: 'CODE128', code39: 'CODE39', ean13: 'EAN13', upca: 'UPC', itf14: 'ITF14', codabar: 'codabar' })[normalized] || 'CODE128';
}

function barcodeCorrectionName(value) {
    return barcodeEnumName(value, ['L', 'M', 'Q', 'H'], 'M').toUpperCase();
}

function barcodeShapeName(value) {
    return barcodeEnumName(value, ['Square', 'Rounded', 'Dots'], 'Square').toLowerCase();
}

function generateQrSvg(options) {
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
}

function generateLinearBarcodeSvg(options) {
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
            valid: result => { valid = result; }
        });
    } catch (error) {
        throw new Error(`${formatToken} could not encode "${value}": ${error?.message || error}`);
    }
    if (!valid) throw new Error(`The value "${value}" is invalid for ${formatToken}.`);
    const width = Number(svg.getAttribute('width')) || 320;
    const height = Number(svg.getAttribute('height')) || 120;
    if (transparent) {
        svg.querySelectorAll('rect').forEach(rect => {
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
        });
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
        svg.querySelectorAll('rect').forEach(rect => {
            const barWidth = number(rect.getAttribute('width'), 0);
            if (barWidth > 0 && barWidth < width * .5) {
                const radius = Math.min(2, barWidth / 2);
                rect.setAttribute('rx', String(radius));
                rect.setAttribute('ry', String(radius));
            }
        });
    return svg.outerHTML;
}

const barcodeLibraryLoads = new Map();

function loadBarcodeLibrary(src, available, errorMessage, timeoutMilliseconds = 5000) {
    if (available()) return Promise.resolve();
    if (barcodeLibraryLoads.has(src)) return barcodeLibraryLoads.get(src);

    const promise = new Promise((resolve, reject) => {
        let script = [...document.scripts].find(item => {
            try { return new URL(item.src, document.baseURI).pathname.endsWith(src); }
            catch { return false; }
        });
        let settled = false;
        const finish = error => {
            if (settled) return;
            settled = true;
            clearTimeout(timer);
            script?.removeEventListener('load', loaded);
            script?.removeEventListener('error', failed);
            if (error || !available()) reject(error || new Error(errorMessage));
            else resolve();
        };
        const loaded = () => finish();
        const failed = () => finish(new Error(errorMessage));
        const timer = setTimeout(() => finish(new Error(errorMessage)), timeoutMilliseconds);

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
        queueMicrotask(() => { if (available()) finish(); });
    });
    barcodeLibraryLoads.set(src, promise);
    promise.catch(() => barcodeLibraryLoads.delete(src));
    return promise;
}

async function waitForBarcodeGenerator(format) {
    const qr = format === 'qrcode';
    if (qr) {
        await loadBarcodeLibrary('js/vendor/qrcode-generator.js',
            () => typeof window.qrcode === 'function', 'QR-code generator did not load.');
        return;
    }
    await loadBarcodeLibrary('js/vendor/JsBarcode.all.min.js',
        () => typeof window.JsBarcode === 'function', 'Barcode generator did not load.');
}

export async function generateBarcodeSvg(options) {
    const format = barcodeFormatToken(options?.format).toLowerCase();
    await waitForBarcodeGenerator(format);
    return format === 'qrcode' ? generateQrSvg(options) : generateLinearBarcodeSvg(options);
}

function sleep(milliseconds) { return new Promise(resolve => setTimeout(resolve, milliseconds)); }

function chooseVideoRecordingMimeType() {
    if (typeof MediaRecorder === 'undefined') return '';
    const probe = document.createElement('video');
    const candidates = ['video/webm;codecs=vp8,opus', 'video/webm', 'video/webm;codecs=vp9,opus'];
    return candidates.find(type => MediaRecorder.isTypeSupported(type) && probe.canPlayType(type) !== '')
        || candidates.find(type => MediaRecorder.isTypeSupported(type))
        || '';
}

function exportedPageDuration(page) {
    const transitionDuration = Math.max(.1, animationNumber(page.dataset.transitionDuration, .55));
    let duration = Math.max(2.5, transitionDuration + .3, animationNumber(page.dataset.timelineDuration, 0), animationNumber(page.dataset.autoAdvanceSeconds, 0));
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
}

function prepareVideoExportPage(page) {
    page.querySelectorAll('[data-media-kind]').forEach(node => {
        if (animationName(node.dataset.mediaTrigger) === 'onclick') node.dataset.mediaTrigger = 'OnPageEnter';
        node.dataset.mediaAutoplay = 'true';
    });
    return animationItems(page).map(item => ({
        node: item.node,
        animation: animationName(item.animation.trigger) === 'onclick'
            ? { ...item.animation, trigger: 'AfterPrevious', timelineStartSeconds: null }
            : item.animation
    }));
}

async function requestPresentationCapture() {
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
}

function evenVideoDimension(value, fallback) {
    const rounded = Math.max(2, Math.round(Number(value) || fallback || 2));
    return rounded % 2 === 0 ? rounded : rounded + 1;
}

function pagePresentationSize(page) {
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
}

function publicationFrameDefinition(pages, evenDimensions = false) {
    const measured = pages.map(pagePresentationSize);
    const fallback = { width: 1280, height: 720, area: 1280 * 720 };
    if (!measured.length)
        return { width: fallback.width, height: fallback.height, pageSizes: [] };

    // A publication containing any landscape page exports to a landscape frame.
    // Every page still participates in the maximum-size calculation: mixed portrait
    // pages contribute their long side to frame width and their short side to frame
    // height. Portrait-only publications keep their native portrait orientation.
    const landscape = measured.some(size => size.width > size.height + .5);
    let width = landscape
        ? Math.max(...measured.map(size => Math.max(size.width, size.height)))
        : Math.max(...measured.map(size => size.width));
    let height = landscape
        ? Math.max(...measured.map(size => Math.min(size.width, size.height)))
        : Math.max(...measured.map(size => size.height));
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
}

async function restrictPresentationCapture(capture, target, targetWidth, targetHeight) {
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
}

function waitForVideoMetadata(video, timeoutMilliseconds = 12000) {
    if (video.readyState >= HTMLMediaElement.HAVE_METADATA && video.videoWidth > 0 && video.videoHeight > 0)
        return Promise.resolve();
    return new Promise((resolve, reject) => {
        const timeout = setTimeout(() => finish(new Error('The selected tab capture did not produce video frames.')), timeoutMilliseconds);
        const finish = error => {
            clearTimeout(timeout);
            video.removeEventListener('loadedmetadata', loaded);
            video.removeEventListener('canplay', loaded);
            video.removeEventListener('error', failed);
            if (error) reject(error); else resolve();
        };
        const loaded = () => video.videoWidth > 0 && video.videoHeight > 0 && finish();
        const failed = () => finish(video.error || new Error('The selected tab capture could not be decoded.'));
        video.addEventListener('loadedmetadata', loaded);
        video.addEventListener('canplay', loaded);
        video.addEventListener('error', failed);
    });
}

async function createPageFrameRecordingStream(capture, frame, targetWidth, targetHeight) {
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
    capture.getAudioTracks().forEach(track => output.addTrack(track));

    let animationFrame = 0;
    let stopped = false;
    const draw = () => {
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
    };
    draw();

    return {
        stream: output,
        displaySurface: captureVideoTrack.getSettings?.().displaySurface || '',
        stop() {
            stopped = true;
            if (animationFrame) cancelAnimationFrame(animationFrame);
            try { sourceVideo.pause(); } catch { }
            sourceVideo.srcObject = null;
            canvasTrack.stop();
        }
    };
}

async function exportPresentationVideo(containerSelector, fileName, title) {
    let step = 'initializing';
    if (typeof MediaRecorder === 'undefined') throw new Error('This browser does not support MediaRecorder video export.');
    if (typeof HTMLCanvasElement.prototype.captureStream !== 'function')
        throw new Error('This browser cannot record the page-sized compositor canvas.');
    const source = document.querySelector(containerSelector);
    if (!source) throw new Error('The publication export surface is not available.');
    const sourcePages = [...source.querySelectorAll(':scope > .print-page')];
    if (!sourcePages.length) throw new Error('The publication does not contain any pages.');

    const overlay = document.createElement('div');
    overlay.className = 'publisher-video-export-overlay';
    overlay.setAttribute('aria-label', `${title || 'Publication'} video export`);

    const frame = document.createElement('div');
    frame.className = 'publisher-video-export-frame';
    const publication = source.cloneNode(true);
    publication.removeAttribute('aria-hidden');
    publication.className = 'publisher-video-export-publication';
    const pages = [...publication.querySelectorAll(':scope > .print-page')];
    pages.forEach((page, index) => {
        page.id = `publisher-video-export-page-${index}-${Date.now()}`;
        page.querySelectorAll('video,audio').forEach(media => { media.controls = false; media.preload = 'auto'; });
    });
    const pageShells = pages.map(page => {
        const shell = document.createElement('div');
        shell.className = 'publisher-video-page-shell';
        page.before(shell);
        shell.appendChild(page);
        shell.hidden = true;
        return shell;
    });
    frame.appendChild(publication);

    const countdown = document.createElement('div');
    countdown.className = 'publisher-video-export-countdown';
    countdown.textContent = 'Select This Tab and enable tab audio when needed.';
    const cancelButton = document.createElement('button');
    cancelButton.type = 'button';
    cancelButton.className = 'publisher-video-export-cancel';
    cancelButton.textContent = 'Cancel export and return';
    let cancelled = false;
    const cancelExport = () => {
        cancelled = true;
        if (recorder && recorder.state !== 'inactive') { try { recorder.stop(); } catch { } }
        if (capture) capture.getTracks().forEach(track => { try { track.stop(); } catch { } });
    };
    const waitForExport = async milliseconds => {
        const end = performance.now() + Math.max(0, milliseconds);
        while (performance.now() < end) {
            if (cancelled) throw new Error('Video export was cancelled.');
            await sleep(Math.min(120, Math.max(1, end - performance.now())));
        }
        if (cancelled) throw new Error('Video export was cancelled.');
    };
    const cancelOnEscape = event => { if (event.key === 'Escape') cancelExport(); };
    cancelButton.addEventListener('click', cancelExport);
    window.addEventListener('keydown', cancelOnEscape, true);
    activeVideoExportCancel?.();
    activeVideoExportCancel = cancelExport;
    overlay.append(frame, countdown, cancelButton);
    document.body.appendChild(overlay);

    const frameDefinition = publicationFrameDefinition(pages, true);
    pageShells.forEach((shell, index) => shell.hidden = index !== 0);
    const frameWidth = frameDefinition.width;
    const frameHeight = frameDefinition.height;
    frame.style.width = `${frameWidth}px`;
    frame.style.height = `${frameHeight}px`;
    frame.style.setProperty('--publisher-video-frame-width', `${frameWidth}px`);
    frame.style.setProperty('--publisher-video-frame-height', `${frameHeight}px`);

    const fitPage = (page, pageIndex = pages.indexOf(page)) => {
        const measured = frameDefinition.pageSizes[pageIndex] || pagePresentationSize(page);
        const scale = Math.min(frameWidth / measured.width, frameHeight / measured.height);
        page.style.width = `${measured.width}px`;
        page.style.height = `${measured.height}px`;
        page.style.transform = `translate(-50%, -50%) scale(${Math.max(.01, scale)})`;
        page.style.transformOrigin = 'center center';
        page.style.translate = 'none';
    };
    const fitFrameToViewport = () => {
        const viewportWidth = window.visualViewport?.width || innerWidth;
        const viewportHeight = window.visualViewport?.height || innerHeight;
        const scale = Math.min((viewportWidth - 32) / frameWidth, (viewportHeight - 32) / frameHeight, 1);
        frame.style.transform = `scale(${Math.max(.05, scale)})`;
    };
    pages.forEach(fitPage);
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
        recorder.addEventListener('dataavailable', event => { if (event.data?.size) chunks.push(event.data); });
        stopped = new Promise((resolve, reject) => {
            recorder.addEventListener('stop', resolve, { once: true });
            recorder.addEventListener('error', event => reject(event.error || new Error('Video recording failed.')), { once: true });
        });

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
            pageShells.forEach((candidate, candidateIndex) => candidate.hidden = candidateIndex !== index);
            fitPage(page, index);
            await waitForExport(120);
            const duration = exportedPageDuration(page);
            totalDuration += duration;
            // Animate the fixed-size shell rather than the fitted page. Page transition
            // transforms therefore no longer overwrite the page's centering/scale.
            previewPublicationItems(page, prepareVideoExportPage(page), true, shell);
            await waitForExport(duration * 1000);
            if (cancelled) throw new Error('Video export was cancelled.');
            clearPublicationPreview(page.id || page);
            page.querySelectorAll('video,audio').forEach(media => {
                try { media.pause(); } catch { }
                try { media.currentTime = 0; } catch { }
            });
        }

        step = 'finalizing WebM';
        if (recorder.state === 'recording') {
            try { recorder.requestData(); } catch { }
            await waitForExport(120);
            recorder.stop();
        }
        await Promise.race([
            stopped,
            new Promise((_, reject) => setTimeout(() => reject(new Error('MediaRecorder did not finish the WebM file.')), 15000))
        ]);
        const blobType = String(recorder.mimeType || 'video/webm').split(';', 1)[0] || 'video/webm';
        const blob = new Blob(chunks, { type: blobType });
        if (!blob.size) throw new Error('The browser completed the capture but produced an empty video.');
        step = 'downloading WebM';
        downloadBlob(fileName || 'publication.webm', blob);
        return {
            fileName: fileName || 'publication.webm',
            durationSeconds: totalDuration,
            width: frameWidth,
            height: frameHeight,
            pageSizedCapture: true
        };
    } catch (error) {
        const message = error?.message || String(error);
        console.error(`Publisher video export failed while ${step}.`, error);
        throw new Error(`Video export failed while ${step}: ${message}`);
    } finally {
        if (recorder && recorder.state !== 'inactive') { try { recorder.stop(); } catch { } }
        try { compositor?.stop(); } catch { }
        if (capture) capture.getTracks().forEach(track => { try { track.stop(); } catch { } });
        window.removeEventListener('resize', fitFrameToViewport);
        window.removeEventListener('keydown', cancelOnEscape, true);
        if (activeVideoExportCancel === cancelExport) activeVideoExportCancel = null;
        overlay.remove();
    }
}

const storyEditorLayouts = new WeakMap();

function initializeStoryEditorLayout(shellId, hostId) {
    const shell = document.getElementById(shellId);
    const host = document.getElementById(hostId);
    if (!shell || !host) return;
    let state = storyEditorLayouts.get(shell);
    if (state) {
        state.host = host;
        state.schedule();
        return;
    }
    let timer = 0;
    const refresh = () => {
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
    };
    const schedule = () => {
        if (timer) clearTimeout(timer);
        timer = window.setTimeout(refresh, 40);
    };
    const click = event => {
        if (!event.target.closest('button,[role="tab"],[role="button"]')) return;
        schedule();
        window.setTimeout(schedule, 120);
        window.setTimeout(schedule, 320);
    };
    shell.addEventListener('click', click, true);
    const resizeObserver = typeof ResizeObserver === 'function' ? new ResizeObserver(schedule) : null;
    resizeObserver?.observe(shell);
    resizeObserver?.observe(host);
    state = { host, schedule, resizeObserver, click };
    storyEditorLayouts.set(shell, state);
    schedule();
}


let dataVisualLayoutTimer = 0;
export function refreshDataVisualLayout(pageId = 'publisher-page') {
    const page = document.getElementById(pageId);
    if (!page?.querySelector?.('.data-visual-view')) return;
    if (dataVisualLayoutTimer) clearTimeout(dataVisualLayoutTimer);
    requestAnimationFrame(() => {
        window.dispatchEvent(new Event('resize'));
        dataVisualLayoutTimer = window.setTimeout(() => {
            dataVisualLayoutTimer = 0;
            if (page.isConnected) window.dispatchEvent(new Event('resize'));
        }, 120);
    });
}

export function cancelCanvasInteraction(stageId = 'publisher-stage') {
    const stage = document.getElementById(stageId);
    const state = stage ? canvasStates.get(stage) : null;
    if (state) resetPointerOperation(state, true);
}

export function clickElementById(id) {
    const element = document.getElementById(id);
    if (!element) throw new Error(`Element '${id}' is not available.`);
    if (element instanceof HTMLInputElement && element.type === 'file') {
        element.value = '';
        delete element.dataset.publisherDropX;
        delete element.dataset.publisherDropY;
    }
    element.click();
}

export function consumeCanvasInsertPlacement(id) {
    const element = document.getElementById(id);
    if (!(element instanceof HTMLInputElement)) return null;
    const x = Number.parseFloat(element.dataset.publisherDropX || '');
    const y = Number.parseFloat(element.dataset.publisherDropY || '');
    delete element.dataset.publisherDropX;
    delete element.dataset.publisherDropY;
    return Number.isFinite(x) && Number.isFinite(y) ? [x, y] : null;
}

window.publisherStudio = {
    setDocumentDirty(value) { publisherDocumentDirty = Boolean(value); },
    restorePublisherWorkspaceAfterExport(stageId = 'publisher-stage') {
        activeVideoExportCancel?.();
        document.querySelectorAll('.publisher-video-export-overlay').forEach(element => element.remove());
        const stage = document.getElementById(stageId);
        if (stage) {
            try { stage.focus({ preventScroll: true }); } catch { }
        }
    },
    cancelCanvasInteraction(stageId = 'publisher-stage') { cancelCanvasInteraction(stageId); },
    initializeStoryEditorLayout(shellId, hostId) { initializeStoryEditorLayout(shellId, hostId); },
    generateBarcodeSvg(options) { return generateBarcodeSvg(options); },
    exportPresentationVideo(containerSelector, fileName, title) { return exportPresentationVideo(containerSelector, fileName, title); },

    clickElement(id) { clickElementById(id); },
    consumeCanvasInsertPlacement(id) { return consumeCanvasInsertPlacement(id); },

    initializeWorkspace(id) {
        const workspace = document.getElementById(id);
        if (!workspace) return;
        if (!workspaceStates.has(workspace)) createWorkspaceState(workspace);
        bindWorkspaceSplitter(workspace, workspace.querySelector('[data-workspace-splitter="left"]'), 'left');
        bindWorkspaceSplitter(workspace, workspace.querySelector('[data-workspace-splitter="right"]'), 'right');
    },

    toggleWorkspacePane(id, side) {
        const workspace = document.getElementById(id);
        if (!workspace) return;
        const state = workspaceStates.get(workspace) || createWorkspaceState(workspace);
        if (side === 'left') state.leftCollapsed = !state.leftCollapsed;
        else state.rightCollapsed = !state.rightCollapsed;
        setWorkspaceColumns(workspace, state);
    },

    resetWorkspaceLayout(id) {
        const workspace = document.getElementById(id);
        if (!workspace) return;
        const state = { left: 172, right: 292, leftCollapsed: false, rightCollapsed: false };
        workspaceStates.set(workspace, state);
        setWorkspaceColumns(workspace, state);
    },

    previewPageAnimations(pageId) { previewPageAnimations(pageId); },
    previewElementAnimations(elementId) { previewElementAnimations(elementId); },
    previewAnimationStep(pageId, animationId) { previewAnimationStep(pageId, animationId); },
    stopAnimationPreview(pageId) { stopAnimationPreview(pageId); },
    playPublicationMedia(elementId) { playPublicationMedia(elementId); },
    pausePublicationMedia(elementId) { pausePublicationMedia(elementId); },

    async downloadStream(fileName, streamReference, mimeType) {
        const buffer = await streamReference.arrayBuffer();
        downloadBlob(fileName, new Blob([buffer], { type: mimeType || 'application/octet-stream' }));
    },

    async exportPage(pageId, fileName, format, dpi, zoom) {
        const page = document.getElementById(pageId);
        if (!page) throw new Error('The publication page is not available.');
        const pageKey = page.dataset.pageId || '';
        const exportSource = pageKey
            ? document.querySelector(`.print-publication > .print-page[data-page-id="${CSS.escape(pageKey)}"]`) || page
            : page;
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
        const blob = await canvasBlob(output, jpeg ? 'image/jpeg' : 'image/png', jpeg ? .92 : undefined);
        downloadBlob(fileName, blob);
    },

    async exportPublicationElement(elementId, fileName, format, dpi) {
        const id = String(elementId || '');
        if (!id) throw new Error('No publication object was selected.');
        const element = document.querySelector(`.print-publication [data-element-id="${CSS.escape(id)}"]`);
        if (!element) throw new Error('The selected object is not available on the export surface.');
        if (element.classList.contains('print-connector')) throw new Error('Connector-only export is not supported yet.');
        const page = element.closest('.print-page');
        if (!page) throw new Error('The selected object is not attached to a publication page.');
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
    },

    async exportPublicationPages(containerSelector, baseName, format, dpi) {
        const container = document.querySelector(containerSelector);
        if (!container) throw new Error('The publication export surface is not available.');
        const pages = [...container.querySelectorAll(':scope > .print-page')];
        if (!pages.length) throw new Error('The publication does not contain any pages.');
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
                if (index > 0) await new Promise(resolve => requestAnimationFrame(resolve));
                const canvas = await rasterizePageElement(pages[index], scale);
                const output = prepareOutputCanvas(canvas, jpeg);
                const blob = await canvasBlob(output, mimeType, jpeg ? .92 : undefined);
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
        downloadBlob(archiveName, await createStoredZip(files));
        return { count: files.length, fileName: archiveName };
    },

    async verifyPageRaster(pageId) {
        const page = document.getElementById(pageId);
        if (!page) throw new Error('The publication page is not available.');
        const canvas = await rasterizePageElement(page, 1, false);
        return { width: canvas.width, height: canvas.height, prefix: canvas.toDataURL('image/png').slice(0, 22) };
    },

    async makeColorTransparent(dataUrl, color, tolerance) {
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
    },

    async exportWebsite(fileName, title) {
        const source = document.querySelector('.print-publication');
        if (!source) throw new Error('The publication export surface is not available.');
        await document.fonts?.ready;
        await waitForImages(source);
        const publication = source.cloneNode(true);
        copyComputedStyles(source, publication);
        publication.removeAttribute('aria-hidden');
        publication.removeAttribute('style');
        publication.className = 'website-publication';
        normalizePublicationPageSizes(publication);
        const websitePages = [...publication.querySelectorAll(':scope > .print-page')];
        const websiteFrame = publicationFrameDefinition(websitePages, false);
        publication.dataset.frameWidthPx = String(websiteFrame.width);
        publication.dataset.frameHeightPx = String(websiteFrame.height);
        await inlineLocalMediaSources(publication);
        publication.querySelectorAll('img').forEach(image => {
            image.draggable = true;
            image.removeAttribute('aria-hidden');
        });
        const css = collectExportCss();
        const runtime = `(${websitePresentationRuntime.toString()})();`;
        const html = `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>${escapeHtml(title)}</title>
<style>${css}
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
.ps-interactive{cursor:pointer}
.ps-interactive:hover{outline:2px solid #48a7e8aa;outline-offset:2px}
.ps-action-hidden{visibility:hidden!important;pointer-events:none!important}
.ps-controls{position:fixed;z-index:100000;left:50%;bottom:14px;display:flex;align-items:center;gap:7px;min-height:38px;padding:6px 9px;border:1px solid #ffffff38;border-radius:999px;background:#111827dd;box-shadow:0 6px 24px #0009;transform:translateX(-50%);backdrop-filter:blur(10px)}
.ps-controls[hidden]{display:none!important}
.ps-controls button{display:grid;place-items:center;width:31px;height:31px;padding:0;border:1px solid #ffffff38;border-radius:50%;color:#fff;background:#ffffff12;font:600 18px/1 Segoe UI,system-ui,sans-serif;cursor:pointer}
.ps-controls button:hover{background:#ffffff2c}
.ps-controls button:disabled{opacity:.35;cursor:default}
.ps-controls span{min-width:58px;color:#e5e7eb;text-align:center;font-size:12px}
@media (prefers-reduced-motion:reduce){.ps-slide,.website-publication [data-publication-element]{animation-duration:.001ms!important;animation-delay:0ms!important}}
@media print{
html,body{width:auto;height:auto;overflow:visible!important;background:#fff!important}
.website-publication{position:static;display:block!important;overflow:visible}
.ps-stage{position:static;width:auto!important;height:auto!important;overflow:visible;transform:none!important;box-shadow:none}
.ps-slide,.ps-slide[hidden]{position:relative;display:block!important;inset:auto;overflow:hidden;break-after:page}
.website-publication .print-page{position:relative;left:auto;top:auto;margin:0 auto;box-shadow:none;transform:none!important}
.ps-controls{display:none!important}
}
</style>
</head>
<body>${publication.outerHTML}<script>${runtime}</script></body>
</html>`;
        downloadBlob(fileName, new Blob([html], { type: 'text/html;charset=utf-8' }));
    },

    async printPublication() {
        const active = document.activeElement;
        try { active?.blur?.(); } catch { }
        document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
        document.body.classList.add('publisher-printing');
        await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
        const cleanup = () => document.body.classList.remove('publisher-printing');
        window.addEventListener('afterprint', cleanup, { once: true });
        try { window.print(); } finally { setTimeout(cleanup, 1500); }
    }
};
