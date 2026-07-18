const timelineStates = new Map();
const pagePlaybackStates = new Map();
const scrubAnimations = new Map();
const mediaClipHandlers = new WeakMap();

function number(value, fallback = 0) {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : fallback;
}
function clamp(value, min, max) { return Math.max(min, Math.min(max, value)); }
function lower(value) { return String(value || '').replace(/[^a-z0-9]/gi, '').toLowerCase(); }
function parse(value, fallback) { try { return JSON.parse(value || ''); } catch { return fallback; } }
function easing(value) {
    switch (lower(value)) {
        case 'linear': return 'linear';
        case 'easein': return 'cubic-bezier(.42,0,1,1)';
        case 'easeout': return 'cubic-bezier(0,0,.2,1)';
        case 'backout': return 'cubic-bezier(.18,.89,.32,1.28)';
        case 'bounceout': return 'cubic-bezier(.22,1.3,.36,1)';
        default: return 'cubic-bezier(.4,0,.2,1)';
    }
}
function vector(direction, distance) {
    const amount = number(distance, 18);
    switch (lower(direction)) {
        case 'right': return { x: amount, y: 0 };
        case 'up': return { x: 0, y: -amount };
        case 'down': return { x: 0, y: amount };
        default: return { x: -amount, y: 0 };
    }
}
function baseTransform(node) {
    const value = getComputedStyle(node).transform;
    return !value || value === 'none' ? '' : value;
}
function compose(base, extra) { return `${extra} ${base}`.trim(); }
function frames(node, animation) {
    const effect = lower(animation.effect);
    const phase = lower(animation.phase);
    const base = baseTransform(node);
    const move = vector(animation.direction, animation.distancePercent);
    const scale = Math.max(.01, number(animation.scalePercent, 20) / 100);
    const rotation = number(animation.rotationDegrees, 360);
    const translated = compose(base, `translate(${move.x}%,${move.y}%)`);
    const reverse = items => phase === 'exit' ? [...items].reverse() : items;
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
        case 'shake': return [0, -2, 2, -1.6, 1.6, -.8, .8, 0].map((factor, index, values) => ({ offset: index / (values.length - 1), transform: compose(base, `translateX(${Math.max(2, number(animation.distancePercent, 18) / 4) * factor}%)`) }));
        case 'move': return [{ transform: base || 'none' }, { transform: translated }];
        default: return [{ opacity: 1 }, { opacity: 1 }];
    }
}
function groupNodes(node) {
    const groupId = String(node?.dataset?.groupId || '').trim();
    const page = node?.closest?.('.publication-page,.print-page');
    if (!groupId || !page) return [node];
    const peers = [...page.querySelectorAll('[data-publication-element][data-group-id]')]
        .filter(candidate => String(candidate.dataset.groupId || '') === groupId);
    return peers.length ? peers : [node];
}
function composite(animations) {
    return {
        cancel() { animations.forEach(animation => { try { animation.cancel(); } catch { } }); },
        pause() { animations.forEach(animation => { try { animation.pause(); } catch { } }); },
        play() { animations.forEach(animation => { try { animation.play(); } catch { } }); },
        get currentTime() { return animations[0]?.currentTime || 0; },
        set currentTime(value) { animations.forEach(animation => animation.currentTime = value); }
    };
}
function animateGroup(node, animation, options) {
    const animations = groupNodes(node).map(member => member.animate(frames(member, animation), options));
    return animations.length === 1 ? animations[0] : composite(animations);
}
function animationSpan(animation) {
    return Math.max(.05, number(animation.durationSeconds, .6)) * Math.max(1, number(animation.repeatCount, 1)) * (animation.autoReverse ? 2 : 1);
}
function animationItems(page) {
    const items = [...page.querySelectorAll('[data-publication-element]')].flatMap(node =>
        parse(node.dataset.animations, []).map(animation => ({ node, animation }))
    ).sort((a, b) => number(a.animation.order) - number(b.animation.order));
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
}
function mediaItems(page) {
    return [...page.querySelectorAll('[data-media-kind]')].map(node => {
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
    }).filter(item => item.media);
}
function mediaLength(item) { return Math.max(.01, (item.trimEnd - item.trimStart) / item.rate); }

