const studioStates = new Map();
const RECORDING_TRANSFER_CHUNK_SIZE = 18 * 1024;

function baseMimeType(value, fallback = 'application/octet-stream') {
    const mimeType = String(value || '').split(';', 1)[0].trim().toLowerCase();
    return mimeType.includes('/') ? mimeType : fallback;
}

function normalizeMediaDataUrl(dataUrl, fallbackMimeType = 'application/octet-stream') {
    const value = String(dataUrl || '');
    if (!value.startsWith('data:')) return value;
    const marker = value.toLowerCase().lastIndexOf(';base64,');
    if (marker < 0) return value;
    const mimeType = baseMimeType(value.slice(5, marker), fallbackMimeType);
    return `data:${mimeType};base64,${value.slice(marker + 8)}`;
}

function mediaDropKind(file) {
    const name = String(file?.name || '').toLowerCase();
    const mime = baseMimeType(file?.type || '', '');
    if (/\.(otio|otioz|mlt|kdenlive|xges|osp|edl)$/.test(name)) return 'project';
    if (mime.startsWith('video/') || /\.(mp4|m4v|webm|ogv|mov)$/.test(name)) return 'video';
    if (mime.startsWith('audio/') || /\.(mp3|wav|oga|ogg|m4a|aac|flac)$/.test(name)) return 'audio';
    return '';
}

function assignMediaDrop(inputId, file) {
    const input = document.getElementById(inputId);
    if (!(input instanceof HTMLInputElement) || input.type !== 'file' || !(file instanceof File)) return false;
    const transfer = new DataTransfer();
    transfer.items.add(file);
    input.value = '';
    input.files = transfer.files;
    input.dispatchEvent(new Event('change', { bubbles: true }));
    return true;
}

function releaseMediaDropBindings(state) {
    const root = state?.dropRoot;
    const handlers = state?.dropHandlers;
    if (root && handlers) {
        root.removeEventListener('dragenter', handlers.dragenter);
        root.removeEventListener('dragover', handlers.dragover);
        root.removeEventListener('dragleave', handlers.dragleave);
        root.removeEventListener('drop', handlers.drop);
        root.classList.remove('media-file-drag-active');
        root.removeAttribute('data-media-drop-mode');
    }
    if (state) {
        const timeOverlay = document.getElementById(state.timeOverlayId || '');
        timeOverlay?.classList.remove('media-video-drop-target');
        timeOverlay?.style.removeProperty('--video-drop-position');
        state.dropRoot = null;
        state.dropHandlers = null;
        state.dropDepth = 0;
    }
}

function bindMediaDrop(state, rootId, sourceInputId, insertInputId, projectInputId, expectedKind) {
    releaseMediaDropBindings(state);
    const root = document.getElementById(rootId);
    if (!root) return;
    const descriptor = event => {
        const file = event.dataTransfer?.files?.[0];
        if (file) return file;
        const item = [...(event.dataTransfer?.items || [])].find(candidate => candidate.kind === 'file');
        return item ? { name: '', type: item.type || '' } : null;
    };
    const modeAt = target => target?.closest?.('.media-sequence-editor,.media-range-selector,.media-video-time-overlay') ? 'insert' : 'replace';
    const videoInsertionAt = (target, clientX) => {
        const overlay = target?.closest?.('.media-video-time-overlay');
        if (!overlay) return null;
        const duration = Math.max(.01, Number(overlay.dataset.duration) || 0);
        const pointSelection = overlay.dataset.selectionPoint === 'true';
        const selectionStart = Math.max(0, Math.min(duration, Number(overlay.dataset.selectionStart) || 0));
        const selectionEnd = Math.max(selectionStart, Math.min(duration, Number(overlay.dataset.selectionEnd) || selectionStart));
        const bounds = overlay.getBoundingClientRect();
        const raw = Math.max(0, Math.min(duration, ((Number(clientX) - bounds.left) / Math.max(1, bounds.width)) * duration));
        const seconds = pointSelection ? selectionStart : Math.max(selectionStart, Math.min(selectionEnd, raw));
        overlay.style.setProperty('--video-drop-position', `${Math.max(0, Math.min(100, seconds / duration * 100))}%`);
        overlay.classList.add('media-video-drop-target');
        return { seconds, pointSelection };
    };
    const clearVideoInsertion = () => {
        const overlay = document.getElementById(state.timeOverlayId || '');
        overlay?.classList.remove('media-video-drop-target');
        overlay?.style.removeProperty('--video-drop-position');
    };
    const show = mode => {
        root.classList.add('media-file-drag-active');
        root.dataset.mediaDropMode = mode;
    };
    const clear = () => {
        state.dropDepth = 0;
        root.classList.remove('media-file-drag-active');
        root.removeAttribute('data-media-drop-mode');
        clearVideoInsertion();
    };
    const handlers = {
        dragenter: event => {
            const file = descriptor(event);
            if (!file) return;
            event.preventDefault();
            state.dropDepth++;
            show(modeAt(event.target));
        },
        dragover: event => {
            const file = descriptor(event);
            if (!file) return;
            event.preventDefault();
            event.stopPropagation();
            const kind = mediaDropKind(file);
            event.dataTransfer.dropEffect = kind === expectedKind || (expectedKind === 'video' && kind === 'project') ? 'copy' : 'none';
            show(kind === 'project' ? 'project' : modeAt(event.target));
            if (!videoInsertionAt(event.target, event.clientX)) clearVideoInsertion();
        },
        dragleave: event => {
            if (event.relatedTarget && root.contains(event.relatedTarget)) return;
            state.dropDepth = Math.max(0, state.dropDepth - 1);
            if (state.dropDepth === 0) clear();
        },
        drop: async event => {
            const file = event.dataTransfer?.files?.[0]
                || [...(event.dataTransfer?.items || [])].find(candidate => candidate.kind === 'file')?.getAsFile?.();
            if (!file) return;
            event.preventDefault();
            event.stopPropagation();
            const actualKind = mediaDropKind(file);
            const mode = actualKind === 'project' ? 'project' : modeAt(event.target);
            const videoInsertion = mode === 'insert' ? videoInsertionAt(event.target, event.clientX) : null;
            clear();
            if (actualKind === 'project' && expectedKind === 'video') {
                if (!assignMediaDrop(projectInputId, file))
                    state.dotnet?.invokeMethodAsync('MediaStudioFileDropRejected', 'The dropped project file could not be forwarded to the open-project importer.').catch(() => {});
                return;
            }
            if (actualKind !== expectedKind) {
                state.dotnet?.invokeMethodAsync(
                    'MediaStudioFileDropRejected',
                    `The dropped file '${file.name || 'file'}' is ${actualKind || 'not recognized as media'}; this Studio accepts ${expectedKind} files.`).catch(() => {});
                return;
            }
            if (videoInsertion) {
                try {
                    await state.dotnet?.invokeMethodAsync(
                        'MediaStudioDropInsertionPointSelected',
                        videoInsertion.seconds,
                        videoInsertion.pointSelection);
                } catch { }
            }
            const inputId = mode === 'insert' ? insertInputId : sourceInputId;
            if (!assignMediaDrop(inputId, file))
                state.dotnet?.invokeMethodAsync('MediaStudioFileDropRejected', 'The dropped media file could not be forwarded to the Studio importer.').catch(() => {});
        }
    };
    state.dropRoot = root;
    state.dropHandlers = handlers;
    state.dropDepth = 0;
    root.addEventListener('dragenter', handlers.dragenter);
    root.addEventListener('dragover', handlers.dragover);
    root.addEventListener('dragleave', handlers.dragleave);
    root.addEventListener('drop', handlers.drop);
}

