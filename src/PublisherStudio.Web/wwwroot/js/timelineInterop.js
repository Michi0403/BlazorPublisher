// javascript-diagnostics: guarded
var publisherStudioDiagnostics = globalThis.publisherStudioJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`PublisherStudio JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("PublisherStudio fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`PublisherStudio fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
const timelineStates = new Map();
const pagePlaybackStates = new Map();
const scrubAnimations = new Map();
const mediaClipHandlers = new WeakMap();

function number(value, fallback = 0) { try {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : fallback;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:number@8', __javascriptError); throw __javascriptError; }}
function clamp(value, min, max) { try { return Math.max(min, Math.min(max, value));  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:clamp@12', __javascriptError); throw __javascriptError; }}
function lower(value) { try { return String(value || '').replace(/[^a-z0-9]/gi, '').toLowerCase();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:lower@13', __javascriptError); throw __javascriptError; }}
function parse(value, fallback) { try { try { return JSON.parse(value || ''); } catch { return fallback; }  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:parse@14', __javascriptError); throw __javascriptError; }}
function easing(value) { try {
    switch (lower(value)) {
        case 'linear': return 'linear';
        case 'easein': return 'cubic-bezier(.42,0,1,1)';
        case 'easeout': return 'cubic-bezier(0,0,.2,1)';
        case 'backout': return 'cubic-bezier(.18,.89,.32,1.28)';
        case 'bounceout': return 'cubic-bezier(.22,1.3,.36,1)';
        default: return 'cubic-bezier(.4,0,.2,1)';
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:easing@15', __javascriptError); throw __javascriptError; }}
function vector(direction, distance) { try {
    const amount = number(distance, 18);
    switch (lower(direction)) {
        case 'right': return { x: amount, y: 0 };
        case 'up': return { x: 0, y: -amount };
        case 'down': return { x: 0, y: amount };
        default: return { x: -amount, y: 0 };
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:vector@25', __javascriptError); throw __javascriptError; }}
function baseTransform(node) { try {
    const inline = String(node?.style?.transform || '').trim();
    if (inline) return inline === 'none' ? '' : inline;
    const value = getComputedStyle(node).transform;
    return !value || value === 'none' ? '' : value;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:baseTransform@34', __javascriptError); throw __javascriptError; }}
function compose(base, extra) { try { return `${extra} ${base}`.trim();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:compose@40', __javascriptError); throw __javascriptError; }}
function frames(node, animation) { try {
    const effect = lower(animation.effect);
    const phase = lower(animation.phase);
    const base = baseTransform(node);
    const move = vector(animation.direction, animation.distancePercent);
    const scale = Math.max(.01, number(animation.scalePercent, 20) / 100);
    const rotation = number(animation.rotationDegrees, 360);
    const translated = compose(base, `translate(${move.x}%,${move.y}%)`);
    const reverse = items => { try { return (phase === 'exit' ? [...items].reverse() : items); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:reverse@49', __javascriptError); throw __javascriptError; } };
    switch (effect) {
        case 'fade': return reverse([{ opacity: 0 }, { opacity: 1 }]);
        case 'fly': return reverse([{ opacity: 0, transform: translated }, { opacity: 1, transform: base || 'none' }]);
        case 'float': return reverse([{ opacity: 0, filter: 'blur(6px)', transform: compose(base, `translate(${move.x / 2}%,${move.y / 2}%)`) }, { opacity: 1, filter: 'blur(0)', transform: base || 'none' }]);
        case 'zoom': return reverse([{ opacity: 0, transform: compose(base, `scale(${Math.max(.02, 1 - scale)})`) }, { opacity: 1, transform: base || 'none' }]);
        case 'wipe': {
            const direction = lower(animation.direction);
            const start = direction === 'right' ? 'inset(0 100% 0 0)' : direction === 'up' ? 'inset(100% 0 0 0)' : direction === 'down' ? 'inset(0 0 100% 0)' : 'inset(0 0 0 100%)';
            return reverse([{ opacity: 0, clipPath: start }, { opacity: 1, clipPath: 'inset(0 0 0 0)' }]);
        }
        case 'bounce': return [{ transform: base || 'none' }, { offset: .5, transform: compose(base, `translateY(${-Math.max(8, number(animation.distancePercent, 18))}%) scale(${1 + scale / 2})`) }, { transform: base || 'none' }];
        case 'pulse':
        case 'growshrink': return [{ transform: base || 'none' }, { transform: compose(base, `scale(${1 + scale})`), offset: .5 }, { transform: base || 'none' }];
        case 'spin': return [{ transform: base || 'none' }, { transform: compose(base, `rotate(${rotation}deg)`) }];
        case 'shake': return [0, -2, 2, -1.6, 1.6, -.8, .8, 0].map((factor, index, values) => { try { return (({ offset: index / (values.length - 1), transform: compose(base, `translateX(${Math.max(2, number(animation.distancePercent, 18) / 4) * factor}%)`) })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:callback:[0, -2, 2, -1.6, 1.6, -.8, .8, 0].map@64', __javascriptError); throw __javascriptError; } });
        case 'move': return [{ transform: base || 'none' }, { transform: translated }];
        default: return [{ opacity: 1 }, { opacity: 1 }];
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:frames@41', __javascriptError); throw __javascriptError; }}
function groupNodes(node) { try {
    const groupId = String(node?.dataset?.groupId || '').trim();
    const page = node?.closest?.('.publication-page,.print-page');
    if (!groupId || !page) return [node];
    const peers = [...page.querySelectorAll('[data-publication-element][data-group-id]')]
        .filter(candidate => { try { return (String(candidate.dataset.groupId || '') === groupId); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:callback:[...page.querySelectorAll(\'[data-publication-element][data-group-id]\')@74', __javascriptError); throw __javascriptError; } });
    return peers.length ? peers : [node];
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:groupNodes@69', __javascriptError); throw __javascriptError; }}
function composite(animations) { try {
    return {
        cancel() { try { animations.forEach(animation => { try { try { animation.cancel(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:suppressed-catch@79', __caughtJavaScriptError);  }  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:callback:animations.forEach@79', __javascriptError); throw __javascriptError; }});  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:cancel@79', __javascriptError); throw __javascriptError; }},
        pause() { try { animations.forEach(animation => { try { try { animation.pause(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:suppressed-catch@80', __caughtJavaScriptError);  }  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:callback:animations.forEach@80', __javascriptError); throw __javascriptError; }});  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:pause@80', __javascriptError); throw __javascriptError; }},
        play() { try { animations.forEach(animation => { try { try { animation.play(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:suppressed-catch@81', __caughtJavaScriptError);  }  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:callback:animations.forEach@81', __javascriptError); throw __javascriptError; }});  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:play@81', __javascriptError); throw __javascriptError; }},
        get currentTime() { try { return animations[0]?.currentTime || 0;  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:currentTime@82', __javascriptError); throw __javascriptError; }},
        set currentTime(value) { try { animations.forEach(animation => { try { return (animation.currentTime = value); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:callback:animations.forEach@83', __javascriptError); throw __javascriptError; } });  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:currentTime@83', __javascriptError); throw __javascriptError; }}
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:composite@77', __javascriptError); throw __javascriptError; }}
function animateGroup(node, animation, options) { try {
    const animations = groupNodes(node).map(member => { try { return (member.animate(frames(member, animation), options)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:callback:groupNodes(node).map@87', __javascriptError); throw __javascriptError; } });
    return animations.length === 1 ? animations[0] : composite(animations);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:animateGroup@86', __javascriptError); throw __javascriptError; }}
function animationSpan(animation) { try {
    return Math.max(.05, number(animation.durationSeconds, .6)) * Math.max(1, number(animation.repeatCount, 1)) * (animation.autoReverse ? 2 : 1);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:animationSpan@90', __javascriptError); throw __javascriptError; }}
function animationItems(page) { try {
    const items = [...page.querySelectorAll('[data-publication-element]')].flatMap(node =>
        { try { return (parse(node.dataset.animations, []).map(animation => { try { return (({ node, animation })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:callback:parse(node.dataset.animations, []).map@95', __javascriptError); throw __javascriptError; } })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:callback:[...page.querySelectorAll(\'[data-publication-element]\')].flatMap@94', __javascriptError); throw __javascriptError; } }
    ).sort((a, b) => { try { return (number(a.animation.order) - number(b.animation.order)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:callback:[...page.querySelectorAll(\'[data-publication-element]\')].flatMap(node @96', __javascriptError); throw __javascriptError; } });
    let previousStart = 0;
    let previousEnd = 0;
    for (const item of items) {
        const animation = item.animation;
        const explicit = animation.timelineStartSeconds;
        let start;
        if (explicit !== null && explicit !== undefined && Number.isFinite(Number(explicit))) {
            start = Math.max(0, Number(explicit));
        } else {
            const delay = Math.max(0, number(animation.delaySeconds));
            const trigger = lower(animation.trigger);
            start = trigger === 'withprevious' ? previousStart + delay : trigger === 'afterprevious' ? previousEnd + delay : delay;
        }
        item.start = start;
        item.span = animationSpan(animation);
        previousStart = start;
        previousEnd = start + item.span;
    }
    return items;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:animationItems@93', __javascriptError); throw __javascriptError; }}
function mediaItems(page) { try {
    return [...page.querySelectorAll('[data-media-kind]')].map(node => { try {
        const media = node.querySelector('video,audio');
        return {
            node,
            media,
            start: Math.max(0, number(node.dataset.mediaStart)),
            trimStart: Math.max(0, number(node.dataset.mediaTrimStart)),
            trimEnd: Math.max(0, number(node.dataset.mediaTrimEnd)),
            rate: clamp(number(node.dataset.mediaRate, 1), .25, 4),
            volume: clamp(number(node.dataset.mediaVolume, 1), 0, 1),
            muted: node.dataset.mediaMuted === 'true',
            loop: node.dataset.mediaLoop === 'true',
            autoPlay: node.dataset.mediaAutoplay !== 'false',
            trigger: lower(node.dataset.mediaTrigger),
            fadeIn: Math.max(0, number(node.dataset.mediaFadeIn)),
            fadeOut: Math.max(0, number(node.dataset.mediaFadeOut))
        };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:callback:[...page.querySelectorAll(\'[data-media-kind]\')].map@118', __javascriptError); throw __javascriptError; }}).filter(item => { try { return (item.media); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:callback:[...page.querySelectorAll(\'[data-media-kind]\')].map(node => { const me@135', __javascriptError); throw __javascriptError; } });
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:mediaItems@117', __javascriptError); throw __javascriptError; }}
function mediaLength(item) { try { return Math.max(.01, (item.trimEnd - item.trimStart) / item.rate);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:mediaLength@137', __javascriptError); throw __javascriptError; }}

export function initializePublicationTimeline(id, dotnet) { try {
    const root = document.getElementById(id);
    if (!root) return;
    let state = timelineStates.get(root);
    if (!state) {
        state = { root, dotnet, operation: null };
        state.pointerDown = event => { try { return (timelinePointerDown(state, event)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:state.pointerDown@145', __javascriptError); throw __javascriptError; } };
        root.addEventListener('pointerdown', state.pointerDown);
        timelineStates.set(root, state);
    }
    state.dotnet = dotnet;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:initializePublicationTimeline@139', __javascriptError); throw __javascriptError; }}

function timelinePointerDown(state, event) { try {
    if (event.button !== 0) return;
    const clip = event.target.closest('[data-timeline-clip]');
    if (!clip || !state.root.contains(clip)) return;
    const track = clip.closest('[data-timeline-track]');
    if (!track) return;
    const handle = event.target.closest('[data-timeline-handle]');
    const kind = clip.dataset.timelineClip;
    const rawMode = handle?.dataset.timelineHandle || 'move';
    const mode = kind === 'media' ? (rawMode === 'trim-left' || rawMode === 'trim-right' ? rawMode : 'move') : rawMode;
    state.operation = {
        pointerId: event.pointerId,
        clip,
        track,
        kind,
        mode,
        id: clip.dataset.clipId,
        startX: event.clientX,
        originalStart: number(clip.dataset.clipStart),
        originalDuration: Math.max(.05, number(clip.dataset.clipDuration, .5)),
        viewStart: number(track.dataset.viewStart),
        viewEnd: number(track.dataset.viewEnd, 10)
    };
    clip.setPointerCapture(event.pointerId);
    clip.classList.add('dragging');
    const move = moveEvent => { try { return (timelinePointerMove(state, moveEvent)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:move@177', __javascriptError); throw __javascriptError; } };
    let finished = false;
    const finish = upEvent => { try {
        if (finished) return;
        if (upEvent?.pointerId !== undefined && upEvent.pointerId !== event.pointerId) return;
        finished = true;
        clip.removeEventListener('pointermove', move);
        clip.removeEventListener('pointerup', finish);
        clip.removeEventListener('pointercancel', finish);
        clip.removeEventListener('lostpointercapture', finish);
        window.removeEventListener('pointerup', finish, true);
        window.removeEventListener('pointercancel', finish, true);
        window.removeEventListener('blur', finish, true);
        try { if (clip.hasPointerCapture(event.pointerId)) clip.releasePointerCapture(event.pointerId); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:suppressed-catch@190', __caughtJavaScriptError);  }
        timelinePointerUp(state, upEvent || { pointerId: event.pointerId });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:finish@179', __javascriptError); throw __javascriptError; }};
    clip.addEventListener('pointermove', move);
    clip.addEventListener('pointerup', finish);
    clip.addEventListener('pointercancel', finish);
    clip.addEventListener('lostpointercapture', finish);
    window.addEventListener('pointerup', finish, true);
    window.addEventListener('pointercancel', finish, true);
    window.addEventListener('blur', finish, true);
    event.preventDefault();
    event.stopPropagation();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:timelinePointerDown@152', __javascriptError); throw __javascriptError; }}

function timelinePointerMove(state, event) { try {
    const op = state.operation;
    if (!op || op.pointerId !== event.pointerId) return;
    const rect = op.track.getBoundingClientRect();
    const visibleSeconds = Math.max(.1, op.viewEnd - op.viewStart);
    const secondsPerPixel = visibleSeconds / Math.max(1, rect.width);
    const pointerX = clamp(number(event.clientX, op.startX), rect.left, rect.right);
    const delta = (pointerX - op.startX) * secondsPerPixel;
    let start = op.originalStart;
    let duration = op.originalDuration;
    if (op.mode === 'resize-left' || op.mode === 'trim-left') {
        const end = op.originalStart + op.originalDuration;
        start = clamp(op.originalStart + delta, 0, end - .05);
        duration = end - start;
    } else if (op.mode === 'resize-right' || op.mode === 'trim-right') {
        duration = clamp(op.originalDuration + delta, .05, 3600 - op.originalStart);
    } else {
        start = clamp(op.originalStart + delta, 0, 3600 - op.originalDuration);
    }
    start = clamp(number(start), 0, 3600);
    duration = clamp(number(duration, .05), .05, Math.max(.05, 3600 - start));
    op.currentStart = start;
    op.currentDuration = duration;
    op.clip.style.left = `${(start - op.viewStart) / visibleSeconds * 100}%`;
    op.clip.style.width = `${Math.max(.35, duration / visibleSeconds * 100)}%`;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:timelinePointerMove@204', __javascriptError); throw __javascriptError; }}

function timelinePointerUp(state, event) { try {
    const op = state.operation;
    if (!op || op.pointerId !== event.pointerId) return;
    op.clip.classList.remove('dragging');
    const start = op.currentStart ?? op.originalStart;
    const duration = op.currentDuration ?? op.originalDuration;
    state.operation = null;
    if (op.kind === 'animation') state.dotnet.invokeMethodAsync('CommitAnimationTimelineClip', op.id, start, duration);
    else state.dotnet.invokeMethodAsync('CommitMediaTimelineClip', op.id, op.mode, start, duration);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:timelinePointerUp@231', __javascriptError); throw __javascriptError; }}

export function timelineSecondsFromPointer(clientX, viewStart, viewEnd) { try {
    const hovered = document.querySelector('.timeline-ruler:hover,[data-timeline-track]:hover');
    if (!hovered) return Math.max(0, number(viewStart));
    const rect = hovered.getBoundingClientRect();
    const ratio = clamp((number(clientX) - rect.left) / Math.max(1, rect.width), 0, 1);
    return number(viewStart) + ratio * (number(viewEnd) - number(viewStart));
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:timelineSecondsFromPointer@242', __javascriptError); throw __javascriptError; }}

function cancelScrub(pageId) { try {
    const active = scrubAnimations.get(pageId) || [];
    for (const animation of active) { try { animation.cancel(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:suppressed-catch@252', __caughtJavaScriptError);  } }
    scrubAnimations.delete(pageId);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:cancelScrub@250', __javascriptError); throw __javascriptError; }}

export function scrubPublicationTimeline(pageId, seconds) { try {
    const page = document.getElementById(pageId);
    if (!page) return;
    stopPublicationTimeline(pageId, false);
    cancelScrub(pageId);
    const timeMs = Math.max(0, number(seconds)) * 1000;
    const active = [];
    for (const item of animationItems(page)) {
        if (lower(item.animation.trigger) === 'onclick' && item.animation.timelineStartSeconds == null) continue;
        const repeat = Math.max(1, Math.round(number(item.animation.repeatCount, 1)));
        const animation = animateGroup(item.node, item.animation, {
            duration: Math.max(.05, number(item.animation.durationSeconds, .6)) * 1000,
            delay: item.start * 1000,
            easing: easing(item.animation.easing),
            iterations: repeat * (item.animation.autoReverse ? 2 : 1),
            direction: item.animation.autoReverse ? 'alternate' : 'normal',
            fill: lower(item.animation.phase) === 'entrance' ? 'both' : 'forwards'
        });
        animation.pause();
        animation.currentTime = timeMs;
        active.push(animation);
    }
    scrubAnimations.set(pageId, active);
    for (const item of mediaItems(page)) {
        item.media.pause();
        const local = number(seconds) - item.start;
        if (local < 0) item.media.currentTime = item.trimStart;
        else item.media.currentTime = clamp(item.trimStart + local * item.rate, item.trimStart, item.trimEnd);
        item.media.volume = envelopeVolume(item, Math.max(0, local));
        item.media.muted = item.muted;
        item.media.playbackRate = item.rate;
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:scrubPublicationTimeline@256', __javascriptError); throw __javascriptError; }}

function envelopeVolume(item, localSeconds) { try {
    const length = mediaLength(item);
    let gain = 1;
    if (item.fadeIn > 0) gain = Math.min(gain, clamp(localSeconds / item.fadeIn, 0, 1));
    if (item.fadeOut > 0) gain = Math.min(gain, clamp((length - localSeconds) / item.fadeOut, 0, 1));
    return clamp(item.volume * gain, 0, 1);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:envelopeVolume@290', __javascriptError); throw __javascriptError; }}

function isCurrentPageState(pageId, state) { try {
    return pagePlaybackStates.get(pageId) === state && !state.cancelled;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:isCurrentPageState@298', __javascriptError); throw __javascriptError; }}

async function flushTimelineNotifications(state) { try {
    if (state.notificationInFlight) return;
    state.notificationInFlight = true;
    try {
        while (state.pendingNotification) {
            const notification = state.pendingNotification;
            state.pendingNotification = null;
            if (!notification.finished && !isCurrentPageState(state.pageId, state)) continue;
            try {
                await state.dotnet?.invokeMethodAsync(
                    'TimelinePositionChanged',
                    state.runId,
                    notification.seconds,
                    notification.finished);
            } catch {
                state.pendingNotification = null;
                break;
            }
        }
    } finally {
        state.notificationInFlight = false;
        if (state.pendingNotification) void flushTimelineNotifications(state);
    }
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:flushTimelineNotifications@302', __javascriptError); throw __javascriptError; }}

function queueTimelineNotification(state, seconds, finished) { try {
    if (state.cancelled && !finished) return;
    const next = {
        seconds: clamp(number(seconds), state.start, state.end),
        finished: Boolean(finished)
    };
    if (state.pendingNotification?.finished && !next.finished) return;
    state.pendingNotification = next;
    void flushTimelineNotifications(state);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:queueTimelineNotification@327', __javascriptError); throw __javascriptError; }}

function stopPageState(pageId, rewind = true, expectedState = null, cancelNotifications = true) { try {
    const state = pagePlaybackStates.get(pageId);
    if (!state || (expectedState && state !== expectedState)) return false;
    state.cancelled = true;
    cancelAnimationFrame(state.frame);
    for (const animation of state.animations) { try { animation.cancel(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:suppressed-catch@343', __caughtJavaScriptError);  } }
    for (const item of state.mediaItems) {
        item.media.pause();
        if (rewind) item.media.currentTime = item.trimStart;
    }
    if (pagePlaybackStates.get(pageId) === state) pagePlaybackStates.delete(pageId);
    if (cancelNotifications) state.pendingNotification = null;
    return true;
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:stopPageState@338', __javascriptError); throw __javascriptError; }}

export function playPublicationTimeline(pageId, startSeconds, endSeconds, dotnet, runId) { try {
    const page = document.getElementById(pageId);
    if (!page) return;
    stopPageState(pageId, false);
    cancelScrub(pageId);
    const start = Math.max(0, number(startSeconds));
    const end = Math.max(start + .01, number(endSeconds, 10));
    const items = animationItems(page).filter(item => { try { return (lower(item.animation.trigger) !== 'onclick' || item.animation.timelineStartSeconds != null); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:callback:animationItems(page).filter@360', __javascriptError); throw __javascriptError; } });
    const animations = [];
    for (const item of items) {
        const repeat = Math.max(1, Math.round(number(item.animation.repeatCount, 1)));
        const animation = animateGroup(item.node, item.animation, {
            duration: Math.max(.05, number(item.animation.durationSeconds, .6)) * 1000,
            delay: item.start * 1000,
            easing: easing(item.animation.easing),
            iterations: repeat * (item.animation.autoReverse ? 2 : 1),
            direction: item.animation.autoReverse ? 'alternate' : 'normal',
            fill: lower(item.animation.phase) === 'entrance' ? 'both' : 'forwards'
        });
        animation.currentTime = start * 1000;
        animation.play();
        animations.push(animation);
    }
    const medias = mediaItems(page);
    const state = {
        pageId,
        page,
        start,
        end,
        wallStart: performance.now(),
        animations,
        mediaItems: medias,
        dotnet,
        runId: Math.trunc(number(runId)),
        frame: 0,
        lastNotify: 0,
        paused: false,
        cancelled: false,
        notificationInFlight: false,
        pendingNotification: null
    };
    pagePlaybackStates.set(pageId, state);
    for (const item of medias) {
        item.media.pause();
        item.media.muted = item.muted;
        item.media.playbackRate = item.rate;
        const local = start - item.start;
        item.media.currentTime = clamp(item.trimStart + Math.max(0, local) * item.rate, item.trimStart, item.trimEnd);
        if (item.autoPlay && item.trigger !== 'onclick' && local >= 0 && local < mediaLength(item)) item.media.play().catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/timelineInterop.js:promise-catch@401', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:callback:item.media.play().catch@401', __javascriptError); throw __javascriptError; }});
    }
    const tick = now => { try {
        if (!isCurrentPageState(pageId, state) || state.paused) return;
        const seconds = state.start + (now - state.wallStart) / 1000;
        for (const item of medias) {
            const local = seconds - item.start;
            const length = mediaLength(item);
            if (!item.autoPlay || item.trigger === 'onclick' || local < 0 || local > length) {
                if (!item.loop || local < 0) item.media.pause();
                continue;
            }
            if (item.media.paused) item.media.play().catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/timelineInterop.js:promise-catch@413', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:callback:item.media.play().catch@413', __javascriptError); throw __javascriptError; }});
            item.media.volume = envelopeVolume(item, item.loop ? local % length : local);
            if (item.media.currentTime >= item.trimEnd - .02) {
                if (item.loop) item.media.currentTime = item.trimStart;
                else item.media.pause();
            }
        }
        if (!isCurrentPageState(pageId, state)) return;
        if (now - state.lastNotify > 80) {
            state.lastNotify = now;
            queueTimelineNotification(state, Math.min(end, seconds), false);
        }
        if (seconds >= end) {
            queueTimelineNotification(state, end, true);
            stopPageState(pageId, false, state, false);
            return;
        }
        if (isCurrentPageState(pageId, state)) state.frame = requestAnimationFrame(tick);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:tick@403', __javascriptError); throw __javascriptError; }};
    state.frame = requestAnimationFrame(tick);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:playPublicationTimeline@353', __javascriptError); throw __javascriptError; }}

export function pausePublicationTimeline(pageId) { try {
    const state = pagePlaybackStates.get(pageId);
    if (!state || state.paused) return;
    state.paused = true;
    state.pendingNotification = null;
    cancelAnimationFrame(state.frame);
    state.pauseAt = state.start + (performance.now() - state.wallStart) / 1000;
    for (const animation of state.animations) animation.pause();
    for (const item of state.mediaItems) item.media.pause();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:pausePublicationTimeline@435', __javascriptError); throw __javascriptError; }}

export function stopPublicationTimeline(pageId, rewind = true) { try {
    stopPageState(pageId, rewind);
    cancelScrub(pageId);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:stopPublicationTimeline@446', __javascriptError); throw __javascriptError; }}

export async function playMediaClip(elementId, trimStart, trimEnd, volume, rate, muted, loop) { try {
    const root = document.getElementById(elementId);
    const media = root?.querySelector('video,audio');
    if (!media) return;
    media.currentTime = Math.max(0, number(trimStart));
    media.volume = clamp(number(volume, 1), 0, 1);
    media.playbackRate = clamp(number(rate, 1), .25, 4);
    media.muted = Boolean(muted);
    const end = Math.max(media.currentTime + .01, number(trimEnd, media.duration));
    const previous = mediaClipHandlers.get(media);
    if (previous) media.removeEventListener('timeupdate', previous);
    const handler = () => { try {
        if (media.currentTime < end - .02) return;
        if (loop) media.currentTime = Math.max(0, number(trimStart));
        else {
            media.pause();
            media.removeEventListener('timeupdate', handler);
            mediaClipHandlers.delete(media);
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:handler@462', __javascriptError); throw __javascriptError; }};
    mediaClipHandlers.set(media, handler);
    media.addEventListener('timeupdate', handler);
    await media.play();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:playMediaClip@451', __javascriptError); throw __javascriptError; }}

export function pauseMediaClip(elementId) { try {
    const root = document.getElementById(elementId);
    root?.querySelector('video,audio')?.pause();
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:pauseMediaClip@476', __javascriptError); throw __javascriptError; }}

export function disposePublicationTimeline(id) { try {
    const root = document.getElementById(id);
    const state = root ? timelineStates.get(root) : null;
    if (!state) return;
    root.removeEventListener('pointerdown', state.pointerDown);
    timelineStates.delete(root);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/timelineInterop.js:disposePublicationTimeline@481', __javascriptError); throw __javascriptError; }}

