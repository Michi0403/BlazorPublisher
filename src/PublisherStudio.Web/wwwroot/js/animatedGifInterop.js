// javascript-diagnostics: guarded
var publisherStudioDiagnostics = globalThis.publisherStudioJavaScriptDiagnostics || {
    report(context, error) {
        try { console.error(`PublisherStudio JavaScript error in ${String(context || "browser-runtime")}.`, error); }
        catch (reportError) { console.error("PublisherStudio fallback JavaScript diagnostics failed.", reportError); }
    }
};
// Shared animation interchange for Picture Studio and Video Studio. No codec download or
// external service is involved: the browser decodes GIF frames and this module writes GIF89a.
// The browser's WebCodecs ImageDecoder is optional. Decode ordinary GIF87a/GIF89a
// frames locally when it is unavailable, preserving per-frame timing, transparency,
// disposal and interlacing. No network service, Python runtime or third-party codec.
function decodeGifLzw(payload, minimumCodeSize, expectedPixels) {
    if (minimumCodeSize < 2 || minimumCodeSize > 8)
        throw new Error('Unsupported GIF LZW code size.');
    const clearCode = 1 << minimumCodeSize;
    const endCode = clearCode + 1;
    let dictionary = [], codeSize = minimumCodeSize + 1, nextCode = endCode + 1;
    const reset = () => {
        dictionary = Array.from({length: clearCode}, (_, i) => [i]);
        codeSize = minimumCodeSize + 1;
        nextCode = endCode + 1;
    };
    reset();
    let bitPosition = 0;
    const result = new Uint8Array(expectedPixels);
    let cursor = 0, previous = null, ended = false;
    while (bitPosition + codeSize <= payload.length * 8) {
        let code = 0;
        for (let bit = 0; bit < codeSize; bit++) {
            const current = bitPosition + bit;
            code |= ((payload[current >> 3] >> (current & 7)) & 1) << bit;
        }
        bitPosition += codeSize;
        if (code === clearCode) { reset(); previous = null; continue; }
        if (code === endCode) { ended = true; break; }
        let entry;
        if (dictionary[code]) entry = dictionary[code];
        else if (code === nextCode && previous) entry = [...previous, previous[0]];
        else throw new Error('GIF frame contains invalid LZW codes.');
        if (cursor + entry.length > expectedPixels)
            throw new Error('GIF frame expands beyond its declared bounds.');
        result.set(entry, cursor);
        cursor += entry.length;
        if (previous && nextCode < 4096) {
            dictionary[nextCode++] = [...previous, entry[0]];
            if (nextCode === (1 << codeSize) && codeSize < 12) codeSize++;
        }
        previous = entry;
    }
    if (!ended || cursor !== expectedPixels)
        throw new Error('GIF frame pixel stream was truncated.');
    return result;
}

// Count frame blocks without allocating a decoded frame buffer. The import
// callback needs a verified total before its first bounded frame transfer.
function countGifImageBlocks(bytes) {
    let offset = 13;
    if (bytes.length < offset) throw new Error('The GIF header is truncated.');
    const globalFlags = bytes[10];
    if (globalFlags & 0x80) offset += 3 * (1 << ((globalFlags & 7) + 1));
    const requireBytes = count => { if (offset + count > bytes.length) throw new Error('GIF image data is truncated.'); };
    const blocks = () => {
        while (true) {
            requireBytes(1);
            const size = bytes[offset++];
            if (!size) break;
            requireBytes(size);
            offset += size;
        }
    };
    let count = 0, terminated = false;
    while (offset < bytes.length) {
        const marker = bytes[offset++];
        if (marker === 0x3b) { terminated = true; break; }
        if (marker === 0x21) { requireBytes(1); offset++; blocks(); continue; }
        if (marker !== 0x2c) throw new Error('Unknown GIF block marker.');
        requireBytes(9);
        const localFlags = bytes[offset + 8];
        offset += 9;
        if (localFlags & 0x80) offset += 3 * (1 << ((localFlags & 7) + 1));
        requireBytes(1);
        offset++; // frame LZW minimum code size
        blocks();
        if (++count > 500) throw new Error('GIF contains more than 500 frames.');
    }
    if (!terminated || !count) throw new Error('The GIF has no complete image sequence.');
    return count;
}

