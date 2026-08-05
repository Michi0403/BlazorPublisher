// javascript-diagnostics: guarded
var publisherStudioDiagnostics = globalThis.publisherStudioJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`PublisherStudio JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("PublisherStudio fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`PublisherStudio fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
(() => { try {
    const runtimes = new Map();

    const clamp = (value, minimum, maximum, fallback = minimum) => { try {
        const number = Number(value);
        return Math.max(minimum, Math.min(maximum, Number.isFinite(number) ? number : fallback));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:clamp@5', __javascriptError); throw __javascriptError; }};

    const normalizeColor = (value, fallback = '#00ff00') => { try { return (/^#[0-9a-f]{6}$/i.test(String(value || ''))
        ? String(value).toLowerCase()
        : fallback); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:normalizeColor@10', __javascriptError); throw __javascriptError; } };

    function filterKind(filter) { try {
        return String(filter?.kind || '').replace(/[^a-z]/gi, '').toLowerCase();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:filterKind@14', __javascriptError); throw __javascriptError; }}

    function blendMode(value) { try {
        switch (String(value || '').toLowerCase()) {
            case 'multiply': return 'multiply';
            case 'screen': return 'screen';
            case 'overlay': return 'overlay';
            case 'darken': return 'darken';
            case 'lighten': return 'lighten';
            default: return 'source-over';
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:blendMode@18', __javascriptError); throw __javascriptError; }}

    function layerKind(value) { try {
        const kind = String(value || '').replace(/[^a-z0-9]/gi, '').toLowerCase();
        if (kind === 'blob3d') return 'blob3d';
        if (kind === 'selection2d') return 'selection2d';
        return 'basevideo';
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:layerKind@29', __javascriptError); throw __javascriptError; }}

    function normalizePoints(points) { try {
        return (Array.isArray(points) ? points : [])
            .slice(0, 256)
            .map(point => { try { return (({ x: clamp(point?.x, 0, 1, 0), y: clamp(point?.y, 0, 1, 0) })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:callback:(Array.isArray(points) ? points : []) .slice(0, 256) .map@39', __javascriptError); throw __javascriptError; } });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:normalizePoints@36', __javascriptError); throw __javascriptError; }}

    function normalizedLayers(config) { try {
        return (Array.isArray(config?.layers) ? config.layers : [])
            .filter(layer => { try { return (layer && layer.visible !== false); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:callback:(Array.isArray(config?.layers) ? config.layers : []) .filter@44', __javascriptError); throw __javascriptError; } })
            .slice(0, 64)
            .map((layer, layerIndex) => { try { return (({
                id: String(layer.id || layerIndex),
                name: String(layer.name || `Video layer ${layerIndex + 1}`),
                kind: layerKind(layer.kind),
                opacity: clamp(layer.opacity, 0, 1, 1),
                blendMode: blendMode(layer.blendMode),
                hasTemporalRange: layer.hasTemporalRange === true,
                temporalStartSeconds: Math.max(0, Number(layer.temporalStartSeconds) || 0),
                temporalEndSeconds: Math.max(0, Number(layer.temporalEndSeconds) || 0),
                morphEnabled: layer.morphEnabled === true,
                animateMorph: layer.animateMorph !== false,
                morphAmount: clamp(layer.morphAmount, 0, 1, 0),
                animationSpeed: clamp(layer.animationSpeed, 0, 8, 1),
                depth: clamp(layer.depth, .02, .5, .18),
                roundness: clamp(layer.roundness, 0, .5, .12),
                htmlExportSupport: String(layer.htmlExportSupport || 'Native'),
                region: {
                    inverted: layer.region?.inverted === true,
                    points: normalizePoints(layer.region?.points)
                },
                morphRegion: {
                    inverted: layer.morphRegion?.inverted === true,
                    points: normalizePoints(layer.morphRegion?.points)
                },
                filters: (Array.isArray(layer.filters) ? layer.filters : [])
                    .filter(filter => { try { return (filter && filter.enabled !== false); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:callback:(Array.isArray(layer.filters) ? layer.filters : []) .filter@71', __javascriptError); throw __javascriptError; } })
                    .slice(0, 64)
                    .map(filter => { try { return (({
                        kind: filterKind(filter),
                        amount: Number(filter.amount),
                        secondaryAmount: Number(filter.secondaryAmount),
                        tertiaryAmount: Number(filter.tertiaryAmount),
                        residualOpacity: Number(filter.residualOpacity),
                        color: normalizeColor(filter.color, filterKind(filter) === 'chromakey' ? '#00ff00' : '#3b82f6'),
                        htmlExportSupport: String(filter.htmlExportSupport || 'Native')
                    })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:callback:(Array.isArray(layer.filters) ? layer.filters : []) .filter(filter => @73', __javascriptError); throw __javascriptError; } })
            })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:callback:(Array.isArray(config?.layers) ? config.layers : []) .filter(layer => @46', __javascriptError); throw __javascriptError; } });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:normalizedLayers@42', __javascriptError); throw __javascriptError; }}

    function cssFilter(filters) { try {
        let brightness = 1;
        let contrast = 1;
        let saturation = 1;
        let hue = 0;
        let blur = 0;
        let grayscale = 0;
        let sepia = 0;
        let invert = 0;
        for (const filter of filters) {
            switch (filter.kind) {
                case 'brightness': brightness *= clamp(filter.amount, 0, 4, 1); break;
                case 'contrast': contrast *= clamp(filter.amount, 0, 4, 1); break;
                case 'saturation': saturation *= clamp(filter.amount, 0, 4, 1); break;
                case 'huerotation': hue += clamp(filter.amount, -360, 360, 0); break;
                case 'blur': blur += clamp(filter.amount, 0, 64, 0); break;
                case 'grayscale': grayscale = Math.max(grayscale, clamp(filter.amount, 0, 1, 1)); break;
                case 'sepia': sepia = Math.max(sepia, clamp(filter.amount, 0, 1, 1)); break;
                case 'invert': invert = Math.max(invert, clamp(filter.amount, 0, 1, 1)); break;
            }
        }
        return `brightness(${brightness}) contrast(${contrast}) saturate(${saturation}) hue-rotate(${hue}deg) blur(${blur}px) grayscale(${grayscale}) sepia(${sepia}) invert(${invert})`;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:cssFilter@85', __javascriptError); throw __javascriptError; }}

    function parseHex(value) { try {
        const color = normalizeColor(value);
        return [
            parseInt(color.slice(1, 3), 16),
            parseInt(color.slice(3, 5), 16),
            parseInt(color.slice(5, 7), 16)
        ];
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:parseHex@109', __javascriptError); throw __javascriptError; }}

    function frameRect(video, width, height, fitMode) { try {
        const sourceWidth = Math.max(1, Number(video.videoWidth) || width);
        const sourceHeight = Math.max(1, Number(video.videoHeight) || height);
        if (String(fitMode || '').toLowerCase() === 'stretch' || String(fitMode || '').toLowerCase() === 'fill')
            return { x: 0, y: 0, width, height };
        const contain = String(fitMode || '').toLowerCase() === 'contain';
        const scale = contain
            ? Math.min(width / sourceWidth, height / sourceHeight)
            : Math.max(width / sourceWidth, height / sourceHeight);
        const renderedWidth = sourceWidth * scale;
        const renderedHeight = sourceHeight * scale;
        return {
            x: (width - renderedWidth) / 2,
            y: (height - renderedHeight) / 2,
            width: renderedWidth,
            height: renderedHeight
        };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:frameRect@118', __javascriptError); throw __javascriptError; }}

    function polygonLength(points) { try {
        if (!Array.isArray(points) || points.length < 2) return 0;
        let total = 0;
        for (let index = 0; index < points.length; index++) {
            const current = points[index];
            const next = points[(index + 1) % points.length];
            total += Math.hypot(next.x - current.x, next.y - current.y);
        }
        return total;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:polygonLength@137', __javascriptError); throw __javascriptError; }}

    function resamplePolygon(points, count) { try {
        if (!Array.isArray(points) || points.length < 3 || count < 3) return [];
        const lengths = [];
        let total = 0;
        for (let index = 0; index < points.length; index++) {
            const current = points[index];
            const next = points[(index + 1) % points.length];
            const length = Math.hypot(next.x - current.x, next.y - current.y);
            lengths.push(length);
            total += length;
        }
        if (total <= 1e-8) return Array.from({ length: count }, () => { try { return (({ ...points[0] })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:callback:Array.from@159', __javascriptError); throw __javascriptError; } });
        const result = [];
        for (let sample = 0; sample < count; sample++) {
            let distance = total * sample / count;
            let edge = 0;
            while (edge < lengths.length - 1 && distance > lengths[edge]) {
                distance -= lengths[edge];
                edge++;
            }
            const current = points[edge];
            const next = points[(edge + 1) % points.length];
            const amount = lengths[edge] > 1e-8 ? distance / lengths[edge] : 0;
            result.push({
                x: current.x + (next.x - current.x) * amount,
                y: current.y + (next.y - current.y) * amount
            });
        }
        return result;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:resamplePolygon@148', __javascriptError); throw __javascriptError; }}

    function morphPhase(layer, currentTime) { try {
        if (!layer.morphEnabled || layer.morphRegion.points.length < 3 || layer.region.points.length < 3) return 0;
        if (!layer.animateMorph) return layer.morphAmount;
        const origin = layer.hasTemporalRange ? layer.temporalStartSeconds : 0;
        const elapsed = Math.max(0, currentTime - origin);
        return (Math.sin(elapsed * layer.animationSpeed * Math.PI * 2 - Math.PI / 2) + 1) / 2;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:morphPhase@179', __javascriptError); throw __javascriptError; }}

    function activeRegion(layer, currentTime) { try {
        const source = layer.region;
        if (!layer.morphEnabled || source.points.length < 3 || layer.morphRegion.points.length < 3) return source;
        const count = Math.max(3, Math.min(256, Math.max(source.points.length, layer.morphRegion.points.length)));
        const from = resamplePolygon(source.points, count);
        const to = resamplePolygon(layer.morphRegion.points, count);
        const amount = morphPhase(layer, currentTime);
        return {
            inverted: source.inverted,
            points: from.map((point, index) => { try { return (({
                x: point.x + (to[index].x - point.x) * amount,
                y: point.y + (to[index].y - point.y) * amount
            })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:callback:from.map@196', __javascriptError); throw __javascriptError; } })
        };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:activeRegion@187', __javascriptError); throw __javascriptError; }}

    function regionPath(context, region, rect, outputWidth, outputHeight, offsetX = 0, offsetY = 0) { try {
        const points = region?.points || [];
        context.beginPath();
        if (region?.inverted && points.length >= 3) context.rect(0, 0, outputWidth, outputHeight);
        if (points.length >= 3) {
            context.moveTo(rect.x + points[0].x * rect.width + offsetX, rect.y + points[0].y * rect.height + offsetY);
            for (let index = 1; index < points.length; index++)
                context.lineTo(rect.x + points[index].x * rect.width + offsetX, rect.y + points[index].y * rect.height + offsetY);
            context.closePath();
        } else {
            context.rect(rect.x + offsetX, rect.y + offsetY, rect.width, rect.height);
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:regionPath@203', __javascriptError); throw __javascriptError; }}

    function drawBlobDepth(context, region, rect, outputWidth, outputHeight, layer) { try {
        if (layer.kind !== 'blob3d' || region.points.length < 3 || region.inverted) return;
        const maximum = Math.max(2, Math.round(Math.min(outputWidth, outputHeight) * layer.depth * .16));
        context.save();
        for (let step = maximum; step >= 1; step--) {
            const ratio = step / maximum;
            regionPath(context, region, rect, outputWidth, outputHeight, step * .55, step * .78);
            context.fillStyle = `rgba(2,12,32,${.14 + (1 - ratio) * .52})`;
            context.fill();
        }
        context.restore();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:drawBlobDepth@217', __javascriptError); throw __javascriptError; }}

    function finishBlobSurface(context, region, rect, outputWidth, outputHeight, layer) { try {
        if (layer.kind !== 'blob3d' || region.points.length < 3 || region.inverted) return;
        context.save();
        regionPath(context, region, rect, outputWidth, outputHeight);
        context.clip();
        const gradient = context.createRadialGradient(
            rect.x + rect.width * .28, rect.y + rect.height * .22, 0,
            rect.x + rect.width * .35, rect.y + rect.height * .32,
            Math.max(rect.width, rect.height) * (.5 + layer.roundness)
        );
        gradient.addColorStop(0, 'rgba(255,255,255,.52)');
        gradient.addColorStop(.42, 'rgba(125,211,252,.08)');
        gradient.addColorStop(1, 'rgba(2,6,23,.42)');
        context.globalCompositeOperation = 'source-atop';
        context.fillStyle = gradient;
        context.fillRect(0, 0, outputWidth, outputHeight);
        context.restore();
        context.save();
        regionPath(context, region, rect, outputWidth, outputHeight);
        context.strokeStyle = 'rgba(186,230,253,.72)';
        context.lineWidth = Math.max(1, Math.min(outputWidth, outputHeight) * (.002 + layer.roundness * .004));
        context.stroke();
        context.restore();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:finishBlobSurface@230', __javascriptError); throw __javascriptError; }}

    function applyChroma(context, width, height, filter) { try {
        if (!filter) return;
        let image;
        try { image = context.getImageData(0, 0, width, height); }
        catch { return; }
        const key = parseHex(filter.color);
        const similarity = clamp(filter.amount, 0, 1, .35) * 441.673;
        const smoothness = Math.max(1, clamp(filter.secondaryAmount, .001, 1, .12) * 441.673);
        const spill = clamp(filter.tertiaryAmount, 0, 1, .3);
        const residual = clamp(filter.residualOpacity, 0, 1, 0);
        const data = image.data;
        for (let index = 0; index < data.length; index += 4) {
            if (data[index + 3] === 0) continue;
            const dr = data[index] - key[0];
            const dg = data[index + 1] - key[1];
            const db = data[index + 2] - key[2];
            const distance = Math.sqrt(dr * dr + dg * dg + db * db);
            const keep = clamp((distance - similarity) / smoothness, 0, 1, 1);
            const nearKey = 1 - keep;
            if (spill > 0 && nearKey > 0) {
                const gray = data[index] * .299 + data[index + 1] * .587 + data[index + 2] * .114;
                const mix = nearKey * spill;
                data[index] = data[index] * (1 - mix) + gray * mix;
                data[index + 1] = data[index + 1] * (1 - mix) + gray * mix;
                data[index + 2] = data[index + 2] * (1 - mix) + gray * mix;
            }
            data[index + 3] *= residual + (1 - residual) * keep;
        }
        context.putImageData(image, 0, 0);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:applyChroma@255', __javascriptError); throw __javascriptError; }}

    function applyColorWash(context, width, height, filter) { try {
        if (!filter) return;
        context.save();
        context.globalCompositeOperation = 'source-atop';
        context.globalAlpha = clamp(filter.amount, 0, 1, .25);
        context.fillStyle = filter.color;
        context.fillRect(0, 0, width, height);
        context.restore();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:applyColorWash@286', __javascriptError); throw __javascriptError; }}

    function applyVignette(context, width, height, filter) { try {
        if (!filter) return;
        const amount = clamp(filter.amount, 0, 1, .45);
        const softness = clamp(filter.secondaryAmount, 0, 1, .55);
        const gradient = context.createRadialGradient(width / 2, height / 2, Math.min(width, height) * .1, width / 2, height / 2, Math.max(width, height) * .72);
        gradient.addColorStop(Math.max(0, softness - .25), 'rgba(0,0,0,0)');
        gradient.addColorStop(1, `rgba(0,0,0,${amount})`);
        context.save();
        context.globalCompositeOperation = 'source-atop';
        context.fillStyle = gradient;
        context.fillRect(0, 0, width, height);
        context.restore();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:applyVignette@296', __javascriptError); throw __javascriptError; }}

    function applyGrain(context, width, height, filter, time) { try {
        if (!filter) return;
        const amount = clamp(filter.amount, 0, 1, .12);
        if (amount <= 0) return;
        const step = Math.max(2, Math.round(Math.min(width, height) / 180));
        let seed = ((Number(filter.secondaryAmount) || 17) * 1009 + Math.floor(time * 30)) >>> 0;
        const random = () => { try {
            seed = (seed * 1664525 + 1013904223) >>> 0;
            return seed / 4294967296;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:random@316', __javascriptError); throw __javascriptError; }};
        context.save();
        context.globalCompositeOperation = 'soft-light';
        for (let y = 0; y < height; y += step) {
            for (let x = 0; x < width; x += step) {
                const value = Math.floor(random() * 255);
                context.fillStyle = `rgba(${value},${value},${value},${amount * .35})`;
                context.fillRect(x, y, step, step);
            }
        }
        context.restore();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:applyGrain@310', __javascriptError); throw __javascriptError; }}

    function layerIsActive(layer, currentTime) { try {
        if (!layer.hasTemporalRange) return true;
        return currentTime >= layer.temporalStartSeconds && currentTime <= layer.temporalEndSeconds;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:layerIsActive@332', __javascriptError); throw __javascriptError; }}

    function needsCanvas(layers, forceCanvas) { try {
        if (forceCanvas) return true;
        if (layers.length !== 1) return layers.length > 0;
        const layer = layers[0];
        return layer.kind === 'blob3d' || layer.opacity < .999 || layer.blendMode !== 'source-over' || layer.hasTemporalRange || layer.region.points.length >= 3 || layer.filters.length > 0;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:needsCanvas@337', __javascriptError); throw __javascriptError; }}

    function createRuntime(key, video, canvas, config) { try {
        const context = canvas.getContext('2d', { alpha: true, willReadFrequently: true });
        const layerCanvas = document.createElement('canvas');
        const layerContext = layerCanvas.getContext('2d', { alpha: true, willReadFrequently: true });
        let currentConfig = config || {};
        let layers = normalizedLayers(currentConfig);
        let frame = 0;
        let stopped = false;
        let lastWidth = 0;
        let lastHeight = 0;

        const resize = () => { try {
            const pixelRatio = Math.min(2, Math.max(1, Number(window.devicePixelRatio) || 1));
            const cssWidth = Math.max(2, canvas.clientWidth || video.clientWidth || 640);
            const cssHeight = Math.max(2, canvas.clientHeight || video.clientHeight || 360);
            const maximumPixels = 1920 * 1080 * 2;
            let width = Math.round(cssWidth * pixelRatio);
            let height = Math.round(cssHeight * pixelRatio);
            const pixels = width * height;
            if (pixels > maximumPixels) {
                const scale = Math.sqrt(maximumPixels / pixels);
                width = Math.max(2, Math.round(width * scale));
                height = Math.max(2, Math.round(height * scale));
            }
            if (width === lastWidth && height === lastHeight) return;
            lastWidth = width;
            lastHeight = height;
            canvas.width = width;
            canvas.height = height;
            layerCanvas.width = width;
            layerCanvas.height = height;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:resize@355', __javascriptError); throw __javascriptError; }};

        const draw = () => { try {
            if (stopped) return;
            resize();
            const width = canvas.width;
            const height = canvas.height;
            const active = needsCanvas(layers, currentConfig.forceCanvas === true);
            canvas.classList.toggle('active', active);
            video.classList.toggle('video-effect-source-hidden', active);
            if (active && context && layerContext && video.readyState >= 2) {
                context.clearRect(0, 0, width, height);
                const fit = currentConfig.fitMode || 'cover';
                const rect = frameRect(video, width, height, fit);
                const currentTime = Math.max(0, Number(video.currentTime) || 0);
                for (const layer of layers) {
                    if (!layerIsActive(layer, currentTime)) continue;
                    const region = activeRegion(layer, currentTime);
                    layerContext.setTransform(1, 0, 0, 1, 0, 0);
                    layerContext.clearRect(0, 0, width, height);
                    drawBlobDepth(layerContext, region, rect, width, height, layer);
                    layerContext.save();
                    regionPath(layerContext, region, rect, width, height);
                    layerContext.clip(region.inverted && region.points.length >= 3 ? 'evenodd' : 'nonzero');
                    layerContext.filter = cssFilter(layer.filters);
                    try { layerContext.drawImage(video, rect.x, rect.y, rect.width, rect.height); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:suppressed-catch@400', __caughtJavaScriptError);  }
                    layerContext.restore();
                    layerContext.filter = 'none';

                    applyChroma(layerContext, width, height, layer.filters.find(filter => { try { return (filter.kind === 'chromakey'); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:callback:layer.filters.find@404', __javascriptError); throw __javascriptError; } }));
                    applyColorWash(layerContext, width, height, layer.filters.find(filter => { try { return (filter.kind === 'colorwash'); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:callback:layer.filters.find@405', __javascriptError); throw __javascriptError; } }));
                    applyVignette(layerContext, width, height, layer.filters.find(filter => { try { return (filter.kind === 'vignette'); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:callback:layer.filters.find@406', __javascriptError); throw __javascriptError; } }));
                    applyGrain(layerContext, width, height, layer.filters.find(filter => { try { return (filter.kind === 'grain'); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:callback:layer.filters.find@407', __javascriptError); throw __javascriptError; } }), currentTime);
                    finishBlobSurface(layerContext, region, rect, width, height, layer);

                    context.save();
                    context.globalAlpha = layer.opacity;
                    context.globalCompositeOperation = layer.blendMode;
                    context.drawImage(layerCanvas, 0, 0);
                    context.restore();
                }
            }
            if (typeof video.requestVideoFrameCallback === 'function')
                frame = video.requestVideoFrameCallback(draw);
            else
                frame = requestAnimationFrame(draw);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:draw@377', __javascriptError); throw __javascriptError; }};

        const runtime = {
            update(nextConfig) { try {
                currentConfig = nextConfig || {};
                layers = normalizedLayers(currentConfig);
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:update@424', __javascriptError); throw __javascriptError; }},
            stop() { try {
                if (stopped) return;
                stopped = true;
                if (typeof video.cancelVideoFrameCallback === 'function') {
                    try { video.cancelVideoFrameCallback(frame); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:suppressed-catch@432', __caughtJavaScriptError);  }
                } else cancelAnimationFrame(frame);
                canvas.classList.remove('active');
                video.classList.remove('video-effect-source-hidden');
                context?.clearRect(0, 0, canvas.width, canvas.height);
                runtimes.delete(key);
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:stop@428', __javascriptError); throw __javascriptError; }}
        };
        draw();
        return runtime;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:createRuntime@344', __javascriptError); throw __javascriptError; }}

    function install(key, video, canvas, config) { try {
        const id = String(key || canvas?.id || video?.id || 'video-effect');
        if (!(video instanceof HTMLVideoElement) || !(canvas instanceof HTMLCanvasElement)) return null;
        const existing = runtimes.get(id);
        if (existing) {
            existing.update(config);
            return existing;
        }
        const runtime = createRuntime(id, video, canvas, config);
        runtimes.set(id, runtime);
        return runtime;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:install@444', __javascriptError); throw __javascriptError; }}

    function installById(key, videoId, canvasId, config) { try {
        const video = document.getElementById(String(videoId || ''));
        const canvas = document.getElementById(String(canvasId || ''));
        return !!install(key, video, canvas, config);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:installById@457', __javascriptError); throw __javascriptError; }}

    function update(key, config) { try {
        const runtime = runtimes.get(String(key || ''));
        runtime?.update(config);
        return !!runtime;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:update@463', __javascriptError); throw __javascriptError; }}

    function dispose(key) { try {
        runtimes.get(String(key || ''))?.stop();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:dispose@469', __javascriptError); throw __javascriptError; }}

    window.publisherVideoEffects = { install, installById, update, dispose, normalizedLayers, resamplePolygon, activeRegion };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/videoEffectRuntime.js:ArrowFunction@2', __javascriptError); throw __javascriptError; }})();

// Guard exported browser namespaces after the file has initialized.
publisherStudioDiagnostics.guardObject("publisherVideoEffects", window.publisherVideoEffects);
