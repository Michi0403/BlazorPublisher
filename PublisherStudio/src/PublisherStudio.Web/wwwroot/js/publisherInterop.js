const canvasStates = new WeakMap();
const boundRulers = new WeakSet();
const wordArtPathStates = new WeakMap();
const PX_PER_MM_AT_96_DPI = 96 / 25.4;

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
    if (state.drawPending) return;
    state.drawPending = true;
    requestAnimationFrame(() => {
        state.drawPending = false;
        drawRulers(state);
    });
}

export function initializeCanvas(stageId, scrollId, pageId, horizontalRulerId, verticalRulerId, dotnet, config) {
    const stage = document.getElementById(stageId);
    const scroll = document.getElementById(scrollId);
    const page = document.getElementById(pageId);
    if (!stage || !scroll || !page) return;

    let state = canvasStates.get(stage);
    if (!state) {
        state = {
            stage,
            scroll,
            page,
            dotnet,
            config,
            operation: null,
            cursorX: null,
            cursorY: null,
            drawPending: false,
            cropTimers: new Map(),
            connectorGhost: null
        };

        stage.addEventListener('pointerdown', event => pointerDown(state, event));
        stage.addEventListener('pointermove', event => pointerMove(state, event));
        stage.addEventListener('pointerup', event => pointerUp(state, event));
        stage.addEventListener('pointercancel', event => pointerUp(state, event));
        stage.addEventListener('pointerleave', () => {
            state.cursorX = null;
            state.cursorY = null;
            nextAnimationFrame(state);
        });
        stage.addEventListener('wheel', event => cropWheel(state, event), { passive: false });
        stage.addEventListener('keydown', event => canvasKeyDown(state, event));
        scroll.addEventListener('scroll', () => nextAnimationFrame(state), { passive: true });

        state.resizeObserver = new ResizeObserver(() => nextAnimationFrame(state));
        state.resizeObserver.observe(stage);
        state.resizeObserver.observe(scroll);
        canvasStates.set(stage, state);
    }

    state.scroll = scroll;
    state.page = page;
    state.dotnet = dotnet;
    state.config = config;
    state.horizontalRuler = document.getElementById(horizontalRulerId);
    state.verticalRuler = document.getElementById(verticalRulerId);

    bindRuler(state.horizontalRuler, 'Horizontal', state);
    bindRuler(state.verticalRuler, 'Vertical', state);
    nextAnimationFrame(state);
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
    state.dotnet.invokeMethodAsync('CancelActiveTool');
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
            state.dotnet.invokeMethodAsync(
                'CommitConnector',
                operation.sourceOwnerId, operation.sourceAnchor,
                target.ownerId, target.anchor, operation.tool);
        } else {
            state.dotnet.invokeMethodAsync(
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

function pointerDown(state, event) {
    if (event.button !== 0 || event.target.closest('.ruler-canvas,.corner-ruler')) return;
    state.stage.focus({ preventScroll: true });

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
        state.stage.setPointerCapture(event.pointerId);
        event.preventDefault();
        event.stopPropagation();
        return;
    }

    const connectorPort = event.target.closest('[data-connector-port]');
    if (connectorPort && state.page.contains(connectorPort) && connectorToolActive(state)) {
        const sourceOwnerId = connectorPort.dataset.ownerId;
        connectorPort.classList.add('connector-port-source');
        state.operation = {
            kind: 'connector-new', pointerId: event.pointerId, sourcePort: connectorPort,
            sourceOwnerId, sourceAnchor: connectorPort.dataset.anchor, fixedPoint: portPointMm(state, connectorPort),
            pathKind: 'Curved', markerEnd: state.config.connectorTool === 'Arrow', tool: state.config.connectorTool,
            excludedIds: [sourceOwnerId]
        };
        state.stage.setPointerCapture(event.pointerId);
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
    if (!element || !state.page.contains(element)) return;

    const id = element.dataset.elementId;
    state.dotnet.invokeMethodAsync('SelectElement', id);
    if (element.classList.contains('locked')) return;

    const handle = event.target.closest('[data-resize-handle]');
    const image = element.querySelector('img');
    const bounds = elementMm(element, state.config.pxPerMm);
    const base = {
        id,
        element,
        pointerId: event.pointerId,
        startX: event.clientX,
        startY: event.clientY,
        ...bounds
    };

    if (state.config.cropMode && image && !handle) {
        state.operation = {
            ...base,
            kind: 'crop',
            image,
            cropX: number(image.dataset.cropX),
            cropY: number(image.dataset.cropY),
            cropScale: number(image.dataset.cropScale, 1),
            imageRotation: number(image.dataset.imageRotation),
            flipX: number(image.dataset.flipX, 1),
            flipY: number(image.dataset.flipY, 1)
        };
    } else if (handle) {
        state.operation = { ...base, kind: 'resize', handle: handle.dataset.resizeHandle };
    } else {
        state.operation = { ...base, kind: 'move' };
    }

    element.setPointerCapture(event.pointerId);
    event.preventDefault();
}

function pointerMove(state, event) {
    const stageRect = state.stage.getBoundingClientRect();
    state.cursorX = event.clientX - stageRect.left;
    state.cursorY = event.clientY - stageRect.top;
    nextAnimationFrame(state);

    const operation = state.operation;
    if (!operation || operation.pointerId !== event.pointerId) return;

    if (operation.kind === 'connector-new' || operation.kind === 'connector-reconnect') {
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

    const dx = (event.clientX - operation.startX) / state.config.pxPerMm;
    const dy = (event.clientY - operation.startY) / state.config.pxPerMm;

    if (operation.kind === 'crop') {
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
        x = snapAxis(operation.x + dx, operation.width, pageWidth, verticalGuides, state.config);
        y = snapAxis(operation.y + dy, operation.height, pageHeight, horizontalGuides, state.config);
    } else {
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
    }

    operation.current = { x, y, width, height };
    operation.element.style.left = `${x * state.config.pxPerMm}px`;
    operation.element.style.top = `${y * state.config.pxPerMm}px`;
    operation.element.style.width = `${width * state.config.pxPerMm}px`;
    operation.element.style.height = `${height * state.config.pxPerMm}px`;
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
    if (operation.kind === 'connector-new' || operation.kind === 'connector-reconnect') {
        finishConnectorDrag(state, operation);
        return;
    }
    state.operation = null;

    if (operation.kind === 'guide') {
        const max = operation.orientation === 'Horizontal'
            ? number(state.page.dataset.pageHeightMm)
            : number(state.page.dataset.pageWidthMm);
        const position = operation.currentPosition ?? operation.position;
        if (position < -10 || position > max + 10)
            state.dotnet.invokeMethodAsync('DeleteGuide', operation.id);
        else
            state.dotnet.invokeMethodAsync('CommitGuide', operation.id, clamp(position, 0, max));
        return;
    }

    if (operation.kind === 'crop') {
        state.dotnet.invokeMethodAsync(
            'CommitCrop',
            operation.id,
            operation.currentCropX ?? operation.cropX,
            operation.currentCropY ?? operation.cropY,
            operation.cropScale);
    } else {
        const value = operation.current ?? { x: operation.x, y: operation.y, width: operation.width, height: operation.height };
        state.dotnet.invokeMethodAsync('CommitBounds', operation.id, value.x, value.y, value.width, value.height);
    }
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
        state.dotnet.invokeMethodAsync('CommitCrop', id, cropX, cropY, nextScale);
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
    context.setTransform(ratio, 0, 0, ratio, 0, 0);
    return { context, rect };
}

function drawRuler(state, canvas, horizontal) {
    const { context, rect } = configureCanvas(canvas);
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
        const href = sheet.href || '';
        if (href && !href.includes('/css/site.css')) continue;
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

function copyComputedStyles(source, clone) {
    if (!(source instanceof Element) || !(clone instanceof Element)) return;
    const computed = getComputedStyle(source);
    const important = [
        'position','display','left','top','right','bottom','width','height','box-sizing','overflow',
        'background','background-color','border','border-radius','box-shadow','opacity','filter',
        'transform','transform-origin','object-fit','object-position','color','font','font-family',
        'font-size','font-weight','font-style','line-height','letter-spacing','text-align','text-decoration',
        'white-space','word-break','padding','margin','z-index','clip-path','isolation','mix-blend-mode',
        'paint-order','stroke','stroke-width','fill'
    ];
    let inline = clone.getAttribute('style') || '';
    for (const property of important) {
        const value = computed.getPropertyValue(property);
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
    return clone;
}

async function pageSvg(page) {
    await document.fonts?.ready;
    await waitForImages(page);
    const rect = page.getBoundingClientRect();
    const clone = cleanPageClone(page);
    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg.setAttribute('xmlns', 'http://www.w3.org/2000/svg');
    svg.setAttribute('xmlns:xlink', 'http://www.w3.org/1999/xlink');
    svg.setAttribute('width', String(rect.width));
    svg.setAttribute('height', String(rect.height));
    svg.setAttribute('viewBox', `0 0 ${rect.width} ${rect.height}`);

    const foreignObject = document.createElementNS('http://www.w3.org/2000/svg', 'foreignObject');
    foreignObject.setAttribute('x', '0');
    foreignObject.setAttribute('y', '0');
    foreignObject.setAttribute('width', String(rect.width));
    foreignObject.setAttribute('height', String(rect.height));

    const host = document.createElement('div');
    host.setAttribute('xmlns', 'http://www.w3.org/1999/xhtml');
    host.style.width = `${rect.width}px`;
    host.style.height = `${rect.height}px`;
    host.style.margin = '0';
    host.style.padding = '0';
    host.appendChild(clone);
    foreignObject.appendChild(host);
    svg.appendChild(foreignObject);
    return { text: new XMLSerializer().serializeToString(svg), width: rect.width, height: rect.height };
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
function publicationReducedMotion() { return typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches; }
function publicationAnimationSpan(animation) {
    if (publicationReducedMotion()) return .001;
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
function runPublicationAnimation(node, animation, delaySeconds = 0) {
    const reducedMotion = publicationReducedMotion();
    const duration = (reducedMotion ? .001 : Math.max(.05, animationNumber(animation.durationSeconds, .6))) * 1000;
    const repeat = Math.max(1, Math.round(animationNumber(animation.repeatCount, 1)));
    const iterations = reducedMotion ? 1 : repeat * (animation.autoReverse ? 2 : 1);
    return node.animate(publicationAnimationFrames(node, animation), {
        duration,
        delay: (reducedMotion ? 0 : Math.max(0, delaySeconds)) * 1000,
        easing: animationEasing(animation.easing),
        iterations,
        direction: animation.autoReverse ? 'alternate' : 'normal',
        fill: animationName(animation.phase) === 'entrance' ? 'both' : 'forwards'
    });
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
function runPublicationPageTransition(page, entering = true) {
    const duration = (publicationReducedMotion() ? .001 : Math.max(.1, animationNumber(page.dataset.transitionDuration, .55))) * 1000;
    return page.animate(publicationPageTransitionFrames(page, entering), {
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
        if (trigger === 'withprevious') start = previousStart + ownDelay;
        else if (trigger === 'afterprevious') start = previousEnd + ownDelay;
        state.animations.push(runPublicationAnimation(item.node, item.animation, start));
        previousStart = start;
        previousEnd = start + publicationAnimationSpan(item.animation);
    }
}
function previewPublicationItems(root, items, includeTransition) {
    clearPublicationPreview(root.id || root);
    const state = { root, animations: [], clickTarget: root, clickHandler: null, clickGroups: [] };
    publicationAnimationPreviews.set(root.id || root, state);
    root.classList.add('pub-animation-previewing');
    if (includeTransition) state.animations.push(runPublicationPageTransition(root, true));

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
    schedulePublicationPreviewGroup(state, automatic, includeTransition && !publicationReducedMotion() ? animationNumber(root.dataset.transitionDuration, .55) : 0);

    if (state.clickGroups.length) {
        root.classList.add('pub-animation-click-hint');
        state.clickHandler = event => {
            if (!state.clickGroups.length) return;
            event.preventDefault();
            event.stopImmediatePropagation();
            schedulePublicationPreviewGroup(state, state.clickGroups.shift(), 0);
            if (!state.clickGroups.length) root.classList.remove('pub-animation-click-hint');
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


function websitePresentationRuntime() {
    const publication = document.querySelector('.website-publication');
    if (!publication) return;
    const pages = [...publication.querySelectorAll(':scope > .print-page')];
    if (!pages.length) return;
    const lower = value => String(value || '').replace(/[^a-z0-9]/gi, '').toLowerCase();
    const num = (value, fallback) => { const parsed = Number(value); return Number.isFinite(parsed) ? parsed : fallback; };
    const bool = value => String(value).toLowerCase() === 'true';
    const parse = (value, fallback) => { try { return JSON.parse(value || ''); } catch { return fallback; } };
    const reducedMotion = typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;
    const animationSpan = animation => reducedMotion ? .001 : Math.max(.05, num(animation.durationSeconds, .6))
        * Math.max(1, num(animation.repeatCount, 1)) * (animation.autoReverse ? 2 : 1);
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
    const playItem = (item, delay = 0) => {
        item.prestate?.cancel();
        item.prestate = null;
        if (lower(item.animation.phase) === 'entrance') item.node.classList.remove('ps-action-hidden');
        const repeat = Math.max(1, Math.round(num(item.animation.repeatCount, 1)));
        const animation = item.node.animate(frames(item.node, item.animation), {
            duration: (reducedMotion ? .001 : Math.max(.05, num(item.animation.durationSeconds, .6))) * 1000,
            delay: (reducedMotion ? 0 : Math.max(0, delay)) * 1000,
            easing: easing(item.animation.easing),
            iterations: reducedMotion ? 1 : repeat * (item.animation.autoReverse ? 2 : 1),
            direction: item.animation.autoReverse ? 'alternate' : 'normal',
            fill: lower(item.animation.phase) === 'entrance' ? 'both' : 'forwards'
        });
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
            if (trigger === 'withprevious') start = previousStart + ownDelay;
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
            item.prestate = item.node.animate(frames(item.node, item.animation), { duration: 1, fill: 'both' });
            item.prestate.pause();
            item.prestate.currentTime = 0;
            activeAnimations.push(item.prestate);
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
        clickGroups = [];
    };
    const fitPages = () => {
        const controlsHeight = controls && !controls.hidden ? 62 : 18;
        for (const page of pages) {
            const width = page.offsetWidth || 1;
            const height = page.offsetHeight || 1;
            const scale = Math.min((innerWidth - 32) / width, (innerHeight - controlsHeight - 24) / height, 1.75);
            page.style.transform = `scale(${Math.max(.05, scale)})`;
        }
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
        if (shouldRun) scheduleGroup(timeline.automatic);
        else primeClickEntrances([timeline.automatic]);
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

    const shells = pages.map(page => {
        const shell = document.createElement('div');
        shell.className = 'ps-slide';
        page.before(shell);
        shell.appendChild(page);
        shell.hidden = true;
        return shell;
    });
    const showControls = bool(publication.dataset.playbackControls);
    const loop = bool(publication.dataset.playbackLoop);
    const startAutomatically = publication.dataset.playbackStart !== 'false';
    let current = 0;
    let activeAnimations = [];
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
            if (lower(interaction.action) === 'none' || !interaction.action) continue;
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
                    }
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
    runCurrentPage(startAutomatically);
}

window.publisherStudio = {
    clickElement(id) {
        const element = document.getElementById(id);
        if (!element) return;
        if (element instanceof HTMLInputElement && element.type === 'file') element.value = '';
        element.click();
    },

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

    async downloadStream(fileName, streamReference, mimeType) {
        const buffer = await streamReference.arrayBuffer();
        downloadBlob(fileName, new Blob([buffer], { type: mimeType || 'application/octet-stream' }));
    },

    async exportPage(pageId, fileName, format, dpi, zoom) {
        const page = document.getElementById(pageId);
        if (!page) throw new Error('The publication page is not available.');
        const serialized = await pageSvg(page);
        const normalized = String(format).toLowerCase();

        if (normalized === 'svg') {
            downloadBlob(fileName, new Blob([serialized.text], { type: 'image/svg+xml;charset=utf-8' }));
            return;
        }

        const jpeg = normalized === 'jpeg' || normalized === 'jpg';
        const scale = clamp(number(dpi, 150) / (96 * Math.max(number(zoom, 1), .05)), .5, 12);
        const canvas = await svgToCanvas(serialized.text, serialized.width, serialized.height, scale, jpeg);
        const blob = await canvasBlob(canvas, jpeg ? 'image/jpeg' : 'image/png', jpeg ? .92 : undefined);
        downloadBlob(fileName, blob);
    },

    async verifyPageRaster(pageId) {
        const page = document.getElementById(pageId);
        if (!page) throw new Error('The publication page is not available.');
        const serialized = await pageSvg(page);
        const canvas = await svgToCanvas(serialized.text, serialized.width, serialized.height, 1, false);
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

    exportWebsite(fileName, title) {
        const source = document.querySelector('.print-publication');
        if (!source) throw new Error('The publication export surface is not available.');
        const publication = source.cloneNode(true);
        publication.removeAttribute('aria-hidden');
        publication.className = 'website-publication';
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
body{margin:0;font-family:Segoe UI,system-ui,sans-serif;user-select:none}
.website-publication{position:fixed;inset:0;display:block!important;overflow:hidden;visibility:visible!important;pointer-events:auto!important}
.ps-slide{position:absolute;inset:0;display:grid;place-items:center;overflow:hidden;transform-origin:center;will-change:transform,opacity,clip-path}
.ps-slide[hidden]{display:none!important}
.website-publication .print-page{position:relative;overflow:hidden;margin:0;box-shadow:0 10px 48px #000a;background-color:#fff;transform-origin:center;will-change:transform}
.website-publication .print-element{position:absolute;transform-origin:center}
.website-publication .print-connector{position:absolute;inset:0;width:100%;height:100%;overflow:visible;transform-box:fill-box;transform-origin:center}
.website-publication .print-connector.ps-interactive{pointer-events:none}
.website-publication .print-connector.ps-interactive .connector-hit{pointer-events:stroke;cursor:pointer}
.website-publication .print-connector.ps-interactive:hover{outline:none}
.website-publication .print-connector.ps-interactive:hover .connector-line{filter:drop-shadow(0 0 2px #48a7e8)}
.website-publication .text-frame-content,.website-publication .image-frame,.website-publication .shape,.website-publication .wordart-svg{width:100%;height:100%;overflow:hidden}
.website-publication img{display:block;width:100%;height:100%;max-width:none;transform-origin:center}
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
.website-publication{position:static;overflow:visible}
.ps-slide,.ps-slide[hidden]{position:relative;display:block!important;inset:auto;overflow:hidden;break-after:page}
.website-publication .print-page{margin:0 auto;box-shadow:none;transform:none!important}
.ps-controls{display:none!important}
}
</style>
</head>
<body>${publication.outerHTML}<script>${runtime}</script></body>
</html>`;
        downloadBlob(fileName, new Blob([html], { type: 'text/html;charset=utf-8' }));
    },

    printPublication() { window.print(); }
};
