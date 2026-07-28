// javascript-diagnostics: guarded
var publisherStudioDiagnostics = globalThis.publisherStudioJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`PublisherStudio JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("PublisherStudio fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`PublisherStudio fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
(function () { try {
    async function blobToDataUrl(blob) { try {
        return await new Promise((resolve, reject) => { try {
            const reader = new FileReader();
            reader.onload = () => { try { return (resolve(String(reader.result || ""))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/secureCaptureInterop.js:reader.onload@6', __javascriptError); throw __javascriptError; } };
            reader.onerror = () => { try { return (reject(reader.error || new Error("Could not read browser capture."))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/secureCaptureInterop.js:reader.onerror@7', __javascriptError); throw __javascriptError; } };
            reader.readAsDataURL(blob);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/secureCaptureInterop.js:ArrowFunction@4', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/secureCaptureInterop.js:blobToDataUrl@3', __javascriptError); throw __javascriptError; }}

    async function getCurrentDisplayStream(includeAudio) { try {
        if (!navigator.mediaDevices?.getDisplayMedia)
            throw new Error("This browser does not support secure screen selection through getDisplayMedia.");
        return await navigator.mediaDevices.getDisplayMedia({
            video: { frameRate: { ideal: 12, max: 20 } },
            audio: Boolean(includeAudio)
        });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/secureCaptureInterop.js:getCurrentDisplayStream@12', __javascriptError); throw __javascriptError; }}

    async function captureScreen() { try {
        const stream = await getCurrentDisplayStream(false);
        try {
            const video = document.createElement("video");
            video.muted = true;
            video.playsInline = true;
            video.srcObject = stream;
            await video.play();
            if (!video.videoWidth || !video.videoHeight)
                await new Promise(resolve => { try { return (video.addEventListener("loadedmetadata", resolve, { once: true })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/secureCaptureInterop.js:ArrowFunction@30', __javascriptError); throw __javascriptError; } });
            const maxWidth = 1920;
            const scale = Math.min(1, maxWidth / Math.max(1, video.videoWidth));
            const width = Math.max(1, Math.round(video.videoWidth * scale));
            const height = Math.max(1, Math.round(video.videoHeight * scale));
            const canvas = document.createElement("canvas");
            canvas.width = width;
            canvas.height = height;
            canvas.getContext("2d", { alpha: false }).drawImage(video, 0, 0, width, height);
            return JSON.stringify({
                kind: "image",
                dataUrl: canvas.toDataURL("image/png"),
                mimeType: "image/png",
                width,
                height,
                durationMilliseconds: 0
            });
        } finally {
            stream.getTracks().forEach(track => { try { return (track.stop()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/secureCaptureInterop.js:callback:stream.getTracks().forEach@48', __javascriptError); throw __javascriptError; } });
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/secureCaptureInterop.js:captureScreen@21', __javascriptError); throw __javascriptError; }}

    async function recordScreen(maximumSeconds, includeAudio) { try {
        const seconds = Math.max(1, Math.min(15, Number(maximumSeconds) || 10));
        const stream = await getCurrentDisplayStream(Boolean(includeAudio));
        try {
            const preferred = ["video/webm;codecs=vp9,opus", "video/webm;codecs=vp8,opus", "video/webm"]
                .find(type => { try { return (!window.MediaRecorder?.isTypeSupported || MediaRecorder.isTypeSupported(type)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/secureCaptureInterop.js:callback:["video/webm;codecs=vp9,opus", "video/webm;codecs=vp8,opus", "video/we@57', __javascriptError); throw __javascriptError; } });
            if (!window.MediaRecorder) throw new Error("This browser does not support MediaRecorder.");
            const chunks = [];
            const recorder = new MediaRecorder(stream, preferred ? { mimeType: preferred, videoBitsPerSecond: 1_200_000 } : { videoBitsPerSecond: 1_200_000 });
            const started = performance.now();
            const stopped = new Promise((resolve, reject) => { try {
                recorder.ondataavailable = event => { try { if (event.data?.size) chunks.push(event.data);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/secureCaptureInterop.js:recorder.ondataavailable@63', __javascriptError); throw __javascriptError; }};
                recorder.onerror = event => { try { return (reject(event.error || new Error("Screen recording failed."))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/secureCaptureInterop.js:recorder.onerror@64', __javascriptError); throw __javascriptError; } };
                recorder.onstop = resolve;
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/secureCaptureInterop.js:ArrowFunction@62', __javascriptError); throw __javascriptError; }});
            recorder.start(500);
            const endEarly = new Promise(resolve => { try { return (stream.getVideoTracks()[0]?.addEventListener("ended", resolve, { once: true })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/secureCaptureInterop.js:ArrowFunction@68', __javascriptError); throw __javascriptError; } });
            await Promise.race([new Promise(resolve => { try { return (setTimeout(resolve, seconds * 1000)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/secureCaptureInterop.js:ArrowFunction@69', __javascriptError); throw __javascriptError; } }), endEarly]);
            if (recorder.state !== "inactive") recorder.stop();
            await stopped;
            const blob = new Blob(chunks, { type: recorder.mimeType || "video/webm" });
            if (blob.size > 6 * 1024 * 1024)
                throw new Error("The selected recording is too large for the bounded 1-Wire response. Record a shorter interval.");
            return JSON.stringify({
                kind: "video",
                dataUrl: await blobToDataUrl(blob),
                mimeType: blob.type || "video/webm",
                width: 0,
                height: 0,
                durationMilliseconds: Math.round(performance.now() - started)
            });
        } finally {
            stream.getTracks().forEach(track => { try { return (track.stop()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/secureCaptureInterop.js:callback:stream.getTracks().forEach@84', __javascriptError); throw __javascriptError; } });
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/secureCaptureInterop.js:recordScreen@52', __javascriptError); throw __javascriptError; }}

    window.publisherSecureCapture = { captureScreen, recordScreen };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/secureCaptureInterop.js:FunctionExpression@2', __javascriptError); throw __javascriptError; }})();

// Guard exported browser namespaces after the file has initialized.
publisherStudioDiagnostics.guardObject("publisherSecureCapture", window.publisherSecureCapture);
