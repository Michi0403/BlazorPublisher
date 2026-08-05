// javascript-diagnostics: guarded
var publisherStudioDiagnostics = globalThis.publisherStudioJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`PublisherStudio JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("PublisherStudio fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`PublisherStudio fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
(() => { try {
    const sources = new Map();
    const externalAuthorizationWindows = new Map();
    const outputContext = { mode: "operator", platform: "Preview", channel: "", outputId: "" };
    let hotkeyListener = null;
    let hotkeyReference = null;
    let configuredHotkeys = [];
    let programCapture = null;
    let chatBridgeState = null;

    function runtimeHttpBase(configured) { try {
        const value = String(configured || "").trim().replace(/\/$/, "");
        return value || window.location.origin;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:runtimeHttpBase@12', __javascriptError); throw __javascriptError; }}

    function runtimeWsBase(configured) { try {
        return runtimeHttpBase(configured).replace(/^http/i, "ws");
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:runtimeWsBase@17', __javascriptError); throw __javascriptError; }}

    function hexColor(value) { try {
        const match = /^#?([0-9a-f]{6})$/i.exec(String(value || ""));
        const hex = match ? match[1] : "00ff00";
        return [parseInt(hex.slice(0, 2), 16) / 255, parseInt(hex.slice(2, 4), 16) / 255, parseInt(hex.slice(4, 6), 16) / 255];
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:hexColor@21', __javascriptError); throw __javascriptError; }}


    function disconnectProgramAudio(state) { try {
        if (!state?.programAudio) return;
        try { state.programAudio.source.disconnect(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@30', __caughtJavaScriptError);  }
        try { state.programAudio.delay.disconnect(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@31', __caughtJavaScriptError);  }
        try { state.programAudio.gain.disconnect(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@32', __caughtJavaScriptError);  }
        state.programAudio = null;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:disconnectProgramAudio@28', __javascriptError); throw __javascriptError; }}

    function connectProgramAudio(state) { try {
        disconnectProgramAudio(state);
        if (!programCapture?.audioContext || !programCapture.audioDestination || !state?.stream) return;
        const audioTracks = state.stream.getAudioTracks?.() || [];
        if (!audioTracks.length) return;
        const audioStream = new MediaStream(audioTracks);
        const source = programCapture.audioContext.createMediaStreamSource(audioStream);
        const delay = programCapture.audioContext.createDelay(10);
        const gain = programCapture.audioContext.createGain();
        const config = state.config || {};
        delay.delayTime.value = Math.max(0, Math.min(10, Number(config.audioDelayMilliseconds || 0) / 1000));
        gain.gain.value = config.muted === true ? 0 : Math.max(0, Math.min(2, Number(config.volume ?? 1)));
        source.connect(delay).connect(gain).connect(programCapture.audioDestination);
        state.programAudio = { source, delay, gain };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:connectProgramAudio@36', __javascriptError); throw __javascriptError; }}

    function connectAllProgramAudio() { try {
        for (const state of sources.values()) connectProgramAudio(state);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:connectAllProgramAudio@52', __javascriptError); throw __javascriptError; }}

    async function acquireNative(config) { try {
        const mediaHostUrl = runtimeHttpBase(config.mediaHostUrl);
        let captureId = "";
        let target = null;
        let objectUrl = "";
        let socket = null;
        let stopped = false;
        const stopCapture = async () => { try {
            if (stopped) return;
            stopped = true;
            try { socket?.close(1000, "Native source detached"); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@66', __caughtJavaScriptError);  }
            try {
                target?.pause?.();
                target?.removeAttribute?.("src");
                target?.load?.();
            } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@71', __caughtJavaScriptError);  }
            if (objectUrl) {
                try { URL.revokeObjectURL(objectUrl); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@73', __caughtJavaScriptError);  }
                objectUrl = "";
            }
            if (captureId) {
                await fetch(`${mediaHostUrl}/api/mediahost/native-captures/${encodeURIComponent(captureId)}`, { method: "DELETE" }).catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/streamingInterop.js:promise-catch@77', __promiseError);  return (undefined); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:fetch(`${mediaHostUrl}/api/mediahost/native-captures/${encodeURICompon@77', __javascriptError); throw __javascriptError; } });
                captureId = "";
            }
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:stopCapture@63', __javascriptError); throw __javascriptError; }};

        try {
            const response = await fetch(`${mediaHostUrl}/api/mediahost/native-captures`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    kind: config.kind,
                    deviceId: config.deviceId || "",
                    audioDeviceId: config.audioDeviceId || "",
                    applicationId: config.applicationId || "",
                    nativeBackend: config.nativeBackend || "",
                    networkUrl: config.networkUrl || "",
                    includeAudio: !!config.includeAudio,
                    width: Math.max(320, Number(config.captureWidth || 1920)),
                    height: Math.max(180, Number(config.captureHeight || 1080)),
                    frameRate: Math.max(15, Number(config.captureFrameRate || 60)),
                    useDeviceTimestamps: config.useDeviceTimestamp !== false,
                    ffmpegPath: config.ffmpegPath || ""
                })
            });
            const result = await response.json().catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/streamingInterop.js:promise-catch@101', __promiseError);  return (({})); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:response.json().catch@101', __javascriptError); throw __javascriptError; } });
            if (!response.ok || !result.captureId) throw new Error(result.error || "PublisherStudio could not start native capture.");
            captureId = String(result.captureId);

            const isAudioOnly = String(config.kind || "").toLowerCase().endsWith("audio")
                || String(config.kind || "").toLowerCase() === "microphone";
            target = document.getElementById(config.videoId) || document.createElement(isAudioOnly ? "audio" : "video");
            target.autoplay = true;
            target.playsInline = true;
            target.muted = config.muted !== false;
            const mimeType = String(result.mimeType || (isAudioOnly ? "audio/webm;codecs=opus" : "video/webm;codecs=vp9,opus"));
            if (!window.MediaSource || !MediaSource.isTypeSupported(mimeType))
                throw new Error(`This browser cannot play the native capture profile ${mimeType}.`);

            const mediaSource = new MediaSource();
            const queue = [];
            let sourceBuffer = null;
            const pump = () => { try {
                if (stopped || !sourceBuffer || sourceBuffer.updating || mediaSource.readyState !== "open") return;
                try {
                    if (sourceBuffer.buffered.length && sourceBuffer.buffered.end(0) - sourceBuffer.buffered.start(0) > 30) {
                        sourceBuffer.remove(sourceBuffer.buffered.start(0), Math.max(sourceBuffer.buffered.start(0), sourceBuffer.buffered.end(0) - 20));
                        return;
                    }
                    const next = queue.shift();
                    if (next) sourceBuffer.appendBuffer(next);
                } catch (error) {
                    console.error("PublisherStudio native capture buffering failed", error);
                }
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:pump@118', __javascriptError); throw __javascriptError; }};
            await new Promise((resolve, reject) => { try {
                const timeout = setTimeout(() => { try { return (reject(new Error("The native capture media buffer did not open."))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:setTimeout@132', __javascriptError); throw __javascriptError; } }, 5000);
                mediaSource.addEventListener("sourceopen", () => { try {
                    clearTimeout(timeout);
                    try {
                        sourceBuffer = mediaSource.addSourceBuffer(mimeType);
                        try { sourceBuffer.mode = "sequence"; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@137', __caughtJavaScriptError);  }
                        sourceBuffer.addEventListener("updateend", pump);
                        resolve();
                    } catch (error) { reject(error); }
                 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:mediaSource.addEventListener@133', __javascriptError); throw __javascriptError; }}, { once: true });
                objectUrl = URL.createObjectURL(mediaSource);
                target.src = objectUrl;
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:ArrowFunction@131', __javascriptError); throw __javascriptError; }});

            const wsBase = runtimeWsBase(mediaHostUrl);
            socket = new WebSocket(`${wsBase}/api/mediahost/native-captures/${encodeURIComponent(captureId)}/websocket`);
            socket.binaryType = "arraybuffer";
            socket.addEventListener("message", event => { try { queue.push(new Uint8Array(event.data)); pump();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:socket.addEventListener@149', __javascriptError); throw __javascriptError; }});
            await new Promise((resolve, reject) => { try {
                const timeout = setTimeout(() => { try { return (reject(new Error("The native capture stream did not connect."))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:setTimeout@151', __javascriptError); throw __javascriptError; } }, 5000);
                socket.addEventListener("open", () => { try { clearTimeout(timeout); resolve();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:socket.addEventListener@152', __javascriptError); throw __javascriptError; }}, { once: true });
                socket.addEventListener("error", () => { try { clearTimeout(timeout); reject(new Error("The native capture WebSocket failed."));  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:socket.addEventListener@153', __javascriptError); throw __javascriptError; }}, { once: true });
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:ArrowFunction@150', __javascriptError); throw __javascriptError; }});
            await target.play().catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/streamingInterop.js:promise-catch@155', __promiseError);  return (undefined); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:target.play().catch@155', __javascriptError); throw __javascriptError; } });
            const capture = target.captureStream?.() || target.mozCaptureStream?.();
            if (!capture) throw new Error("This browser cannot expose the native media element as a MediaStream.");
            capture.__publisherNativeCleanup = stopCapture;
            capture.__publisherNativeElement = target;
            return capture;
        } catch (error) {
            await stopCapture();
            throw error;
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:acquireNative@56', __javascriptError); throw __javascriptError; }}

    function networkSourceNeedsNative(url) { try {
        const value = String(url || "").trim().toLowerCase();
        return /^(rtsp|rtsps|rtmp|rtmps|srt|udp|tcp):/.test(value);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:networkSourceNeedsNative@167', __javascriptError); throw __javascriptError; }}

    async function acquire(config) { try {
        const kind = String(config.kind || "Camera").toLowerCase();
        const backend = String(config.captureBackend || "Auto").toLowerCase();
        if (backend === "native") return acquireNative(config);
        if (kind === "camera" || kind === "capturedevice") {
            const videoConstraints = {
                width: { ideal: Math.max(320, Number(config.captureWidth || 1920)) },
                height: { ideal: Math.max(180, Number(config.captureHeight || 1080)) },
                frameRate: { ideal: Math.max(15, Number(config.captureFrameRate || 60)) }
            };
            if (config.deviceId) videoConstraints.deviceId = { exact: config.deviceId };
            try {
                const audioConstraints = config.includeAudio
                    ? (config.audioDeviceId ? { deviceId: { exact: config.audioDeviceId } } : true)
                    : false;
                return await navigator.mediaDevices.getUserMedia({ video: videoConstraints, audio: audioConstraints });
            } catch (error) {
                if (backend === "auto" && (config.nativeBackend || kind === "capturedevice")) return acquireNative(config);
                throw error;
            }
        }
        if (kind === "microphone") {
            try {
                return await navigator.mediaDevices.getUserMedia({ video: false, audio: config.deviceId ? { deviceId: { exact: config.deviceId } } : true });
            } catch (error) {
                if (backend === "auto" && config.nativeBackend) return acquireNative(config);
                throw error;
            }
        }
        if (kind === "screen" || kind === "window" || kind === "browsertab") {
            return navigator.mediaDevices.getDisplayMedia({
                video: {
                    displaySurface: kind === "browsertab" ? "browser" : kind === "window" ? "window" : "monitor",
                    width: { ideal: Math.max(320, Number(config.captureWidth || 1920)) },
                    height: { ideal: Math.max(180, Number(config.captureHeight || 1080)) },
                    frameRate: { ideal: Math.max(15, Number(config.captureFrameRate || 60)) }
                },
                audio: !!config.includeAudio,
                preferCurrentTab: kind === "browsertab",
                selfBrowserSurface: "exclude",
                surfaceSwitching: "include",
                systemAudio: config.includeAudio ? "include" : "exclude"
            });
        }
        if (kind === "networkmedia") {
            if (backend === "browser" || (backend === "auto" && !networkSourceNeedsNative(config.networkUrl))) return null;
            return acquireNative(config);
        }
        if (kind === "applicationaudio" || kind === "systemaudio") {
            if (navigator.mediaDevices?.getDisplayMedia) {
                try {
                    const selected = await navigator.mediaDevices.getDisplayMedia({
                        video: { displaySurface: kind === "applicationaudio" ? "window" : "monitor" },
                        audio: true,
                        systemAudio: "include",
                        windowAudio: kind === "applicationaudio" ? "window" : "system",
                        selfBrowserSurface: "exclude",
                        surfaceSwitching: "include"
                    });
                    const audioTracks = selected.getAudioTracks();
                    selected.getVideoTracks().forEach(track => { try { return (track.stop()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:selected.getVideoTracks().forEach@232', __javascriptError); throw __javascriptError; } });
                    if (audioTracks.length) return new MediaStream(audioTracks);
                    selected.getTracks().forEach(track => { try { return (track.stop()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:selected.getTracks().forEach@234', __javascriptError); throw __javascriptError; } });
                } catch (error) {
                    if (error?.name === "NotAllowedError" && backend === "browser") throw error;
                }
            }
            window.dispatchEvent(new CustomEvent("publisherstudio:native-source-request", { detail: config }));
            if (backend !== "browser") return acquireNative(config);
            throw new Error("This browser did not expose isolated application/system audio and the source is restricted to Browser capture.");
        }
        return null;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:acquire@172', __javascriptError); throw __javascriptError; }}

    function installMeter(stream, meter) { try {
        if (!stream || !meter || !window.AudioContext) return null;
        const context = new AudioContext();
        const source = context.createMediaStreamSource(stream);
        const analyser = context.createAnalyser();
        analyser.fftSize = 512;
        source.connect(analyser);
        const values = new Uint8Array(analyser.frequencyBinCount);
        let frame = 0;
        const draw = () => { try {
            analyser.getByteTimeDomainData(values);
            let sum = 0;
            for (const value of values) { const normalized = (value - 128) / 128; sum += normalized * normalized; }
            meter.value = Math.min(1, Math.sqrt(sum / values.length) * 2.5);
            frame = requestAnimationFrame(draw);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:draw@255', __javascriptError); throw __javascriptError; }};
        draw();
        return () => { try { cancelAnimationFrame(frame); source.disconnect(); analyser.disconnect(); context.close().catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/streamingInterop.js:promise-catch@263', __promiseError);  return (undefined); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:context.close().catch@263', __javascriptError); throw __javascriptError; } });  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:ArrowFunction@263', __javascriptError); throw __javascriptError; }};
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:installMeter@246', __javascriptError); throw __javascriptError; }}

    function legacyVideoLayers(config) { try {
        const filters = [];
        const add = (kind, amount, extra = {}) => { try { return (filters.push({ kind, enabled: true, amount, ...extra })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:add@268', __javascriptError); throw __javascriptError; } };
        if (Math.abs(Number(config.brightness ?? 1) - 1) > .001) add('Brightness', Number(config.brightness));
        if (Math.abs(Number(config.contrast ?? 1) - 1) > .001) add('Contrast', Number(config.contrast));
        if (Math.abs(Number(config.saturation ?? 1) - 1) > .001) add('Saturation', Number(config.saturation));
        if (Math.abs(Number(config.hueRotation || 0)) > .001) add('HueRotation', Number(config.hueRotation));
        if (Number(config.blur || 0) > .001) add('Blur', Number(config.blur));
        if (config.chromaKeyEnabled) add('ChromaKey', Number(config.chromaSimilarity || .35), {
            secondaryAmount: Number(config.chromaSmoothness || .12),
            tertiaryAmount: Number(config.chromaSpill || .3),
            residualOpacity: Number(config.chromaResidualOpacity ?? 0),
            color: config.chromaKeyColor || '#00ff00'
        });
        return [{
            id: 'legacy-live-layer',
            name: 'Live input filters',
            visible: true,
            opacity: 1,
            blendMode: 'Normal',
            region: { points: [], inverted: false },
            filters
        }];
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:legacyVideoLayers@266', __javascriptError); throw __javascriptError; }}

    function installVideoEffects(video, canvas, config) { try {
        if (!video || !canvas || !window.publisherVideoEffects) {
            canvas?.classList.remove('active');
            return null;
        }
        const layers = Array.isArray(config.videoLayers) && config.videoLayers.length
            ? config.videoLayers
            : legacyVideoLayers(config);
        const key = `live-source-${String(config.id || canvas.id || video.id)}`;
        window.publisherVideoEffects.install(key, video, canvas, {
            layers,
            fitMode: config.fitMode || 'cover',
            forceCanvas: layers.some(layer => { try { return (Array.isArray(layer?.filters) && layer.filters.length > 0); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:layers.some@303', __javascriptError); throw __javascriptError; } })
        });
        return () => { try { return (window.publisherVideoEffects?.dispose(key)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:ArrowFunction@305', __javascriptError); throw __javascriptError; } };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:installVideoEffects@291', __javascriptError); throw __javascriptError; }}



    function installNowPlaying(config) { try {
        const root = document.getElementById(config.metadataId);
        if (!root || !config.nowPlayingDirectory) return null;
        const title = root.querySelector("[data-now-playing-title]");
        const artist = root.querySelector("[data-now-playing-artist]");
        const album = root.querySelector("[data-now-playing-album]");
        const cover = root.querySelector("[data-now-playing-cover]");
        let stopped = false;
        let lastIdentity = "";
        const refresh = async () => { try {
            if (stopped) return;
            try {
                const url = `${runtimeHttpBase(config.mediaHostUrl)}/api/mediahost/now-playing?directory=${encodeURIComponent(config.nowPlayingDirectory)}`;
                const response = await fetch(url, { cache: "no-store" });
                if (response.status === 204) return;
                if (!response.ok) throw new Error(`Now Playing returned ${response.status}`);
                const value = await response.json();
                if (title) title.textContent = value.title || value.fileName || "Unknown track";
                if (artist) artist.textContent = value.artist || "";
                if (album) album.textContent = [value.album, value.year].filter(Boolean).join(" · ") || value.fileName || "";
                if (cover) {
                    if (value.coverImage) { cover.src = value.coverImage; cover.hidden = false; }
                    else { cover.removeAttribute("src"); cover.hidden = true; }
                }
                const identity = `${value.fullPath || value.fileName || ""}|${value.lastWriteUtc || ""}`;
                window.PublisherStudioNowPlaying = { ...value, sourceId: String(config.id) };
                if (identity !== lastIdentity) {
                    lastIdentity = identity;
                    window.dispatchEvent(new CustomEvent("publisherstudio:now-playing-changed", { detail: window.PublisherStudioNowPlaying }));
                }
            } catch (error) {
                if (album) album.textContent = error?.message || "Streaming runtime unavailable";
            }
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:refresh@319', __javascriptError); throw __javascriptError; }};
        refresh();
        const timer = window.setInterval(refresh, 1500);
        return () => { try { stopped = true; window.clearInterval(timer);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:ArrowFunction@346', __javascriptError); throw __javascriptError; }};
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:installNowPlaying@310', __javascriptError); throw __javascriptError; }}

    async function attachSource(config) { try {
        detachSource(config.id);
        const video = document.getElementById(config.videoId);
        const canvas = document.getElementById(config.canvasId);
        const meter = document.getElementById(config.meterId);
        let stream = null;
        try {
            if (String(config.kind || "").toLowerCase() === "nowplaying") {
                const stopMetadata = installNowPlaying(config);
                if (!stopMetadata) return false;
                sources.set(String(config.id), { stopMetadata, config });
                return true;
            }
            stream = await acquire(config);
            if (video) {
                video.style.objectFit = config.fitMode || "cover";
                video.muted = config.muted !== false;
                video.volume = Math.max(0, Math.min(1, Number(config.volume ?? 1)));
                if (stream) video.srcObject = stream;
                else if (String(config.kind).toLowerCase() === "networkmedia" && config.networkUrl) video.src = config.networkUrl;
                await video.play().catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/streamingInterop.js:promise-catch@369', __promiseError);  return (undefined); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:video.play().catch@369', __javascriptError); throw __javascriptError; } });
            }
            const stopVideoEffects = installVideoEffects(video, canvas, config);
            const stopMeter = installMeter(stream, meter);
            const tracks = stream ? stream.getTracks() : [];
            const ended = () => { try { return (detachSource(config.id)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:ended@374', __javascriptError); throw __javascriptError; } };
            tracks.forEach(track => { try { return (track.addEventListener("ended", ended, { once: true })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:tracks.forEach@375', __javascriptError); throw __javascriptError; } });
            const state = { stream, video, stopVideoEffects, stopMeter, ended, config };
            sources.set(String(config.id), state);
            connectProgramAudio(state);
            return !!stream || !!(video && video.src);
        } catch (error) {
            console.error("PublisherStudio live source failed", error);
            window.dispatchEvent(new CustomEvent("publisherstudio:stream-error", { detail: { id: config.id, message: error?.message || String(error) } }));
            return false;
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:attachSource@349', __javascriptError); throw __javascriptError; }}

    function updateSourceEffects(config) { try {
        const state = sources.get(String(config?.id || ''));
        if (!state) return false;
        const video = state.video || document.getElementById(config.videoId);
        const canvas = document.getElementById(config.canvasId);
        state.stopVideoEffects?.();
        if (video) video.style.objectFit = config.fitMode || state.config?.fitMode || 'cover';
        state.stopVideoEffects = installVideoEffects(video, canvas, { ...state.config, ...config });
        state.config = { ...state.config, ...config };
        return true;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:updateSourceEffects@387', __javascriptError); throw __javascriptError; }}

    function detachSource(id) { try {
        const state = sources.get(String(id));
        if (!state) return;
        disconnectProgramAudio(state);
        state.stopVideoEffects?.();
        state.stopMeter?.();
        state.stopMetadata?.();
        try { state.stream?.__publisherNativeCleanup?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@406', __caughtJavaScriptError);  }
        state.stream?.getTracks?.().forEach(track => { try { return (track.stop()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:state.stream?.getTracks?.().forEach@407', __javascriptError); throw __javascriptError; } });
        if (state.video) { try { state.video.pause(); state.video.srcObject = null; state.video.removeAttribute("src"); state.video.load(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@408', __caughtJavaScriptError);  } }
        sources.delete(String(id));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:detachSource@399', __javascriptError); throw __javascriptError; }}

    async function enumerateDevices() { try {
        if (!navigator.mediaDevices?.enumerateDevices) return [];
        try {
            const permission = await navigator.mediaDevices.getUserMedia({ audio: true, video: true });
            permission.getTracks().forEach(track => { try { return (track.stop()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:permission.getTracks().forEach@416', __javascriptError); throw __javascriptError; } });
        } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@417', __caughtJavaScriptError);  }
        const devices = await navigator.mediaDevices.enumerateDevices();
        return devices.map(device => { try { return (({ deviceId: device.deviceId, kind: device.kind, label: device.label || "Permission required" })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:devices.map@419', __javascriptError); throw __javascriptError; } });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:enumerateDevices@412', __javascriptError); throw __javascriptError; }}

    async function chooseDirectory() { try {
        if (!window.showDirectoryPicker) return null;
        try { const handle = await window.showDirectoryPicker({ mode: "readwrite" }); return handle?.name || null; }
        catch { return null; }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:chooseDirectory@422', __javascriptError); throw __javascriptError; }}



    function supportedProgramMimeType() { try {
        if (typeof MediaRecorder === "undefined") return "";
        return [
            "video/webm;codecs=vp9,opus",
            "video/webm;codecs=vp8,opus",
            "video/webm"
        ].find(value => { try { return (MediaRecorder.isTypeSupported(value)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:[ "video/webm;codecs=vp9,opus", "video/webm;codecs=vp8,opus", "video/w@436', __javascriptError); throw __javascriptError; } }) || "";
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:supportedProgramMimeType@430', __javascriptError); throw __javascriptError; }}

    function ensureStreamingCaptureStyles() { try {
        if (document.getElementById("publisherstream-capture-style")) return;
        const style = document.createElement("style");
        style.id = "publisherstream-capture-style";
        style.textContent = `html.publisherstream-base-capture [data-publisher-stream-chat-layer]{visibility:hidden!important}`;
        document.head.append(style);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:ensureStreamingCaptureStyles@439', __javascriptError); throw __javascriptError; }}

    function markStreamingChatLayers() { try {
        ensureStreamingCaptureStyles();
        for (const host of document.querySelectorAll?.("[data-ps-component-runtime].ps-dx-chat") || []) {
            const owner = host.closest?.("[data-publication-element]") || host;
            owner.setAttribute("data-publisher-stream-chat-layer", "");
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:markStreamingChatLayers@447', __javascriptError); throw __javascriptError; }}

    function wrapCanvasText(context, text, maxWidth) { try {
        const source = String(text || "").replace(/[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F]/g, "").trim();
        if (!source) return [""];
        const tokens = source.split(/\s+/).filter(Boolean);
        const words = [];
        for (const token of tokens) {
            if (context.measureText(token).width <= maxWidth) { words.push(token); continue; }
            let part = "";
            for (const character of token) {
                const next = part + character;
                if (part && context.measureText(next).width > maxWidth) { words.push(part); part = character; }
                else part = next;
            }
            if (part) words.push(part);
        }
        if (!words.length) return [""];
        const lines = [];
        let line = words.shift();
        for (const word of words) {
            const next = `${line} ${word}`;
            if (context.measureText(next).width <= maxWidth) line = next;
            else { lines.push(line); line = word; }
        }
        lines.push(line);
        return lines;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:wrapCanvasText@455', __javascriptError); throw __javascriptError; }}

    function roundedRect(context, x, y, width, height, radius) { try {
        const r = Math.max(0, Math.min(radius, width / 2, height / 2));
        context.beginPath();
        context.moveTo(x + r, y);
        context.arcTo(x + width, y, x + width, y + height, r);
        context.arcTo(x + width, y + height, x, y + height, r);
        context.arcTo(x, y + height, x, y, r);
        context.arcTo(x, y, x + width, y, r);
        context.closePath();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:roundedRect@482', __javascriptError); throw __javascriptError; }}

    function drawBroadcastChatLayer(context, layer, outputWidth, outputHeight) { try {
        const pageWidth = Math.max(1, Number(layer.pageWidth || outputWidth));
        const pageHeight = Math.max(1, Number(layer.pageHeight || outputHeight));
        const sx = outputWidth / pageWidth;
        const sy = outputHeight / pageHeight;
        const x = Number(layer.x || 0) * sx;
        const y = Number(layer.y || 0) * sy;
        const width = Math.max(8, Number(layer.width || 0) * sx);
        const height = Math.max(8, Number(layer.height || 0) * sy);
        if (x >= outputWidth || y >= outputHeight || x + width <= 0 || y + height <= 0) return;

        const fontScale = Math.max(.65, Math.min(2.5, (sx + sy) / 2));
        const baseFontSize = Math.max(10, Number(layer.fontSize || 16) * fontScale);
        const compact = layer.compact === true;
        const padding = Math.max(6, baseFontSize * (compact ? .42 : .62));
        const radius = Math.max(4, Number(layer.borderRadius || 8) * fontScale);
        const backgroundOpacity = Math.max(0, Math.min(1, Number(layer.backgroundOpacity ?? .88)));
        const messageOpacity = Math.max(0, Math.min(1, Number(layer.messageOpacity ?? .78)));
        const showHeader = layer.showPlatformBadge !== false;
        const showAvatar = layer.showAvatar !== false;
        const showTimestamp = layer.showTimestamp !== false;
        const maximum = Math.max(1, Math.min(100, Number(layer.maxVisibleMessages || 12)));
        const items = (Array.isArray(layer.items) ? layer.items : []).slice(-maximum);

        context.save();
        roundedRect(context, x, y, width, height, radius);
        context.clip();
        context.globalAlpha = backgroundOpacity;
        context.fillStyle = layer.background && layer.background !== "rgba(0, 0, 0, 0)" ? layer.background : "rgb(8,15,28)";
        context.fillRect(x, y, width, height);
        context.globalAlpha = 1;
        context.textBaseline = "top";

        let contentTop = y + padding;
        if (showHeader) {
            context.fillStyle = "rgba(255,255,255,.055)";
            context.fillRect(x, y, width, Math.max(baseFontSize * 2, padding * 2 + baseFontSize));
            context.beginPath();
            context.arc(x + padding + baseFontSize * .24, y + padding + baseFontSize * .48, Math.max(3, baseFontSize * .22), 0, Math.PI * 2);
            context.fillStyle = "#f43f5e";
            context.fill();
            context.font = `700 ${Math.max(10, baseFontSize * .82)}px ${layer.fontFamily || "system-ui"}`;
            context.fillStyle = layer.color || "#f8fafc";
            const heading = `${layer.platform || "Chat"}${layer.channel ? ` · ${layer.channel}` : ""}`;
            context.fillText(heading, x + padding + baseFontSize * .65, y + padding, Math.max(0, width - padding * 2 - baseFontSize * .65));
            contentTop = y + Math.max(baseFontSize * 2, padding * 2 + baseFontSize);
        }

        const messageFont = Math.max(9, baseFontSize * (compact ? .72 : .82));
        const authorFont = Math.max(9, messageFont * .92);
        const lineHeight = messageFont * 1.3;
        const avatarSize = showAvatar ? Math.max(20, baseFontSize * (compact ? 1.45 : 1.75)) : 0;
        let cursor = y + height - padding;
        for (let index = items.length - 1; index >= 0; index--) {
            const item = items[index] || {};
            const age = items.length - index - 1;
            const messageLeft = x + padding + (showAvatar ? avatarSize + padding * .65 : 0);
            const messageWidth = Math.max(20, width - padding * 2 - (showAvatar ? avatarSize + padding * .65 : 0));
            context.font = `${messageFont}px ${layer.fontFamily || "system-ui"}`;
            const lines = wrapCanvasText(context, String(item.text || "").slice(0, 1600), messageWidth);
            const metaHeight = authorFont * 1.25;
            const blockHeight = Math.max(avatarSize, metaHeight + lines.length * lineHeight) + padding * (compact ? .6 : .85);
            cursor -= blockHeight;
            if (cursor < contentTop + padding * .4) break;

            const fade = layer.fadeOlder === false ? 1 : Math.max(.42, 1 - age * .055);
            context.globalAlpha = fade * messageOpacity;
            context.fillStyle = "rgb(30,41,59)";
            roundedRect(context, x + padding * .45, cursor, width - padding * .9, blockHeight - padding * .18, radius * .72);
            context.fill();
            context.globalAlpha = fade;

            if (showAvatar) {
                const centerX = x + padding + avatarSize / 2;
                const centerY = cursor + padding * .32 + avatarSize / 2;
                context.beginPath();
                context.arc(centerX, centerY, avatarSize / 2, 0, Math.PI * 2);
                context.fillStyle = item.authorColor || "#4f46e5";
                context.fill();
                const parts = String(item.authorName || "Viewer").trim().split(/\s+/).filter(Boolean);
                const initials = `${parts[0]?.[0] || "?"}${parts.length > 1 ? parts[parts.length - 1]?.[0] || "" : ""}`.toUpperCase();
                context.font = `800 ${Math.max(8, avatarSize * .34)}px ${layer.fontFamily || "system-ui"}`;
                context.fillStyle = "#f8fafc";
                context.textAlign = "center";
                context.textBaseline = "middle";
                context.fillText(initials, centerX, centerY, avatarSize * .8);
                context.textAlign = "left";
                context.textBaseline = "top";
            }

            context.font = `700 ${authorFont}px ${layer.fontFamily || "system-ui"}`;
            context.fillStyle = item.authorColor || layer.color || "#f8fafc";
            context.fillText(String(item.authorName || "Viewer").slice(0, 80), messageLeft, cursor + padding * .28, messageWidth);
            if (showTimestamp && item.timestamp) {
                let timestamp = "";
                try { timestamp = new Date(item.timestamp).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@588', __caughtJavaScriptError);  }
                if (timestamp) {
                    context.font = `${Math.max(8, authorFont * .82)}px ${layer.fontFamily || "system-ui"}`;
                    context.fillStyle = "#94a3b8";
                    const measured = context.measureText(timestamp).width;
                    context.fillText(timestamp, messageLeft + Math.max(0, messageWidth - measured), cursor + padding * .3, measured);
                }
            }
            context.font = `${messageFont}px ${layer.fontFamily || "system-ui"}`;
            context.fillStyle = layer.color || "#e2e8f0";
            let lineY = cursor + padding * .3 + metaHeight;
            for (const line of lines) {
                context.fillText(line, messageLeft, lineY, messageWidth);
                lineY += lineHeight;
            }
            cursor -= padding * .22;
        }
        context.restore();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:drawBroadcastChatLayer@493', __javascriptError); throw __javascriptError; }}

    function broadcastChatLayers(output, pageElementId) { try {
        try {
            return window.PublisherStudioChatRuntime?.getBroadcastLayers?.({
                mode: "broadcast",
                platform: output.platform || "Preview",
                channel: output.channel || "",
                outputId: output.outputId || ""
            }, pageElementId) || [];
        } catch (error) {
            console.warn("PublisherStudio could not build the broadcast Chat layer", error);
            return [];
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:broadcastChatLayers@608', __javascriptError); throw __javascriptError; }}

    function createCaptureVariant(output, audioTracks) { try {
        const width = Math.max(320, Math.min(7680, Number(output.width || 1920)));
        const height = Math.max(180, Math.min(4320, Number(output.height || 1080)));
        const frameRate = Math.max(15, Math.min(120, Number(output.frameRate || 60)));
        const canvas = document.createElement("canvas");
        canvas.width = width;
        canvas.height = height;
        const context = canvas.getContext("2d", { alpha: false, desynchronized: true });
        if (!context) throw new Error("The browser did not provide an output compositor canvas.");
        const stream = canvas.captureStream(frameRate);
        for (const track of audioTracks || []) stream.addTrack(track);
        return { ...output, width, height, frameRate, canvas, context, stream, recorder: null, socket: null };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:createCaptureVariant@622', __javascriptError); throw __javascriptError; }}

    function drawProgramFrame(state) { try {
        const { video, baseCanvas, baseContext, width, height, pageElementId } = state;
        const page = document.getElementById(String(pageElementId || "publisher-page"));
        const rect = page?.getBoundingClientRect?.();
        const viewWidth = Math.max(1, document.documentElement.clientWidth || window.innerWidth);
        const viewHeight = Math.max(1, document.documentElement.clientHeight || window.innerHeight);
        const scaleX = Math.max(.0001, (video.videoWidth || viewWidth) / viewWidth);
        const scaleY = Math.max(.0001, (video.videoHeight || viewHeight) / viewHeight);
        baseContext.fillStyle = "#000";
        baseContext.fillRect(0, 0, width, height);
        if (rect && rect.width > 1 && rect.height > 1 && video.readyState >= 2) {
            const sx = Math.max(0, rect.left * scaleX);
            const sy = Math.max(0, rect.top * scaleY);
            const sw = Math.min(video.videoWidth - sx, rect.width * scaleX);
            const sh = Math.min(video.videoHeight - sy, rect.height * scaleY);
            if (sw > 1 && sh > 1) baseContext.drawImage(video, sx, sy, sw, sh, 0, 0, width, height);
        }
        for (const variant of state.variants.values()) {
            const context = variant.context;
            context.fillStyle = "#000";
            context.fillRect(0, 0, variant.width, variant.height);
            context.drawImage(baseCanvas, 0, 0, width, height, 0, 0, variant.width, variant.height);
            for (const layer of broadcastChatLayers(variant, pageElementId))
                drawBroadcastChatLayer(context, layer, variant.width, variant.height);
        }
        state.drawFrame = requestAnimationFrame(() => { try { return (drawProgramFrame(state)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:requestAnimationFrame@661', __javascriptError); throw __javascriptError; } });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:drawProgramFrame@636', __javascriptError); throw __javascriptError; }}

    async function prepareProgramCapture(config = {}) { try {
        await stopProgramIngest();
        if (!navigator.mediaDevices?.getDisplayMedia || typeof MediaRecorder === "undefined")
            throw new Error("This browser cannot capture and encode the Publisher program output.");
        const frameRate = Math.max(15, Math.min(120, Number(config.frameRate || 60)));
        const width = Math.max(320, Math.min(7680, Number(config.width || 1920)));
        const height = Math.max(180, Math.min(4320, Number(config.height || 1080)));
        markStreamingChatLayers();
        document.documentElement.classList.add("publisherstream-base-capture");
        const sourceStream = await navigator.mediaDevices.getDisplayMedia({
            video: { frameRate, width: { ideal: width }, height: { ideal: height }, displaySurface: "browser" },
            audio: true,
            preferCurrentTab: true,
            selfBrowserSurface: "include",
            surfaceSwitching: "include",
            systemAudio: "include"
        });
        const video = document.createElement("video");
        video.muted = true;
        video.playsInline = true;
        video.srcObject = sourceStream;
        await video.play();
        const baseCanvas = document.createElement("canvas");
        baseCanvas.width = width;
        baseCanvas.height = height;
        const baseContext = baseCanvas.getContext("2d", { alpha: false, desynchronized: true });
        if (!baseContext) throw new Error("The browser did not provide a streaming compositor canvas.");
        const baseStream = baseCanvas.captureStream(frameRate);
        let audioContext = null;
        let audioDestination = null;
        let captureAudio = null;
        if (window.AudioContext) {
            audioContext = new AudioContext({ latencyHint: "interactive", sampleRate: 48000 });
            await audioContext.resume().catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/streamingInterop.js:promise-catch@697', __promiseError);  return (undefined); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:audioContext.resume().catch@697', __javascriptError); throw __javascriptError; } });
            audioDestination = audioContext.createMediaStreamDestination();
            const displayAudioTracks = sourceStream.getAudioTracks();
            if (displayAudioTracks.length) {
                captureAudio = audioContext.createMediaStreamSource(new MediaStream(displayAudioTracks));
                captureAudio.connect(audioDestination);
            }
            audioDestination.stream.getAudioTracks().forEach(track => { try { return (baseStream.addTrack(track)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:audioDestination.stream.getAudioTracks().forEach@704', __javascriptError); throw __javascriptError; } });
        } else {
            sourceStream.getAudioTracks().forEach(track => { try { return (baseStream.addTrack(track)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:sourceStream.getAudioTracks().forEach@706', __javascriptError); throw __javascriptError; } });
        }
        programCapture = {
            sourceStream,
            video,
            baseCanvas,
            baseContext,
            canvas: baseCanvas,
            canvasStream: baseStream,
            width,
            height,
            frameRate,
            pageElementId: config.pageElementId || "publisher-page",
            drawFrame: 0,
            recorders: [],
            sockets: [],
            variants: new Map(),
            peers: new Map(),
            signalSocket: null,
            audioContext,
            audioDestination,
            captureAudio
        };
        drawProgramFrame(programCapture);
        connectAllProgramAudio();
        const ended = () => { try { void stopProgramIngest();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:ended@731', __javascriptError); throw __javascriptError; }};
        sourceStream.getVideoTracks()[0]?.addEventListener("ended", ended, { once: true });
        programCapture.ended = ended;
        window.dispatchEvent(new CustomEvent("publisherstudio:program-capture-ready", { detail: { width, height, frameRate } }));
        return true;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:prepareProgramCapture@664', __javascriptError); throw __javascriptError; }}

    async function openIngestSocket(config, outputId = "") { try {
        const baseUrl = runtimeWsBase(config.mediaHostUrl);
        if (!config.sessionId) throw new Error("The integrated streaming session is not available.");
        const query = outputId ? `?outputId=${encodeURIComponent(outputId)}` : "";
        const socket = new WebSocket(`${baseUrl}/api/mediahost/sessions/${encodeURIComponent(config.sessionId)}/ingest/websocket${query}`);
        socket.binaryType = "arraybuffer";
        await new Promise((resolve, reject) => { try {
            const timeout = setTimeout(() => { try { return (reject(new Error("PublisherStudio did not accept the browser ingest."))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:setTimeout@745', __javascriptError); throw __javascriptError; } }, 5000);
            socket.addEventListener("open", () => { try { clearTimeout(timeout); resolve();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:socket.addEventListener@746', __javascriptError); throw __javascriptError; }}, { once: true });
            socket.addEventListener("error", () => { try { clearTimeout(timeout); reject(new Error("The browser could not connect to PublisherStudio's ingest socket."));  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:socket.addEventListener@747', __javascriptError); throw __javascriptError; }}, { once: true });
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:ArrowFunction@744', __javascriptError); throw __javascriptError; }});
        return socket;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:openIngestSocket@738', __javascriptError); throw __javascriptError; }}

    async function startVariantRecorder(config, variant, outputId = "") { try {
        const mimeType = supportedProgramMimeType();
        if (!mimeType) throw new Error("The browser has no supported WebM MediaRecorder profile.");
        const socket = await openIngestSocket(config, outputId);
        const configuredBitrate = Number(variant.videoBitsPerSecond || config.videoBitsPerSecond || 16_000_000);
        const recorder = new MediaRecorder(variant.stream, {
            mimeType,
            videoBitsPerSecond: Math.max(1_000_000, configuredBitrate),
            audioBitsPerSecond: Math.max(64_000, Number(config.audioBitsPerSecond || 192_000))
        });
        socket.send(JSON.stringify({
            kind: "webm-websocket",
            url: "pipe:0",
            codec: mimeType,
            width: variant.width,
            height: variant.height,
            frameRate: variant.frameRate,
            outputId: outputId || null
        }));
        const pendingWrites = new Set();
        recorder.__publisherPendingWrites = pendingWrites;
        recorder.addEventListener("dataavailable", event => { try {
            if (!event.data?.size || socket.readyState !== WebSocket.OPEN) return;
            const write = (async () => {
                try {
                    const buffer = await event.data.arrayBuffer();
                    if (socket.readyState === WebSocket.OPEN) socket.send(buffer);
                } catch (error) {
                    console.error("PublisherStudio could not send a program frame chunk", error);
                }
            })();
            pendingWrites.add(write);
            write.finally(() => { try { return (pendingWrites.delete(write)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:write.finally@784', __javascriptError); throw __javascriptError; } });
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:recorder.addEventListener@773', __javascriptError); throw __javascriptError; }});
        recorder.addEventListener("error", event => { try {
            window.dispatchEvent(new CustomEvent("publisherstudio:stream-error", { detail: { outputId, message: event.error?.message || "Program encoding failed." } }));
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:recorder.addEventListener@786', __javascriptError); throw __javascriptError; }});
        socket.addEventListener("close", () => { try {
            if (recorder.state !== "inactive") try { recorder.stop(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@790', __caughtJavaScriptError);  }
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:socket.addEventListener@789', __javascriptError); throw __javascriptError; }});
        recorder.start(250);
        programCapture.recorders.push(recorder);
        programCapture.sockets.push(socket);
        variant.recorder = recorder;
        variant.socket = socket;
        return { mimeType, width: variant.width, height: variant.height, frameRate: variant.frameRate };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:startVariantRecorder@752', __javascriptError); throw __javascriptError; }}

    async function startPublisherWebRtc(config, stream) { try {
        if (!config.enableWebRtc || typeof RTCPeerConnection === "undefined") return;
        const baseUrl = runtimeWsBase(config.mediaHostUrl);
        const socket = new WebSocket(`${baseUrl}/api/mediahost/sessions/${encodeURIComponent(config.sessionId)}/webrtc/publisher`);
        programCapture.signalSocket = socket;
        const send = value => { try { if (socket.readyState === WebSocket.OPEN) socket.send(JSON.stringify(value));  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:send@805', __javascriptError); throw __javascriptError; }};
        const closePeer = viewerId => { try {
            const peer = programCapture?.peers?.get(viewerId);
            if (!peer) return;
            try { peer.close(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@809', __caughtJavaScriptError);  }
            programCapture.peers.delete(viewerId);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:closePeer@806', __javascriptError); throw __javascriptError; }};
        socket.addEventListener("message", async event => { try {
            let message;
            try { message = JSON.parse(event.data); } catch { return; }
            const viewerId = String(message.viewerId || "");
            if (!viewerId) return;
            if (message.type === "viewer-left") { closePeer(viewerId); return; }
            let peer = programCapture?.peers?.get(viewerId);
            if (!peer) {
                peer = new RTCPeerConnection({ iceServers: [] });
                for (const track of stream.getTracks()) peer.addTrack(track, stream);
                peer.addEventListener("icecandidate", ice => { try {
                    if (ice.candidate) send({ type: "publisher-candidate", viewerId, candidate: ice.candidate });
                 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:peer.addEventListener@822', __javascriptError); throw __javascriptError; }});
                peer.addEventListener("connectionstatechange", () => { try {
                    if (["failed", "closed", "disconnected"].includes(peer.connectionState)) closePeer(viewerId);
                 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:peer.addEventListener@825', __javascriptError); throw __javascriptError; }});
                programCapture.peers.set(viewerId, peer);
            }
            try {
                if (message.type === "viewer-offer") {
                    await peer.setRemoteDescription({ type: "offer", sdp: message.sdp });
                    const answer = await peer.createAnswer();
                    await peer.setLocalDescription(answer);
                    send({ type: "publisher-answer", viewerId, sdp: answer.sdp });
                } else if (message.type === "viewer-candidate" && message.candidate) {
                    await peer.addIceCandidate(message.candidate);
                }
            } catch (error) {
                send({ type: "publisher-error", viewerId, message: error?.message || String(error) });
                closePeer(viewerId);
            }
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:socket.addEventListener@812', __javascriptError); throw __javascriptError; }});
        socket.addEventListener("close", () => { try {
            for (const viewerId of [...(programCapture?.peers?.keys?.() || [])]) closePeer(viewerId);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:socket.addEventListener@844', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:startPublisherWebRtc@800', __javascriptError); throw __javascriptError; }}

    async function startProgramIngest(config = {}) { try {
        if (!programCapture) throw new Error("Prepare the program capture before starting a streaming session.");
        const audioTracks = programCapture.canvasStream.getAudioTracks();
        const master = {
            outputId: "",
            platform: "CleanMaster",
            channel: "",
            width: programCapture.width,
            height: programCapture.height,
            frameRate: programCapture.frameRate,
            stream: programCapture.canvasStream,
            videoBitsPerSecond: Number(config.videoBitsPerSecond || 16_000_000)
        };
        const results = [await startVariantRecorder(config, master, "")];
        for (const output of Array.isArray(config.outputs) ? config.outputs.filter(item => { try { return (item?.outputId && item.captureRequired !== false); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:config.outputs.filter@863', __javascriptError); throw __javascriptError; } }) : []) {
            const variant = createCaptureVariant({
                ...output,
                outputId: String(output.outputId || ""),
                platform: String(output.platform || output.provider || "Preview"),
                channel: String(output.channel || ""),
                videoBitsPerSecond: Math.max(
                    Number(output.videoBitsPerSecond || 0),
                    Number(output.videoBitrateKbps || 0) * 2000,
                    2_000_000)
            }, audioTracks);
            programCapture.variants.set(variant.outputId, variant);
            results.push(await startVariantRecorder(config, variant, variant.outputId));
        }
        await startPublisherWebRtc(config, programCapture.canvasStream);
        return { master: results[0], outputs: results.slice(1) };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:startProgramIngest@849', __javascriptError); throw __javascriptError; }}

    async function stopProgramIngest() { try {
        const state = programCapture;
        programCapture = null;
        document.documentElement.classList.remove("publisherstream-base-capture");
        if (!state) return true;

        try { cancelAnimationFrame(state.drawFrame); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@887', __caughtJavaScriptError);  }
        const recorders = [...(state.recorders || [])];
        const waitForStop = recorder => { try { return (new Promise(resolve => { try {
            if (!recorder || recorder.state === "inactive") { resolve(); return; }
            let settled = false;
            let timeout = 0;
            const finish = () => { try {
                if (settled) return;
                settled = true;
                if (timeout) window.clearTimeout(timeout);
                resolve();
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:finish@893', __javascriptError); throw __javascriptError; }};
            recorder.addEventListener("stop", finish, { once: true });
            timeout = window.setTimeout(finish, 3000);
            try { recorder.requestData(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@901', __caughtJavaScriptError);  }
            try { recorder.stop(); } catch { finish(); }
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:ArrowFunction@889', __javascriptError); throw __javascriptError; }})); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:waitForStop@889', __javascriptError); throw __javascriptError; } };
        await Promise.all(recorders.map(waitForStop));
        await Promise.allSettled(recorders.flatMap(recorder => { try { return ([...(recorder?.__publisherPendingWrites || [])]); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:recorders.flatMap@905', __javascriptError); throw __javascriptError; } }));

        for (const socket of state.sockets || []) {
            try { if (socket.readyState < WebSocket.CLOSING) socket.close(1000, "PublisherStudio session stopped"); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@908', __caughtJavaScriptError);  }
        }
        try { if (state.signalSocket?.readyState < WebSocket.CLOSING) state.signalSocket.close(1000, "PublisherStudio session stopped"); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@910', __caughtJavaScriptError);  }
        for (const peer of state.peers?.values?.() || []) { try { peer.close(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@911', __caughtJavaScriptError);  } }
        for (const sourceState of sources.values()) disconnectProgramAudio(sourceState);
        try { state.captureAudio?.disconnect?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@913', __caughtJavaScriptError);  }
        try { state.audioDestination?.disconnect?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@914', __caughtJavaScriptError);  }
        try { await state.audioContext?.close?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@915', __caughtJavaScriptError);  }
        state.canvasStream?.getTracks?.().forEach(track => { try { return (track.stop()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:state.canvasStream?.getTracks?.().forEach@916', __javascriptError); throw __javascriptError; } });
        for (const variant of state.variants?.values?.() || []) variant.stream?.getTracks?.().forEach(track => { try { return (track.stop()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:variant.stream?.getTracks?.().forEach@917', __javascriptError); throw __javascriptError; } });
        state.sourceStream?.getTracks?.().forEach(track => { try { return (track.stop()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:state.sourceStream?.getTracks?.().forEach@918', __javascriptError); throw __javascriptError; } });
        try { state.video.pause(); state.video.srcObject = null; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@919', __caughtJavaScriptError);  }
        window.dispatchEvent(new CustomEvent("publisherstudio:program-capture-stopped"));
        return true;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:stopProgramIngest@881', __javascriptError); throw __javascriptError; }}

    function chatKey(platform, channel) { try {
        return `${String(platform || "").trim().toLowerCase()}|${String(channel || "").trim().toLowerCase()}`;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:chatKey@924', __javascriptError); throw __javascriptError; }}

    function stopChatBridge() { try {
        const state = chatBridgeState;
        chatBridgeState = null;
        if (!state) return;
        for (const socket of state.sockets.values()) {
            try { if (socket.readyState < WebSocket.CLOSING) socket.close(1000, "PublisherStudio Chat stopped"); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@933', __caughtJavaScriptError);  }
        }
        state.sockets.clear();
        state.subscribers.clear();
        state.messages.clear();
        if (window.PublisherStudioChatBridge === state.bridge) delete window.PublisherStudioChatBridge;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:stopChatBridge@928', __javascriptError); throw __javascriptError; }}

    function configureChatBridge(config = {}) { try {
        stopChatBridge();
        const mediaHostUrl = runtimeHttpBase(config.mediaHostUrl);
        const sessionId = String(config.sessionId || "");
        if (!sessionId) return false;
        const outputs = (Array.isArray(config.outputs) ? config.outputs : [])
            .filter(item => { try { return (item?.outputId && item.captureRequired !== false && item.chatEnabled === true); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:(Array.isArray(config.outputs) ? config.outputs : []) .filter@947', __javascriptError); throw __javascriptError; } })
            .map(item => { try { return (({
                outputId: String(item.outputId),
                platform: String(item.platform || item.provider || "Preview"),
                channel: String(item.channel || "")
            })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:(Array.isArray(config.outputs) ? config.outputs : []) .filter(item => @948', __javascriptError); throw __javascriptError; } });
        const state = {
            mediaHostUrl,
            sessionId,
            outputs,
            sockets: new Map(),
            messages: new Map(),
            subscribers: new Set(),
            bridge: null
        };
        const notify = detail => { try {
            const key = chatKey(detail.platform, detail.channel);
            const bucket = state.messages.get(key) || [];
            if (!bucket.some(item => { try { return (String(item.id || "") === String(detail.message?.id || "")); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:bucket.some@965', __javascriptError); throw __javascriptError; } })) {
                bucket.push(detail.message);
                while (bucket.length > 200) bucket.shift();
                state.messages.set(key, bucket);
            }
            window.dispatchEvent(new CustomEvent("publisherstudio:chat-message", { detail }));
            for (const subscription of [...state.subscribers]) {
                if (subscription.platform && subscription.platform !== String(detail.platform || "").toLowerCase()) continue;
                if (subscription.channel && subscription.channel !== String(detail.channel || "").toLowerCase()) continue;
                try { subscription.receive(detail); } catch (error) { console.error("PublisherStudio Chat subscriber failed", error); }
            }
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:notify@962', __javascriptError); throw __javascriptError; }};
        const bridge = {
            subscribe(context, receive) { try {
                if (typeof receive !== "function") return () => { try { } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:ArrowFunction@979', __javascriptError); throw __javascriptError; }};
                const subscription = {
                    platform: String(context?.platform || "").toLowerCase(),
                    channel: String(context?.channel || "").toLowerCase(),
                    receive
                };
                state.subscribers.add(subscription);
                for (const message of state.messages.get(chatKey(context?.platform, context?.channel)) || []) {
                    try { receive({ platform: context?.platform || "", channel: context?.channel || "", message }); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@987', __caughtJavaScriptError);  }
                }
                return () => { try { return (state.subscribers.delete(subscription)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:ArrowFunction@989', __javascriptError); throw __javascriptError; } };
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:subscribe@978', __javascriptError); throw __javascriptError; }},
            async send(detail) { try {
                const message = String(detail?.message?.text || detail?.message || "").trim();
                if (!message) return false;
                const requestedOutputId = String(detail?.outputId || outputContext.outputId || "");
                const output = state.outputs.find(item => { try { return (item.outputId === requestedOutputId); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:state.outputs.find@995', __javascriptError); throw __javascriptError; } })
                    || state.outputs.find(item => { try { return (chatKey(item.platform, item.channel) === chatKey(detail?.platform, detail?.channel)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:state.outputs.find@996', __javascriptError); throw __javascriptError; } });
                if (!output) throw new Error("No configured provider Chat matches the selected operator Chat.");
                const response = await fetch(`${state.mediaHostUrl}/api/mediahost/sessions/${encodeURIComponent(state.sessionId)}/chat/${encodeURIComponent(output.outputId)}/send`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ message })
                });
                if (!response.ok) {
                    const error = await response.json().catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/streamingInterop.js:promise-catch@1004', __promiseError);  return (({})); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:response.json().catch@1004', __javascriptError); throw __javascriptError; } });
                    throw new Error(error.error || `Chat send failed (${response.status}).`);
                }
                return true;
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:send@991', __javascriptError); throw __javascriptError; }},
            getMessages(platform, channel) { try {
                return [...(state.messages.get(chatKey(platform, channel)) || [])];
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:getMessages@1009', __javascriptError); throw __javascriptError; }}
        };
        state.bridge = bridge;
        chatBridgeState = state;
        window.PublisherStudioChatBridge = bridge;

        const wsBase = runtimeWsBase(mediaHostUrl);
        for (const output of outputs) {
            const socket = new WebSocket(`${wsBase}/api/mediahost/sessions/${encodeURIComponent(sessionId)}/chat/${encodeURIComponent(output.outputId)}/websocket`);
            state.sockets.set(output.outputId, socket);
            socket.addEventListener("message", event => { try {
                let source;
                try { source = JSON.parse(event.data); } catch { return; }
                const timestamp = source.timestamp ? new Date(source.timestamp) : new Date();
                const message = {
                    id: String(source.id || `chat-${Date.now()}`),
                    text: String(source.text || ""),
                    timestamp: Number.isNaN(timestamp.getTime()) ? new Date() : timestamp,
                    author: {
                        id: String(source.authorId || "viewer"),
                        name: String(source.authorName || "Viewer"),
                        avatarUrl: String(source.authorAvatar || "") || undefined
                    },
                    platform: String(source.platform || output.platform),
                    channel: String(source.channel || output.channel),
                    color: String(source.color || ""),
                    badges: String(source.badges || "")
                };
                notify({ outputId: output.outputId, platform: message.platform, channel: message.channel, message });
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:socket.addEventListener@1021', __javascriptError); throw __javascriptError; }});
            socket.addEventListener("error", () => { try { return (window.dispatchEvent(new CustomEvent("publisherstudio:stream-error", {
                detail: { id: output.outputId, message: `${output.platform} Chat connection failed.` }
            }))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:socket.addEventListener@1041', __javascriptError); throw __javascriptError; } });
        }
        return true;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:configureChatBridge@941', __javascriptError); throw __javascriptError; }}

    function normalizeGesture(event) { try {
        const parts = [];
        if (event.ctrlKey) parts.push("Ctrl");
        if (event.altKey) parts.push("Alt");
        if (event.shiftKey) parts.push("Shift");
        if (event.metaKey) parts.push("Meta");
        let key = String(event.key || "");
        if (key === " ") key = "Space";
        if (key.length === 1) key = key.toUpperCase();
        const aliases = { Esc: "Escape", Del: "Delete", Left: "ArrowLeft", Right: "ArrowRight", Up: "ArrowUp", Down: "ArrowDown" };
        key = aliases[key] || key;
        if (!["Control", "Alt", "Shift", "Meta"].includes(key)) parts.push(key);
        return parts.join("+");
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:normalizeGesture@1048', __javascriptError); throw __javascriptError; }}

    function normalizeConfiguredGesture(value) { try {
        const aliases = { CTRL: "Ctrl", CONTROL: "Ctrl", ALT: "Alt", SHIFT: "Shift", META: "Meta", WIN: "Meta", CMD: "Meta", ESC: "Escape", DEL: "Delete", SPACEBAR: "Space" };
        return String(value || "")
            .split("+")
            .map(part => { try { return (part.trim()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:String(value || "") .split("+") .map@1067', __javascriptError); throw __javascriptError; } })
            .filter(Boolean)
            .map(part => { try { return (aliases[part.toUpperCase()] || (part.length === 1 ? part.toUpperCase() : part)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:String(value || "") .split("+") .map(part => part.trim()) .filter(Bool@1069', __javascriptError); throw __javascriptError; } })
            .join("+");
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:normalizeConfiguredGesture@1063', __javascriptError); throw __javascriptError; }}

    function isTypingTarget(target) { try {
        if (!(target instanceof Element)) return false;
        return !!target.closest("input, textarea, select, [contenteditable='true'], .dx-texteditor-input");
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:isTypingTarget@1073', __javascriptError); throw __javascriptError; }}

    function unbindHotkeys() { try {
        if (hotkeyListener) window.removeEventListener("keydown", hotkeyListener, true);
        hotkeyListener = null;
        hotkeyReference = null;
        configuredHotkeys = [];
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:unbindHotkeys@1078', __javascriptError); throw __javascriptError; }}

    function bindHotkeys(hotkeys, dotnetReference) { try {
        unbindHotkeys();
        configuredHotkeys = Array.isArray(hotkeys)
            ? hotkeys.filter(item => { try { return (item && item.gesture && item.command && !item.global); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:hotkeys.filter@1088', __javascriptError); throw __javascriptError; } }).map(item => { try { return (({ ...item, normalized: normalizeConfiguredGesture(item.gesture) })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:hotkeys.filter(item => item && item.gesture && item.command && !item.g@1088', __javascriptError); throw __javascriptError; } })
            : [];
        hotkeyReference = dotnetReference || null;
        if (!configuredHotkeys.length || !hotkeyReference) return;
        hotkeyListener = event => { try {
            if (event.repeat || event.isComposing) return;
            const gesture = normalizeGesture(event);
            const match = configuredHotkeys.find(item => { try { return (item.normalized === gesture); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:configuredHotkeys.find@1095', __javascriptError); throw __javascriptError; } });
            if (!match) return;
            if (isTypingTarget(event.target) && !/^F\d{1,2}$/.test(String(event.key || ""))) return;
            event.preventDefault();
            event.stopPropagation();
            hotkeyReference.invokeMethodAsync("HandleStreamingHotkey", String(match.command), match.targetId ? String(match.targetId) : null)
                .catch(error => { try { return (console.error("PublisherStudio streaming hotkey failed", error)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:hotkeyReference.invokeMethodAsync("HandleStreamingHotkey", String(matc@1101', __javascriptError); throw __javascriptError; } });
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:hotkeyListener@1092', __javascriptError); throw __javascriptError; }};
        window.addEventListener("keydown", hotkeyListener, true);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:bindHotkeys@1085', __javascriptError); throw __javascriptError; }}

    function activateSource(id) { try {
        const normalized = String(id || "").replace(/[^0-9a-f]/gi, "").toLowerCase();
        if (!normalized) return false;
        const button = document.getElementById(`live-source-activate-${normalized}`);
        if (!(button instanceof HTMLButtonElement)) return false;
        button.click();
        return true;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:activateSource@1106', __javascriptError); throw __javascriptError; }}

    function externalAuthorizationWindowName(name) { try {
        const normalized = String(name || "publisherstudio-oauth").replace(/[^a-z0-9_-]/gi, "-");
        return normalized || "publisherstudio-oauth";
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:externalAuthorizationWindowName@1115', __javascriptError); throw __javascriptError; }}

    function reserveExternalAuthorizationWindow(name) { try {
        const key = externalAuthorizationWindowName(name);
        try {
            const popup = window.open("", key, "popup=yes,width=760,height=820,resizable=yes,scrollbars=yes");
            if (!popup) return false;
            try {
                popup.document.title = "PublisherStudio authorization";
                if (popup.document.body) popup.document.body.textContent = "Waiting for the provider authorization page…";
            } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@1128', __caughtJavaScriptError);  }
            externalAuthorizationWindows.set(key, popup);
            return true;
        } catch {
            return false;
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:reserveExternalAuthorizationWindow@1120', __javascriptError); throw __javascriptError; }}

    function navigateExternalAuthorizationWindow(name, url) { try {
        const key = externalAuthorizationWindowName(name);
        const destination = String(url || "").trim();
        if (!destination) return false;
        let popup = externalAuthorizationWindows.get(key);
        try {
            if (!popup || popup.closed) popup = window.open(destination, key, "popup=yes,width=760,height=820,resizable=yes,scrollbars=yes");
            else popup.location.replace(destination);
            if (!popup) return false;
            try { popup.focus(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@1145', __caughtJavaScriptError);  }
            externalAuthorizationWindows.set(key, popup);
            return true;
        } catch {
            return false;
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:navigateExternalAuthorizationWindow@1136', __javascriptError); throw __javascriptError; }}

    function showExternalAuthorizationMessage(name, title, message) { try {
        const key = externalAuthorizationWindowName(name);
        let popup = externalAuthorizationWindows.get(key);
        try {
            if (!popup || popup.closed) popup = window.open("", key, "popup=yes,width=760,height=820,resizable=yes,scrollbars=yes");
            if (!popup) return false;
            try { popup.location.replace("about:blank"); }
            catch { try { popup.location.href = "about:blank"; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:suppressed-catch@1160', __caughtJavaScriptError);  } }
            let attempts = 0;
            const renderMessage = () => { try {
                try {
                    if (!popup.document?.body) throw new Error("The authorization window is still navigating.");
                    popup.document.title = String(title || "PublisherStudio authorization");
                    popup.document.body.replaceChildren();
                    popup.document.body.style.cssText = "margin:0;padding:40px;font:16px/1.5 system-ui,sans-serif;background:#f8fafc;color:#172554";
                    const heading = popup.document.createElement("h1");
                    heading.textContent = String(title || "PublisherStudio authorization");
                    heading.style.fontSize = "24px";
                    const paragraph = popup.document.createElement("p");
                    paragraph.textContent = String(message || "The authorization could not be completed.");
                    paragraph.style.whiteSpace = "pre-wrap";
                    const closeButton = popup.document.createElement("button");
                    closeButton.type = "button";
                    closeButton.textContent = "Close";
                    closeButton.style.cssText = "margin-top:16px;padding:9px 18px;border:0;border-radius:4px;background:#17365d;color:white;cursor:pointer";
                    closeButton.addEventListener("click", () => { try { return (popup.close()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:callback:closeButton.addEventListener@1178', __javascriptError); throw __javascriptError; } });
                    popup.document.body.append(heading, paragraph, closeButton);
                    popup.focus();
                } catch {
                    attempts++;
                    if (attempts < 20 && !popup.closed) window.setTimeout(renderMessage, 50);
                }
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:renderMessage@1162', __javascriptError); throw __javascriptError; }};
            window.setTimeout(renderMessage, 50);
            externalAuthorizationWindows.set(key, popup);
            return true;
        } catch {
            return false;
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:showExternalAuthorizationMessage@1153', __javascriptError); throw __javascriptError; }}

    function closeExternalAuthorizationWindow(name) { try {
        const key = externalAuthorizationWindowName(name);
        const popup = externalAuthorizationWindows.get(key);
        externalAuthorizationWindows.delete(key);
        if (!popup) return false;
        try {
            if (!popup.closed) popup.close();
            return true;
        } catch {
            return false;
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:closeExternalAuthorizationWindow@1194', __javascriptError); throw __javascriptError; }}

    function setOutputContext(context = {}) { try {
        Object.assign(outputContext, context);
        window.PublisherStudioOutputContext = { ...outputContext };
        window.PublisherStudioChatPlatform = outputContext.platform || "Preview";
        window.PublisherStudioChatChannel = outputContext.channel || "";
        window.dispatchEvent(new CustomEvent("publisherstudio:output-context-changed", { detail: { ...outputContext } }));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:setOutputContext@1207', __javascriptError); throw __javascriptError; }}

    window.publisherStreaming = {
        attachSource,
        updateSourceEffects,
        detachSource,
        activateSource,
        enumerateDevices,
        chooseDirectory,
        reserveExternalAuthorizationWindow,
        navigateExternalAuthorizationWindow,
        showExternalAuthorizationMessage,
        closeExternalAuthorizationWindow,
        setOutputContext,
        prepareProgramCapture,
        startProgramIngest,
        stopProgramIngest,
        configureChatBridge,
        stopChatBridge,
        bindHotkeys,
        unbindHotkeys,
        getOutputContext: () => { try { return (({ ...outputContext })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:getOutputContext@1234', __javascriptError); throw __javascriptError; } },
        stopAll: async () => { try { [...sources.keys()].forEach(detachSource); stopChatBridge(); await stopProgramIngest();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:stopAll@1235', __javascriptError); throw __javascriptError; }}
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/streamingInterop.js:ArrowFunction@2', __javascriptError); throw __javascriptError; }})();

// Guard exported browser namespaces after the file has initialized.
publisherStudioDiagnostics.guardObject("publisherStreaming", window.publisherStreaming);