export function initializePublicationTimeline(id, dotnet) {
    const root = document.getElementById(id);
    if (!root) return;
    let state = timelineStates.get(root);
    if (!state) {
        state = { root, dotnet, operation: null };
        state.pointerDown = event => timelinePointerDown(state, event);
        root.addEventListener('pointerdown', state.pointerDown);
        timelineStates.set(root, state);
    }
    state.dotnet = dotnet;
}

function timelinePointerDown(state, event) {
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
    const move = moveEvent => timelinePointerMove(state, moveEvent);
    let finished = false;
    const finish = upEvent => {
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
        try { if (clip.hasPointerCapture(event.pointerId)) clip.releasePointerCapture(event.pointerId); } catch { }
        timelinePointerUp(state, upEvent || { pointerId: event.pointerId });
    };
    clip.addEventListener('pointermove', move);
    clip.addEventListener('pointerup', finish);
    clip.addEventListener('pointercancel', finish);
    clip.addEventListener('lostpointercapture', finish);
    window.addEventListener('pointerup', finish, true);
    window.addEventListener('pointercancel', finish, true);
    window.addEventListener('blur', finish, true);
    event.preventDefault();
    event.stopPropagation();
}

function timelinePointerMove(state, event) {
    const op = state.operation;
    if (!op || op.pointerId !== event.pointerId) return;
    const rect = op.track.getBoundingClientRect();
    const secondsPerPixel = (op.viewEnd - op.viewStart) / Math.max(1, rect.width);
    const delta = (event.clientX - op.startX) * secondsPerPixel;
    let start = op.originalStart;
    let duration = op.originalDuration;
    if (op.mode === 'resize-left' || op.mode === 'trim-left') {
        const end = op.originalStart + op.originalDuration;
        start = clamp(op.originalStart + delta, 0, end - .05);
        duration = end - start;
    } else if (op.mode === 'resize-right' || op.mode === 'trim-right') {
        duration = Math.max(.05, op.originalDuration + delta);
    } else {
        start = Math.max(0, op.originalStart + delta);
    }
    op.currentStart = start;
    op.currentDuration = duration;
    op.clip.style.left = `${(start - op.viewStart) / (op.viewEnd - op.viewStart) * 100}%`;
    op.clip.style.width = `${Math.max(.35, duration / (op.viewEnd - op.viewStart) * 100)}%`;
}

function timelinePointerUp(state, event) {
    const op = state.operation;
    if (!op || op.pointerId !== event.pointerId) return;
    op.clip.classList.remove('dragging');
    const start = op.currentStart ?? op.originalStart;
    const duration = op.currentDuration ?? op.originalDuration;
    state.operation = null;
    if (op.kind === 'animation') state.dotnet.invokeMethodAsync('CommitAnimationTimelineClip', op.id, start, duration);
    else state.dotnet.invokeMethodAsync('CommitMediaTimelineClip', op.id, op.mode, start, duration);
}

export function timelineSecondsFromPointer(clientX, viewStart, viewEnd) {
    const hovered = document.querySelector('.timeline-ruler:hover,[data-timeline-track]:hover');
    if (!hovered) return Math.max(0, number(viewStart));
    const rect = hovered.getBoundingClientRect();
    const ratio = clamp((number(clientX) - rect.left) / Math.max(1, rect.width), 0, 1);
    return number(viewStart) + ratio * (number(viewEnd) - number(viewStart));
}

function cancelScrub(pageId) {
    const active = scrubAnimations.get(pageId) || [];
    for (const animation of active) { try { animation.cancel(); } catch { } }
    scrubAnimations.delete(pageId);
}

export function scrubPublicationTimeline(pageId, seconds) {
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
}

function envelopeVolume(item, localSeconds) {
    const length = mediaLength(item);
    let gain = 1;
    if (item.fadeIn > 0) gain = Math.min(gain, clamp(localSeconds / item.fadeIn, 0, 1));
    if (item.fadeOut > 0) gain = Math.min(gain, clamp((length - localSeconds) / item.fadeOut, 0, 1));
    return clamp(item.volume * gain, 0, 1);
}