function stateFor(id) {
    let state = studioStates.get(id);
    if (!state) {
        state = {
            id,
            sessionId: '',
            dotnet: null,
            recorder: null,
            stream: null,
            chunks: [],
            rangeHandler: null,
            stopAt: null,
            discardRecording: false,
            retainedRecordingBlob: null,
            retainedRecordingUrl: '',
            retainedRecordingKind: '',
            retainedRecordingMimeType: '',
            retainedRecordingFileName: '',
            keyboardHandler: null,
            rootId: '',
            frameStageId: '',
            frameOverlayId: '',
            timeOverlayId: '',
            frameResizeObserver: null,
            frameMetadataHandler: null,
            frameOverlayMoveHandler: null,
            frameOverlayLeaveHandler: null,
            frameNodePointerDownHandler: null,
            frameNodePointerMoveHandler: null,
            frameNodePointerUpHandler: null,
            frameNodeGesture: null,
            effectRuntimeKey: '',
            timeOverlayPointerDownHandler: null,
            timeOverlayPointerMoveHandler: null,
            timeOverlayPointerUpHandler: null,
            timeOverlayPointerCancelHandler: null,
            timeOverlayDoubleClickHandler: null,
            timeOverlayMetadataHandler: null,
            timeOverlayDurationHandler: null,
            timeOverlayUpdateHandler: null,
            timeOverlayGesture: null,
            lastReportedDuration: 0,
            durationReportPending: false,
            dropRoot: null,
            dropHandlers: null,
            dropDepth: 0
        };
        studioStates.set(id, state);
    }
    return state;
}

function releaseRetainedRecording(state) {
    if (state.retainedRecordingUrl) {
        try { URL.revokeObjectURL(state.retainedRecordingUrl); } catch { }
    }
    state.retainedRecordingBlob = null;
    state.retainedRecordingUrl = '';
    state.retainedRecordingKind = '';
    state.retainedRecordingMimeType = '';
    state.retainedRecordingFileName = '';
}

function recordingExtension(mimeType) {
    const normalized = baseMimeType(mimeType);
    if (normalized.includes('mp4')) return 'mp4';
    if (normalized.includes('ogg')) return 'ogg';
    if (normalized.includes('wav')) return 'wav';
    return 'webm';
}

function recordingFileName(kind, mimeType) {
    return `Recorded ${kind === 'video' ? 'Video' : 'Audio'}.${recordingExtension(mimeType)}`;
}

function arrayBufferToBase64(buffer) {
    const bytes = new Uint8Array(buffer);
    const characterChunk = 0x8000;
    let binary = '';
    for (let offset = 0; offset < bytes.length; offset += characterChunk)
        binary += String.fromCharCode(...bytes.subarray(offset, Math.min(bytes.length, offset + characterChunk)));
    return btoa(binary);
}

function mediaElement(id) {
    const element = document.getElementById(id);
    return element instanceof HTMLMediaElement ? element : null;
}

function releaseFrameOverlayBindings(state) {
    const video = mediaElement(state.id);
    const overlay = document.getElementById(state.frameOverlayId);
    if (video && state.frameMetadataHandler) {
        video.removeEventListener('loadedmetadata', state.frameMetadataHandler);
        video.removeEventListener('loadeddata', state.frameMetadataHandler);
    }
    if (overlay && state.frameOverlayMoveHandler) overlay.removeEventListener('pointermove', state.frameOverlayMoveHandler);
    if (overlay && state.frameNodePointerDownHandler) overlay.removeEventListener('pointerdown', state.frameNodePointerDownHandler, true);
    if (overlay && state.frameNodePointerMoveHandler) overlay.removeEventListener('pointermove', state.frameNodePointerMoveHandler, true);
    if (overlay && state.frameNodePointerUpHandler) {
        overlay.removeEventListener('pointerup', state.frameNodePointerUpHandler, true);
        overlay.removeEventListener('pointercancel', state.frameNodePointerUpHandler, true);
    }
    if (overlay && state.frameOverlayLeaveHandler) {
        overlay.removeEventListener('pointerleave', state.frameOverlayLeaveHandler);
        overlay.removeEventListener('pointercancel', state.frameOverlayLeaveHandler);
    }
    try { state.frameResizeObserver?.disconnect(); } catch { }
    state.frameResizeObserver = null;
    state.frameMetadataHandler = null;
    state.frameOverlayMoveHandler = null;
    state.frameOverlayLeaveHandler = null;
    state.frameNodePointerDownHandler = null;
    state.frameNodePointerMoveHandler = null;
    state.frameNodePointerUpHandler = null;
    state.frameNodeGesture = null;
}

function releaseVideoTimeOverlayBindings(state) {
    const video = mediaElement(state.id);
    const overlay = document.getElementById(state.timeOverlayId);
    if (overlay && state.timeOverlayPointerDownHandler) overlay.removeEventListener('pointerdown', state.timeOverlayPointerDownHandler);
    if (overlay && state.timeOverlayPointerMoveHandler) overlay.removeEventListener('pointermove', state.timeOverlayPointerMoveHandler);
    if (overlay && state.timeOverlayPointerUpHandler) overlay.removeEventListener('pointerup', state.timeOverlayPointerUpHandler);
    if (overlay && state.timeOverlayPointerCancelHandler) overlay.removeEventListener('pointercancel', state.timeOverlayPointerCancelHandler);
    if (overlay && state.timeOverlayDoubleClickHandler) overlay.removeEventListener('dblclick', state.timeOverlayDoubleClickHandler);
    if (video && state.timeOverlayMetadataHandler) {
        video.removeEventListener('loadedmetadata', state.timeOverlayMetadataHandler);
        video.removeEventListener('loadeddata', state.timeOverlayMetadataHandler);
    }
    if (video && state.timeOverlayDurationHandler)
        video.removeEventListener('durationchange', state.timeOverlayDurationHandler);
    if (video && state.timeOverlayUpdateHandler) {
        video.removeEventListener('timeupdate', state.timeOverlayUpdateHandler);
        video.removeEventListener('seeked', state.timeOverlayUpdateHandler);
    }
    state.timeOverlayPointerDownHandler = null;
    state.timeOverlayPointerMoveHandler = null;
    state.timeOverlayPointerUpHandler = null;
    state.timeOverlayPointerCancelHandler = null;
    state.timeOverlayDoubleClickHandler = null;
    state.timeOverlayMetadataHandler = null;
    state.timeOverlayDurationHandler = null;
    state.timeOverlayUpdateHandler = null;
    state.timeOverlayGesture = null;
    state.durationReportPending = false;
}

