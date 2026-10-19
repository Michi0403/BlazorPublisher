// javascript-diagnostics: guarded
var publisherStudioDiagnostics = globalThis.publisherStudioJavaScriptDiagnostics || {
    report(context, error) {
        try { console.error(`PublisherStudio JavaScript error in ${String(context || "browser-runtime")}.`, error); }
        catch (reportError) { console.error("PublisherStudio fallback JavaScript diagnostics failed.", reportError); }
    }
};
// Shared animation interchange for Picture Studio and Video Studio. No codec download or
// external service is involved: the browser decodes GIF frames and this module writes GIF89a.
export async function decodeGifFrames(dataUrl, onFrame) { try {
    if (typeof ImageDecoder === 'undefined')
        throw new Error('Animated GIF frame editing needs browser ImageDecoder support (use a current Chromium-based browser).');
    const response = await fetch(dataUrl);
    if (!response.ok) throw new Error('The animated GIF image could not be read.');
    const bytes = new Uint8Array(await response.arrayBuffer());
    if (bytes.length < 13 || String.fromCharCode(...bytes.slice(0, 6)) !== 'GIF89a' &&
        String.fromCharCode(...bytes.slice(0, 6)) !== 'GIF87a')
        throw new Error('The selected file is not a GIF image.');
    const decoder = new ImageDecoder({ data: bytes, type: 'image/gif', preferAnimation: true });
    try {
        await decoder.tracks.ready;
        const track = decoder.tracks.selectedTrack;
        const count = track?.frameCount || 0;
        if (count < 1 || count > 500) throw new Error('GIF must contain between 1 and 500 frames.');
        let width = 0, height = 0;
        for (let index = 0; index < count; index++) {
            const decoded = await decoder.decode({ frameIndex: index, completeFramesOnly: true });
            const frame = decoded.image;
            try {
                width = frame.displayWidth;
                height = frame.displayHeight;
                if (!width || !height || width > 8192 || height > 8192)
                    throw new Error('GIF frame dimensions are outside the supported picture canvas.');
                const canvas = document.createElement('canvas');
                canvas.width = width;
                canvas.height = height;
                canvas.getContext('2d').drawImage(frame, 0, 0, width, height);
                await onFrame({ index, count, width, height,
                    delayMs: Math.max(20, Math.round((Number(frame.duration) || 100000) / 1000)),
                    dataUrl: canvas.toDataURL('image/png') });
            } finally { frame.close(); }
        }
        return { width, height, count };
    } finally { decoder.close(); }
} catch (error) {
        publisherStudioDiagnostics.report('js/animatedGifInterop.js:decodeGifFrames', error);
        throw error;
    }
}

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
