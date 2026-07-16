const editors = new Map();
const imageCache = new Map();
const proceduralCache = new Map();

const layerKinds = ["raster", "text", "shape", "fill", "render", "paint"];
const blendModes = ["source-over", "multiply", "screen", "overlay", "darken", "lighten"];
const rasterFits = ["stretch", "contain", "cover"];
const shapeKinds = ["rectangle", "roundedRectangle", "ellipse", "line"];
const fillKinds = ["solid", "linearGradient", "radialGradient"];
const renderKinds = ["clouds", "noise", "stripes", "vignette", "bloom", "neon", "lensflare"];
const textAlignments = ["left", "center", "right"];
const drawTools = ["select", "brush", "pencil", "spray", "toothbrush", "line", "eraser", "eyedropper"];

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

function normalizeToolSettings(settings) {
    const rawTool = typeof settings?.tool === "string" ? settings.tool.toLowerCase() : "select";
    return {
        tool: drawTools.includes(rawTool) ? rawTool : "select",
        color: cssColor(settings?.color, "#111827"),
        width: clamp(settings?.width ?? 12, .25, 512),
        opacity: clamp(settings?.opacity ?? 1, 0, 1),
        hardness: clamp(settings?.hardness ?? .8, 0, 1)
    };
}

function createCanvas(width, height) {
    const canvas = document.createElement("canvas");
    canvas.width = Math.max(1, Math.round(width));
    canvas.height = Math.max(1, Math.round(height));
    return canvas;
}