function stopPageState(pageId, rewind = true) {
    const state = pagePlaybackStates.get(pageId);
    if (!state) return;
    cancelAnimationFrame(state.frame);
    for (const animation of state.animations) { try { animation.cancel(); } catch { } }
    for (const item of state.mediaItems) {
        item.media.pause();
        if (rewind) item.media.currentTime = item.trimStart;
    }
    pagePlaybackStates.delete(pageId);
}

export function playPublicationTimeline(pageId, startSeconds, endSeconds, dotnet) {
    const page = document.getElementById(pageId);
    if (!page) return;
    stopPageState(pageId, false);
    cancelScrub(pageId);
    const start = Math.max(0, number(startSeconds));
    const end = Math.max(start + .01, number(endSeconds, 10));
    const items = animationItems(page).filter(item => lower(item.animation.trigger) !== 'onclick' || item.animation.timelineStartSeconds != null);
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
    const state = { page, start, end, wallStart: performance.now(), animations, mediaItems: medias, dotnet, frame: 0, lastNotify: 0, paused: false };
    pagePlaybackStates.set(pageId, state);
    for (const item of medias) {
        item.media.pause();
        item.media.muted = item.muted;
        item.media.playbackRate = item.rate;
        const local = start - item.start;
        item.media.currentTime = clamp(item.trimStart + Math.max(0, local) * item.rate, item.trimStart, item.trimEnd);
        if (item.autoPlay && item.trigger !== 'onclick' && local >= 0 && local < mediaLength(item)) item.media.play().catch(() => {});
    }
    const tick = now => {
        if (!pagePlaybackStates.has(pageId) || state.paused) return;
        const seconds = state.start + (now - state.wallStart) / 1000;
        for (const item of medias) {
            const local = seconds - item.start;
            const length = mediaLength(item);
            if (!item.autoPlay || item.trigger === 'onclick' || local < 0 || local > length) {
                if (!item.loop || local < 0) item.media.pause();
                continue;
            }
            if (item.media.paused) item.media.play().catch(() => {});
            item.media.volume = envelopeVolume(item, item.loop ? local % length : local);
            if (item.media.currentTime >= item.trimEnd - .02) {
                if (item.loop) item.media.currentTime = item.trimStart;
                else item.media.pause();
            }
        }
        if (now - state.lastNotify > 80) {
            state.lastNotify = now;
            state.dotnet?.invokeMethodAsync('TimelinePositionChanged', Math.min(end, seconds), false);
        }
        if (seconds >= end) {
            stopPageState(pageId, false);
            state.dotnet?.invokeMethodAsync('TimelinePositionChanged', end, true);
            return;
        }
        state.frame = requestAnimationFrame(tick);
    };
    state.frame = requestAnimationFrame(tick);
}

export function pausePublicationTimeline(pageId) {
    const state = pagePlaybackStates.get(pageId);
    if (!state || state.paused) return;
    state.paused = true;
    cancelAnimationFrame(state.frame);
    state.pauseAt = state.start + (performance.now() - state.wallStart) / 1000;
    for (const animation of state.animations) animation.pause();
    for (const item of state.mediaItems) item.media.pause();
}

export function stopPublicationTimeline(pageId, rewind = true) {
    stopPageState(pageId, rewind);
    cancelScrub(pageId);
}

export async function playMediaClip(elementId, trimStart, trimEnd, volume, rate, muted, loop) {
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
    const handler = () => {
        if (media.currentTime < end - .02) return;
        if (loop) media.currentTime = Math.max(0, number(trimStart));
        else {
            media.pause();
            media.removeEventListener('timeupdate', handler);
            mediaClipHandlers.delete(media);
        }
    };
    mediaClipHandlers.set(media, handler);
    media.addEventListener('timeupdate', handler);
    await media.play();
}

export function pauseMediaClip(elementId) {
    const root = document.getElementById(elementId);
    root?.querySelector('video,audio')?.pause();
}

export function disposePublicationTimeline(id) {
    const root = document.getElementById(id);
    const state = root ? timelineStates.get(root) : null;
    if (!state) return;
    root.removeEventListener('pointerdown', state.pointerDown);
    timelineStates.delete(root);
}
