// javascript-diagnostics: guarded
var publisherStudioDiagnostics = globalThis.publisherStudioJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`PublisherStudio JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("PublisherStudio fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`PublisherStudio fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
(function () { try {
    "use strict";

    const states = new Map();
    const decoder = new TextDecoder();
    let pointerOwnershipBound = false;

    function clearVisualInteraction(state) { try {
        const instance = state?.instance;
        if (!instance) return;
        try { instance.hideTooltip?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:suppressed-catch@12', __caughtJavaScriptError);  }
        try { instance.clearHover?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:suppressed-catch@13', __caughtJavaScriptError);  }
        try {
            instance.getAllSeries?.().forEach(series => { try {
                try { series.clearHover?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:suppressed-catch@16', __caughtJavaScriptError);  }
                try { series.getAllPoints?.().forEach(point => { try { return (point.clearHover?.()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:series.getAllPoints?.().forEach@17', __javascriptError); throw __javascriptError; } }); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:suppressed-catch@17', __caughtJavaScriptError);  }
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:instance.getAllSeries?.().forEach@15', __javascriptError); throw __javascriptError; }});
        } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:suppressed-catch@19', __caughtJavaScriptError);  }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:clearVisualInteraction@9', __javascriptError); throw __javascriptError; }}

    function eventBelongsTo(element, event) { try {
        if (!element || !event) return false;
        const path = typeof event.composedPath === "function" ? event.composedPath() : [];
        if (path.includes(element)) return true;
        return event.target instanceof Node && element.contains(event.target);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:eventBelongsTo@22', __javascriptError); throw __javascriptError; }}

    function clearVisualsOutside(event) { try {
        states.forEach((state, element) => { try {
            if (!eventBelongsTo(element, event)) clearVisualInteraction(state);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:states.forEach@30', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:clearVisualsOutside@29', __javascriptError); throw __javascriptError; }}

    function clearAllVisualInteractions() { try {
        states.forEach(clearVisualInteraction);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:clearAllVisualInteractions@35', __javascriptError); throw __javascriptError; }}

    function bindPointerOwnership() { try {
        if (pointerOwnershipBound || typeof document === "undefined") return;
        pointerOwnershipBound = true;
        document.addEventListener("pointerover", clearVisualsOutside, true);
        document.addEventListener("pointerdown", clearVisualsOutside, true);
        document.addEventListener("pointerout", event => { try {
            if (!event.relatedTarget) clearAllVisualInteractions();
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:document.addEventListener@44', __javascriptError); throw __javascriptError; }}, true);
        window.addEventListener("blur", clearAllVisualInteractions);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:bindPointerOwnership@39', __javascriptError); throw __javascriptError; }}

    function decodeConfig(value) { try {
        if (!value) return null;
        if (typeof value === "object") return value;
        try {
            const binary = atob(value);
            const bytes = Uint8Array.from(binary, character => { try { return (character.charCodeAt(0)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:Uint8Array.from@55', __javascriptError); throw __javascriptError; } });
            return JSON.parse(decoder.decode(bytes));
        } catch {
            try { return JSON.parse(value); } catch { return null; }
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:decodeConfig@50', __javascriptError); throw __javascriptError; }}

    function number(value, declaredKind) { try {
        if (typeof value === "number") return Number.isFinite(value) ? value : 0;
        if (typeof value === "boolean") return value ? 1 : 0;

        const raw = String(value ?? "").trim();
        const kind = String(declaredKind || "").toLowerCase();
        if (kind === "boolean") return /^true$/i.test(raw) ? 1 : 0;
        if (kind === "text" || kind === "datetime") return raw ? 1 : 0;
        if (/^true$/i.test(raw)) return 1;
        if (/^false$/i.test(raw) || !raw) return 0;

        let normalized = raw.replace(/[\s'’]/g, "");
        const negativeParentheses = /^\(.*\)$/.test(normalized);
        normalized = normalized.replace(/[()]/g, "").replace(/[^0-9+\-.,eE]/g, "");
        const comma = normalized.lastIndexOf(",");
        const dot = normalized.lastIndexOf(".");
        if (comma >= 0 && dot >= 0) {
            const decimal = comma > dot ? "," : ".";
            const group = decimal === "," ? "." : ",";
            normalized = normalized.split(group).join("");
            if (decimal === ",") normalized = normalized.replace(/,/g, ".");
        } else if (comma >= 0) {
            const parts = normalized.split(",");
            normalized = parts.length > 2 && parts.slice(1).every(part => { try { return (part.length === 3); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:parts.slice(1).every@85', __javascriptError); throw __javascriptError; } })
                ? parts.join("")
                : `${parts.slice(0, -1).join("")}.${parts.at(-1)}`;
        } else if ((normalized.match(/\./g) || []).length > 1) {
            const parts = normalized.split(".");
            normalized = parts.slice(1).every(part => { try { return (part.length === 3); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:parts.slice(1).every@90', __javascriptError); throw __javascriptError; } })
                ? parts.join("")
                : `${parts.slice(0, -1).join("")}.${parts.at(-1)}`;
        }
        const parsed = Number(normalized);
        if (!Number.isFinite(parsed)) return raw ? 1 : 0;
        return negativeParentheses ? -Math.abs(parsed) : parsed;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:number@62', __javascriptError); throw __javascriptError; }}

    function get(row, field) { try {
        if (!row || !field) return "";
        if (Object.prototype.hasOwnProperty.call(row, field)) return row[field];
        const wanted = field.toLowerCase();
        const key = Object.keys(row).find(candidate => { try { return (candidate.toLowerCase() === wanted); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:Object.keys(row).find@103', __javascriptError); throw __javascriptError; } });
        return key ? row[key] : "";
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:get@99', __javascriptError); throw __javascriptError; }}

    function friendly(value) { try {
        return String(value || "").replace(/([a-z0-9])([A-Z])/g, "$1 $2");
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:friendly@107', __javascriptError); throw __javascriptError; }}

    function visualRoot(element) { try {
        if (!element) return null;
        return element.matches?.("[data-ps-visual-config]") ? element : element.querySelector?.("[data-ps-visual-config]");
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:visualRoot@111', __javascriptError); throw __javascriptError; }}

    function dataBaseUrl() { try {
        const query = new URLSearchParams(location.search).get("publisherApi");
        let stored = "";
        try { stored = localStorage.getItem("PublisherStudioDataBaseUrl") || ""; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:suppressed-catch@119', __caughtJavaScriptError);  }
        const configured = query || window.PublisherStudioDataBaseUrl || stored;
        if (configured) return String(configured).replace(/\/$/, "");
        if (/^https?:$/.test(location.protocol)) return location.origin;
        return "";
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:dataBaseUrl@116', __javascriptError); throw __javascriptError; }}

    function resolveUrl(url) { try {
        if (!url) return "";
        try { return new URL(url).toString(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:suppressed-catch@128', __caughtJavaScriptError);  }
        const base = dataBaseUrl();
        return base ? new URL(url.replace(/^\//, ""), base + "/").toString() : "";
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:resolveUrl@126', __javascriptError); throw __javascriptError; }}

    function selectJsonPath(value, path) { try {
        if (!path) return value;
        return path.split(".").filter(Boolean).reduce((current, segment) => { try {
            if (current == null) return undefined;
            if (Array.isArray(current) && /^\d+$/.test(segment)) return current[Number(segment)];
            if (typeof current !== "object") return undefined;
            if (Object.prototype.hasOwnProperty.call(current, segment)) return current[segment];
            const key = Object.keys(current).find(candidate => { try { return (candidate.toLowerCase() === segment.toLowerCase()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:Object.keys(current).find@140', __javascriptError); throw __javascriptError; } });
            return key ? current[key] : undefined;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:path.split(".").filter(Boolean).reduce@135', __javascriptError); throw __javascriptError; }}, value);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:selectJsonPath@133', __javascriptError); throw __javascriptError; }}

    function unwrapJsonString(value) { try {
        if (typeof value !== "string") return value;
        const trimmed = value.trim();
        if (!trimmed.startsWith("{") && !trimmed.startsWith("[")) return value;
        try { return JSON.parse(trimmed); } catch { return value; }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:unwrapJsonString@145', __javascriptError); throw __javascriptError; }}

    function findJsonRows(value, depth) { try {
        if (Array.isArray(value)) return value;
        if (!value || typeof value !== "object" || depth > 4) return null;

        const entries = Object.entries(value);
        const wrappers = new Set(["data", "items", "results", "records", "rows"]);
        for (const [name, nested] of entries.filter(([name]) => { try { return (wrappers.has(name.toLowerCase())); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:entries.filter@158', __javascriptError); throw __javascriptError; } })) {
            if (Array.isArray(nested)) return nested;
            const rows = findJsonRows(nested, depth + 1);
            if (rows) return rows;
        }

        const arrays = entries.map(([, nested]) => { try { return (nested); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:entries.map@164', __javascriptError); throw __javascriptError; } }).filter(Array.isArray);
        if (arrays.length === 1) return arrays[0];
        const objects = entries.map(([, nested]) => { try { return (nested); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:entries.map@166', __javascriptError); throw __javascriptError; } }).filter(nested => { try { return (nested && typeof nested === "object" && !Array.isArray(nested)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:entries.map(([, nested]) => nested).filter@166', __javascriptError); throw __javascriptError; } });
        return objects.length === 1 ? findJsonRows(objects[0], depth + 1) : null;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:findJsonRows@152', __javascriptError); throw __javascriptError; }}

    function flattenJsonRow(source, prefix, target) { try {
        for (const [property, value] of Object.entries(source || {})) {
            const name = prefix ? `${prefix}.${property}` : property;
            if (value && typeof value === "object" && !Array.isArray(value)) {
                flattenJsonRow(value, name, target);
            } else if (value === null || value === undefined) {
                target[name] = "";
            } else if (Array.isArray(value)) {
                target[name] = JSON.stringify(value);
            } else {
                target[name] = value;
            }
        }
        return target;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:flattenJsonRow@170', __javascriptError); throw __javascriptError; }}

    function parseDelimited(text, delimiter, hasHeaders) { try {
        delimiter = delimiter || ",";
        const rows = [];
        let row = [], field = "", quoted = false;
        for (let index = 0; index <= text.length; index++) {
            const character = index < text.length ? text[index] : "\n";
            if (quoted) {
                if (character === '"' && text[index + 1] === '"') { field += '"'; index++; }
                else if (character === '"') quoted = false;
                else field += character;
            } else if (character === '"') quoted = true;
            else if (character === delimiter) { row.push(field); field = ""; }
            else if (character === "\n") {
                row.push(field.replace(/\r$/, "")); field = "";
                if (row.some(value => { try { return (value !== ""); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:row.some@200', __javascriptError); throw __javascriptError; } })) rows.push(row);
                row = [];
            } else field += character;
        }
        if (!rows.length) return [];
        const width = Math.max(...rows.map(item => { try { return (item.length); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:rows.map@205', __javascriptError); throw __javascriptError; } }));
        const rawHeaders = hasHeaders
            ? rows.shift().map((value, index) => { try { return (value || `Column ${index + 1}`); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:rows.shift().map@207', __javascriptError); throw __javascriptError; } })
            : Array.from({ length: width }, (_, index) => { try { return (`Column ${index + 1}`); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:Array.from@208', __javascriptError); throw __javascriptError; } });
        const usedHeaders = new Set();
        const headers = rawHeaders.map((raw, index) => { try {
            const basis = String(raw || `Column ${index + 1}`).trim() || `Column ${index + 1}`;
            let candidate = basis, suffix = 2;
            while (usedHeaders.has(candidate.toLowerCase())) candidate = `${basis} ${suffix++}`;
            usedHeaders.add(candidate.toLowerCase());
            return candidate;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:rawHeaders.map@210', __javascriptError); throw __javascriptError; }});
        return rows.map(values => { try { return (Object.fromEntries(headers.map((header, index) => { try { return ([header, values[index] ?? ""]); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:headers.map@217', __javascriptError); throw __javascriptError; } }))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:rows.map@217', __javascriptError); throw __javascriptError; } });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:parseDelimited@186', __javascriptError); throw __javascriptError; }}

    function parseXml(text) { try {
        const document = new DOMParser().parseFromString(text, "application/xml");
        if (document.querySelector("parsererror")) throw new Error("The endpoint returned invalid XML.");
        const root = document.documentElement;
        let nodes = Array.from(root.children);
        const groups = new Map();
        nodes.forEach(node => { try { return (groups.set(node.localName, [...(groups.get(node.localName) || []), node])); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:nodes.forEach@226', __javascriptError); throw __javascriptError; } });
        const repeated = [...groups.values()].sort((a, b) => { try { return (b.length - a.length); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:[...groups.values()].sort@227', __javascriptError); throw __javascriptError; } })[0];
        if (repeated?.length > 1) nodes = repeated;
        else if (nodes.length === 1 && nodes[0].children.length) nodes = Array.from(nodes[0].children);
        else if (!nodes.length) nodes = [root];

        return nodes.map(node => { try {
            const result = {};
            const add = (name, value, attribute) => { try {
                const basis = String(name || (attribute ? "Attribute" : "Value")).trim() || (attribute ? "Attribute" : "Value");
                let candidate = basis;
                if (Object.prototype.hasOwnProperty.call(result, candidate) && attribute) candidate = `@${basis}`;
                let suffix = 2;
                while (Object.prototype.hasOwnProperty.call(result, candidate)) candidate = `${basis} ${suffix++}`;
                result[candidate] = String(value ?? "").trim();
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:add@234', __javascriptError); throw __javascriptError; }};
            Array.from(node.attributes).forEach(attribute => { try { return (add(attribute.localName, attribute.value, true)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:Array.from(node.attributes).forEach@242', __javascriptError); throw __javascriptError; } });
            const children = Array.from(node.children);
            if (!children.length) {
                add(node === root ? root.localName : "Value", node.textContent || "", false);
            } else {
                children.forEach(child => { try {
                    add(child.localName, child.textContent || "", false);
                    Array.from(child.attributes).forEach(attribute => { try { return (add(`${child.localName}.@${attribute.localName}`, attribute.value, true)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:Array.from(child.attributes).forEach@249', __javascriptError); throw __javascriptError; } });
                 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:children.forEach@247', __javascriptError); throw __javascriptError; }});
            }
            return result;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:nodes.map@232', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:parseXml@220', __javascriptError); throw __javascriptError; }}

    function parseResponse(text, contentType, live) { try {
        contentType = String(contentType || "").toLowerCase();
        let format = String(live.responseFormat || "Auto").toLowerCase();
        if (format === "auto") {
            const trimmed = text.trimStart();
            let encodedJson = false;
            if (trimmed.startsWith('"')) {
                try {
                    const decoded = JSON.parse(trimmed);
                    encodedJson = typeof decoded === "string" && /^[\s]*[\[{]/.test(decoded);
                } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:suppressed-catch@266', __caughtJavaScriptError);  }
            }
            format = contentType.includes("json") || trimmed.startsWith("{") || trimmed.startsWith("[") || encodedJson ? "json"
                : contentType.includes("xml") || trimmed.startsWith("<") ? "xml" : "delimitedtext";
        }
        if (format === "json") {
            let value = unwrapJsonString(JSON.parse(text));
            value = unwrapJsonString(selectJsonPath(value, live.jsonPath));
            const rows = findJsonRows(value, 0) || (value && typeof value === "object" && !Array.isArray(value) ? [value] : []);
            if (rows.some(item => { try { return (!item || typeof item !== "object" || Array.isArray(item)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:rows.some@275', __javascriptError); throw __javascriptError; } }))
                throw new Error("Every JSON row must be an object with named properties.");
            return rows.map(row => { try { return (flattenJsonRow(row, "", {})); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:rows.map@277', __javascriptError); throw __javascriptError; } });
        }
        if (format === "xml") return parseXml(text);
        if (format === "text") return [{ Value: text }];
        return parseDelimited(text, live.delimiter || ",", live.firstRowContainsHeaders !== false);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:parseResponse@256', __javascriptError); throw __javascriptError; }}

    async function fetchRows(config) { try {
        const live = config.live;
        if (!live?.enabled || !live.allowExportedHtmlFetch) return null;
        if (String(live.transport).toLowerCase() === "stream") return null;

        // Prefer the tokenized monolith snapshot when the exported page is connected
        // with ?publisherApi=... . It works for local APIs, external REST polling, and
        // webhook bindings and avoids browser CORS restrictions on the original source.
        const monolithUrl = live.monolithRowsUrl ? resolveUrl(live.monolithRowsUrl) : "";
        if (monolithUrl) {
            const response = await fetch(monolithUrl, { method: "GET", cache: "no-store" });
            if (response.ok) {
                const value = await response.json();
                if (Array.isArray(value)) return value;
            } else if (String(live.transport).toLowerCase() === "webhook") {
                throw new Error(`PublisherStudio data endpoint returned ${response.status} ${response.statusText}.`);
            }
        }

        if (String(live.transport).toLowerCase() === "webhook") return null;
        const url = resolveUrl(live.url);
        if (!url) throw new Error("A data server address is required. Open the HTML with ?publisherApi=http://127.0.0.1:PORT or set PublisherStudioDataBaseUrl.");
        const headers = {};
        (live.headers || []).forEach(header => { try { if (header.name) headers[header.name] = header.value || "";  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:(live.headers || []).forEach@307', __javascriptError); throw __javascriptError; }});
        const method = String(live.method || "GET").toUpperCase();
        const response = await fetch(url, {
            method,
            headers,
            body: ["GET", "HEAD"].includes(method) ? undefined : (live.body || ""),
            cache: "no-store"
        });
        const text = await response.text();
        if (!response.ok) throw new Error(`Data endpoint returned ${response.status} ${response.statusText}.`);
        return parseResponse(text, response.headers.get("content-type") || "", live);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:fetchRows@284', __javascriptError); throw __javascriptError; }}

    function disposeWidget(element) { try {
        const state = states.get(element);
        if (state?.timer) clearInterval(state.timer);
        clearVisualInteraction(state);
        try {
            const instance = state?.instance;
            if (instance?.dispose) instance.dispose();
        } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:suppressed-catch@327', __caughtJavaScriptError);  }
        states.delete(element);
        element.replaceChildren();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:disposeWidget@320', __javascriptError); throw __javascriptError; }}

    function fallback(element, config, rows, error) { try {
        element.replaceChildren();
        const wrapper = document.createElement("div");
        wrapper.className = "ps-live-fallback";
        if (config.showTitle) {
            const title = document.createElement("strong");
            title.textContent = config.title || friendly(config.kind);
            wrapper.append(title);
        }
        if (error) {
            const message = document.createElement("small");
            message.textContent = error;
            wrapper.append(message);
        }
        const table = document.createElement("table");
        const columns = [...new Set((rows || []).flatMap(row => { try { return (Object.keys(row || {})); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:(rows || []).flatMap@347', __javascriptError); throw __javascriptError; } }))].slice(0, 12);
        if (columns.length) {
            const head = document.createElement("thead");
            const tr = document.createElement("tr");
            columns.forEach(column => { try { const th = document.createElement("th"); th.textContent = column; tr.append(th);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:columns.forEach@351', __javascriptError); throw __javascriptError; }});
            head.append(tr); table.append(head);
            const body = document.createElement("tbody");
            (rows || []).slice(0, config.rowLimit || 20).forEach(row => { try {
                const tr = document.createElement("tr");
                columns.forEach(column => { try { const td = document.createElement("td"); td.textContent = get(row, column); tr.append(td);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:columns.forEach@356', __javascriptError); throw __javascriptError; }});
                body.append(tr);
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:(rows || []).slice(0, config.rowLimit || 20).forEach@354', __javascriptError); throw __javascriptError; }});
            table.append(body);
        }
        wrapper.append(table);
        element.append(wrapper);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:fallback@332', __javascriptError); throw __javascriptError; }}

    function columnKind(config, field) { try {
        return String(config?.columnKinds?.[field] || config?.columnKinds?.[String(field || "").toLowerCase()] || "").toLowerCase();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:columnKind@365', __javascriptError); throw __javascriptError; }}

    function measure(config, row, field) { try {
        return number(get(row, field), columnKind(config, field));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:measure@369', __javascriptError); throw __javascriptError; }}

    function argumentValue(config, row, index) { try {
        const raw = get(row, config.argumentField);
        const mode = String(config.argumentMode || "Auto").toLowerCase();
        const kind = columnKind(config, config.argumentField);
        if (mode === "continuous" || (mode === "auto" && kind === "number")) return number(raw, "number");
        if (mode === "datetime" || (mode === "auto" && kind === "datetime")) {
            const date = raw instanceof Date ? raw : new Date(raw);
            return Number.isFinite(date.getTime()) ? date : new Date(0);
        }
        const text = String(raw ?? "").trim();
        return text || `(blank ${index + 1})`;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:argumentValue@373', __javascriptError); throw __javascriptError; }}

    function argumentKey(value) { try {
        if (value instanceof Date) return `date:${value.getTime()}`;
        return `${typeof value}:${String(value)}`;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:argumentKey@386', __javascriptError); throw __javascriptError; }}

    function argumentAxis(config) { try {
        const mode = String(config.argumentMode || "Auto").toLowerCase();
        const kind = columnKind(config, config.argumentField);
        if (mode === "datetime" || (mode === "auto" && kind === "datetime")) return { type: "continuous", argumentType: "datetime" };
        if (mode === "continuous" || (mode === "auto" && kind === "number")) return { type: "continuous", argumentType: "numeric" };
        return { type: "discrete", discreteAxisDivisionMode: "crossLabels" };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:argumentAxis@391', __javascriptError); throw __javascriptError; }}

    function rangeScale(config) { try {
        const mode = String(config.argumentMode || "Auto").toLowerCase();
        const kind = columnKind(config, config.argumentField);
        if (mode === "datetime" || (mode === "auto" && kind === "datetime")) return { type: "continuous", valueType: "datetime" };
        if (mode === "continuous" || (mode === "auto" && kind === "number")) return { type: "continuous", valueType: "numeric" };
        return { type: "discrete", valueType: "string", discreteAxisDivisionMode: "crossLabels" };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:rangeScale@399', __javascriptError); throw __javascriptError; }}

    function sortVisualPoints(config, points) { try {
        const mode = String(config.sortMode || "DataOrder").toLowerCase();
        if (mode === "dataorder") return points;
        const direction = mode.endsWith("descending") ? -1 : 1;
        const byValue = mode.startsWith("value");
        return [...points].sort((left, right) => { try {
            const a = byValue ? number(left.value) : left.argument;
            const b = byValue ? number(right.value) : right.argument;
            const av = a instanceof Date ? a.getTime() : a;
            const bv = b instanceof Date ? b.getTime() : b;
            if (typeof av === "number" && typeof bv === "number") return (av - bv) * direction;
            return String(av).localeCompare(String(bv), undefined, { numeric: true, sensitivity: "base" }) * direction;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:[...points].sort@412', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:sortVisualPoints@407', __javascriptError); throw __javascriptError; }}

    function aggregateVisualPoints(config, points, enabled = true) { try {
        if (!enabled) return sortVisualPoints(config, points);
        const mode = String(config.aggregationMode || "Auto").toLowerCase();
        if (mode === "none") return sortVisualPoints(config, points);
        const groups = new Map();
        points.forEach((point, index) => { try {
            const key = `${String(point.series || "")}\u0000${argumentKey(point.argument)}`;
            const group = groups.get(key) || { ...point, value: 0, __values: [], __first: index };
            group.__values.push(number(point.value));
            groups.set(key, group);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:points.forEach@427', __javascriptError); throw __javascriptError; }});
        const aggregated = [...groups.values()].map(group => { try {
            const values = group.__values;
            let value;
            switch (mode) {
                case "average": value = values.reduce((sum, item) => { try { return (sum + item); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:values.reduce@437', __javascriptError); throw __javascriptError; } }, 0) / Math.max(1, values.length); break;
                case "minimum": value = Math.min(...values); break;
                case "maximum": value = Math.max(...values); break;
                case "count": value = values.length; break;
                case "sum":
                case "auto":
                default: value = values.reduce((sum, item) => { try { return (sum + item); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:values.reduce@443', __javascriptError); throw __javascriptError; } }, 0); break;
            }
            const result = { ...group, value };
            delete result.__values;
            delete result.__first;
            return result;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:[...groups.values()].map@433', __javascriptError); throw __javascriptError; }});
        return sortVisualPoints(config, aggregated);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:aggregateVisualPoints@422', __javascriptError); throw __javascriptError; }}

    function visualTooltip(config) { try {
        const element = config?.__element;
        const exportedOwner = element?.closest?.(".website-publication [data-publication-element]");
        return exportedOwner
            ? { enabled: true, container: exportedOwner }
            : { enabled: true };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:visualTooltip@453', __javascriptError); throw __javascriptError; }}

    function common(config) { try {
        return {
            title: config.showTitle ? { text: config.title || "" } : undefined,
            legend: { visible: config.showLegend !== false },
            tooltip: visualTooltip(config),
            animation: { enabled: false },
            size: { width: elementSize(config).width, height: elementSize(config).height }
        };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:common@461', __javascriptError); throw __javascriptError; }}

    function elementSize(config) { try {
        const element = config.__element;
        return { width: Math.max(1, element?.clientWidth || 1), height: Math.max(1, element?.clientHeight || 1) };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:elementSize@471', __javascriptError); throw __javascriptError; }}

    function seriesData(config, rows, oneValuePerGroup) { try {
        const argument = config.argumentField;
        const seriesField = config.seriesField;
        let values = config.valueFields?.length ? config.valueFields : [config.highValueField || config.closeValueField || "Value"];
        if (oneValuePerGroup) values = [values[0]];
        const result = [];
        if (seriesField) {
            const names = [...new Set(rows.map(row => { try { return (String(get(row, seriesField))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:rows.map@483', __javascriptError); throw __javascriptError; } }))];
            names.forEach(name => { try { return (values.forEach(field => { try { return (result.push({ name: values.length > 1 ? `${name} · ${field}` : name, field, series: name })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:values.forEach@484', __javascriptError); throw __javascriptError; } })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:names.forEach@484', __javascriptError); throw __javascriptError; } });
        } else values.forEach(field => { try { return (result.push({ name: values.length > 1 ? field : (config.title || field), field, series: null })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:values.forEach@485', __javascriptError); throw __javascriptError; } });
        return { argument, seriesField, result };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:seriesData@476', __javascriptError); throw __javascriptError; }}

    function mapCartesianType(style) { try {
        const map = {
            Bar: "bar", Line: "line", Spline: "spline", Scatter: "scatter", Area: "area", SplineArea: "splinearea",
            StepLine: "stepline", StepArea: "steparea", StackedBar: "stackedbar", FullStackedBar: "fullstackedbar",
            StackedArea: "stackedarea", FullStackedArea: "fullstackedarea", StackedLine: "stackedline",
            FullStackedLine: "fullstackedline", StackedSpline: "stackedspline", FullStackedSpline: "fullstackedspline",
            StackedSplineArea: "stackedsplinearea", FullStackedSplineArea: "fullstackedsplinearea",
            RangeArea: "rangearea", RangeBar: "rangebar", Bubble: "bubble", Candlestick: "candlestick", Stock: "stock"
        };
        return map[style] || "bar";
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:mapCartesianType@489', __javascriptError); throw __javascriptError; }}

    function chartOptions(config, rows) { try {
        const type = mapCartesianType(config.cartesianStyle);
        const financial = ["candlestick", "stock"].includes(type);
        const range = ["rangearea", "rangebar"].includes(type);
        const bubble = type === "bubble";
        const { argument, result } = seriesData(config, rows, financial || range || bubble);
        const normalized = [];
        result.forEach(definition => { try { return (rows.forEach((row, index) => { try {
            if (definition.series !== null && String(get(row, config.seriesField)) !== definition.series) return;
            normalized.push({
                argument: argumentValue(config, row, index),
                series: definition.name,
                value: measure(config, row, definition.field),
                low: measure(config, row, config.lowValueField || definition.field),
                high: measure(config, row, config.highValueField || definition.field),
                open: measure(config, row, config.openValueField || definition.field),
                close: measure(config, row, config.closeValueField || definition.field),
                size: Math.max(0, measure(config, row, config.sizeField || definition.field))
            });
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:rows.forEach@508', __javascriptError); throw __javascriptError; }})); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:result.forEach@508', __javascriptError); throw __javascriptError; } });
        const commonSeriesSettings = { type, argumentField: "argument", label: { visible: !!config.showLabels } };
        if (financial) Object.assign(commonSeriesSettings, { openValueField: "open", highValueField: "high", lowValueField: "low", closeValueField: "close" });
        else if (range) Object.assign(commonSeriesSettings, { rangeValue1Field: "low", rangeValue2Field: "high" });
        else if (bubble) Object.assign(commonSeriesSettings, { valueField: "value", sizeField: "size" });
        else commonSeriesSettings.valueField = "value";
        return Object.assign(common(config), {
            dataSource: aggregateVisualPoints(config, normalized, !(financial || range || bubble)),
            commonSeriesSettings,
            seriesTemplate: { nameField: "series" },
            argumentAxis: argumentAxis(config)
        });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:chartOptions@501', __javascriptError); throw __javascriptError; }}

    function polarOptions(config, rows) { try {
        const typeMap = { Line: "line", Area: "area", Bar: "bar", StackedBar: "stackedbar", Scatter: "scatter" };
        const type = typeMap[config.polarStyle] || "line";
        const { argument, result } = seriesData(config, rows, false);
        const normalized = [];
        result.forEach(definition => { try { return (rows.forEach((row, index) => { try {
            if (definition.series !== null && String(get(row, config.seriesField)) !== definition.series) return;
            normalized.push({
                argument: argumentValue(config, row, index),
                series: definition.name,
                value: measure(config, row, definition.field)
            });
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:rows.forEach@539', __javascriptError); throw __javascriptError; }})); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:result.forEach@539', __javascriptError); throw __javascriptError; } });
        return Object.assign(common(config), {
            dataSource: aggregateVisualPoints(config, normalized),
            commonSeriesSettings: {
                type,
                argumentField: "argument",
                valueField: "value",
                label: { visible: !!config.showLabels }
            },
            seriesTemplate: { nameField: "series" }
        });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:polarOptions@534', __javascriptError); throw __javascriptError; }}

    function renderWidget(element, config, rows) { try {
        config.__element = element;
        if (!window.jQuery || !window.DevExpress) { fallback(element, config, rows, "DevExtreme browser assets are not loaded."); return null; }
        const $element = window.jQuery(element);
        const kind = String(config.kind || "CartesianChart");
        const valueField = config.valueFields?.[0] || config.highValueField || config.closeValueField || "Value";
        const rawPoints = rows.map((row, index) => { try { return (({ argument: argumentValue(config, row, index), value: measure(config, row, valueField) })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:rows.map@565', __javascriptError); throw __javascriptError; } });
        const points = aggregateVisualPoints(config, rawPoints);
        let plugin, options;
        switch (kind) {
            case "CartesianChart": plugin = "dxChart"; options = chartOptions(config, rows); break;
            case "PieChart":
                plugin = "dxPieChart";
                options = Object.assign(common(config), { dataSource: points, type: config.pieStyle === "Doughnut" ? "doughnut" : "pie", series: [{ argumentField: "argument", valueField: "value", label: { visible: !!config.showLabels } }] });
                break;
            case "PolarChart": plugin = "dxPolarChart"; options = polarOptions(config, rows); break;
            case "Sparkline": {
                plugin = "dxSparkline";
                const typeMap = { Line: "line", Spline: "spline", StepLine: "stepline", Area: "area", SplineArea: "splinearea", StepArea: "steparea", Bar: "bar", WinLoss: "winloss" };
                options = { dataSource: points, argumentField: "argument", valueField: "value", type: typeMap[config.sparklineStyle] || "line", tooltip: visualTooltip(config), size: elementSize(config) };
                break;
            }
            case "BarGauge":
                plugin = "dxBarGauge"; options = Object.assign(common(config), { startValue: config.minimumValue, endValue: config.maximumValue, values: points.map(point => { try { return (point.value); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:points.map@582', __javascriptError); throw __javascriptError; } }) }); break;
            case "CircularGauge":
                plugin = "dxCircularGauge"; options = Object.assign(common(config), { value: points[0]?.value || 0, subvalues: points.slice(1).map(point => { try { return (point.value); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:points.slice(1).map@584', __javascriptError); throw __javascriptError; } }), scale: { startValue: config.minimumValue, endValue: config.maximumValue }, title: config.showTitle ? { text: config.title || "" } : undefined }); break;
            case "LinearGauge":
                plugin = "dxLinearGauge"; options = Object.assign(common(config), { value: points[0]?.value || 0, subvalues: points.slice(1).map(point => { try { return (point.value); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:points.slice(1).map@586', __javascriptError); throw __javascriptError; } }), scale: { startValue: config.minimumValue, endValue: config.maximumValue }, title: config.showTitle ? { text: config.title || "" } : undefined }); break;
            case "RangeSelector":
                plugin = "dxRangeSelector"; options = { dataSource: points, chart: { series: { argumentField: "argument", valueField: "value", type: mapCartesianType(config.cartesianStyle) } }, scale: rangeScale(config), size: elementSize(config), title: config.showTitle ? config.title : undefined }; break;
            case "Sankey":
                plugin = "dxSankey"; options = Object.assign(common(config), { dataSource: rows.map(row => { try { return (({ source: String(get(row, config.argumentField)), target: String(get(row, config.targetField)), weight: measure(config, row, valueField) })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:rows.map@590', __javascriptError); throw __javascriptError; } }), sourceField: "source", targetField: "target", weightField: "weight", label: { visible: !!config.showLabels } }); break;
            case "Funnel":
            case "Pyramid":
                plugin = "dxFunnel"; options = Object.assign(common(config), { dataSource: points, argumentField: "argument", valueField: "value", inverted: kind === "Pyramid", label: { visible: !!config.showLabels } }); break;
            case "TreeMap":
                plugin = "dxTreeMap"; options = Object.assign(common(config), { dataSource: rows.map((row, index) => { try { return (({ id: String(get(row, config.argumentField) || index), parent: String(get(row, config.parentField)), label: String(get(row, config.argumentField)), value: measure(config, row, valueField) })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:rows.map@595', __javascriptError); throw __javascriptError; } }), idField: "id", parentField: "parent", labelField: "label", valueField: "value", tooltip: visualTooltip(config) }); break;
            case "DataTable":
                plugin = "dxDataGrid"; options = { dataSource: rows, showBorders: true, columnAutoWidth: true, filterRow: { visible: !!config.tableShowFilterRow }, paging: { pageSize: Math.max(1, config.rowLimit || 12) }, pager: { visible: false }, height: "100%", width: "100%" }; break;
            case "KpiProgress": {
                const current = points[0]?.value || 0;
                element.innerHTML = `<div class="ps-kpi"><span>${escapeHtml(config.title || "KPI")}</span><strong>${current.toLocaleString()}</strong><progress min="${number(config.minimumValue)}" max="${number(config.maximumValue) || 100}" value="${current}"></progress></div>`;
                return null;
            }
            default: fallback(element, config, rows, `Unknown visualization type: ${kind}`); return null;
        }
        try {
            $element[plugin](options);
            return $element[plugin]("instance");
        } catch (error) {
            fallback(element, config, rows, error?.message || String(error));
            return null;
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:renderWidget@559', __javascriptError); throw __javascriptError; }}

    function escapeHtml(value) { try {
        return String(value ?? "").replace(/[&<>"']/g, character => { try { return (({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[character]); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:String(value ?? "").replace@615', __javascriptError); throw __javascriptError; } });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:escapeHtml@614', __javascriptError); throw __javascriptError; }}

    async function render(element, rawConfig, options) { try {
        element = visualRoot(element) || element;
        if (!element) return;
        const config = decodeConfig(rawConfig || element.dataset.psVisualConfig);
        if (!config) return;
        const prior = states.get(element);
        if (prior?.timer) clearInterval(prior.timer);
        clearVisualInteraction(prior);
        try { prior?.instance?.dispose?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:suppressed-catch@626', __caughtJavaScriptError);  }
        element.replaceChildren();
        let rows = Array.isArray(config.rows) ? config.rows : [];
        let error = "";
        if (options?.fetchNow) {
            try { rows = await fetchRows(config) || rows; }
            catch (exception) { error = exception?.message || String(exception); }
        }
        const instance = renderWidget(element, config, rows);
        const state = { config, rows, instance, timer: null, error };
        states.set(element, state);
        const interval = Number(config.live?.refreshIntervalSeconds || 0);
        if (options?.polling !== false && config.live?.enabled && config.live?.allowExportedHtmlFetch && interval > 0) {
            state.timer = setInterval(async () => { try {
                try {
                    const nextRows = await fetchRows(config);
                    if (!nextRows) return;
                    state.rows = nextRows;
                    try { state.instance?.dispose?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:suppressed-catch@644', __caughtJavaScriptError);  }
                    element.replaceChildren();
                    state.instance = renderWidget(element, config, nextRows);
                } catch (exception) {
                    if (!config.live.useSnapshotOnFailure) fallback(element, config, state.rows, exception?.message || String(exception));
                }
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:setInterval@639', __javascriptError); throw __javascriptError; }}, Math.max(1, interval) * 1000);
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:render@618', __javascriptError); throw __javascriptError; }}

    async function refreshAll(root, options) { try {
        const elements = [...(root || document).querySelectorAll("[data-ps-visual-config]")];
        await Promise.all(elements.map(element => { try { return (render(element, element.dataset.psVisualConfig, { polling: options?.polling, fetchNow: true })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:elements.map@656', __javascriptError); throw __javascriptError; } }));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:refreshAll@654', __javascriptError); throw __javascriptError; }}

    function start(root, options) { try {
        bindPointerOwnership();
        const scope = root || document;
        scope.querySelectorAll("[data-ps-visual-config]").forEach(element => { try { return (render(element, element.dataset.psVisualConfig, { polling: options?.polling !== false, fetchNow: options?.fetchNow !== false })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:callback:scope.querySelectorAll("[data-ps-visual-config]").forEach@662', __javascriptError); throw __javascriptError; } });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:start@659', __javascriptError); throw __javascriptError; }}

    window.PublisherStudioLiveDataRuntime = {
        renderVisualById(id, config) { try { bindPointerOwnership(); return render(document.getElementById(id), config, { polling: false, fetchNow: false });  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:renderVisualById@666', __javascriptError); throw __javascriptError; }},
        disposeById(id) { try { const element = document.getElementById(id); if (element) disposeWidget(element);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:disposeById@667', __javascriptError); throw __javascriptError; }},
        render,
        start,
        refreshAll,
        dispose(root) { try {
            if (!root) return;
            const elements = root.matches?.("[data-ps-visual-config]") ? [root] : [...root.querySelectorAll?.("[data-ps-visual-config]") || []];
            elements.forEach(disposeWidget);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:dispose@671', __javascriptError); throw __javascriptError; }},
        setDataBaseUrl(value) { try {
            window.PublisherStudioDataBaseUrl = value || "";
            try {
                if (value) localStorage.setItem("PublisherStudioDataBaseUrl", value);
                else localStorage.removeItem("PublisherStudioDataBaseUrl");
            } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:suppressed-catch@681', __caughtJavaScriptError);  }
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:setDataBaseUrl@676', __javascriptError); throw __javascriptError; }}
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/liveDataInterop.js:FunctionExpression@2', __javascriptError); throw __javascriptError; }})();

// Guard exported browser namespaces after the file has initialized.
publisherStudioDiagnostics.guardObject("PublisherStudioLiveDataRuntime", window.PublisherStudioLiveDataRuntime);
