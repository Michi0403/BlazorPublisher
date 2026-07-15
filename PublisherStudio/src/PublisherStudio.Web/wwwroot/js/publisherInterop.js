const canvasStates = new WeakMap();

function number(value, fallback = 0) {
    const parsed = Number.parseFloat(value);
    return Number.isFinite(parsed) ? parsed : fallback;
}

function snap(value, candidates, tolerance = 2) {
    let result = Math.round(value * 2) / 2;
    let distance = tolerance;
    for (const candidate of candidates) {
        const current = Math.abs(value - candidate);
        if (current < distance) { result = candidate; distance = current; }
    }
    return result;
}

function elementMm(element, pxPerMm) {
    return {
        x: number(element.style.left) / pxPerMm,
        y: number(element.style.top) / pxPerMm,
        width: number(element.style.width) / pxPerMm,
        height: number(element.style.height) / pxPerMm
    };
}

export function initializeCanvas(pageId, dotnet, pxPerMm, cropMode) {
    const page = document.getElementById(pageId);
    if (!page) return;
    let state = canvasStates.get(page);
    if (!state) {
        state = { dotnet, pxPerMm, cropMode, operation: null };
        page.addEventListener('pointerdown', event => pointerDown(page, state, event));
        page.addEventListener('pointermove', event => pointerMove(page, state, event));
        page.addEventListener('pointerup', event => pointerUp(page, state, event));
        page.addEventListener('pointercancel', event => pointerUp(page, state, event));
        canvasStates.set(page, state);
    }
    state.dotnet = dotnet;
    state.pxPerMm = pxPerMm;
    state.cropMode = cropMode;
}

function pointerDown(page, state, event) {
    const element = event.target.closest('[data-publication-element]');
    if (!element || !page.contains(element)) return;
    const id = element.dataset.elementId;
    state.dotnet.invokeMethodAsync('SelectElement', id);
    if (element.classList.contains('locked')) return;

    const handle = event.target.closest('[data-resize-handle]');
    const image = element.querySelector('img');
    const bounds = elementMm(element, state.pxPerMm);
    const base = {
        id, element, pointerId: event.pointerId,
        startX: event.clientX, startY: event.clientY,
        ...bounds
    };

    if (state.cropMode && image && !handle) {
        state.operation = {
            ...base, kind: 'crop', image,
            cropX: number(image.dataset.cropX), cropY: number(image.dataset.cropY),
            cropScale: number(image.dataset.cropScale, 1), flipX: number(image.dataset.flipX, 1), flipY: number(image.dataset.flipY, 1)
        };
    } else if (handle) {
        state.operation = { ...base, kind: 'resize', handle: handle.dataset.resizeHandle };
    } else {
        state.operation = { ...base, kind: 'move' };
    }
    element.setPointerCapture(event.pointerId);
    event.preventDefault();
}

function pointerMove(page, state, event) {
    const op = state.operation;
    if (!op || op.pointerId !== event.pointerId) return;
    const dx = (event.clientX - op.startX) / state.pxPerMm;
    const dy = (event.clientY - op.startY) / state.pxPerMm;

    if (op.kind === 'crop') {
        const cropX = Math.max(-100, Math.min(100, op.cropX + dx / Math.max(op.width, 1) * 100));
        const cropY = Math.max(-100, Math.min(100, op.cropY + dy / Math.max(op.height, 1) * 100));
        op.currentCropX = cropX; op.currentCropY = cropY;
        op.image.style.transform = `translate(${cropX}%, ${cropY}%) scale(${op.cropScale * op.flipX}, ${op.cropScale * op.flipY})`;
        return;
    }

    const pageWidth = page.clientWidth / state.pxPerMm;
    const pageHeight = page.clientHeight / state.pxPerMm;
    const verticalGuides = [...page.querySelectorAll('.guide-line.vertical')].map(line => number(line.style.left) / state.pxPerMm);
    const horizontalGuides = [...page.querySelectorAll('.guide-line.horizontal')].map(line => number(line.style.top) / state.pxPerMm);
    const xCandidates = [0, pageWidth / 2, pageWidth, ...verticalGuides];
    const yCandidates = [0, pageHeight / 2, pageHeight, ...horizontalGuides];
    let x = op.x, y = op.y, width = op.width, height = op.height;

    if (op.kind === 'move') {
        x = snap(op.x + dx, [...xCandidates, pageWidth / 2 - op.width / 2, pageWidth - op.width]);
        y = snap(op.y + dy, [...yCandidates, pageHeight / 2 - op.height / 2, pageHeight - op.height]);
    } else {
        const h = op.handle;
        if (h.includes('e')) width = Math.max(2, snap(op.width + dx, []));
        if (h.includes('s')) height = Math.max(2, snap(op.height + dy, []));
        if (h.includes('w')) { x = snap(op.x + dx, xCandidates); width = Math.max(2, op.width - (x - op.x)); }
        if (h.includes('n')) { y = snap(op.y + dy, yCandidates); height = Math.max(2, op.height - (y - op.y)); }
    }
    op.current = { x, y, width, height };
    op.element.style.left = `${x * state.pxPerMm}px`;
    op.element.style.top = `${y * state.pxPerMm}px`;
    op.element.style.width = `${width * state.pxPerMm}px`;
    op.element.style.height = `${height * state.pxPerMm}px`;
}

function pointerUp(page, state, event) {
    const op = state.operation;
    if (!op || op.pointerId !== event.pointerId) return;
    state.operation = null;
    if (op.kind === 'crop') {
        state.dotnet.invokeMethodAsync('CommitCrop', op.id, op.currentCropX ?? op.cropX, op.currentCropY ?? op.cropY);
    } else {
        const value = op.current ?? { x: op.x, y: op.y, width: op.width, height: op.height };
        state.dotnet.invokeMethodAsync('CommitBounds', op.id, value.x, value.y, value.width, value.height);
    }
}

window.publisherStudio = {
    clickElement(id) { document.getElementById(id)?.click(); },
    async downloadStream(fileName, streamReference, mimeType) {
        const buffer = await streamReference.arrayBuffer();
        const blob = new Blob([buffer], { type: mimeType || 'application/octet-stream' });
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = fileName;
        anchor.click();
        anchor.remove();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    },
    printPublication() { window.print(); }
};
