(() => {
    const sources = new Map();
    const outputContext = { mode: "operator", platform: "Preview", channel: "", outputId: "" };
    let hotkeyListener = null;
    let hotkeyReference = null;
    let configuredHotkeys = [];
    let programCapture = null;
    let chatBridgeState = null;

    function hexColor(value) {
        const match = /^#?([0-9a-f]{6})$/i.exec(String(value || ""));
        const hex = match ? match[1] : "00ff00";
        return [parseInt(hex.slice(0, 2), 16) / 255, parseInt(hex.slice(2, 4), 16) / 255, parseInt(hex.slice(4, 6), 16) / 255];
    }


    function disconnectProgramAudio(state) {
        if (!state?.programAudio) return;
        try { state.programAudio.source.disconnect(); } catch { }
        try { state.programAudio.delay.disconnect(); } catch { }
        try { state.programAudio.gain.disconnect(); } catch { }
        state.programAudio = null;
    }

    function connectProgramAudio(state) {
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
    }

    function connectAllProgramAudio() {
        for (const state of sources.values()) connectProgramAudio(state);
    }

    async function acquireNative(config) {
        const mediaHostUrl = String(config.mediaHostUrl || "").replace(/\/$/, "");
        if (!mediaHostUrl) throw new Error("The local Media Host address is missing.");
        let captureId = "";
        let target = null;
        let objectUrl = "";
        let socket = null;
        let stopped = false;
        const stopCapture = async () => {
            if (stopped) return;
            stopped = true;
            try { socket?.close(1000, "Native source detached"); } catch { }
            try {
                target?.pause?.();
                target?.removeAttribute?.("src");
                target?.load?.();
            } catch { }
            if (objectUrl) {
                try { URL.revokeObjectURL(objectUrl); } catch { }
                objectUrl = "";
            }
            if (captureId) {
                await fetch(`${mediaHostUrl}/api/mediahost/native-captures/${encodeURIComponent(captureId)}`, { method: "DELETE" }).catch(() => undefined);
                captureId = "";
            }
        };

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
            const result = await response.json().catch(() => ({}));
            if (!response.ok || !result.captureId) throw new Error(result.error || "The Media Host could not start native capture.");
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
            const pump = () => {
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
            };
            await new Promise((resolve, reject) => {
                const timeout = setTimeout(() => reject(new Error("The native capture media buffer did not open.")), 5000);
                mediaSource.addEventListener("sourceopen", () => {
                    clearTimeout(timeout);
                    try {
                        sourceBuffer = mediaSource.addSourceBuffer(mimeType);
                        try { sourceBuffer.mode = "sequence"; } catch { }
                        sourceBuffer.addEventListener("updateend", pump);
                        resolve();
                    } catch (error) { reject(error); }
                }, { once: true });
                objectUrl = URL.createObjectURL(mediaSource);
                target.src = objectUrl;
            });

            const wsBase = mediaHostUrl.replace(/^http/i, "ws");
            socket = new WebSocket(`${wsBase}/api/mediahost/native-captures/${encodeURIComponent(captureId)}/websocket`);
            socket.binaryType = "arraybuffer";
            socket.addEventListener("message", event => { queue.push(new Uint8Array(event.data)); pump(); });
            await new Promise((resolve, reject) => {
                const timeout = setTimeout(() => reject(new Error("The native capture stream did not connect.")), 5000);
                socket.addEventListener("open", () => { clearTimeout(timeout); resolve(); }, { once: true });
                socket.addEventListener("error", () => { clearTimeout(timeout); reject(new Error("The native capture WebSocket failed.")); }, { once: true });
            });
            await target.play().catch(() => undefined);
            const capture = target.captureStream?.() || target.mozCaptureStream?.();
            if (!capture) throw new Error("This browser cannot expose the native media element as a MediaStream.");
            capture.__publisherNativeCleanup = stopCapture;
            capture.__publisherNativeElement = target;
            return capture;
        } catch (error) {
            await stopCapture();
            throw error;
        }
    }

    function networkSourceNeedsNative(url) {
        const value = String(url || "").trim().toLowerCase();
        return /^(rtsp|rtsps|rtmp|rtmps|srt|udp|tcp):/.test(value);
    }

    async function acquire(config) {
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
                    selected.getVideoTracks().forEach(track => track.stop());
                    if (audioTracks.length) return new MediaStream(audioTracks);
                    selected.getTracks().forEach(track => track.stop());
                } catch (error) {
                    if (error?.name === "NotAllowedError" && backend === "browser") throw error;
                }
            }
            window.dispatchEvent(new CustomEvent("publisherstudio:native-source-request", { detail: config }));
            if (backend !== "browser") return acquireNative(config);
            throw new Error("This browser did not expose isolated application/system audio and the source is restricted to Browser capture.");
        }
        return null;
    }

    function installMeter(stream, meter) {
        if (!stream || !meter || !window.AudioContext) return null;
        const context = new AudioContext();
        const source = context.createMediaStreamSource(stream);
        const analyser = context.createAnalyser();
        analyser.fftSize = 512;
        source.connect(analyser);
        const values = new Uint8Array(analyser.frequencyBinCount);
        let frame = 0;
        const draw = () => {
            analyser.getByteTimeDomainData(values);
            let sum = 0;
            for (const value of values) { const normalized = (value - 128) / 128; sum += normalized * normalized; }
            meter.value = Math.min(1, Math.sqrt(sum / values.length) * 2.5);
            frame = requestAnimationFrame(draw);
        };
        draw();
        return () => { cancelAnimationFrame(frame); source.disconnect(); analyser.disconnect(); context.close().catch(() => undefined); };
    }

    function compileShader(gl, type, source) {
        const shader = gl.createShader(type);
        gl.shaderSource(shader, source);
        gl.compileShader(shader);
        if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) throw new Error(gl.getShaderInfoLog(shader) || "Shader compilation failed");
        return shader;
    }

    function installChroma(video, canvas, config) {
        if (!video || !canvas || !config.chromaKeyEnabled) {
            canvas?.classList.remove("active");
            return null;
        }
        const gl = canvas.getContext("webgl", { premultipliedAlpha: false, alpha: true });
        if (!gl) return null;
        const program = gl.createProgram();
        gl.attachShader(program, compileShader(gl, gl.VERTEX_SHADER, `attribute vec2 p;varying vec2 uv;void main(){uv=(p+1.0)*0.5;uv.y=1.0-uv.y;gl_Position=vec4(p,0,1);}`));
        gl.attachShader(program, compileShader(gl, gl.FRAGMENT_SHADER, `precision mediump float;varying vec2 uv;uniform sampler2D tex;uniform vec3 key;uniform float similarity;uniform float smoothness;uniform float spill;uniform float residual;void main(){vec4 c=texture2D(tex,uv);float d=distance(c.rgb,key);float keep=smoothstep(similarity,similarity+max(0.001,smoothness),d);float nearKey=1.0-keep;float gray=dot(c.rgb,vec3(0.299,0.587,0.114));c.rgb=mix(c.rgb,vec3(gray),nearKey*spill);c.a*=mix(residual,1.0,keep);gl_FragColor=c;}`));
        gl.linkProgram(program);
        if (!gl.getProgramParameter(program, gl.LINK_STATUS)) throw new Error(gl.getProgramInfoLog(program) || "Shader link failed");
        gl.useProgram(program);
        const buffer = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, buffer);
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1,-1,1,-1,-1,1,-1,1,1,-1,1,1]), gl.STATIC_DRAW);
        const position = gl.getAttribLocation(program, "p");
        gl.enableVertexAttribArray(position);
        gl.vertexAttribPointer(position, 2, gl.FLOAT, false, 0, 0);
        const texture = gl.createTexture();
        gl.bindTexture(gl.TEXTURE_2D, texture);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
        const key = hexColor(config.chromaKeyColor);
        gl.uniform3f(gl.getUniformLocation(program, "key"), key[0], key[1], key[2]);
        gl.uniform1f(gl.getUniformLocation(program, "similarity"), Number(config.chromaSimilarity || .35));
        gl.uniform1f(gl.getUniformLocation(program, "smoothness"), Number(config.chromaSmoothness || .12));
        gl.uniform1f(gl.getUniformLocation(program, "spill"), Number(config.chromaSpill || .3));
        gl.uniform1f(gl.getUniformLocation(program, "residual"), Number(config.chromaResidualOpacity ?? 1));
        canvas.classList.add("active");
        let frame = 0;
        const render = () => {
            const width = Math.max(2, video.videoWidth || canvas.clientWidth || 640);
            const height = Math.max(2, video.videoHeight || canvas.clientHeight || 360);
            if (canvas.width !== width || canvas.height !== height) { canvas.width = width; canvas.height = height; gl.viewport(0, 0, width, height); }
            if (video.readyState >= 2) {
                gl.bindTexture(gl.TEXTURE_2D, texture);
                gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, video);
                gl.drawArrays(gl.TRIANGLES, 0, 6);
            }
            frame = requestAnimationFrame(render);
        };
        render();
        return () => { cancelAnimationFrame(frame); canvas.classList.remove("active"); try { gl.deleteTexture(texture); gl.deleteProgram(program); } catch { } };
    }


    function installNowPlaying(config) {
        const root = document.getElementById(config.metadataId);
        if (!root || !config.nowPlayingDirectory || !config.mediaHostUrl) return null;
        const title = root.querySelector("[data-now-playing-title]");
        const artist = root.querySelector("[data-now-playing-artist]");
        const album = root.querySelector("[data-now-playing-album]");
        const cover = root.querySelector("[data-now-playing-cover]");
        let stopped = false;
        let lastIdentity = "";
        const refresh = async () => {
            if (stopped) return;
            try {
                const url = `${String(config.mediaHostUrl).replace(/\/$/, "")}/api/mediahost/now-playing?directory=${encodeURIComponent(config.nowPlayingDirectory)}`;
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
                if (album) album.textContent = error?.message || "Media Host unavailable";
            }
        };
        refresh();
        const timer = window.setInterval(refresh, 1500);
        return () => { stopped = true; window.clearInterval(timer); };
    }

    async function attachSource(config) {
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
                await video.play().catch(() => undefined);
            }
            const stopChroma = installChroma(video, canvas, config);
            const stopMeter = installMeter(stream, meter);
            const tracks = stream ? stream.getTracks() : [];
            const ended = () => detachSource(config.id);
            tracks.forEach(track => track.addEventListener("ended", ended, { once: true }));
            const state = { stream, video, stopChroma, stopMeter, ended, config };
            sources.set(String(config.id), state);
            connectProgramAudio(state);
            return !!stream || !!(video && video.src);
        } catch (error) {
            console.error("PublisherStudio live source failed", error);
            window.dispatchEvent(new CustomEvent("publisherstudio:stream-error", { detail: { id: config.id, message: error?.message || String(error) } }));
            return false;
        }
    }

    function detachSource(id) {
        const state = sources.get(String(id));
        if (!state) return;
        disconnectProgramAudio(state);
        state.stopChroma?.();
        state.stopMeter?.();
        state.stopMetadata?.();
        try { state.stream?.__publisherNativeCleanup?.(); } catch { }
        state.stream?.getTracks?.().forEach(track => track.stop());
        if (state.video) { try { state.video.pause(); state.video.srcObject = null; state.video.removeAttribute("src"); state.video.load(); } catch { } }
        sources.delete(String(id));
    }

    async function enumerateDevices() {
        if (!navigator.mediaDevices?.enumerateDevices) return [];
        try {
            const permission = await navigator.mediaDevices.getUserMedia({ audio: true, video: true });
            permission.getTracks().forEach(track => track.stop());
        } catch { }
        const devices = await navigator.mediaDevices.enumerateDevices();
        return devices.map(device => ({ deviceId: device.deviceId, kind: device.kind, label: device.label || "Permission required" }));
    }

    async function chooseDirectory() {
        if (!window.showDirectoryPicker) return null;
        try { const handle = await window.showDirectoryPicker({ mode: "readwrite" }); return handle?.name || null; }
        catch { return null; }
    }



    function supportedProgramMimeType() {
        if (typeof MediaRecorder === "undefined") return "";
        return [
            "video/webm;codecs=vp9,opus",
            "video/webm;codecs=vp8,opus",
            "video/webm"
        ].find(value => MediaRecorder.isTypeSupported(value)) || "";
    }

    function ensureStreamingCaptureStyles() {
        if (document.getElementById("publisherstream-capture-style")) return;
        const style = document.createElement("style");
        style.id = "publisherstream-capture-style";
        style.textContent = `html.publisherstream-base-capture [data-publisher-stream-chat-layer]{visibility:hidden!important}`;
        document.head.append(style);
    }

    function markStreamingChatLayers() {
        ensureStreamingCaptureStyles();
        for (const host of document.querySelectorAll?.("[data-ps-component-runtime].ps-dx-chat") || []) {
            const owner = host.closest?.("[data-publication-element]") || host;
            owner.setAttribute("data-publisher-stream-chat-layer", "");
        }
    }

    function wrapCanvasText(context, text, maxWidth) {
        const words = String(text || "").split(/\s+/).filter(Boolean);
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
    }

    function roundedRect(context, x, y, width, height, radius) {
        const r = Math.max(0, Math.min(radius, width / 2, height / 2));
        context.beginPath();
        context.moveTo(x + r, y);
        context.arcTo(x + width, y, x + width, y + height, r);
        context.arcTo(x + width, y + height, x, y + height, r);
        context.arcTo(x, y + height, x, y, r);
        context.arcTo(x, y, x + width, y, r);
        context.closePath();
    }

    function drawBroadcastChatLayer(context, layer, outputWidth, outputHeight) {
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
        const padding = Math.max(8, baseFontSize * .65);
        const radius = Math.max(4, Number(layer.borderRadius || 8) * fontScale);
        context.save();
        roundedRect(context, x, y, width, height, radius);
        context.clip();
        context.fillStyle = layer.background && layer.background !== "rgba(0, 0, 0, 0)" ? layer.background : "rgba(15,23,42,.88)";
        context.fillRect(x, y, width, height);
        context.font = `600 ${baseFontSize}px ${layer.fontFamily || "system-ui"}`;
        context.fillStyle = layer.color || "#f8fafc";
        context.textBaseline = "top";
        const heading = `${layer.platform || "Chat"}${layer.channel ? ` · ${layer.channel}` : ""}`;
        context.fillText(heading, x + padding, y + padding, Math.max(0, width - padding * 2));
        const headingHeight = baseFontSize * 1.5;
        const messageFont = Math.max(9, baseFontSize * .9);
        const authorFont = Math.max(9, messageFont * .88);
        let cursor = y + height - padding;
        const items = Array.isArray(layer.items) ? layer.items : [];
        for (let index = items.length - 1; index >= 0; index--) {
            const item = items[index] || {};
            context.font = `${messageFont}px ${layer.fontFamily || "system-ui"}`;
            const lines = wrapCanvasText(context, item.text || "", Math.max(20, width - padding * 2));
            const lineHeight = messageFont * 1.25;
            const blockHeight = authorFont * 1.25 + lines.length * lineHeight + padding * .45;
            cursor -= blockHeight;
            if (cursor < y + padding + headingHeight) break;
            context.fillStyle = "rgba(255,255,255,.075)";
            roundedRect(context, x + padding * .5, cursor, width - padding, blockHeight - padding * .15, radius * .65);
            context.fill();
            context.font = `600 ${authorFont}px ${layer.fontFamily || "system-ui"}`;
            context.fillStyle = layer.color || "#f8fafc";
            context.fillText(String(item.authorName || "Viewer"), x + padding, cursor + padding * .25);
            context.font = `${messageFont}px ${layer.fontFamily || "system-ui"}`;
            context.fillStyle = layer.color || "#f8fafc";
            let lineY = cursor + padding * .25 + authorFont * 1.2;
            for (const line of lines) {
                context.fillText(line, x + padding, lineY);
                lineY += lineHeight;
            }
            cursor -= padding * .25;
        }
        context.restore();
    }

    function broadcastChatLayers(output, pageElementId) {
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
    }

    function createCaptureVariant(output, audioTracks) {
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
    }

    function drawProgramFrame(state) {
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
        state.drawFrame = requestAnimationFrame(() => drawProgramFrame(state));
    }

    async function prepareProgramCapture(config = {}) {
        stopProgramIngest();
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
            await audioContext.resume().catch(() => undefined);
            audioDestination = audioContext.createMediaStreamDestination();
            const displayAudioTracks = sourceStream.getAudioTracks();
            if (displayAudioTracks.length) {
                captureAudio = audioContext.createMediaStreamSource(new MediaStream(displayAudioTracks));
                captureAudio.connect(audioDestination);
            }
            audioDestination.stream.getAudioTracks().forEach(track => baseStream.addTrack(track));
        } else {
            sourceStream.getAudioTracks().forEach(track => baseStream.addTrack(track));
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
        const ended = () => stopProgramIngest();
        sourceStream.getVideoTracks()[0]?.addEventListener("ended", ended, { once: true });
        programCapture.ended = ended;
        window.dispatchEvent(new CustomEvent("publisherstudio:program-capture-ready", { detail: { width, height, frameRate } }));
        return true;
    }

    async function openIngestSocket(config, outputId = "") {
        const baseUrl = String(config.mediaHostUrl || "").replace(/^http/i, "ws").replace(/\/$/, "");
        if (!baseUrl || !config.sessionId) throw new Error("The Media Host session is not available.");
        const query = outputId ? `?outputId=${encodeURIComponent(outputId)}` : "";
        const socket = new WebSocket(`${baseUrl}/api/mediahost/sessions/${encodeURIComponent(config.sessionId)}/ingest/websocket${query}`);
        socket.binaryType = "arraybuffer";
        await new Promise((resolve, reject) => {
            const timeout = setTimeout(() => reject(new Error("The Media Host did not accept the browser ingest.")), 5000);
            socket.addEventListener("open", () => { clearTimeout(timeout); resolve(); }, { once: true });
            socket.addEventListener("error", () => { clearTimeout(timeout); reject(new Error("The browser could not connect to the Media Host ingest socket.")); }, { once: true });
        });
        return socket;
    }

    async function startVariantRecorder(config, variant, outputId = "") {
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
        recorder.addEventListener("dataavailable", async event => {
            if (!event.data?.size || socket.readyState !== WebSocket.OPEN) return;
            try { socket.send(await event.data.arrayBuffer()); }
            catch (error) { console.error("PublisherStudio could not send a program frame chunk", error); }
        });
        recorder.addEventListener("error", event => {
            window.dispatchEvent(new CustomEvent("publisherstudio:stream-error", { detail: { outputId, message: event.error?.message || "Program encoding failed." } }));
        });
        socket.addEventListener("close", () => {
            if (recorder.state !== "inactive") try { recorder.stop(); } catch { }
        });
        recorder.start(250);
        programCapture.recorders.push(recorder);
        programCapture.sockets.push(socket);
        variant.recorder = recorder;
        variant.socket = socket;
        return { mimeType, width: variant.width, height: variant.height, frameRate: variant.frameRate };
    }

    async function startPublisherWebRtc(config, stream) {
        if (!config.enableWebRtc || typeof RTCPeerConnection === "undefined") return;
        const baseUrl = String(config.mediaHostUrl || "").replace(/^http/i, "ws").replace(/\/$/, "");
        const socket = new WebSocket(`${baseUrl}/api/mediahost/sessions/${encodeURIComponent(config.sessionId)}/webrtc/publisher`);
        programCapture.signalSocket = socket;
        const send = value => { if (socket.readyState === WebSocket.OPEN) socket.send(JSON.stringify(value)); };
        const closePeer = viewerId => {
            const peer = programCapture?.peers?.get(viewerId);
            if (!peer) return;
            try { peer.close(); } catch { }
            programCapture.peers.delete(viewerId);
        };
        socket.addEventListener("message", async event => {
            let message;
            try { message = JSON.parse(event.data); } catch { return; }
            const viewerId = String(message.viewerId || "");
            if (!viewerId) return;
            if (message.type === "viewer-left") { closePeer(viewerId); return; }
            let peer = programCapture?.peers?.get(viewerId);
            if (!peer) {
                peer = new RTCPeerConnection({ iceServers: [] });
                for (const track of stream.getTracks()) peer.addTrack(track, stream);
                peer.addEventListener("icecandidate", ice => {
                    if (ice.candidate) send({ type: "publisher-candidate", viewerId, candidate: ice.candidate });
                });
                peer.addEventListener("connectionstatechange", () => {
                    if (["failed", "closed", "disconnected"].includes(peer.connectionState)) closePeer(viewerId);
                });
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
        });
        socket.addEventListener("close", () => {
            for (const viewerId of [...(programCapture?.peers?.keys?.() || [])]) closePeer(viewerId);
        });
    }

    async function startProgramIngest(config = {}) {
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
        for (const output of Array.isArray(config.outputs) ? config.outputs.filter(item => item?.outputId && item.captureRequired !== false) : []) {
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
    }

    function stopProgramIngest() {
        const state = programCapture;
        programCapture = null;
        document.documentElement.classList.remove("publisherstream-base-capture");
        if (!state) return;
        for (const recorder of state.recorders || []) {
            try { if (recorder.state !== "inactive") recorder.stop(); } catch { }
        }
        for (const socket of state.sockets || []) {
            try { if (socket.readyState < WebSocket.CLOSING) socket.close(1000, "PublisherStudio session stopped"); } catch { }
        }
        try { if (state.signalSocket?.readyState < WebSocket.CLOSING) state.signalSocket.close(1000, "PublisherStudio session stopped"); } catch { }
        for (const peer of state.peers?.values?.() || []) { try { peer.close(); } catch { } }
        try { cancelAnimationFrame(state.drawFrame); } catch { }
        for (const sourceState of sources.values()) disconnectProgramAudio(sourceState);
        try { state.captureAudio?.disconnect?.(); } catch { }
        try { state.audioDestination?.disconnect?.(); } catch { }
        try { state.audioContext?.close?.(); } catch { }
        state.canvasStream?.getTracks?.().forEach(track => track.stop());
        for (const variant of state.variants?.values?.() || []) variant.stream?.getTracks?.().forEach(track => track.stop());
        state.sourceStream?.getTracks?.().forEach(track => track.stop());
        try { state.video.pause(); state.video.srcObject = null; } catch { }
        window.dispatchEvent(new CustomEvent("publisherstudio:program-capture-stopped"));
    }

    function chatKey(platform, channel) {
        return `${String(platform || "").trim().toLowerCase()}|${String(channel || "").trim().toLowerCase()}`;
    }

    function stopChatBridge() {
        const state = chatBridgeState;
        chatBridgeState = null;
        if (!state) return;
        for (const socket of state.sockets.values()) {
            try { if (socket.readyState < WebSocket.CLOSING) socket.close(1000, "PublisherStudio Chat stopped"); } catch { }
        }
        state.sockets.clear();
        state.subscribers.clear();
        state.messages.clear();
        if (window.PublisherStudioChatBridge === state.bridge) delete window.PublisherStudioChatBridge;
    }

    function configureChatBridge(config = {}) {
        stopChatBridge();
        const mediaHostUrl = String(config.mediaHostUrl || "").replace(/\/$/, "");
        const sessionId = String(config.sessionId || "");
        if (!mediaHostUrl || !sessionId) return false;
        const outputs = (Array.isArray(config.outputs) ? config.outputs : [])
            .filter(item => item?.outputId && item.captureRequired !== false && item.chatEnabled === true)
            .map(item => ({
                outputId: String(item.outputId),
                platform: String(item.platform || item.provider || "Preview"),
                channel: String(item.channel || "")
            }));
        const state = {
            mediaHostUrl,
            sessionId,
            outputs,
            sockets: new Map(),
            messages: new Map(),
            subscribers: new Set(),
            bridge: null
        };
        const notify = detail => {
            const key = chatKey(detail.platform, detail.channel);
            const bucket = state.messages.get(key) || [];
            if (!bucket.some(item => String(item.id || "") === String(detail.message?.id || ""))) {
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
        };
        const bridge = {
            subscribe(context, receive) {
                if (typeof receive !== "function") return () => {};
                const subscription = {
                    platform: String(context?.platform || "").toLowerCase(),
                    channel: String(context?.channel || "").toLowerCase(),
                    receive
                };
                state.subscribers.add(subscription);
                for (const message of state.messages.get(chatKey(context?.platform, context?.channel)) || []) {
                    try { receive({ platform: context?.platform || "", channel: context?.channel || "", message }); } catch { }
                }
                return () => state.subscribers.delete(subscription);
            },
            async send(detail) {
                const message = String(detail?.message?.text || detail?.message || "").trim();
                if (!message) return false;
                const requestedOutputId = String(detail?.outputId || outputContext.outputId || "");
                const output = state.outputs.find(item => item.outputId === requestedOutputId)
                    || state.outputs.find(item => chatKey(item.platform, item.channel) === chatKey(detail?.platform, detail?.channel));
                if (!output) throw new Error("No configured provider Chat matches the selected operator Chat.");
                const response = await fetch(`${state.mediaHostUrl}/api/mediahost/sessions/${encodeURIComponent(state.sessionId)}/chat/${encodeURIComponent(output.outputId)}/send`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ message })
                });
                if (!response.ok) {
                    const error = await response.json().catch(() => ({}));
                    throw new Error(error.error || `Chat send failed (${response.status}).`);
                }
                return true;
            },
            getMessages(platform, channel) {
                return [...(state.messages.get(chatKey(platform, channel)) || [])];
            }
        };
        state.bridge = bridge;
        chatBridgeState = state;
        window.PublisherStudioChatBridge = bridge;

        const wsBase = mediaHostUrl.replace(/^http/i, "ws");
        for (const output of outputs) {
            const socket = new WebSocket(`${wsBase}/api/mediahost/sessions/${encodeURIComponent(sessionId)}/chat/${encodeURIComponent(output.outputId)}/websocket`);
            state.sockets.set(output.outputId, socket);
            socket.addEventListener("message", event => {
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
            });
            socket.addEventListener("error", () => window.dispatchEvent(new CustomEvent("publisherstudio:stream-error", {
                detail: { id: output.outputId, message: `${output.platform} Chat connection failed.` }
            })));
        }
        return true;
    }

    function normalizeGesture(event) {
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
    }

    function normalizeConfiguredGesture(value) {
        const aliases = { CTRL: "Ctrl", CONTROL: "Ctrl", ALT: "Alt", SHIFT: "Shift", META: "Meta", WIN: "Meta", CMD: "Meta", ESC: "Escape", DEL: "Delete", SPACEBAR: "Space" };
        return String(value || "")
            .split("+")
            .map(part => part.trim())
            .filter(Boolean)
            .map(part => aliases[part.toUpperCase()] || (part.length === 1 ? part.toUpperCase() : part))
            .join("+");
    }

    function isTypingTarget(target) {
        if (!(target instanceof Element)) return false;
        return !!target.closest("input, textarea, select, [contenteditable='true'], .dx-texteditor-input");
    }

    function unbindHotkeys() {
        if (hotkeyListener) window.removeEventListener("keydown", hotkeyListener, true);
        hotkeyListener = null;
        hotkeyReference = null;
        configuredHotkeys = [];
    }

    function bindHotkeys(hotkeys, dotnetReference) {
        unbindHotkeys();
        configuredHotkeys = Array.isArray(hotkeys)
            ? hotkeys.filter(item => item && item.gesture && item.command && !item.global).map(item => ({ ...item, normalized: normalizeConfiguredGesture(item.gesture) }))
            : [];
        hotkeyReference = dotnetReference || null;
        if (!configuredHotkeys.length || !hotkeyReference) return;
        hotkeyListener = event => {
            if (event.repeat || event.isComposing) return;
            const gesture = normalizeGesture(event);
            const match = configuredHotkeys.find(item => item.normalized === gesture);
            if (!match) return;
            if (isTypingTarget(event.target) && !/^F\d{1,2}$/.test(String(event.key || ""))) return;
            event.preventDefault();
            event.stopPropagation();
            hotkeyReference.invokeMethodAsync("HandleStreamingHotkey", String(match.command), match.targetId ? String(match.targetId) : null)
                .catch(error => console.error("PublisherStudio streaming hotkey failed", error));
        };
        window.addEventListener("keydown", hotkeyListener, true);
    }

    function setOutputContext(context = {}) {
        Object.assign(outputContext, context);
        window.PublisherStudioOutputContext = { ...outputContext };
        window.PublisherStudioChatPlatform = outputContext.platform || "Preview";
        window.PublisherStudioChatChannel = outputContext.channel || "";
        window.dispatchEvent(new CustomEvent("publisherstudio:output-context-changed", { detail: { ...outputContext } }));
    }

    window.publisherStreaming = {
        attachSource,
        detachSource,
        enumerateDevices,
        chooseDirectory,
        setOutputContext,
        prepareProgramCapture,
        startProgramIngest,
        stopProgramIngest,
        configureChatBridge,
        stopChatBridge,
        bindHotkeys,
        unbindHotkeys,
        getOutputContext: () => ({ ...outputContext }),
        stopAll: () => { [...sources.keys()].forEach(detachSource); stopChatBridge(); stopProgramIngest(); }
    };
})();