function syncFrameOverlay(state) {
    const video = mediaElement(state.id);
    const stage = document.getElementById(state.frameStageId);
    const frameOverlay = document.getElementById(state.frameOverlayId);
    const timeOverlay = document.getElementById(state.timeOverlayId);
    if (!(video instanceof HTMLVideoElement) || !stage || (!frameOverlay && !timeOverlay)) return;

    const stageBounds = stage.getBoundingClientRect();
    const videoBounds = video.getBoundingClientRect();
    const availableWidth = Math.max(1, videoBounds.width);
    const availableHeight = Math.max(1, videoBounds.height);
    const sourceWidth = Math.max(1, Number(video.videoWidth) || availableWidth);
    const sourceHeight = Math.max(1, Number(video.videoHeight) || availableHeight);
    const objectFit = String(getComputedStyle(video).objectFit || 'contain').toLowerCase();
    const scale = objectFit === 'cover'
        ? Math.max(availableWidth / sourceWidth, availableHeight / sourceHeight)
        : Math.min(availableWidth / sourceWidth, availableHeight / sourceHeight);
    const contentWidth = objectFit === 'fill' ? availableWidth : Math.max(1, sourceWidth * scale);
    const contentHeight = objectFit === 'fill' ? availableHeight : Math.max(1, sourceHeight * scale);
    const left = videoBounds.left - stageBounds.left + (availableWidth - contentWidth) / 2;
    const top = videoBounds.top - stageBounds.top + (availableHeight - contentHeight) / 2;

    if (frameOverlay) {
        frameOverlay.style.left = `${left}px`;
        frameOverlay.style.top = `${top}px`;
        frameOverlay.style.width = `${contentWidth}px`;
        frameOverlay.style.height = `${contentHeight}px`;
    }

    if (timeOverlay) {
        // Temporal interaction belongs to the entire play canvas. It must not collapse
        // to the contained source image when the video uses Fit whole.
        timeOverlay.style.left = '0px';
        timeOverlay.style.top = '0px';
        timeOverlay.style.width = `${Math.max(1, stageBounds.width)}px`;
        timeOverlay.style.height = `${Math.max(1, stageBounds.height)}px`;
    }
}

function bindFrameOverlay(state, frameStageId, frameOverlayId) {
    releaseFrameOverlayBindings(state);
    state.frameStageId = String(frameStageId || '');
    state.frameOverlayId = String(frameOverlayId || '');
    if (!state.frameStageId || !state.frameOverlayId) return;

    const video = mediaElement(state.id);
    const stage = document.getElementById(state.frameStageId);
    const overlay = document.getElementById(state.frameOverlayId);
    if (!(video instanceof HTMLVideoElement) || !stage || !overlay) return;

    state.frameMetadataHandler = () => syncFrameOverlay(state);
    video.addEventListener('loadedmetadata', state.frameMetadataHandler);
    video.addEventListener('loadeddata', state.frameMetadataHandler);

    state.frameOverlayMoveHandler = event => {
        const bounds = overlay.getBoundingClientRect();
        const x = Math.max(0, Math.min(bounds.width, Number(event.clientX) - bounds.left));
        const y = Math.max(0, Math.min(bounds.height, Number(event.clientY) - bounds.top));
        overlay.style.setProperty('--media-pointer-x', `${x}px`);
        overlay.style.setProperty('--media-pointer-y', `${y}px`);
        overlay.classList.add('pointer-visible');
    };
    state.frameOverlayLeaveHandler = () => overlay.classList.remove('pointer-visible');
    overlay.addEventListener('pointermove', state.frameOverlayMoveHandler);
    overlay.addEventListener('pointerleave', state.frameOverlayLeaveHandler);
    overlay.addEventListener('pointercancel', state.frameOverlayLeaveHandler);

    const updateFrameNodeVisual = (node, x, y) => {
        node.setAttribute('cx', String(x * 1000));
        node.setAttribute('cy', String(y * 1000));
        const nodes = [...overlay.querySelectorAll('[data-frame-node-index]')]
            .sort((left, right) => Number(left.dataset.frameNodeIndex) - Number(right.dataset.frameNodeIndex));
        const points = nodes.map(candidate => `${candidate.getAttribute('cx') || '0'},${candidate.getAttribute('cy') || '0'}`).join(' ');
        const polyline = overlay.querySelector('.media-frame-cutline');
        const polygon = overlay.querySelector('.media-frame-selection');
        if (polyline) polyline.setAttribute('points', points);
        if (polygon) polygon.setAttribute('points', points);
        const dim = overlay.querySelector('.media-frame-dim');
        if (dim && nodes.length >= 3) {
            const pathPoints = nodes.map(candidate => `${candidate.getAttribute('cx') || '0'} ${candidate.getAttribute('cy') || '0'}`).join(' L ');
            dim.setAttribute('d', `M 0 0 H 1000 V 1000 H 0 Z M ${pathPoints} Z`);
        }
    };
    const normalizedFramePoint = event => {
        const bounds = overlay.getBoundingClientRect();
        return {
            x: Math.max(0, Math.min(1, (Number(event.clientX) - bounds.left) / Math.max(1, bounds.width))),
            y: Math.max(0, Math.min(1, (Number(event.clientY) - bounds.top) / Math.max(1, bounds.height)))
        };
    };
    state.frameNodePointerDownHandler = event => {
        const node = event.target?.closest?.('[data-frame-node-index]');
        if (!(node instanceof SVGCircleElement) || !overlay.classList.contains('active') || event.button !== 0) return;
        event.preventDefault();
        event.stopImmediatePropagation();
        const pointIndex = Number(node.dataset.frameNodeIndex);
        if (!Number.isInteger(pointIndex) || pointIndex < 0) return;
        state.frameNodeGesture = { pointerId: event.pointerId, pointIndex, node };
        try { overlay.setPointerCapture(event.pointerId); } catch { }
        node.classList.add('dragging');
    };
    state.frameNodePointerMoveHandler = event => {
        const gesture = state.frameNodeGesture;
        if (!gesture || gesture.pointerId !== event.pointerId) return;
        event.preventDefault();
        event.stopImmediatePropagation();
        const point = normalizedFramePoint(event);
        updateFrameNodeVisual(gesture.node, point.x, point.y);
        gesture.x = point.x;
        gesture.y = point.y;
    };
    state.frameNodePointerUpHandler = event => {
        const gesture = state.frameNodeGesture;
        if (!gesture || gesture.pointerId !== event.pointerId) return;
        event.preventDefault();
        event.stopImmediatePropagation();
        const point = gesture.x == null ? normalizedFramePoint(event) : { x: gesture.x, y: gesture.y };
        updateFrameNodeVisual(gesture.node, point.x, point.y);
        gesture.node.classList.remove('dragging');
        try { overlay.releasePointerCapture(event.pointerId); } catch { }
        state.frameNodeGesture = null;
        state.dotnet?.invokeMethodAsync('VideoFramePointCommitted', gesture.pointIndex, point.x, point.y).catch(() => {});
    };
    overlay.addEventListener('pointerdown', state.frameNodePointerDownHandler, true);
    overlay.addEventListener('pointermove', state.frameNodePointerMoveHandler, true);
    overlay.addEventListener('pointerup', state.frameNodePointerUpHandler, true);
    overlay.addEventListener('pointercancel', state.frameNodePointerUpHandler, true);

    if (typeof ResizeObserver !== 'undefined') {
        state.frameResizeObserver = new ResizeObserver(() => syncFrameOverlay(state));
        state.frameResizeObserver.observe(stage);
        state.frameResizeObserver.observe(video);
    }
    requestAnimationFrame(() => syncFrameOverlay(state));
}

function videoTimeData(overlay) {
    const duration = Math.max(.01, Number(overlay?.dataset?.duration) || 0);
    const pointSelection = overlay?.dataset?.selectionPoint === 'true';
    const trimStart = Math.max(0, Math.min(duration, Number(overlay?.dataset?.trimStart) || 0));
    const trimEnd = Math.max(trimStart, Math.min(duration, Number(overlay?.dataset?.trimEnd) || duration));
    const start = Math.max(0, Math.min(duration, Number(overlay?.dataset?.selectionStart) || 0));
    const end = Math.max(start, Math.min(duration, Number(overlay?.dataset?.selectionEnd) || start));
    return { duration, pointSelection, trimStart, trimEnd, start, end };
}