/** Decode each full composited GIF frame, including delay, into a separate RGBA image. */
export async function decodeGifRgbaFrames(bytes, onFrame) {
    if (!(bytes instanceof Uint8Array) || bytes.length < 14 ||
        !['GIF87a', 'GIF89a'].includes(String.fromCharCode(...bytes.subarray(0, 6))))
        throw new Error('The selected file is not a valid GIF image.');
    let offset = 6;
    const need = count => { if (offset + count > bytes.length) throw new Error('GIF data is truncated.'); };
    const byte = () => { need(1); return bytes[offset++]; };
    const word = () => { const lo = byte(); return lo | (byte() << 8); };
    const width = word(), height = word(), flags = byte(), backgroundIndex = byte();
    byte(); // pixel aspect ratio
    if (!width || !height || width > 8192 || height > 8192)
        throw new Error('GIF dimensions exceed the supported picture canvas.');
    const pixelsPerFrame = width * height;
    const frameCount = countGifImageBlocks(bytes);
    if (pixelsPerFrame * frameCount > 100_000_000)
        throw new Error('Animated GIF frame/pixel safety budget exceeded.');
    const palette = size => { need(size * 3); const value = bytes.subarray(offset, offset + size * 3); offset += size * 3; return value; };
    const globalPalette = (flags & 0x80) ? palette(1 << ((flags & 7) + 1)) : null;
    const skipSubBlocks = () => {
        while (true) { const size = byte(); if (!size) break; need(size); offset += size; }
    };
    const imageDataBlocks = () => {
        const parts = []; let length = 0;
        while (true) {
            const size = byte(); if (!size) break;
            need(size); parts.push(bytes.subarray(offset, offset + size)); offset += size;
            length += size;
            if (length > 128 * 1024 * 1024) throw new Error('GIF compressed frame is too large.');
        }
        const compressed = new Uint8Array(length);
        let cursor = 0;
        for (const part of parts) { compressed.set(part, cursor); cursor += part.length; }
        return compressed;
    };
    let canvas = new Uint8ClampedArray(pixelsPerFrame * 4);
    let previousDisposal = 0, previousRect = null, previousCanvas = null;
    let control = { delayMs: 100, disposal: 0, transparentIndex: -1 };
    let index = 0;
    while (offset < bytes.length) {
        const marker = byte();
        if (marker === 0x3b) break;
        if (marker === 0x21) {
            const extension = byte();
            if (extension === 0xf9) {
                if (byte() !== 4) throw new Error('Malformed GIF graphic-control block.');
                const packed = byte(), delay = word(), transparentIndex = byte();
                if (byte() !== 0) throw new Error('Malformed GIF graphic-control terminator.');
                control = { delayMs: Math.max(20, (delay || 10) * 10), disposal: (packed >> 2) & 7,
                    transparentIndex: (packed & 1) ? transparentIndex : -1 };
            } else skipSubBlocks();
            continue;
        }
        if (marker !== 0x2c) throw new Error('Unknown GIF image-block marker.');
        const left = word(), top = word(), frameWidth = word(), frameHeight = word(), imageFlags = byte();
        if (!frameWidth || !frameHeight || left + frameWidth > width || top + frameHeight > height)
            throw new Error('GIF frame is outside its canvas.');
        if (index >= 500 || (index + 1) * pixelsPerFrame > 100_000_000)
            throw new Error('Animated GIF frame/pixel safety budget exceeded.');
        const localPalette = (imageFlags & 0x80) ? palette(1 << ((imageFlags & 7) + 1)) : globalPalette;
        if (!localPalette) throw new Error('GIF frame has no color palette.');
        const lzwMinimum = byte(), compressed = imageDataBlocks();
        const indices = decodeGifLzw(compressed, lzwMinimum, frameWidth * frameHeight);
        if (previousDisposal === 2 && previousRect) {
            for (let y = previousRect.top; y < previousRect.top + previousRect.height; y++)
                canvas.fill(0, (y * width + previousRect.left) * 4,
                    (y * width + previousRect.left + previousRect.width) * 4);
        } else if (previousDisposal === 3 && previousCanvas) {
            canvas = previousCanvas;
        }
        const saved = control.disposal === 3 ? canvas.slice() : null;
        let cursor = 0;
        const rows = [];
        if (imageFlags & 0x40) {
            for (const [start, step] of [[0, 8], [4, 8], [2, 4], [1, 2]])
                for (let y = start; y < frameHeight; y += step) rows.push(y);
        } else for (let y = 0; y < frameHeight; y++) rows.push(y);
        for (const y of rows) for (let x = 0; x < frameWidth; x++) {
            const color = indices[cursor++];
            if (color === control.transparentIndex) continue;
            const paletteOffset = color * 3;
            if (paletteOffset + 2 >= localPalette.length)
                throw new Error('GIF frame references a color outside its palette.');
            const target = ((top + y) * width + left + x) * 4;
            canvas[target] = localPalette[paletteOffset];
            canvas[target + 1] = localPalette[paletteOffset + 1];
            canvas[target + 2] = localPalette[paletteOffset + 2];
            canvas[target + 3] = 255;
        }
        await onFrame({ index, count: frameCount, width, height, delayMs: control.delayMs, rgba: canvas.slice() });
        index++;
        previousDisposal = control.disposal;
        previousRect = { left, top, width: frameWidth, height: frameHeight };
        previousCanvas = saved;
        control = { delayMs: 100, disposal: 0, transparentIndex: -1 };
    }
    if (!index) throw new Error('The GIF contains no image frames.');
    return { width, height, count: index, backgroundIndex };
}

