import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const sourcePath = new URL('../src/PublisherStudio.Web/wwwroot/js/timelineInterop.js', import.meta.url);
const source = await readFile(sourcePath, 'utf8');
const moduleUrl = `data:text/javascript;base64,${Buffer.from(source).toString('base64')}`;

let now = 0;
let nextFrameId = 1;
const frames = new Map();
const elements = new Map();
const windowListeners = new Map();

globalThis.performance = { now: () => now };
globalThis.requestAnimationFrame = callback => {
    const id = nextFrameId++;
    frames.set(id, callback);
    return id;
};
globalThis.cancelAnimationFrame = id => frames.delete(id);
globalThis.document = {
    getElementById: id => elements.get(id) || null,
    querySelector: () => null
};
globalThis.window = {
    addEventListener(type, callback) {
        const list = windowListeners.get(type) || [];
        list.push(callback);
        windowListeners.set(type, list);
    },
    removeEventListener(type, callback) {
        const list = windowListeners.get(type) || [];
        windowListeners.set(type, list.filter(item => item !== callback));
    }
};

const timeline = await import(moduleUrl);
const emptyPage = { querySelectorAll: () => [] };
elements.set('publisher-page', emptyPage);

function takeOnlyFrame() {
    assert.equal(frames.size, 1, 'exactly one playback frame should be scheduled');
    const [id, callback] = frames.entries().next().value;
    frames.delete(id);
    return callback;
}

async function settle() {
    await Promise.resolve();
    await new Promise(resolve => setImmediate(resolve));
}

// Restarting playback invalidates a callback which was already dequeued by the browser.
const notifications = [];
const dotnet = {
    invokeMethodAsync(method, runId, seconds, finished) {
        notifications.push({ method, runId, seconds, finished });
        return Promise.resolve();
    }
};

now = 0;
timeline.playPublicationTimeline('publisher-page', 0, 0.25, dotnet, 1);
const staleFrame = takeOnlyFrame();
timeline.playPublicationTimeline('publisher-page', 0, 0.25, dotnet, 2);
assert.equal(frames.size, 1, 'restart must replace rather than add a playback loop');

staleFrame(100);
assert.equal(frames.size, 1, 'a stale callback must not schedule another frame');
assert.equal(notifications.length, 0, 'a stale callback must not report playhead progress');

let activeFrame = takeOnlyFrame();
activeFrame(100);
await settle();
assert.equal(frames.size, 1);
activeFrame = takeOnlyFrame();
activeFrame(300);
await settle();
assert.ok(notifications.length >= 2, 'active playback should report progress and completion');
assert.ok(notifications.every(item => item.runId === 2), 'only the newest run may report progress');
assert.equal(notifications.at(-1).finished, true);
assert.equal(frames.size, 0, 'completed playback must not leave an animation-frame loop behind');

// Notifications are coalesced while Blazor is still processing the previous callback.
frames.clear();
notifications.length = 0;
let releaseNotification;
let invokeCount = 0;
const blockedDotnet = {
    invokeMethodAsync(method, runId, seconds, finished) {
        notifications.push({ method, runId, seconds, finished });
        invokeCount++;
        if (invokeCount === 1) return new Promise(resolve => { releaseNotification = resolve; });
        return Promise.resolve();
    }
};

now = 0;
timeline.playPublicationTimeline('publisher-page', 0, 1, blockedDotnet, 3);
for (const timestamp of [100, 200, 300]) {
    const frame = takeOnlyFrame();
    frame(timestamp);
}
assert.equal(invokeCount, 1, 'only one JS-to-.NET playhead callback may be in flight');
releaseNotification();
await settle();
assert.equal(invokeCount, 2, 'queued progress should collapse to the newest playhead position');
assert.ok(notifications[1].seconds >= 0.29 && notifications[1].seconds <= 0.31);
timeline.stopPublicationTimeline('publisher-page', false);
frames.clear();

// Pointer capture outside the visible track is bounded and cannot create an infinite trim.
const rootListeners = new Map();
const clipListeners = new Map();
const commits = [];
const track = {
    dataset: { viewStart: '0', viewEnd: '10', pageDuration: '10' },
    getBoundingClientRect: () => ({ left: 0, right: 100, width: 100 })
};
const clip = {
    dataset: { timelineClip: 'media', clipId: 'clip-1', clipStart: '2', clipDuration: '2' },
    style: {},
    classList: { add() {}, remove() {} },
    closest(selector) {
        if (selector === '[data-timeline-clip]') return clip;
        if (selector === '[data-timeline-track]') return track;
        return null;
    },
    setPointerCapture() {},
    hasPointerCapture() { return false; },
    releasePointerCapture() {},
    addEventListener(type, callback) { clipListeners.set(type, callback); },
    removeEventListener(type, callback) { if (clipListeners.get(type) === callback) clipListeners.delete(type); }
};
const handle = {
    dataset: { timelineHandle: 'trim-right' },
    closest(selector) {
        if (selector === '[data-timeline-clip]') return clip;
        if (selector === '[data-timeline-handle]') return handle;
        return null;
    }
};
const root = {
    contains: item => item === clip,
    addEventListener(type, callback) { rootListeners.set(type, callback); },
    removeEventListener(type, callback) { if (rootListeners.get(type) === callback) rootListeners.delete(type); }
};
elements.set('publisher-timeline', root);
timeline.initializePublicationTimeline('publisher-timeline', {
    invokeMethodAsync(method, id, mode, start, duration) {
        commits.push({ method, id, mode, start, duration });
        return Promise.resolve();
    }
});
rootListeners.get('pointerdown')({
    button: 0,
    pointerId: 7,
    clientX: 50,
    target: handle,
    preventDefault() {},
    stopPropagation() {}
});
clipListeners.get('pointermove')({ pointerId: 7, clientX: 1_000_000_000 });
clipListeners.get('pointerup')({ pointerId: 7 });
assert.equal(commits.length, 1);
assert.equal(commits[0].method, 'CommitMediaTimelineClip');
assert.equal(commits[0].mode, 'trim-right');
assert.ok(Number.isFinite(commits[0].duration));
assert.equal(commits[0].duration, 7, 'pointer movement is limited to the visible track edge');
assert.ok(!clip.style.width.includes('Infinity') && !clip.style.width.includes('NaN'));

timeline.disposePublicationTimeline('publisher-timeline');
console.log('timelineInterop lifecycle, backpressure, and bounded trim tests passed');
