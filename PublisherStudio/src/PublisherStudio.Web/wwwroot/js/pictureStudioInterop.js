const editors = new Map();
const imageCache = new Map();
const proceduralCache = new Map();

const layerKinds = ["raster", "text", "shape", "fill", "render"];
const blendModes = ["source-over", "multiply", "screen", "overlay", "darken", "lighten"];
const rasterFits = ["stretch", "contain", "cover"];
const shapeKinds = ["rectangle", "roundedRectangle", "ellipse", "line"];
const fillKinds = ["solid", "linearGradient", "radialGradient"];
const renderKinds = ["clouds", "noise", "stripes", "vignette"];
const textAlignments = ["left", "center", "right"];

function enumName(value, names, fallback) {
    if (typeof value === "string") return value;
    if (Number.isInteger(value) && value >= 0 && value < names.length) return names[value];
    return fallback;
}

function layerKind(layer) {
    const discriminator = layer?.$type;
    if (typeof discriminator === "string") return discriminator.toLowerCase();
    return enumName(layer?.kind, layerKinds, "shape").toLowerCase();
}

function blendMode(value) {
    if (typeof value === "string") {
        const name = value.toLowerCase();
        return name === "normal" ? "source-over" : name;
    }
    return enumName(value, blendModes, "source-over");
}

function clamp(value, minimum, maximum) {
    const number = Number(value);
    if (!Number.isFinite(number)) return minimum;
    return Math.max(minimum, Math.min(maximum, number));
}

function normalizeDocument(document) {
    return {
        ...document,
        widthPx: Math.round(clamp(document?.widthPx, 16, 8192)),
        heightPx: Math.round(clamp(document?.heightPx, 16, 8192)),
        zoom: clamp(document?.zoom ?? .65, .05, 4),
        gridSpacingPx: Math.round(clamp(document?.gridSpacingPx ?? 25, 2, 1000)),
        background: document?.background || "transparent",
        layers: Array.isArray(document?.layers) ? document.layers : []
    };
}

function cloneDocument(document) {
    return JSON.parse(JSON.stringify(normalizeDocument(document)));
}

function createCanvas(width, height) {
    const canvas = document.createElement("canvas");
    canvas.width = Math.max(1, Math.round(width));
    canvas.height = Math.max(1, Math.round(height));
    return canvas;
}

function loadImage(dataUrl) {
    if (!dataUrl) return Promise.resolve(null);
    if (imageCache.has(dataUrl)) return imageCache.get(dataUrl);
    const promise = new Promise((resolve, reject) => {
        const image = new Image();
        image.decoding = "async";
        image.onload = () => resolve(image);
        image.onerror = () => reject(new Error("The image layer could not be decoded."));
        image.src = dataUrl;
    });
    imageCache.set(dataUrl, promise);
    return promise;
}

function parseColor(value, fallback = [0, 0, 0, 255]) {
    if (typeof value !== "string") return fallback;
    const text = value.trim();
    if (text === "transparent") return [0, 0, 0, 0];
    if (/^#[0-9a-f]{3}$/i.test(text)) {
        return [
            parseInt(text[1] + text[1], 16),
            parseInt(text[2] + text[2], 16),
            parseInt(text[3] + text[3], 16),
            255
        ];
    }
    if (/^#[0-9a-f]{6}$/i.test(text)) {
        return [parseInt(text.slice(1, 3), 16), parseInt(text.slice(3, 5), 16), parseInt(text.slice(5, 7), 16), 255];
    }
    if (/^#[0-9a-f]{8}$/i.test(text)) {
        return [
            parseInt(text.slice(1, 3), 16), parseInt(text.slice(3, 5), 16),
            parseInt(text.slice(5, 7), 16), parseInt(text.slice(7, 9), 16)
        ];
    }
    return fallback;
}

function cssColor(value, fallback = "#000000") {
    if (typeof value !== "string" || !value.trim()) return fallback;
    return value;
}

function mixColor(first, second, amount) {
    amount = clamp(amount, 0, 1);
    return [
        Math.round(first[0] + (second[0] - first[0]) * amount),
        Math.round(first[1] + (second[1] - first[1]) * amount),
        Math.round(first[2] + (second[2] - first[2]) * amount),
        Math.round(first[3] + (second[3] - first[3]) * amount)
    ];
}