export async function decodeGifFrames(dataUrl, onFrame) { try {
    const response = await fetch(dataUrl);
    if (!response.ok) throw new Error('The animated GIF image could not be read.');
    const bytes = new Uint8Array(await response.arrayBuffer());
    if (bytes.length < 13 || !['GIF89a', 'GIF87a'].includes(String.fromCharCode(...bytes.subarray(0, 6))))
        throw new Error('The selected file is not a GIF image.');

    // Decode the whole frame sequence before transferring to Blazor. This is a
    // small bounded metadata scan, not a partial import: a corrupt tail frame
    // must not leave the user with an incomplete, apparently successful timeline.
    // The pure-JS fallback is also available when Chromium disables WebCodecs.
    if (typeof ImageDecoder !== 'undefined') {
        let delivered = 0;
        try {
            const decoder = new ImageDecoder({ data: bytes, type: 'image/gif', preferAnimation: true });
            try {
                await decoder.tracks.ready;
                const count = decoder.tracks.selectedTrack?.frameCount || 0;
                if (count < 1 || count > 500)
                    throw new Error('GIF must contain between 1 and 500 frames.');
                // Do not accept a single still from WebCodecs when the source
                // actually contains a full multi-frame animation.
                if (count !== countGifImageBlocks(bytes))
                    throw new Error('The browser reported an incomplete GIF frame track.');
                for (let index = 0; index < count; index++) {
                    const decoded = await decoder.decode({ frameIndex: index, completeFramesOnly: true });
                    const frame = decoded.image;
                    try {
                        const width = frame.displayWidth, height = frame.displayHeight;
                        if (!width || !height || width > 8192 || height > 8192 ||
                            width * height * count > 100_000_000)
                            throw new Error('GIF frame dimensions exceed the supported pixel budget.');
                        const canvas = document.createElement('canvas');
                        canvas.width = width;
                        canvas.height = height;
                        const ctx = canvas.getContext('2d');
                        if (!ctx) throw new Error('The browser cannot render decoded GIF frames.');
                        ctx.drawImage(frame, 0, 0, width, height);
                        delivered++;
                        await onFrame({ index, count, width, height,
                            delayMs: Math.max(20, Math.round((Number(frame.duration) || 100000) / 1000)),
                            dataUrl: canvas.toDataURL('image/png') });
                    } finally { frame.close(); }
                }
                return { count };
            } finally { decoder.close(); }
        } catch (error) {
            // Never restart a partially delivered transfer. The caller will
            // retain its original raster and surface this decoding failure.
            if (delivered) throw error;
            publisherStudioDiagnostics.report('js/animatedGifInterop.js:webcodecs-fallback', error);
        }
    }
    return await decodeGifRgbaFrames(bytes, async frame => {
        const canvas = document.createElement('canvas');
        canvas.width = frame.width;
        canvas.height = frame.height;
        const context = canvas.getContext('2d');
        if (!context) throw new Error('The browser cannot render the GIF frame.');
        const imageData = context.createImageData(frame.width, frame.height);
        imageData.data.set(frame.rgba);
        context.putImageData(imageData, 0, 0);
        await onFrame({ index: frame.index, count: frame.count, width: frame.width,
            height: frame.height, delayMs: frame.delayMs, dataUrl: canvas.toDataURL('image/png') });
    });
} catch (error) {
    publisherStudioDiagnostics.report('js/animatedGifInterop.js:decodeGifFrames', error);
    throw error;
}}