function videoTimeAt(overlay, clientX, clampToSelection = false) {
    const data = videoTimeData(overlay);
    const bounds = overlay.getBoundingClientRect();
    const ratio = Math.max(0, Math.min(1, (Number(clientX) - bounds.left) / Math.max(1, bounds.width)));
    const raw = ratio * data.duration;
    if (!clampToSelection) return raw;
    return data.pointSelection ? data.start : Math.max(data.start, Math.min(data.end, raw));
}

function formatMediaTime(seconds) {
    const safe = Math.max(0, Number(seconds) || 0);
    const minutes = Math.floor(safe / 60);
    const remainder = safe - minutes * 60;
    return `${minutes}:${remainder.toFixed(1).padStart(4, '0')}`;
}

function syncVideoSelectionControls(overlay, start, end, pointSelection) {
    const modeInput = document.getElementById('media-studio-video-selection-mode');
    const startInput = document.getElementById('media-studio-video-selection-start');
    const endInput = document.getElementById('media-studio-video-selection-end');
    const label = document.getElementById('media-studio-video-selection-label');
    const readout = document.getElementById('media-studio-video-selection-readout');
    const title = document.getElementById('media-studio-selection-title');
    const summary = document.getElementById('media-studio-selection-summary');
    const detail = document.getElementById('media-studio-video-selection-value');
    const currentTime = document.getElementById('media-studio-video-current-time');
    const sequenceSelection = document.getElementById('media-studio-sequence-selection');
    const duration = Math.max(.01, Number(overlay?.dataset?.duration) || 0);
    const clipName = String(overlay?.dataset?.clipName || 'Selected clip');
    if (modeInput instanceof HTMLSelectElement) modeInput.value = pointSelection ? 'Point' : 'Range';
    if (startInput instanceof HTMLInputElement) startInput.value = Number(start).toFixed(2);
    if (endInput instanceof HTMLInputElement) {
        endInput.value = Number(end).toFixed(2);
        endInput.disabled = pointSelection;
    }
    if (label) label.textContent = pointSelection ? 'Timestamp' : 'Range';
    if (currentTime) currentTime.textContent = formatMediaTime(start);
    if (readout) readout.textContent = pointSelection
        ? `/ ${formatMediaTime(duration)}`
        : `— ${formatMediaTime(end)} / ${formatMediaTime(duration)}`;
    if (title) title.textContent = pointSelection ? 'Selected timestamp' : 'Selected range';
    if (summary) summary.textContent = pointSelection
        ? `${clipName} · ${formatMediaTime(start)} / ${formatMediaTime(duration)}`
        : `${clipName} · ${formatMediaTime(start)} — ${formatMediaTime(end)} / ${formatMediaTime(duration)}`;
    const sequenceDuration = Math.max(.01, Number(overlay?.dataset?.sequenceDuration) || 0);
    const segmentTimelineStart = Math.max(0, Number(overlay?.dataset?.segmentTimelineStart) || 0);
    const segmentSourceStart = Math.max(0, Number(overlay?.dataset?.segmentSourceStart) || 0);
    const playbackRate = Math.max(.1, Number(overlay?.dataset?.playbackRate) || 1);
    const timelineStart = segmentTimelineStart + Math.max(0, Number(start) - segmentSourceStart) / playbackRate;
    const timelineEnd = segmentTimelineStart + Math.max(0, Number(end) - segmentSourceStart) / playbackRate;
    if (detail) detail.textContent = pointSelection
        ? `Source timestamp ${formatMediaTime(start)} · project timestamp ${formatMediaTime(timelineStart)}`
        : `Source ${formatMediaTime(start)} — ${formatMediaTime(end)} · project ${formatMediaTime(timelineStart)} — ${formatMediaTime(timelineEnd)} · ${formatMediaTime(Math.max(0, end - start))} selected`;
    if (sequenceSelection instanceof HTMLElement) {
        sequenceSelection.style.left = `${Math.max(0, Math.min(100, timelineStart / sequenceDuration * 100))}%`;
        sequenceSelection.style.width = pointSelection
            ? '2px'
            : `${Math.max(0, Math.min(100, Math.max(0, timelineEnd - timelineStart) / sequenceDuration * 100))}%`;
        sequenceSelection.classList.toggle('point', pointSelection);
        sequenceSelection.classList.toggle('range', !pointSelection);
        sequenceSelection.title = pointSelection
            ? `${clipName}: source ${formatMediaTime(start)} · project ${formatMediaTime(timelineStart)}`
            : `${clipName}: source ${formatMediaTime(start)} — ${formatMediaTime(end)} · project ${formatMediaTime(timelineStart)} — ${formatMediaTime(timelineEnd)}`;
    }
}

function setVideoTimeVisual(overlay, start, end, pointSelection) {
    const data = videoTimeData(overlay);
    const duration = data.duration;
    const minimum = Math.max(0, data.trimStart);
    const maximum = Math.max(minimum, data.trimEnd);
    const safeStart = Math.max(minimum, Math.min(maximum, Number(start) || minimum));
    const safeEnd = pointSelection
        ? safeStart
        : Math.max(safeStart, Math.min(maximum, Number(end) || safeStart));
    const point = pointSelection ? safeStart : safeStart;
    overlay.dataset.selectionStart = String(safeStart);
    overlay.dataset.selectionEnd = String(safeEnd);
    overlay.dataset.selectionPoint = pointSelection ? 'true' : 'false';
    overlay.classList.toggle('point-selection', pointSelection);
    overlay.classList.toggle('range-selection', !pointSelection);
    overlay.style.setProperty('--video-time-start', `${safeStart / duration * 100}%`);
    overlay.style.setProperty('--video-time-end', `${safeEnd / duration * 100}%`);
    overlay.style.setProperty('--video-time-playhead', `${point / duration * 100}%`);
    syncVideoSelectionControls(overlay, safeStart, safeEnd, pointSelection);
}

function normalizeVideoTimeRange(start, end, duration, minimum = 0, maximum = duration) {
    const safeDuration = Math.max(.01, Number(duration) || 0);
    const safeMinimum = Math.max(0, Math.min(safeDuration, Number(minimum) || 0));
    const safeMaximum = Math.max(safeMinimum, Math.min(safeDuration, Number(maximum) || safeDuration));
    const minimumSpan = Math.min(.01, safeMaximum - safeMinimum);
    const maximumStart = Math.max(safeMinimum, safeMaximum - minimumSpan);
    const safeStart = Math.max(safeMinimum, Math.min(maximumStart, Number(start) || safeMinimum));
    const minimumEnd = Math.min(safeMaximum, safeStart + minimumSpan);
    const safeEnd = Math.max(minimumEnd, Math.min(safeMaximum, Number(end) || minimumEnd));
    return { start: safeStart, end: safeEnd };
}

function videoCanvasMode(overlay) {
    const value = String(overlay?.dataset?.mouseMode || 'SelectSection');
    return value === 'PlacePlayhead' || value === 'AddCutLine' || value === 'FrameRegion'
        ? value
        : 'SelectSection';
}