function layerFilter(layer) {
    return [
        `brightness(${clamp(layer.brightness ?? 1, 0, 3)})`,
        `contrast(${clamp(layer.contrast ?? 1, 0, 3)})`,
        `saturate(${clamp(layer.saturation ?? 1, 0, 3)})`,
        `hue-rotate(${clamp(layer.hueRotation ?? 0, -360, 360)}deg)`,
        `blur(${clamp(layer.blur ?? 0, 0, 100)}px)`,
        `grayscale(${clamp(layer.grayscale ?? 0, 0, 1)})`,
        `sepia(${clamp(layer.sepia ?? 0, 0, 1)})`,
        `invert(${clamp(layer.invert ?? 0, 0, 1)})`
    ].join(" ");
}

function beginLayer(ctx, layer) {
    const width = Math.max(1, Number(layer.width) || 1);
    const height = Math.max(1, Number(layer.height) || 1);
    const x = Number(layer.x) || 0;
    const y = Number(layer.y) || 0;
    const rotation = (Number(layer.rotation) || 0) * Math.PI / 180;
    ctx.save();
    ctx.globalAlpha = clamp(layer.opacity ?? 1, 0, 1);
    ctx.globalCompositeOperation = blendMode(layer.blendMode);
    ctx.filter = layerFilter(layer);
    ctx.translate(x + width / 2, y + height / 2);
    ctx.rotate(rotation);
    return { width, height };
}

function endLayer(ctx) {
    ctx.restore();
}

function roundedRectanglePath(ctx, x, y, width, height, radius) {
    radius = Math.max(0, Math.min(radius, Math.abs(width) / 2, Math.abs(height) / 2));
    ctx.beginPath();
    ctx.moveTo(x + radius, y);
    ctx.lineTo(x + width - radius, y);
    ctx.quadraticCurveTo(x + width, y, x + width, y + radius);
    ctx.lineTo(x + width, y + height - radius);
    ctx.quadraticCurveTo(x + width, y + height, x + width - radius, y + height);
    ctx.lineTo(x + radius, y + height);
    ctx.quadraticCurveTo(x, y + height, x, y + height - radius);
    ctx.lineTo(x, y + radius);
    ctx.quadraticCurveTo(x, y, x + radius, y);
    ctx.closePath();
}

function drawImageWithFit(ctx, image, width, height, fit) {
    if (!image) return;
    fit = enumName(fit, rasterFits, "contain").toLowerCase();
    if (fit === "stretch") {
        ctx.drawImage(image, -width / 2, -height / 2, width, height);
        return;
    }
    const imageRatio = image.naturalWidth / Math.max(1, image.naturalHeight);
    const frameRatio = width / Math.max(1, height);
    const cover = fit === "cover";
    let drawWidth;
    let drawHeight;
    if ((imageRatio > frameRatio) !== cover) {
        drawWidth = width;
        drawHeight = width / imageRatio;
    } else {
        drawHeight = height;
        drawWidth = height * imageRatio;
    }
    ctx.drawImage(image, -drawWidth / 2, -drawHeight / 2, drawWidth, drawHeight);
}

async function drawRasterLayer(ctx, layer) {
    const image = await loadImage(layer.dataUrl);
    const { width, height } = beginLayer(ctx, layer);
    const scratch = createCanvas(Math.max(1, Math.round(width)), Math.max(1, Math.round(height)));
    const scratchContext = scratch.getContext("2d");
    scratchContext.save();
    scratchContext.translate(scratch.width / 2, scratch.height / 2);
    scratchContext.scale(layer.flipHorizontal ? -1 : 1, layer.flipVertical ? -1 : 1);
    drawImageWithFit(scratchContext, image, scratch.width, scratch.height, layer.fitMode);
    scratchContext.restore();
    const tintOpacity = clamp(layer.tintOpacity ?? 0, 0, 1);
    if (tintOpacity > .001) {
        scratchContext.save();
        scratchContext.globalCompositeOperation = "source-atop";
        scratchContext.globalAlpha = tintOpacity;
        scratchContext.fillStyle = cssColor(layer.tintColor, "#2f75b5");
        scratchContext.fillRect(0, 0, scratch.width, scratch.height);
        scratchContext.restore();
    }
    ctx.drawImage(scratch, -width / 2, -height / 2, width, height);
    endLayer(ctx);
}

function wrapText(ctx, text, maximumWidth) {
    const paragraphs = String(text ?? "").replace(/\r/g, "").split("\n");
    const lines = [];
    for (const paragraph of paragraphs) {
        const words = paragraph.split(/\s+/).filter(Boolean);
        if (words.length === 0) {
            lines.push("");
            continue;
        }
        let line = words[0];
        for (let index = 1; index < words.length; index++) {
            const candidate = `${line} ${words[index]}`;
            if (ctx.measureText(candidate).width <= maximumWidth) line = candidate;
            else {
                lines.push(line);
                line = words[index];
            }
        }
        lines.push(line);
    }
    return lines;
}

