// javascript-diagnostics: guarded
var publisherStudioDiagnostics = globalThis.publisherStudioJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`PublisherStudio JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("PublisherStudio fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`PublisherStudio fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
const editors = new Map();
const imageCache = new Map();
const proceduralCache = new Map();

const layerKinds = ["raster", "text", "shape", "fill", "render", "paint", "vector"];
const blendModes = ["source-over", "multiply", "screen", "overlay", "darken", "lighten"];
const rasterFits = ["stretch", "contain", "cover"];
const shapeKinds = ["rectangle", "roundedRectangle", "ellipse", "line", "arrow", "freeform", "path"];
const fillKinds = ["solid", "linearGradient", "radialGradient"];
const renderKinds = ["clouds", "noise", "stripes", "vignette", "bloom", "neon", "lensflare", "grainnoise", "motionblur", "wind", "oceanwaves"];
const textAlignments = ["left", "center", "right"];
const drawTools = ["select", "brush", "pencil", "spray", "toothbrush", "square", "rectangle", "ellipse", "arrow", "line", "path", "eraser", "eyedropper", "rectangleselect", "ellipseselect", "freeselect", "magneticselect", "polygonselect", "fillsolid", "fillgradient"];

function enumName(value, names, fallback) { try {
    if (typeof value === "string") return value;
    if (Number.isInteger(value) && value >= 0 && value < names.length) return names[value];
    return fallback;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:enumName@16', __javascriptError); throw __javascriptError; }}

function layerKind(layer) { try {
    const discriminator = layer?.$type;
    if (typeof discriminator === "string") return discriminator.toLowerCase();
    return enumName(layer?.kind, layerKinds, "shape").toLowerCase();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:layerKind@22', __javascriptError); throw __javascriptError; }}

function blendMode(value) { try {
    if (typeof value === "string") {
        const name = value.toLowerCase();
        return name === "normal" ? "source-over" : name;
    }
    return enumName(value, blendModes, "source-over");
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:blendMode@28', __javascriptError); throw __javascriptError; }}

function clamp(value, minimum, maximum) { try {
    const number = Number(value);
    if (!Number.isFinite(number)) return minimum;
    return Math.max(minimum, Math.min(maximum, number));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:clamp@36', __javascriptError); throw __javascriptError; }}

function normalizeDocument(document) { try {
    return {
        ...document,
        widthPx: Math.round(clamp(document?.widthPx, 16, 8192)),
        heightPx: Math.round(clamp(document?.heightPx, 16, 8192)),
        zoom: clamp(document?.zoom ?? .65, .05, 4),
        gridSpacingPx: Math.round(clamp(document?.gridSpacingPx ?? 25, 2, 1000)),
        background: document?.background || "transparent",
        layers: Array.isArray(document?.layers) ? document.layers : []
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:normalizeDocument@42', __javascriptError); throw __javascriptError; }}

function cloneDocument(document) { try {
    return JSON.parse(JSON.stringify(normalizeDocument(document)));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:cloneDocument@54', __javascriptError); throw __javascriptError; }}

function pictureDropRoute(file) { try {
    const name = String(file?.name || "").toLowerCase();
    const mime = String(file?.type || "").toLowerCase();
    if (/\.(ora|svgz|svg)$/.test(name)
        || mime === "image/svg+xml"
        || mime.includes("openraster")
        || mime.includes("gzip")) return "layers";
    if (mime.startsWith("image/") || /\.(png|jpe?g|gif|webp)$/.test(name)) return "image";
    return "";
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:pictureDropRoute@58', __javascriptError); throw __javascriptError; }}

function assignDroppedFile(inputId, file) { try {
    const input = document.getElementById(inputId);
    if (!(input instanceof HTMLInputElement) || input.type !== "file" || !(file instanceof File)) return false;
    const transfer = new DataTransfer();
    transfer.items.add(file);
    input.value = "";
    input.files = transfer.files;
    input.dispatchEvent(new Event("change", { bubbles: true }));
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:assignDroppedFile@69', __javascriptError); throw __javascriptError; }}

function releasePictureDropBindings(editor) { try {
    const root = editor?.dropRoot;
    const handlers = editor?.dropHandlers;
    if (root && handlers) {
        root.removeEventListener("dragenter", handlers.dragenter);
        root.removeEventListener("dragover", handlers.dragover);
        root.removeEventListener("dragleave", handlers.dragleave);
        root.removeEventListener("drop", handlers.drop);
        root.classList.remove("picture-file-drag-active");
        root.removeAttribute("data-picture-drop-mode");
    }
    if (editor) {
        editor.dropRoot = null;
        editor.dropHandlers = null;
        editor.dropDepth = 0;
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:releasePictureDropBindings@80', __javascriptError); throw __javascriptError; }}

function bindPictureDrop(editor, rootId, imageInputId, layeredInputId) { try {
    releasePictureDropBindings(editor);
    const root = document.getElementById(rootId);
    if (!root) return;
    const show = route => { try {
        root.classList.add("picture-file-drag-active");
        root.dataset.pictureDropMode = route || "picture";
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:show@102', __javascriptError); throw __javascriptError; }};
    const clear = () => { try {
        editor.dropDepth = 0;
        root.classList.remove("picture-file-drag-active");
        root.removeAttribute("data-picture-drop-mode");
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:clear@106', __javascriptError); throw __javascriptError; }};
    const descriptor = event => { try {
        const file = event.dataTransfer?.files?.[0];
        if (file) return file;
        const item = [...(event.dataTransfer?.items || [])].find(candidate => { try { return (candidate.kind === "file"); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:[...(event.dataTransfer?.items || [])].find@114', __javascriptError); throw __javascriptError; } });
        return item ? { name: "", type: item.type || "" } : null;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:descriptor@111', __javascriptError); throw __javascriptError; }};
    const dropPoint = event => { try {
        const canvas = editor.canvas;
        if (!canvas) return null;
        const bounds = canvas.getBoundingClientRect();
        if (event.clientX < bounds.left || event.clientX > bounds.right || event.clientY < bounds.top || event.clientY > bounds.bottom)
            return null;
        return canvasPoint(canvas, event);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:dropPoint@117', __javascriptError); throw __javascriptError; }};
    const handlers = {
        dragenter: event => { try {
            const file = descriptor(event);
            if (!file) return;
            event.preventDefault();
            editor.dropDepth++;
            show(pictureDropRoute(file));
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:dragenter@126', __javascriptError); throw __javascriptError; }},
        dragover: event => { try {
            const file = descriptor(event);
            if (!file) return;
            event.preventDefault();
            event.stopPropagation();
            event.dataTransfer.dropEffect = "copy";
            show(pictureDropRoute(file));
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:dragover@133', __javascriptError); throw __javascriptError; }},
        dragleave: event => { try {
            if (event.relatedTarget && root.contains(event.relatedTarget)) return;
            editor.dropDepth = Math.max(0, editor.dropDepth - 1);
            if (editor.dropDepth === 0) clear();
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:dragleave@141', __javascriptError); throw __javascriptError; }},
        drop: async event => { try {
            const file = event.dataTransfer?.files?.[0]
                || [...(event.dataTransfer?.items || [])].find(candidate => { try { return (candidate.kind === "file"); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:[...(event.dataTransfer?.items || [])].find@148', __javascriptError); throw __javascriptError; } })?.getAsFile?.();
            if (!file) return;
            event.preventDefault();
            event.stopPropagation();
            const route = pictureDropRoute(file);
            clear();
            const inputId = route === "layers" ? layeredInputId : route === "image" ? imageInputId : "";
            const point = dropPoint(event);
            try {
                await editor.dotNetRef?.invokeMethodAsync("PictureStudioFileDropPositioned", point?.x ?? null, point?.y ?? null);
            } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:suppressed-catch@158', __caughtJavaScriptError);  }
            if (!inputId || !assignDroppedFile(inputId, file)) {
                editor.dotNetRef?.invokeMethodAsync(
                    "PictureStudioFileDropRejected",
                    `The dropped file '${file.name || "file"}' is not a supported Picture Studio image or layered document.`).catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:promise-catch@160', __promiseError);   } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:editor.dotNetRef?.invokeMethodAsync( "PictureStudioFileDropRejected", @162', __javascriptError); throw __javascriptError; }});
            }
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:drop@146', __javascriptError); throw __javascriptError; }}
    };
    editor.dropRoot = root;
    editor.dropHandlers = handlers;
    editor.dropDepth = 0;
    root.addEventListener("dragenter", handlers.dragenter);
    root.addEventListener("dragover", handlers.dragover);
    root.addEventListener("dragleave", handlers.dragleave);
    root.addEventListener("drop", handlers.drop);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:bindPictureDrop@98', __javascriptError); throw __javascriptError; }}

function normalizeToolSettings(settings) { try {
    const rawTool = typeof settings?.tool === "string" ? settings.tool.toLowerCase() : "select";
    return {
        tool: drawTools.includes(rawTool) ? rawTool : "select",
        color: cssColor(settings?.color, "#111827"),
        secondaryColor: cssColor(settings?.secondaryColor, "#ffffff"),
        width: clamp(settings?.width ?? 12, .25, 512),
        opacity: clamp(settings?.opacity ?? 1, 0, 1),
        hardness: clamp(settings?.hardness ?? .8, 0, 1)
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:normalizeToolSettings@175', __javascriptError); throw __javascriptError; }}

function createCanvas(width, height) { try {
    const canvas = document.createElement("canvas");
    canvas.width = Math.max(1, Math.round(width));
    canvas.height = Math.max(1, Math.round(height));
    return canvas;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:createCanvas@187', __javascriptError); throw __javascriptError; }}

function loadImage(dataUrl) { try {
    if (!dataUrl) return Promise.resolve(null);
    const source = String(dataUrl).trim();
    if (!source.startsWith("data:image/") && !source.startsWith("blob:"))
        return Promise.reject(new Error("The image layer contains an invalid source instead of embedded image data."));
    if (imageCache.has(source)) return imageCache.get(source);
    const promise = new Promise((resolve, reject) => { try {
        const image = new Image();
        image.decoding = "async";
        image.onload = () => { try { return (resolve(image)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:image.onload@203', __javascriptError); throw __javascriptError; } };
        image.onerror = () => { try { return (reject(new Error("The image layer could not be decoded."))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:image.onerror@204', __javascriptError); throw __javascriptError; } };
        image.src = source;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:ArrowFunction@200', __javascriptError); throw __javascriptError; }}).catch(error => { try { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:promise-catch@200', error); 
        imageCache.delete(source);
        throw error;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:new Promise((resolve, reject) => { const image = new Image(); image.de@206', __javascriptError); throw __javascriptError; }});
    imageCache.set(source, promise);
    return promise;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:loadImage@194', __javascriptError); throw __javascriptError; }}

function svgMarkupDataUrl(markup) { try {
    const source = String(markup || "").trim();
    if (!source.toLowerCase().startsWith("<svg")) return "";
    return `data:image/svg+xml;charset=utf-8,${encodeURIComponent(source)}`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:svgMarkupDataUrl@214', __javascriptError); throw __javascriptError; }}

async function drawSvgLayer(ctx, layer) { try {
    const { width, height } = beginLayer(ctx, layer);
    try {
        const source = svgMarkupDataUrl(layer.svgMarkup);
        if (!source) throw new Error("The vector layer does not contain a standalone SVG document.");
        const image = await loadImage(source);
        if (!image) throw new Error("The vector layer could not be decoded.");
        drawImageWithFit(ctx, image, width, height, layer.preserveAspectRatio === false ? "stretch" : "contain");
        endLayer(ctx);
        return null;
    } catch (error) {
        endLayer(ctx);
        drawBrokenLayer(ctx, layer, "SVG");
        return `${layer.name || "Vector layer"}: ${error?.message || error}`;
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:drawSvgLayer@220', __javascriptError); throw __javascriptError; }}

function parseColor(value, fallback = [0, 0, 0, 255]) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:parseColor@237', __javascriptError); throw __javascriptError; }}

function cssColor(value, fallback = "#000000") { try {
    if (typeof value !== "string" || !value.trim()) return fallback;
    return value;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:cssColor@261', __javascriptError); throw __javascriptError; }}

function rgba(color, alpha = 1) { try {
    const parsed = parseColor(color, [0, 0, 0, 255]);
    return `rgba(${parsed[0]}, ${parsed[1]}, ${parsed[2]}, ${clamp(alpha * (parsed[3] / 255), 0, 1)})`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:rgba@266', __javascriptError); throw __javascriptError; }}

function mixColor(first, second, amount) { try {
    amount = clamp(amount, 0, 1);
    return [
        Math.round(first[0] + (second[0] - first[0]) * amount),
        Math.round(first[1] + (second[1] - first[1]) * amount),
        Math.round(first[2] + (second[2] - first[2]) * amount),
        Math.round(first[3] + (second[3] - first[3]) * amount)
    ];
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:mixColor@271', __javascriptError); throw __javascriptError; }}

function layerFilter(layer) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:layerFilter@281', __javascriptError); throw __javascriptError; }}

function beginLayer(ctx, layer) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:beginLayer@294', __javascriptError); throw __javascriptError; }}

function endLayer(ctx) { try {
    ctx.restore();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:endLayer@309', __javascriptError); throw __javascriptError; }}

function roundedRectanglePath(ctx, x, y, width, height, radius) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:roundedRectanglePath@313', __javascriptError); throw __javascriptError; }}

function drawImageWithFit(ctx, image, width, height, fit) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:drawImageWithFit@328', __javascriptError); throw __javascriptError; }}

async function drawRasterLayer(ctx, layer) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:drawRasterLayer@350', __javascriptError); throw __javascriptError; }}

function wrapText(ctx, text, maximumWidth) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:wrapText@393', __javascriptError); throw __javascriptError; }}

function drawTextLayer(ctx, layer) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:drawTextLayer@416', __javascriptError); throw __javascriptError; }}

function shapeFillStyle(ctx, layer, width, height) { try {
    const fillKind = enumName(layer.fillKind, fillKinds, "solid").toLowerCase();
    const first = cssColor(layer.fillColor, "#60a5fa");
    const second = cssColor(layer.secondaryFillColor, "#ffffff");
    if (fillKind === "solid") return first;
    if (fillKind === "radialgradient") {
        const gradient = ctx.createRadialGradient(0, 0, 0, 0, 0, Math.max(width, height) * .7);
        gradient.addColorStop(0, first); gradient.addColorStop(1, second); return gradient;
    }
    const angle = (Number(layer.fillAngleDegrees) || 0) * Math.PI / 180;
    const distance = Math.abs(width * Math.cos(angle)) + Math.abs(height * Math.sin(angle));
    const dx = Math.cos(angle) * distance / 2; const dy = Math.sin(angle) * distance / 2;
    const gradient = ctx.createLinearGradient(-dx, -dy, dx, dy);
    gradient.addColorStop(0, first); gradient.addColorStop(1, second); return gradient;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:shapeFillStyle@455', __javascriptError); throw __javascriptError; }}

function drawShapeLayer(ctx, layer) { try {
    const { width, height } = beginLayer(ctx, layer);
    const shape = enumName(layer.shape, shapeKinds, "rectangle").toLowerCase();
    const x = -width / 2;
    const y = -height / 2;
    ctx.fillStyle = shapeFillStyle(ctx, layer, width, height);
    ctx.strokeStyle = cssColor(layer.strokeColor, "#1d4ed8");
    ctx.lineWidth = clamp(layer.strokeWidthPx ?? 3, 0, 200);
    if (shape === "ellipse") {
        ctx.beginPath(); ctx.ellipse(0, 0, width / 2, height / 2, 0, 0, Math.PI * 2);
    } else if (shape === "freeform" || shape === "path") {
        const points = Array.isArray(layer.pathPoints) ? layer.pathPoints : [];
        const closed = shape === "freeform" ? true : layer.pathClosed === true;
        const smooth = layer.pathSmooth === true;
        ctx.beginPath();
        if (points.length) {
            const local = points.map(point => { try { return (({ x: (Number(point.x) || 0) - width / 2, y: (Number(point.y) || 0) - height / 2 })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:points.map@487', __javascriptError); throw __javascriptError; } });
            ctx.moveTo(local[0].x, local[0].y);
            if (smooth && local.length > 2) {
                for (let index = 1; index < local.length - 1; index++) {
                    const middleX = (local[index].x + local[index + 1].x) / 2;
                    const middleY = (local[index].y + local[index + 1].y) / 2;
                    ctx.quadraticCurveTo(local[index].x, local[index].y, middleX, middleY);
                }
                const last = local[local.length - 1];
                ctx.lineTo(last.x, last.y);
            } else {
                for (let index = 1; index < local.length; index++) ctx.lineTo(local[index].x, local[index].y);
            }
            if (closed) ctx.closePath();
        } else ctx.rect(x, y, width, height);
    } else if (shape === "arrow") {
        const shaftHalf = Math.max(1, height * .17);
        const headStart = Math.max(-width * .15, width * .08);
        ctx.beginPath(); ctx.moveTo(-width / 2, -shaftHalf); ctx.lineTo(headStart, -shaftHalf); ctx.lineTo(headStart, -height / 2);
        ctx.lineTo(width / 2, 0); ctx.lineTo(headStart, height / 2); ctx.lineTo(headStart, shaftHalf); ctx.lineTo(-width / 2, shaftHalf); ctx.closePath();
    } else if (shape === "line") {
        ctx.beginPath(); ctx.moveTo(-width / 2, 0); ctx.lineTo(width / 2, 0); if (ctx.lineWidth > 0) ctx.stroke(); endLayer(ctx); return;
    } else if (shape === "roundedrectangle") roundedRectanglePath(ctx, x, y, width, height, clamp(layer.cornerRadiusPx ?? 24, 0, 2000));
    else { ctx.beginPath(); ctx.rect(x, y, width, height); }
    if (layer.fillColor !== "transparent" && (shape !== "path" || layer.pathClosed === true)) ctx.fill();
    if (ctx.lineWidth > 0 && layer.strokeColor !== "transparent") ctx.stroke();
    endLayer(ctx);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:drawShapeLayer@471', __javascriptError); throw __javascriptError; }}

function createFillStyle(ctx, layer, width, height) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:createFillStyle@516', __javascriptError); throw __javascriptError; }}

function drawFillLayer(ctx, layer) { try {
    const { width, height } = beginLayer(ctx, layer);
    ctx.fillStyle = createFillStyle(ctx, layer, width, height);
    ctx.fillRect(-width / 2, -height / 2, width, height);
    endLayer(ctx);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:drawFillLayer@538', __javascriptError); throw __javascriptError; }}

function hashNoise(x, y, seed) { try {
    let value = Math.imul(x | 0, 374761393) + Math.imul(y | 0, 668265263) + Math.imul(seed | 0, 1442695041);
    value = (value ^ (value >>> 13)) | 0;
    value = Math.imul(value, 1274126177);
    value = value ^ (value >>> 16);
    return (value >>> 0) / 4294967295;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:hashNoise@545', __javascriptError); throw __javascriptError; }}

function smoothstep(value) { try {
    return value * value * (3 - 2 * value);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:smoothstep@553', __javascriptError); throw __javascriptError; }}

function valueNoise(x, y, seed) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:valueNoise@557', __javascriptError); throw __javascriptError; }}

function fractalNoise(x, y, seed, detail) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:fractalNoise@571', __javascriptError); throw __javascriptError; }}

function proceduralKey(layer, width, height) { try {
    return JSON.stringify([
        layer.renderKind, layer.primaryColor, layer.secondaryColor, layer.seed, layer.scale,
        layer.detail, layer.softness, layer.renderContrast, layer.angleDegrees, layer.stripeWidthPx,
        Math.round(width), Math.round(height)
    ]);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:proceduralKey@585', __javascriptError); throw __javascriptError; }}

function createNoiseOrClouds(layer, width, height, clouds) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:createNoiseOrClouds@593', __javascriptError); throw __javascriptError; }}

function createBloomCanvas(layer, width, height) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:createBloomCanvas@634', __javascriptError); throw __javascriptError; }}

function createNeonCanvas(layer, width, height) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:createNeonCanvas@658', __javascriptError); throw __javascriptError; }}

function createLensFlareCanvas(layer, width, height) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:createLensFlareCanvas@687', __javascriptError); throw __javascriptError; }}

function proceduralCanvasSize(width, height, maximum = 640) { try {
    const ratio = width / Math.max(1, height);
    return ratio >= 1
        ? { width: maximum, height: Math.max(64, Math.round(maximum / ratio)) }
        : { width: Math.max(64, Math.round(maximum * ratio)), height: maximum };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:proceduralCanvasSize@731', __javascriptError); throw __javascriptError; }}

function createGrainNoiseCanvas(layer, width, height) { try {
    const size = proceduralCanvasSize(width, height, 560);
    const canvas = createCanvas(size.width, size.height);
    const ctx = canvas.getContext("2d");
    const image = ctx.createImageData(canvas.width, canvas.height);
    const first = parseColor(layer.primaryColor, [15, 23, 42, 255]);
    const second = parseColor(layer.secondaryColor, [248, 250, 252, 255]);
    const seed = Number(layer.seed) || 1;
    const contrast = clamp(layer.renderContrast ?? 1, .1, 5);
    const softness = clamp(layer.softness ?? .25, 0, 1);
    for (let y = 0; y < canvas.height; y++) {
        for (let x = 0; x < canvas.width; x++) {
            const fine = hashNoise(x, y, seed);
            const coarse = valueNoise(x / 9, y / 9, seed + 31);
            let amount = fine * (1 - softness * .65) + coarse * softness * .65;
            amount = clamp(.5 + (amount - .5) * contrast, 0, 1);
            const color = mixColor(first, second, amount);
            const index = (y * canvas.width + x) * 4;
            image.data[index] = color[0];
            image.data[index + 1] = color[1];
            image.data[index + 2] = color[2];
            image.data[index + 3] = color[3];
        }
    }
    ctx.putImageData(image, 0, 0);
    return canvas;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:createGrainNoiseCanvas@738', __javascriptError); throw __javascriptError; }}

function createMotionBlurCanvas(layer, width, height) { try {
    const size = proceduralCanvasSize(width, height, 720);
    const canvas = createCanvas(size.width, size.height);
    const ctx = canvas.getContext("2d");
    const primary = cssColor(layer.primaryColor, "#0f172a");
    const secondary = cssColor(layer.secondaryColor, "#60a5fa");
    const angle = (Number(layer.angleDegrees) || 0) * Math.PI / 180;
    const seed = Number(layer.seed) || 1;
    const streakLength = clamp(layer.scale ?? 90, 12, 900) * canvas.width / Math.max(1, width);
    const count = Math.max(32, Math.min(420, Math.round((layer.detail ?? 4) * 58)));
    const background = ctx.createLinearGradient(0, 0, canvas.width, canvas.height);
    background.addColorStop(0, primary);
    background.addColorStop(1, rgba(layer.secondaryColor, .18));
    ctx.fillStyle = background;
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    ctx.save();
    ctx.translate(canvas.width / 2, canvas.height / 2);
    ctx.rotate(angle);
    ctx.translate(-canvas.width / 2, -canvas.height / 2);
    ctx.lineCap = "round";
    for (let index = 0; index < count; index++) {
        const y = hashNoise(index, 7, seed) * canvas.height;
        const x = hashNoise(index, 17, seed + 11) * canvas.width - streakLength * .5;
        const length = streakLength * (.15 + hashNoise(index, 29, seed + 23) * 1.15);
        const alpha = .04 + hashNoise(index, 37, seed + 41) * .26;
        const gradient = ctx.createLinearGradient(x, y, x + length, y);
        gradient.addColorStop(0, rgba(layer.secondaryColor, 0));
        gradient.addColorStop(.4, rgba(layer.secondaryColor, alpha));
        gradient.addColorStop(1, rgba(layer.primaryColor, 0));
        ctx.strokeStyle = gradient;
        ctx.lineWidth = .5 + hashNoise(index, 43, seed + 61) * Math.max(1.5, (layer.stripeWidthPx ?? 32) * .08);
        ctx.beginPath();
        ctx.moveTo(x, y);
        ctx.lineTo(x + length, y);
        ctx.stroke();
    }
    ctx.restore();
    ctx.globalCompositeOperation = "screen";
    ctx.globalAlpha = .18;
    ctx.fillStyle = secondary;
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    return canvas;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:createMotionBlurCanvas@766', __javascriptError); throw __javascriptError; }}

function createWindCanvas(layer, width, height) { try {
    const size = proceduralCanvasSize(width, height, 720);
    const canvas = createCanvas(size.width, size.height);
    const ctx = canvas.getContext("2d");
    const angle = (Number(layer.angleDegrees) || 0) * Math.PI / 180;
    const seed = Number(layer.seed) || 1;
    const background = ctx.createLinearGradient(0, 0, canvas.width, canvas.height);
    background.addColorStop(0, cssColor(layer.primaryColor, "#e0f2fe"));
    background.addColorStop(1, cssColor(layer.secondaryColor, "#0369a1"));
    ctx.fillStyle = background;
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    ctx.save();
    ctx.translate(canvas.width / 2, canvas.height / 2);
    ctx.rotate(angle);
    ctx.translate(-canvas.width / 2, -canvas.height / 2);
    const gusts = Math.max(18, Math.min(180, Math.round((layer.detail ?? 4) * 28)));
    for (let index = 0; index < gusts; index++) {
        const y = hashNoise(index, 19, seed) * canvas.height;
        const x = -canvas.width * .2 + hashNoise(index, 31, seed + 17) * canvas.width;
        const length = canvas.width * (.12 + hashNoise(index, 47, seed + 29) * .65);
        const lift = (hashNoise(index, 59, seed + 43) - .5) * canvas.height * .14;
        ctx.strokeStyle = rgba(index % 3 ? layer.primaryColor : layer.secondaryColor, .08 + hashNoise(index, 71, seed + 53) * .3);
        ctx.lineWidth = .7 + hashNoise(index, 83, seed + 67) * Math.max(1.2, (layer.stripeWidthPx ?? 32) * .06);
        ctx.beginPath();
        ctx.moveTo(x, y);
        ctx.bezierCurveTo(x + length * .28, y - lift, x + length * .65, y + lift, x + length, y - lift * .25);
        ctx.stroke();
    }
    ctx.restore();
    return canvas;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:createWindCanvas@810', __javascriptError); throw __javascriptError; }}

function createOceanWavesCanvas(layer, width, height) { try {
    const size = proceduralCanvasSize(width, height, 720);
    const canvas = createCanvas(size.width, size.height);
    const ctx = canvas.getContext("2d");
    const first = parseColor(layer.primaryColor, [224, 242, 254, 255]);
    const second = parseColor(layer.secondaryColor, [3, 105, 161, 255]);
    const seed = Number(layer.seed) || 1;
    const scale = clamp(layer.scale ?? 90, 8, 900);
    const detail = Math.max(1, Math.min(8, Math.round(layer.detail ?? 4)));
    const contrast = clamp(layer.renderContrast ?? 1, .1, 5);
    const image = ctx.createImageData(canvas.width, canvas.height);
    for (let y = 0; y < canvas.height; y++) {
        for (let x = 0; x < canvas.width; x++) {
            const nx = x / canvas.width;
            const ny = y / canvas.height;
            let wave = 0;
            let weight = 0;
            for (let octave = 1; octave <= detail; octave++) {
                const frequency = octave * (width / scale) * .85;
                const phase = hashNoise(octave, 17, seed) * Math.PI * 2;
                const amplitude = 1 / octave;
                wave += Math.sin((nx * frequency + ny * frequency * .42) * Math.PI * 2 + phase) * amplitude;
                wave += Math.cos((ny * frequency * .72 - nx * frequency * .22) * Math.PI * 2 + phase * .7) * amplitude * .55;
                weight += amplitude * 1.55;
            }
            let amount = .5 + wave / Math.max(.001, weight) * .5;
            amount += (valueNoise(nx * 8, ny * 8, seed + 101) - .5) * .18;
            amount = clamp(.5 + (amount - .5) * contrast, 0, 1);
            const foam = Math.max(0, (amount - .72) / .28);
            const color = mixColor(first, second, amount * .9);
            const index = (y * canvas.width + x) * 4;
            image.data[index] = Math.round(color[0] + (255 - color[0]) * foam * .65);
            image.data[index + 1] = Math.round(color[1] + (255 - color[1]) * foam * .72);
            image.data[index + 2] = Math.round(color[2] + (255 - color[2]) * foam * .8);
            image.data[index + 3] = color[3];
        }
    }
    ctx.putImageData(image, 0, 0);
    return canvas;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:createOceanWavesCanvas@842', __javascriptError); throw __javascriptError; }}

function getProceduralCanvas(layer, width, height) { try {
    const key = proceduralKey(layer, width, height);
    if (proceduralCache.has(key)) return proceduralCache.get(key);
    const kind = enumName(layer.renderKind, renderKinds, "clouds").toLowerCase();
    let canvas;
    switch (kind) {
        case "noise": canvas = createNoiseOrClouds(layer, width, height, false); break;
        case "bloom": canvas = createBloomCanvas(layer, width, height); break;
        case "neon": canvas = createNeonCanvas(layer, width, height); break;
        case "lensflare": canvas = createLensFlareCanvas(layer, width, height); break;
        case "grainnoise": canvas = createGrainNoiseCanvas(layer, width, height); break;
        case "motionblur": canvas = createMotionBlurCanvas(layer, width, height); break;
        case "wind": canvas = createWindCanvas(layer, width, height); break;
        case "oceanwaves": canvas = createOceanWavesCanvas(layer, width, height); break;
        default: canvas = createNoiseOrClouds(layer, width, height, true); break;
    }
    proceduralCache.set(key, canvas);
    if (proceduralCache.size > 40) proceduralCache.delete(proceduralCache.keys().next().value);
    return canvas;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:getProceduralCanvas@883', __javascriptError); throw __javascriptError; }}

function drawRenderLayer(ctx, layer) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:drawRenderLayer@904', __javascriptError); throw __javascriptError; }}

function strokeKind(stroke) { try {
    if (typeof stroke?.kind === "string") return stroke.kind.toLowerCase();
    return enumName(stroke?.kind, ["brush", "pencil", "spray", "toothbrush", "line", "eraser"], "brush").toLowerCase();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:strokeKind@932', __javascriptError); throw __javascriptError; }}

function traceStrokePath(ctx, points, kind) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:traceStrokePath@937', __javascriptError); throw __javascriptError; }}

function drawSprayStroke(ctx, points, width, color, opacity) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:drawSprayStroke@962', __javascriptError); throw __javascriptError; }}

function drawToothbrushStroke(ctx, points, width, color, opacity) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:drawToothbrushStroke@994', __javascriptError); throw __javascriptError; }}

function drawPaintStroke(ctx, stroke, preview = false) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:drawPaintStroke@1040', __javascriptError); throw __javascriptError; }}

function drawPaintLayer(ctx, layer) { try {
    const { width, height } = beginLayer(ctx, layer);
    const scratch = createCanvas(Math.max(1, Math.round(width)), Math.max(1, Math.round(height)));
    const scratchContext = scratch.getContext("2d", { alpha: true });
    for (const stroke of Array.isArray(layer.strokes) ? layer.strokes : [])
        drawPaintStroke(scratchContext, stroke);
    ctx.drawImage(scratch, -width / 2, -height / 2, width, height);
    endLayer(ctx);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:drawPaintLayer@1082', __javascriptError); throw __javascriptError; }}

function applyLayerClip(ctx, layer) { try {
    const points = Array.isArray(layer?.clipPolygon) ? layer.clipPolygon : [];
    if (points.length < 3) return;
    ctx.beginPath();
    if (layer.clipInverted === true) ctx.rect(0, 0, ctx.canvas.width, ctx.canvas.height);
    ctx.moveTo(Number(points[0].x) || 0, Number(points[0].y) || 0);
    for (let index = 1; index < points.length; index++)
        ctx.lineTo(Number(points[index].x) || 0, Number(points[index].y) || 0);
    ctx.closePath();
    ctx.clip(layer.clipInverted === true ? "evenodd" : "nonzero");
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:applyLayerClip@1092', __javascriptError); throw __javascriptError; }}

async function drawLayer(ctx, layer) { try {
    if (!layer || layer.visible === false || clamp(layer.opacity ?? 1, 0, 1) <= 0) return null;
    ctx.save();
    try {
        applyLayerClip(ctx, layer);
        switch (layerKind(layer)) {
            case "raster": return await drawRasterLayer(ctx, layer);
            case "text": drawTextLayer(ctx, layer); break;
            case "fill": drawFillLayer(ctx, layer); break;
            case "render": drawRenderLayer(ctx, layer); break;
            case "paint": drawPaintLayer(ctx, layer); break;
            case "svg": return await drawSvgLayer(ctx, layer);
            default: drawShapeLayer(ctx, layer); break;
        }
        return null;
    } finally {
        ctx.restore();
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:drawLayer@1104', __javascriptError); throw __javascriptError; }}

function drawBackground(ctx, document, forceOpaque = false) { try {
    const value = document.background || "transparent";
    if (value === "transparent" && !forceOpaque) return;
    ctx.save();
    ctx.fillStyle = value === "transparent" ? "#ffffff" : cssColor(value, "#ffffff");
    ctx.fillRect(0, 0, document.widthPx, document.heightPx);
    ctx.restore();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:drawBackground@1124', __javascriptError); throw __javascriptError; }}

function drawGrid(ctx, document, zoom) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:drawGrid@1133', __javascriptError); throw __javascriptError; }}

function localToWorld(layer, localX, localY) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:localToWorld@1152', __javascriptError); throw __javascriptError; }}

function worldToLocal(layer, worldX, worldY) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:worldToLocal@1166', __javascriptError); throw __javascriptError; }}

function selectionHandles(layer, zoom) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:selectionHandles@1180', __javascriptError); throw __javascriptError; }}

function drawSelection(ctx, layer, zoom) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:drawSelection@1193', __javascriptError); throw __javascriptError; }}

function isAreaSelectionTool(tool) { try { return ["rectangleselect", "ellipseselect", "freeselect", "magneticselect", "polygonselect"].includes(String(tool || "").toLowerCase());  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:isAreaSelectionTool@1233', __javascriptError); throw __javascriptError; }}
function isAreaFillTool(tool) { try { return ["fillsolid", "fillgradient"].includes(String(tool || "").toLowerCase());  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:isAreaFillTool@1234', __javascriptError); throw __javascriptError; }}
function selectionKindForTool(tool) { try {
    const name = String(tool || "").toLowerCase();
    return name === "ellipseselect" ? "ellipse" : name === "freeselect" ? "free" : name === "magneticselect" ? "magnetic" : name === "polygonselect" ? "polygon" : "rectangle";
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:selectionKindForTool@1235', __javascriptError); throw __javascriptError; }}
function selectionFromDrawing(drawing) { try {
    const points = Array.isArray(drawing?.points) ? drawing.points.map(point => { try { return (({ x: Number(point.x) || 0, y: Number(point.y) || 0 })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:drawing.points.map@1240', __javascriptError); throw __javascriptError; } }) : [];
    if (points.length < 2) return null;
    const kind = selectionKindForTool(drawing.tool);
    if (kind === "rectangle" || kind === "ellipse") {
        const first = points[0], last = points[points.length - 1];
        return { kind, points: [{ x: Math.min(first.x,last.x), y: Math.min(first.y,last.y) }, { x: Math.max(first.x,last.x), y: Math.max(first.y,last.y) }] };
    }
    return { kind, points };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:selectionFromDrawing@1239', __javascriptError); throw __javascriptError; }}
function selectionCoordinates(selection) { try {
    const points = Array.isArray(selection?.points) ? selection.points : [];
    const values = [];
    for (const point of points) values.push(Number(point.x) || 0, Number(point.y) || 0);
    return values;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:selectionCoordinates@1249', __javascriptError); throw __javascriptError; }}
function appendAreaSelectionPath(ctx, selection) { try {
    const points = Array.isArray(selection?.points) ? selection.points : [];
    if (points.length < 2) return false;
    if (selection.kind === "rectangle" || selection.kind === "ellipse") {
        const a = points[0], b = points[1];
        const x = Math.min(a.x, b.x), y = Math.min(a.y, b.y), w = Math.abs(b.x - a.x), h = Math.abs(b.y - a.y);
        if (selection.kind === "ellipse") ctx.ellipse(x + w / 2, y + h / 2, w / 2, h / 2, 0, 0, Math.PI * 2);
        else ctx.rect(x, y, w, h);
        return true;
    }
    ctx.moveTo(points[0].x, points[0].y);
    for (let index = 1; index < points.length; index++) ctx.lineTo(points[index].x, points[index].y);
    ctx.closePath();
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:appendAreaSelectionPath@1255', __javascriptError); throw __javascriptError; }}

function areaSelectionHandlePoints(selection) { try {
    const points = Array.isArray(selection?.points) ? selection.points : [];
    if (points.length < 2) return [];
    if (selection.kind === "rectangle" || selection.kind === "ellipse") {
        const a = points[0], b = points[1];
        const left = Math.min(a.x, b.x), top = Math.min(a.y, b.y), right = Math.max(a.x, b.x), bottom = Math.max(a.y, b.y);
        return [{ x: left, y: top }, { x: right, y: top }, { x: right, y: bottom }, { x: left, y: bottom }];
    }
    return points;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:areaSelectionHandlePoints@1271', __javascriptError); throw __javascriptError; }}

function drawSelectionModeVeil(ctx) { try {
    ctx.save();
    ctx.fillStyle = "rgba(2,6,23,.34)";
    ctx.fillRect(0, 0, ctx.canvas.width, ctx.canvas.height);
    ctx.restore();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:drawSelectionModeVeil@1282', __javascriptError); throw __javascriptError; }}

function drawAreaSelection(ctx, selection, zoom) { try {
    const points = Array.isArray(selection?.points) ? selection.points : [];
    if (points.length < 2) return;
    const scale = Math.max(.05, zoom);
    ctx.save();
    ctx.fillStyle = "rgba(2,6,23,.58)";
    ctx.beginPath();
    ctx.rect(0, 0, ctx.canvas.width, ctx.canvas.height);
    appendAreaSelectionPath(ctx, selection);
    ctx.fill("evenodd");

    ctx.strokeStyle = "#38bdf8";
    ctx.fillStyle = "rgba(14,165,233,.08)";
    ctx.lineWidth = Math.max(.5, 1.6 / scale);
    ctx.setLineDash([7 / scale, 5 / scale]);
    ctx.beginPath();
    appendAreaSelectionPath(ctx, selection);
    ctx.fill();
    ctx.stroke();
    ctx.setLineDash([]);

    const radius = Math.max(2.5, 4.5 / scale);
    for (const point of areaSelectionHandlePoints(selection)) {
        ctx.beginPath();
        ctx.arc(point.x, point.y, radius, 0, Math.PI * 2);
        ctx.fillStyle = "#ffffff";
        ctx.fill();
        ctx.strokeStyle = "#0284c7";
        ctx.lineWidth = Math.max(.5, 1.2 / scale);
        ctx.stroke();
    }
    ctx.restore();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:drawAreaSelection@1289', __javascriptError); throw __javascriptError; }}
function magneticSnapPoint(editor, point) { try {
    let best = point; let distance = 18 / Math.max(.05, editor.zoom || 1);
    for (const layer of editor.document?.layers || []) {
        if (!layer.visible) continue;
        const width=Math.max(1,Number(layer.width)||1), height=Math.max(1,Number(layer.height)||1);
        const candidates=[localToWorld(layer,-width/2,-height/2),localToWorld(layer,width/2,-height/2),localToWorld(layer,width/2,height/2),localToWorld(layer,-width/2,height/2),localToWorld(layer,0,-height/2),localToWorld(layer,width/2,0),localToWorld(layer,0,height/2),localToWorld(layer,-width/2,0)];
        for (const candidate of candidates) { const d=Math.hypot(candidate.x-point.x,candidate.y-point.y); if (d<distance) {distance=d;best=candidate;} }
    }
    return {x:best.x,y:best.y};
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:magneticSnapPoint@1322', __javascriptError); throw __javascriptError; }}
function commitAreaFill(editor, selection, gradient) { try {
    if (!selection) return;
    safeInvoke(editor, "PictureAreaFillCommitted", selection.kind, selectionCoordinates(selection), editor.toolSettings.color, editor.toolSettings.secondaryColor, gradient);
    editor.areaSelection = null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:commitAreaFill@1332', __javascriptError); throw __javascriptError; }}

function isShapeDrawingTool(tool) { try {
    return ["square", "rectangle", "ellipse", "arrow"].includes(String(tool || "").toLowerCase());
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:isShapeDrawingTool@1338', __javascriptError); throw __javascriptError; }}

function shapeDrawingGeometry(drawing) { try {
    const first = drawing?.points?.[0];
    const last = drawing?.points?.[drawing.points.length - 1];
    if (!first || !last) return null;
    const tool = String(drawing.tool || "").toLowerCase();
    const dx = (Number(last.x) || 0) - (Number(first.x) || 0);
    const dy = (Number(last.y) || 0) - (Number(first.y) || 0);
    if (tool === "arrow") {
        const length = Math.max(4, Math.hypot(dx, dy));
        const height = Math.max(8, Math.min(256, (Number(drawing.widthPx) || 8) * 3));
        const centerX = (Number(first.x) + Number(last.x)) / 2;
        const centerY = (Number(first.y) + Number(last.y)) / 2;
        return { tool, x: centerX - length / 2, y: centerY - height / 2, width: length, height, rotation: Math.atan2(dy, dx) * 180 / Math.PI };
    }
    if (tool === "square") {
        const side = Math.max(4, Math.max(Math.abs(dx), Math.abs(dy)));
        return {
            tool,
            x: dx >= 0 ? Number(first.x) : Number(first.x) - side,
            y: dy >= 0 ? Number(first.y) : Number(first.y) - side,
            width: side,
            height: side,
            rotation: 0
        };
    }
    return {
        tool,
        x: Math.min(Number(first.x), Number(last.x)),
        y: Math.min(Number(first.y), Number(last.y)),
        width: Math.max(4, Math.abs(dx)),
        height: Math.max(4, Math.abs(dy)),
        rotation: 0
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:shapeDrawingGeometry@1342', __javascriptError); throw __javascriptError; }}

function drawDrawingPreview(ctx, drawing) { try {
    if (!drawing) return;
    const effectiveTool = drawing.selectionTool || drawing.tool;
    if (isAreaSelectionTool(effectiveTool)) {
        const proxy = { ...drawing, tool: effectiveTool };
        const selection = selectionFromDrawing(proxy);
        if (selection) drawAreaSelection(ctx, selection, 1);
        return;
    }
    if (String(drawing.tool || "").toLowerCase() === "path") {
        const points = Array.isArray(drawing.points) ? drawing.points : [];
        if (points.length < 2) return;
        ctx.save();
        ctx.globalAlpha = Math.max(.45, clamp(drawing.opacity ?? 1, 0, 1));
        ctx.strokeStyle = cssColor(drawing.color, "#0284c7");
        ctx.lineWidth = Math.max(.25, Number(drawing.widthPx) || 2);
        ctx.lineCap = "round"; ctx.lineJoin = "round"; ctx.setLineDash([8, 5]);
        ctx.beginPath(); ctx.moveTo(points[0].x, points[0].y);
        for (let index = 1; index < points.length; index++) ctx.lineTo(points[index].x, points[index].y);
        ctx.stroke(); ctx.restore();
        return;
    }
    if (!isShapeDrawingTool(drawing.tool)) {
        drawPaintStroke(ctx, drawing, true);
        return;
    }
    const geometry = shapeDrawingGeometry(drawing);
    if (!geometry) return;
    ctx.save();
    ctx.globalAlpha = Math.max(.45, clamp(drawing.opacity ?? 1, 0, 1));
    ctx.strokeStyle = cssColor(drawing.color, "#0284c7");
    ctx.fillStyle = rgba(drawing.color, .15);
    ctx.lineWidth = Math.max(1, Math.min(8, Number(drawing.widthPx) || 2));
    ctx.setLineDash([8, 5]);
    if (geometry.tool === "arrow") {
        const first = drawing.points[0];
        const last = drawing.points[drawing.points.length - 1];
        const angle = Math.atan2((Number(last.y) || 0) - (Number(first.y) || 0), (Number(last.x) || 0) - (Number(first.x) || 0));
        const head = Math.max(10, Math.min(48, geometry.height * .8));
        ctx.beginPath();
        ctx.moveTo(Number(first.x) || 0, Number(first.y) || 0);
        ctx.lineTo(Number(last.x) || 0, Number(last.y) || 0);
        ctx.moveTo(Number(last.x) || 0, Number(last.y) || 0);
        ctx.lineTo((Number(last.x) || 0) - Math.cos(angle - .55) * head, (Number(last.y) || 0) - Math.sin(angle - .55) * head);
        ctx.moveTo(Number(last.x) || 0, Number(last.y) || 0);
        ctx.lineTo((Number(last.x) || 0) - Math.cos(angle + .55) * head, (Number(last.y) || 0) - Math.sin(angle + .55) * head);
        ctx.stroke();
    } else if (geometry.tool === "ellipse") {
        ctx.beginPath();
        ctx.ellipse(geometry.x + geometry.width / 2, geometry.y + geometry.height / 2, geometry.width / 2, geometry.height / 2, 0, 0, Math.PI * 2);
        ctx.fill();
        ctx.stroke();
    } else {
        ctx.fillRect(geometry.x, geometry.y, geometry.width, geometry.height);
        ctx.strokeRect(geometry.x, geometry.y, geometry.width, geometry.height);
    }
    ctx.restore();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:drawDrawingPreview@1377', __javascriptError); throw __javascriptError; }}

async function drawDocument(canvas, document, options = {}) { try {
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
    if (options.selectionMode && !options.previewStroke && !options.areaSelection) drawSelectionModeVeil(ctx);
    if (options.previewStroke) drawDrawingPreview(ctx, options.previewStroke);
    if (options.areaSelection) drawAreaSelection(ctx, options.areaSelection, options.zoom || 1);
    if (options.selectedLayerId) {
        const selected = document.layers.find(layer => { try { return (String(layer.id).toLowerCase() === String(options.selectedLayerId).toLowerCase()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:document.layers.find@1454', __javascriptError); throw __javascriptError; } });
        drawSelection(ctx, selected, options.zoom || 1);
    }
    canvas.pictureStudioErrors = errors;
    return canvas;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:drawDocument@1436', __javascriptError); throw __javascriptError; }}

function canvasPoint(canvas, event) { try {
    const bounds = canvas.getBoundingClientRect();
    return {
        x: (event.clientX - bounds.left) * canvas.width / Math.max(1, bounds.width),
        y: (event.clientY - bounds.top) * canvas.height / Math.max(1, bounds.height)
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:canvasPoint@1461', __javascriptError); throw __javascriptError; }}

function distanceToSegment(pointX, pointY, firstX, firstY, secondX, secondY) { try {
    const dx = secondX - firstX;
    const dy = secondY - firstY;
    if (Math.abs(dx) < .0001 && Math.abs(dy) < .0001) return Math.hypot(pointX - firstX, pointY - firstY);
    const amount = clamp(((pointX - firstX) * dx + (pointY - firstY) * dy) / (dx * dx + dy * dy), 0, 1);
    return Math.hypot(pointX - (firstX + dx * amount), pointY - (firstY + dy * amount));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:distanceToSegment@1469', __javascriptError); throw __javascriptError; }}

function hitPaintLayer(layer, worldX, worldY) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:hitPaintLayer@1477', __javascriptError); throw __javascriptError; }}

function pointInPolygon(points, x, y) { try {
    let inside = false;
    for (let index = 0, previous = points.length - 1; index < points.length; previous = index++) {
        const currentPoint = points[index];
        const previousPoint = points[previous];
        const currentX = Number(currentPoint?.x) || 0;
        const currentY = Number(currentPoint?.y) || 0;
        const previousX = Number(previousPoint?.x) || 0;
        const previousY = Number(previousPoint?.y) || 0;
        const crosses = (currentY > y) !== (previousY > y)
            && x < (previousX - currentX) * (y - currentY) / ((previousY - currentY) || .0000001) + currentX;
        if (crosses) inside = !inside;
    }
    return inside;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:pointInPolygon@1497', __javascriptError); throw __javascriptError; }}

function pointPassesLayerClip(layer, x, y) { try {
    const points = Array.isArray(layer?.clipPolygon) ? layer.clipPolygon : [];
    if (points.length < 3) return true;
    const inside = pointInPolygon(points, x, y);
    return layer.clipInverted === true ? !inside : inside;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:pointPassesLayerClip@1513', __javascriptError); throw __javascriptError; }}

function hitLayer(document, x, y) { try {
    for (let index = document.layers.length - 1; index >= 0; index--) {
        const layer = document.layers[index];
        if (!layer.visible) continue;
        if (!pointPassesLayerClip(layer, x, y)) continue;
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:hitLayer@1520', __javascriptError); throw __javascriptError; }}

function hitHandle(layer, x, y, zoom) { try {
    if (!layer || layer.locked || layerKind(layer) === "paint") return null;
    const handles = selectionHandles(layer, zoom);
    const threshold = 13 / Math.max(.05, zoom);
    for (const [name, point] of Object.entries(handles)) {
        if (Math.hypot(point.x - x, point.y - y) <= threshold) return name;
    }
    return null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:hitHandle@1538', __javascriptError); throw __javascriptError; }}

function safeInvoke(editor, method, ...args) { try {
    if (!editor?.dotNetRef) return Promise.resolve();
    return editor.dotNetRef.invokeMethodAsync(method, ...args).catch(error => { try { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:promise-catch@1550', error); 
        const message = String(error?.message || error || "");
        if (!/disconnected|disposed|circuit/i.test(message))
            console.warn(`Picture Studio callback ${method} failed.`, error);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:editor.dotNetRef.invokeMethodAsync(method, ...args).catch@1550', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:safeInvoke@1548', __javascriptError); throw __javascriptError; }}

function reportRenderState(editor, message) { try {
    const next = message || "";
    if (next === editor.lastRenderError) return;
    const hadError = Boolean(editor.lastRenderError);
    editor.lastRenderError = next;
    if (next) safeInvoke(editor, "PictureRenderFailed", next);
    else if (hadError) safeInvoke(editor, "PictureRenderRecovered");
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:reportRenderState@1557', __javascriptError); throw __javascriptError; }}

function scheduleEditorRender(editor) { try {
    if (editor.animationFrame || !editor.canvas || !editor.document) return;
    editor.animationFrame = requestAnimationFrame(async () => { try {
        editor.animationFrame = 0;
        const token = ++editor.renderToken;
        try {
            const rendered = await drawDocument(editor.canvas, editor.document, {
                grid: true,
                selectedLayerId: editor.selectedLayerId,
                zoom: editor.zoom,
                previewStroke: editor.pathDraft ? { ...editor.pathDraft, points: [...editor.pathDraft.points, ...(editor.pathDraft.hover ? [editor.pathDraft.hover] : [])] } : editor.drawing,
                areaSelection: editor.areaSelection,
                selectionMode: isAreaSelectionTool(editor.toolSettings?.tool)
            });
            if (token === editor.renderToken) {
                const errors = Array.isArray(rendered.pictureStudioErrors) ? rendered.pictureStudioErrors : [];
                reportRenderState(editor, errors[0] || "");
            }
        } catch (error) {
            reportRenderState(editor, error?.message || String(error));
        }
        if (token !== editor.renderToken) scheduleEditorRender(editor);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:requestAnimationFrame@1568', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:scheduleEditorRender@1566', __javascriptError); throw __javascriptError; }}

function snap(value, spacing, enabled) { try {
    return enabled ? Math.round(value / spacing) * spacing : value;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:snap@1591', __javascriptError); throw __javascriptError; }}

function resizeLayer(interaction, point) { try {
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:resizeLayer@1595', __javascriptError); throw __javascriptError; }}

function releaseEditorPointer(editor, pointerId) { try {
    if (!editor?.canvas || pointerId == null) return;
    try { editor.canvas.releasePointerCapture(pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:suppressed-catch@1623', __caughtJavaScriptError);  }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:releaseEditorPointer@1621', __javascriptError); throw __javascriptError; }}

function resetEditorPointerState(editor, cancel = true) { try {
    if (!editor) return;
    if (editor.drawing) finishDrawing(editor, null, cancel);
    if (editor.interaction) finishInteraction(editor, null, cancel);
    if (cancel && editor.pathDraft) {
        editor.pathDraft = null;
        scheduleEditorRender(editor);
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:resetEditorPointerState@1626', __javascriptError); throw __javascriptError; }}

function addPathNode(editor, event) { try {
    let point = canvasPoint(editor.canvas, event);
    if (editor.document.snapToGrid) {
        const spacing = Math.max(2, Number(editor.document.gridSpacingPx) || 25);
        point = { x: snap(point.x, spacing, true), y: snap(point.y, spacing, true) };
    }
    if (!editor.pathDraft) {
        editor.pathDraft = {
            tool: editor.toolSettings?.tool === "polygonselect" ? "polygonselect" : "path",
            color: editor.toolSettings.color,
            widthPx: editor.toolSettings.width,
            opacity: editor.toolSettings.opacity,
            points: [point],
            hover: point
        };
    } else {
        const last = editor.pathDraft.points[editor.pathDraft.points.length - 1];
        if (Math.hypot(point.x - last.x, point.y - last.y) > .25) editor.pathDraft.points.push(point);
        editor.pathDraft.hover = point;
    }
    scheduleEditorRender(editor);
    event.preventDefault();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:addPathNode@1636', __javascriptError); throw __javascriptError; }}

function finishPathDraft(editor, closed = false) { try {
    const draft = editor.pathDraft;
    editor.pathDraft = null;
    if (!draft) { scheduleEditorRender(editor); return; }
    if (draft.tool === "polygonselect") {
        if (draft.points.length >= 3)
            editor.areaSelection = { kind: "polygon", points: draft.points.map(point => { try { return (({ x: Number(point.x) || 0, y: Number(point.y) || 0 })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:draft.points.map@1666', __javascriptError); throw __javascriptError; } }) };
        scheduleEditorRender(editor);
        return;
    }
    if (draft.points.length < 2) { scheduleEditorRender(editor); return; }
    const coordinates = [];
    for (const point of draft.points) coordinates.push(Number(point.x) || 0, Number(point.y) || 0);
    safeInvoke(editor, "PicturePathCommitted", coordinates, closed === true, false);
    scheduleEditorRender(editor);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:finishPathDraft@1660', __javascriptError); throw __javascriptError; }}

function beginInteraction(editor, event) { try {
    if (!editor.document) return;
    if (editor.interaction || editor.drawing) resetEditorPointerState(editor, true);
    const point = canvasPoint(editor.canvas, event);
    let selected = editor.document.layers.find(layer => { try { return (String(layer.id) === String(editor.selectedLayerId)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:editor.document.layers.find@1681', __javascriptError); throw __javascriptError; } });
    const handle = hitHandle(selected, point.x, point.y, editor.zoom);
    if (!handle) {
        selected = hitLayer(editor.document, point.x, point.y);
        const nextId = selected ? String(selected.id) : null;
        if (nextId !== editor.selectedLayerId) {
            editor.selectedLayerId = nextId;
            safeInvoke(editor, "PictureLayerSelected", editor.selectedLayerId);
            scheduleEditorRender(editor);
        }
    }
    if (!selected || selected.locked || layerKind(selected) === "paint") return;
    const mode = handle || "move";
    editor.interaction = {
        mode,
        pointerId: event.pointerId,
        start: point,
        startClientX: event.clientX,
        startClientY: event.clientY,
        moved: false,
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
    try { editor.canvas.setPointerCapture(event.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:suppressed-catch@1711', __caughtJavaScriptError);  }
    if (handle) event.preventDefault();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:beginInteraction@1677', __javascriptError); throw __javascriptError; }}

function updateInteraction(editor, event) { try {
    const interaction = editor.interaction;
    if (!interaction || interaction.pointerId !== event.pointerId) return;
    if (event.pointerType === "mouse" && (event.buttons & 1) === 0) {
        finishInteraction(editor, event);
        return;
    }
    const movementPixels = Math.hypot(event.clientX - interaction.startClientX, event.clientY - interaction.startClientY);
    if (!interaction.moved && movementPixels < (interaction.mode === "move" ? 3 : 1.5)) return;
    interaction.moved = true;
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
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:updateInteraction@1715', __javascriptError); throw __javascriptError; }}

function finishInteraction(editor, event, cancel = false) { try {
    const interaction = editor.interaction;
    if (!interaction || (event && interaction.pointerId !== event.pointerId)) return;
    editor.interaction = null;
    releaseEditorPointer(editor, interaction.pointerId);
    if (cancel) Object.assign(interaction.layer, interaction.original);
    if (!cancel && interaction.moved) {
        safeInvoke(editor,
            "PictureTransformCommitted", String(interaction.layer.id),
            Number(interaction.layer.x) || 0, Number(interaction.layer.y) || 0,
            Math.max(1, Number(interaction.layer.width) || 1), Math.max(1, Number(interaction.layer.height) || 1),
            Number(interaction.layer.rotation) || 0
        );
    }
    scheduleEditorRender(editor);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:finishInteraction@1748', __javascriptError); throw __javascriptError; }}

async function pickCanvasColor(editor, point) { try {
    try {
        const clean = createCanvas(editor.document.widthPx, editor.document.heightPx);
        await drawDocument(clean, editor.document, { grid: false, selectedLayerId: null, zoom: 1 });
        const data = clean.getContext("2d", { willReadFrequently: true }).getImageData(
            Math.round(clamp(point.x, 0, clean.width - 1)), Math.round(clamp(point.y, 0, clean.height - 1)), 1, 1).data;
        const hex = `#${[data[0], data[1], data[2]].map(value => { try { return (value.toString(16).padStart(2, "0")); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:[data[0], data[1], data[2]].map@1771', __javascriptError); throw __javascriptError; } }).join("")}`;
        safeInvoke(editor, "PictureColorPicked", hex);
    } catch (error) {
        reportRenderState(editor, `The eyedropper could not read this pixel: ${error?.message || error}`);
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:pickCanvasColor@1765', __javascriptError); throw __javascriptError; }}

function beginDrawing(editor, event) { try {
    if (!editor.document) return;
    if (editor.interaction || editor.drawing) resetEditorPointerState(editor, true);
    const settings = editor.toolSettings || normalizeToolSettings(null);
    const point = canvasPoint(editor.canvas, event);
    if (settings.tool === "eyedropper") {
        void pickCanvasColor(editor, point);
        event.preventDefault();
        return;
    }
    if (isAreaFillTool(settings.tool) && editor.areaSelection) {
        commitAreaFill(editor, editor.areaSelection, settings.tool === "fillgradient");
        scheduleEditorRender(editor); event.preventDefault(); return;
    }
    if (settings.tool === "path" || settings.tool === "polygonselect") {
        // The second pointerdown of a double-click must not create a duplicate terminal node.
        if ((Number(event.detail) || 0) < 2) addPathNode(editor, event);
        else event.preventDefault();
        return;
    }
    if (!["brush", "pencil", "spray", "toothbrush", "square", "rectangle", "ellipse", "arrow", "line", "path", "eraser", "rectangleselect", "ellipseselect", "freeselect", "magneticselect", "polygonselect", "fillsolid", "fillgradient"].includes(settings.tool)) return;
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
        fillAfterSelection: isAreaFillTool(settings.tool),
        fillGradient: settings.tool === "fillgradient",
        selectionTool: isAreaFillTool(settings.tool) ? "rectangleselect" : settings.tool,
        points: [point, { ...point }]
    };
    try { editor.canvas.setPointerCapture(event.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:suppressed-catch@1820', __caughtJavaScriptError);  }
    scheduleEditorRender(editor);
    event.preventDefault();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:beginDrawing@1778', __javascriptError); throw __javascriptError; }}

function updateDrawing(editor, event) { try {
    const drawing = editor.drawing;
    if (!drawing || drawing.pointerId !== event.pointerId) return;
    if (event.pointerType === "mouse" && (event.buttons & 1) === 0) {
        finishDrawing(editor, event);
        return;
    }
    let point = canvasPoint(editor.canvas, event);
    const effectiveTool = drawing.selectionTool || drawing.tool;
    if (effectiveTool === "magneticselect") point = magneticSnapPoint(editor, point);
    const directShape = drawing.tool === "line" || isShapeDrawingTool(drawing.tool) || effectiveTool === "rectangleselect" || effectiveTool === "ellipseselect";
    if (directShape && editor.document.snapToGrid) {
        const spacing = Math.max(2, Number(editor.document.gridSpacingPx) || 25);
        point = { x: snap(point.x, spacing, true), y: snap(point.y, spacing, true) };
    }
    if (directShape) drawing.points[drawing.points.length - 1] = point;
    else {
        const last = drawing.points[drawing.points.length - 1];
        if (Math.hypot(point.x - last.x, point.y - last.y) >= Math.max(.5, drawing.widthPx * .06)) drawing.points.push(point);
    }
    scheduleEditorRender(editor);
    event.preventDefault();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:updateDrawing@1825', __javascriptError); throw __javascriptError; }}

function localizeStrokePoints(editor, points, tool) { try {
    let layer = editor.document.layers.find(item => { try { return (String(item.id) === String(editor.selectedLayerId)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:editor.document.layers.find@1850', __javascriptError); throw __javascriptError; } });
    if ((!layer || layer.locked || layerKind(layer) !== "paint") && tool === "eraser")
        layer = [...editor.document.layers].reverse().find(item => { try { return (layerKind(item) === "paint" && !item.locked); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:[...editor.document.layers].reverse().find@1852', __javascriptError); throw __javascriptError; } });
    if (!layer || layer.locked || layerKind(layer) !== "paint") return points;
    const width = Math.max(1, Number(layer.width) || 1);
    const height = Math.max(1, Number(layer.height) || 1);
    return points.map(point => { try {
        const local = worldToLocal(layer, point.x, point.y);
        return { x: local.x + width / 2, y: local.y + height / 2 };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:points.map@1856', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:localizeStrokePoints@1849', __javascriptError); throw __javascriptError; }}

function finishDrawing(editor, event, cancel = false) { try {
    const drawing = editor.drawing;
    if (!drawing || (event && drawing.pointerId !== event.pointerId)) return;
    editor.drawing = null;
    releaseEditorPointer(editor, drawing.pointerId);
    const effectiveTool = drawing.selectionTool || drawing.tool;
    if (!cancel && isAreaSelectionTool(effectiveTool)) {
        drawing.tool = effectiveTool;
        editor.areaSelection = selectionFromDrawing(drawing);
        if (drawing.fillAfterSelection) commitAreaFill(editor, editor.areaSelection, drawing.fillGradient === true);
        scheduleEditorRender(editor);
        return;
    }
    if (!cancel && isShapeDrawingTool(drawing.tool)) {
        const geometry = shapeDrawingGeometry(drawing);
        if (geometry) safeInvoke(editor, "PictureShapeCommitted", drawing.tool, geometry.x, geometry.y, geometry.width, geometry.height, geometry.rotation);
        scheduleEditorRender(editor);
        return;
    }
    if (!cancel && drawing.tool === "path") {
        const points = drawing.points;
        if (points.length >= 2) {
            const coordinates = [];
            for (const point of points) coordinates.push(Number(point.x) || 0, Number(point.y) || 0);
            safeInvoke(editor, "PicturePathCommitted", coordinates, false, true);
        }
        scheduleEditorRender(editor);
        return;
    }
    if (!cancel) {
        const points = localizeStrokePoints(editor, drawing.points, drawing.tool);
        if (points.length === 1) points.push({ x: points[0].x + .01, y: points[0].y + .01 });
        const coordinates = [];
        for (const point of points) coordinates.push(Number(point.x) || 0, Number(point.y) || 0);
        safeInvoke(editor, "PictureStrokeCommitted", drawing.tool, coordinates, drawing.color,
            drawing.widthPx, drawing.opacity, drawing.hardness);
    }
    scheduleEditorRender(editor);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:finishDrawing@1862', __javascriptError); throw __javascriptError; }}

function pictureSelectionModeLabel(tool) { try {
    const name = String(tool || "").toLowerCase();
    if (name === "ellipseselect") return "Ellipse selection · drag to select";
    if (name === "freeselect") return "Freehand selection · draw around the area";
    if (name === "magneticselect") return "Magnetic selection · trace nearby edges";
    if (name === "polygonselect") return "Polygon selection · click angled vertices";
    return "Rectangle selection · drag to select";
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:pictureSelectionModeLabel@1902', __javascriptError); throw __javascriptError; }}

function pictureGestureSurface(editor) { try {
    return editor.canvas?.closest?.(".picture-canvas-surface") || null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:pictureGestureSurface@1911', __javascriptError); throw __javascriptError; }}

function updatePictureGestureMode(editor) { try {
    const surface = pictureGestureSurface(editor);
    if (!surface) return;
    const tool = editor.toolSettings?.tool || "select";
    const active = isAreaSelectionTool(tool);
    surface.classList.toggle("selection-gesture-active", active);
    if (!active) surface.classList.remove("pointer-visible");
    const label = surface.querySelector(".picture-gesture-mode-label");
    if (label) label.dataset.modeLabel = active ? pictureSelectionModeLabel(tool) : "";
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:updatePictureGestureMode@1915', __javascriptError); throw __javascriptError; }}

function updatePictureGesturePointer(editor, event) { try {
    const surface = pictureGestureSurface(editor);
    if (!surface || !isAreaSelectionTool(editor.toolSettings?.tool)) return;
    const bounds = editor.canvas.getBoundingClientRect();
    const x = Math.max(0, Math.min(bounds.width, Number(event.clientX) - bounds.left));
    const y = Math.max(0, Math.min(bounds.height, Number(event.clientY) - bounds.top));
    surface.style.setProperty("--picture-pointer-x", `${x}px`);
    surface.style.setProperty("--picture-pointer-y", `${y}px`);
    surface.classList.add("pointer-visible");
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:updatePictureGesturePointer@1926', __javascriptError); throw __javascriptError; }}

function hidePictureGesturePointer(editor) { try {
    pictureGestureSurface(editor)?.classList.remove("pointer-visible");
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:hidePictureGesturePointer@1937', __javascriptError); throw __javascriptError; }}

function updateCanvasCursor(editor) { try {
    if (!editor.canvas) return;
    const tool = editor.toolSettings?.tool || "select";
    editor.canvas.style.cursor = isAreaSelectionTool(tool) ? "none" : tool === "select" ? "default" : tool === "eyedropper" ? "copy" : "crosshair";
    updatePictureGestureMode(editor);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:updateCanvasCursor@1941', __javascriptError); throw __javascriptError; }}

function bindEditorCanvas(editor, canvas) { try {
    if (editor.canvas === canvas && canvas.dataset.pictureStudioBound === "true") return;
    editor.canvas = canvas;
    editor.interaction = null;
    editor.drawing = null;
    editor.pathDraft = null;
    canvas.dataset.pictureStudioBound = "true";
    canvas.addEventListener("pointerdown", event => { try {
        canvas.focus({ preventScroll: true });
        if (event.button !== 0) return;
        if ((editor.toolSettings?.tool || "select") === "select") beginInteraction(editor, event);
        else beginDrawing(editor, event);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:canvas.addEventListener@1955', __javascriptError); throw __javascriptError; }});
    canvas.addEventListener("pointermove", event => { try {
        updatePictureGesturePointer(editor, event);
        if (editor.pathDraft && ["path", "polygonselect"].includes(editor.toolSettings?.tool || "select")) {
            editor.pathDraft.hover = canvasPoint(canvas, event);
            scheduleEditorRender(editor);
        } else if (editor.drawing) updateDrawing(editor, event);
        else updateInteraction(editor, event);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:canvas.addEventListener@1961', __javascriptError); throw __javascriptError; }});
    canvas.addEventListener("pointerup", event => { try {
        if (editor.drawing) finishDrawing(editor, event);
        else finishInteraction(editor, event);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:canvas.addEventListener@1969', __javascriptError); throw __javascriptError; }});
    canvas.addEventListener("pointercancel", event => { try {
        hidePictureGesturePointer(editor);
        if (editor.drawing) finishDrawing(editor, event, true);
        else finishInteraction(editor, event, true);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:canvas.addEventListener@1973', __javascriptError); throw __javascriptError; }});
    canvas.addEventListener("pointerleave", () => { try { return (hidePictureGesturePointer(editor)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:canvas.addEventListener@1978', __javascriptError); throw __javascriptError; } });
    canvas.addEventListener("lostpointercapture", event => { try {
        if (editor.drawing?.pointerId === event.pointerId) finishDrawing(editor, event, true);
        else if (editor.interaction?.pointerId === event.pointerId) finishInteraction(editor, event, true);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:canvas.addEventListener@1979', __javascriptError); throw __javascriptError; }});
    canvas.addEventListener("dblclick", event => { try {
        if (["path", "polygonselect"].includes(editor.toolSettings?.tool || "select")) {
            finishPathDraft(editor, event.shiftKey === true);
            event.preventDefault();
            return;
        }
        if ((editor.toolSettings?.tool || "select") !== "select") return;
        const point = canvasPoint(canvas, event);
        const layer = hitLayer(editor.document, point.x, point.y);
        const nextId = layer ? String(layer.id) : null;
        if (nextId !== editor.selectedLayerId) {
            editor.selectedLayerId = nextId;
            safeInvoke(editor, "PictureLayerSelected", nextId);
            scheduleEditorRender(editor);
        }
        event.preventDefault();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:canvas.addEventListener@1983', __javascriptError); throw __javascriptError; }});
    canvas.addEventListener("keydown", event => { try {
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

        if (editor.pathDraft && event.key === "Enter") {
            finishPathDraft(editor, event.shiftKey === true);
            event.preventDefault();
            return;
        }
        if (command) {
            safeInvoke(editor, "PictureShortcutRequested", command);
            event.preventDefault();
            return;
        }
        if (event.key === "Escape") {
            if (editor.pathDraft) { editor.pathDraft = null; scheduleEditorRender(editor); }
            else if (editor.drawing) finishDrawing(editor, null, true);
            else if (editor.interaction) finishInteraction(editor, null, true);
            else safeInvoke(editor, "PictureShortcutRequested", "select");
            event.preventDefault();
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:canvas.addEventListener@2000', __javascriptError); throw __javascriptError; }});
    updateCanvasCursor(editor);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:bindEditorCanvas@1948', __javascriptError); throw __javascriptError; }}

export function initializePictureStudio(canvasId, dotNetRef, rootId = "", imageInputId = "", layeredInputId = "") { try {
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
            pathDraft: null,
            areaSelection: null,
            toolSettings: normalizeToolSettings(null),
            animationFrame: 0,
            renderToken: 0,
            lastRenderError: "",
            dropRoot: null,
            dropHandlers: null,
            dropDepth: 0
        };
        const globalHandlers = {
            pointerdown: event => { try {
                if ((editor.drawing || editor.interaction) && event.target !== editor.canvas)
                    resetEditorPointerState(editor, true);
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:pointerdown@2058', __javascriptError); throw __javascriptError; }},
            pointerup: event => { try {
                if (editor.drawing?.pointerId === event.pointerId) finishDrawing(editor, event);
                else if (editor.interaction?.pointerId === event.pointerId) finishInteraction(editor, event);
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:pointerup@2062', __javascriptError); throw __javascriptError; }},
            pointercancel: event => { try {
                if (editor.drawing?.pointerId === event.pointerId) finishDrawing(editor, event, true);
                else if (editor.interaction?.pointerId === event.pointerId) finishInteraction(editor, event, true);
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:pointercancel@2066', __javascriptError); throw __javascriptError; }},
            blur: () => { try { return (resetEditorPointerState(editor, true)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:blur@2070', __javascriptError); throw __javascriptError; } },
            visibilitychange: () => { try { if (document.hidden) resetEditorPointerState(editor, true);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:visibilitychange@2071', __javascriptError); throw __javascriptError; }}
        };
        editor.globalHandlers = globalHandlers;
        window.addEventListener("pointerdown", globalHandlers.pointerdown, true);
        window.addEventListener("pointerup", globalHandlers.pointerup, true);
        window.addEventListener("pointercancel", globalHandlers.pointercancel, true);
        window.addEventListener("blur", globalHandlers.blur);
        document.addEventListener("visibilitychange", globalHandlers.visibilitychange);
        editors.set(canvasId, editor);
    } else {
        editor.dotNetRef = dotNetRef;
    }
    bindEditorCanvas(editor, canvas);
    bindPictureDrop(editor, rootId, imageInputId, layeredInputId);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:initializePictureStudio@2034', __javascriptError); throw __javascriptError; }}

export function cancelPictureStudioInteraction(canvasId) { try {
    const editor = editors.get(canvasId);
    if (editor) resetEditorPointerState(editor, true);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:cancelPictureStudioInteraction@2087', __javascriptError); throw __javascriptError; }}

export function disposePictureStudio(canvasId) { try {
    const editor = editors.get(canvasId);
    if (!editor) return;
    resetEditorPointerState(editor, true);
    if (editor.animationFrame) cancelAnimationFrame(editor.animationFrame);
    releasePictureDropBindings(editor);
    const handlers = editor.globalHandlers;
    if (handlers) {
        window.removeEventListener("pointerdown", handlers.pointerdown, true);
        window.removeEventListener("pointerup", handlers.pointerup, true);
        window.removeEventListener("pointercancel", handlers.pointercancel, true);
        window.removeEventListener("blur", handlers.blur);
        document.removeEventListener("visibilitychange", handlers.visibilitychange);
    }
    if (editor.canvas) delete editor.canvas.dataset.pictureStudioBound;
    const gestureSurface = pictureGestureSurface(editor);
    if (gestureSurface) {
        gestureSurface.classList.remove("selection-gesture-active", "pointer-visible");
        gestureSurface.style.removeProperty("--picture-pointer-x");
        gestureSurface.style.removeProperty("--picture-pointer-y");
    }
    editor.dotNetRef = null;
    editor.canvas = null;
    editor.document = null;
    editor.globalHandlers = null;
    editors.delete(canvasId);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:disposePictureStudio@2092', __javascriptError); throw __javascriptError; }}

export async function renderPictureStudio(canvasId, documentModel, selectedLayerId, zoom, toolSettings) { try {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const editor = editors.get(canvasId);
    if (!editor) return;
    const nextDocument = cloneDocument(documentModel);
    editor.selectedLayerId = selectedLayerId || null;
    editor.zoom = clamp(zoom ?? nextDocument.zoom, .05, 4);
    const nextToolSettings = normalizeToolSettings(toolSettings);
    if (editor.pathDraft && !["path", "polygonselect"].includes(nextToolSettings.tool)) editor.pathDraft = null;
    editor.toolSettings = nextToolSettings;
    canvas.style.width = `${Math.round(nextDocument.widthPx * editor.zoom)}px`;
    canvas.style.height = `${Math.round(nextDocument.heightPx * editor.zoom)}px`;
    if (!editor.interaction && !editor.drawing && !editor.pathDraft) editor.document = nextDocument;
    updateCanvasCursor(editor);
    scheduleEditorRender(editor);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:renderPictureStudio@2120', __javascriptError); throw __javascriptError; }}

export function hitTestPictureStudioLayer(canvasId, clientX, clientY) { try {
    const editor = editors.get(canvasId);
    const canvas = document.getElementById(canvasId);
    if (!editor?.document || !canvas) return null;
    const point = canvasPoint(canvas, { clientX: Number(clientX) || 0, clientY: Number(clientY) || 0 });
    const layer = hitLayer(editor.document, point.x, point.y);
    return layer ? String(layer.id) : null;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:hitTestPictureStudioLayer@2138', __javascriptError); throw __javascriptError; }}

export function clearPictureStudioAreaSelection(canvasId) { try {
    const editor = editors.get(canvasId);
    if (!editor) return;
    editor.areaSelection = null;
    scheduleEditorRender(editor);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:clearPictureStudioAreaSelection@2147', __javascriptError); throw __javascriptError; }}

export function getPictureStudioAreaSelection(canvasId) { try {
    const editor = editors.get(canvasId);
    const selection = editor?.areaSelection;
    if (!selection || !Array.isArray(selection.points) || selection.points.length < 2) return null;
    return {
        kind: String(selection.kind || "polygon"),
        points: selection.points.map(point => { try { return (({ x: Number(point.x) || 0, y: Number(point.y) || 0 })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:selection.points.map@2160', __javascriptError); throw __javascriptError; } })
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:getPictureStudioAreaSelection@2154', __javascriptError); throw __javascriptError; }}

export function hasPictureStudioAreaSelection(canvasId) { try {
    const selection = getPictureStudioAreaSelection(canvasId);
    return Boolean(selection && selection.points.length >= 2);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:hasPictureStudioAreaSelection@2164', __javascriptError); throw __javascriptError; }}

export function fitPictureStudio(hostId, width, height) { try {
    const host = document.getElementById(hostId);
    if (!host) return .65;
    const bounds = host.getBoundingClientRect();
    const availableWidth = Math.max(100, bounds.width - 90);
    const availableHeight = Math.max(100, bounds.height - 90);
    return clamp(Math.min(availableWidth / Math.max(1, width), availableHeight / Math.max(1, height)), .05, 2);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:fitPictureStudio@2169', __javascriptError); throw __javascriptError; }}

export async function getPictureImageSize(dataUrl) { try {
    const image = await loadImage(dataUrl);
    return { width: image?.naturalWidth || 0, height: image?.naturalHeight || 0 };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:getPictureImageSize@2178', __javascriptError); throw __javascriptError; }}

async function renderExportCanvas(documentModel, mimeType) { try {
    const document = cloneDocument(documentModel);
    const canvas = createCanvas(document.widthPx, document.heightPx);
    await drawDocument(canvas, document, {
        grid: false,
        selectedLayerId: null,
        zoom: 1,
        forceOpaque: mimeType === "image/jpeg"
    });
    return canvas;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:renderExportCanvas@2183', __javascriptError); throw __javascriptError; }}

function canvasToBlob(canvas, mimeType, quality) { try {
    return new Promise((resolve, reject) => { try {
        canvas.toBlob(blob => { try { return (blob ? resolve(blob) : reject(new Error("The browser could not rasterize the picture."))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:canvas.toBlob@2197', __javascriptError); throw __javascriptError; } }, mimeType, quality);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:ArrowFunction@2196', __javascriptError); throw __javascriptError; }});
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:canvasToBlob@2195', __javascriptError); throw __javascriptError; }}

async function createPictureStudioBlob(documentModel, mimeType = "image/png", quality = 1) { try {
    const canvas = await renderExportCanvas(documentModel, mimeType);
    return await canvasToBlob(canvas, mimeType, quality);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:createPictureStudioBlob@2201', __javascriptError); throw __javascriptError; }}

const pictureExportChunkSize = 24 * 1024;

export function startPictureStudioDataUrlExport(
    documentModel,
    mimeType = "image/png",
    quality = 1,
    dotNetReference,
    exportId) { try {
    // Return immediately so the initiating .NET -> JS call is finished before
    // JavaScript starts invoking .NET with the generated image chunks.
    void exportPictureStudioDataUrlInChunks(
        documentModel,
        mimeType,
        quality,
        dotNetReference,
        exportId);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:startPictureStudioDataUrlExport@2208', __javascriptError); throw __javascriptError; }}

async function exportPictureStudioDataUrlInChunks(
    documentModel,
    mimeType,
    quality,
    dotNetReference,
    exportId) { try {
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
        } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:suppressed-catch@2260', __caughtJavaScriptError); 
            // The Blazor circuit may already be gone.
        }
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:exportPictureStudioDataUrlInChunks@2224', __javascriptError); throw __javascriptError; }}

function xmlEscape(value) { try {
    return String(value ?? "").replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;").replaceAll('"', "&quot;");
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:xmlEscape@2266', __javascriptError); throw __javascriptError; }}

function svgNumber(value, fallback = 0) { try {
    const number = Number(value);
    return Number.isFinite(number) ? Math.round(number * 1000) / 1000 : fallback;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:svgNumber@2270', __javascriptError); throw __javascriptError; }}

function svgIdentifier(value, fallback = "layer") { try {
    const identifier = String(value || fallback).replace(/[^a-z0-9_-]/gi, "-");
    return identifier || fallback;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:svgIdentifier@2275', __javascriptError); throw __javascriptError; }}

function svgLayerTransform(layer) { try {
    const width = Math.max(1, svgNumber(layer.width, 1));
    const height = Math.max(1, svgNumber(layer.height, 1));
    const centerX = svgNumber(layer.x) + width / 2;
    const centerY = svgNumber(layer.y) + height / 2;
    return `translate(${centerX} ${centerY}) rotate(${svgNumber(layer.rotation)})`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:svgLayerTransform@2280', __javascriptError); throw __javascriptError; }}

function svgLayerStyle(layer, includeAdjustments = true) { try {
    const styles = [];
    const opacity = clamp(layer.opacity ?? 1, 0, 1);
    if (opacity < .9999) styles.push(`opacity:${opacity}`);
    const blend = blendMode(layer.blendMode);
    if (blend && blend !== "source-over") styles.push(`mix-blend-mode:${blend}`);
    if (includeAdjustments) {
        const filter = layerFilter(layer);
        if (filter && filter !== "brightness(1) contrast(1) saturate(1) hue-rotate(0deg) blur(0px) grayscale(0) sepia(0) invert(0)")
            styles.push(`filter:${filter}`);
    }
    return styles.join(";");
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:svgLayerStyle@2288', __javascriptError); throw __javascriptError; }}

function svgLayerClip(definitions, layer, prefix, documentWidth, documentHeight) { try {
    const points = Array.isArray(layer?.clipPolygon) ? layer.clipPolygon : [];
    if (points.length < 3) return "";
    const polygon = points
        .map((point, index) => { try { return (`${index === 0 ? "M" : "L"} ${svgNumber(point?.x)} ${svgNumber(point?.y)}`); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:points .map@2306', __javascriptError); throw __javascriptError; } })
        .join(" ");
    const id = `${prefix}-area-clip`;
    const path = layer.clipInverted === true
        ? `M 0 0 H ${svgNumber(documentWidth)} V ${svgNumber(documentHeight)} H 0 Z ${polygon} Z`
        : `${polygon} Z`;
    definitions.push(`<clipPath id="${id}" clipPathUnits="userSpaceOnUse"><path d="${path}" clip-rule="evenodd" fill-rule="evenodd"/></clipPath>`);
    return id;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:svgLayerClip@2302', __javascriptError); throw __javascriptError; }}

function svgGradient(definitions, layer, width, height, prefix, shape = false) { try {
    const fillKind = enumName(layer.fillKind, fillKinds, "solid").toLowerCase();
    const first = cssColor(shape ? layer.fillColor : layer.primaryColor, shape ? "#60a5fa" : "#dbeafe");
    const second = cssColor(shape ? layer.secondaryFillColor : layer.secondaryColor, shape ? "#ffffff" : "#6366f1");
    if (fillKind === "solid") return first;
    const id = `${prefix}-${fillKind}`;
    if (fillKind === "radialgradient") {
        const radius = Math.max(width, height) * .7;
        definitions.push(`<radialGradient id="${id}" gradientUnits="userSpaceOnUse" cx="0" cy="0" r="${svgNumber(radius)}"><stop offset="0" stop-color="${xmlEscape(first)}"/><stop offset="1" stop-color="${xmlEscape(second)}"/></radialGradient>`);
    } else {
        const angle = svgNumber(shape ? layer.fillAngleDegrees : layer.angleDegrees) * Math.PI / 180;
        const distance = Math.abs(width * Math.cos(angle)) + Math.abs(height * Math.sin(angle));
        const dx = Math.cos(angle) * distance / 2;
        const dy = Math.sin(angle) * distance / 2;
        definitions.push(`<linearGradient id="${id}" gradientUnits="userSpaceOnUse" x1="${svgNumber(-dx)}" y1="${svgNumber(-dy)}" x2="${svgNumber(dx)}" y2="${svgNumber(dy)}"><stop offset="0" stop-color="${xmlEscape(first)}"/><stop offset="1" stop-color="${xmlEscape(second)}"/></linearGradient>`);
    }
    return `url(#${id})`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:svgGradient@2316', __javascriptError); throw __javascriptError; }}

function svgPathData(layer, width, height, closeOverride = null) { try {
    const points = Array.isArray(layer.pathPoints) ? layer.pathPoints : [];
    if (!points.length) return `M ${svgNumber(-width / 2)} ${svgNumber(-height / 2)} H ${svgNumber(width / 2)} V ${svgNumber(height / 2)} H ${svgNumber(-width / 2)} Z`;
    const local = points.map(point => { try { return (({
        x: svgNumber(point.x) - width / 2,
        y: svgNumber(point.y) - height / 2
    })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:points.map@2338', __javascriptError); throw __javascriptError; } });
    const commands = [`M ${svgNumber(local[0].x)} ${svgNumber(local[0].y)}`];
    if (layer.pathSmooth === true && local.length > 2) {
        for (let index = 1; index < local.length - 1; index++) {
            const middleX = (local[index].x + local[index + 1].x) / 2;
            const middleY = (local[index].y + local[index + 1].y) / 2;
            commands.push(`Q ${svgNumber(local[index].x)} ${svgNumber(local[index].y)} ${svgNumber(middleX)} ${svgNumber(middleY)}`);
        }
        const last = local[local.length - 1];
        commands.push(`L ${svgNumber(last.x)} ${svgNumber(last.y)}`);
    } else {
        for (let index = 1; index < local.length; index++)
            commands.push(`L ${svgNumber(local[index].x)} ${svgNumber(local[index].y)}`);
    }
    const closed = closeOverride ?? layer.pathClosed === true;
    if (closed) commands.push("Z");
    return commands.join(" ");
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:svgPathData@2335', __javascriptError); throw __javascriptError; }}

function svgShapeLayer(layer, definitions, prefix) { try {
    const width = Math.max(1, svgNumber(layer.width, 1));
    const height = Math.max(1, svgNumber(layer.height, 1));
    const shape = enumName(layer.shape, shapeKinds, "rectangle").toLowerCase();
    const stroke = cssColor(layer.strokeColor, "#1d4ed8");
    const strokeWidth = clamp(layer.strokeWidthPx ?? 3, 0, 200);
    const openPath = shape === "path" && layer.pathClosed !== true;
    const fill = openPath || shape === "line" ? "none" : svgGradient(definitions, layer, width, height, prefix, true);
    const common = `fill="${xmlEscape(fill)}" stroke="${xmlEscape(stroke)}" stroke-width="${svgNumber(strokeWidth)}" stroke-linecap="round" stroke-linejoin="round"`;
    let markup;
    if (shape === "ellipse") {
        markup = `<ellipse cx="0" cy="0" rx="${svgNumber(width / 2)}" ry="${svgNumber(height / 2)}" ${common}/>`;
    } else if (shape === "line") {
        markup = `<path d="M ${svgNumber(-width / 2)} 0 L ${svgNumber(width / 2)} 0" ${common}/>`;
    } else if (shape === "arrow") {
        const shaftHalf = Math.max(1, height * .17);
        const headStart = Math.max(-width * .15, width * .08);
        const d = `M ${svgNumber(-width / 2)} ${svgNumber(-shaftHalf)} L ${svgNumber(headStart)} ${svgNumber(-shaftHalf)} L ${svgNumber(headStart)} ${svgNumber(-height / 2)} L ${svgNumber(width / 2)} 0 L ${svgNumber(headStart)} ${svgNumber(height / 2)} L ${svgNumber(headStart)} ${svgNumber(shaftHalf)} L ${svgNumber(-width / 2)} ${svgNumber(shaftHalf)} Z`;
        markup = `<path d="${d}" ${common}/>`;
    } else if (shape === "freeform" || shape === "path") {
        markup = `<path d="${svgPathData(layer, width, height, shape === "freeform" ? true : null)}" ${common}/>`;
    } else if (shape === "roundedrectangle") {
        const radius = Math.min(clamp(layer.cornerRadiusPx ?? 24, 0, 2000), width / 2, height / 2);
        markup = `<rect x="${svgNumber(-width / 2)}" y="${svgNumber(-height / 2)}" width="${svgNumber(width)}" height="${svgNumber(height)}" rx="${svgNumber(radius)}" ry="${svgNumber(radius)}" ${common}/>`;
    } else {
        markup = `<rect x="${svgNumber(-width / 2)}" y="${svgNumber(-height / 2)}" width="${svgNumber(width)}" height="${svgNumber(height)}" ${common}/>`;
    }
    return markup;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:svgShapeLayer@2360', __javascriptError); throw __javascriptError; }}

function svgFillLayer(layer, definitions, prefix) { try {
    const width = Math.max(1, svgNumber(layer.width, 1));
    const height = Math.max(1, svgNumber(layer.height, 1));
    const fill = svgGradient(definitions, layer, width, height, prefix, false);
    return `<rect x="${svgNumber(-width / 2)}" y="${svgNumber(-height / 2)}" width="${svgNumber(width)}" height="${svgNumber(height)}" fill="${xmlEscape(fill)}"/>`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:svgFillLayer@2390', __javascriptError); throw __javascriptError; }}

function svgTextLayer(layer, definitions, prefix) { try {
    const width = Math.max(1, svgNumber(layer.width, 1));
    const height = Math.max(1, svgNumber(layer.height, 1));
    const fontSize = clamp(layer.fontSizePx ?? 72, 4, 1024);
    const family = layer.fontFamily || "Segoe UI";
    const measure = createCanvas(1, 1).getContext("2d");
    measure.font = `${layer.italic ? "italic " : ""}${layer.bold ? "700 " : "400 "}${fontSize}px ${family}`;
    const lines = wrapText(measure, layer.text, width);
    const lineHeight = fontSize * 1.18;
    const totalHeight = lines.length * lineHeight;
    const top = Math.max(-height / 2, -totalHeight / 2);
    const alignment = enumName(layer.alignment, textAlignments, "center").toLowerCase();
    const anchor = alignment === "left" ? "start" : alignment === "right" ? "end" : "middle";
    const x = alignment === "left" ? -width / 2 : alignment === "right" ? width / 2 : 0;
    const clipId = `${prefix}-clip`;
    definitions.push(`<clipPath id="${clipId}"><rect x="${svgNumber(-width / 2)}" y="${svgNumber(-height / 2)}" width="${svgNumber(width)}" height="${svgNumber(height)}"/></clipPath>`);
    const shadow = layer.shadowEnabled
        ? `filter:drop-shadow(${svgNumber(layer.shadowOffsetXPx)}px ${svgNumber(layer.shadowOffsetYPx)}px ${svgNumber(clamp(layer.shadowBlurPx ?? 8, 0, 200))}px ${cssColor(layer.shadowColor, "#00000080")})`
        : "";
    const outlineWidth = clamp(layer.outlineWidthPx ?? 0, 0, 64) * 2;
    const outline = outlineWidth > 0 && layer.outlineColor !== "transparent"
        ? `stroke="${xmlEscape(cssColor(layer.outlineColor, "#000000"))}" stroke-width="${svgNumber(outlineWidth)}" paint-order="stroke fill" stroke-linejoin="round"`
        : "";
    const spans = lines.map((line, index) => { try { return (`<tspan x="${svgNumber(x)}" y="${svgNumber(top + fontSize * .86 + index * lineHeight)}">${xmlEscape(line)}</tspan>`); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:lines.map@2420', __javascriptError); throw __javascriptError; } }).join("");
    return `<text clip-path="url(#${clipId})" x="${svgNumber(x)}" text-anchor="${anchor}" font-family="${xmlEscape(family)}" font-size="${svgNumber(fontSize)}" font-weight="${layer.bold ? "700" : "400"}" font-style="${layer.italic ? "italic" : "normal"}" fill="${xmlEscape(cssColor(layer.fillColor, "#17365d"))}" ${outline} style="${xmlEscape(shadow)}">${spans}</text>`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:svgTextLayer@2397', __javascriptError); throw __javascriptError; }}

async function svgRasterizedLayer(layer) { try {
    const width = Math.max(1, Math.round(svgNumber(layer.width, 1)));
    const height = Math.max(1, Math.round(svgNumber(layer.height, 1)));
    const canvas = createCanvas(width, height);
    const context = canvas.getContext("2d", { alpha: true, desynchronized: false });
    context.clearRect(0, 0, width, height);
    const local = {
        ...layer,
        x: 0,
        y: 0,
        width,
        height,
        rotation: 0,
        opacity: 1,
        blendMode: "Normal",
        clipPolygon: [],
        clipInverted: false
    };
    await drawLayer(context, local);
    const dataUrl = canvas.toDataURL("image/png");
    return `<image x="${svgNumber(-width / 2)}" y="${svgNumber(-height / 2)}" width="${width}" height="${height}" href="${dataUrl}" preserveAspectRatio="none"/>`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:svgRasterizedLayer@2424', __javascriptError); throw __javascriptError; }}

export async function createPictureStudioSvg(documentModel) { try {
    const document = normalizeDocument(documentModel);
    const definitions = [];
    const layers = [];
    for (let index = 0; index < document.layers.length; index++) {
        const layer = document.layers[index];
        if (!layer || layer.visible === false || clamp(layer.opacity ?? 1, 0, 1) <= 0) continue;
        const kind = layerKind(layer);
        const prefix = `ps-${svgIdentifier(layer.id, `layer-${index}`)}-${index}`;
        let markup;
        let adjustments = true;
        if (kind === "shape") markup = svgShapeLayer(layer, definitions, prefix);
        else if (kind === "text") markup = svgTextLayer(layer, definitions, prefix);
        else if (kind === "fill") markup = svgFillLayer(layer, definitions, prefix);
        else if (kind === "svg") {
            const source = svgMarkupDataUrl(layer.svgMarkup);
            markup = `<image x="${svgNumber(-Math.max(1, svgNumber(layer.width, 1)) / 2)}" y="${svgNumber(-Math.max(1, svgNumber(layer.height, 1)) / 2)}" width="${svgNumber(Math.max(1, svgNumber(layer.width, 1)))}" height="${svgNumber(Math.max(1, svgNumber(layer.height, 1)))}" href="${xmlEscape(source)}" preserveAspectRatio="${layer.preserveAspectRatio === false ? "none" : "xMidYMid meet"}"/>`;
        } else {
            markup = await svgRasterizedLayer(layer);
            adjustments = false;
        }
        const style = svgLayerStyle(layer, adjustments);
        const transformed = `<g id="${prefix}" data-picture-layer-id="${xmlEscape(layer.id || "")}" data-picture-layer-kind="${xmlEscape(kind)}" transform="${svgLayerTransform(layer)}"${style ? ` style="${xmlEscape(style)}"` : ""}>${markup}</g>`;
        const clipId = svgLayerClip(definitions, layer, prefix, document.widthPx, document.heightPx);
        layers.push(clipId ? `<g clip-path="url(#${clipId})">${transformed}</g>` : transformed);
    }
    const metadata = xmlEscape(JSON.stringify(document));
    const background = document.background && document.background !== "transparent"
        ? `<rect width="100%" height="100%" fill="${xmlEscape(document.background)}"/>`
        : "";
    const defs = definitions.length ? `<defs>${definitions.join("")}</defs>` : "";
    return `<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="${document.widthPx}" height="${document.heightPx}" viewBox="0 0 ${document.widthPx} ${document.heightPx}">
<title>${xmlEscape(document.name || "Picture")}</title><metadata data-publisherstudio-picture="1.4">${metadata}</metadata>${defs}${background}${layers.join("")}</svg>`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:createPictureStudioSvg@2447', __javascriptError); throw __javascriptError; }}


const svgImportRemovedElements = new Set(["script", "foreignobject", "iframe", "object", "embed", "audio", "video", "canvas"]);
const svgImportNonVisualAncestors = new Set(["defs", "clippath", "mask", "marker", "pattern", "symbol", "lineargradient", "radialgradient", "filter"]);
const svgImportVisualSelector = "path,rect,circle,ellipse,line,polyline,polygon,text,image,use";

function decodeSvgDataUrl(dataUrl) { try {
    const source = String(dataUrl || "");
    const comma = source.indexOf(",");
    if (comma < 0 || !source.slice(0, comma).toLowerCase().includes("image/svg+xml"))
        throw new Error("The selected file is not SVG data.");
    const header = source.slice(0, comma).toLowerCase();
    const payload = source.slice(comma + 1);
    if (header.includes(";base64")) {
        const binary = atob(payload);
        const bytes = new Uint8Array(binary.length);
        for (let index = 0; index < binary.length; index++) bytes[index] = binary.charCodeAt(index);
        return new TextDecoder("utf-8", { fatal: false }).decode(bytes);
    }
    return decodeURIComponent(payload);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:decodeSvgDataUrl@2488', __javascriptError); throw __javascriptError; }}

function safeSvgReference(value) { try {
    const text = String(value || "").trim();
    if (!text || text.startsWith("#") || /^data:image\/(?:png|jpe?g|gif|webp|bmp)(?:;|,)/i.test(text)) return text;
    return "";
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:safeSvgReference@2504', __javascriptError); throw __javascriptError; }}

function hasUnsafeSvgCssReference(value) { try {
    const text = String(value || "");
    if (/@import/i.test(text)) return true;
    const expression = /url\s*\(\s*(["']?)(.*?)\1\s*\)/gi;
    let match;
    while ((match = expression.exec(text))) {
        const target = String(match[2] || "").trim();
        if (!target.startsWith("#") && !safeSvgReference(target)) return true;
    }
    return false;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:hasUnsafeSvgCssReference@2510', __javascriptError); throw __javascriptError; }}

function sanitizeSvgImportDocument(sourceText) { try {
    const parsed = new DOMParser().parseFromString(sourceText, "image/svg+xml");
    if (parsed.querySelector("parsererror")) throw new Error("The SVG XML is malformed.");
    const root = parsed.documentElement;
    if (!root || root.localName.toLowerCase() !== "svg") throw new Error("The selected file has no SVG root element.");
    for (const element of [root, ...root.querySelectorAll("*")]) {
        const name = element.localName.toLowerCase();
        if (svgImportRemovedElements.has(name)) {
            element.remove();
            continue;
        }
        for (const attribute of [...element.attributes]) {
            const local = attribute.localName.toLowerCase();
            const value = String(attribute.value || "").trim();
            if (local.startsWith("on") || /(?:java|vb)script:/i.test(value) || hasUnsafeSvgCssReference(value)) {
                element.removeAttributeNode(attribute);
                continue;
            }
            if (local === "href" || local === "src") {
                const safe = safeSvgReference(value);
                if (safe) attribute.value = safe;
                else element.removeAttributeNode(attribute);
            }
        }
        if (name === "style") {
            const css = element.textContent || "";
            if (/javascript:/i.test(css) || hasUnsafeSvgCssReference(css)) element.remove();
        }
    }
    root.setAttribute("xmlns", "http://www.w3.org/2000/svg");
    root.removeAttribute("onload");
    return parsed;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:sanitizeSvgImportDocument@2522', __javascriptError); throw __javascriptError; }}

function svgNumberValue(value, fallback = 0) { try {
    const text = String(value ?? "").trim();
    const match = text.match(/^([+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:e[+-]?\d+)?)\s*([a-z%]*)$/i);
    if (!match) return fallback;
    const parsed = Number.parseFloat(match[1]);
    if (!Number.isFinite(parsed)) return fallback;
    const unit = String(match[2] || "").toLowerCase();
    if (unit === "mm") return parsed * 96 / 25.4;
    if (unit === "cm") return parsed * 96 / 2.54;
    if (unit === "in") return parsed * 96;
    if (unit === "pt") return parsed * 96 / 72;
    if (unit === "pc") return parsed * 16;
    if (unit === "q") return parsed * 96 / 101.6;
    if (unit === "%") return fallback;
    return parsed;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:svgNumberValue@2556', __javascriptError); throw __javascriptError; }}

function svgImportViewport(root) { try {
    const viewBox = String(root.getAttribute("viewBox") || "").trim().split(/[\s,]+/).map(Number).filter(Number.isFinite);
    let width = svgNumberValue(root.getAttribute("width"), viewBox.length === 4 ? viewBox[2] : 1200);
    let height = svgNumberValue(root.getAttribute("height"), viewBox.length === 4 ? viewBox[3] : 800);
    width = Math.round(clamp(width, 16, 8192));
    height = Math.round(clamp(height, 16, 8192));
    return { width, height, viewBox: viewBox.length === 4 ? viewBox : [0, 0, width, height] };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:svgImportViewport@2573', __javascriptError); throw __javascriptError; }}

function svgImportLayerPath(element) { try {
    const names = [];
    const inkscapeNamespace = "http://www.inkscape.org/namespaces/inkscape";
    let current = element.parentElement;
    while (current && current.localName.toLowerCase() !== "svg") {
        if (current.localName.toLowerCase() === "g") {
            const groupMode = current.getAttributeNS(inkscapeNamespace, "groupmode") || current.getAttribute("inkscape:groupmode");
            const label = current.getAttributeNS(inkscapeNamespace, "label") || current.getAttribute("inkscape:label") || current.getAttribute("data-name") || current.getAttribute("aria-label");
            if (String(groupMode || "").toLowerCase() === "layer" || label || current.id)
                names.unshift(label || current.id || "Layer");
        }
        current = current.parentElement;
    }
    return names.join(" / ");
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:svgImportLayerPath@2582', __javascriptError); throw __javascriptError; }}

function svgImportLayerName(element, index) { try {
    const inkscapeNamespace = "http://www.inkscape.org/namespaces/inkscape";
    const label = element.getAttributeNS(inkscapeNamespace, "label") || element.getAttribute("inkscape:label") || element.getAttribute("data-name") || element.getAttribute("aria-label");
    const title = element.querySelector(":scope > title")?.textContent?.trim();
    return label || title || element.id || `${element.localName} ${index + 1}`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:svgImportLayerName@2598', __javascriptError); throw __javascriptError; }}

function svgImportElementVisible(element, root) { try {
    let current = element;
    while (current && current !== root.parentElement) {
        const style = getComputedStyle(current);
        if (style.display === "none" || style.visibility === "hidden" || style.visibility === "collapse") return false;
        if (current === root) break;
        current = current.parentElement;
    }
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:svgImportElementVisible@2605', __javascriptError); throw __javascriptError; }}

function revealSvgElementForMeasurement(element, root) { try {
    const changed = [];
    let current = element;
    while (current && current !== root.parentElement) {
        const style = getComputedStyle(current);
        if (style.display === "none" || style.visibility === "hidden" || style.visibility === "collapse") {
            changed.push([current, current.getAttribute("style")]);
            current.style.setProperty("display", "inline", "important");
            current.style.setProperty("visibility", "visible", "important");
        }
        if (current === root) break;
        current = current.parentElement;
    }
    return () => { try {
        for (const [node, style] of changed.reverse()) {
            if (style == null) node.removeAttribute("style");
            else node.setAttribute("style", style);
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:ArrowFunction@2629', __javascriptError); throw __javascriptError; }};
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:revealSvgElementForMeasurement@2616', __javascriptError); throw __javascriptError; }}

function transformedSvgBounds(element, root) { try {
    const restore = revealSvgElementForMeasurement(element, root);
    try {
        let box;
        try { box = element.getBBox({ fill: true, stroke: true, markers: true, clipped: false }); }
        catch { box = element.getBBox(); }
        const matrix = element.getCTM();
        if (!box || !matrix || !Number.isFinite(box.width) || !Number.isFinite(box.height)) return null;
        const points = [
            new DOMPoint(box.x, box.y).matrixTransform(matrix),
            new DOMPoint(box.x + box.width, box.y).matrixTransform(matrix),
            new DOMPoint(box.x, box.y + box.height).matrixTransform(matrix),
            new DOMPoint(box.x + box.width, box.y + box.height).matrixTransform(matrix)
        ];
        const minX = Math.min(...points.map(point => { try { return (point.x); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:points.map@2651', __javascriptError); throw __javascriptError; } }));
        const minY = Math.min(...points.map(point => { try { return (point.y); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:points.map@2652', __javascriptError); throw __javascriptError; } }));
        const maxX = Math.max(...points.map(point => { try { return (point.x); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:points.map@2653', __javascriptError); throw __javascriptError; } }));
        const maxY = Math.max(...points.map(point => { try { return (point.y); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:points.map@2654', __javascriptError); throw __javascriptError; } }));
        const width = Math.max(.01, maxX - minX);
        const height = Math.max(.01, maxY - minY);
        const padding = Math.max(1, Math.min(24, Math.max(width, height) * .0125));
        return { x: minX - padding, y: minY - padding, width: width + padding * 2, height: height + padding * 2 };
    } finally {
        restore();
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:transformedSvgBounds@2637', __javascriptError); throw __javascriptError; }}

function forceSvgLayerVisibility(element) { try {
    element.removeAttribute("display");
    element.removeAttribute("visibility");
    element.style?.setProperty("display", "inline", "important");
    element.style?.setProperty("visibility", "visible", "important");
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:forceSvgLayerVisibility@2664', __javascriptError); throw __javascriptError; }}

function cloneSvgElementWithAncestors(element, root) { try {
    let content = element.cloneNode(true);
    forceSvgLayerVisibility(content);
    let current = element.parentElement;
    while (current && current !== root) {
        const wrapper = current.cloneNode(false);
        wrapper.removeAttribute("id");
        forceSvgLayerVisibility(wrapper);
        wrapper.appendChild(content);
        content = wrapper;
        current = current.parentElement;
    }
    return content;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:cloneSvgElementWithAncestors@2671', __javascriptError); throw __javascriptError; }}

function standaloneSvgForElement(root, element, bounds) { try {
    const document = root.ownerDocument;
    const output = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    output.setAttribute("xmlns", "http://www.w3.org/2000/svg");
    output.setAttribute("xmlns:xlink", "http://www.w3.org/1999/xlink");
    output.setAttribute("width", String(bounds.width));
    output.setAttribute("height", String(bounds.height));
    output.setAttribute("viewBox", `${bounds.x} ${bounds.y} ${bounds.width} ${bounds.height}`);
    output.setAttribute("preserveAspectRatio", "xMidYMid meet");
    for (const child of [...root.children]) {
        const name = child.localName.toLowerCase();
        if (name === "defs" || name === "style") output.appendChild(child.cloneNode(true));
    }
    for (const style of [...root.querySelectorAll("defs style")]) {
        if (!output.querySelector("defs")) output.appendChild(document.createElementNS("http://www.w3.org/2000/svg", "defs"));
        output.querySelector("defs").appendChild(style.cloneNode(true));
    }
    const content = cloneSvgElementWithAncestors(element, root);
    const rootMatrix = root.getCTM?.();
    if (rootMatrix && [rootMatrix.a, rootMatrix.b, rootMatrix.c, rootMatrix.d, rootMatrix.e, rootMatrix.f].every(Number.isFinite)) {
        const wrapper = document.createElementNS("http://www.w3.org/2000/svg", "g");
        wrapper.setAttribute("transform", `matrix(${rootMatrix.a} ${rootMatrix.b} ${rootMatrix.c} ${rootMatrix.d} ${rootMatrix.e} ${rootMatrix.f})`);
        wrapper.appendChild(content);
        output.appendChild(wrapper);
    } else {
        output.appendChild(content);
    }
    return new XMLSerializer().serializeToString(output);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:standaloneSvgForElement@2686', __javascriptError); throw __javascriptError; }}

function publisherStudioPictureMetadata(root) { try {
    const metadata = root.querySelector("metadata[data-publisherstudio-picture]");
    if (!metadata?.textContent?.trim()) return null;
    try {
        const documentModel = JSON.parse(metadata.textContent);
        return normalizeDocument(documentModel);
    } catch {
        return null;
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:publisherStudioPictureMetadata@2716', __javascriptError); throw __javascriptError; }}

export async function importPictureStudioSvg(dataUrl, fileName) { try {
    const sourceText = decodeSvgDataUrl(dataUrl);
    const parsed = sanitizeSvgImportDocument(sourceText);
    const root = parsed.documentElement;
    const embedded = publisherStudioPictureMetadata(root);
    if (embedded) {
        embedded.name = String(fileName || embedded.name || "SVG").replace(/\.(?:svg|svgz)$/i, "");
        return { document: embedded, issues: [] };
    }

    const viewport = svgImportViewport(root);
    root.setAttribute("width", `${viewport.width}px`);
    root.setAttribute("height", `${viewport.height}px`);
    if (!root.hasAttribute("viewBox")) root.setAttribute("viewBox", viewport.viewBox.join(" "));
    root.style.position = "fixed";
    root.style.left = "-100000px";
    root.style.top = "-100000px";
    // Keep authored visibility semantics available to getComputedStyle while making the
    // measurement tree fully transparent and far outside the viewport.
    root.style.opacity = "0";
    root.style.pointerEvents = "none";
    document.body.appendChild(document.importNode(root, true));
    const mounted = document.body.lastElementChild;
    const issues = [];
    try {
        await document.fonts?.ready;
        const visualElements = [...mounted.querySelectorAll(svgImportVisualSelector)].filter(element =>
            { try { return (!element.closest("defs,clipPath,mask,marker,pattern,symbol,linearGradient,radialGradient,filter")); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:[...mounted.querySelectorAll(svgImportVisualSelector)].filter@2753', __javascriptError); throw __javascriptError; } });
        const layers = [];
        for (let index = 0; index < visualElements.length && layers.length < 5000; index++) {
            const element = visualElements[index];
            if (element.localName.toLowerCase() === "image" && !safeSvgReference(element.getAttribute("href") || element.getAttributeNS("http://www.w3.org/1999/xlink", "href"))) {
                issues.push({ severity: 2, code: "SVG_EXTERNAL_IMAGE_SKIPPED", message: "An externally linked SVG image was skipped because PublisherStudio imports remain offline.", source: element.id || null });
                continue;
            }
            const visible = svgImportElementVisible(element, mounted);
            const bounds = transformedSvgBounds(element, mounted);
            if (!bounds || bounds.width <= .01 || bounds.height <= .01) continue;
            const markup = standaloneSvgForElement(mounted, element, bounds);
            layers.push({
                $type: "svg",
                id: crypto.randomUUID(),
                name: svgImportLayerName(element, index),
                groupPath: svgImportLayerPath(element),
                x: bounds.x,
                y: bounds.y,
                width: bounds.width,
                height: bounds.height,
                rotation: 0,
                // Authored opacity and ancestor opacity remain inside the retained SVG markup.
                // Applying them again at the Picture Studio layer would square the opacity.
                opacity: 1,
                visible,
                locked: false,
                blendMode: "Normal",
                brightness: 1,
                contrast: 1,
                saturation: 1,
                hueRotation: 0,
                blur: 0,
                grayscale: 0,
                sepia: 0,
                invert: 0,
                svgMarkup: markup,
                sourceFormat: "SVG",
                sourceElementId: element.id || "",
                preserveAspectRatio: true
            });
        }
        if (visualElements.length > 5000)
            issues.push({ severity: 1, code: "SVG_LAYER_LIMIT", message: "The SVG contains more than 5000 visual objects; remaining objects were not imported as separate layers.", source: null });
        if (!layers.length) throw new Error("The SVG contains no supported visible vector objects.");
        return {
            document: {
                id: crypto.randomUUID(),
                name: String(fileName || "SVG").replace(/\.(?:svg|svgz)$/i, ""),
                formatVersion: "1.4",
                widthPx: viewport.width,
                heightPx: viewport.height,
                background: "transparent",
                zoom: .65,
                gridVisible: false,
                snapToGrid: true,
                gridSpacingPx: 25,
                layers
            },
            issues
        };
    } finally {
        mounted?.remove();
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:importPictureStudioSvg@2727', __javascriptError); throw __javascriptError; }}

export async function downloadPictureStudioSvg(documentModel, fileName) { try {
    const svg = await createPictureStudioSvg(documentModel);
    const blob = new Blob([svg], { type: "image/svg+xml;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    try {
        const anchor = globalThis.document.createElement("a");
        anchor.href = url; anchor.download = fileName || "picture.svg";
        globalThis.document.body.appendChild(anchor); anchor.click(); anchor.remove();
    } finally { setTimeout(() => { try { return (URL.revokeObjectURL(url)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:setTimeout@2828', __javascriptError); throw __javascriptError; } }, 2000); }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:downloadPictureStudioSvg@2820', __javascriptError); throw __javascriptError; }}

export async function downloadPictureStudio(documentModel, fileName, mimeType = "image/png", quality = 1) { try {
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
        setTimeout(() => { try { return (URL.revokeObjectURL(url)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:callback:setTimeout@2842', __javascriptError); throw __javascriptError; } }, 2000);
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/pictureStudioInterop.js:downloadPictureStudio@2831', __javascriptError); throw __javascriptError; }}