function setVideoPlayheadVisual(overlay, sourceSeconds) {
    const data = videoTimeData(overlay);
    const current = Math.max(data.trimStart, Math.min(data.trimEnd, Number(sourceSeconds) || data.trimStart));
    overlay.style.setProperty('--video-time-playhead', `${current / data.duration * 100}%`);
    const readout = document.getElementById('media-studio-video-current-time');
    if (readout) readout.textContent = formatMediaTime(current);

    const sequenceDuration = Math.max(.01, Number(overlay?.dataset?.sequenceDuration) || 0);
    const segmentTimelineStart = Math.max(0, Number(overlay?.dataset?.segmentTimelineStart) || 0);
    const segmentSourceStart = Math.max(0, Number(overlay?.dataset?.segmentSourceStart) || 0);
    const playbackRate = Math.max(.1, Number(overlay?.dataset?.playbackRate) || 1);
    const timelineSeconds = segmentTimelineStart + Math.max(0, current - segmentSourceStart) / playbackRate;
    const sequencePlayhead = document.getElementById('media-studio-sequence-playhead');
    if (sequencePlayhead instanceof HTMLElement) {
        sequencePlayhead.style.left = `${Math.max(0, Math.min(100, timelineSeconds / sequenceDuration * 100))}%`;
        sequencePlayhead.title = `Source ${formatMediaTime(current)} · project ${formatMediaTime(timelineSeconds)}`;
    }
    return current;
}

function updateVideoTimeReadout(state) {
    const video = mediaElement(state.id);
    const overlay = document.getElementById(state.timeOverlayId);
    if (!(video instanceof HTMLVideoElement) || !overlay) return;
    setVideoPlayheadVisual(overlay, Number(video.currentTime) || 0);
}

function resolvedVideoDuration(video) {
    const direct = Number(video?.duration);
    if (Number.isFinite(direct) && direct > .01) return direct;
    try {
        const seekable = video?.seekable;
        if (seekable?.length) {
            const end = Number(seekable.end(seekable.length - 1));
            if (Number.isFinite(end) && end > .01) return end;
        }
    } catch { }
    return 0;
}

function reportResolvedVideoDuration(state, video, overlay) {
    const duration = resolvedVideoDuration(video);
    if (!(duration > .01) || state.durationReportPending) return;
    const modeled = Math.max(.01, Number(overlay?.dataset?.duration) || 0);
    const tolerance = Math.max(.02, duration / 10000);
    if (Math.abs(duration - modeled) <= tolerance || Math.abs(duration - state.lastReportedDuration) <= tolerance) return;
    state.lastReportedDuration = duration;
    state.durationReportPending = true;
    Promise.resolve(state.dotnet?.invokeMethodAsync('VideoSourceDurationResolved', duration))
        .catch(() => { state.lastReportedDuration = 0; })
        .finally(() => { state.durationReportPending = false; });
}

function bindVideoTimeOverlay(state, timeOverlayId) {
    releaseVideoTimeOverlayBindings(state);
    state.timeOverlayId = String(timeOverlayId || '');
    if (!state.timeOverlayId) return;

    const overlay = document.getElementById(state.timeOverlayId);
    const video = mediaElement(state.id);
    if (!overlay || !(video instanceof HTMLVideoElement)) return;

    video.controls = false;
    video.playsInline = true;
    try { video.disablePictureInPicture = true; } catch { }
    const clipTimeAt = clientX => {
        const data = videoTimeData(overlay);
        const value = videoTimeAt(overlay, clientX, false);
        return Math.max(data.trimStart, Math.min(data.trimEnd, value));
    };
    const locallyScrub = sourceSeconds => {
        const current = setVideoPlayheadVisual(overlay, sourceSeconds);
        try {
            video.pause();
            if (Number.isFinite(video.duration)) video.currentTime = current;
        } catch { }
        return current;
    };

    const finishGesture = async event => {
        const gesture = state.timeOverlayGesture;
        if (!gesture || gesture.pointerId !== event.pointerId) return;
        event.preventDefault();
        event.stopPropagation();
        try { overlay.releasePointerCapture(event.pointerId); } catch { }
        const current = clipTimeAt(event.clientX);

        if (gesture.mode === 'playhead' || gesture.mode === 'cutline') {
            const sourceSeconds = locallyScrub(current);
            state.timeOverlayGesture = null;
            try {
                await state.dotnet?.invokeMethodAsync(
                    gesture.mode === 'cutline' ? 'VideoCutlineCommitted' : 'VideoPlayheadCommitted',
                    sourceSeconds);
            } catch { }
            return;
        }

        const moved = gesture.moved || Math.abs(Number(event.clientX) - gesture.clientX) >= 4;
        let start;
        let end;
        let pointSelection = false;

        if (gesture.mode === 'start') {
            start = Math.min(current, gesture.end - .01);
            end = gesture.end;
        } else if (gesture.mode === 'end') {
            start = gesture.start;
            end = Math.max(current, gesture.start + .01);
        } else if (!moved) {
            const data = videoTimeData(overlay);
            start = Math.max(data.trimStart, Math.min(data.trimEnd, current));
            end = start;
            pointSelection = true;
        } else {
            start = Math.min(gesture.anchor, current);
            end = Math.max(gesture.anchor, current);
            if (end - start < .01) end = Math.min(videoTimeData(overlay).duration, start + .01);
        }

        const data = videoTimeData(overlay);
        const duration = data.duration;
        if (pointSelection) {
            start = Math.max(0, Math.min(duration, start));
            end = start;
        } else {
            const range = normalizeVideoTimeRange(start, end, duration, data.trimStart, data.trimEnd);
            start = range.start;
            end = range.end;
        }
        setVideoTimeVisual(overlay, start, end, pointSelection);
        locallyScrub(start);
        state.timeOverlayGesture = null;
        try {
            await state.dotnet?.invokeMethodAsync('VideoTimeSelectionCommitted', start, end, pointSelection);
        } catch { }
    };

    state.timeOverlayPointerDownHandler = event => {
        if (event.button !== 0 || overlay.classList.contains('disabled')) return;
        if (event.target?.closest?.('[data-video-time-control],button,input,label,select')) return;
        event.preventDefault();
        event.stopPropagation();
        const data = videoTimeData(overlay);
        const pointerMode = videoCanvasMode(overlay);
        const handle = event.target?.closest?.('[data-video-time-handle]')?.dataset?.videoTimeHandle || '';
        const anchor = clipTimeAt(event.clientX);
        const gestureMode = pointerMode === 'PlacePlayhead'
            ? 'playhead'
            : pointerMode === 'AddCutLine'
                ? 'cutline'
                : handle === 'start' || handle === 'end'
                    ? handle
                    : 'select';
        state.timeOverlayGesture = {
            pointerId: event.pointerId,
            clientX: Number(event.clientX),
            anchor,
            start: data.start,
            end: data.end,
            pointSelection: data.pointSelection,
            mode: gestureMode,
            moved: false
        };
        try { overlay.setPointerCapture(event.pointerId); } catch { }
        if (gestureMode === 'playhead' || gestureMode === 'cutline') locallyScrub(anchor);
        else if (!handle) setVideoTimeVisual(overlay, anchor, anchor, true);
    };

    state.timeOverlayPointerMoveHandler = event => {
        const gesture = state.timeOverlayGesture;
        if (!gesture || gesture.pointerId !== event.pointerId) return;
        event.preventDefault();
        event.stopPropagation();
        if (Math.abs(Number(event.clientX) - gesture.clientX) >= 4) gesture.moved = true;
        const current = clipTimeAt(event.clientX);
        if (gesture.mode === 'playhead' || gesture.mode === 'cutline') {
            locallyScrub(current);
        } else if (gesture.mode === 'start') {
            setVideoTimeVisual(overlay, Math.min(current, gesture.end - .01), gesture.end, false);
        } else if (gesture.mode === 'end') {
            setVideoTimeVisual(overlay, gesture.start, Math.max(current, gesture.start + .01), false);
        } else if (gesture.moved) {
            setVideoTimeVisual(overlay, Math.min(gesture.anchor, current), Math.max(gesture.anchor, current), false);
        }
    };

    state.timeOverlayPointerUpHandler = finishGesture;
    state.timeOverlayPointerCancelHandler = event => {
        const gesture = state.timeOverlayGesture;
        if (!gesture || gesture.pointerId !== event.pointerId) return;
        if (gesture.mode !== 'playhead' && gesture.mode !== 'cutline')
            setVideoTimeVisual(overlay, gesture.start, gesture.end, gesture.pointSelection);
        state.timeOverlayGesture = null;
    };
    state.timeOverlayDoubleClickHandler = event => {
        event.preventDefault();
        event.stopPropagation();
    };
    state.timeOverlayMetadataHandler = () => {
        reportResolvedVideoDuration(state, video, overlay);
        syncFrameOverlay(state);
        updateVideoTimeReadout(state);
    };
    state.timeOverlayDurationHandler = () => reportResolvedVideoDuration(state, video, overlay);
    state.timeOverlayUpdateHandler = () => {
        reportResolvedVideoDuration(state, video, overlay);
        updateVideoTimeReadout(state);
    };

    overlay.addEventListener('pointerdown', state.timeOverlayPointerDownHandler);
    overlay.addEventListener('pointermove', state.timeOverlayPointerMoveHandler);
    overlay.addEventListener('pointerup', state.timeOverlayPointerUpHandler);
    overlay.addEventListener('pointercancel', state.timeOverlayPointerCancelHandler);
    overlay.addEventListener('dblclick', state.timeOverlayDoubleClickHandler);
    video.addEventListener('loadedmetadata', state.timeOverlayMetadataHandler);
    video.addEventListener('loadeddata', state.timeOverlayMetadataHandler);
    video.addEventListener('durationchange', state.timeOverlayDurationHandler);
    video.addEventListener('timeupdate', state.timeOverlayUpdateHandler);
    video.addEventListener('seeked', state.timeOverlayUpdateHandler);
    requestAnimationFrame(() => {
        reportResolvedVideoDuration(state, video, overlay);
        syncFrameOverlay(state);
        const data = videoTimeData(overlay);
        setVideoTimeVisual(overlay, data.start, data.end, data.pointSelection);
        updateVideoTimeReadout(state);
    });
}