function drawTextLayer(ctx, layer) {
    const { width, height } = beginLayer(ctx, layer);
    const fontSize = clamp(layer.fontSizePx ?? 72, 4, 1024);
    const fontStyle = `${layer.italic ? "italic " : ""}${layer.bold ? "700 " : "400 "}${fontSize}px ${layer.fontFamily || "Segoe UI"}`;
    ctx.font = fontStyle;
    ctx.textBaseline = "top";
    const alignment = enumName(layer.alignment, textAlignments, "center").toLowerCase();
    ctx.textAlign = alignment;
    const x = alignment === "left" ? -width / 2 : alignment === "right" ? width / 2 : 0;
    const lineHeight = fontSize * 1.18;
    const lines = wrapText(ctx, layer.text, Math.max(1, width));
    const totalHeight = lines.length * lineHeight;
    let y = Math.max(-height / 2, -totalHeight / 2);
    ctx.save();
    ctx.beginPath();
    ctx.rect(-width / 2, -height / 2, width, height);
    ctx.clip();
    if (layer.shadowEnabled) {
        ctx.shadowColor = cssColor(layer.shadowColor, "#00000080");
        ctx.shadowBlur = clamp(layer.shadowBlurPx ?? 8, 0, 200);
        ctx.shadowOffsetX = Number(layer.shadowOffsetXPx) || 0;
        ctx.shadowOffsetY = Number(layer.shadowOffsetYPx) || 0;
    }
    for (const line of lines) {
        if (layer.outlineWidthPx > 0 && layer.outlineColor !== "transparent") {
            ctx.lineWidth = clamp(layer.outlineWidthPx, 0, 64) * 2;
            ctx.lineJoin = "round";
            ctx.strokeStyle = cssColor(layer.outlineColor, "#000000");
            ctx.strokeText(line, x, y, width);
        }
        ctx.fillStyle = cssColor(layer.fillColor, "#17365d");
        ctx.fillText(line, x, y, width);
        y += lineHeight;
        if (y > height / 2) break;
    }
    ctx.restore();
    endLayer(ctx);
}

function drawShapeLayer(ctx, layer) {
    const { width, height } = beginLayer(ctx, layer);
    const shape = enumName(layer.shape, shapeKinds, "rectangle").toLowerCase();
    const x = -width / 2;
    const y = -height / 2;
    ctx.fillStyle = cssColor(layer.fillColor, "#60a5fa");
    ctx.strokeStyle = cssColor(layer.strokeColor, "#1d4ed8");
    ctx.lineWidth = clamp(layer.strokeWidthPx ?? 3, 0, 200);
    if (shape === "ellipse") {
        ctx.beginPath();
        ctx.ellipse(0, 0, width / 2, height / 2, 0, 0, Math.PI * 2);
    } else if (shape === "line") {
        ctx.beginPath();
        ctx.moveTo(-width / 2, 0);
        ctx.lineTo(width / 2, 0);
        if (ctx.lineWidth > 0) ctx.stroke();
        endLayer(ctx);
        return;
    } else if (shape === "roundedrectangle") {
        roundedRectanglePath(ctx, x, y, width, height, clamp(layer.cornerRadiusPx ?? 24, 0, 2000));
    } else {
        ctx.beginPath();
        ctx.rect(x, y, width, height);
    }
    if (layer.fillColor !== "transparent") ctx.fill();
    if (ctx.lineWidth > 0 && layer.strokeColor !== "transparent") ctx.stroke();
    endLayer(ctx);
}

function createFillStyle(ctx, layer, width, height) {
    const fillKind = enumName(layer.fillKind, fillKinds, "linearGradient").toLowerCase();
    const first = cssColor(layer.primaryColor, "#dbeafe");
    const second = cssColor(layer.secondaryColor, "#6366f1");
    if (fillKind === "solid") return first;
    if (fillKind === "radialgradient") {
        const radius = Math.max(width, height) * .7;
        const gradient = ctx.createRadialGradient(0, 0, 0, 0, 0, radius);
        gradient.addColorStop(0, first);
        gradient.addColorStop(1, second);
        return gradient;
    }
    const angle = (Number(layer.angleDegrees) || 0) * Math.PI / 180;
    const distance = Math.abs(width * Math.cos(angle)) + Math.abs(height * Math.sin(angle));
    const dx = Math.cos(angle) * distance / 2;
    const dy = Math.sin(angle) * distance / 2;
    const gradient = ctx.createLinearGradient(-dx, -dy, dx, dy);
    gradient.addColorStop(0, first);
    gradient.addColorStop(1, second);
    return gradient;
}