// 8-bit RGB332 global palette; palette slot 0 is transparent. Opaque near-black
// pixels map to slot 1 instead. This avoids large dependencies and allows GIFs
// to retain alpha (one-bit GIF transparency, as required by the format).
function gifPixelIndices(data) { try {
    const result = new Uint8Array(data.length / 4);
    for (let i = 0; i < result.length; i++) {
        const offset = i * 4;
        if (data[offset + 3] < 128) { result[i] = 0; continue; }
        const value = (data[offset] & 224) | ((data[offset + 1] & 224) >> 3) | (data[offset + 2] >> 6);
        result[i] = Math.max(1, value);
    }
    return result;
} catch (error) {
        publisherStudioDiagnostics.report('js/animatedGifInterop.js:gifPixelIndices', error);
        throw error;
    }
}

function gifLzw(indices) { try {
    const output = [];
    let bits = 0, bitCount = 0;
    const write = (value, width) => {
        bits |= value << bitCount;
        bitCount += width;
        while (bitCount >= 8) { output.push(bits & 255); bits >>>= 8; bitCount -= 8; }
    };
    let dictionary, nextCode, width;
    const reset = () => { dictionary = new Map(); nextCode = 258; width = 9; };
    reset();
    write(256, width);
    let prefix = indices[0];
    for (let i = 1; i < indices.length; i++) {
        const symbol = indices[i];
        const key = (prefix << 8) | symbol;
        const found = dictionary.get(key);
        if (found !== undefined) { prefix = found; continue; }
        write(prefix, width);
        if (nextCode < 4096) {
            dictionary.set(key, nextCode++);
            // The decoder adds the corresponding entry after reading this code.
            // Increase width only when its next read will require the new width.
            if (nextCode > (1 << width) && width < 12) width++;
        } else {
            write(256, width);
            reset();
        }
        prefix = symbol;
    }
    write(prefix, width);
    write(257, width);
    if (bitCount) output.push(bits & 255);
    return output;
} catch (error) {
        publisherStudioDiagnostics.report('js/animatedGifInterop.js:gifLzw', error);
        throw error;
    }
}

export function encodeGifFrames(frames, width, height, loops = 0) { try {
    width = Math.trunc(width); height = Math.trunc(height);
    if (!Array.isArray(frames) || !frames.length || frames.length > 500 ||
        width < 1 || height < 1 || width > 8192 || height > 8192)
        throw new Error('Invalid animated GIF frame count or dimensions.');
    const data = [];
    const bytes = (...items) => data.push(...items);
    const word = value => bytes(value & 255, (value >> 8) & 255);
    for (const char of 'GIF89a') bytes(char.charCodeAt(0));
    word(width); word(height); bytes(0xf7, 0, 0); // 256-entry global color table
    for (let index = 0; index < 256; index++)
        bytes(index & 224, (index & 28) << 3, (index & 3) << 6);
    bytes(0x21, 0xff, 11);
    for (const char of 'NETSCAPE2.0') bytes(char.charCodeAt(0));
    bytes(3, 1); word(Math.max(0, Math.min(65535, Math.trunc(loops)))); bytes(0);
    for (const frame of frames) {
        if (!frame?.rgba || frame.rgba.length !== width * height * 4)
            throw new Error('Animated GIF frame has invalid pixel data.');
        const delay = Math.max(2, Math.min(65535, Math.round((Number(frame.delayMs) || 100) / 10)));
        bytes(0x21, 0xf9, 4, 0x09); // restore background, transparent index 0
        word(delay); bytes(0, 0);
        bytes(0x2c); word(0); word(0); word(width); word(height); bytes(0);
        bytes(8); // LZW minimum code size
        const compressed = gifLzw(gifPixelIndices(frame.rgba));
        for (let pos = 0; pos < compressed.length; pos += 255) {
            const chunk = compressed.slice(pos, pos + 255);
            bytes(chunk.length, ...chunk);
        }
        bytes(0);
    }
    bytes(0x3b);
    return new Blob([Uint8Array.from(data)], { type: 'image/gif' });
} catch (error) {
        publisherStudioDiagnostics.report('js/animatedGifInterop.js:encodeGifFrames', error);
        throw error;
    }
}
