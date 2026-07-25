import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = (...parts) => fs.readFileSync(path.join(root, ...parts), 'utf8');

const studio = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'MediaStudio.razor');
const models = read('src', 'PublisherStudio.Web', 'Domain', 'PublicationMediaModels.cs');
const timeline = read('src', 'PublisherStudio.Web', 'Services', 'MediaStudio', 'UseCases', 'MediaTimelineEditService.cs');
const interop = read('src', 'PublisherStudio.Web', 'wwwroot', 'js', 'mediaStudioInterop.js');

// Pointer release commits browser duration and the selected range in one ordered JS -> .NET call.
assert.match(interop, /function reconcileResolvedVideoDuration/);
assert.match(interop, /reportResolvedVideoDuration\(state, video, overlay\)/);
assert.match(interop, /VideoTimeSelectionCommitted', start, end, pointSelection, sourceDuration/);
assert.match(studio, /VideoTimeSelectionCommitted\(double startSeconds, double endSeconds, bool pointSelection, double sourceDurationSeconds\)/);
assert.match(studio, /ReconcileVideoDuration\(sourceDurationSeconds, preserveCurrentSelection: true\)[\s\S]*SetVideoSelectionRange\(startSeconds, endSeconds, pointSelection\)/);
assert.match(studio, /_videoSelectionUserCommitted = true;[\s\S]*CommitSelectedSegment\(\)/);
assert.doesNotMatch(studio, /VideoTimeSelectionCommitted[\s\S]{0,700}InvokeVoidAsync\("seekMedia"/);

// Explicit selection ownership survives metadata repair and project serialization.
assert.match(models, /bool TemporalSelectionCommitted/);
assert.match(timeline, /TemporalSelectionCommitted = segment\.TemporalSelectionCommitted/);
assert.match(timeline, /segment\.TemporalSelectionCommitted = segment\.TemporalSelectionIsPoint/);
assert.match(studio, /segment\.TemporalSelectionCommitted = IsVideo && _videoSelectionUserCommitted/);
assert.match(studio, /preserveCurrentSelection \|\| _videoSelectionUserCommitted/);

// Saved selection and selected-layer timing share the same committed range.
assert.match(studio, /Text="Apply to layer"/);
assert.match(studio, /Text="Selection → layer"/);
assert.match(studio, /Apply selected range to layer/);
assert.match(studio, /private void ApplySelectionToSelectedVideoLayer\(\)/);
assert.match(studio, /layer\.TemporalStartSeconds = VideoSelectionStart/);
assert.match(studio, /layer\.TemporalEndSeconds = VideoSelectionEnd/);
assert.match(studio, /CommitSelectedSegment\(refreshVideoEffects: true\)/);
assert.doesNotMatch(studio, /layer\.Opacity,\s*layer\.Opacity,/);

// Playback commands cancel older promises and swallow expected browser AbortError races.
assert.match(interop, /function isInterruptedPlaybackError/);
assert.match(interop, /function cancelRangePlayback/);
assert.match(interop, /playCommandVersion/);
assert.match(interop, /isInterruptedPlaybackError\(error\)/);
assert.match(interop, /return false;[\s\S]*export function pauseMedia/);
assert.match(interop, /export function pauseMedia[\s\S]*cancelRangePlayback/);
assert.match(interop, /export function seekMedia[\s\S]*cancelRangePlayback/);
assert.match(studio, /catch \(JSException ex\) when \(IsInterruptedPlaybackException\(ex\)\)/);
assert.match(studio, /data-video-command="play"/);
assert.match(interop, /lostpointercapture/);

// Layer/filter mutations explicitly refresh the live canvas, including chroma key.
assert.match(studio, /private void QueueVideoEffectsRefresh\(\)/);
assert.match(studio, /private void AddChromaKeyFilter\(\) => AddVideoFilter/);
assert.match(studio, /AddVideoFilter[\s\S]*CommitSelectedSegment\(refreshVideoEffects: true\)/);

console.log('PublisherStudio committed video selection, layer-range binding, chroma refresh, and interruption-safe playback contracts passed.');

// The actual JS bridge must absorb the browser's expected play/pause AbortError so
// Blazor Server's RemoteRenderer never receives a rejected play() promise.
class FakeMediaElement extends EventTarget {
    constructor() {
        super();
        this.currentTime = 0;
        this.duration = 12;
        this.volume = 1;
        this.playbackRate = 1;
        this.muted = false;
        this.pendingReject = null;
        this.mode = 'resolve';
    }
    play() {
        if (this.mode === 'resolve') return Promise.resolve();
        return new Promise((_resolve, reject) => { this.pendingReject = reject; });
    }
    pause() {
        if (!this.pendingReject) return;
        const reject = this.pendingReject;
        this.pendingReject = null;
        const error = new Error('The play() request was interrupted by a call to pause().');
        error.name = 'AbortError';
        queueMicrotask(() => reject(error));
    }
}

globalThis.HTMLMediaElement = FakeMediaElement;
globalThis.HTMLVideoElement = class extends FakeMediaElement {};
globalThis.HTMLCanvasElement = class {};
const fakeMedia = new FakeMediaElement();
globalThis.document = { getElementById: id => id === 'interaction-test-video' ? fakeMedia : null };
const interopModule = await import(`${new URL('../src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js', import.meta.url).href}?v=1077`);
assert.equal(await interopModule.playMediaRange('interaction-test-video', 1, 3, 1, 1, false, false), true);
fakeMedia.mode = 'pending';
const pendingPlay = interopModule.playMediaRange('interaction-test-video', 2, 4, 1, 1, false, false);
await new Promise(resolve => setTimeout(resolve, 0));
assert.equal(interopModule.pauseMedia('interaction-test-video'), true);
assert.equal(await pendingPlay, false);