function drawFillLayer(ctx, layer) {
    const { width, height } = beginLayer(ctx, layer);
    ctx.fillStyle = createFillStyle(ctx, layer, width, height);
    ctx.fillRect(-width / 2, -height / 2, width, height);
    endLayer(ctx);
}

function hashNoise(x, y, seed) {
    let value = Math.imul(x | 0, 374761393) + Math.imul(y | 0, 668265263) + Math.imul(seed | 0, 1442695041);
    value = (value ^ (value >>> 13)) | 0;
    value = Math.imul(value, 1274126177);
    value = value ^ (value >>> 16);
    return (value >>> 0) / 4294967295;
}

function smoothstep(value) {
    return value * value * (3 - 2 * value);
}

function valueNoise(x, y, seed) {
    const x0 = Math.floor(x);
    const y0 = Math.floor(y);
    const tx = smoothstep(x - x0);
    const ty = smoothstep(y - y0);
    const a = hashNoise(x0, y0, seed);
    const b = hashNoise(x0 + 1, y0, seed);
    const c = hashNoise(x0, y0 + 1, seed);
    const d = hashNoise(x0 + 1, y0 + 1, seed);
    const first = a + (b - a) * tx;
    const second = c + (d - c) * tx;
    return first + (second - first) * ty;
}

function fractalNoise(x, y, seed, detail) {
    let sum = 0;
    let amplitude = 1;
    let frequency = 1;
    let total = 0;
    for (let octave = 0; octave < detail; octave++) {
        sum += valueNoise(x * frequency, y * frequency, seed + octave * 997) * amplitude;
        total += amplitude;
        amplitude *= .5;
        frequency *= 2;
    }
    return sum / Math.max(.0001, total);
}

function proceduralKey(layer, width, height) {
    return JSON.stringify([
        layer.renderKind, layer.primaryColor, layer.secondaryColor, layer.seed, layer.scale,
        layer.detail, layer.softness, layer.renderContrast, layer.angleDegrees, layer.stripeWidthPx,
        Math.round(width), Math.round(height)
    ]);
}

function createNoiseOrClouds(layer, width, height, clouds) {
    const maximum = 480;
    const ratio = width / Math.max(1, height);
    const renderWidth = ratio >= 1 ? maximum : Math.max(64, Math.round(maximum * ratio));
    const renderHeight = ratio >= 1 ? Math.max(64, Math.round(maximum / ratio)) : maximum;
    const canvas = createCanvas(renderWidth, renderHeight);
    const ctx = canvas.getContext("2d", { willReadFrequently: false });
    const image = ctx.createImageData(renderWidth, renderHeight);
    const first = parseColor(layer.primaryColor, [255, 255, 255, 255]);
    const second = parseColor(layer.secondaryColor, [96, 165, 250, 255]);
    const seed = Number(layer.seed) || 1;
    const scale = clamp(layer.scale ?? 90, 4, 2000);
    const detail = Math.round(clamp(layer.detail ?? 4, 1, 8));
    const softness = clamp(layer.softness ?? .6, 0, 1);
    const contrast = clamp(layer.renderContrast ?? 1, .1, 5);
    const scaleX = width / scale;
    const scaleY = height / scale;
    for (let y = 0; y < renderHeight; y++) {
        for (let x = 0; x < renderWidth; x++) {
            let amount;
            if (clouds) {
                amount = fractalNoise(x / renderWidth * scaleX, y / renderHeight * scaleY, seed, detail);
                amount = .5 + (amount - .5) * contrast;
                amount = amount * (1 - softness * .35) + .5 * softness * .35;
            } else {
                amount = hashNoise(x, y, seed);
                amount = .5 + (amount - .5) * contrast;
            }
            amount = clamp(amount, 0, 1);
            const color = mixColor(first, second, amount);
            const index = (y * renderWidth + x) * 4;
            image.data[index] = color[0];
            image.data[index + 1] = color[1];
            image.data[index + 2] = color[2];
            image.data[index + 3] = color[3];
        }
    }
    ctx.putImageData(image, 0, 0);
    return canvas;
}

function getProceduralCanvas(layer, width, height) {
    const key = proceduralKey(layer, width, height);
    if (proceduralCache.has(key)) return proceduralCache.get(key);
    const kind = enumName(layer.renderKind, renderKinds, "clouds").toLowerCase();
    const canvas = kind === "noise"
        ? createNoiseOrClouds(layer, width, height, false)
        : createNoiseOrClouds(layer, width, height, true);
    proceduralCache.set(key, canvas);
    if (proceduralCache.size > 40) proceduralCache.delete(proceduralCache.keys().next().value);
    return canvas;
}

