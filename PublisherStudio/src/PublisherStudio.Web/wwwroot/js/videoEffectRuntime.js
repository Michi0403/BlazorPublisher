(() => {
    const runtimes = new Map();

    const clamp = (value, minimum, maximum, fallback = minimum) => {
        const number = Number(value);
        return Math.max(minimum, Math.min(maximum, Number.isFinite(number) ? number : fallback));
    };

    const normalizeColor = (value, fallback = '#00ff00') => /^#[0-9a-f]{6}$/i.test(String(value || ''))
        ? String(value).toLowerCase()
        : fallback;

    function filterKind(filter) {
        return String(filter?.kind || '').replace(/[^a-z]/gi, '').toLowerCase();
    }

    function blendMode(value) {
        switch (String(value || '').toLowerCase()) {
            case 'multiply': return 'multiply';
            case 'screen': return 'screen';
            case 'overlay': return 'overlay';
            case 'darken': return 'darken';
            case 'lighten': return 'lighten';
            default: return 'source-over';
        }
    }

    function normalizedLayers(config) {
        return (Array.isArray(config?.layers) ? config.layers : [])
            .filter(layer => layer && layer.visible !== false)
            .slice(0, 64)
            .map((layer, layerIndex) => ({
                id: String(layer.id || layerIndex),
                name: String(layer.name || `Video layer ${layerIndex + 1}`),
                opacity: clamp(layer.opacity, 0, 1, 1),
                blendMode: blendMode(layer.blendMode),
                hasTemporalRange: layer.hasTemporalRange === true,
                temporalStartSeconds: Math.max(0, Number(layer.temporalStartSeconds) || 0),
                temporalEndSeconds: Math.max(0, Number(layer.temporalEndSeconds) || 0),
                region: {
                    inverted: layer.region?.inverted === true,
                    points: (Array.isArray(layer.region?.points) ? layer.region.points : [])
                        .slice(0, 256)
                        .map(point => ({ x: clamp(point?.x, 0, 1, 0), y: clamp(point?.y, 0, 1, 0) }))
                },
                filters: (Array.isArray(layer.filters) ? layer.filters : [])
                    .filter(filter => filter && filter.enabled !== false)
                    .slice(0, 64)
                    .map(filter => ({
                        kind: filterKind(filter),
                        amount: Number(filter.amount),
                        secondaryAmount: Number(filter.secondaryAmount),
                        tertiaryAmount: Number(filter.tertiaryAmount),
                        residualOpacity: Number(filter.residualOpacity),
                        color: normalizeColor(filter.color, filterKind(filter) === 'chromakey' ? '#00ff00' : '#3b82f6')
                    }))
            }));
    }

    function cssFilter(filters) {
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
    }

    function parseHex(value) {
        const color = normalizeColor(value);
        return [
            parseInt(color.slice(1, 3), 16),
            parseInt(color.slice(3, 5), 16),
            parseInt(color.slice(5, 7), 16)
        ];
    }

    function frameRect(video, width, height, fitMode) {
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
    }

    function regionPath(context, region, rect, outputWidth, outputHeight) {
        const points = region?.points || [];
        context.beginPath();
        if (region?.inverted && points.length >= 3) context.rect(0, 0, outputWidth, outputHeight);
        if (points.length >= 3) {
            context.moveTo(rect.x + points[0].x * rect.width, rect.y + points[0].y * rect.height);
            for (let index = 1; index < points.length; index++)
                context.lineTo(rect.x + points[index].x * rect.width, rect.y + points[index].y * rect.height);
            context.closePath();
        } else {
            context.rect(rect.x, rect.y, rect.width, rect.height);
        }
    }

    function applyChroma(context, width, height, filter) {
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
    }

    function applyColorWash(context, width, height, filter) {
        if (!filter) return;
        context.save();
        context.globalCompositeOperation = 'source-atop';
        context.globalAlpha = clamp(filter.amount, 0, 1, .25);
        context.fillStyle = filter.color;
        context.fillRect(0, 0, width, height);
        context.restore();
    }

    function applyVignette(context, width, height, filter) {
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
    }

    function applyGrain(context, width, height, filter, time) {
        if (!filter) return;
        const amount = clamp(filter.amount, 0, 1, .12);
        if (amount <= 0) return;
        const step = Math.max(2, Math.round(Math.min(width, height) / 180));
        let seed = ((Number(filter.secondaryAmount) || 17) * 1009 + Math.floor(time * 30)) >>> 0;
        const random = () => {
            seed = (seed * 1664525 + 1013904223) >>> 0;
            return seed / 4294967296;
        };
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
    }

    function layerIsActive(layer, currentTime) {
        if (!layer.hasTemporalRange) return true;
        return currentTime >= layer.temporalStartSeconds && currentTime <= layer.temporalEndSeconds;
    }

    function needsCanvas(layers, forceCanvas) {
        if (forceCanvas) return true;
        if (layers.length !== 1) return layers.length > 0;
        const layer = layers[0];
        return layer.opacity < .999 || layer.blendMode !== 'source-over' || layer.hasTemporalRange || layer.region.points.length >= 3 || layer.filters.length > 0;
    }

    function createRuntime(key, video, canvas, config) {
        const context = canvas.getContext('2d', { alpha: true, willReadFrequently: true });
        const layerCanvas = document.createElement('canvas');
        const layerContext = layerCanvas.getContext('2d', { alpha: true, willReadFrequently: true });
        let currentConfig = config || {};
        let layers = normalizedLayers(currentConfig);
        let frame = 0;
        let stopped = false;
        let lastWidth = 0;
        let lastHeight = 0;

        const resize = () => {
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
        };

        const draw = () => {
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
                    layerContext.setTransform(1, 0, 0, 1, 0, 0);
                    layerContext.clearRect(0, 0, width, height);
                    layerContext.save();
                    regionPath(layerContext, layer.region, rect, width, height);
                    layerContext.clip(layer.region.inverted && layer.region.points.length >= 3 ? 'evenodd' : 'nonzero');
                    layerContext.filter = cssFilter(layer.filters);
                    try { layerContext.drawImage(video, rect.x, rect.y, rect.width, rect.height); } catch { }
                    layerContext.restore();
                    layerContext.filter = 'none';

                    applyChroma(layerContext, width, height, layer.filters.find(filter => filter.kind === 'chromakey'));
                    applyColorWash(layerContext, width, height, layer.filters.find(filter => filter.kind === 'colorwash'));
                    applyVignette(layerContext, width, height, layer.filters.find(filter => filter.kind === 'vignette'));
                    applyGrain(layerContext, width, height, layer.filters.find(filter => filter.kind === 'grain'), currentTime);

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
        };

        const runtime = {
            update(nextConfig) {
                currentConfig = nextConfig || {};
                layers = normalizedLayers(currentConfig);
            },
            stop() {
                if (stopped) return;
                stopped = true;
                if (typeof video.cancelVideoFrameCallback === 'function') {
                    try { video.cancelVideoFrameCallback(frame); } catch { }
                } else cancelAnimationFrame(frame);
                canvas.classList.remove('active');
                video.classList.remove('video-effect-source-hidden');
                context?.clearRect(0, 0, canvas.width, canvas.height);
                runtimes.delete(key);
            }
        };
        draw();
        return runtime;
    }

    function install(key, video, canvas, config) {
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
    }

    function installById(key, videoId, canvasId, config) {
        const video = document.getElementById(String(videoId || ''));
        const canvas = document.getElementById(String(canvasId || ''));
        return !!install(key, video, canvas, config);
    }

    function update(key, config) {
        const runtime = runtimes.get(String(key || ''));
        runtime?.update(config);
        return !!runtime;
    }

    function dispose(key) {
        runtimes.get(String(key || ''))?.stop();
    }

    window.publisherVideoEffects = { install, installById, update, dispose, normalizedLayers };
})();