export function configureVideoEffects(videoId, canvasId, config) {
    const video = document.getElementById(videoId);
    const canvas = document.getElementById(canvasId);
    if (!(video instanceof HTMLVideoElement) || !(canvas instanceof HTMLCanvasElement) || !window.publisherVideoEffects) return false;
    const state = stateFor(videoId);
    const key = `media-studio-${videoId}`;
    state.effectRuntimeKey = key;
    window.publisherVideoEffects.install(key, video, canvas, config || {});
    return true;
}

export function refreshMediaStudioOverlay(id) {
    const state = studioStates.get(id);
    if (!state) return;
    requestAnimationFrame(() => {
        syncFrameOverlay(state);
        const overlay = document.getElementById(state.timeOverlayId);
        if (overlay) {
            const data = videoTimeData(overlay);
            setVideoTimeVisual(overlay, data.start, data.end, data.pointSelection);
        }
        updateVideoTimeReadout(state);
    });
}

export function clickElement(id) {
    const element = document.getElementById(id);
    if (!element) throw new Error(`Element '${id}' is not available.`);
    if (element instanceof HTMLInputElement && element.type === 'file') element.value = '';
    element.click();
}

export function initializeMediaStudio(
    id,
    dotnet,
    sessionId,
    rootId = "",
    frameStageId = "",
    frameOverlayId = "",
    timeOverlayId = "",
    sourceInputId = "",
    insertInputId = "",
    projectInputId = "",
    expectedKind = "video") {
    const state = stateFor(id);
    const nextSessionId = String(sessionId || '');
    if (state.sessionId && state.sessionId !== nextSessionId) releaseRetainedRecording(state);
    state.sessionId = nextSessionId;
    state.dotnet = dotnet;
    state.discardRecording = false;
    state.rootId = String(rootId || "");
    if (state.keyboardHandler) document.removeEventListener("keydown", state.keyboardHandler, true);
    state.keyboardHandler = event => {
        const root = document.getElementById(state.rootId);
        if (!root || !root.contains(event.target)) return;
        const target = event.target;
        if (target instanceof HTMLInputElement || target instanceof HTMLTextAreaElement || target instanceof HTMLSelectElement || target?.isContentEditable) return;
        const modifier = event.ctrlKey || event.metaKey;
        const key = String(event.key || "").toLowerCase();
        let command = "";
        if (modifier && key === "c") command = "copy";
        else if (modifier && key === "v") command = "paste";
        else if (event.key === "Delete" || event.key === "Backspace") command = "delete";
        else if (event.key === "Enter") command = "commit";
        else if (event.key === "Escape") command = "cancel";
        if (!command) return;
        event.preventDefault();
        event.stopPropagation();
        state.dotnet?.invokeMethodAsync("MediaStudioShortcutRequested", command).catch(() => {});
    };
    document.addEventListener("keydown", state.keyboardHandler, true);
    bindFrameOverlay(state, frameStageId, frameOverlayId);
    bindVideoTimeOverlay(state, timeOverlayId);
    bindMediaDrop(state, state.rootId, sourceInputId, insertInputId, projectInputId, String(expectedKind || 'video').toLowerCase());
}

function waitForMetadata(element) {
    if (Number.isFinite(element.duration) && element.duration > 0 && element.readyState >= 1)
        return Promise.resolve();
    return new Promise((resolve, reject) => {
        const timer = setTimeout(() => failed(new Error('Timed out while reading media metadata.')), 15000);
        const cleanup = () => {
            clearTimeout(timer);
            element.removeEventListener('loadedmetadata', loaded);
            element.removeEventListener('error', failed);
        };
        const loaded = () => { cleanup(); resolve(); };
        const failed = error => {
            cleanup();
            reject(error instanceof Error ? error : new Error('The browser could not decode this media format.'));
        };
        element.addEventListener('loadedmetadata', loaded, { once: true });
        element.addEventListener('error', failed, { once: true });
        element.load();
    });
}

async function waveformFromSource(dataUrl, sampleCount = 96) {
    try {
        const response = await fetch(dataUrl);
        const bytes = await response.arrayBuffer();
        const AudioContextType = window.AudioContext || window.webkitAudioContext;
        if (!AudioContextType) return [];
        const context = new AudioContextType();
        try {
            const buffer = await context.decodeAudioData(bytes.slice(0));
            const channel = buffer.getChannelData(0);
            const block = Math.max(1, Math.floor(channel.length / sampleCount));
            const values = [];
            let max = .0001;
            for (let index = 0; index < sampleCount; index++) {
                const start = index * block;
                const end = Math.min(channel.length, start + block);
                let peak = 0;
                for (let cursor = start; cursor < end; cursor += Math.max(1, Math.floor(block / 128)))
                    peak = Math.max(peak, Math.abs(channel[cursor] || 0));
                values.push(peak);
                max = Math.max(max, peak);
            }
            return values.map(value => Math.max(.04, value / max));
        } finally {
            await context.close();
        }
    } catch {
        return [];
    }
}