function drawRenderLayer(ctx, layer) {
    const { width, height } = beginLayer(ctx, layer);
    const kind = enumName(layer.renderKind, renderKinds, "clouds").toLowerCase();
    if (kind === "stripes") {
        const angle = (Number(layer.angleDegrees) || 0) * Math.PI / 180;
        const stripeWidth = clamp(layer.stripeWidthPx ?? 32, 1, 1000);
        const diagonal = Math.hypot(width, height);
        ctx.rotate(angle);
        ctx.fillStyle = cssColor(layer.primaryColor, "#ffffff");
        ctx.fillRect(-diagonal, -diagonal, diagonal * 2, diagonal * 2);
        ctx.fillStyle = cssColor(layer.secondaryColor, "#60a5fa");
        for (let x = -diagonal * 2; x < diagonal * 2; x += stripeWidth * 2)
            ctx.fillRect(x, -diagonal * 2, stripeWidth, diagonal * 4);
    } else if (kind === "vignette") {
        const gradient = ctx.createRadialGradient(0, 0, 0, 0, 0, Math.max(width, height) * .72);
        gradient.addColorStop(0, cssColor(layer.primaryColor, "#ffffff"));
        gradient.addColorStop(1, cssColor(layer.secondaryColor, "#000000"));
        ctx.fillStyle = gradient;
        ctx.fillRect(-width / 2, -height / 2, width, height);
    } else {
        const procedural = getProceduralCanvas(layer, width, height);
        ctx.imageSmoothingEnabled = true;
        ctx.imageSmoothingQuality = "high";
        ctx.drawImage(procedural, -width / 2, -height / 2, width, height);
    }
    endLayer(ctx);
}

async function drawLayer(ctx, layer) {
    if (!layer || layer.visible === false || clamp(layer.opacity ?? 1, 0, 1) <= 0) return;
    switch (layerKind(layer)) {
        case "raster": await drawRasterLayer(ctx, layer); break;
        case "text": drawTextLayer(ctx, layer); break;
        case "fill": drawFillLayer(ctx, layer); break;
        case "render": drawRenderLayer(ctx, layer); break;
        default: drawShapeLayer(ctx, layer); break;
    }
}

function drawBackground(ctx, document, forceOpaque = false) {
    const value = document.background || "transparent";
    if (value === "transparent" && !forceOpaque) return;
    ctx.save();
    ctx.fillStyle = value === "transparent" ? "#ffffff" : cssColor(value, "#ffffff");
    ctx.fillRect(0, 0, document.widthPx, document.heightPx);
    ctx.restore();
}

function drawGrid(ctx, document, zoom) {
    if (!document.gridVisible) return;
    const spacing = Math.max(2, Number(document.gridSpacingPx) || 25);
    ctx.save();
    ctx.strokeStyle = "rgba(15, 23, 42, .13)";
    ctx.lineWidth = Math.max(.35, 1 / Math.max(.05, zoom));
    ctx.beginPath();
    for (let x = spacing; x < document.widthPx; x += spacing) {
        ctx.moveTo(x, 0);
        ctx.lineTo(x, document.heightPx);
    }
    for (let y = spacing; y < document.heightPx; y += spacing) {
        ctx.moveTo(0, y);
        ctx.lineTo(document.widthPx, y);
    }
    ctx.stroke();
    ctx.restore();
}

function localToWorld(layer, localX, localY) {
    const width = Number(layer.width) || 1;
    const height = Number(layer.height) || 1;
    const centerX = (Number(layer.x) || 0) + width / 2;
    const centerY = (Number(layer.y) || 0) + height / 2;
    const angle = (Number(layer.rotation) || 0) * Math.PI / 180;
    const cos = Math.cos(angle);
    const sin = Math.sin(angle);
    return {
        x: centerX + localX * cos - localY * sin,
        y: centerY + localX * sin + localY * cos
    };
}

function worldToLocal(layer, worldX, worldY) {
    const width = Number(layer.width) || 1;
    const height = Number(layer.height) || 1;
    const centerX = (Number(layer.x) || 0) + width / 2;
    const centerY = (Number(layer.y) || 0) + height / 2;
    const angle = -(Number(layer.rotation) || 0) * Math.PI / 180;
    const dx = worldX - centerX;
    const dy = worldY - centerY;
    return {
        x: dx * Math.cos(angle) - dy * Math.sin(angle),
        y: dx * Math.sin(angle) + dy * Math.cos(angle)
    };
}

