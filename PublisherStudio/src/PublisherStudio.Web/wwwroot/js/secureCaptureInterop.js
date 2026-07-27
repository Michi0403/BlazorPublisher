(function () {
    async function blobToDataUrl(blob) {
        return await new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => resolve(String(reader.result || ""));
            reader.onerror = () => reject(reader.error || new Error("Could not read browser capture."));
            reader.readAsDataURL(blob);
        });
    }

    async function getCurrentDisplayStream(includeAudio) {
        if (!navigator.mediaDevices?.getDisplayMedia)
            throw new Error("This browser does not support secure screen selection through getDisplayMedia.");
        return await navigator.mediaDevices.getDisplayMedia({
            video: { frameRate: { ideal: 12, max: 20 } },
            audio: Boolean(includeAudio)
        });
    }

    async function captureScreen() {
        const stream = await getCurrentDisplayStream(false);
        try {
            const video = document.createElement("video");
            video.muted = true;
            video.playsInline = true;
            video.srcObject = stream;
            await video.play();
            if (!video.videoWidth || !video.videoHeight)
                await new Promise(resolve => video.addEventListener("loadedmetadata", resolve, { once: true }));
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
            stream.getTracks().forEach(track => track.stop());
        }
    }

    async function recordScreen(maximumSeconds, includeAudio) {
        const seconds = Math.max(1, Math.min(15, Number(maximumSeconds) || 10));
        const stream = await getCurrentDisplayStream(Boolean(includeAudio));
        try {
            const preferred = ["video/webm;codecs=vp9,opus", "video/webm;codecs=vp8,opus", "video/webm"]
                .find(type => !window.MediaRecorder?.isTypeSupported || MediaRecorder.isTypeSupported(type));
            if (!window.MediaRecorder) throw new Error("This browser does not support MediaRecorder.");
            const chunks = [];
            const recorder = new MediaRecorder(stream, preferred ? { mimeType: preferred, videoBitsPerSecond: 1_200_000 } : { videoBitsPerSecond: 1_200_000 });
            const started = performance.now();
            const stopped = new Promise((resolve, reject) => {
                recorder.ondataavailable = event => { if (event.data?.size) chunks.push(event.data); };
                recorder.onerror = event => reject(event.error || new Error("Screen recording failed."));
                recorder.onstop = resolve;
            });
            recorder.start(500);
            const endEarly = new Promise(resolve => stream.getVideoTracks()[0]?.addEventListener("ended", resolve, { once: true }));
            await Promise.race([new Promise(resolve => setTimeout(resolve, seconds * 1000)), endEarly]);
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
            stream.getTracks().forEach(track => track.stop());
        }
    }

    window.publisherSecureCapture = { captureScreen, recordScreen };
})();