function loadImage(dataUrl) {
    if (!dataUrl) return Promise.resolve(null);
    const source = String(dataUrl).trim();
    if (!source.startsWith("data:image/") && !source.startsWith("blob:"))
        return Promise.reject(new Error("The image layer contains an invalid source instead of embedded image data."));
    if (imageCache.has(source)) return imageCache.get(source);
    const promise = new Promise((resolve, reject) => {
        const image = new Image();
        image.decoding = "async";
        image.onload = () => resolve(image);
        image.onerror = () => reject(new Error("The image layer could not be decoded."));
        image.src = source;
    }).catch(error => {
        imageCache.delete(source);
        throw error;
    });
    imageCache.set(source, promise);
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

function rgba(color, alpha = 1) {
    const parsed = parseColor(color, [0, 0, 0, 255]);
    return `rgba(${parsed[0]}, ${parsed[1]}, ${parsed[2]}, ${clamp(alpha * (parsed[3] / 255), 0, 1)})`;
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
    const { width, height } = beginLayer(ctx, layer);
    let image;
    try {
        image = await loadImage(layer.dataUrl);
    } catch (error) {
        ctx.save();
        ctx.fillStyle = "#f8fafc";
        ctx.fillRect(-width / 2, -height / 2, width, height);
        ctx.strokeStyle = "#dc2626";
        ctx.lineWidth = Math.max(2, Math.min(width, height) * .015);
        ctx.strokeRect(-width / 2, -height / 2, width, height);
        ctx.beginPath();
        ctx.moveTo(-width / 2, -height / 2);
        ctx.lineTo(width / 2, height / 2);
        ctx.moveTo(width / 2, -height / 2);
        ctx.lineTo(-width / 2, height / 2);
        ctx.stroke();
        ctx.restore();
        endLayer(ctx);
        return `${layer.name || "Image"}: ${error?.message || error}`;
    }
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
    return null;
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

function createBloomCanvas(layer, width, height) {
    const canvas = createCanvas(Math.max(1, Math.round(width)), Math.max(1, Math.round(height)));
    const ctx = canvas.getContext("2d", { alpha: true });
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    const seed = Number(layer.seed) || 1;
    ctx.fillStyle = rgba(layer.primaryColor, .08);
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    const blooms = Math.max(3, Math.min(10, Math.round((layer.detail ?? 4) + 2)));
    for (let index = 0; index < blooms; index++) {
        const cx = canvas.width * (.15 + .7 * hashNoise(seed + index * 7, 11 + index, seed));
        const cy = canvas.height * (.15 + .7 * hashNoise(seed + index * 11, 19 + index, seed));
        const radius = Math.max(24, Math.min(canvas.width, canvas.height) * (.08 + .18 * hashNoise(seed + index * 17, 23 + index, seed)));
        const gradient = ctx.createRadialGradient(cx, cy, 0, cx, cy, radius);
        gradient.addColorStop(0, rgba(index % 2 === 0 ? layer.secondaryColor : layer.primaryColor, .65));
        gradient.addColorStop(.45, rgba(index % 2 === 0 ? layer.primaryColor : layer.secondaryColor, .28));
        gradient.addColorStop(1, rgba(layer.secondaryColor, 0));
        ctx.fillStyle = gradient;
        ctx.beginPath();
        ctx.arc(cx, cy, radius, 0, Math.PI * 2);
        ctx.fill();
    }
    return canvas;
}

function createNeonCanvas(layer, width, height) {
    const canvas = createCanvas(Math.max(1, Math.round(width)), Math.max(1, Math.round(height)));
    const ctx = canvas.getContext("2d", { alpha: true });
    const background = ctx.createLinearGradient(0, 0, canvas.width, canvas.height);
    background.addColorStop(0, "rgba(5,8,20,.95)");
    background.addColorStop(1, "rgba(18,24,42,.95)");
    ctx.fillStyle = background;
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    const bands = Math.max(2, Math.min(6, Math.round(layer.detail ?? 4)));
    for (let index = 0; index < bands; index++) {
        const startY = canvas.height * (.2 + index * .14);
        ctx.save();
        ctx.lineWidth = Math.max(2, canvas.height * .012 + index * 1.6);
        ctx.strokeStyle = index % 2 === 0 ? cssColor(layer.primaryColor, "#22d3ee") : cssColor(layer.secondaryColor, "#f472b6");
        ctx.shadowColor = ctx.strokeStyle;
        ctx.shadowBlur = 18 + index * 6;
        ctx.beginPath();
        for (let x = 0; x <= canvas.width; x += 18) {
            const wave = Math.sin((x / Math.max(20, layer.scale || 90)) + index * .8) * canvas.height * .07;
            const y = startY + wave;
            if (x === 0) ctx.moveTo(x, y);
            else ctx.lineTo(x, y);
        }
        ctx.stroke();
        ctx.restore();
    }
    return canvas;
}

function createLensFlareCanvas(layer, width, height) {
    const canvas = createCanvas(Math.max(1, Math.round(width)), Math.max(1, Math.round(height)));
    const ctx = canvas.getContext("2d", { alpha: true });
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    const seed = Number(layer.seed) || 1;
    const focusX = canvas.width * (.2 + .6 * hashNoise(seed, 7, seed));
    const focusY = canvas.height * (.18 + .28 * hashNoise(seed, 13, seed));
    const star = ctx.createRadialGradient(focusX, focusY, 0, focusX, focusY, Math.max(canvas.width, canvas.height) * .26);
    star.addColorStop(0, rgba(layer.primaryColor, .95));
    star.addColorStop(.1, rgba(layer.secondaryColor, .7));
    star.addColorStop(.35, rgba(layer.primaryColor, .14));
    star.addColorStop(1, rgba(layer.primaryColor, 0));
    ctx.fillStyle = star;
    ctx.beginPath();
    ctx.arc(focusX, focusY, Math.max(canvas.width, canvas.height) * .28, 0, Math.PI * 2);
    ctx.fill();
    ctx.save();
    ctx.strokeStyle = rgba(layer.primaryColor, .35);
    ctx.lineWidth = Math.max(1, Math.min(canvas.width, canvas.height) * .006);
    ctx.shadowColor = cssColor(layer.secondaryColor, "#ffffff");
    ctx.shadowBlur = 20;
    ctx.beginPath();
    ctx.moveTo(0, focusY); ctx.lineTo(canvas.width, focusY);
    ctx.moveTo(focusX, 0); ctx.lineTo(focusX, canvas.height);
    ctx.stroke();
    ctx.restore();
    const dx = canvas.width - focusX;
    const dy = canvas.height - focusY;
    for (let index = 1; index <= 6; index++) {
        const t = index / 7;
        const cx = focusX + dx * t * .85;
        const cy = focusY + dy * t * .85;
        const radius = Math.max(10, Math.min(canvas.width, canvas.height) * (.012 + .022 * (1 - t)));
        const orb = ctx.createRadialGradient(cx, cy, 0, cx, cy, radius);
        orb.addColorStop(0, rgba(index % 2 ? layer.secondaryColor : layer.primaryColor, .35));
        orb.addColorStop(1, rgba(layer.primaryColor, 0));
        ctx.fillStyle = orb;
        ctx.beginPath();
        ctx.arc(cx, cy, radius, 0, Math.PI * 2);
        ctx.fill();
    }
    return canvas;
}

function getProceduralCanvas(layer, width, height) {
    const key = proceduralKey(layer, width, height);
    if (proceduralCache.has(key)) return proceduralCache.get(key);
    const kind = enumName(layer.renderKind, renderKinds, "clouds").toLowerCase();
    const canvas = kind === "noise"
        ? createNoiseOrClouds(layer, width, height, false)
        : kind === "bloom"
            ? createBloomCanvas(layer, width, height)
            : kind === "neon"
                ? createNeonCanvas(layer, width, height)
                : kind === "lensflare"
                    ? createLensFlareCanvas(layer, width, height)
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

function strokeKind(stroke) {
    if (typeof stroke?.kind === "string") return stroke.kind.toLowerCase();
    return enumName(stroke?.kind, ["brush", "pencil", "spray", "toothbrush", "line", "eraser"], "brush").toLowerCase();
}

function traceStrokePath(ctx, points, kind) {
    if (!points.length) return;
    ctx.beginPath();
    ctx.moveTo(Number(points[0].x) || 0, Number(points[0].y) || 0);
    if (kind === "line" || points.length === 2) {
        const last = points[points.length - 1];
        ctx.lineTo(Number(last.x) || 0, Number(last.y) || 0);
        return;
    }
    if (points.length < 3) {
        for (let index = 1; index < points.length; index++)
            ctx.lineTo(Number(points[index].x) || 0, Number(points[index].y) || 0);
        return;
    }
    for (let index = 1; index < points.length - 1; index++) {
        const point = points[index];
        const next = points[index + 1];
        const middleX = ((Number(point.x) || 0) + (Number(next.x) || 0)) / 2;
        const middleY = ((Number(point.y) || 0) + (Number(next.y) || 0)) / 2;
        ctx.quadraticCurveTo(Number(point.x) || 0, Number(point.y) || 0, middleX, middleY);
    }
    const last = points[points.length - 1];
    ctx.lineTo(Number(last.x) || 0, Number(last.y) || 0);
}

function drawSprayStroke(ctx, points, width, color, opacity) {
    const radius = Math.max(1, width / 2);
    ctx.save();
    ctx.fillStyle = color;
    ctx.globalAlpha = opacity * .22;
    let particle = 0;
    for (let index = 1; index < points.length; index++) {
        const a = points[index - 1];
        const b = points[index];
        const dx = (Number(b.x) || 0) - (Number(a.x) || 0);
        const dy = (Number(b.y) || 0) - (Number(a.y) || 0);
        const length = Math.max(1, Math.hypot(dx, dy));
        const steps = Math.max(1, Math.ceil(length / Math.max(2, radius * .45)));
        for (let step = 0; step <= steps; step++) {
            const t = step / steps;
            const px = (Number(a.x) || 0) + dx * t;
            const py = (Number(a.y) || 0) + dy * t;
            const density = Math.max(8, Math.round(width * .8));
            for (let dot = 0; dot < density; dot++) {
                const angle = hashNoise(particle, dot + 1, 97) * Math.PI * 2;
                const distance = Math.sqrt(hashNoise(particle + 17, dot + 13, 211)) * radius;
                const size = Math.max(.5, width * (.012 + hashNoise(particle + 31, dot + 29, 313) * .028));
                ctx.beginPath();
                ctx.arc(px + Math.cos(angle) * distance, py + Math.sin(angle) * distance, size, 0, Math.PI * 2);
                ctx.fill();
            }
            particle++;
        }
    }
    ctx.restore();
}

function drawToothbrushStroke(ctx, points, width, color, opacity) {
    const last = points[points.length - 1];
    if (!last) return;
    ctx.save();
    ctx.lineCap = "round";
    ctx.lineJoin = "round";
    ctx.strokeStyle = color;
    ctx.globalAlpha = opacity;
    const bristles = Math.max(3, Math.min(11, Math.round(width / 3)));
    for (let band = 0; band < bristles; band++) {
        ctx.beginPath();
        for (let index = 0; index < points.length; index++) {
            const point = points[index];
            const prev = points[Math.max(0, index - 1)] || point;
            const next = points[Math.min(points.length - 1, index + 1)] || point;
            const tangentX = (Number(next.x) || 0) - (Number(prev.x) || 0);
            const tangentY = (Number(next.y) || 0) - (Number(prev.y) || 0);
            const length = Math.max(.001, Math.hypot(tangentX, tangentY));
            const nx = -tangentY / length;
            const ny = tangentX / length;
            const spread = ((band / Math.max(1, bristles - 1)) - .5) * width * .8;
            const jitter = (hashNoise(index + band * 13, band + 5, 401) - .5) * width * .12;
            const x = (Number(point.x) || 0) + nx * (spread + jitter);
            const y = (Number(point.y) || 0) + ny * (spread + jitter);
            if (index === 0) ctx.moveTo(x, y);
            else ctx.lineTo(x, y);
        }
        ctx.lineWidth = Math.max(.6, width * (.08 + (band % 3) * .03));
        ctx.globalAlpha = opacity * (.35 + ((band + 1) / bristles) * .35);
        ctx.stroke();
    }
    ctx.globalAlpha = opacity * .18;
    ctx.fillStyle = color;
    for (let index = 0; index < points.length; index += Math.max(1, Math.floor(points.length / 18) || 1)) {
        const point = points[index];
        for (let dot = 0; dot < Math.max(2, Math.round(width / 5)); dot++) {
            const angle = hashNoise(index, dot + 3, 509) * Math.PI * 2;
            const distance = hashNoise(index + 7, dot + 17, 601) * width * .42;
            ctx.beginPath();
            ctx.arc((Number(point.x) || 0) + Math.cos(angle) * distance, (Number(point.y) || 0) + Math.sin(angle) * distance, Math.max(.4, width * .02), 0, Math.PI * 2);
            ctx.fill();
        }
    }
    ctx.restore();
}

function drawPaintStroke(ctx, stroke, preview = false) {
    const points = Array.isArray(stroke?.points) ? stroke.points : [];
    if (points.length < 2) return;
    const kind = strokeKind(stroke);
    const width = clamp(stroke.widthPx ?? stroke.width ?? 1, .25, 512);
    const opacity = clamp(stroke.opacity ?? 1, 0, 1);
    const hardness = clamp(stroke.hardness ?? .8, 0, 1);
    const erasing = kind === "eraser" && !preview;
    const strokeColor = preview && kind === "eraser" ? "#ef4444" : cssColor(stroke.color, "#111827");
    if (kind === "spray") {
        drawSprayStroke(ctx, points, width, strokeColor, preview ? Math.max(.55, opacity) : opacity);
        return;
    }
    if (kind === "toothbrush") {
        drawToothbrushStroke(ctx, points, width, strokeColor, preview ? Math.max(.55, opacity) : opacity);
        return;
    }
    ctx.save();
    ctx.lineCap = kind === "pencil" ? "square" : "round";
    ctx.lineJoin = "round";
    ctx.globalCompositeOperation = erasing ? "destination-out" : "source-over";
    ctx.strokeStyle = strokeColor;
    ctx.globalAlpha = preview ? Math.max(.55, opacity) : opacity;
    if (kind === "brush" && hardness < .98 && !erasing) {
        ctx.shadowColor = ctx.strokeStyle;
        ctx.shadowBlur = width * (1 - hardness) * 1.5;
        ctx.lineWidth = Math.max(.25, width * (.55 + hardness * .45));
    } else {
        ctx.lineWidth = width;
    }
    traceStrokePath(ctx, points, kind);
    ctx.stroke();
    if (kind === "brush" && hardness < .98 && !erasing) {
        ctx.shadowBlur = 0;
        ctx.globalAlpha = preview ? Math.max(.65, opacity) : opacity;
        ctx.lineWidth = Math.max(.25, width * (.25 + hardness * .65));
        traceStrokePath(ctx, points, kind);
        ctx.stroke();
    }
    ctx.restore();
}

function drawPaintLayer(ctx, layer) {
    const { width, height } = beginLayer(ctx, layer);
    const scratch = createCanvas(Math.max(1, Math.round(width)), Math.max(1, Math.round(height)));
    const scratchContext = scratch.getContext("2d", { alpha: true });
    for (const stroke of Array.isArray(layer.strokes) ? layer.strokes : [])
        drawPaintStroke(scratchContext, stroke);
    ctx.drawImage(scratch, -width / 2, -height / 2, width, height);
    endLayer(ctx);
}

async function drawLayer(ctx, layer) {
    if (!layer || layer.visible === false || clamp(layer.opacity ?? 1, 0, 1) <= 0) return null;
    switch (layerKind(layer)) {
        case "raster": return await drawRasterLayer(ctx, layer);
        case "text": drawTextLayer(ctx, layer); break;
        case "fill": drawFillLayer(ctx, layer); break;
        case "render": drawRenderLayer(ctx, layer); break;
        case "paint": drawPaintLayer(ctx, layer); break;
        default: drawShapeLayer(ctx, layer); break;
    }
    return null;
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
    if (!layer.locked && layerKind(layer) !== "paint") {
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
    const errors = [];
    for (const layer of document.layers) {
        const error = await drawLayer(ctx, layer);
        if (error) errors.push(error);
    }
    if (options.grid) drawGrid(ctx, document, options.zoom || 1);
    if (options.previewStroke) drawPaintStroke(ctx, options.previewStroke, true);
    if (options.selectedLayerId) {
        const selected = document.layers.find(layer => String(layer.id).toLowerCase() === String(options.selectedLayerId).toLowerCase());
        drawSelection(ctx, selected, options.zoom || 1);
    }
    canvas.pictureStudioErrors = errors;
    return canvas;
}

function canvasPoint(canvas, event) {
    const bounds = canvas.getBoundingClientRect();
    return {
        x: (event.clientX - bounds.left) * canvas.width / Math.max(1, bounds.width),
        y: (event.clientY - bounds.top) * canvas.height / Math.max(1, bounds.height)
    };
}

function distanceToSegment(pointX, pointY, firstX, firstY, secondX, secondY) {
    const dx = secondX - firstX;
    const dy = secondY - firstY;
    if (Math.abs(dx) < .0001 && Math.abs(dy) < .0001) return Math.hypot(pointX - firstX, pointY - firstY);
    const amount = clamp(((pointX - firstX) * dx + (pointY - firstY) * dy) / (dx * dx + dy * dy), 0, 1);
    return Math.hypot(pointX - (firstX + dx * amount), pointY - (firstY + dy * amount));
}

function hitPaintLayer(layer, worldX, worldY) {
    const local = worldToLocal(layer, worldX, worldY);
    const width = Math.max(1, Number(layer.width) || 1);
    const height = Math.max(1, Number(layer.height) || 1);
    const x = local.x + width / 2;
    const y = local.y + height / 2;
    const strokes = Array.isArray(layer.strokes) ? layer.strokes : [];
    for (let strokeIndex = strokes.length - 1; strokeIndex >= 0; strokeIndex--) {
        const stroke = strokes[strokeIndex];
        if (strokeKind(stroke) === "eraser") continue;
        const points = Array.isArray(stroke.points) ? stroke.points : [];
        const threshold = Math.max(5, clamp(stroke.widthPx ?? 1, .25, 512) / 2 + 3);
        for (let index = 1; index < points.length; index++) {
            if (distanceToSegment(x, y, Number(points[index - 1].x) || 0, Number(points[index - 1].y) || 0,
                Number(points[index].x) || 0, Number(points[index].y) || 0) <= threshold) return true;
        }
    }
    return false;
}

function hitLayer(document, x, y) {
    for (let index = document.layers.length - 1; index >= 0; index--) {
        const layer = document.layers[index];
        if (!layer.visible) continue;
        if (layerKind(layer) === "paint") {
            if (hitPaintLayer(layer, x, y)) return layer;
            continue;
        }
        const local = worldToLocal(layer, x, y);
        const width = Math.max(1, Number(layer.width) || 1);
        const height = Math.max(1, Number(layer.height) || 1);
        if (local.x >= -width / 2 && local.x <= width / 2 && local.y >= -height / 2 && local.y <= height / 2)
            return layer;
    }
    return null;
}

function hitHandle(layer, x, y, zoom) {
    if (!layer || layer.locked || layerKind(layer) === "paint") return null;
    const handles = selectionHandles(layer, zoom);
    const threshold = 13 / Math.max(.05, zoom);
    for (const [name, point] of Object.entries(handles)) {
        if (Math.hypot(point.x - x, point.y - y) <= threshold) return name;
    }
    return null;
}

function safeInvoke(editor, method, ...args) {
    if (!editor?.dotNetRef) return;
    editor.dotNetRef.invokeMethodAsync(method, ...args).catch(error => console.warn(`Picture Studio callback ${method} failed.`, error));
}

function reportRenderState(editor, message) {
    const next = message || "";
    if (next === editor.lastRenderError) return;
    const hadError = Boolean(editor.lastRenderError);
    editor.lastRenderError = next;
    if (next) safeInvoke(editor, "PictureRenderFailed", next);
    else if (hadError) safeInvoke(editor, "PictureRenderRecovered");
}

function scheduleEditorRender(editor) {
    if (editor.animationFrame || !editor.canvas || !editor.document) return;
    editor.animationFrame = requestAnimationFrame(async () => {
        editor.animationFrame = 0;
        const token = ++editor.renderToken;
        try {
            const rendered = await drawDocument(editor.canvas, editor.document, {
                grid: true,
                selectedLayerId: editor.selectedLayerId,
                zoom: editor.zoom,
                previewStroke: editor.drawing
            });
            if (token === editor.renderToken) {
                const errors = Array.isArray(rendered.pictureStudioErrors) ? rendered.pictureStudioErrors : [];
                reportRenderState(editor, errors[0] || "");
            }
        } catch (error) {
            reportRenderState(editor, error?.message || String(error));
        }
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
        safeInvoke(editor, "PictureLayerSelected", editor.selectedLayerId);
        scheduleEditorRender(editor);
    }
    if (!selected || selected.locked || layerKind(selected) === "paint") return;
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
        safeInvoke(editor,
            "PictureTransformCommitted", String(interaction.layer.id),
            Number(interaction.layer.x) || 0, Number(interaction.layer.y) || 0,
            Math.max(1, Number(interaction.layer.width) || 1), Math.max(1, Number(interaction.layer.height) || 1),
            Number(interaction.layer.rotation) || 0
        );
    }
    scheduleEditorRender(editor);
}

async function pickCanvasColor(editor, point) {
    try {
        const clean = createCanvas(editor.document.widthPx, editor.document.heightPx);
        await drawDocument(clean, editor.document, { grid: false, selectedLayerId: null, zoom: 1 });
        const data = clean.getContext("2d", { willReadFrequently: true }).getImageData(
            Math.round(clamp(point.x, 0, clean.width - 1)), Math.round(clamp(point.y, 0, clean.height - 1)), 1, 1).data;
        const hex = `#${[data[0], data[1], data[2]].map(value => value.toString(16).padStart(2, "0")).join("")}`;
        safeInvoke(editor, "PictureColorPicked", hex);
    } catch (error) {
        reportRenderState(editor, `The eyedropper could not read this pixel: ${error?.message || error}`);
    }
}

function beginDrawing(editor, event) {
    if (!editor.document) return;
    const settings = editor.toolSettings || normalizeToolSettings(null);
    const point = canvasPoint(editor.canvas, event);
    if (settings.tool === "eyedropper") {
        void pickCanvasColor(editor, point);
        event.preventDefault();
        return;
    }
    if (!["brush", "pencil", "spray", "toothbrush", "line", "eraser"].includes(settings.tool)) return;
    const adjustedWidth = settings.tool === "pencil"
        ? Math.min(settings.width, 6)
        : settings.tool === "spray"
            ? Math.max(6, settings.width)
            : settings.tool === "toothbrush"
                ? Math.max(4, settings.width)
                : settings.width;
    editor.drawing = {
        pointerId: event.pointerId,
        tool: settings.tool,
        kind: settings.tool,
        color: settings.color,
        width: adjustedWidth,
        widthPx: adjustedWidth,
        opacity: settings.opacity,
        hardness: settings.tool === "pencil" ? 1 : settings.tool === "spray" ? Math.min(settings.hardness, .55) : settings.hardness,
        points: [point, { ...point }]
    };
    editor.canvas.setPointerCapture(event.pointerId);
    scheduleEditorRender(editor);
    event.preventDefault();
}

function updateDrawing(editor, event) {
    const drawing = editor.drawing;
    if (!drawing || drawing.pointerId !== event.pointerId) return;
    let point = canvasPoint(editor.canvas, event);
    if (drawing.tool === "line" && editor.document.snapToGrid) {
        const spacing = Math.max(2, Number(editor.document.gridSpacingPx) || 25);
        point = { x: snap(point.x, spacing, true), y: snap(point.y, spacing, true) };
    }
    if (drawing.tool === "line") drawing.points[drawing.points.length - 1] = point;
    else {
        const last = drawing.points[drawing.points.length - 1];
        if (Math.hypot(point.x - last.x, point.y - last.y) >= Math.max(.5, drawing.widthPx * .06)) drawing.points.push(point);
    }
    scheduleEditorRender(editor);
    event.preventDefault();
}

function localizeStrokePoints(editor, points, tool) {
    let layer = editor.document.layers.find(item => String(item.id) === String(editor.selectedLayerId));
    if ((!layer || layer.locked || layerKind(layer) !== "paint") && tool === "eraser")
        layer = [...editor.document.layers].reverse().find(item => layerKind(item) === "paint" && !item.locked);
    if (!layer || layer.locked || layerKind(layer) !== "paint") return points;
    const width = Math.max(1, Number(layer.width) || 1);
    const height = Math.max(1, Number(layer.height) || 1);
    return points.map(point => {
        const local = worldToLocal(layer, point.x, point.y);
        return { x: local.x + width / 2, y: local.y + height / 2 };
    });
}

function finishDrawing(editor, event, cancel = false) {
    const drawing = editor.drawing;
    if (!drawing || (event && drawing.pointerId !== event.pointerId)) return;
    editor.drawing = null;
    if (!cancel) {
        const points = localizeStrokePoints(editor, drawing.points, drawing.tool);
        if (points.length === 1) points.push({ x: points[0].x + .01, y: points[0].y + .01 });
        const coordinates = [];
        for (const point of points) coordinates.push(Number(point.x) || 0, Number(point.y) || 0);
        safeInvoke(editor, "PictureStrokeCommitted", drawing.tool, coordinates, drawing.color,
            drawing.widthPx, drawing.opacity, drawing.hardness);
    }
    scheduleEditorRender(editor);
}

function updateCanvasCursor(editor) {
    if (!editor.canvas) return;
    const tool = editor.toolSettings?.tool || "select";
    editor.canvas.style.cursor = tool === "select" ? "default" : tool === "eyedropper" ? "copy" : "crosshair";
}

function bindEditorCanvas(editor, canvas) {
    if (editor.canvas === canvas && canvas.dataset.pictureStudioBound === "true") return;
    editor.canvas = canvas;
    editor.interaction = null;
    editor.drawing = null;
    canvas.dataset.pictureStudioBound = "true";
    canvas.addEventListener("pointerdown", event => {
        canvas.focus({ preventScroll: true });
        if (event.button !== 0) return;
        if ((editor.toolSettings?.tool || "select") === "select") beginInteraction(editor, event);
        else beginDrawing(editor, event);
    });
    canvas.addEventListener("pointermove", event => {
        if (editor.drawing) updateDrawing(editor, event);
        else updateInteraction(editor, event);
    });
    canvas.addEventListener("pointerup", event => {
        if (editor.drawing) finishDrawing(editor, event);
        else finishInteraction(editor, event);
    });
    canvas.addEventListener("pointercancel", event => {
        if (editor.drawing) finishDrawing(editor, event, true);
        else finishInteraction(editor, event, true);
    });
    canvas.addEventListener("keydown", event => {
        const modifier = event.ctrlKey || event.metaKey;
        const key = String(event.key || "").toLowerCase();
        let command = null;
        if (modifier && key === "z") command = event.shiftKey ? "redo" : "undo";
        else if (modifier && key === "y") command = "redo";
        else if (modifier && key === "c") command = "copy";
        else if (modifier && key === "v") command = "paste";
        else if (modifier && key === "d") command = "duplicate";
        else if (event.key === "Delete") command = "delete";
        else if (event.key === "Home") command = "front";
        else if (event.key === "End") command = "back";

        if (command) {
            safeInvoke(editor, "PictureShortcutRequested", command);
            event.preventDefault();
            return;
        }
        if (event.key === "Escape") {
            if (editor.drawing) finishDrawing(editor, null, true);
            else if (editor.interaction) finishInteraction(editor, null, true);
            else safeInvoke(editor, "PictureShortcutRequested", "select");
            event.preventDefault();
        }
    });
    updateCanvasCursor(editor);
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
            drawing: null,
            toolSettings: normalizeToolSettings(null),
            animationFrame: 0,
            renderToken: 0,
            lastRenderError: ""
        };
        editors.set(canvasId, editor);
    } else {
        editor.dotNetRef = dotNetRef;
    }
    bindEditorCanvas(editor, canvas);
}

export async function renderPictureStudio(canvasId, documentModel, selectedLayerId, zoom, toolSettings) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const editor = editors.get(canvasId);
    if (!editor) return;
    const nextDocument = cloneDocument(documentModel);
    editor.selectedLayerId = selectedLayerId || null;
    editor.zoom = clamp(zoom ?? nextDocument.zoom, .05, 4);
    editor.toolSettings = normalizeToolSettings(toolSettings);
    canvas.style.width = `${Math.round(nextDocument.widthPx * editor.zoom)}px`;
    canvas.style.height = `${Math.round(nextDocument.heightPx * editor.zoom)}px`;
    if (!editor.interaction && !editor.drawing) editor.document = nextDocument;
    updateCanvasCursor(editor);
    scheduleEditorRender(editor);
}

export function hitTestPictureStudioLayer(canvasId, clientX, clientY) {
    const editor = editors.get(canvasId);
    const canvas = document.getElementById(canvasId);
    if (!editor?.document || !canvas) return null;
    const point = canvasPoint(canvas, { clientX: Number(clientX) || 0, clientY: Number(clientY) || 0 });
    const layer = hitLayer(editor.document, point.x, point.y);
    return layer ? String(layer.id) : null;
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

const pictureExportChunkSize = 24 * 1024;

export function startPictureStudioDataUrlExport(
    documentModel,
    mimeType = "image/png",
    quality = 1,
    dotNetReference,
    exportId) {
    // Return immediately so the initiating .NET -> JS call is finished before
    // JavaScript starts invoking .NET with the generated image chunks.
    void exportPictureStudioDataUrlInChunks(
        documentModel,
        mimeType,
        quality,
        dotNetReference,
        exportId);
}

async function exportPictureStudioDataUrlInChunks(
    documentModel,
    mimeType,
    quality,
    dotNetReference,
    exportId) {
    try {
        const canvas = await renderExportCanvas(documentModel, mimeType);
        const dataUrl = canvas.toDataURL(mimeType, quality);
        if (!dataUrl || dataUrl === "data:," || !dataUrl.startsWith("data:image/"))
            throw new Error("The browser could not rasterize the picture.");

        const chunkCount = Math.ceil(dataUrl.length / pictureExportChunkSize);
        const exportAccepted = await dotNetReference.invokeMethodAsync(
            "BeginPictureExport",
            exportId,
            dataUrl.length,
            chunkCount);
        if (!exportAccepted) return;

        for (let chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++) {
            const offset = chunkIndex * pictureExportChunkSize;
            const chunk = dataUrl.slice(offset, offset + pictureExportChunkSize);
            const chunkAccepted = await dotNetReference.invokeMethodAsync(
                "AppendPictureExportChunk",
                exportId,
                chunkIndex,
                chunk);
            if (!chunkAccepted) return;
        }

        await dotNetReference.invokeMethodAsync("CompletePictureExport", exportId);
    } catch (error) {
        const message = error?.message || String(error);
        try {
            await dotNetReference.invokeMethodAsync("FailPictureExport", exportId, message);
        } catch {
            // The Blazor circuit may already be gone.
        }
    }
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