function selectionHandles(layer, zoom) {
    const width = Number(layer.width) || 1;
    const height = Number(layer.height) || 1;
    const offset = 28 / Math.max(.05, zoom);
    return {
        nw: localToWorld(layer, -width / 2, -height / 2),
        ne: localToWorld(layer, width / 2, -height / 2),
        se: localToWorld(layer, width / 2, height / 2),
        sw: localToWorld(layer, -width / 2, height / 2),
        rotate: localToWorld(layer, 0, -height / 2 - offset)
    };
}

function drawSelection(ctx, layer, zoom) {
    if (!layer || layer.visible === false) return;
    const width = Math.max(1, Number(layer.width) || 1);
    const height = Math.max(1, Number(layer.height) || 1);
    const x = Number(layer.x) || 0;
    const y = Number(layer.y) || 0;
    const rotation = (Number(layer.rotation) || 0) * Math.PI / 180;
    const scale = Math.max(.05, zoom);
    const handleSize = 10 / scale;
    const rotationOffset = 28 / scale;
    ctx.save();
    ctx.translate(x + width / 2, y + height / 2);
    ctx.rotate(rotation);
    ctx.strokeStyle = layer.locked ? "#7c3aed" : "#0284c7";
    ctx.lineWidth = 1.5 / scale;
    ctx.setLineDash(layer.locked ? [6 / scale, 4 / scale] : []);
    ctx.strokeRect(-width / 2, -height / 2, width, height);
    if (!layer.locked) {
        ctx.setLineDash([]);
        ctx.beginPath();
        ctx.moveTo(0, -height / 2);
        ctx.lineTo(0, -height / 2 - rotationOffset);
        ctx.stroke();
        const points = [
            [-width / 2, -height / 2], [width / 2, -height / 2],
            [width / 2, height / 2], [-width / 2, height / 2]
        ];
        ctx.fillStyle = "#ffffff";
        for (const point of points) {
            ctx.fillRect(point[0] - handleSize / 2, point[1] - handleSize / 2, handleSize, handleSize);
            ctx.strokeRect(point[0] - handleSize / 2, point[1] - handleSize / 2, handleSize, handleSize);
        }
        ctx.beginPath();
        ctx.arc(0, -height / 2 - rotationOffset, handleSize * .62, 0, Math.PI * 2);
        ctx.fill();
        ctx.stroke();
    }
    ctx.restore();
}

async function drawDocument(canvas, document, options = {}) {
    document = normalizeDocument(document);
    if (canvas.width !== document.widthPx) canvas.width = document.widthPx;
    if (canvas.height !== document.heightPx) canvas.height = document.heightPx;
    const ctx = canvas.getContext("2d", { alpha: true, desynchronized: false });
    ctx.setTransform(1, 0, 0, 1, 0, 0);
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    drawBackground(ctx, document, options.forceOpaque === true);
    for (const layer of document.layers) await drawLayer(ctx, layer);
    if (options.grid) drawGrid(ctx, document, options.zoom || 1);
    if (options.selectedLayerId) {
        const selected = document.layers.find(layer => String(layer.id).toLowerCase() === String(options.selectedLayerId).toLowerCase());
        drawSelection(ctx, selected, options.zoom || 1);
    }
    return canvas;
}

function canvasPoint(canvas, event) {
    const bounds = canvas.getBoundingClientRect();
    return {
        x: (event.clientX - bounds.left) * canvas.width / Math.max(1, bounds.width),
        y: (event.clientY - bounds.top) * canvas.height / Math.max(1, bounds.height)
    };
}

function hitLayer(document, x, y) {
    for (let index = document.layers.length - 1; index >= 0; index--) {
        const layer = document.layers[index];
        if (!layer.visible) continue;
        const local = worldToLocal(layer, x, y);
        const width = Math.max(1, Number(layer.width) || 1);
        const height = Math.max(1, Number(layer.height) || 1);
        if (local.x >= -width / 2 && local.x <= width / 2 && local.y >= -height / 2 && local.y <= height / 2)
            return layer;
    }
    return null;
}

function hitHandle(layer, x, y, zoom) {
    if (!layer || layer.locked) return null;
    const handles = selectionHandles(layer, zoom);
    const threshold = 13 / Math.max(.05, zoom);
    for (const [name, point] of Object.entries(handles)) {
        if (Math.hypot(point.x - x, point.y - y) <= threshold) return name;
    }
    return null;
}