async function posterFromVideo(video) {
    try {
        const previous = video.currentTime;
        const target = Math.min(Math.max(.05, video.duration * .12), Math.max(.05, video.duration - .05));
        await new Promise(resolve => {
            if (Math.abs(video.currentTime - target) < .005) { resolve(); return; }
            const timer = setTimeout(done, 4000);
            function done() {
                clearTimeout(timer);
                video.removeEventListener('seeked', done);
                resolve();
            }
            video.addEventListener('seeked', done, { once: true });
            video.currentTime = target;
        });
        const canvas = document.createElement('canvas');
        canvas.width = Math.max(1, video.videoWidth || 640);
        canvas.height = Math.max(1, video.videoHeight || 360);
        canvas.getContext('2d').drawImage(video, 0, 0, canvas.width, canvas.height);
        const poster = canvas.toDataURL('image/jpeg', .82);
        video.currentTime = Number.isFinite(previous) ? previous : 0;
        return poster;
    } catch {
        return '';
    }
}

async function inspectElement(element, dataUrl, kind) {
    const normalizedDataUrl = normalizeMediaDataUrl(dataUrl, kind === 'video' ? 'video/webm' : 'audio/webm');
    element.src = normalizedDataUrl;
    element.load();
    await waitForMetadata(element);
    const durationSeconds = Number.isFinite(element.duration) ? element.duration : 0;
    const waveformSamples = kind === 'audio' ? await waveformFromSource(normalizedDataUrl) : [];
    const posterDataUrl = kind === 'video' && element instanceof HTMLVideoElement ? await posterFromVideo(element) : '';
    element.currentTime = 0;
    return { durationSeconds, waveformSamples, posterDataUrl };
}

export async function inspectMediaSource(id, dataUrl, kind) {
    const element = mediaElement(id);
    if (!element) throw new Error('Media preview is not available.');
    pauseMedia(id);
    return inspectElement(element, dataUrl, kind);
}

export async function inspectMediaDataUrl(dataUrl, kind) {
    const element = document.createElement(kind === 'video' ? 'video' : 'audio');
    element.preload = 'metadata';
    element.playsInline = true;
    try {
        return await inspectElement(element, dataUrl, kind);
    } finally {
        element.pause();
        element.removeAttribute('src');
        element.load();
    }
}

export async function inspectMediaFileInput(inputId, kind) {
    const input = document.getElementById(inputId);
    const file = input instanceof HTMLInputElement ? input.files?.[0] : null;
    if (!file) throw new Error('No media file was selected.');
    const objectUrl = URL.createObjectURL(file);
    const element = document.createElement(kind === 'video' ? 'video' : 'audio');
    element.preload = 'metadata';
    element.playsInline = true;
    try {
        const info = await inspectElement(element, objectUrl, kind);
        return { ...info, mimeType: baseMimeType(file.type, kind === 'video' ? 'video/mp4' : 'audio/mpeg') };
    } finally {
        element.pause();
        element.removeAttribute('src');
        element.load();
        URL.revokeObjectURL(objectUrl);
    }
}

function preferredMime(kind) {
    const choices = kind === 'video'
        ? ['video/webm;codecs=vp8,opus', 'video/webm;codecs=vp8', 'video/webm;codecs=vp9,opus', 'video/webm']
        : ['audio/webm;codecs=opus', 'audio/webm', 'audio/ogg;codecs=opus'];
    const probe = document.createElement(kind === 'video' ? 'video' : 'audio');
    return choices.find(value => MediaRecorder.isTypeSupported(value) && probe.canPlayType(value) !== '')
        || choices.find(value => MediaRecorder.isTypeSupported(value))
        || '';
}

async function streamFor(kind, source) {
    if (source === 'screen') {
        const stream = await navigator.mediaDevices.getDisplayMedia({ video: true, audio: true });
        if (kind === 'video' && stream.getAudioTracks().length === 0) {
            try {
                const microphone = await navigator.mediaDevices.getUserMedia({ audio: true });
                for (const track of microphone.getAudioTracks()) stream.addTrack(track);
            } catch { }
        }
        return stream;
    }
    if (source === 'camera') return navigator.mediaDevices.getUserMedia({ video: true, audio: true });
    return navigator.mediaDevices.getUserMedia({ audio: true });
}

async function retainRecording(state, blob, kind) {
    const fallbackMimeType = kind === 'video' ? 'video/webm' : 'audio/webm';
    const mimeType = baseMimeType(blob.type, fallbackMimeType);
    const retainedBlob = blob.type === mimeType ? blob : blob.slice(0, blob.size, mimeType);
    releaseRetainedRecording(state);
    state.retainedRecordingBlob = retainedBlob;
    state.retainedRecordingUrl = URL.createObjectURL(retainedBlob);
    state.retainedRecordingKind = kind;
    state.retainedRecordingMimeType = mimeType;
    state.retainedRecordingFileName = recordingFileName(kind, mimeType);

    const preview = mediaElement(state.id);
    let info = { durationSeconds: 0, waveformSamples: [], posterDataUrl: '' };
    let metadataWarning = '';
    try {
        if (!preview) throw new Error('Media preview is not available.');
        preview.muted = false;
        info = await inspectElement(preview, state.retainedRecordingUrl, kind);
    } catch (error) {
        metadataWarning = error?.message || String(error);
        if (preview) {
            preview.src = state.retainedRecordingUrl;
            preview.load();
        }
    }

    await state.dotnet?.invokeMethodAsync('MediaRecordingReady', {
        objectUrl: state.retainedRecordingUrl,
        mimeType,
        fileName: state.retainedRecordingFileName,
        sizeBytes: retainedBlob.size,
        durationSeconds: info.durationSeconds || 0,
        waveformSamples: info.waveformSamples || [],
        posterDataUrl: info.posterDataUrl || '',
        metadataWarning
    });
}

export async function embedRetainedRecording(id) {
    const state = stateFor(id);
    const blob = state.retainedRecordingBlob;
    if (!(blob instanceof Blob) || !blob.size)
        throw new Error('No completed recording is available to embed.');

    const mimeType = state.retainedRecordingMimeType || baseMimeType(blob.type, state.retainedRecordingKind === 'audio' ? 'audio/webm' : 'video/webm');
    const transferId = crypto.randomUUID();
    const chunkCount = Math.max(1, Math.ceil(blob.size / RECORDING_TRANSFER_CHUNK_SIZE));
    const accepted = await state.dotnet?.invokeMethodAsync('BeginMediaRecordingTransfer', transferId, mimeType, blob.size, chunkCount);
    if (!accepted) throw new Error('The publication could not begin the recording transfer.');

    let transferred = 0;
    for (let index = 0; index < chunkCount; index++) {
        const start = index * RECORDING_TRANSFER_CHUNK_SIZE;
        const end = Math.min(blob.size, start + RECORDING_TRANSFER_CHUNK_SIZE);
        const buffer = await blob.slice(start, end).arrayBuffer();
        const chunk = arrayBufferToBase64(buffer);
        const ok = await state.dotnet.invokeMethodAsync('AppendMediaRecordingChunk', transferId, index, chunk);
        if (!ok) throw new Error('The recording transfer was interrupted.');
        transferred = end;
        if (index === chunkCount - 1 || index % 32 === 0)
            await state.dotnet.invokeMethodAsync('MediaRecordingTransferProgress', transferred, blob.size);
    }
    await state.dotnet.invokeMethodAsync('CompleteMediaRecordingTransfer', transferId);
    return true;
}

