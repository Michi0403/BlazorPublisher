// javascript-diagnostics: guarded
var publisherStudioDiagnostics = globalThis.publisherStudioJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`PublisherStudio JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("PublisherStudio fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`PublisherStudio fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
const studioStates = new Map();
const RECORDING_TRANSFER_CHUNK_SIZE = 18 * 1024;

function baseMimeType(value, fallback = 'application/octet-stream') { try {
    const mimeType = String(value || '').split(';', 1)[0].trim().toLowerCase();
    return mimeType.includes('/') ? mimeType : fallback;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:baseMimeType@6', __javascriptError); throw __javascriptError; }}

function finiteMediaTime(value, fallback = 0) { try {
    const number = Number(value);
    return Number.isFinite(number) ? Math.max(0, number) : Math.max(0, Number(fallback) || 0);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:finiteMediaTime', __javascriptError); return Math.max(0, Number(fallback) || 0); }}

function normalizeMediaDataUrl(dataUrl, fallbackMimeType = 'application/octet-stream') { try {
    const value = String(dataUrl || '');
    if (!value.startsWith('data:')) return value;
    const marker = value.toLowerCase().lastIndexOf(';base64,');
    if (marker < 0) return value;
    const mimeType = baseMimeType(value.slice(5, marker), fallbackMimeType);
    return `data:${mimeType};base64,${value.slice(marker + 8)}`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:normalizeMediaDataUrl@11', __javascriptError); throw __javascriptError; }}

function mediaDropKind(file) { try {
    const name = String(file?.name || '').toLowerCase();
    const mime = baseMimeType(file?.type || '', '');
    if (/\.(otio|otioz|mlt|kdenlive|xges|osp|edl)$/.test(name)) return 'project';
    if (mime.startsWith('video/') || /\.(mp4|m4v|webm|ogv|mov)$/.test(name)) return 'video';
    if (mime.startsWith('audio/') || /\.(mp3|wav|oga|ogg|m4a|aac|flac)$/.test(name)) return 'audio';
    return '';
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:mediaDropKind@20', __javascriptError); throw __javascriptError; }}

function assignMediaDrop(inputId, file) { try {
    const input = document.getElementById(inputId);
    if (!(input instanceof HTMLInputElement) || input.type !== 'file' || !(file instanceof File)) return false;
    const transfer = new DataTransfer();
    transfer.items.add(file);
    input.value = '';
    input.files = transfer.files;
    input.dispatchEvent(new Event('change', { bubbles: true }));
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:assignMediaDrop@29', __javascriptError); throw __javascriptError; }}

function sharedMediaStudioDragRead(transfer, kind) { try {
    return globalThis.publisherStudio?.readStudioDragTransfer?.(transfer, kind) || null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:sharedMediaStudioDragRead', __javascriptError); return null; }}

function sharedMediaStudioDragConfigure(transfer, payload) { try {
    return globalThis.publisherStudio?.configureStudioDragTransfer?.(transfer, payload) || false;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:sharedMediaStudioDragConfigure', __javascriptError); return false; }}

function sharedMediaStudioDescriptor(transfer) { try {
    return globalThis.publisherStudio?.studioMediaDescriptorFromTransfer?.(transfer) || null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:sharedMediaStudioDescriptor', __javascriptError); return null; }}

async function sharedMediaStudioFile(transfer) { try {
    if (globalThis.publisherStudio?.fileFromStudioDragTransfer)
        return await globalThis.publisherStudio.fileFromStudioDragTransfer(transfer);
    return transfer?.files?.[0] || null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:sharedMediaStudioFile', __javascriptError); throw __javascriptError; }}

function clearSequenceDrop(state) { try {
    const timeline = document.getElementById('media-studio-sequence-timeline');
    timeline?.classList.remove('media-sequence-drop-target');
    timeline?.style.removeProperty('--media-sequence-drop-position');
    state?.dropRoot?.querySelectorAll?.('.media-sequence-segment.media-segment-dragging')?.forEach(segment => segment.classList.remove('media-segment-dragging'));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:clearSequenceDrop', __javascriptError); }}

function sequenceInsertionAt(target, clientX) { try {
    const timeline = target?.closest?.('.media-sequence-timeline');
    if (!timeline) return null;
    const duration = Math.max(.01, Number(timeline.dataset.sequenceDuration) || 0);
    const bounds = timeline.getBoundingClientRect();
    const ratio = Math.max(0, Math.min(1, (Number(clientX) - bounds.left) / Math.max(1, bounds.width)));
    const seconds = ratio * duration;
    timeline.style.setProperty('--media-sequence-drop-position', `${ratio * 100}%`);
    timeline.classList.add('media-sequence-drop-target');
    return { seconds, ratio };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:sequenceInsertionAt', __javascriptError); return null; }}

function releaseMediaDropBindings(state) { try {
    const root = state?.dropRoot;
    const handlers = state?.dropHandlers;
    if (root && handlers) {
        root.removeEventListener('dragenter', handlers.dragenter);
        root.removeEventListener('dragover', handlers.dragover);
        root.removeEventListener('dragleave', handlers.dragleave);
        root.removeEventListener('drop', handlers.drop);
        root.removeEventListener('dragstart', handlers.dragstart);
        root.removeEventListener('dragend', handlers.dragend);
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
        clearSequenceDrop(state);
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:releaseMediaDropBindings@40', __javascriptError); throw __javascriptError; }}

function clearVideoEffectDropMarkers(root) { try {
    root?.querySelectorAll?.('.media-video-layer-row,.media-video-filter-row')?.forEach(row => {
        row.classList.remove('media-effect-dragging', 'media-effect-drop-before', 'media-effect-drop-after');
    });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:clearVideoEffectDropMarkers', __javascriptError); }}

function effectDropTarget(target, kind, clientY) { try {
    const selector = kind === 'video-effect-filter'
        ? '.media-video-filter-row[data-video-filter-id]'
        : '.media-video-layer-row[data-video-layer-id]';
    const row = target?.closest?.(selector);
    if (!row) return null;
    const bounds = row.getBoundingClientRect();
    const placeAfter = Number(clientY) >= bounds.top + bounds.height / 2;
    row.classList.remove('media-effect-drop-before', 'media-effect-drop-after');
    row.classList.add(placeAfter ? 'media-effect-drop-after' : 'media-effect-drop-before');
    return {
        id: kind === 'video-effect-filter' ? String(row.dataset.videoFilterId || '') : String(row.dataset.videoLayerId || ''),
        placeAfter
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:effectDropTarget', __javascriptError); return null; }}

function bindMediaDrop(state, rootId, sourceInputId, insertInputId, projectInputId, expectedKind) { try {
    releaseMediaDropBindings(state);
    const root = document.getElementById(rootId);
    if (!root) return;
    const descriptor = event => { try {
        const file = event.dataTransfer?.files?.[0];
        if (file) return file;
        const item = [...(event.dataTransfer?.items || [])].find(candidate => { try { return (candidate.kind === 'file'); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:[...(event.dataTransfer?.items || [])].find@68', __javascriptError); throw __javascriptError; } });
        if (item) return { name: '', type: item.type || '' };
        const media = sharedMediaStudioDescriptor(event.dataTransfer);
        return media ? { name: media.filename || '', type: media.mime || '' } : null;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:descriptor@65', __javascriptError); throw __javascriptError; }};
    const modeAt = target => { try { return (target?.closest?.('.media-sequence-editor,.media-range-selector,.media-video-time-overlay') ? 'insert' : 'replace'); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:modeAt@71', __javascriptError); throw __javascriptError; } };
    const videoInsertionAt = (target, clientX) => { try {
        const overlay = target?.closest?.('.media-video-time-overlay');
        if (!overlay) return null;
        const duration = Math.max(.01, Number(overlay.dataset.duration) || 0);
        const trimStart = Math.max(0, Math.min(duration, Number(overlay.dataset.trimStart) || 0));
        const trimEnd = Math.max(trimStart + .01, Math.min(duration, Number(overlay.dataset.trimEnd) || duration));
        const visibleSpan = Math.max(.01, trimEnd - trimStart);
        const pointSelection = overlay.dataset.selectionPoint === 'true';
        const selectionStart = Math.max(trimStart, Math.min(trimEnd, Number(overlay.dataset.selectionStart) || trimStart));
        const selectionEnd = Math.max(selectionStart, Math.min(trimEnd, Number(overlay.dataset.selectionEnd) || selectionStart));
        const bounds = overlay.getBoundingClientRect();
        const ratio = Math.max(0, Math.min(1, (Number(clientX) - bounds.left) / Math.max(1, bounds.width)));
        const raw = trimStart + ratio * visibleSpan;
        const seconds = pointSelection ? selectionStart : Math.max(selectionStart, Math.min(selectionEnd, raw));
        overlay.style.setProperty('--video-drop-position', `${Math.max(0, Math.min(100, (seconds - trimStart) / visibleSpan * 100))}%`);
        overlay.classList.add('media-video-drop-target');
        return { seconds, pointSelection };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:videoInsertionAt@72', __javascriptError); throw __javascriptError; }};
    const clearVideoInsertion = () => { try {
        const overlay = document.getElementById(state.timeOverlayId || '');
        overlay?.classList.remove('media-video-drop-target');
        overlay?.style.removeProperty('--video-drop-position');
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:clearVideoInsertion@90', __javascriptError); throw __javascriptError; }};
    const show = mode => { try {
        root.classList.add('media-file-drag-active');
        root.dataset.mediaDropMode = mode;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:show@95', __javascriptError); throw __javascriptError; }};
    const clear = () => { try {
        state.dropDepth = 0;
        root.classList.remove('media-file-drag-active');
        root.removeAttribute('data-media-drop-mode');
        clearVideoInsertion();
        clearSequenceDrop(state);
        clearVideoEffectDropMarkers(root);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:clear@99', __javascriptError); throw __javascriptError; }};
    const handlers = {
        dragstart: event => { try {
            if (!event.dataTransfer) return;
            const filterRow = event.target?.closest?.('.media-video-filter-row[data-video-filter-id]');
            if (filterRow) {
                const id = String(filterRow.dataset.videoFilterId || '');
                sharedMediaStudioDragConfigure(event.dataTransfer, { effectAllowed: 'move', internalKind: 'video-effect-filter', id, name: filterRow.querySelector('span')?.textContent || 'Video effect' });
                filterRow.classList.add('media-effect-dragging');
                return;
            }
            const layerRow = event.target?.closest?.('.media-video-layer-row[data-video-layer-id]');
            if (layerRow) {
                const id = String(layerRow.dataset.videoLayerId || '');
                sharedMediaStudioDragConfigure(event.dataTransfer, { effectAllowed: 'move', internalKind: 'video-effect-layer', id, name: layerRow.querySelector('span')?.textContent || 'Video layer' });
                layerRow.classList.add('media-effect-dragging');
                return;
            }
            const segment = event.target?.closest?.('.media-sequence-segment[data-media-segment-id]');
            if (!segment) return;
            const segmentId = String(segment.dataset.mediaSegmentId || '');
            const timeline = segment.closest?.('.media-sequence-timeline');
            const preview = document.getElementById(state.id || '');
            const source = String(timeline?.dataset?.selectedSegmentId === segmentId
                ? (preview?.currentSrc || preview?.getAttribute?.('src') || '')
                : '');
            const mime = String(segment.dataset.mediaSegmentMime || (expectedKind === 'video' ? 'video/webm' : 'audio/webm'));
            const name = String(segment.dataset.mediaSegmentName || (expectedKind === 'video' ? 'Video clip' : 'Audio clip'));
            let filename = name.replace(/[<>:"/\\|?*\u0000-\u001f]+/g, '-').trim() || 'media-clip';
            const extension = mime.includes('mp4') ? 'mp4' : mime.includes('mpeg') ? 'mp3' : mime.includes('ogg') ? (expectedKind === 'video' ? 'ogv' : 'ogg') : 'webm';
            if (!new RegExp(`\\.${extension}$`, 'i').test(filename)) filename += `.${extension}`;
            sharedMediaStudioDragConfigure(event.dataTransfer, {
                effectAllowed: 'copyMove', internalKind: 'media-segment', id: segmentId,
                name, filename, mime, href: source
            });
            segment.classList.add('media-segment-dragging');
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:dragstart:segment', __javascriptError); throw __javascriptError; }},
        dragend: event => { try { clearSequenceDrop(state); clearVideoEffectDropMarkers(root); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:dragend:segment', __javascriptError); }},
        dragenter: event => { try {
            const effectLayer = sharedMediaStudioDragRead(event.dataTransfer, 'video-effect-layer');
            const effectFilter = sharedMediaStudioDragRead(event.dataTransfer, 'video-effect-filter');
            if (effectLayer?.id || effectFilter?.id) {
                event.preventDefault();
                state.dropDepth++;
                return;
            }
            const file = descriptor(event);
            if (!file) return;
            event.preventDefault();
            state.dropDepth++;
            show(modeAt(event.target));
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:dragenter@106', __javascriptError); throw __javascriptError; }},
        dragover: event => { try {
            const effectLayer = sharedMediaStudioDragRead(event.dataTransfer, 'video-effect-layer');
            const effectFilter = sharedMediaStudioDragRead(event.dataTransfer, 'video-effect-filter');
            if (effectLayer?.id || effectFilter?.id) {
                const kind = effectFilter?.id ? 'video-effect-filter' : 'video-effect-layer';
                clearVideoEffectDropMarkers(root);
                const target = effectDropTarget(event.target, kind, event.clientY);
                if (!target || target.id === String((effectFilter || effectLayer).id)) return;
                event.preventDefault();
                event.stopPropagation();
                event.dataTransfer.dropEffect = 'move';
                return;
            }
            const internal = sharedMediaStudioDragRead(event.dataTransfer, 'media-segment');
            if (internal?.id) {
                const insertion = sequenceInsertionAt(event.target, event.clientX);
                if (!insertion) return;
                event.preventDefault();
                event.stopPropagation();
                event.dataTransfer.dropEffect = 'move';
                return;
            }
            const file = descriptor(event);
            if (!file) return;
            event.preventDefault();
            event.stopPropagation();
            const kind = mediaDropKind(file);
            event.dataTransfer.dropEffect = kind === expectedKind || (expectedKind === 'video' && kind === 'project') ? 'copy' : 'none';
            show(kind === 'project' ? 'project' : modeAt(event.target));
            const sequenceInsertion = sequenceInsertionAt(event.target, event.clientX);
            if (!sequenceInsertion && !videoInsertionAt(event.target, event.clientX)) clearVideoInsertion();
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:dragover@113', __javascriptError); throw __javascriptError; }},
        dragleave: event => { try {
            if (event.relatedTarget && root.contains(event.relatedTarget)) return;
            state.dropDepth = Math.max(0, state.dropDepth - 1);
            if (state.dropDepth === 0) clear();
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:dragleave@123', __javascriptError); throw __javascriptError; }},
        drop: async event => { try {
            const effectLayer = sharedMediaStudioDragRead(event.dataTransfer, 'video-effect-layer');
            const effectFilter = sharedMediaStudioDragRead(event.dataTransfer, 'video-effect-filter');
            if (effectLayer?.id || effectFilter?.id) {
                const kind = effectFilter?.id ? 'video-effect-filter' : 'video-effect-layer';
                const target = effectDropTarget(event.target, kind, event.clientY);
                event.preventDefault();
                event.stopPropagation();
                clearVideoEffectDropMarkers(root);
                if (!target || target.id === String((effectFilter || effectLayer).id)) return;
                if (kind === 'video-effect-filter')
                    await state.dotnet?.invokeMethodAsync('MediaStudioVideoFilterDropped', String(effectFilter.id), target.id, target.placeAfter);
                else
                    await state.dotnet?.invokeMethodAsync('MediaStudioVideoLayerDropped', String(effectLayer.id), target.id, target.placeAfter);
                return;
            }
            const internal = sharedMediaStudioDragRead(event.dataTransfer, 'media-segment');
            if (internal?.id) {
                const sequenceInsertion = sequenceInsertionAt(event.target, event.clientX);
                if (!sequenceInsertion) { clear(); return; }
                event.preventDefault();
                event.stopPropagation();
                clear();
                await state.dotnet?.invokeMethodAsync('MediaStudioTimelineSegmentDropped', String(internal.id), sequenceInsertion.seconds);
                return;
            }
            const sequenceInsertion = sequenceInsertionAt(event.target, event.clientX);
            const file = await sharedMediaStudioFile(event.dataTransfer);
            if (!file) return;
            event.preventDefault();
            event.stopPropagation();
            const actualKind = mediaDropKind(file);
            const mode = actualKind === 'project' ? 'project' : modeAt(event.target);
            const videoInsertion = mode === 'insert' && !sequenceInsertion ? videoInsertionAt(event.target, event.clientX) : null;
            clear();
            if (actualKind === 'project' && expectedKind === 'video') {
                if (!assignMediaDrop(projectInputId, file))
                    state.dotnet?.invokeMethodAsync('MediaStudioFileDropRejected', 'The dropped project file could not be forwarded to the open-project importer.').catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:promise-catch@140', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:state.dotnet?.invokeMethodAsync(\'MediaStudioFileDropRejected\', \'The dr@140', __javascriptError); throw __javascriptError; }});
                return;
            }
            if (actualKind !== expectedKind) {
                state.dotnet?.invokeMethodAsync(
                    'MediaStudioFileDropRejected',
                    `The dropped file '${file.name || 'file'}' is ${actualKind || 'not recognized as media'}; this Studio accepts ${expectedKind} files.`).catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:promise-catch@144', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:state.dotnet?.invokeMethodAsync( \'MediaStudioFileDropRejected\', `The d@146', __javascriptError); throw __javascriptError; }});
                return;
            }
            if (sequenceInsertion && mode === 'insert') {
                try {
                    await state.dotnet?.invokeMethodAsync('MediaStudioDropTimelinePointSelected', sequenceInsertion.seconds);
                } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch:timeline-drop', __caughtJavaScriptError); }
            } else if (videoInsertion) {
                try {
                    await state.dotnet?.invokeMethodAsync(
                        'MediaStudioDropInsertionPointSelected',
                        videoInsertion.seconds,
                        videoInsertion.pointSelection);
                } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@155', __caughtJavaScriptError);  }
            }
            const inputId = mode === 'insert' ? insertInputId : sourceInputId;
            if (!assignMediaDrop(inputId, file))
                state.dotnet?.invokeMethodAsync('MediaStudioFileDropRejected', 'The dropped media file could not be forwarded to the Studio importer.').catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:promise-catch@159', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:state.dotnet?.invokeMethodAsync(\'MediaStudioFileDropRejected\', \'The dr@159', __javascriptError); throw __javascriptError; }});
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:drop@128', __javascriptError); throw __javascriptError; }}
    };
    state.dropRoot = root;
    state.dropHandlers = handlers;
    state.dropDepth = 0;
    root.addEventListener('dragenter', handlers.dragenter);
    root.addEventListener('dragover', handlers.dragover);
    root.addEventListener('dragleave', handlers.dragleave);
    root.addEventListener('drop', handlers.drop);
    root.addEventListener('dragstart', handlers.dragstart);
    root.addEventListener('dragend', handlers.dragend);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:bindMediaDrop@61', __javascriptError); throw __javascriptError; }}

function stateFor(id) { try {
    let state = studioStates.get(id);
    if (!state) {
        state = {
            id,
            sessionId: '',
            dotnet: null,
            recorder: null,
            stream: null,
            recordingPreviewTimer: 0,
            recordingPreviewElement: null,
            chunks: [],
            rangeHandler: null,
            stopAt: null,
            discardRecording: false,
            retainedRecordingBlob: null,
            retainedRecordingUrl: '',
            retainedRecordingKind: '',
            retainedRecordingMimeType: '',
            retainedRecordingFileName: '',
            retainedRecordingInfo: null,
            retainedRecordingCommitted: false,
            recordingFinalizationError: '',
            recordingStopRequested: false,
            recordingFinalizing: false,
            recordingStartedAtMs: 0,
            recordingStopRequestedAtMs: 0,
            recordingOptions: null,
            renderExport: null,
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
            timeOverlayLostCaptureHandler: null,
            timeOverlayPlayHandler: null,
            timeOverlayPauseHandler: null,
            timeOverlayGesture: null,
            sequenceTimelineId: '',
            sequencePointerDownHandler: null,
            sequencePointerMoveHandler: null,
            sequencePointerUpHandler: null,
            sequenceGesture: null,
            lastReportedDuration: 0,
            durationReportPending: false,
            durationProbePending: false,
            durationProbeSeekedHandler: null,
            durationProbeTimer: 0,
            durationProbeRestoreTime: 0,
            playCommandVersion: 0,
            dropRoot: null,
            dropHandlers: null,
            dropDepth: 0
        };
        studioStates.set(id, state);
    }
    return state;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:stateFor@171', __javascriptError); throw __javascriptError; }}

function releaseRetainedRecording(state) { try {
    if (state.retainedRecordingUrl) {
        try { URL.revokeObjectURL(state.retainedRecordingUrl); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@238', __caughtJavaScriptError);  }
    }
    state.retainedRecordingBlob = null;
    state.retainedRecordingUrl = '';
    state.retainedRecordingKind = '';
    state.retainedRecordingMimeType = '';
    state.retainedRecordingFileName = '';
    state.retainedRecordingInfo = null;
    state.retainedRecordingCommitted = false;
    state.recordingFinalizationError = '';
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:releaseRetainedRecording@236', __javascriptError); throw __javascriptError; }}

function recordingExtension(mimeType) { try {
    const normalized = baseMimeType(mimeType);
    if (normalized.includes('mp4')) return 'mp4';
    if (normalized.includes('ogg')) return 'ogg';
    if (normalized.includes('wav')) return 'wav';
    return 'webm';
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:recordingExtension@247', __javascriptError); throw __javascriptError; }}

function recordingFileName(kind, mimeType) { try {
    return `Recorded ${kind === 'video' ? 'Video' : 'Audio'}.${recordingExtension(mimeType)}`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:recordingFileName@255', __javascriptError); throw __javascriptError; }}

function arrayBufferToBase64(buffer) { try {
    const bytes = new Uint8Array(buffer);
    const characterChunk = 0x8000;
    let binary = '';
    for (let offset = 0; offset < bytes.length; offset += characterChunk)
        binary += String.fromCharCode(...bytes.subarray(offset, Math.min(bytes.length, offset + characterChunk)));
    return btoa(binary);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:arrayBufferToBase64@259', __javascriptError); throw __javascriptError; }}


function readEbmlVint(bytes, offset, maximumWidth, keepMarker) { try {
    if (!(bytes instanceof Uint8Array) || offset < 0 || offset >= bytes.length) return null;
    const first = bytes[offset];
    let marker = 0x80;
    let width = 1;
    while (width <= maximumWidth && (first & marker) === 0) {
        marker >>= 1;
        width++;
    }
    if (width > maximumWidth || offset + width > bytes.length || marker === 0) return null;

    let value = keepMarker ? first : (first & (marker - 1));
    let unknown = !keepMarker && (first & (marker - 1)) === marker - 1;
    for (let index = 1; index < width; index++) {
        value = value * 256 + bytes[offset + index];
        if (unknown && bytes[offset + index] !== 0xff) unknown = false;
    }
    return { value, width, unknown };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:readEbmlVint', __javascriptError); throw __javascriptError; }}

function encodeEbmlSize(value, preferredWidth = 0) { try {
    const normalized = Math.max(0, Math.trunc(Number(value) || 0));
    let width = Math.max(0, Math.trunc(Number(preferredWidth) || 0));
    const fits = candidate => candidate >= 1
        && candidate <= 8
        && normalized < Math.pow(2, candidate * 7) - 1;
    if (!fits(width)) {
        width = 1;
        while (width < 8 && !fits(width)) width++;
    }
    if (!fits(width)) throw new RangeError('EBML size is too large to encode safely.');

    const bytes = new Uint8Array(width);
    let remaining = normalized;
    for (let index = width - 1; index >= 0; index--) {
        bytes[index] = remaining & 0xff;
        remaining = Math.floor(remaining / 256);
    }
    bytes[0] |= 1 << (8 - width);
    return bytes;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:encodeEbmlSize', __javascriptError); throw __javascriptError; }}

function locateWebmInfo(bytes) { try {
    if (!(bytes instanceof Uint8Array) || bytes.length < 16) return null;
    let cursor = 0;
    const ebmlId = readEbmlVint(bytes, cursor, 4, true);
    if (!ebmlId || ebmlId.value !== 0x1a45dfa3) return null;
    const ebmlSize = readEbmlVint(bytes, cursor + ebmlId.width, 8, false);
    if (!ebmlSize || ebmlSize.unknown) return null;
    cursor += ebmlId.width + ebmlSize.width + ebmlSize.value;
    if (cursor >= bytes.length) return null;

    const segmentId = readEbmlVint(bytes, cursor, 4, true);
    if (!segmentId || segmentId.value !== 0x18538067) return null;
    const segmentSize = readEbmlVint(bytes, cursor + segmentId.width, 8, false);
    if (!segmentSize) return null;
    cursor += segmentId.width + segmentSize.width;
    const segmentEnd = segmentSize.unknown ? bytes.length : Math.min(bytes.length, cursor + segmentSize.value);

    while (cursor < segmentEnd) {
        const elementId = readEbmlVint(bytes, cursor, 4, true);
        if (!elementId) return null;
        const sizeOffset = cursor + elementId.width;
        const elementSize = readEbmlVint(bytes, sizeOffset, 8, false);
        if (!elementSize || elementSize.unknown) return null;
        const contentOffset = sizeOffset + elementSize.width;
        const contentEnd = contentOffset + elementSize.value;
        if (contentEnd > segmentEnd) return null;
        if (elementId.value === 0x1549a966)
            return { sizeOffset, sizeWidth: elementSize.width, contentOffset, contentEnd, contentSize: elementSize.value };
        cursor = contentEnd;
    }
    return null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:locateWebmInfo', __javascriptError); throw __javascriptError; }}

function webmTimecodeScaleAndDuration(bytes, info) { try {
    let cursor = info.contentOffset;
    let timecodeScale = 1000000;
    let duration = null;
    while (cursor < info.contentEnd) {
        const elementId = readEbmlVint(bytes, cursor, 4, true);
        if (!elementId) break;
        const elementSize = readEbmlVint(bytes, cursor + elementId.width, 8, false);
        if (!elementSize || elementSize.unknown) break;
        const contentOffset = cursor + elementId.width + elementSize.width;
        const contentEnd = contentOffset + elementSize.value;
        if (contentEnd > info.contentEnd) break;

        if (elementId.value === 0x2ad7b1 && elementSize.value > 0 && elementSize.value <= 8) {
            let scale = 0;
            for (let index = contentOffset; index < contentEnd; index++)
                scale = scale * 256 + bytes[index];
            if (Number.isFinite(scale) && scale > 0) timecodeScale = scale;
        } else if (elementId.value === 0x4489 && (elementSize.value === 4 || elementSize.value === 8)) {
            duration = { contentOffset, byteLength: elementSize.value };
        }
        cursor = contentEnd;
    }
    return { timecodeScale, duration };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:webmTimecodeScaleAndDuration', __javascriptError); throw __javascriptError; }}

async function webmBlobWithDuration(blob, durationSeconds, mimeType) { try {
    const normalizedMime = baseMimeType(mimeType || blob?.type, '');
    const duration = Number(durationSeconds);
    if (!(blob instanceof Blob)
        || !blob.size
        || !normalizedMime.toLowerCase().includes('webm')
        || !Number.isFinite(duration)
        || duration <= 0)
        return blob;

    const source = new Uint8Array(await blob.arrayBuffer());
    const info = locateWebmInfo(source);
    if (!info) return blob;
    const metadata = webmTimecodeScaleAndDuration(source, info);
    const durationTicks = duration * 1000000000 / Math.max(1, metadata.timecodeScale);
    if (!Number.isFinite(durationTicks) || durationTicks <= 0) return blob;

    if (metadata.duration) {
        const patched = source.slice();
        const view = new DataView(patched.buffer, patched.byteOffset, patched.byteLength);
        if (metadata.duration.byteLength === 8)
            view.setFloat64(metadata.duration.contentOffset, durationTicks, false);
        else
            view.setFloat32(metadata.duration.contentOffset, durationTicks, false);
        return new Blob([patched], { type: normalizedMime || blob.type });
    }

    const durationElement = new Uint8Array(11);
    durationElement[0] = 0x44;
    durationElement[1] = 0x89;
    durationElement[2] = 0x88;
    new DataView(durationElement.buffer).setFloat64(3, durationTicks, false);

    const newInfoSize = info.contentSize + durationElement.byteLength;
    const encodedSize = encodeEbmlSize(newInfoSize, info.sizeWidth);
    const patched = new Uint8Array(
        info.sizeOffset
        + encodedSize.byteLength
        + info.contentSize
        + durationElement.byteLength
        + (source.byteLength - info.contentEnd));
    let target = 0;
    patched.set(source.subarray(0, info.sizeOffset), target);
    target += info.sizeOffset;
    patched.set(encodedSize, target);
    target += encodedSize.byteLength;
    patched.set(source.subarray(info.contentOffset, info.contentEnd), target);
    target += info.contentSize;
    patched.set(durationElement, target);
    target += durationElement.byteLength;
    patched.set(source.subarray(info.contentEnd), target);
    return new Blob([patched], { type: normalizedMime || blob.type });
 } catch (__javascriptError) {
    publisherStudioDiagnostics.report('js/mediaStudioInterop.js:webmBlobWithDuration', __javascriptError);
    return blob;
 }}

function isInterruptedPlaybackError(error) { try {
    const name = String(error?.name || '');
    const message = String(error?.message || error || '').toLowerCase();
    return name === 'AbortError'
        || message.includes('play() request was interrupted')
        || message.includes('interrupted by a call to pause')
        || message.includes('interrupted by a new load request');
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:isInterruptedPlaybackError@268', __javascriptError); throw __javascriptError; }}


function stopRecordingPreviewWatch(state) { try {
    if (state.recordingPreviewTimer) clearInterval(state.recordingPreviewTimer);
    state.recordingPreviewTimer = 0;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:stopRecordingPreviewWatch', __javascriptError); throw __javascriptError; }}

function detachRecordingPreview(state) { try {
    stopRecordingPreviewWatch(state);
    const candidates = [state.recordingPreviewElement, mediaElement(state.id)].filter(Boolean);
    for (const preview of new Set(candidates)) {
        if (!(preview instanceof HTMLVideoElement) || preview.srcObject !== state.stream) continue;
        cancelRangePlayback(state, preview, true);
        preview.srcObject = null;
    }
    state.recordingPreviewElement = null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:detachRecordingPreview', __javascriptError); throw __javascriptError; }}

function releaseRecordingCapture(state) { try {
    detachRecordingPreview(state);
    const stream = state.stream;
    if (!stream) return false;
    for (const track of stream.getTracks?.() || []) {
        try { if (track.readyState !== 'ended') track.stop(); }
        catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch:release-recording-track', __caughtJavaScriptError); }
    }
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:releaseRecordingCapture', __javascriptError); throw __javascriptError; }}

function ensureRecordingPreview(state) { try {
    if (!state.stream) return false;
    const preview = mediaElement(state.id);
    if (!(preview instanceof HTMLVideoElement)) return false;
    if (state.recordingPreviewElement && state.recordingPreviewElement !== preview && state.recordingPreviewElement.srcObject === state.stream) {
        try { state.recordingPreviewElement.pause(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch:recording-preview-pause', __caughtJavaScriptError); }
        state.recordingPreviewElement.srcObject = null;
    }
    if (preview.srcObject !== state.stream) {
        cancelRangePlayback(state, preview, true);
        preview.removeAttribute('src');
        preview.srcObject = state.stream;
    }
    state.recordingPreviewElement = preview;
    preview.muted = true;
    preview.playsInline = true;
    if (preview.paused || preview.readyState < HTMLMediaElement.HAVE_CURRENT_DATA) {
        const request = preview.play();
        if (request?.catch) request.catch(error => { try {
            const name = String(error?.name || '');
            if (!isInterruptedPlaybackError(error) && name !== 'NotAllowedError')
                publisherStudioDiagnostics.report('js/mediaStudioInterop.js:recording-preview-play', error);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:recording-preview-play-callback', __javascriptError); throw __javascriptError; }});
    }
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:ensureRecordingPreview', __javascriptError); throw __javascriptError; }}

function startRecordingPreviewWatch(state) { try {
    stopRecordingPreviewWatch(state);
    ensureRecordingPreview(state);
    state.recordingPreviewTimer = setInterval(() => { try {
        if (!state.stream) { stopRecordingPreviewWatch(state); return; }
        ensureRecordingPreview(state);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:recording-preview-watch', __javascriptError); }}, 1000);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:startRecordingPreviewWatch', __javascriptError); throw __javascriptError; }}

function cancelRangePlayback(state, element, pause = true) { try {
    state.playCommandVersion = (Number(state.playCommandVersion) || 0) + 1;
    if (state.rangeHandler) element?.removeEventListener?.('timeupdate', state.rangeHandler);
    state.rangeHandler = null;
    state.stopAt = null;
    if (pause) {
        try { element?.pause?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@283', __caughtJavaScriptError);  }
    }
    return state.playCommandVersion;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:cancelRangePlayback@277', __javascriptError); throw __javascriptError; }}

function mediaElement(id) { try {
    const element = document.getElementById(id);
    return element instanceof HTMLMediaElement ? element : null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:mediaElement@288', __javascriptError); throw __javascriptError; }}

function releaseFrameOverlayBindings(state) { try {
    const video = mediaElement(state.id);
    const overlay = document.getElementById(state.frameOverlayId);
    if (video && state.frameMetadataHandler) {
        video.removeEventListener('loadedmetadata', state.frameMetadataHandler);
        video.removeEventListener('loadeddata', state.frameMetadataHandler);
    }
    if (overlay && state.frameOverlayMoveHandler) overlay.removeEventListener('pointermove', state.frameOverlayMoveHandler);
    if (overlay && state.frameNodePointerDownHandler) overlay.removeEventListener('pointerdown', state.frameNodePointerDownHandler, true);
    if (overlay && state.frameNodeContextHandler) overlay.removeEventListener('contextmenu', state.frameNodeContextHandler, true);
    if (overlay && state.frameNodePointerMoveHandler) overlay.removeEventListener('pointermove', state.frameNodePointerMoveHandler, true);
    if (overlay && state.frameNodePointerUpHandler) {
        overlay.removeEventListener('pointerup', state.frameNodePointerUpHandler, true);
        overlay.removeEventListener('pointercancel', state.frameNodePointerUpHandler, true);
    }
    if (overlay && state.frameOverlayLeaveHandler) {
        overlay.removeEventListener('pointerleave', state.frameOverlayLeaveHandler);
        overlay.removeEventListener('pointercancel', state.frameOverlayLeaveHandler);
    }
    try { state.frameResizeObserver?.disconnect(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@312', __caughtJavaScriptError);  }
    state.frameResizeObserver = null;
    state.frameMetadataHandler = null;
    state.frameOverlayMoveHandler = null;
    state.frameOverlayLeaveHandler = null;
    state.frameNodePointerDownHandler = null;
    state.frameNodeContextHandler = null;
    state.frameNodePointerMoveHandler = null;
    state.frameNodePointerUpHandler = null;
    state.frameNodeGesture = null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:releaseFrameOverlayBindings@293', __javascriptError); throw __javascriptError; }}

function releaseVideoTimeOverlayBindings(state) { try {
    const video = mediaElement(state.id);
    const overlay = document.getElementById(state.timeOverlayId);
    if (overlay && state.timeOverlayPointerDownHandler) overlay.removeEventListener('pointerdown', state.timeOverlayPointerDownHandler);
    if (overlay && state.timeOverlayPointerMoveHandler) overlay.removeEventListener('pointermove', state.timeOverlayPointerMoveHandler);
    if (overlay && state.timeOverlayPointerUpHandler) overlay.removeEventListener('pointerup', state.timeOverlayPointerUpHandler);
    if (overlay && state.timeOverlayPointerCancelHandler) overlay.removeEventListener('pointercancel', state.timeOverlayPointerCancelHandler);
    if (overlay && state.timeOverlayDoubleClickHandler) overlay.removeEventListener('dblclick', state.timeOverlayDoubleClickHandler);
    if (overlay && state.timeOverlayLostCaptureHandler) overlay.removeEventListener('lostpointercapture', state.timeOverlayLostCaptureHandler);
    const playButton = overlay?.querySelector?.('[data-video-command="play"]');
    const pauseButton = overlay?.querySelector?.('[data-video-command="pause"]');
    if (playButton && state.timeOverlayPlayHandler) playButton.removeEventListener('click', state.timeOverlayPlayHandler);
    if (pauseButton && state.timeOverlayPauseHandler) pauseButton.removeEventListener('click', state.timeOverlayPauseHandler);
    if (video && state.timeOverlayMetadataHandler) {
        video.removeEventListener('loadedmetadata', state.timeOverlayMetadataHandler);
        video.removeEventListener('loadeddata', state.timeOverlayMetadataHandler);
    }
    if (video && state.timeOverlayDurationHandler)
        video.removeEventListener('durationchange', state.timeOverlayDurationHandler);
    if (video && state.durationProbeSeekedHandler)
        video.removeEventListener('seeked', state.durationProbeSeekedHandler);
    if (video && state.timeOverlayUpdateHandler) {
        video.removeEventListener('timeupdate', state.timeOverlayUpdateHandler);
        video.removeEventListener('seeked', state.timeOverlayUpdateHandler);
    }
    state.timeOverlayPointerDownHandler = null;
    state.timeOverlayPointerMoveHandler = null;
    state.timeOverlayPointerUpHandler = null;
    state.timeOverlayPointerCancelHandler = null;
    state.timeOverlayDoubleClickHandler = null;
    state.timeOverlayLostCaptureHandler = null;
    state.timeOverlayPlayHandler = null;
    state.timeOverlayPauseHandler = null;
    state.timeOverlayMetadataHandler = null;
    state.timeOverlayDurationHandler = null;
    state.timeOverlayUpdateHandler = null;
    state.timeOverlayGesture = null;
    state.durationReportPending = false;
    state.durationProbePending = false;
    state.durationProbeSeekedHandler = null;
    if (state.durationProbeTimer) clearTimeout(state.durationProbeTimer);
    state.durationProbeTimer = 0;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:releaseVideoTimeOverlayBindings@324', __javascriptError); throw __javascriptError; }}

function syncFrameOverlay(state) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:syncFrameOverlay@368', __javascriptError); throw __javascriptError; }}

function bindFrameOverlay(state, frameStageId, frameOverlayId) { try {
    releaseFrameOverlayBindings(state);
    state.frameStageId = String(frameStageId || '');
    state.frameOverlayId = String(frameOverlayId || '');
    if (!state.frameStageId || !state.frameOverlayId) return;

    const video = mediaElement(state.id);
    const stage = document.getElementById(state.frameStageId);
    const overlay = document.getElementById(state.frameOverlayId);
    if (!(video instanceof HTMLVideoElement) || !stage || !overlay) return;

    state.frameMetadataHandler = () => { try { return (syncFrameOverlay(state)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:state.frameMetadataHandler@418', __javascriptError); throw __javascriptError; } };
    video.addEventListener('loadedmetadata', state.frameMetadataHandler);
    video.addEventListener('loadeddata', state.frameMetadataHandler);

    state.frameOverlayMoveHandler = event => { try {
        const bounds = overlay.getBoundingClientRect();
        const x = Math.max(0, Math.min(bounds.width, Number(event.clientX) - bounds.left));
        const y = Math.max(0, Math.min(bounds.height, Number(event.clientY) - bounds.top));
        overlay.style.setProperty('--media-pointer-x', `${x}px`);
        overlay.style.setProperty('--media-pointer-y', `${y}px`);
        overlay.classList.add('pointer-visible');
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:state.frameOverlayMoveHandler@422', __javascriptError); throw __javascriptError; }};
    state.frameOverlayLeaveHandler = () => { try { return (overlay.classList.remove('pointer-visible')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:state.frameOverlayLeaveHandler@430', __javascriptError); throw __javascriptError; } };
    overlay.addEventListener('pointermove', state.frameOverlayMoveHandler);
    overlay.addEventListener('pointerleave', state.frameOverlayLeaveHandler);
    overlay.addEventListener('pointercancel', state.frameOverlayLeaveHandler);

    const updateFrameNodeVisual = (node, x, y) => { try {
        node.setAttribute('cx', String(x * 1000));
        node.setAttribute('cy', String(y * 1000));
        const nodes = [...overlay.querySelectorAll('[data-frame-node-index]')]
            .sort((left, right) => { try { return (Number(left.dataset.frameNodeIndex) - Number(right.dataset.frameNodeIndex)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:[...overlay.querySelectorAll(\'[data-frame-node-index]\')] .sort@439', __javascriptError); throw __javascriptError; } });
        const points = nodes.map(candidate => { try { return (`${candidate.getAttribute('cx') || '0'},${candidate.getAttribute('cy') || '0'}`); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:nodes.map@440', __javascriptError); throw __javascriptError; } }).join(' ');
        const polyline = overlay.querySelector('.media-frame-cutline');
        const polygon = overlay.querySelector('.media-frame-selection');
        if (polyline) polyline.setAttribute('points', points);
        if (polygon) polygon.setAttribute('points', points);
        const dim = overlay.querySelector('.media-frame-dim');
        if (dim && nodes.length >= 3) {
            const pathPoints = nodes.map(candidate => { try { return (`${candidate.getAttribute('cx') || '0'} ${candidate.getAttribute('cy') || '0'}`); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:nodes.map@447', __javascriptError); throw __javascriptError; } }).join(' L ');
            dim.setAttribute('d', `M 0 0 H 1000 V 1000 H 0 Z M ${pathPoints} Z`);
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:updateFrameNodeVisual@435', __javascriptError); throw __javascriptError; }};
    const normalizedFramePoint = event => { try {
        const bounds = overlay.getBoundingClientRect();
        return {
            x: Math.max(0, Math.min(1, (Number(event.clientX) - bounds.left) / Math.max(1, bounds.width))),
            y: Math.max(0, Math.min(1, (Number(event.clientY) - bounds.top) / Math.max(1, bounds.height)))
        };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:normalizedFramePoint@451', __javascriptError); throw __javascriptError; }};
    state.frameNodePointerDownHandler = event => { try {
        const node = event.target?.closest?.('[data-frame-node-index]');
        if (!(node instanceof SVGCircleElement) || !overlay.classList.contains('active') || event.button !== 0) return;
        event.preventDefault();
        event.stopImmediatePropagation();
        const pointIndex = Number(node.dataset.frameNodeIndex);
        if (!Number.isInteger(pointIndex) || pointIndex < 0) return;
        state.dotnet?.invokeMethodAsync('VideoFramePointSelected', pointIndex).catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:promise-catch@465', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:state.dotnet?.invokeMethodAsync(\'VideoFramePointSelected\', pointIndex)@465', __javascriptError); throw __javascriptError; }});
        overlay.querySelectorAll('[data-frame-node-index].selected').forEach(candidate => { try { return (candidate.classList.remove('selected')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:overlay.querySelectorAll(\'[data-frame-node-index].selected\').forEach@466', __javascriptError); throw __javascriptError; } });
        node.classList.add('selected');
        state.frameNodeGesture = { pointerId: event.pointerId, pointIndex, node };
        try { overlay.setPointerCapture(event.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@469', __caughtJavaScriptError);  }
        node.classList.add('dragging');
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:state.frameNodePointerDownHandler@458', __javascriptError); throw __javascriptError; }};
    state.frameNodeContextHandler = event => { try {
        const node = event.target?.closest?.('[data-frame-node-index]');
        if (!(node instanceof SVGCircleElement) || !overlay.classList.contains('active')) return;
        const pointIndex = Number(node.dataset.frameNodeIndex);
        if (!Number.isInteger(pointIndex) || pointIndex < 0) return;
        state.dotnet?.invokeMethodAsync('VideoFramePointSelected', pointIndex).catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:promise-catch@477', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:state.dotnet?.invokeMethodAsync(\'VideoFramePointSelected\', pointIndex)@477', __javascriptError); throw __javascriptError; }});
        overlay.querySelectorAll('[data-frame-node-index].selected').forEach(candidate => { try { return (candidate.classList.remove('selected')); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:overlay.querySelectorAll(\'[data-frame-node-index].selected\').forEach@478', __javascriptError); throw __javascriptError; } });
        node.classList.add('selected');
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:state.frameNodeContextHandler@472', __javascriptError); throw __javascriptError; }};
    state.frameNodePointerMoveHandler = event => { try {
        const gesture = state.frameNodeGesture;
        if (!gesture || gesture.pointerId !== event.pointerId) return;
        event.preventDefault();
        event.stopImmediatePropagation();
        const point = normalizedFramePoint(event);
        updateFrameNodeVisual(gesture.node, point.x, point.y);
        gesture.x = point.x;
        gesture.y = point.y;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:state.frameNodePointerMoveHandler@481', __javascriptError); throw __javascriptError; }};
    state.frameNodePointerUpHandler = event => { try {
        const gesture = state.frameNodeGesture;
        if (!gesture || gesture.pointerId !== event.pointerId) return;
        event.preventDefault();
        event.stopImmediatePropagation();
        const point = gesture.x == null ? normalizedFramePoint(event) : { x: gesture.x, y: gesture.y };
        updateFrameNodeVisual(gesture.node, point.x, point.y);
        gesture.node.classList.remove('dragging');
        try { overlay.releasePointerCapture(event.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@499', __caughtJavaScriptError);  }
        state.frameNodeGesture = null;
        state.dotnet?.invokeMethodAsync('VideoFramePointCommitted', gesture.pointIndex, point.x, point.y).catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:promise-catch@501', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:state.dotnet?.invokeMethodAsync(\'VideoFramePointCommitted\', gesture.po@501', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:state.frameNodePointerUpHandler@491', __javascriptError); throw __javascriptError; }};
    overlay.addEventListener('pointerdown', state.frameNodePointerDownHandler, true);
    overlay.addEventListener('contextmenu', state.frameNodeContextHandler, true);
    overlay.addEventListener('pointermove', state.frameNodePointerMoveHandler, true);
    overlay.addEventListener('pointerup', state.frameNodePointerUpHandler, true);
    overlay.addEventListener('pointercancel', state.frameNodePointerUpHandler, true);

    if (typeof ResizeObserver !== 'undefined') {
        state.frameResizeObserver = new ResizeObserver(() => { try { return (syncFrameOverlay(state)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:ArrowFunction@510', __javascriptError); throw __javascriptError; } });
        state.frameResizeObserver.observe(stage);
        state.frameResizeObserver.observe(video);
    }
    requestAnimationFrame(() => { try { return (syncFrameOverlay(state)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:requestAnimationFrame@514', __javascriptError); throw __javascriptError; } });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:bindFrameOverlay@407', __javascriptError); throw __javascriptError; }}

function videoTimeData(overlay) { try {
    const duration = Math.max(.01, Number(overlay?.dataset?.duration) || 0);
    const pointSelection = overlay?.dataset?.selectionPoint === 'true';
    const trimStart = Math.max(0, Math.min(duration, Number(overlay?.dataset?.trimStart) || 0));
    const trimEnd = Math.max(trimStart, Math.min(duration, Number(overlay?.dataset?.trimEnd) || duration));
    const start = Math.max(0, Math.min(duration, Number(overlay?.dataset?.selectionStart) || 0));
    const end = Math.max(start, Math.min(duration, Number(overlay?.dataset?.selectionEnd) || start));
    return { duration, pointSelection, trimStart, trimEnd, start, end };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:videoTimeData@517', __javascriptError); throw __javascriptError; }}

function videoTimeAt(overlay, clientX, clampToSelection = false) { try {
    const data = videoTimeData(overlay);
    const bounds = overlay.getBoundingClientRect();
    const ratio = Math.max(0, Math.min(1, (Number(clientX) - bounds.left) / Math.max(1, bounds.width)));
    const visibleSpan = Math.max(.01, data.trimEnd - data.trimStart);
    const raw = data.trimStart + ratio * visibleSpan;
    if (!clampToSelection) return raw;
    return data.pointSelection ? data.start : Math.max(data.start, Math.min(data.end, raw));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:videoTimeAt@527', __javascriptError); throw __javascriptError; }}

function formatMediaTime(seconds) { try {
    const safe = Math.max(0, Number(seconds) || 0);
    const minutes = Math.floor(safe / 60);
    const remainder = safe - minutes * 60;
    return `${minutes}:${remainder.toFixed(1).padStart(4, '0')}`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:formatMediaTime@537', __javascriptError); throw __javascriptError; }}

function syncVideoSelectionControls(overlay, start, end, pointSelection) { try {
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
    const sequenceStartHandle = document.getElementById('media-studio-sequence-selection-start');
    const sequenceEndHandle = document.getElementById('media-studio-sequence-selection-end');
    const startPercent = Math.max(0, Math.min(100, timelineStart / sequenceDuration * 100));
    const endPercent = Math.max(0, Math.min(100, timelineEnd / sequenceDuration * 100));
    if (sequenceStartHandle instanceof HTMLElement) {
        sequenceStartHandle.style.left = `${startPercent}%`;
        sequenceStartHandle.hidden = pointSelection;
    }
    if (sequenceEndHandle instanceof HTMLElement) {
        sequenceEndHandle.style.left = `${endPercent}%`;
        sequenceEndHandle.hidden = pointSelection;
    }
    if (sequenceSelection instanceof HTMLElement) {
        sequenceSelection.style.left = `${startPercent}%`;
        sequenceSelection.style.width = pointSelection
            ? '2px'
            : `${Math.max(0, Math.min(100, Math.max(0, timelineEnd - timelineStart) / sequenceDuration * 100))}%`;
        sequenceSelection.classList.toggle('point', pointSelection);
        sequenceSelection.classList.toggle('range', !pointSelection);
        sequenceSelection.title = pointSelection
            ? `${clipName}: source ${formatMediaTime(start)} · project ${formatMediaTime(timelineStart)}`
            : `${clipName}: source ${formatMediaTime(start)} — ${formatMediaTime(end)} · project ${formatMediaTime(timelineStart)} — ${formatMediaTime(timelineEnd)}`;
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:syncVideoSelectionControls@544', __javascriptError); throw __javascriptError; }}

function setVideoTimeVisual(overlay, start, end, pointSelection) { try {
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
    const visibleSpan = Math.max(.01, maximum - minimum);
    overlay.style.setProperty('--video-time-start', `${(safeStart - minimum) / visibleSpan * 100}%`);
    overlay.style.setProperty('--video-time-end', `${(safeEnd - minimum) / visibleSpan * 100}%`);
    overlay.style.setProperty('--video-time-playhead', `${(point - minimum) / visibleSpan * 100}%`);
    syncVideoSelectionControls(overlay, safeStart, safeEnd, pointSelection);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:setVideoTimeVisual@606', __javascriptError); throw __javascriptError; }}

function normalizeVideoTimeRange(start, end, duration, minimum = 0, maximum = duration) { try {
    const safeDuration = Math.max(.01, Number(duration) || 0);
    const safeMinimum = Math.max(0, Math.min(safeDuration, Number(minimum) || 0));
    const safeMaximum = Math.max(safeMinimum, Math.min(safeDuration, Number(maximum) || safeDuration));
    const minimumSpan = Math.min(.01, safeMaximum - safeMinimum);
    const maximumStart = Math.max(safeMinimum, safeMaximum - minimumSpan);
    const safeStart = Math.max(safeMinimum, Math.min(maximumStart, Number(start) || safeMinimum));
    const minimumEnd = Math.min(safeMaximum, safeStart + minimumSpan);
    const safeEnd = Math.max(minimumEnd, Math.min(safeMaximum, Number(end) || minimumEnd));
    return { start: safeStart, end: safeEnd };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:normalizeVideoTimeRange@628', __javascriptError); throw __javascriptError; }}

function videoCanvasMode(overlay) { try {
    const value = String(overlay?.dataset?.mouseMode || 'SelectSection');
    return value === 'PlacePlayhead' || value === 'AddCutLine' || value === 'FrameRegion'
        ? value
        : 'SelectSection';
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:videoCanvasMode@640', __javascriptError); throw __javascriptError; }}

function setVideoPlayheadVisual(overlay, sourceSeconds) { try {
    const data = videoTimeData(overlay);
    const current = Math.max(data.trimStart, Math.min(data.trimEnd, Number(sourceSeconds) || data.trimStart));
    const visibleSpan = Math.max(.01, data.trimEnd - data.trimStart);
    overlay.style.setProperty('--video-time-playhead', `${(current - data.trimStart) / visibleSpan * 100}%`);
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:setVideoPlayheadVisual@647', __javascriptError); throw __javascriptError; }}

function updateVideoTimeReadout(state) { try {
    const video = mediaElement(state.id);
    const overlay = document.getElementById(state.timeOverlayId);
    if (!(video instanceof HTMLVideoElement) || !overlay) return;
    setVideoPlayheadVisual(overlay, Number(video.currentTime) || 0);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:updateVideoTimeReadout@668', __javascriptError); throw __javascriptError; }}

function resolvedVideoDuration(video) { try {
    const direct = Number(video?.duration);
    if (Number.isFinite(direct) && direct > .01) return direct;
    for (const ranges of [video?.seekable, video?.buffered]) {
        try {
            if (ranges?.length) {
                const end = Number(ranges.end(ranges.length - 1));
                if (Number.isFinite(end) && end > .01) return end;
            }
        } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@684', __caughtJavaScriptError);  }
    }
    const current = Number(video?.currentTime);
    return Number.isFinite(current) && current > .01 && direct === Infinity ? current : 0;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:resolvedVideoDuration@675', __javascriptError); throw __javascriptError; }}

function requestVideoDurationProbe(state, video, overlay) { try {
    if (!(video instanceof HTMLVideoElement) || !overlay || state.durationProbePending) return;
    if (resolvedVideoDuration(video) > .01 || video.readyState < 1) return;
    state.durationProbePending = true;
    state.durationProbeRestoreTime = finiteMediaTime(video.currentTime);
    const finish = () => { try {
        if (!state.durationProbePending) return;
        state.durationProbePending = false;
        if (state.durationProbeTimer) clearTimeout(state.durationProbeTimer);
        state.durationProbeTimer = 0;
        if (state.durationProbeSeekedHandler) video.removeEventListener('seeked', state.durationProbeSeekedHandler);
        state.durationProbeSeekedHandler = null;
        reportResolvedVideoDuration(state, video, overlay);
        try { video.currentTime = finiteMediaTime(state.durationProbeRestoreTime); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@703', __caughtJavaScriptError);  }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:finish@695', __javascriptError); throw __javascriptError; }};
    state.durationProbeSeekedHandler = finish;
    video.addEventListener('seeked', finish, { once: true });
    state.durationProbeTimer = window.setTimeout(finish, 1200);
    try { video.currentTime = Number.MAX_SAFE_INTEGER; }
    catch { finish(); }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:requestVideoDurationProbe@690', __javascriptError); throw __javascriptError; }}

function reconcileResolvedVideoDuration(video, overlay) { try {
    const duration = resolvedVideoDuration(video);
    if (!(duration > .01) || !overlay) return duration;

    const modeled = Math.max(.01, Number(overlay.dataset.duration) || 0);
    const tolerance = Math.max(.02, duration / 10000);
    if (Math.abs(duration - modeled) <= tolerance) return duration;

    const trimStart = Math.max(0, Math.min(modeled, Number(overlay.dataset.trimStart) || 0));
    const trimEnd = Math.max(trimStart, Math.min(modeled, Number(overlay.dataset.trimEnd) || modeled));
    const pointSelection = overlay.dataset.selectionPoint === 'true';
    const selectionStart = Math.max(trimStart, Math.min(trimEnd, Number(overlay.dataset.selectionStart) || trimStart));
    const selectionEnd = Math.max(selectionStart, Math.min(trimEnd, Number(overlay.dataset.selectionEnd) || selectionStart));
    const placeholderDuration = modeled <= .05;
    const trimReachedModeledEnd = Math.abs(trimEnd - modeled) <= .02;
    const selectionWasWholeTrim = !pointSelection
        && Math.abs(selectionStart - trimStart) <= .02
        && Math.abs(selectionEnd - trimEnd) <= .02;

    const nextTrimEnd = placeholderDuration || trimReachedModeledEnd
        ? duration
        : Math.max(trimStart, Math.min(duration, trimEnd));
    let nextStart = Math.max(trimStart, Math.min(nextTrimEnd, selectionStart));
    let nextEnd = pointSelection
        ? nextStart
        : Math.max(nextStart, Math.min(nextTrimEnd, selectionEnd));
    if (!pointSelection && (placeholderDuration || selectionWasWholeTrim)) nextEnd = nextTrimEnd;

    overlay.dataset.duration = String(duration);
    overlay.dataset.trimEnd = String(nextTrimEnd);
    setVideoTimeVisual(overlay, nextStart, nextEnd, pointSelection);
    return duration;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:reconcileResolvedVideoDuration@712', __javascriptError); throw __javascriptError; }}

function reportResolvedVideoDuration(state, video, overlay) { try {
    const modeled = Math.max(.01, Number(overlay?.dataset?.duration) || 0);
    const duration = reconcileResolvedVideoDuration(video, overlay);
    if (!(duration > .01) || state.durationReportPending) return duration;
    const tolerance = Math.max(.02, duration / 10000);
    if (Math.abs(duration - state.lastReportedDuration) <= tolerance
        && Math.abs(duration - modeled) <= tolerance) return duration;
    state.lastReportedDuration = duration;
    state.durationReportPending = true;
    Promise.resolve(state.dotnet?.invokeMethodAsync('VideoSourceDurationResolved', duration))
        .catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:promise-catch@755', __promiseError);  state.lastReportedDuration = 0;  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:Promise.resolve(state.dotnet?.invokeMethodAsync(\'VideoSourceDurationRe@756', __javascriptError); throw __javascriptError; }})
        .finally(() => { try { state.durationReportPending = false;  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:Promise.resolve(state.dotnet?.invokeMethodAsync(\'VideoSourceDurationRe@757', __javascriptError); throw __javascriptError; }});
    return duration;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:reportResolvedVideoDuration@746', __javascriptError); throw __javascriptError; }}

function bindVideoTimeOverlay(state, timeOverlayId) { try {
    releaseVideoTimeOverlayBindings(state);
    state.timeOverlayId = String(timeOverlayId || '');
    if (!state.timeOverlayId) return;

    const overlay = document.getElementById(state.timeOverlayId);
    const video = mediaElement(state.id);
    if (!overlay || !(video instanceof HTMLVideoElement)) return;

    video.controls = false;
    video.playsInline = true;
    try { video.disablePictureInPicture = true; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@772', __caughtJavaScriptError);  }
    const clipTimeAt = clientX => { try {
        const data = videoTimeData(overlay);
        const value = videoTimeAt(overlay, clientX, false);
        return Math.max(data.trimStart, Math.min(data.trimEnd, value));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:clipTimeAt@773', __javascriptError); throw __javascriptError; }};
    const locallyScrub = sourceSeconds => { try {
        const current = setVideoPlayheadVisual(overlay, sourceSeconds);
        cancelRangePlayback(state, video, true);
        try { if (Number.isFinite(video.duration)) video.currentTime = finiteMediaTime(current); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@781', __caughtJavaScriptError);  }
        return current;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:locallyScrub@778', __javascriptError); throw __javascriptError; }};

    const finishGesture = async event => { try {
        const gesture = state.timeOverlayGesture;
        if (!gesture || gesture.pointerId !== event.pointerId) return;
        event.preventDefault();
        event.stopPropagation();
        state.timeOverlayGesture = null;
        try { overlay.releasePointerCapture(event.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@791', __caughtJavaScriptError);  }
        const sourceDuration = reportResolvedVideoDuration(state, video, overlay) || videoTimeData(overlay).duration;
        const current = clipTimeAt(event.clientX);

        if (gesture.mode === 'playhead' || gesture.mode === 'cutline') {
            const sourceSeconds = locallyScrub(current);
            try {
                await state.dotnet?.invokeMethodAsync(
                    gesture.mode === 'cutline' ? 'VideoCutlineCommitted' : 'VideoPlayheadCommitted',
                    sourceSeconds);
            } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@801', __caughtJavaScriptError);  }
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
        try {
            await state.dotnet?.invokeMethodAsync('VideoTimeSelectionCommitted', start, end, pointSelection, sourceDuration);
        } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@841', __caughtJavaScriptError);  }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:finishGesture@785', __javascriptError); throw __javascriptError; }};

    state.timeOverlayPointerDownHandler = event => { try {
        if (event.button !== 0 || overlay.classList.contains('disabled')) return;
        if (event.target?.closest?.('[data-video-time-control],button,input,label,select')) return;
        event.preventDefault();
        event.stopPropagation();
        reportResolvedVideoDuration(state, video, overlay);
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
        try { overlay.setPointerCapture(event.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@871', __caughtJavaScriptError);  }
        if (gestureMode === 'playhead' || gestureMode === 'cutline') locallyScrub(anchor);
        else if (!handle) setVideoTimeVisual(overlay, anchor, anchor, true);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:state.timeOverlayPointerDownHandler@844', __javascriptError); throw __javascriptError; }};

    state.timeOverlayPointerMoveHandler = event => { try {
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
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:state.timeOverlayPointerMoveHandler@876', __javascriptError); throw __javascriptError; }};

    const commitInterruptedGesture = event => { try {
        const gesture = state.timeOverlayGesture;
        if (!gesture || (event?.pointerId != null && gesture.pointerId !== event.pointerId)) return;
        event?.preventDefault?.();
        event?.stopPropagation?.();
        state.timeOverlayGesture = null;
        const sourceDuration = reportResolvedVideoDuration(state, video, overlay) || videoTimeData(overlay).duration;
        const data = videoTimeData(overlay);
        try { overlay.releasePointerCapture(gesture.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@902', __caughtJavaScriptError);  }
        if (gesture.mode === 'playhead' || gesture.mode === 'cutline') {
            const sourceSeconds = Math.max(data.trimStart, Math.min(data.trimEnd, Number(video.currentTime) || gesture.anchor));
            state.dotnet?.invokeMethodAsync(
                gesture.mode === 'cutline' ? 'VideoCutlineCommitted' : 'VideoPlayheadCommitted',
                sourceSeconds).catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:promise-catch@905', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:state.dotnet?.invokeMethodAsync( gesture.mode === \'cutline\' ? \'VideoCu@907', __javascriptError); throw __javascriptError; }});
            return;
        }
        locallyScrub(data.start);
        state.dotnet?.invokeMethodAsync(
            'VideoTimeSelectionCommitted',
            data.start,
            data.end,
            data.pointSelection,
            sourceDuration).catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:promise-catch@911', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:state.dotnet?.invokeMethodAsync( \'VideoTimeSelectionCommitted\', data.s@916', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:commitInterruptedGesture@894', __javascriptError); throw __javascriptError; }};
    state.timeOverlayPointerUpHandler = finishGesture;
    state.timeOverlayPointerCancelHandler = commitInterruptedGesture;
    state.timeOverlayLostCaptureHandler = commitInterruptedGesture;
    state.timeOverlayDoubleClickHandler = event => { try {
        event.preventDefault();
        event.stopPropagation();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:state.timeOverlayDoubleClickHandler@921', __javascriptError); throw __javascriptError; }};
    state.timeOverlayPlayHandler = event => { try {
        event.preventDefault();
        event.stopPropagation();
        const data = videoTimeData(overlay);
        const start = data.start;
        const end = data.pointSelection ? data.trimEnd : data.end;
        void playMediaRange(
            state.id,
            start,
            end,
            Number(overlay.dataset.volume) || 0,
            Number(overlay.dataset.playbackRate) || 1,
            overlay.dataset.muted === 'true',
            overlay.dataset.loop === 'true');
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:state.timeOverlayPlayHandler@925', __javascriptError); throw __javascriptError; }};
    state.timeOverlayPauseHandler = event => { try {
        event.preventDefault();
        event.stopPropagation();
        pauseMedia(state.id);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:state.timeOverlayPauseHandler@940', __javascriptError); throw __javascriptError; }};
    state.timeOverlayMetadataHandler = () => { try {
        const duration = reportResolvedVideoDuration(state, video, overlay);
        if (!(duration > .01)) requestVideoDurationProbe(state, video, overlay);
        syncFrameOverlay(state);
        updateVideoTimeReadout(state);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:state.timeOverlayMetadataHandler@945', __javascriptError); throw __javascriptError; }};
    state.timeOverlayDurationHandler = () => { try {
        const duration = reportResolvedVideoDuration(state, video, overlay);
        if (!(duration > .01)) requestVideoDurationProbe(state, video, overlay);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:state.timeOverlayDurationHandler@951', __javascriptError); throw __javascriptError; }};
    state.timeOverlayUpdateHandler = () => { try {
        reportResolvedVideoDuration(state, video, overlay);
        updateVideoTimeReadout(state);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:state.timeOverlayUpdateHandler@955', __javascriptError); throw __javascriptError; }};

    overlay.addEventListener('pointerdown', state.timeOverlayPointerDownHandler);
    overlay.addEventListener('pointermove', state.timeOverlayPointerMoveHandler);
    overlay.addEventListener('pointerup', state.timeOverlayPointerUpHandler);
    overlay.addEventListener('pointercancel', state.timeOverlayPointerCancelHandler);
    overlay.addEventListener('lostpointercapture', state.timeOverlayLostCaptureHandler);
    overlay.addEventListener('dblclick', state.timeOverlayDoubleClickHandler);
    overlay.querySelector('[data-video-command="play"]')?.addEventListener('click', state.timeOverlayPlayHandler);
    overlay.querySelector('[data-video-command="pause"]')?.addEventListener('click', state.timeOverlayPauseHandler);
    video.addEventListener('loadedmetadata', state.timeOverlayMetadataHandler);
    video.addEventListener('loadeddata', state.timeOverlayMetadataHandler);
    video.addEventListener('durationchange', state.timeOverlayDurationHandler);
    video.addEventListener('timeupdate', state.timeOverlayUpdateHandler);
    video.addEventListener('seeked', state.timeOverlayUpdateHandler);
    requestAnimationFrame(() => { try {
        const initialResolvedDuration = reportResolvedVideoDuration(state, video, overlay);
        if (!(initialResolvedDuration > .01)) requestVideoDurationProbe(state, video, overlay);
        syncFrameOverlay(state);
        const data = videoTimeData(overlay);
        setVideoTimeVisual(overlay, data.start, data.end, data.pointSelection);
        updateVideoTimeReadout(state);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:requestAnimationFrame@973', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:bindVideoTimeOverlay@761', __javascriptError); throw __javascriptError; }}


function releaseSequenceSelectionBindings(state) { try {
    const timeline = document.getElementById(state.sequenceTimelineId || '');
    if (timeline && state.sequencePointerDownHandler) timeline.removeEventListener('pointerdown', state.sequencePointerDownHandler, true);
    if (timeline && state.sequencePointerMoveHandler) timeline.removeEventListener('pointermove', state.sequencePointerMoveHandler, true);
    if (timeline && state.sequencePointerUpHandler) {
        timeline.removeEventListener('pointerup', state.sequencePointerUpHandler, true);
        timeline.removeEventListener('pointercancel', state.sequencePointerUpHandler, true);
        timeline.removeEventListener('lostpointercapture', state.sequencePointerUpHandler, true);
    }
    state.sequenceTimelineId = '';
    state.sequencePointerDownHandler = null;
    state.sequencePointerMoveHandler = null;
    state.sequencePointerUpHandler = null;
    state.sequenceGesture = null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:releaseSequenceSelectionBindings@984', __javascriptError); throw __javascriptError; }}

function bindSequenceSelectionHandles(state, timelineId, timeOverlayId) { try {
    releaseSequenceSelectionBindings(state);
    const timeline = document.getElementById(timelineId);
    const overlay = document.getElementById(timeOverlayId);
    if (!(timeline instanceof HTMLElement) || !(overlay instanceof HTMLElement)) return;
    state.sequenceTimelineId = timelineId;

    const sourceAt = clientX => { try {
        const bounds = timeline.getBoundingClientRect();
        const ratio = Math.max(0, Math.min(1, (Number(clientX) - bounds.left) / Math.max(1, bounds.width)));
        const sequenceDuration = Math.max(.01, Number(timeline.dataset.sequenceDuration) || Number(overlay.dataset.sequenceDuration) || .01);
        const segmentTimelineStart = Math.max(0, Number(timeline.dataset.segmentTimelineStart) || Number(overlay.dataset.segmentTimelineStart) || 0);
        const sourceStart = Math.max(0, Number(timeline.dataset.segmentSourceStart) || Number(overlay.dataset.segmentSourceStart) || 0);
        const sourceEnd = Math.max(sourceStart, Number(timeline.dataset.segmentSourceEnd) || Number(overlay.dataset.trimEnd) || sourceStart);
        const effectiveRate = Math.max(.0001, Number(timeline.dataset.segmentEffectiveRate) || Number(overlay.dataset.playbackRate) || 1);
        const source = sourceStart + (ratio * sequenceDuration - segmentTimelineStart) * effectiveRate;
        return Math.max(sourceStart, Math.min(sourceEnd, source));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:sourceAt@1007', __javascriptError); throw __javascriptError; }};

    state.sequencePointerDownHandler = event => { try {
        const handle = event.target?.closest?.('[data-sequence-selection-handle]');
        if (!handle || event.button !== 0) return;
        event.preventDefault();
        event.stopPropagation();
        event.stopImmediatePropagation?.();
        const data = videoTimeData(overlay);
        state.sequenceGesture = {
            pointerId: event.pointerId,
            mode: handle.dataset.sequenceSelectionHandle,
            start: data.start,
            end: data.end
        };
        try { timeline.setPointerCapture(event.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@1032', __caughtJavaScriptError);  }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:state.sequencePointerDownHandler@1019', __javascriptError); throw __javascriptError; }};
    state.sequencePointerMoveHandler = event => { try {
        const gesture = state.sequenceGesture;
        if (!gesture || gesture.pointerId !== event.pointerId) return;
        event.preventDefault();
        event.stopPropagation();
        const current = sourceAt(event.clientX);
        if (gesture.mode === 'start') setVideoTimeVisual(overlay, Math.min(current, gesture.end - .01), gesture.end, false);
        else setVideoTimeVisual(overlay, gesture.start, Math.max(current, gesture.start + .01), false);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:state.sequencePointerMoveHandler@1034', __javascriptError); throw __javascriptError; }};
    state.sequencePointerUpHandler = event => { try {
        const gesture = state.sequenceGesture;
        if (!gesture || (event.pointerId != null && gesture.pointerId !== event.pointerId)) return;
        event.preventDefault?.();
        event.stopPropagation?.();
        state.sequenceGesture = null;
        try { timeline.releasePointerCapture(gesture.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@1049', __caughtJavaScriptError);  }
        const data = videoTimeData(overlay);
        locallySeekMedia(state.id, data.start);
        state.dotnet?.invokeMethodAsync('VideoTimeSelectionCommitted', data.start, data.end, false, data.duration).catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:promise-catch@1052', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:state.dotnet?.invokeMethodAsync(\'VideoTimeSelectionCommitted\', data.st@1052', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:state.sequencePointerUpHandler@1043', __javascriptError); throw __javascriptError; }};
    timeline.addEventListener('pointerdown', state.sequencePointerDownHandler, true);
    timeline.addEventListener('pointermove', state.sequencePointerMoveHandler, true);
    timeline.addEventListener('pointerup', state.sequencePointerUpHandler, true);
    timeline.addEventListener('pointercancel', state.sequencePointerUpHandler, true);
    timeline.addEventListener('lostpointercapture', state.sequencePointerUpHandler, true);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:bindSequenceSelectionHandles@1000', __javascriptError); throw __javascriptError; }}

function locallySeekMedia(id, seconds) { try {
    const element = mediaElement(id);
    if (!element) return;
    try { element.currentTime = finiteMediaTime(seconds); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@1064', __caughtJavaScriptError);  }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:locallySeekMedia@1061', __javascriptError); throw __javascriptError; }}

export function configureVideoEffects(videoId, canvasId, config) { try {
    const video = document.getElementById(videoId);
    const canvas = document.getElementById(canvasId);
    if (!(video instanceof HTMLVideoElement) || !(canvas instanceof HTMLCanvasElement) || !window.publisherVideoEffects) return false;
    const state = stateFor(videoId);
    const key = `media-studio-${videoId}`;
    state.effectRuntimeKey = key;
    window.publisherVideoEffects.install(key, video, canvas, config || {});
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:configureVideoEffects@1067', __javascriptError); throw __javascriptError; }}

export function refreshMediaStudioOverlay(id) { try {
    const state = studioStates.get(id);
    if (!state) return;
    requestAnimationFrame(() => { try {
        syncFrameOverlay(state);
        const overlay = document.getElementById(state.timeOverlayId);
        if (overlay) {
            const data = videoTimeData(overlay);
            setVideoTimeVisual(overlay, data.start, data.end, data.pointSelection);
        }
        updateVideoTimeReadout(state);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:requestAnimationFrame@1081', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:refreshMediaStudioOverlay@1078', __javascriptError); throw __javascriptError; }}

export function clickElement(id) { try {
    const element = document.getElementById(id);
    if (!element) throw new Error(`Element '${id}' is not available.`);
    if (element instanceof HTMLInputElement && element.type === 'file') element.value = '';
    element.click();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:clickElement@1092', __javascriptError); throw __javascriptError; }}

export function initializeMediaStudio(
    id,
    dotnet,
    sessionId,
    rootId = "",
    frameStageId = "",
    frameOverlayId = "",
    timeOverlayId = "",
    sequenceTimelineId = "",
    sourceInputId = "",
    insertInputId = "",
    projectInputId = "",
    expectedKind = "video") { try {
    const state = stateFor(id);
    const nextSessionId = String(sessionId || '');
    if (state.sessionId && state.sessionId !== nextSessionId) releaseRetainedRecording(state);
    state.sessionId = nextSessionId;
    state.dotnet = dotnet;
    state.discardRecording = false;
    state.rootId = String(rootId || "");
    if (state.keyboardHandler) document.removeEventListener("keydown", state.keyboardHandler, true);
    state.keyboardHandler = event => { try {
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
        state.dotnet?.invokeMethodAsync("MediaStudioShortcutRequested", command).catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:promise-catch@1136', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:state.dotnet?.invokeMethodAsync("MediaStudioShortcutRequested", comman@1136', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:state.keyboardHandler@1120', __javascriptError); throw __javascriptError; }};
    document.addEventListener("keydown", state.keyboardHandler, true);
    bindFrameOverlay(state, frameStageId, frameOverlayId);
    bindVideoTimeOverlay(state, timeOverlayId);
    bindSequenceSelectionHandles(state, sequenceTimelineId, timeOverlayId);
    bindMediaDrop(state, state.rootId, sourceInputId, insertInputId, projectInputId, String(expectedKind || 'video').toLowerCase());
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:initializeMediaStudio@1099', __javascriptError); throw __javascriptError; }}

function waitForMetadata(element) { try {
    if (Number.isFinite(element.duration) && element.duration > 0 && element.readyState >= 1)
        return Promise.resolve();
    return new Promise((resolve, reject) => { try {
        const timer = setTimeout(() => { try { return (failed(new Error('Timed out while reading media metadata.'))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:setTimeout@1149', __javascriptError); throw __javascriptError; } }, 15000);
        const cleanup = () => { try {
            clearTimeout(timer);
            element.removeEventListener('loadedmetadata', loaded);
            element.removeEventListener('error', failed);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:cleanup@1150', __javascriptError); throw __javascriptError; }};
        const loaded = () => { try { cleanup(); resolve();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:loaded@1155', __javascriptError); throw __javascriptError; }};
        const failed = error => { try {
            cleanup();
            reject(error instanceof Error ? error : new Error('The browser could not decode this media format.'));
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:failed@1156', __javascriptError); throw __javascriptError; }};
        element.addEventListener('loadedmetadata', loaded, { once: true });
        element.addEventListener('error', failed, { once: true });
        element.load();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:ArrowFunction@1148', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:waitForMetadata@1145', __javascriptError); throw __javascriptError; }}

async function waveformFromSource(dataUrl, sampleCount = 96) { try {
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
            return values.map(value => { try { return (Math.max(.04, value / max)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:values.map@1188', __javascriptError); throw __javascriptError; } });
        } finally {
            await context.close();
        }
    } catch {
        return [];
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:waveformFromSource@1166', __javascriptError); throw __javascriptError; }}

async function posterFromVideo(video, maximumPixels = 0) { try {
    try {
        const previous = Number(video.currentTime);
        const duration = Number(video.duration);
        const finiteDuration = Number.isFinite(duration) && duration > 0 ? duration : 0;
        const target = finiteDuration > .05
            ? Math.min(Math.max(.05, finiteDuration * .12), Math.max(.05, finiteDuration - .05))
            : 0;
        if (target > 0 && Number.isFinite(target)) {
            await new Promise(resolve => { try {
                if (Math.abs((Number(video.currentTime) || 0) - target) < .005) { resolve(); return; }
                const timer = setTimeout(done, 4000);
                function done() { try {
                    clearTimeout(timer);
                    video.removeEventListener('seeked', done);
                    resolve();
                 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:done@1204', __javascriptError); throw __javascriptError; }}
                video.addEventListener('seeked', done, { once: true });
                video.currentTime = target;
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:ArrowFunction@1201', __javascriptError); resolve(); }});
        }
        const canvas = document.createElement('canvas');
        const sourceWidth = Math.max(1, video.videoWidth || 640);
        const sourceHeight = Math.max(1, video.videoHeight || 360);
        const sourcePixels = sourceWidth * sourceHeight;
        const pixelLimit = Math.max(0, Math.trunc(Number(maximumPixels) || 0));
        const scale = pixelLimit > 0 && sourcePixels > pixelLimit
            ? Math.sqrt(pixelLimit / sourcePixels)
            : 1;
        canvas.width = Math.max(1, Math.round(sourceWidth * scale));
        canvas.height = Math.max(1, Math.round(sourceHeight * scale));
        canvas.getContext('2d', { willReadFrequently: false }).drawImage(video, 0, 0, canvas.width, canvas.height);
        const poster = canvas.toDataURL('image/jpeg', .82);
        try { video.currentTime = Number.isFinite(previous) ? Math.max(0, previous) : 0; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:posterFromVideo:restore', __caughtJavaScriptError); }
        return poster;
    } catch {
        return '';
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:posterFromVideo@1197', __javascriptError); throw __javascriptError; }}

async function inspectElement(element, dataUrl, kind, metadataOptions = null) { try {
    const normalizedDataUrl = normalizeMediaDataUrl(dataUrl, kind === 'video' ? 'video/webm' : 'audio/webm');
    element.src = normalizedDataUrl;
    element.load();
    await waitForMetadata(element);
    const durationSeconds = Number.isFinite(element.duration) ? element.duration : 0;
    const waveformSamples = kind === 'audio' ? await waveformFromSource(normalizedDataUrl) : [];
    const posterDataUrl = kind === 'video' && element instanceof HTMLVideoElement
        ? await posterFromVideo(element, metadataOptions?.metadataPosterMaximumPixels)
        : '';
    try {
        if (Number.isFinite(Number(element.duration)) && Number(element.duration) > 0) element.currentTime = 0;
    } catch (__caughtJavaScriptError) {
        publisherStudioDiagnostics.report('js/mediaStudioInterop.js:inspectElement:reset-currentTime', __caughtJavaScriptError);
    }
    return { durationSeconds, waveformSamples, posterDataUrl };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:inspectElement@1224', __javascriptError); throw __javascriptError; }}

export async function inspectMediaSource(id, dataUrl, kind) { try {
    const element = mediaElement(id);
    if (!element) throw new Error('Media preview is not available.');
    pauseMedia(id);
    return inspectElement(element, dataUrl, kind);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:inspectMediaSource@1236', __javascriptError); throw __javascriptError; }}

export async function inspectMediaDataUrl(dataUrl, kind) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:inspectMediaDataUrl@1243', __javascriptError); throw __javascriptError; }}

export async function inspectMediaFileInput(inputId, kind) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:inspectMediaFileInput@1256', __javascriptError); throw __javascriptError; }}

function normalizedCodecPreferenceOrder(value) { try {
    const requested = Array.isArray(value) ? value : [];
    const normalized = requested
        .map(item => String(item || '').trim().toLowerCase())
        .filter(item => item === 'vp9' || item === 'vp8')
        .filter((item, index, values) => values.indexOf(item) === index);
    for (const fallback of ['vp9', 'vp8']) if (!normalized.includes(fallback)) normalized.push(fallback);
    return normalized;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:normalizedCodecPreferenceOrder', __javascriptError); return ['vp9', 'vp8']; }}

function recordingMimeCandidates(codecPreference, codecPreferenceOrder = null) { try {
    const preference = String(codecPreference || 'auto').trim().toLowerCase();
    const families = {
        vp9: ['video/webm;codecs=vp9,opus', 'video/webm;codecs=vp9'],
        vp8: ['video/webm;codecs=vp8,opus', 'video/webm;codecs=vp8']
    };
    const order = preference === 'vp8' || preference === 'vp9'
        ? [preference, ...normalizedCodecPreferenceOrder(codecPreferenceOrder).filter(item => item !== preference)]
        : normalizedCodecPreferenceOrder(codecPreferenceOrder);
    return [...order.flatMap(item => families[item] || []), 'video/webm'];
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:recordingMimeCandidates', __javascriptError); return ['video/webm']; }}

function preferredMime(kind, codecPreference = 'auto', codecPreferenceOrder = null) { try {
    const choices = kind === 'video'
        ? recordingMimeCandidates(codecPreference, codecPreferenceOrder)
        : ['audio/webm;codecs=opus', 'audio/webm', 'audio/ogg;codecs=opus'];
    const probe = document.createElement(kind === 'video' ? 'video' : 'audio');
    return choices.find(value => { try { return (MediaRecorder.isTypeSupported(value) && probe.canPlayType(value) !== ''); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:preferredMime:playback-probe', __javascriptError); throw __javascriptError; } })
        || choices.find(value => { try { return (MediaRecorder.isTypeSupported(value)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:preferredMime:recorder-probe', __javascriptError); throw __javascriptError; } })
        || '';
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:preferredMime', __javascriptError); throw __javascriptError; }}

function normalizedAdaptiveRecordingPolicy(value) { try {
    const number = (candidate, fallback = 0) => {
        const parsed = Number(candidate);
        return Number.isFinite(parsed) ? parsed : fallback;
    };
    const boolean = (candidate, fallback = false) => candidate === undefined || candidate === null ? fallback : Boolean(candidate);
    return {
        enabled: boolean(value?.enabled, false),
        adaptVideo: boolean(value?.adaptVideo, true),
        adaptAudio: boolean(value?.adaptAudio, true),
        useBrowserCapabilityProbe: boolean(value?.useBrowserCapabilityProbe, true),
        profile: String(value?.profile || 'quality').trim().toLowerCase(),
        preserveNativeResolution: boolean(value?.preserveNativeResolution, true),
        allowFrameRateReduction: boolean(value?.allowFrameRateReduction, true),
        allowResolutionReduction: boolean(value?.allowResolutionReduction, false),
        preferPowerEfficientCodec: boolean(value?.preferPowerEfficientCodec, true),
        preferSmoothEncoding: boolean(value?.preferSmoothEncoding, true),
        codecPreferenceOrder: normalizedCodecPreferenceOrder(value?.codecPreferenceOrder),
        screenBitsPerPixel: number(value?.screenBitsPerPixel),
        cameraBitsPerPixel: number(value?.cameraBitsPerPixel),
        mixedBitsPerPixel: number(value?.mixedBitsPerPixel),
        efficiencyMultiplier: number(value?.efficiencyMultiplier, 1),
        balancedMultiplier: number(value?.balancedMultiplier, 1),
        qualityMultiplier: number(value?.qualityMultiplier, 1),
        vp9BitrateFactor: number(value?.vp9BitrateFactor, 1),
        vp8BitrateFactor: number(value?.vp8BitrateFactor, 1),
        minimumVideoBitrateKbps: Math.max(0, Math.trunc(number(value?.minimumVideoBitrateKbps))),
        maximumVideoBitrateKbps: Math.max(0, Math.trunc(number(value?.maximumVideoBitrateKbps))),
        minimumAudioBitrateKbps: Math.max(0, Math.trunc(number(value?.minimumAudioBitrateKbps))),
        audioBitratePerChannelKbps: Math.max(0, Math.trunc(number(value?.audioBitratePerChannelKbps))),
        maximumAudioBitrateKbps: Math.max(0, Math.trunc(number(value?.maximumAudioBitrateKbps))),
        defaultAudioChannels: Math.max(1, Math.trunc(number(value?.defaultAudioChannels, 2))),
        maximumAutomaticFrameRate: Math.max(0, number(value?.maximumAutomaticFrameRate)),
        minimumFrameRate: Math.max(1, number(value?.minimumFrameRate, 1)),
        frameRateFallbackRatio: Math.min(.99, Math.max(.1, number(value?.frameRateFallbackRatio, .75))),
        resolutionFallbackRatio: Math.min(.99, Math.max(.1, number(value?.resolutionFallbackRatio, .85))),
        maximumAdaptationAttempts: Math.max(0, Math.trunc(number(value?.maximumAdaptationAttempts, 0))),
        metadataPosterMaximumPixels: Math.max(0, Math.trunc(number(value?.metadataPosterMaximumPixels, 0))),
        metadataAnalysisDelayMilliseconds: Math.min(10000, Math.max(0, Math.trunc(number(value?.metadataAnalysisDelayMilliseconds, 0))))
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:normalizedAdaptiveRecordingPolicy', __javascriptError); throw __javascriptError; }}

function normalizedRecordingOptions(options) { try {
    const positiveInteger = value => { try {
        const number = Math.trunc(Number(value));
        return Number.isFinite(number) && number > 0 ? number : 0;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:normalizedRecordingOptions:positiveInteger', __javascriptError); return 0; }};
    const positiveNumber = value => { try {
        const number = Number(value);
        return Number.isFinite(number) && number > 0 ? number : 0;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:normalizedRecordingOptions:positiveNumber', __javascriptError); return 0; }};
    return {
        width: positiveInteger(options?.width),
        height: positiveInteger(options?.height),
        frameRate: positiveNumber(options?.frameRate),
        maximumFrameRate: positiveNumber(options?.maximumFrameRate),
        bitrateFrameRate: positiveNumber(options?.bitrateFrameRate),
        videoBitsPerSecond: positiveInteger(options?.videoBitsPerSecond),
        audioBitsPerSecond: positiveInteger(options?.audioBitsPerSecond),
        recorderChunkMilliseconds: Math.min(5000, Math.max(100, positiveInteger(options?.recorderChunkMilliseconds) || 500)),
        codecPreference: String(options?.codecPreference || 'auto').trim().toLowerCase(),
        adaptive: normalizedAdaptiveRecordingPolicy(options?.adaptive)
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:normalizedRecordingOptions', __javascriptError); throw __javascriptError; }}

function adaptiveProfileMultiplier(policy) { try {
    if (policy.profile === 'efficiency') return policy.efficiencyMultiplier > 0 ? policy.efficiencyMultiplier : 1;
    if (policy.profile === 'balanced') return policy.balancedMultiplier > 0 ? policy.balancedMultiplier : 1;
    return policy.qualityMultiplier > 0 ? policy.qualityMultiplier : 1;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:adaptiveProfileMultiplier', __javascriptError); return 1; }}

function adaptiveCodecFactor(mimeType, policy) { try {
    const mime = String(mimeType || '').toLowerCase();
    if (mime.includes('vp9')) return policy.vp9BitrateFactor > 0 ? policy.vp9BitrateFactor : 1;
    if (mime.includes('vp8')) return policy.vp8BitrateFactor > 0 ? policy.vp8BitrateFactor : 1;
    return 1;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:adaptiveCodecFactor', __javascriptError); return 1; }}

function mediaCapabilitiesVideoContentType(mimeType) { try {
    const mime = String(mimeType || '').toLowerCase();
    if (mime.includes('vp9')) return 'video/webm;codecs=vp9';
    if (mime.includes('vp8')) return 'video/webm;codecs=vp8';
    return baseMimeType(mime, 'video/webm');
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:mediaCapabilitiesVideoContentType', __javascriptError); return 'video/webm'; }}

function mediaCapabilitiesAudioConfiguration(stream, audioBitsPerSecond) { try {
    const track = stream?.getAudioTracks?.()[0];
    if (!track || !(audioBitsPerSecond > 0)) return null;
    const settings = track.getSettings?.() || {};
    return {
        contentType: 'audio/webm;codecs=opus',
        channels: Math.max(1, Number(settings.channelCount) || 2),
        bitrate: Math.max(1, Math.round(audioBitsPerSecond)),
        samplerate: Math.max(8000, Number(settings.sampleRate) || 48000)
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:mediaCapabilitiesAudioConfiguration', __javascriptError); return null; }}

function adaptiveVideoBitsPerSecond(settings, source, mimeType, options) { try {
    const policy = options.adaptive;
    if (!policy.enabled || !policy.adaptVideo) return options.videoBitsPerSecond;
    const width = Math.max(1, Number(settings?.width) || options.width || 0);
    const height = Math.max(1, Number(settings?.height) || options.height || 0);
    let frameRate = Math.max(1, Number(options.bitrateFrameRate) || Number(settings?.frameRate) || options.maximumFrameRate || 0);
    if (!width || !height || !frameRate) return 0;
    const ceiling = options.maximumFrameRate > 0 ? options.maximumFrameRate : policy.maximumAutomaticFrameRate;
    if (ceiling > 0) frameRate = Math.min(frameRate, ceiling);
    const contentBpp = source === 'screen'
        ? policy.screenBitsPerPixel
        : source === 'camera'
            ? policy.cameraBitsPerPixel
            : policy.mixedBitsPerPixel;
    if (!(contentBpp > 0)) return 0;
    let bits = width * height * frameRate * contentBpp * adaptiveProfileMultiplier(policy) * adaptiveCodecFactor(mimeType, policy);
    const minimum = Math.max(0, policy.minimumVideoBitrateKbps) * 1000;
    const maximum = Math.max(minimum, policy.maximumVideoBitrateKbps * 1000);
    if (maximum > 0) bits = Math.min(bits, maximum);
    if (minimum > 0) bits = Math.max(bits, minimum);
    return Math.max(1, Math.round(bits));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:adaptiveVideoBitsPerSecond', __javascriptError); return options.videoBitsPerSecond || 0; }}

function adaptiveAudioBitsPerSecond(stream, options) { try {
    const policy = options.adaptive;
    if (!policy.enabled || !policy.adaptAudio) return options.audioBitsPerSecond;
    const settings = stream?.getAudioTracks?.()[0]?.getSettings?.() || {};
    const channels = Math.max(1, Number(settings.channelCount) || policy.defaultAudioChannels || 1);
    let kbps = Math.max(1, policy.audioBitratePerChannelKbps) * channels * adaptiveProfileMultiplier(policy);
    if (policy.minimumAudioBitrateKbps > 0) kbps = Math.max(kbps, policy.minimumAudioBitrateKbps);
    if (policy.maximumAudioBitrateKbps > 0) kbps = Math.min(kbps, policy.maximumAudioBitrateKbps);
    return Math.max(1, Math.round(kbps * 1000));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:adaptiveAudioBitsPerSecond', __javascriptError); return options.audioBitsPerSecond || 0; }}

let mediaCapabilitiesRecordProbeSupported = null;

function recordEncodingProbeUnsupported(error) { try {
    if (!(error instanceof Error) && !error) return false;
    const name = String(error?.name || '');
    const message = String(error?.message || error || '').toLowerCase();
    return name === 'TypeError' && (message.includes('mediaencodingtype') || message.includes('valid enum') || message.includes('record'));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:recordEncodingProbeUnsupported', __javascriptError); return false; }}

async function tryRecordEncodingInfo(capabilityRequest) { try {
    if (mediaCapabilitiesRecordProbeSupported === false) return null;
    try {
        const capability = await navigator.mediaCapabilities.encodingInfo(capabilityRequest);
        mediaCapabilitiesRecordProbeSupported = true;
        return capability;
    } catch (error) {
        if (recordEncodingProbeUnsupported(error)) {
            // Some Chromium/Edge builds expose MediaCapabilities.encodingInfo but reject the
            // standards-defined MediaRecorder `record` encoding type. Treat that as an absent
            // optional probe and fall back to MediaRecorder.isTypeSupported instead of flooding
            // diagnostics once for every codec candidate.
            mediaCapabilitiesRecordProbeSupported = false;
            return null;
        }
        publisherStudioDiagnostics.report('js/mediaStudioInterop.js:probeAdaptiveRecordingProfile:encodingInfo', error);
        return null;
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:tryRecordEncodingInfo', __javascriptError); return null; }}

async function probeAdaptiveRecordingProfile(stream, source, options) { try {
    const videoTrack = stream?.getVideoTracks?.()[0];
    if (!videoTrack) return { mimeType: preferredMime('audio', options.codecPreference, options.adaptive.codecPreferenceOrder), videoBitsPerSecond: 0, audioBitsPerSecond: adaptiveAudioBitsPerSecond(stream, options) };
    try { videoTrack.contentHint = source === 'screen' ? 'detail' : 'motion'; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:probeAdaptiveRecordingProfile:contentHint', __caughtJavaScriptError); }

    const canProbe = options.adaptive.enabled && options.adaptive.adaptVideo && options.adaptive.useBrowserCapabilityProbe && navigator.mediaCapabilities && typeof navigator.mediaCapabilities.encodingInfo === 'function';
    const choose = async () => {
        const settings = videoTrack.getSettings?.() || {};
        const width = Math.max(1, Number(settings.width) || options.width || 1);
        const height = Math.max(1, Number(settings.height) || options.height || 1);
        const frameRate = Math.max(1, Number(settings.frameRate) || options.maximumFrameRate || 1);
        const audioBitsPerSecond = adaptiveAudioBitsPerSecond(stream, options);
        const candidates = recordingMimeCandidates(options.codecPreference, options.adaptive.codecPreferenceOrder).filter(type => {
            try { return !MediaRecorder.isTypeSupported || MediaRecorder.isTypeSupported(type); }
            catch { return false; }
        });
        let best = null;
        for (const mimeType of candidates) {
            const videoBitsPerSecond = adaptiveVideoBitsPerSecond(settings, source, mimeType, options);
            let capability = null;
            if (canProbe && videoBitsPerSecond > 0) {
                try {
                    const capabilityRequest = {
                        type: 'record',
                        video: {
                            contentType: mediaCapabilitiesVideoContentType(mimeType),
                            width,
                            height,
                            bitrate: videoBitsPerSecond,
                            framerate: frameRate
                        }
                    };
                    const audioConfiguration = mediaCapabilitiesAudioConfiguration(stream, audioBitsPerSecond);
                    if (audioConfiguration) capabilityRequest.audio = audioConfiguration;
                    capability = await tryRecordEncodingInfo(capabilityRequest);
                } catch (__capabilityError) {
                    publisherStudioDiagnostics.report('js/mediaStudioInterop.js:probeAdaptiveRecordingProfile:encodingInfo-wrapper', __capabilityError);
                }
            }
            const supported = capability ? capability.supported !== false : true;
            if (!supported) continue;
            const family = mimeType.includes('vp9') ? 'vp9' : mimeType.includes('vp8') ? 'vp8' : '';
            const preferenceIndex = options.adaptive.codecPreferenceOrder.indexOf(family);
            const preferenceScore = preferenceIndex < 0 ? 0 : Math.max(0, options.adaptive.codecPreferenceOrder.length - preferenceIndex);
            const score = (capability?.smooth ? 100 : 0)
                + (capability?.powerEfficient && options.adaptive.preferPowerEfficientCodec ? 20 : 0)
                + preferenceScore;
            const candidate = { mimeType, videoBitsPerSecond, audioBitsPerSecond, smooth: capability?.smooth !== false, score };
            if (!best || candidate.score > best.score) best = candidate;
        }
        return best || {
            mimeType: preferredMime('video', options.codecPreference, options.adaptive.codecPreferenceOrder),
            videoBitsPerSecond: adaptiveVideoBitsPerSecond(settings, source, preferredMime('video', options.codecPreference, options.adaptive.codecPreferenceOrder), options),
            audioBitsPerSecond,
            smooth: true,
            score: 0
        };
    };

    let selected = await choose();
    let attempt = 0;
    while (options.adaptive.enabled
        && options.adaptive.preferSmoothEncoding
        && selected
        && selected.smooth === false
        && attempt < options.adaptive.maximumAdaptationAttempts) {
        attempt++;
        const settings = videoTrack.getSettings?.() || {};
        let changed = false;
        if (options.adaptive.allowFrameRateReduction && typeof videoTrack.applyConstraints === 'function') {
            const currentFps = Number(settings.frameRate) || options.maximumFrameRate || 0;
            const nextFps = Math.max(options.adaptive.minimumFrameRate, currentFps * options.adaptive.frameRateFallbackRatio);
            if (currentFps > options.adaptive.minimumFrameRate && nextFps < currentFps) {
                try {
                    await videoTrack.applyConstraints({ frameRate: { ideal: nextFps, max: nextFps } });
                    changed = true;
                } catch (__constraintError) {
                    publisherStudioDiagnostics.report('js/mediaStudioInterop.js:probeAdaptiveRecordingProfile:fps-fallback', __constraintError);
                }
            }
        }
        if (!changed && options.adaptive.allowResolutionReduction && typeof videoTrack.applyConstraints === 'function') {
            const width = Number(settings.width) || 0;
            const height = Number(settings.height) || 0;
            if (width > 1 && height > 1) {
                try {
                    await videoTrack.applyConstraints({
                        width: { ideal: Math.max(1, Math.round(width * options.adaptive.resolutionFallbackRatio)) },
                        height: { ideal: Math.max(1, Math.round(height * options.adaptive.resolutionFallbackRatio)) }
                    });
                    changed = true;
                } catch (__constraintError) {
                    publisherStudioDiagnostics.report('js/mediaStudioInterop.js:probeAdaptiveRecordingProfile:resolution-fallback', __constraintError);
                }
            }
        }
        if (!changed) break;
        selected = await choose();
    }
    return selected;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:probeAdaptiveRecordingProfile', __javascriptError); throw __javascriptError; }}

function recordingVideoConstraints(options) { try {
    const normalized = normalizedRecordingOptions(options);
    const constraints = {};
    if (normalized.width > 0) constraints.width = { ideal: normalized.width };
    if (normalized.height > 0) constraints.height = { ideal: normalized.height };
    const requestedFrameRate = normalized.frameRate > 0 ? normalized.frameRate : normalized.maximumFrameRate;
    if (requestedFrameRate > 0) constraints.frameRate = { ideal: requestedFrameRate };
    return Object.keys(constraints).length ? constraints : true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:recordingVideoConstraints', __javascriptError); throw __javascriptError; }}

async function applyRecordingVideoConstraints(stream, options) { try {
    const track = stream?.getVideoTracks?.()[0];
    if (!track || typeof track.applyConstraints !== 'function') return;
    const constraints = recordingVideoConstraints(options);
    if (constraints === true) return;
    try { await track.applyConstraints(constraints); }
    catch (error) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:applyRecordingVideoConstraints:browser-kept-source-settings', error); }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:applyRecordingVideoConstraints', __javascriptError); throw __javascriptError; }}

async function streamFor(kind, source, options) { try {
    const videoConstraints = recordingVideoConstraints(options);
    if (source === 'screen') {
        const stream = await navigator.mediaDevices.getDisplayMedia({ video: videoConstraints, audio: true });
        await applyRecordingVideoConstraints(stream, options);
        if (kind === 'video' && stream.getAudioTracks().length === 0) {
            try {
                const microphone = await navigator.mediaDevices.getUserMedia({ audio: true });
                for (const track of microphone.getAudioTracks()) stream.addTrack(track);
            } catch (__caughtJavaScriptError) {
                if (__caughtJavaScriptError?.name !== 'NotAllowedError' && __caughtJavaScriptError?.name !== 'AbortError')
                    publisherStudioDiagnostics.report('js/mediaStudioInterop.js:streamFor:screen-microphone-fallback', __caughtJavaScriptError);
            }
        }
        return stream;
    }
    if (source === 'camera') {
        const stream = await navigator.mediaDevices.getUserMedia({ video: videoConstraints, audio: true });
        await applyRecordingVideoConstraints(stream, options);
        return stream;
    }
    return navigator.mediaDevices.getUserMedia({ audio: true });
 } catch (__javascriptError) {
    if (__javascriptError?.name !== 'NotAllowedError' && __javascriptError?.name !== 'AbortError')
        publisherStudioDiagnostics.report('js/mediaStudioInterop.js:streamFor', __javascriptError);
    throw __javascriptError;
 }}

async function enrichRetainedRecordingMetadata(state, retainedBlob, retainedUrl, retainedInfo, kind) { try {
    const preview = mediaElement(state.id);
    let info = { durationSeconds: retainedInfo.durationSeconds || .01, waveformSamples: [], posterDataUrl: '' };
    let metadataWarning = '';
    try {
        if (!preview) throw new Error('Media preview is not available.');
        const metadataOptions = state.recordingOptions?.adaptive || null;
        const analysisDelay = Math.max(0, Number(metadataOptions?.metadataAnalysisDelayMilliseconds) || 0);
        if (analysisDelay > 0)
            await new Promise(resolve => setTimeout(resolve, analysisDelay));
        if (state.retainedRecordingBlob !== retainedBlob || state.retainedRecordingUrl !== retainedUrl) return;
        preview.muted = false;
        info = await inspectElement(preview, retainedUrl, kind, metadataOptions);
    } catch (error) {
        metadataWarning = error?.message || String(error);
        if (preview && state.retainedRecordingUrl === retainedUrl) {
            preview.src = retainedUrl;
            preview.load();
        }
    }

    if (state.retainedRecordingBlob !== retainedBlob || state.retainedRecordingUrl !== retainedUrl) return;
    const enrichedInfo = {
        ...retainedInfo,
        durationSeconds: Math.max(.01, Number(info.durationSeconds) || Number(retainedInfo.durationSeconds) || .01),
        waveformSamples: info.waveformSamples || [],
        posterDataUrl: info.posterDataUrl || '',
        committedToSequence: Boolean(state.retainedRecordingCommitted || retainedInfo.committedToSequence),
        metadataWarning
    };
    state.retainedRecordingInfo = enrichedInfo;
    try {
        await state.dotnet?.invokeMethodAsync('MediaRecordingMetadataReady', enrichedInfo);
    } catch (callbackError) {
        publisherStudioDiagnostics.report('js/mediaStudioInterop.js:enrichRetainedRecordingMetadata:dotnet-callback', callbackError);
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:enrichRetainedRecordingMetadata', __javascriptError); throw __javascriptError; }}

async function retainRecording(state, blob, kind) { try {
    const fallbackMimeType = kind === 'video' ? 'video/webm' : 'audio/webm';
    const mimeType = baseMimeType(blob.type, fallbackMimeType);
    const retainedBlob = blob.type === mimeType ? blob : blob.slice(0, blob.size, mimeType);
    releaseRetainedRecording(state);
    state.retainedRecordingBlob = retainedBlob;
    state.retainedRecordingUrl = URL.createObjectURL(retainedBlob);
    state.retainedRecordingKind = kind;
    state.retainedRecordingMimeType = mimeType;
    state.retainedRecordingFileName = recordingFileName(kind, mimeType);
    state.retainedRecordingCommitted = false;

    const stopAt = state.recordingStopRequestedAtMs > 0 ? state.recordingStopRequestedAtMs : performance.now();
    const startedAt = state.recordingStartedAtMs > 0 ? state.recordingStartedAtMs : stopAt - 10;
    const estimatedDurationSeconds = Math.max(.01, (stopAt - startedAt) / 1000);
    const retainedInfo = {
        objectUrl: state.retainedRecordingUrl,
        mimeType,
        fileName: state.retainedRecordingFileName,
        sizeBytes: retainedBlob.size,
        durationSeconds: estimatedDurationSeconds,
        waveformSamples: [],
        posterDataUrl: '',
        committedToSequence: false,
        metadataWarning: 'Browser metadata analysis is continuing in the background.'
    };
    state.retainedRecordingInfo = retainedInfo;
    state.recordingFinalizationError = '';
    state.recordingFinalizing = false;
    state.recordingStopRequested = false;

    const preview = mediaElement(state.id);
    if (preview) {
        preview.src = state.retainedRecordingUrl;
        preview.load();
    }

    // A complete Blob is already safe to download/embed. Do not hold Save/Insert hostage to
    // duration/poster/waveform analysis: expose the retained file first, enrich metadata later.
    Promise.resolve(state.dotnet?.invokeMethodAsync('MediaRecordingReady', retainedInfo))
        .catch(callbackError => publisherStudioDiagnostics.report('js/mediaStudioInterop.js:retainRecording:dotnet-callback', callbackError));

    void enrichRetainedRecordingMetadata(state, retainedBlob, state.retainedRecordingUrl, retainedInfo, kind)
        .catch(error => publisherStudioDiagnostics.report('js/mediaStudioInterop.js:retainRecording:metadata-enrichment', error));
    return retainedInfo;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:retainRecording', __javascriptError); throw __javascriptError; }}

export function getMediaRecordingState(id, dotnet) { try {
    const state = stateFor(id);
    if (dotnet) state.dotnet = dotnet;
    return {
        isRecording: Boolean(state.recorder && state.recorder.state !== 'inactive'),
        isFinalizing: Boolean(state.recordingFinalizing),
        retainedRecording: state.retainedRecordingInfo,
        finalizationError: state.recordingFinalizationError || ''
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:getMediaRecordingState', __javascriptError); throw __javascriptError; }}

export async function embedRetainedRecording(id) { try {
    const state = stateFor(id);
    const blob = state.retainedRecordingBlob;
    if (!(blob instanceof Blob) || !blob.size)
        throw new Error('No completed recording is available to embed.');

    const mimeType = state.retainedRecordingMimeType || baseMimeType(blob.type, state.retainedRecordingKind === 'audio' ? 'audio/webm' : 'video/webm');
    const embeddedBlob = await webmBlobWithDuration(blob, state.retainedRecordingInfo?.durationSeconds, mimeType);
    const transferId = crypto.randomUUID();
    const chunkCount = Math.max(1, Math.ceil(embeddedBlob.size / RECORDING_TRANSFER_CHUNK_SIZE));
    const accepted = await state.dotnet?.invokeMethodAsync('BeginMediaRecordingTransfer', transferId, mimeType, embeddedBlob.size, chunkCount);
    if (!accepted) throw new Error('The publication could not begin the recording transfer.');

    let transferred = 0;
    for (let index = 0; index < chunkCount; index++) {
        const start = index * RECORDING_TRANSFER_CHUNK_SIZE;
        const end = Math.min(embeddedBlob.size, start + RECORDING_TRANSFER_CHUNK_SIZE);
        const buffer = await embeddedBlob.slice(start, end).arrayBuffer();
        const chunk = arrayBufferToBase64(buffer);
        const ok = await state.dotnet.invokeMethodAsync('AppendMediaRecordingChunk', transferId, index, chunk);
        if (!ok) throw new Error('The recording transfer was interrupted.');
        transferred = end;
        if (index === chunkCount - 1 || index % 32 === 0)
            await state.dotnet.invokeMethodAsync('MediaRecordingTransferProgress', transferred, embeddedBlob.size);
    }
    await state.dotnet.invokeMethodAsync('CompleteMediaRecordingTransfer', transferId);
    state.retainedRecordingCommitted = true;
    if (state.retainedRecordingInfo)
        state.retainedRecordingInfo = { ...state.retainedRecordingInfo, committedToSequence: true };
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:embedRetainedRecording@1338', __javascriptError); throw __javascriptError; }}

function renderedVideoFileName(sourceName, mimeType) { try {
    const normalizedMimeType = baseMimeType(mimeType, '').toLowerCase();
    const sourceExtensionMatch = String(sourceName || '').match(/(\.[a-z0-9]{1,8})$/i);
    const extension = normalizedMimeType.includes('mp4') ? '.mp4'
        : normalizedMimeType.includes('ogg') ? '.ogv'
        : normalizedMimeType.includes('quicktime') ? '.mov'
        : normalizedMimeType.includes('webm') ? '.webm'
        : sourceExtensionMatch?.[1]?.toLowerCase() || '.webm';
    const cleaned = String(sourceName || 'video')
        .replace(/\.[a-z0-9]{1,8}$/i, '')
        .replace(/[\\/:*?"<>|]+/g, '-')
        .replace(/\s+/g, ' ')
        .trim() || 'video';
    return `${cleaned}-rendered${extension}`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:renderedVideoFileName', __javascriptError); return 'video-rendered.webm'; }}

function downloadBlob(blob, fileName) { try {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = String(fileName || 'rendered-video.webm');
    anchor.style.display = 'none';
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    setTimeout(() => { try { URL.revokeObjectURL(url); }
    catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:downloadBlob:revoke', __javascriptError); } }, 0);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:downloadBlob', __javascriptError); throw __javascriptError; }}

function stopRenderedVideoExportJob(job) { try {
    if (!job) return;
    job.cancelled = true;
    try { if (job.frame) cancelAnimationFrame(job.frame); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:render-export:cancel-frame', __caughtJavaScriptError); }
    job.frame = 0;
    try { job.resolvePlayback?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:render-export:resolve-playback', __caughtJavaScriptError); }
    try { job.video?.pause?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:render-export:pause', __caughtJavaScriptError); }
    try { if (job.recorder && job.recorder.state !== 'inactive') job.recorder.stop(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:render-export:recorder-stop', __caughtJavaScriptError); }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:stopRenderedVideoExportJob', __javascriptError); }}

function videoEffectConfigurationNeedsCanvas(effectConfig) { try {
    const layers = Array.isArray(effectConfig?.layers) ? effectConfig.layers.filter(layer => layer && layer.visible !== false) : [];
    if (layers.length === 0) return false;
    if (layers.length !== 1) return true;
    const layer = layers[0];
    const kind = String(layer.kind || '').trim().toLowerCase();
    const blendMode = String(layer.blendMode || 'normal').trim().toLowerCase();
    const filters = Array.isArray(layer.filters) ? layer.filters.filter(filter => filter && filter.enabled !== false) : [];
    const points = Array.isArray(layer.region?.points) ? layer.region.points : [];
    const morphPoints = Array.isArray(layer.morphRegion?.points) ? layer.morphRegion.points : [];
    return (kind && kind !== 'basevideo' && kind !== 'base')
        || Number(layer.opacity ?? 1) < .999
        || (blendMode !== 'normal' && blendMode !== 'source-over')
        || layer.hasTemporalRange === true
        || layer.morphEnabled === true
        || layer.animateMorph === true
        || points.length >= 3
        || morphPoints.length >= 3
        || filters.length > 0;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:videoEffectConfigurationNeedsCanvas', __javascriptError); return true; }}

async function downloadUnmodifiedRenderedSource(video, sourceName, dotnet, outputWidth, outputHeight, duration) { try {
    const sourceUrl = String(video.currentSrc || video.src || '').trim();
    if (!sourceUrl) return false;
    const response = await fetch(sourceUrl);
    if (!response.ok) return false;
    const blob = await response.blob();
    if (!blob.size) return false;
    const mimeType = baseMimeType(blob.type, 'video/webm');
    const fileName = renderedVideoFileName(sourceName, mimeType);
    downloadBlob(blob, fileName);
    try {
        await dotnet?.invokeMethodAsync('RenderedVideoExportReady', fileName, mimeType, blob.size, outputWidth, outputHeight, duration);
    } catch (__callbackError) {
        const message = String(__callbackError?.message || __callbackError || '').toLowerCase();
        if (message.includes('disconnected') || message.includes('disposed') || message.includes('connection') || message.includes('circuit'))
            console.debug('PublisherStudio rendered-source completion callback ended with the Blazor circuit.', __callbackError);
        else
            publisherStudioDiagnostics.report('js/mediaStudioInterop.js:render-export:direct-source-ready-callback', __callbackError);
    }
    return true;
 } catch (__javascriptError) {
    publisherStudioDiagnostics.report('js/mediaStudioInterop.js:downloadUnmodifiedRenderedSource', __javascriptError);
    return false;
 }}

async function estimateDecodedVideoFrameRate(video, startSeconds, job) { try {
    if (typeof video.requestVideoFrameCallback !== 'function') return 0;
    const start = Math.max(0, Number(startSeconds) || 0);
    const previousMuted = video.muted;
    const previousRate = video.playbackRate;
    video.muted = true;
    video.playbackRate = 1;
    video.currentTime = start;
    await new Promise((resolve, reject) => { try {
        if (Math.abs((Number(video.currentTime) || 0) - start) <= .03 && video.readyState >= 2) { resolve(); return; }
        const cleanup = () => { video.removeEventListener('seeked', ready); video.removeEventListener('error', failed); };
        const ready = () => { cleanup(); resolve(); };
        const failed = () => { cleanup(); reject(new Error('The browser could not seek while probing source frame rate.')); };
        video.addEventListener('seeked', ready, { once: true });
        video.addEventListener('error', failed, { once: true });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:estimate-frame-rate:seek', __javascriptError); reject(__javascriptError); }});
    if (job?.cancelled) return 0;

    const mediaTimes = [];
    let callbackId = 0;
    let timeoutId = 0;
    const samplePromise = new Promise(resolve => { try {
        let settled = false;
        const finish = () => {
            if (settled) return;
            settled = true;
            if (timeoutId) clearTimeout(timeoutId);
            if (callbackId && typeof video.cancelVideoFrameCallback === 'function') {
                try { video.cancelVideoFrameCallback(callbackId); }
                catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:estimate-frame-rate:cancel-callback', __caughtJavaScriptError); }
            }
            resolve();
        };
        const sample = (_now, metadata) => { try {
            callbackId = 0;
            if (job?.cancelled) { finish(); return; }
            const mediaTime = Number(metadata?.mediaTime);
            if (Number.isFinite(mediaTime)) mediaTimes.push(mediaTime);
            if (mediaTimes.length >= 8 || video.ended) { finish(); return; }
            callbackId = video.requestVideoFrameCallback(sample);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:estimate-frame-rate:sample', __javascriptError); finish(); }};
        callbackId = video.requestVideoFrameCallback(sample);
        timeoutId = window.setTimeout(finish, 800);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:estimate-frame-rate:promise', __javascriptError); resolve(); }});
    try { await video.play(); }
    catch (__playError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:estimate-frame-rate:play', __playError); }
    await samplePromise;
    video.pause();
    video.muted = previousMuted;
    video.playbackRate = previousRate;

    const deltas = [];
    for (let index = 1; index < mediaTimes.length; index++) {
        const delta = mediaTimes[index] - mediaTimes[index - 1];
        if (Number.isFinite(delta) && delta > .0001 && delta < .2) deltas.push(delta);
    }
    if (deltas.length === 0) return 0;
    deltas.sort((left, right) => left - right);
    const median = deltas[Math.floor(deltas.length / 2)];
    const estimated = median > 0 ? 1 / median : 0;
    if (!Number.isFinite(estimated) || estimated < 1) return 0;
    return Math.min(240, Math.max(1, estimated));
 } catch (__javascriptError) {
    publisherStudioDiagnostics.report('js/mediaStudioInterop.js:estimateDecodedVideoFrameRate', __javascriptError);
    try { video.pause(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:estimate-frame-rate:cleanup-pause', __caughtJavaScriptError); }
    return 0;
 }}

async function runRenderedVideoExport(state, sourceId, dotnet, effectConfig, rawRecordingOptions, startSeconds, endSeconds, playbackRate, audioGain, sourceName) { try {
    const source = mediaElement(sourceId);
    if (!(source instanceof HTMLVideoElement)) throw new Error('The active Video Studio source is not a video element.');
    if (!source.currentSrc && !source.src) throw new Error('The active video has no browser-readable source.');
    if (typeof MediaRecorder === 'undefined') throw new Error('This browser does not support MediaRecorder video export.');
    if (typeof HTMLCanvasElement.prototype.captureStream !== 'function') throw new Error('This browser cannot capture a rendered video canvas.');
    if (!window.publisherVideoEffects?.install) throw new Error('The Video Studio effect renderer is not available.');

    const options = normalizedRecordingOptions(rawRecordingOptions);
    const video = document.createElement('video');
    const canvas = document.createElement('canvas');
    const runtimeKey = `media-studio-render-export-${Date.now()}-${Math.random().toString(16).slice(2)}`;
    video.preload = 'auto';
    video.playsInline = true;
    video.src = source.currentSrc || source.src;
    video.style.cssText = 'position:fixed;left:-100000px;top:-100000px;width:2px;height:2px;opacity:0;pointer-events:none';
    canvas.style.cssText = 'position:fixed;left:-100000px;top:-100000px;width:2px;height:2px;opacity:0;pointer-events:none';
    document.body.append(video, canvas);

    const job = state.renderExport;
    job.video = video;
    job.canvas = canvas;
    job.runtimeKey = runtimeKey;
    let audioContext = null;
    let audioSource = null;
    let audioGainNode = null;
    let audioDestination = null;
    let stream = null;
    try {
        await waitForMetadata(video);
        if (job.cancelled) return;
        const duration = Math.max(.01, Number(video.duration) || 0);
        const start = Math.max(0, Math.min(duration, Number(startSeconds) || 0));
        const requestedEnd = Number(endSeconds);
        const end = Math.max(start + .001, Math.min(duration, Number.isFinite(requestedEnd) && requestedEnd > start ? requestedEnd : duration));
        const sourceWidth = Math.max(2, Math.trunc(Number(video.videoWidth) || 2));
        const sourceHeight = Math.max(2, Math.trunc(Number(video.videoHeight) || 2));
        const outputWidth = options.width > 0 ? options.width : sourceWidth;
        const outputHeight = options.height > 0 ? options.height : sourceHeight;
        const requestedFrameRate = options.frameRate > 0 ? options.frameRate : options.maximumFrameRate;
        const playback = Math.max(.1, Number(playbackRate) || 1);
        const gain = Math.max(0, Math.min(1, Number(audioGain) || 0));
        const fullSourceRange = start <= .002 && end >= duration - .002;
        const sourceDimensionsPreserved = outputWidth === sourceWidth && outputHeight === sourceHeight;
        const noVisualBake = !videoEffectConfigurationNeedsCanvas(effectConfig);
        if (fullSourceRange && sourceDimensionsPreserved && noVisualBake && Math.abs(playback - 1) <= .001 && Math.abs(gain - 1) <= .001) {
            if (await downloadUnmodifiedRenderedSource(video, sourceName, dotnet, sourceWidth, sourceHeight, duration)) return;
        }

        let estimatedSourceFrameRate = 0;
        if (requestedFrameRate <= 0 && typeof video.requestVideoFrameCallback === 'function')
            estimatedSourceFrameRate = await estimateDecodedVideoFrameRate(video, start, job);
        if (job.cancelled) return;
        const fallbackFrameRate = Math.max(24, Math.min(240, requestedFrameRate > 0 ? requestedFrameRate : estimatedSourceFrameRate > 0 ? estimatedSourceFrameRate : 60));
        canvas.width = outputWidth;
        canvas.height = outputHeight;

        // Native/source mode must not silently fall back to the browser's canvas default cadence.
        // A zero-rate canvas track is advanced explicitly after each decoded frame. If this browser
        // lacks CanvasCaptureMediaStreamTrack.requestFrame, use the measured source cadence instead.
        let canvasVideoTrack = null;
        let canFollowSourceFrames = false;
        if (requestedFrameRate <= 0 && typeof video.requestVideoFrameCallback === 'function') {
            stream = canvas.captureStream(0);
            canvasVideoTrack = stream.getVideoTracks?.()[0] || null;
            canFollowSourceFrames = typeof canvasVideoTrack?.requestFrame === 'function';
            if (!canFollowSourceFrames) {
                try { for (const track of stream.getTracks?.() || []) track.stop(); }
                catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:render-export:replace-zero-rate-track', __caughtJavaScriptError); }
                stream = canvas.captureStream(fallbackFrameRate);
                canvasVideoTrack = stream.getVideoTracks?.()[0] || null;
            }
        } else {
            stream = canvas.captureStream(fallbackFrameRate);
            canvasVideoTrack = stream.getVideoTracks?.()[0] || null;
        }
        job.stream = stream;
        let lastCapturedMediaTime = -1;
        const renderConfig = {
            ...(effectConfig || {}),
            outputWidth,
            outputHeight,
            maximumPixels: 0,
            forceCanvas: true,
            onFrameRendered: canFollowSourceFrames
                ? mediaTime => { try {
                    const normalizedTime = Math.max(0, Number(mediaTime) || 0);
                    if (Math.abs(normalizedTime - lastCapturedMediaTime) <= .000001) return;
                    lastCapturedMediaTime = normalizedTime;
                    canvasVideoTrack.requestFrame();
                 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:render-export:request-source-frame', __javascriptError); }}
                : null
        };
        window.publisherVideoEffects.install(runtimeKey, video, canvas, renderConfig);

        video.currentTime = start;
        await new Promise((resolve, reject) => { try {
            if (Math.abs((Number(video.currentTime) || 0) - start) <= .03 && video.readyState >= 2) { resolve(); return; }
            const cleanup = () => { video.removeEventListener('seeked', ready); video.removeEventListener('error', failed); };
            const ready = () => { cleanup(); resolve(); };
            const failed = () => { cleanup(); reject(new Error('The browser could not seek the source video for rendered export.')); };
            video.addEventListener('seeked', ready, { once: true });
            video.addEventListener('error', failed, { once: true });
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:render-export:seek-promise', __javascriptError); reject(__javascriptError); }});
        await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
        if (job.cancelled) return;

        if (gain > 0) {
            const AudioContextType = window.AudioContext || window.webkitAudioContext;
            if (AudioContextType) {
                audioContext = new AudioContextType();
                try { await audioContext.resume(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:render-export:audio-resume', __caughtJavaScriptError); }
                audioSource = audioContext.createMediaElementSource(video);
                audioGainNode = audioContext.createGain();
                audioGainNode.gain.value = gain;
                audioDestination = audioContext.createMediaStreamDestination();
                audioSource.connect(audioGainNode);
                audioGainNode.connect(audioDestination);
                for (const track of audioDestination.stream.getAudioTracks()) stream.addTrack(track);
            }
        }

        const exportProfileOptions = {
            ...options,
            width: outputWidth,
            height: outputHeight,
            maximumFrameRate: fallbackFrameRate,
            bitrateFrameRate: fallbackFrameRate,
            adaptive: {
                ...options.adaptive,
                preserveNativeResolution: true,
                allowFrameRateReduction: false,
                allowResolutionReduction: false
            }
        };
        const adaptiveProfile = await probeAdaptiveRecordingProfile(stream, 'mixed', exportProfileOptions);
        if (job.cancelled) return;
        const mimeType = adaptiveProfile?.mimeType || preferredMime('video', options.codecPreference, options.adaptive.codecPreferenceOrder);
        const recorderOptions = {};
        if (mimeType) recorderOptions.mimeType = mimeType;
        const selectedVideoBitsPerSecond = options.adaptive.enabled && options.adaptive.adaptVideo
            ? Number(adaptiveProfile?.videoBitsPerSecond) || 0
            : options.videoBitsPerSecond;
        const selectedAudioBitsPerSecond = options.adaptive.enabled && options.adaptive.adaptAudio
            ? Number(adaptiveProfile?.audioBitsPerSecond) || 0
            : options.audioBitsPerSecond;
        if (selectedVideoBitsPerSecond > 0) recorderOptions.videoBitsPerSecond = selectedVideoBitsPerSecond;
        if (stream.getAudioTracks().length && selectedAudioBitsPerSecond > 0) recorderOptions.audioBitsPerSecond = selectedAudioBitsPerSecond;
        const recorder = Object.keys(recorderOptions).length ? new MediaRecorder(stream, recorderOptions) : new MediaRecorder(stream);
        job.recorder = recorder;
        const chunks = [];
        const blobPromise = new Promise((resolve, reject) => { try {
            recorder.addEventListener('dataavailable', event => { if (event.data?.size) chunks.push(event.data); });
            recorder.addEventListener('error', event => reject(event.error || new Error('The browser recorder failed while baking Video Studio effects.')), { once: true });
            recorder.addEventListener('stop', () => { try {
                const blob = new Blob(chunks, { type: recorder.mimeType || mimeType || 'video/webm' });
                if (!job.cancelled && !blob.size) reject(new Error('Rendered export produced an empty video file.'));
                else resolve(blob);
             } catch (error) { reject(error); } }, { once: true });
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:render-export:blob-promise', __javascriptError); reject(__javascriptError); }});

        video.playbackRate = playback;
        recorder.start(options.recorderChunkMilliseconds);
        await video.play();
        await new Promise((resolve, reject) => { try {
            job.resolvePlayback = resolve;
            const tick = () => { try {
                if (job.cancelled) { resolve(); return; }
                if (video.error) { reject(new Error('The source video failed while rendering effects.')); return; }
                if (video.ended || Number(video.currentTime) >= end - .001) { resolve(); return; }
                job.frame = requestAnimationFrame(tick);
             } catch (error) { reject(error); }};
            tick();
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:render-export:playback-promise', __javascriptError); reject(__javascriptError); }});
        job.resolvePlayback = null;
        if (job.frame) cancelAnimationFrame(job.frame);
        job.frame = 0;
        video.pause();
        if (recorder.state !== 'inactive') recorder.stop();
        const blob = await blobPromise;
        if (job.cancelled) return;
        const finalMimeType = baseMimeType(blob.type || recorder.mimeType, 'video/webm');
        const fileName = renderedVideoFileName(sourceName, finalMimeType);
        downloadBlob(blob, fileName);
        try {
            await dotnet?.invokeMethodAsync('RenderedVideoExportReady', fileName, finalMimeType, blob.size, outputWidth, outputHeight, Math.max(.001, (end - start) / playback));
        } catch (__callbackError) {
            // The video is already complete and downloaded. A circuit teardown or disposed .NET
            // callback must not reclassify a successful render as a failed browser render.
            const callbackMessage = String(__callbackError?.message || __callbackError || '').toLowerCase();
            if (callbackMessage.includes('disconnected') || callbackMessage.includes('disposed') || callbackMessage.includes('connection') || callbackMessage.includes('circuit'))
                console.debug('PublisherStudio rendered-video completion callback ended with the Blazor circuit.', __callbackError);
            else
                publisherStudioDiagnostics.report('js/mediaStudioInterop.js:render-export:ready-callback', __callbackError);
        }
    } finally {
        try { video.pause(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:render-export:cleanup-pause', __caughtJavaScriptError); }
        try { window.publisherVideoEffects?.dispose(runtimeKey); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:render-export:cleanup-runtime', __caughtJavaScriptError); }
        try { for (const track of stream?.getTracks?.() || []) track.stop(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:render-export:cleanup-tracks', __caughtJavaScriptError); }
        try { audioSource?.disconnect?.(); audioGainNode?.disconnect?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:render-export:cleanup-audio-nodes', __caughtJavaScriptError); }
        try { await audioContext?.close?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:render-export:cleanup-audio-context', __caughtJavaScriptError); }
        video.removeAttribute('src');
        try { video.load(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:render-export:cleanup-video-load', __caughtJavaScriptError); }
        video.remove();
        canvas.remove();
        if (state.renderExport === job) state.renderExport = null;
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:runRenderedVideoExport', __javascriptError); throw __javascriptError; }}

export function startRenderedVideoExport(id, dotnet, effectConfig, recordingOptions, startSeconds, endSeconds, playbackRate, audioGain, sourceName) { try {
    const state = stateFor(id);
    if (state.renderExport) return false;
    if (dotnet) state.dotnet = dotnet;
    const job = { cancelled: false, recorder: null, video: null, canvas: null, stream: null, runtimeKey: '', frame: 0, resolvePlayback: null };
    state.renderExport = job;
    void runRenderedVideoExport(state, id, state.dotnet, effectConfig, recordingOptions, startSeconds, endSeconds, playbackRate, audioGain, sourceName)
        .catch(async error => { try {
            if (!job.cancelled) {
                const message = error?.message || String(error);
                try { await state.dotnet?.invokeMethodAsync('RenderedVideoExportFailed', message); }
                catch (callbackError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:render-export:failed-callback', callbackError); }
            }
            if (state.renderExport === job) state.renderExport = null;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:startRenderedVideoExport:catch', __javascriptError); }});
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:startRenderedVideoExport', __javascriptError); throw __javascriptError; }}

export function cancelRenderedVideoExport(id) { try {
    const state = stateFor(id);
    stopRenderedVideoExportJob(state.renderExport);
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:cancelRenderedVideoExport', __javascriptError); throw __javascriptError; }}

export function downloadRetainedRecording(id, requestedFileName) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:downloadRetainedRecording@1366', __javascriptError); throw __javascriptError; }}

export async function downloadCurrentMedia(id, requestedFileName) { try {
    const state = stateFor(id);
    const retained = state.retainedRecordingBlob;
    if (retained instanceof Blob && retained.size) {
        downloadBlob(retained, String(requestedFileName || state.retainedRecordingFileName || recordingFileName(state.retainedRecordingKind, state.retainedRecordingMimeType)));
        return true;
    }
    const element = mediaElement(id);
    const source = String(element.currentSrc || element.src || '').trim();
    if (!source) throw new Error('The selected media has no browser-readable source to download.');
    const response = await fetch(source);
    if (!response.ok) throw new Error(`The selected media source returned HTTP ${response.status}.`);
    const blob = await response.blob();
    if (!blob.size) throw new Error('The selected media source is empty.');
    downloadBlob(blob, String(requestedFileName || 'publisherstudio-media'));
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:downloadCurrentMedia', __javascriptError); throw __javascriptError; }}

export function clearRetainedRecording(id) { try {
    const state = stateFor(id);
    releaseRetainedRecording(state);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:clearRetainedRecording@1380', __javascriptError); throw __javascriptError; }}

function requestMediaRecordingStop(state) { try {
    if (!state || state.recordingStopRequested || state.recordingFinalizing) return false;
    if (!state.recorder || state.recorder.state === 'inactive') return Boolean(state.retainedRecordingInfo);
    state.recordingStopRequested = true;
    state.recordingFinalizing = true;
    state.recordingStopRequestedAtMs = performance.now();
    try {
        if (state.recorder.state === 'recording' && typeof state.recorder.requestData === 'function')
            state.recorder.requestData();
    } catch (__caughtJavaScriptError) {
        publisherStudioDiagnostics.report('js/mediaStudioInterop.js:requestMediaRecordingStop:requestData', __caughtJavaScriptError);
    }
    state.recorder.stop();
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:requestMediaRecordingStop', __javascriptError); throw __javascriptError; }}

export async function startMediaRecording(id, kind, source, dotnet, options = null) { try {
    if (!navigator.mediaDevices || typeof MediaRecorder === 'undefined')
        throw new Error('This browser does not support media recording.');
    const state = stateFor(id);
    state.dotnet = dotnet || state.dotnet;
    if (state.recorder && state.recorder.state !== 'inactive') return null;
    state.recordingFinalizationError = '';
    state.recordingStopRequested = false;
    state.recordingFinalizing = false;
    state.recordingStartedAtMs = 0;
    state.recordingStopRequestedAtMs = 0;
    const recordingOptions = normalizedRecordingOptions(options);
    state.recordingOptions = recordingOptions;
    state.stream = await streamFor(kind, source, recordingOptions);
    state.chunks = [];
    state.discardRecording = false;
    const preview = mediaElement(id);
    if (kind === 'video') startRecordingPreviewWatch(state);
    const adaptiveProfile = kind === 'video'
        ? await probeAdaptiveRecordingProfile(state.stream, source, recordingOptions)
        : { mimeType: preferredMime(kind, recordingOptions.codecPreference, recordingOptions.adaptive.codecPreferenceOrder), videoBitsPerSecond: 0, audioBitsPerSecond: adaptiveAudioBitsPerSecond(state.stream, recordingOptions) };
    const mimeType = adaptiveProfile?.mimeType || preferredMime(kind, recordingOptions.codecPreference, recordingOptions.adaptive.codecPreferenceOrder);
    const recorderOptions = {};
    if (mimeType) recorderOptions.mimeType = mimeType;
    const selectedVideoBitsPerSecond = recordingOptions.adaptive.enabled && recordingOptions.adaptive.adaptVideo
        ? Number(adaptiveProfile?.videoBitsPerSecond) || 0
        : recordingOptions.videoBitsPerSecond;
    const selectedAudioBitsPerSecond = recordingOptions.adaptive.enabled && recordingOptions.adaptive.adaptAudio
        ? Number(adaptiveProfile?.audioBitsPerSecond) || 0
        : recordingOptions.audioBitsPerSecond;
    if (kind === 'video' && selectedVideoBitsPerSecond > 0) recorderOptions.videoBitsPerSecond = selectedVideoBitsPerSecond;
    if (selectedAudioBitsPerSecond > 0) recorderOptions.audioBitsPerSecond = selectedAudioBitsPerSecond;
    try {
        state.recorder = Object.keys(recorderOptions).length ? new MediaRecorder(state.stream, recorderOptions) : new MediaRecorder(state.stream);
    } catch (error) {
        releaseRecordingCapture(state);
        state.stream = null;
        throw error;
    }
    state.recorder.addEventListener('dataavailable', event => { try {
        if (event.data && event.data.size) state.chunks.push(event.data);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:state.recorder.addEventListener@1414', __javascriptError); throw __javascriptError; }});
    state.recorder.addEventListener('stop', async () => { try {
        try {
            detachRecordingPreview(state);
            if (state.discardRecording) return;
            const blob = new Blob(state.chunks, { type: state.recorder?.mimeType || mimeType || (kind === 'video' ? 'video/webm' : 'audio/webm') });
            if (!blob.size) throw new Error('The browser completed the recording but produced an empty file.');
            // The Blob is complete now. Release capture before slower metadata/poster inspection so
            // browser sharing indicators stop promptly without risking the final dataavailable event.
            releaseRecordingCapture(state);
            state.stream = null;
            await retainRecording(state, blob, kind);
        } catch (error) {
            state.recordingFinalizationError = error?.message || String(error);
            try { await state.dotnet?.invokeMethodAsync('MediaRecordingFailed', state.recordingFinalizationError); }
            catch (callbackError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:recording-failed:dotnet-callback', callbackError); }
        } finally {
            releaseRecordingCapture(state);
            state.stream = null;
            state.recorder = null;
            state.chunks = [];
            state.recordingStopRequested = false;
            state.recordingFinalizing = false;
            state.recordingStartedAtMs = 0;
            state.recordingStopRequestedAtMs = 0;
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:state.recorder.addEventListener@1417', __javascriptError); throw __javascriptError; }}, { once: true });
    const endingTracks = kind === 'video' && state.stream.getVideoTracks().length
        ? state.stream.getVideoTracks()
        : state.stream.getAudioTracks();
    for (const track of endingTracks) {
        track.addEventListener('ended', () => { try {
            requestMediaRecordingStop(state);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:track.addEventListener@1441', __javascriptError); throw __javascriptError; }}, { once: true });
    }
    state.recordingStartedAtMs = performance.now();
    state.recorder.start(recordingOptions.recorderChunkMilliseconds);
    releaseRetainedRecording(state);
    await state.dotnet?.invokeMethodAsync('MediaRecordingCleared');
    const videoSettings = state.stream.getVideoTracks?.()[0]?.getSettings?.() || {};
    return {
        mimeType: state.recorder?.mimeType || mimeType || '',
        width: Number(videoSettings.width) || 0,
        height: Number(videoSettings.height) || 0,
        frameRate: Number(videoSettings.frameRate) || 0,
        videoBitsPerSecond: Number(state.recorder?.videoBitsPerSecond) || selectedVideoBitsPerSecond || 0,
        audioBitsPerSecond: Number(state.recorder?.audioBitsPerSecond) || selectedAudioBitsPerSecond || 0
    };
 } catch (__javascriptError) {
    if (__javascriptError?.name !== 'NotAllowedError' && __javascriptError?.name !== 'AbortError')
        publisherStudioDiagnostics.report('js/mediaStudioInterop.js:startMediaRecording', __javascriptError);
    throw __javascriptError;
 }}

export function stopMediaRecording(id, dotnet) { try {
    const state = stateFor(id);
    if (dotnet) state.dotnet = dotnet;
    state.discardRecording = false;
    state.recordingFinalizationError = '';
    // Preserve the original asynchronous MediaRecorder contract: trigger Stop once and return
    // immediately. The browser owns finalization and reports MediaRecordingReady later. This keeps
    // Blazor/SignalR out of the potentially slow Blob metadata path and makes repeated clicks safe.
    return requestMediaRecordingStop(state);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:stopMediaRecording', __javascriptError); throw __javascriptError; }}

export function cancelMediaRecording(id) { try {
    const state = stateFor(id);
    state.discardRecording = true;
    try { if (state.recorder && state.recorder.state !== 'inactive') state.recorder.stop(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@1464', __caughtJavaScriptError);  }
    releaseRecordingCapture(state);
    state.recordingStopRequested = false;
    state.recordingFinalizing = false;
    state.recordingStartedAtMs = 0;
    state.recordingStopRequestedAtMs = 0;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:cancelMediaRecording@1456', __javascriptError); throw __javascriptError; }}

export async function playMediaRange(id, start, end, volume, rate, muted, loop) { try {
    const element = mediaElement(id);
    if (!element) return false;
    const state = stateFor(id);
    cancelRangePlayback(state, element, true);
    const commandVersion = state.playCommandVersion;
    const rangeStart = finiteMediaTime(start);
    const requestedEnd = Number(end);
    const mediaDuration = Number(element.duration);
    const rangeEnd = Math.max(rangeStart + .01, Number.isFinite(requestedEnd) ? requestedEnd : (Number.isFinite(mediaDuration) ? mediaDuration : rangeStart + .01));
    try { element.currentTime = finiteMediaTime(rangeStart); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@1477', __caughtJavaScriptError);  }
    element.volume = Math.max(0, Math.min(1, Number(volume) || 0));
    element.playbackRate = Math.max(.25, Math.min(4, Number(rate) || 1));
    element.muted = Boolean(muted);
    state.stopAt = rangeEnd;
    state.rangeHandler = () => { try {
        if (commandVersion !== state.playCommandVersion || state.stopAt == null || element.currentTime < state.stopAt - .015) return;
        if (loop) {
            try { element.currentTime = finiteMediaTime(rangeStart); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@1485', __caughtJavaScriptError);  }
            const retry = element.play();
            if (retry?.catch) retry.catch(error => { try { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:promise-catch@1487', error); 
                if (!isInterruptedPlaybackError(error) && commandVersion === state.playCommandVersion)
                    console.warn('PublisherStudio loop playback failed.', error);
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:retry.catch@1487', __javascriptError); throw __javascriptError; }});
        } else {
            const stopAt = finiteMediaTime(state.stopAt, rangeEnd);
            cancelRangePlayback(state, element, true);
            try { element.currentTime = stopAt; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@1494', __caughtJavaScriptError);  }
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:state.rangeHandler@1482', __javascriptError); throw __javascriptError; }};
    element.addEventListener('timeupdate', state.rangeHandler);
    try {
        const playRequest = element.play();
        if (playRequest?.then) await playRequest;
        return commandVersion === state.playCommandVersion;
    } catch (error) {
        if (!isInterruptedPlaybackError(error) && commandVersion === state.playCommandVersion)
            console.warn('PublisherStudio playback could not start.', error);
        if (commandVersion === state.playCommandVersion) cancelRangePlayback(state, element, false);
        return false;
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:playMediaRange@1468', __javascriptError); throw __javascriptError; }}

export function pauseMedia(id) { try {
    const element = mediaElement(id);
    if (!element) return false;
    cancelRangePlayback(stateFor(id), element, true);
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:pauseMedia@1510', __javascriptError); throw __javascriptError; }}

export function seekMedia(id, seconds) { try {
    const element = mediaElement(id);
    if (!element) return false;
    cancelRangePlayback(stateFor(id), element, true);
    try { element.currentTime = finiteMediaTime(seconds); } catch { return false; }
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:seekMedia@1517', __javascriptError); throw __javascriptError; }}

export function getMediaPosition(id) { try {
    const element = mediaElement(id);
    return element && Number.isFinite(element.currentTime) ? element.currentTime : 0;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:getMediaPosition@1525', __javascriptError); throw __javascriptError; }}

export function disposeMediaStudio(id) { try {
    const state = studioStates.get(id);
    if (!state) return;
    state.discardRecording = true;
    stopRenderedVideoExportJob(state.renderExport);
    const element = mediaElement(id);
    try { if (state.recorder && state.recorder.state !== 'inactive') state.recorder.stop(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:suppressed-catch@1539', __caughtJavaScriptError);  }
    releaseRecordingCapture(state);
    if (element) cancelRangePlayback(state, element, true);
    if (state.keyboardHandler) document.removeEventListener("keydown", state.keyboardHandler, true);
    state.keyboardHandler = null;
    releaseMediaDropBindings(state);
    releaseVideoTimeOverlayBindings(state);
    releaseSequenceSelectionBindings(state);
    releaseFrameOverlayBindings(state);
    if (state.effectRuntimeKey) window.publisherVideoEffects?.dispose(state.effectRuntimeKey);
    state.effectRuntimeKey = '';
    releaseRetainedRecording(state);
    studioStates.delete(id);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:disposeMediaStudio@1530', __javascriptError); throw __javascriptError; }}

export function horizontalRatio(elementId, clientX) { try {
    const element = document.getElementById(elementId);
    if (!element) return 0;
    const bounds = element.getBoundingClientRect();
    return Math.max(0, Math.min(1, (Number(clientX) - bounds.left) / Math.max(1, bounds.width)));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:horizontalRatio@1554', __javascriptError); throw __javascriptError; }}

export function normalizedPoint(elementId, clientX, clientY) { try {
    const element = document.getElementById(elementId);
    if (!element) return { x: 0, y: 0 };
    const bounds = element.getBoundingClientRect();
    return {
        x: Math.max(0, Math.min(1, (Number(clientX) - bounds.left) / Math.max(1, bounds.width))),
        y: Math.max(0, Math.min(1, (Number(clientY) - bounds.top) / Math.max(1, bounds.height)))
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:normalizedPoint@1561', __javascriptError); throw __javascriptError; }}

export function mediaClipPath(points) { try {
    const values = Array.isArray(points) ? points : [];
    if (values.length < 3) return '';
    return `polygon(${values.map(point => { try { return (`${Math.max(0, Math.min(1, Number(point?.x) || 0)) * 100}% ${Math.max(0, Math.min(1, Number(point?.y) || 0)) * 100}%`); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:callback:values.map@1574', __javascriptError); throw __javascriptError; } }).join(',')})`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/mediaStudioInterop.js:mediaClipPath@1571', __javascriptError); throw __javascriptError; }}