function scheduleEditorRender(editor) {
    if (editor.animationFrame) return;
    editor.animationFrame = requestAnimationFrame(async () => {
        editor.animationFrame = 0;
        const token = ++editor.renderToken;
        await drawDocument(editor.canvas, editor.document, {
            grid: true,
            selectedLayerId: editor.selectedLayerId,
            zoom: editor.zoom
        });
        if (token !== editor.renderToken) scheduleEditorRender(editor);
    });
}

function snap(value, spacing, enabled) {
    return enabled ? Math.round(value / spacing) * spacing : value;
}

function resizeLayer(interaction, point) {
    const original = interaction.original;
    const local = worldToLocal(original, point.x, point.y);
    let left = -original.width / 2;
    let right = original.width / 2;
    let top = -original.height / 2;
    let bottom = original.height / 2;
    if (interaction.mode.includes("w")) left = Math.min(local.x, right - 8);
    if (interaction.mode.includes("e")) right = Math.max(local.x, left + 8);
    if (interaction.mode.includes("n")) top = Math.min(local.y, bottom - 8);
    if (interaction.mode.includes("s")) bottom = Math.max(local.y, top + 8);
    const newWidth = right - left;
    const newHeight = bottom - top;
    const localCenterX = (left + right) / 2;
    const localCenterY = (top + bottom) / 2;
    const originalCenterX = original.x + original.width / 2;
    const originalCenterY = original.y + original.height / 2;
    const angle = original.rotation * Math.PI / 180;
    const worldCenterX = originalCenterX + localCenterX * Math.cos(angle) - localCenterY * Math.sin(angle);
    const worldCenterY = originalCenterY + localCenterX * Math.sin(angle) + localCenterY * Math.cos(angle);
    interaction.layer.width = newWidth;
    interaction.layer.height = newHeight;
    interaction.layer.x = worldCenterX - newWidth / 2;
    interaction.layer.y = worldCenterY - newHeight / 2;
}

function beginInteraction(editor, event) {
    if (!editor.document) return;
    const point = canvasPoint(editor.canvas, event);
    let selected = editor.document.layers.find(layer => String(layer.id) === String(editor.selectedLayerId));
    const handle = hitHandle(selected, point.x, point.y, editor.zoom);
    if (!handle) {
        selected = hitLayer(editor.document, point.x, point.y);
        editor.selectedLayerId = selected ? String(selected.id) : null;
        editor.dotNetRef.invokeMethodAsync("PictureLayerSelected", editor.selectedLayerId);
        scheduleEditorRender(editor);
    }
    if (!selected || selected.locked) return;
    const mode = handle || "move";
    editor.interaction = {
        mode,
        pointerId: event.pointerId,
        start: point,
        layer: selected,
        original: {
            id: selected.id,
            x: Number(selected.x) || 0,
            y: Number(selected.y) || 0,
            width: Math.max(1, Number(selected.width) || 1),
            height: Math.max(1, Number(selected.height) || 1),
            rotation: Number(selected.rotation) || 0
        }
    };
    editor.canvas.setPointerCapture(event.pointerId);
    event.preventDefault();
}

function updateInteraction(editor, event) {
    const interaction = editor.interaction;
    if (!interaction || interaction.pointerId !== event.pointerId) return;
    const point = canvasPoint(editor.canvas, event);
    const grid = Math.max(2, Number(editor.document.gridSpacingPx) || 25);
    if (interaction.mode === "move") {
        const dx = point.x - interaction.start.x;
        const dy = point.y - interaction.start.y;
        interaction.layer.x = snap(interaction.original.x + dx, grid, editor.document.snapToGrid);
        interaction.layer.y = snap(interaction.original.y + dy, grid, editor.document.snapToGrid);
    } else if (interaction.mode === "rotate") {
        const centerX = interaction.layer.x + interaction.layer.width / 2;
        const centerY = interaction.layer.y + interaction.layer.height / 2;
        const angle = Math.atan2(point.y - centerY, point.x - centerX) * 180 / Math.PI + 90;
        interaction.layer.rotation = snap((angle % 360 + 360) % 360, 15, editor.document.snapToGrid);
    } else {
        resizeLayer(interaction, point);
        if (editor.document.snapToGrid) {
            interaction.layer.width = Math.max(8, snap(interaction.layer.width, grid, true));
            interaction.layer.height = Math.max(8, snap(interaction.layer.height, grid, true));
        }
    }
    scheduleEditorRender(editor);
    event.preventDefault();
}

