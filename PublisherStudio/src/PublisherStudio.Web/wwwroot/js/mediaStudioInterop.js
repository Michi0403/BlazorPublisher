const studioStates = new Map();
const CHUNK_SIZE = 24 * 1024;

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

function stateFor(id) {
    let state = studioStates.get(id);
    if (!state) {
        state = { id, dotnet: null, recorder: null, stream: null, chunks: [], rangeHandler: null, stopAt: null, discardRecording: false };
        studioStates.set(id, state);
    }
    return state;
}

function mediaElement(id) {
    const element = document.getElementById(id);
    return element instanceof HTMLMediaElement ? element : null;
}

export function initializeMediaStudio(id, dotnet) {
    const state = stateFor(id);
    state.dotnet = dotnet;
    state.discardRecording = false;
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

async function transferRecording(state, blob, kind) {
    // Codec lists such as "video/webm;codecs=vp8,opus" contain a comma. A comma is
    // also the data-URL header separator, so FileReader would otherwise create a
    // malformed URL that the video element cannot decode. Store the container MIME
    // type only; the encoded WebM bytes still retain their actual codecs.
    const mimeType = baseMimeType(blob.type, kind === 'video' ? 'video/webm' : 'audio/webm');
    const transferableBlob = blob.type === mimeType ? blob : blob.slice(0, blob.size, mimeType);
    const dataUrl = await new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(normalizeMediaDataUrl(String(reader.result || ''), mimeType));
        reader.onerror = () => reject(reader.error || new Error('The recording could not be read.'));
        reader.readAsDataURL(transferableBlob);
    });
    const transferId = crypto.randomUUID();
    const chunkCount = Math.ceil(dataUrl.length / CHUNK_SIZE);
    const accepted = await state.dotnet.invokeMethodAsync('BeginMediaRecordingTransfer', transferId, mimeType, dataUrl.length, chunkCount);
    if (!accepted) throw new Error('The recording is too large for the embedded publication.');
    for (let index = 0; index < chunkCount; index++) {
        const chunk = dataUrl.slice(index * CHUNK_SIZE, (index + 1) * CHUNK_SIZE);
        const ok = await state.dotnet.invokeMethodAsync('AppendMediaRecordingChunk', transferId, index, chunk);
        if (!ok) throw new Error('The recording transfer was interrupted.');
    }
    await state.dotnet.invokeMethodAsync('CompleteMediaRecordingTransfer', transferId);
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
            await transferRecording(state, blob, kind);
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
    studioStates.delete(id);
}
