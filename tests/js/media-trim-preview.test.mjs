import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const moduleSource = await readFile(new URL("../../src/PublisherStudio.Web/wwwroot/js/mediaTrimPreview.js", import.meta.url));
const { watchDropSurface, bind, close, playSelection } = await import("data:text/javascript;base64," + moduleSource.toString("base64"));

test("a delayed popup binds once per open and stops playback at the selected end", async () => {
    const elements = new Map();
    const observers = new Set();
    const render = () => [...observers].forEach(observer => observer.callback());
    const bindings = [];
    globalThis.document = { body: {}, getElementById: id => elements.get(id) };
    globalThis.MutationObserver = class {
        constructor(callback) { this.callback = callback; }
        observe() { observers.add(this); }
        disconnect() { observers.delete(this); }
    };
    globalThis.publisherStudio = {
        bindMediaConverterDrop: (element, reference) => bindings.push(["bind", element, reference]),
        unbindMediaConverterDrop: element => bindings.push(["unbind", element])
    };
    globalThis.HTMLMediaElement = { HAVE_METADATA: 1 };
    const reference = {};
    const surface = {};
    const events = new Map();
    const player = {
        readyState: 1,
        duration: 4,
        isConnected: true,
        addEventListener: (name, listener) => events.set(name, listener),
        removeEventListener: name => events.delete(name),
        removeAttribute: name => delete player[name],
        async play() { this.paused = false; },
        pause() { this.paused = true; },
        load() {}
    };

    try {
        watchDropSurface("converter", reference);
        close("converter");
        elements.set("media-converter-source-card", surface);
        render();
        assert.deepEqual(bindings, []);

        elements.clear();
        watchDropSurface("converter", reference);
        watchDropSurface("converter", reference);
        bind("converter", "/fixture.webm", false);
        assert.equal(observers.size, 2);
        elements.set("media-converter-source-card", surface);
        elements.set("media-converter-trim-player", player);
        render();
        assert.deepEqual(bindings, [["bind", surface, reference]]);
        assert.equal(player.src, "/fixture.webm");
        player.currentTime = 0;
        assert.equal(await playSelection("converter", 1.25, 1.5), true);
        assert.equal(player.currentTime, 1.25);
        player.currentTime = 2.9;
        events.get("timeupdate")({ currentTarget: player });
        assert.equal(player.currentTime, 2.75);
        assert.equal(player.paused, true);

        close("converter");
        assert.deepEqual(bindings.at(-1), ["unbind", surface]);
        assert.equal(events.size, 0);
        assert.equal(observers.size, 0);
        assert.equal(player.src, undefined);
    } finally {
        close("converter");
        delete globalThis.document;
        delete globalThis.MutationObserver;
        delete globalThis.publisherStudio;
        delete globalThis.HTMLMediaElement;
    }
});