function finishInteraction(editor, event, cancel = false) {
    const interaction = editor.interaction;
    if (!interaction || (event && interaction.pointerId !== event.pointerId)) return;
    editor.interaction = null;
    if (cancel) Object.assign(interaction.layer, interaction.original);
    if (!cancel) {
        editor.dotNetRef.invokeMethodAsync(
            "PictureTransformCommitted", String(interaction.layer.id),
            Number(interaction.layer.x) || 0, Number(interaction.layer.y) || 0,
            Math.max(1, Number(interaction.layer.width) || 1), Math.max(1, Number(interaction.layer.height) || 1),
            Number(interaction.layer.rotation) || 0
        );
    }
    scheduleEditorRender(editor);
}

function bindEditorCanvas(editor, canvas) {
    if (editor.canvas === canvas && canvas.dataset.pictureStudioBound === "true") return;
    editor.canvas = canvas;
    editor.interaction = null;
    canvas.dataset.pictureStudioBound = "true";
    canvas.addEventListener("pointerdown", event => beginInteraction(editor, event));
    canvas.addEventListener("pointermove", event => updateInteraction(editor, event));
    canvas.addEventListener("pointerup", event => finishInteraction(editor, event));
    canvas.addEventListener("pointercancel", event => finishInteraction(editor, event, true));
    canvas.addEventListener("keydown", event => {
        if (event.key === "Escape") finishInteraction(editor, null, true);
    });
}

export function initializePictureStudio(canvasId, dotNetRef) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    let editor = editors.get(canvasId);
    if (!editor) {
        editor = {
            canvas: null,
            dotNetRef,
            document: null,
            selectedLayerId: null,
            zoom: .65,
            interaction: null,
            animationFrame: 0,
            renderToken: 0
        };
        editors.set(canvasId, editor);
    } else {
        editor.dotNetRef = dotNetRef;
    }
    bindEditorCanvas(editor, canvas);
}

export async function renderPictureStudio(canvasId, documentModel, selectedLayerId, zoom) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const editor = editors.get(canvasId);
    if (!editor) return;
    const nextDocument = cloneDocument(documentModel);
    editor.selectedLayerId = selectedLayerId || null;
    editor.zoom = clamp(zoom ?? nextDocument.zoom, .05, 4);
    canvas.style.width = `${Math.round(nextDocument.widthPx * editor.zoom)}px`;
    canvas.style.height = `${Math.round(nextDocument.heightPx * editor.zoom)}px`;
    if (!editor.interaction) editor.document = nextDocument;
    scheduleEditorRender(editor);
}

export function fitPictureStudio(hostId, width, height) {
    const host = document.getElementById(hostId);
    if (!host) return .65;
    const bounds = host.getBoundingClientRect();
    const availableWidth = Math.max(100, bounds.width - 90);
    const availableHeight = Math.max(100, bounds.height - 90);
    return clamp(Math.min(availableWidth / Math.max(1, width), availableHeight / Math.max(1, height)), .05, 2);
}

export async function getPictureImageSize(dataUrl) {
    const image = await loadImage(dataUrl);
    return { width: image?.naturalWidth || 0, height: image?.naturalHeight || 0 };
}

async function renderExportCanvas(documentModel, mimeType) {
    const document = cloneDocument(documentModel);
    const canvas = createCanvas(document.widthPx, document.heightPx);
    await drawDocument(canvas, document, {
        grid: false,
        selectedLayerId: null,
        zoom: 1,
        forceOpaque: mimeType === "image/jpeg"
    });
    return canvas;
}

function canvasToBlob(canvas, mimeType, quality) {
    return new Promise((resolve, reject) => {
        canvas.toBlob(blob => blob ? resolve(blob) : reject(new Error("The browser could not rasterize the picture.")), mimeType, quality);
    });
}

async function createPictureStudioBlob(documentModel, mimeType = "image/png", quality = 1) {
    const canvas = await renderExportCanvas(documentModel, mimeType);
    return await canvasToBlob(canvas, mimeType, quality);
}

export async function exportPictureStudioBlob(documentModel, mimeType = "image/png", quality = 1) {
    const blob = await createPictureStudioBlob(documentModel, mimeType, quality);
    return DotNet.createJSStreamReference(blob);
}

export async function downloadPictureStudio(documentModel, fileName, mimeType = "image/png", quality = 1) {
    const blob = await createPictureStudioBlob(documentModel, mimeType, quality);
    const url = URL.createObjectURL(blob);
    try {
        const anchor = document.createElement("a");
        anchor.href = url;
        anchor.download = fileName;
        document.body.appendChild(anchor);
        anchor.click();
        anchor.remove();
    } finally {
        setTimeout(() => URL.revokeObjectURL(url), 2000);
    }
}
