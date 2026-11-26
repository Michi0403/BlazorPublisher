// javascript-diagnostics: guarded
const sources = new Map();
const stopTimes = new WeakMap();
const dropSurfaces = new Map();

// DevExpress mounts popup templates after the parent component's render callback.
function whenRendered(elementId, attach) {
    const element = document.getElementById(elementId);
    if (element) {
        attach(element);
        return null;
    }
    const observer = new MutationObserver(() => {
        try {
            const rendered = document.getElementById(elementId);
            if (!rendered) return;
            observer.disconnect();
            attach(rendered);
        } catch (error) {
            report("whenRendered", error);
        }
    });
    observer.observe(document.body, { childList: true, subtree: true });
    return observer;
}

export function watchDropSurface(id, dotNetReference) {
    if (dropSurfaces.has(id)) return;
    const surface = {};
    surface.observer = whenRendered("media-converter-source-card", element => {
        globalThis.publisherStudio.bindMediaConverterDrop(element, dotNetReference);
        surface.element = element;
    });
    dropSurfaces.set(id, surface);
}

export function close(id) {
    const surface = dropSurfaces.get(id);
    if (surface) {
        surface.observer?.disconnect();
        if (surface.element) globalThis.publisherStudio.unbindMediaConverterDrop(surface.element);
        dropSurfaces.delete(id);
    }
    release(id);
}

function stopAtSelectionEnd(event) {
    try {
        const player = event.currentTarget;
        const end = stopTimes.get(player);
        if (end === undefined || player.currentTime < end) return;
        stopTimes.delete(player);
        player.pause();
        player.currentTime = end;
    } catch (error) {
        report("stopAtSelectionEnd", error);
    }
}

function report(operation, error) {
    if (globalThis.publisherStudioJavaScriptDiagnostics)
        globalThis.publisherStudioJavaScriptDiagnostics.report(`js/mediaTrimPreview.js:${operation}`, error);
    else console.error(operation, error);
}

function detach(player) {
    if (!player) return;
    player.pause();
    player.removeEventListener("timeupdate", stopAtSelectionEnd);
    stopTimes.delete(player);
    player.removeAttribute("src");
    player.load();
}

export function release(id) {
    const source = sources.get(id);
    if (!source) return;
    try {
        source.observer?.disconnect();
        detach(source.player);
    } catch (error) {
        report("release", error);
    } finally {
        if (source.objectUrl) URL.revokeObjectURL(source.objectUrl);
        sources.delete(id);
    }
}

export function bind(id, assetUrl, sourceChanged) {
    try {
        if (sourceChanged) release(id);
        let source = sources.get(id);
        if (!source) {
            const file = assetUrl ? null : document.getElementById("media-converter-source-input")?.files?.[0];
            const objectUrl = file ? URL.createObjectURL(file) : null;
            source = { url: assetUrl || objectUrl, objectUrl };
            if (!source.url) return;
            sources.set(id, source);
        }
        if (source.player?.isConnected || (source.observer && !source.player)) return;
        detach(source.player);
        source.player = null;
        source.observer = whenRendered("media-converter-trim-player", player => {
            source.player = player;
            player.addEventListener("timeupdate", stopAtSelectionEnd);
            player.src = source.url;
        });
    } catch (error) {
        report("bind", error);
        throw error;
    }
}

export function readPosition(id) {
    const player = sources.get(id)?.player;
    return player?.readyState >= HTMLMediaElement.HAVE_METADATA && Number.isFinite(player.currentTime)
        ? player.currentTime
        : null;
}

export async function playSelection(id, start, duration) {
    const player = sources.get(id)?.player;
    const end = start + duration;
    if (readPosition(id) === null || !Number.isFinite(end) || start < 0 || duration <= 0 || start >= player.duration) {
        return false;
    }
    try {
        player.currentTime = start;
        stopTimes.set(player, Math.min(end, player.duration));
        await player.play();
        return true;
    } catch (error) {
        report("playSelection", error);
        stopTimes.delete(player);
        return false;
    }
}
