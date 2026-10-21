// Optional self-contained browser-code regression test. Invoke with:
// node tests/js/picture-studio-gif-frames.test.mjs
// Not a build-time dependency: PublisherStudio maintenance remains PowerShell-only.
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
globalThis.publisherStudioJavaScriptDiagnostics = { report() {} }; // Expected negative/fallback fixtures.
const moduleSource = readFileSync(resolve(here, '../../src/PublisherStudio.Web/wwwroot/js/animatedGifInterop.js'), 'utf8');
const { decodeGifRgbaFrames, decodeGifFrames } = await import('data:text/javascript;base64,' + Buffer.from(moduleSource).toString('base64'));
const readFixture = name => new Uint8Array(readFileSync(resolve(here, 'fixtures', name)));

const four = [];
const small = readFixture('gif-four-transparent-frames.gif');
const fourResult = await decodeGifRgbaFrames(small, async frame => four.push(frame));
assert.equal(fourResult.count, 4);
assert.equal(four.length, 4);
assert.deepEqual(four.map(frame => frame.delayMs), [40, 100, 180, 250]);
assert.deepEqual(four.map(frame => frame.count), [4, 4, 4, 4]);
const colors = [[255, 0, 0, 255], [0, 128, 0, 255], [0, 0, 255, 255], [255, 255, 0, 255]];
for (let i = 0; i < 4; i++) {
    const offset = ((3 * fourResult.width) + i + 1) * 4;
    assert.deepEqual(Array.from(four[i].rgba.subarray(offset, offset + 4)), colors[i]);
    assert.deepEqual(Array.from(four[i].rgba.subarray(0, 4)), [0, 0, 0, 0]);
}

const many = [];
const large = readFixture('gif-seventeen-frames.gif');
const largeResult = await decodeGifRgbaFrames(large, async frame => many.push(frame));
assert.equal(largeResult.count, 17);
assert.equal(many.length, 17);
for (const frame of many) {
    assert.equal(frame.count, 17);
    assert.equal(frame.rgba.length, 120 * 77 * 4);
}
await assert.rejects(async () => decodeGifRgbaFrames(large.subarray(0, large.length - 5), async () => {}), /truncated|complete/i);
// A browser that incorrectly exposes only one WebCodecs frame must not turn a
// four-frame GIF into a one-layer still. Exercise the actual transfer API path.
const savedDecoder = globalThis.ImageDecoder;
const savedDocument = globalThis.document;
class SingleFrameDecoder {
    constructor() { this.tracks = { ready: Promise.resolve(), selectedTrack: { frameCount: 1 } }; }
    close() {}
}
globalThis.ImageDecoder = SingleFrameDecoder;
globalThis.document = {
    createElement() {
        return {
            width: 0, height: 0,
            getContext() { return {
                createImageData(width, height) { return { data: new Uint8ClampedArray(width * height * 4) }; },
                putImageData() {}
            }; },
            toDataURL() { return 'data:image/png;base64,aGVsbG8='; }
        };
    }
};
try {
    const sent = [];
    await decodeGifFrames('data:image/gif;base64,' + Buffer.from(small).toString('base64'),
        async frame => sent.push(frame));
    assert.equal(sent.length, 4, 'WebCodecs frame-count mismatch must fall back to four editable raster frames');
    assert.deepEqual(sent.map(frame => frame.index), [0, 1, 2, 3]);
} finally {
    if (savedDecoder === undefined) delete globalThis.ImageDecoder;
    else globalThis.ImageDecoder = savedDecoder;
    if (savedDocument === undefined) delete globalThis.document;
    else globalThis.document = savedDocument;
}

console.log('Picture Studio GIF decoder: 4 timed transparent frames, 17-frame animation, WebCodecs one-frame fallback, truncated input rejection.');