export function downloadRetainedRecording(id, requestedFileName) {
    const state = stateFor(id);
    const blob = state.retainedRecordingBlob;
    if (!(blob instanceof Blob) || !blob.size)
        throw new Error('No completed recording is available to download.');
    const anchor = document.createElement('a');
    anchor.href = state.retainedRecordingUrl || URL.createObjectURL(blob);
    anchor.download = String(requestedFileName || state.retainedRecordingFileName || recordingFileName(state.retainedRecordingKind, state.retainedRecordingMimeType));
    anchor.style.display = 'none';
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
}

export function clearRetainedRecording(id) {
    const state = stateFor(id);
    releaseRetainedRecording(state);
}

export async function startMediaRecording(id, kind, source, dotnet) {
    if (!navigator.mediaDevices || typeof MediaRecorder === 'undefined')
        throw new Error('This browser does not support media recording.');
    const state = stateFor(id);
    state.dotnet = dotnet || state.dotnet;
    if (state.recorder && state.recorder.state !== 'inactive') return;
    state.stream = await streamFor(kind, source);
    state.chunks = [];
    state.discardRecording = false;
    const preview = mediaElement(id);
    if (kind === 'video' && preview instanceof HTMLVideoElement) {
        preview.pause();
        preview.removeAttribute('src');
        preview.srcObject = state.stream;
        preview.muted = true;
        preview.play().catch(() => {});
    }
    const mimeType = preferredMime(kind);
    try {
        state.recorder = mimeType ? new MediaRecorder(state.stream, { mimeType }) : new MediaRecorder(state.stream);
    } catch (error) {
        if (preview instanceof HTMLVideoElement && preview.srcObject === state.stream) {
            preview.pause();
            preview.srcObject = null;
        }
        for (const track of state.stream.getTracks()) track.stop();
        state.stream = null;
        throw error;
    }
    state.recorder.addEventListener('dataavailable', event => {
        if (event.data && event.data.size) state.chunks.push(event.data);
    });
    state.recorder.addEventListener('stop', async () => {
        try {
            const preview = mediaElement(id);
            if (preview instanceof HTMLVideoElement && preview.srcObject === state.stream) {
                preview.pause();
                preview.srcObject = null;
            }
            if (state.discardRecording) return;
            const blob = new Blob(state.chunks, { type: state.recorder?.mimeType || mimeType || (kind === 'video' ? 'video/webm' : 'audio/webm') });
            if (!blob.size) throw new Error('The browser completed the recording but produced an empty file.');
            await retainRecording(state, blob, kind);
        } catch (error) {
            await state.dotnet?.invokeMethodAsync('MediaRecordingFailed', error?.message || String(error));
        } finally {
            for (const track of state.stream?.getTracks() || []) track.stop();
            state.stream = null;
            state.recorder = null;
            state.chunks = [];
        }
    }, { once: true });
    const endingTracks = kind === 'video' && state.stream.getVideoTracks().length
        ? state.stream.getVideoTracks()
        : state.stream.getAudioTracks();
    for (const track of endingTracks) {
        track.addEventListener('ended', () => {
            if (state.recorder && state.recorder.state !== 'inactive') state.recorder.stop();
        }, { once: true });
    }
    state.recorder.start(250);
    releaseRetainedRecording(state);
    await state.dotnet?.invokeMethodAsync('MediaRecordingCleared');
}

export function stopMediaRecording(id) {
    const state = stateFor(id);
    state.discardRecording = false;
    if (state.recorder && state.recorder.state !== 'inactive') state.recorder.stop();
}

export function cancelMediaRecording(id) {
    const state = stateFor(id);
    state.discardRecording = true;
    const preview = mediaElement(id);
    if (preview instanceof HTMLVideoElement && preview.srcObject === state.stream) {
        preview.pause();
        preview.srcObject = null;
    }
    try { if (state.recorder && state.recorder.state !== 'inactive') state.recorder.stop(); } catch { }
    for (const track of state.stream?.getTracks() || []) track.stop();
}

export async function playMediaRange(id, start, end, volume, rate, muted, loop) {
    const element = mediaElement(id);
    if (!element) return;
    const state = stateFor(id);
    if (state.rangeHandler) element.removeEventListener('timeupdate', state.rangeHandler);
    element.currentTime = Math.max(0, Number(start) || 0);
    element.volume = Math.max(0, Math.min(1, Number(volume) || 0));
    element.playbackRate = Math.max(.25, Math.min(4, Number(rate) || 1));
    element.muted = Boolean(muted);
    state.stopAt = Math.max(element.currentTime + .01, Number(end) || element.duration || 0);
    state.rangeHandler = () => {
        if (element.currentTime < state.stopAt - .015) return;
        if (loop) {
            element.currentTime = Math.max(0, Number(start) || 0);
            element.play().catch(() => {});
        } else {
            element.pause();
            element.currentTime = state.stopAt;
        }
    };
    element.addEventListener('timeupdate', state.rangeHandler);
    await element.play();
}

export function pauseMedia(id) {
    const element = mediaElement(id);
    if (element) element.pause();
}

export function seekMedia(id, seconds) {
    const element = mediaElement(id);
    if (element) element.currentTime = Math.max(0, Number(seconds) || 0);
}

export function getMediaPosition(id) {
    const element = mediaElement(id);
    return element && Number.isFinite(element.currentTime) ? element.currentTime : 0;
}

export function disposeMediaStudio(id) {
    const state = studioStates.get(id);
    if (!state) return;
    state.discardRecording = true;
    const element = mediaElement(id);
    if (element instanceof HTMLVideoElement && element.srcObject === state.stream) {
        element.pause();
        element.srcObject = null;
    }
    try { if (state.recorder && state.recorder.state !== 'inactive') state.recorder.stop(); } catch { }
    for (const track of state.stream?.getTracks() || []) track.stop();
    if (element && state.rangeHandler) element.removeEventListener('timeupdate', state.rangeHandler);
    if (state.keyboardHandler) document.removeEventListener("keydown", state.keyboardHandler, true);
    state.keyboardHandler = null;
    releaseMediaDropBindings(state);
    releaseVideoTimeOverlayBindings(state);
    releaseFrameOverlayBindings(state);
    if (state.effectRuntimeKey) window.publisherVideoEffects?.dispose(state.effectRuntimeKey);
    state.effectRuntimeKey = '';
    releaseRetainedRecording(state);
    studioStates.delete(id);
}

export function horizontalRatio(elementId, clientX) {
    const element = document.getElementById(elementId);
    if (!element) return 0;
    const bounds = element.getBoundingClientRect();
    return Math.max(0, Math.min(1, (Number(clientX) - bounds.left) / Math.max(1, bounds.width)));
}

export function normalizedPoint(elementId, clientX, clientY) {
    const element = document.getElementById(elementId);
    if (!element) return { x: 0, y: 0 };
    const bounds = element.getBoundingClientRect();
    return {
        x: Math.max(0, Math.min(1, (Number(clientX) - bounds.left) / Math.max(1, bounds.width))),
        y: Math.max(0, Math.min(1, (Number(clientY) - bounds.top) / Math.max(1, bounds.height)))
    };
}

export function mediaClipPath(points) {
    const values = Array.isArray(points) ? points : [];
    if (values.length < 3) return '';
    return `polygon(${values.map(point => `${Math.max(0, Math.min(1, Number(point?.x) || 0)) * 100}% ${Math.max(0, Math.min(1, Number(point?.y) || 0)) * 100}%`).join(',')})`;
}
