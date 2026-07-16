const canvasStates = new WeakMap();
const boundRulers = new WeakSet();
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
            cropTimers: new Map()
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

function pointerDown(state, event) {
    if (event.button !== 0 || event.target.closest('.ruler-canvas,.corner-ruler')) return;

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
            for (const rule of sheet.cssRules) css += `${rule.cssText}\n`;
        } catch {
            // Cross-origin component styles are not required for publication page export.
        }
    }
    return css;
}

function cleanPageClone(page) {
    const clone = page.cloneNode(true);
    clone.removeAttribute('id');
    clone.classList.remove('crop-mode');
    clone.style.margin = '0';
    clone.style.boxShadow = 'none';
    clone.style.backgroundImage = 'none';
    clone.querySelectorAll('.selection-handle,.guide-line,.crop-thirds,.crop-help').forEach(item => item.remove());
    clone.querySelectorAll('.selected').forEach(item => item.classList.remove('selected'));
    clone.querySelectorAll('[id]').forEach(item => item.removeAttribute('id'));
    return clone;
}

function pageSvg(page) {
    const rect = page.getBoundingClientRect();
    const clone = cleanPageClone(page);
    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg.setAttribute('xmlns', 'http://www.w3.org/2000/svg');
    svg.setAttribute('width', String(rect.width));
    svg.setAttribute('height', String(rect.height));
    svg.setAttribute('viewBox', `0 0 ${rect.width} ${rect.height}`);

    const foreignObject = document.createElementNS('http://www.w3.org/2000/svg', 'foreignObject');
    foreignObject.setAttribute('x', '0');
    foreignObject.setAttribute('y', '0');
    foreignObject.setAttribute('width', '100%');
    foreignObject.setAttribute('height', '100%');

    const host = document.createElement('div');
    host.setAttribute('xmlns', 'http://www.w3.org/1999/xhtml');
    host.style.width = `${rect.width}px`;
    host.style.height = `${rect.height}px`;
    const style = document.createElement('style');
    style.textContent = collectExportCss();
    host.appendChild(style);
    host.appendChild(clone);
    foreignObject.appendChild(host);
    svg.appendChild(foreignObject);
    return { text: new XMLSerializer().serializeToString(svg), width: rect.width, height: rect.height };
}

async function svgToCanvas(svgText, width, height, scale) {
    await document.fonts?.ready;
    const blob = new Blob([svgText], { type: 'image/svg+xml;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    try {
        const image = new Image();
        await new Promise((resolve, reject) => {
            image.onload = resolve;
            image.onerror = () => reject(new Error('The browser could not render the publication page.'));
            image.src = url;
        });
        const canvas = document.createElement('canvas');
        canvas.width = Math.max(1, Math.round(width * scale));
        canvas.height = Math.max(1, Math.round(height * scale));
        const context = canvas.getContext('2d');
        context.setTransform(scale, 0, 0, scale, 0, 0);
        context.drawImage(image, 0, 0, width, height);
        return canvas;
    } finally {
        URL.revokeObjectURL(url);
    }
}

function canvasBlob(canvas, mimeType, quality) {
    return new Promise((resolve, reject) => {
        canvas.toBlob(blob => blob ? resolve(blob) : reject(new Error('The browser could not create the image file.')), mimeType, quality);
    });
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

window.publisherStudio = {
    clickElement(id) {
        const element = document.getElementById(id);
        if (!element) return;
        if (element instanceof HTMLInputElement && element.type === 'file')
            element.value = '';
        element.click();
    },

    async downloadStream(fileName, streamReference, mimeType) {
        const buffer = await streamReference.arrayBuffer();
        downloadBlob(fileName, new Blob([buffer], { type: mimeType || 'application/octet-stream' }));
    },

    async exportPage(pageId, fileName, format, dpi, zoom) {
        const page = document.getElementById(pageId);
        if (!page) throw new Error('The publication page is not available.');
        const serialized = pageSvg(page);
        const normalized = String(format).toLowerCase();

        if (normalized === 'svg') {
            downloadBlob(fileName, new Blob([serialized.text], { type: 'image/svg+xml;charset=utf-8' }));
            return;
        }

        const scale = clamp(number(dpi, 150) / (96 * Math.max(number(zoom, 1), .05)), .5, 12);
        const canvas = await svgToCanvas(serialized.text, serialized.width, serialized.height, scale);
        const jpeg = normalized === 'jpeg' || normalized === 'jpg';
        const blob = await canvasBlob(canvas, jpeg ? 'image/jpeg' : 'image/png', jpeg ? .92 : undefined);
        downloadBlob(fileName, blob);
    },

    exportWebsite(fileName, title) {
        const source = document.querySelector('.print-publication');
        if (!source) throw new Error('The publication export surface is not available.');
        const publication = source.cloneNode(true);
        publication.removeAttribute('aria-hidden');
        publication.className = 'website-publication';
        const css = collectExportCss();
        const html = `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>${escapeHtml(title)}</title>
<style>${css}
html,body{min-height:100%;overflow:auto!important;background:#d4d7dc!important}
body{margin:0;padding:24px;font-family:Segoe UI,system-ui,sans-serif}
.website-publication{display:block!important}
.website-publication .print-page{position:relative;overflow:hidden;margin:0 auto 24px;box-shadow:0 4px 24px #0005;background:#fff}
.website-publication .print-element{position:absolute;transform-origin:center}
.website-publication .text-frame-content,.website-publication .image-frame,.website-publication .shape{width:100%;height:100%;overflow:hidden}
.website-publication img{display:block;width:100%;height:100%;max-width:none;transform-origin:center}
@media print{body{padding:0;background:#fff!important}.website-publication .print-page{margin:0 auto;box-shadow:none;break-after:page}}
</style>
</head>
<body>${publication.outerHTML}</body>
</html>`;
        downloadBlob(fileName, new Blob([html], { type: 'text/html;charset=utf-8' }));
    },

    printPublication() {
        window.print();
    }
};
