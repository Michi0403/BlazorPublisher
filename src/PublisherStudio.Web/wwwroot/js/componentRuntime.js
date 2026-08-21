// javascript-diagnostics: guarded
var publisherStudioDiagnostics = globalThis.publisherStudioJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`PublisherStudio JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("PublisherStudio fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`PublisherStudio fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
(() => { try {
    "use strict";

    const states = new Map();
    const pluginNames = {
        DataGrid: "dxDataGrid",
        TreeList: "dxTreeList",
        Scheduler: "dxScheduler",
        Form: "dxForm",
        TextBox: "dxTextBox",
        TextArea: "dxTextArea",
        NumberBox: "dxNumberBox",
        DateBox: "dxDateBox",
        CheckBox: "dxCheckBox",
        SelectBox: "dxSelectBox",
        TagBox: "dxTagBox",
        Gallery: "dxGallery",
        TileView: "dxTileView",
        Menu: "dxMenu",
        ContextMenu: "dxContextMenu",
        TabPanel: "dxTabPanel",
        MultiView: "dxMultiView",
        Splitter: "dxSplitter",
        ScrollView: "dxScrollView",
        PivotGrid: "dxPivotGrid",
        Map: "dxMap",
        VectorMap: "dxVectorMap",
        Chat: "dxChat",
        Button: "dxButton"
    };

    const lower = value => { try { return (String(value ?? "").replace(/[^a-z0-9]/gi, "").toLowerCase()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:lower@33', __javascriptError); throw __javascriptError; } };
    const bool = value => { try { return (value === true || String(value).toLowerCase() === "true"); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:bool@34', __javascriptError); throw __javascriptError; } };
    const number = (value, fallback = 0) => { try {
        const parsed = Number(value);
        return Number.isFinite(parsed) ? parsed : fallback;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:number@35', __javascriptError); throw __javascriptError; }};
    const validDate = value => { try {
        if (value === null || value === undefined || value === "") return null;
        if (value instanceof Date) return Number.isFinite(value.getTime()) ? value : null;
        const parsed = new Date(value);
        return Number.isFinite(parsed.getTime()) ? parsed : null;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:validDate@39', __javascriptError); throw __javascriptError; }};
    const componentOrientation = (config, fallback = "horizontal") => { try {
        const value = lower(config?.orientation);
        if (value === "vertical") return "vertical";
        if (value === "horizontal") return "horizontal";
        return fallback;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:componentOrientation@45', __javascriptError); throw __javascriptError; }};

    const isMapKind = config => { try { return (["map", "vectormap"].includes(lower(config?.kind))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:isMapKind@52', __javascriptError); throw __javascriptError; } };
    const designerMapContentEnabled = config => { try { return (!config?.designerMode || lower(config?.designerInteractionMode) === "content"); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:designerMapContentEnabled@53', __javascriptError); throw __javascriptError; } };
    const mapProviders = new Set(["google", "googlestatic", "azure", "bing"]);
    const normalizedMapProvider = config => { try {
        const provider = lower(config?.mapProvider);
        return provider === "googlestatic" ? "googleStatic" : mapProviders.has(provider) ? provider : "";
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:normalizedMapProvider@55', __javascriptError); throw __javascriptError; }};
    const hasMapProviderConfiguration = config => { try { return (normalizedMapProvider(config) !== "" && String(config?.mapApiKey || "").trim() !== ""); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:hasMapProviderConfiguration@59', __javascriptError); throw __javascriptError; } };

    function commitDesignerMapViewport(element, config, delay = 420) { try {
        if (!element?.__psMapViewportSnapshot || element.__psMapGestureActive) return;
        if (element.__psMapViewportTimer) clearTimeout(element.__psMapViewportTimer);
        element.__psMapViewportTimer = setTimeout(() => { try {
            element.__psMapViewportTimer = null;
            const detail = element.__psMapViewportSnapshot;
            if (!detail || !element.isConnected || !element.__psMapUserGesture || element.__psMapGestureActive) return;
            config.mapCenterLongitude = detail.longitude;
            config.mapCenterLatitude = detail.latitude;
            config.mapZoom = detail.zoom;
            element.__psMapUserGesture = false;
            element.dispatchEvent(new CustomEvent("publisherstudio:map-viewport-changed", {
                bubbles: true,
                detail: { componentId: String(config.id || ""), ...detail }
            }));
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:setTimeout@64', __javascriptError); throw __javascriptError; }}, delay);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:commitDesignerMapViewport@61', __javascriptError); throw __javascriptError; }}

    function scheduleDesignerMapViewport(element, config, center, zoom) { try {
        if (!element || !config?.designerMode || !isMapKind(config) || !designerMapContentEnabled(config)) return;
        if (!element.__psMapReady || !element.__psMapUserGesture) return;
        const longitude = Array.isArray(center)
            ? Number(center[0])
            : Number(center?.lng ?? center?.longitude);
        const latitude = Array.isArray(center)
            ? Number(center[1])
            : Number(center?.lat ?? center?.latitude);
        const zoomValue = Number(zoom);
        if (!Number.isFinite(longitude) || !Number.isFinite(latitude) || !Number.isFinite(zoomValue)) return;
        if (Math.abs(number(config.mapCenterLongitude) - longitude) < .000001 &&
            Math.abs(number(config.mapCenterLatitude) - latitude) < .000001 &&
            Math.abs(number(config.mapZoom, 1) - zoomValue) < .0001) return;

        element.__psMapViewportSnapshot = { longitude, latitude, zoom: zoomValue };
        if (!element.__psMapGestureActive) commitDesignerMapViewport(element, config);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:scheduleDesignerMapViewport@79', __javascriptError); throw __javascriptError; }}

    function renderMapConfigurationPlaceholder(element, config) { try {
        const provider = normalizedMapProvider(config);
        const reason = provider ? "Enter an API key before this provider can be loaded." : "Select a map provider and enter its API key.";
        element.classList.add("ps-map-configuration-required");
        element.innerHTML = `<div class="ps-component-map-placeholder"><span class="dx-icon dx-icon-map" aria-hidden="true"></span><strong>${escapeHtml(config.title || "Map")}</strong><p>${escapeHtml(reason)}</p><small>No external map request was made. Use Vector Map for the bundled keyless map.</small></div>`;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:renderMapConfigurationPlaceholder@98', __javascriptError); throw __javascriptError; }}

    function normalizeDateRows(config, rows) { try {
        if (!Array.isArray(rows)) return [];
        const dateFields = new Set((config.fields || [])
            .filter(column => { try { return (lower(column?.valueKind) === "datetime"); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:(config.fields || []) .filter@108', __javascriptError); throw __javascriptError; } })
            .map(column => { try { return (String(column.dataField || "").trim()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:(config.fields || []) .filter(column => lower(column?.valueKind) === "@109', __javascriptError); throw __javascriptError; } })
            .filter(Boolean));
        for (const [name, valueKind] of Object.entries(config.columnKinds || {})) {
            if (lower(valueKind) === "datetime" && String(name).trim()) dateFields.add(String(name).trim());
        }
        if (String(config.kind || "") === "Scheduler") {
            for (const name of [config.startDateField || "startDate", config.endDateField || "endDate"]) if (name) dateFields.add(String(name));
        }
        if (!dateFields.size) return rows;
        const scheduler = String(config.kind || "") === "Scheduler";
        const startField = String(config.startDateField || "startDate");
        const endField = String(config.endDateField || "endDate");
        return rows.map(source => { try {
            if (!source || typeof source !== "object") return source;
            const row = { ...source };
            for (const name of dateFields) {
                const key = Object.keys(row).find(candidate => { try { return (lower(candidate) === lower(name)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:Object.keys(row).find@125', __javascriptError); throw __javascriptError; } });
                if (!key) continue;
                row[key] = validDate(row[key]);
            }
            if (scheduler) {
                const startKey = Object.keys(row).find(candidate => { try { return (lower(candidate) === lower(startField)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:Object.keys(row).find@130', __javascriptError); throw __javascriptError; } });
                const endKey = Object.keys(row).find(candidate => { try { return (lower(candidate) === lower(endField)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:Object.keys(row).find@131', __javascriptError); throw __javascriptError; } });
                if (!startKey || !row[startKey]) return null;
                if (endKey && !row[endKey]) row[endKey] = new Date(row[startKey].getTime() + 60 * 60 * 1000);
            }
            return row;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:rows.map@121', __javascriptError); throw __javascriptError; }}).filter(row => { try { return (row !== null); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:rows.map(source => { if (!source || typeof source !== "object") return@136', __javascriptError); throw __javascriptError; } });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:normalizeDateRows@105', __javascriptError); throw __javascriptError; }}
    const escapeHtml = value => { try { return (String(value ?? "").replace(/[&<>"']/g, character => { try { return (({
        "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
    })[character]); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:String(value ?? "").replace@138', __javascriptError); throw __javascriptError; } })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:escapeHtml@138', __javascriptError); throw __javascriptError; } };

    function decodeConfig(value) { try {
        if (!value) return null;
        if (typeof value === "object") return value;
        const source = String(value);
        try {
            const binary = atob(source);
            const bytes = Uint8Array.from(binary, character => { try { return (character.charCodeAt(0)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:Uint8Array.from@148', __javascriptError); throw __javascriptError; } });
            return JSON.parse(new TextDecoder().decode(bytes));
        } catch {
            try { return JSON.parse(source); } catch { return null; }
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:decodeConfig@142', __javascriptError); throw __javascriptError; }}

    function clone(value) { try {
        if (value === undefined) return undefined;
        if (typeof structuredClone === "function") {
            try { return structuredClone(value); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:suppressed-catch@158', __caughtJavaScriptError);  }
        }
        return JSON.parse(JSON.stringify(value));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:clone@155', __javascriptError); throw __javascriptError; }}

    function deepMerge(target, source) { try {
        if (!source || typeof source !== "object" || Array.isArray(source)) return target;
        for (const [key, value] of Object.entries(source)) {
            if (value && typeof value === "object" && !Array.isArray(value)) {
                const current = target[key] && typeof target[key] === "object" && !Array.isArray(target[key]) ? target[key] : {};
                target[key] = deepMerge(current, value);
            } else target[key] = value;
        }
        return target;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:deepMerge@163', __javascriptError); throw __javascriptError; }}

    function advancedOptions(config) { try {
        try {
            const value = JSON.parse(config.advancedOptionsJson || "{}");
            return value && typeof value === "object" && !Array.isArray(value) ? value : {};
        } catch { return {}; }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:advancedOptions@174', __javascriptError); throw __javascriptError; }}

    function dataBaseUrl() { try {
        const query = new URLSearchParams(location.search).get("publisherApi");
        let stored = "";
        try { stored = localStorage.getItem("PublisherStudioDataBaseUrl") || ""; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:suppressed-catch@184', __caughtJavaScriptError);  }
        const configured = query || window.PublisherStudioDataBaseUrl || stored;
        if (configured) return String(configured).replace(/\/$/, "");
        return /^https?:$/.test(location.protocol) ? location.origin : "";
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:dataBaseUrl@181', __javascriptError); throw __javascriptError; }}

    function resolveUrl(value) { try {
        const url = String(value || "").trim();
        if (!url) return "";
        try { return new URL(url).toString(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:suppressed-catch@193', __caughtJavaScriptError);  }
        const base = dataBaseUrl();
        if (!base) return "";
        try { return new URL(url.replace(/^\//, ""), base + "/").toString(); } catch { return ""; }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:resolveUrl@190', __javascriptError); throw __javascriptError; }}

    function headersObject(headers) { try {
        const result = {};
        for (const header of headers || []) {
            const name = String(header.name || "").trim();
            if (name) result[name] = String(header.value ?? "");
        }
        return result;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:headersObject@199', __javascriptError); throw __javascriptError; }}

    function jsonPath(value, path) { try {
        if (!path) return value;
        return String(path).split(".").filter(Boolean).reduce((current, segment) => { try {
            if (current == null) return undefined;
            if (Array.isArray(current) && /^\d+$/.test(segment)) return current[Number(segment)];
            if (typeof current !== "object") return undefined;
            if (Object.prototype.hasOwnProperty.call(current, segment)) return current[segment];
            const key = Object.keys(current).find(candidate => { try { return (candidate.toLowerCase() === segment.toLowerCase()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:Object.keys(current).find@215', __javascriptError); throw __javascriptError; } });
            return key ? current[key] : undefined;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:String(path).split(".").filter(Boolean).reduce@210', __javascriptError); throw __javascriptError; }}, value);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:jsonPath@208', __javascriptError); throw __javascriptError; }}

    function unwrapJson(value) { try {
        let current = value;
        for (let index = 0; index < 3 && typeof current === "string"; index++) {
            const trimmed = current.trim();
            if (!trimmed.startsWith("{") && !trimmed.startsWith("[")) break;
            try { current = JSON.parse(trimmed); } catch { break; }
        }
        return current;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:unwrapJson@220', __javascriptError); throw __javascriptError; }}

    function normalizeRows(value, path = "") { try {
        let current = unwrapJson(jsonPath(value, path));
        if (Array.isArray(current)) return current;
        if (!current || typeof current !== "object") return [];
        for (const name of ["data", "items", "results", "records", "rows", "value"]) {
            const key = Object.keys(current).find(candidate => { try { return (candidate.toLowerCase() === name); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:Object.keys(current).find@235', __javascriptError); throw __javascriptError; } });
            if (!key) continue;
            const nested = unwrapJson(current[key]);
            if (Array.isArray(nested)) return nested;
            if (nested && typeof nested === "object") {
                const rows = normalizeRows(nested);
                if (rows.length) return rows;
            }
        }
        const arrays = Object.values(current).filter(Array.isArray);
        if (arrays.length === 1) return arrays[0];
        return [current];
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:normalizeRows@230', __javascriptError); throw __javascriptError; }}

    function appendLoadOptions(url, loadOptions) { try {
        const result = new URL(url);
        const keys = ["filter", "group", "groupSummary", "parentIds", "requireGroupCount", "requireTotalCount", "searchExpr", "searchOperation", "searchValue", "select", "sort", "skip", "take", "totalSummary", "userData"];
        for (const key of keys) {
            const value = loadOptions?.[key];
            if (value === undefined || value === null || value === "") continue;
            result.searchParams.set(key, typeof value === "string" ? value : JSON.stringify(value));
        }
        return result.toString();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:appendLoadOptions@249', __javascriptError); throw __javascriptError; }}

    async function readResponse(response, path = "") { try {
        const text = await response.text();
        if (!response.ok) throw new Error(`Endpoint returned ${response.status} ${response.statusText}.`);
        if (!text.trim()) return { raw: null, rows: [] };
        const contentType = String(response.headers.get("content-type") || "").toLowerCase();
        if (contentType.includes("json") || /^[\s]*[\[{\"]/.test(text)) {
            const value = unwrapJson(JSON.parse(text));
            return { raw: value, rows: normalizeRows(value, path) };
        }
        return { raw: text, rows: [{ Value: text }] };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:readResponse@260', __javascriptError); throw __javascriptError; }}

    async function fetchDataObjectLive(live) { try {
        if (!live?.enabled || !live.allowExportedHtmlFetch) return null;
        const monolithUrl = resolveUrl(live.monolithRowsUrl);
        if (monolithUrl) {
            const response = await fetch(monolithUrl, { cache: "no-store" });
            if (response.ok) {
                const value = await response.json();
                if (Array.isArray(value)) return value;
            } else if (lower(live.transport) === "webhook") {
                throw new Error(`PublisherStudio data endpoint returned ${response.status} ${response.statusText}.`);
            }
        }
        if (lower(live.transport) === "webhook") return null;
        const url = resolveUrl(live.url);
        if (!url) return null;
        const method = String(live.method || "GET").toUpperCase();
        const response = await fetch(url, {
            method,
            headers: headersObject(live.headers),
            body: ["GET", "HEAD"].includes(method) ? undefined : (live.body || ""),
            cache: "no-store"
        });
        return (await readResponse(response, live.jsonPath)).rows;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:fetchDataObjectLive@272', __javascriptError); throw __javascriptError; }}

    function endpointWithKey(url, key, appendKey) { try {
        const resolved = resolveUrl(url);
        if (!resolved || !appendKey || key === undefined || key === null || key === "") return resolved;
        return resolved.replace(/\/$/, "") + "/" + encodeURIComponent(typeof key === "object" ? JSON.stringify(key) : String(key));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:endpointWithKey@297', __javascriptError); throw __javascriptError; }}

    function requestBody(values) { try {
        return JSON.stringify(values ?? {});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:requestBody@303', __javascriptError); throw __javascriptError; }}

    function createRestStore(config, rows) { try {
        const connection = config.connection || {};
        const key = connection.keyField || config.keyField || "id";
        const rawMode = lower(connection.processingMode) !== "remote";
        const storeOptions = {
            key,
            loadMode: rawMode ? "raw" : "processed",
            cacheRawData: false,
            async load(loadOptions) { try {
                if (connection.allowLoad === false) return rows;
                const liveRows = await fetchDataObjectLive(connection.dataObjectLive);
                if (liveRows) return normalizeDateRows(config, liveRows);
                if (lower(connection.mode) !== "rest") return rows;
                const base = resolveUrl(connection.url);
                if (!base) return rows;
                const url = rawMode ? base : appendLoadOptions(base, loadOptions);
                const method = String(connection.loadMethod || "GET").toUpperCase();
                const response = await fetch(url, {
                    method,
                    headers: headersObject(connection.headers),
                    body: ["GET", "HEAD"].includes(method) ? undefined : (connection.loadBody || ""),
                    credentials: connection.withCredentials ? "include" : "same-origin",
                    cache: "no-store"
                });
                const result = await readResponse(response, connection.jsonPath);
                if (!rawMode && result.raw && typeof result.raw === "object" && !Array.isArray(result.raw)) {
                    if (Array.isArray(result.raw.data)) return result.raw;
                    const normalized = normalizeDateRows(config, result.rows);
                    return { data: normalized, totalCount: number(result.raw.totalCount, normalized.length), summary: result.raw.summary, groupCount: result.raw.groupCount };
                }
                return normalizeDateRows(config, result.rows);
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:load@315', __javascriptError); throw __javascriptError; }}
        };
        if (connection.allowInsert) storeOptions.insert = async values => { try {
            const url = resolveUrl(connection.insertUrl || connection.url);
            if (!url) throw new Error("Insert endpoint is not configured.");
            const response = await fetch(url, {
                method: String(connection.insertMethod || "POST").toUpperCase(),
                headers: { "Content-Type": "application/json", ...headersObject(connection.headers) },
                body: requestBody(values),
                credentials: connection.withCredentials ? "include" : "same-origin"
            });
            const result = await readResponse(response, connection.jsonPath);
            return result.rows[0] || values;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:storeOptions.insert@340', __javascriptError); throw __javascriptError; }};
        if (connection.allowUpdate) storeOptions.update = async (itemKey, values) => { try {
            const url = endpointWithKey(connection.updateUrl || connection.url, itemKey, connection.appendKeyToWriteUrl !== false);
            if (!url) throw new Error("Update endpoint is not configured.");
            const response = await fetch(url, {
                method: String(connection.updateMethod || "PUT").toUpperCase(),
                headers: { "Content-Type": "application/json", ...headersObject(connection.headers) },
                body: requestBody(values),
                credentials: connection.withCredentials ? "include" : "same-origin"
            });
            if (!response.ok) throw new Error(`Update endpoint returned ${response.status} ${response.statusText}.`);
            return values;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:storeOptions.update@352', __javascriptError); throw __javascriptError; }};
        if (connection.allowDelete) storeOptions.remove = async itemKey => { try {
            const url = endpointWithKey(connection.deleteUrl || connection.url, itemKey, connection.appendKeyToWriteUrl !== false);
            if (!url) throw new Error("Delete endpoint is not configured.");
            const response = await fetch(url, {
                method: String(connection.deleteMethod || "DELETE").toUpperCase(),
                headers: headersObject(connection.headers),
                credentials: connection.withCredentials ? "include" : "same-origin"
            });
            if (!response.ok) throw new Error(`Delete endpoint returned ${response.status} ${response.statusText}.`);
            return itemKey;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:storeOptions.remove@364', __javascriptError); throw __javascriptError; }};
        return new DevExpress.data.CustomStore(storeOptions);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:createRestStore@307', __javascriptError); throw __javascriptError; }}

    function createODataStore(config) { try {
        const connection = config.connection || {};
        const url = resolveUrl(connection.url);
        if (!url) return null;
        const headers = headersObject(connection.headers);
        return new DevExpress.data.ODataStore({
            url,
            key: connection.keyField || config.keyField || "id",
            keyType: connection.keyType || "Int32",
            version: number(connection.oDataVersion, 4),
            beforeSend(request) { try {
                request.headers = { ...(request.headers || {}), ...headers };
                request.withCredentials = !!connection.withCredentials;
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:beforeSend@388', __javascriptError); throw __javascriptError; }},
            errorHandler(error) { try { showError(error?.message || String(error));  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:errorHandler@392', __javascriptError); throw __javascriptError; }}
        });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:createODataStore@378', __javascriptError); throw __javascriptError; }}

    function createData(config) { try {
        const rows = Array.isArray(config.rows) ? clone(config.rows) : [];
        const connection = config.connection || {};
        const mode = lower(connection.mode);
        if (!window.DevExpress?.data) return { dataSource: rows, store: null };
        if (mode === "odata") {
            if (connection.allowLoad === false) return { dataSource: rows, store: null };
            const store = createODataStore(config);
            return { dataSource: store || rows, store };
        }
        if (mode === "rest" || connection.dataObjectLive?.enabled) {
            const store = createRestStore(config, rows);
            return { dataSource: store, store };
        }
        const key = connection.keyField || config.keyField;
        if (key && rows.every(row => { try { return (row && Object.prototype.hasOwnProperty.call(row, key)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:rows.every@411', __javascriptError); throw __javascriptError; } })) {
            const store = new DevExpress.data.ArrayStore({ key, data: rows });
            return { dataSource: store, store };
        }
        return { dataSource: rows, store: null };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:createData@396', __javascriptError); throw __javascriptError; }}

    async function materializeRows(config, data) { try {
        const mode = lower(config.connection?.mode);
        const requiresBrowserLoad = mode === "rest" || mode === "odata" || config.connection?.dataObjectLive?.enabled;
        if (!requiresBrowserLoad) return Array.isArray(config.rows) ? clone(config.rows) : [];
        const source = data?.store || data?.dataSource;
        if (!source?.load) return Array.isArray(config.rows) ? clone(config.rows) : [];
        const loaded = await source.load({ take: Math.max(1, number(config.pageSize, 100)) });
        if (Array.isArray(loaded)) return loaded;
        if (Array.isArray(loaded?.data)) return loaded.data;
        return normalizeRows(loaded, config.connection?.jsonPath || "");
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:materializeRows@418', __javascriptError); throw __javascriptError; }}

    function menuItems(config, rows) { try {
        const manual = lower(config.menuSourceMode) === "manualitems";
        const values = manual ? clone(config.menuItems || []) : (Array.isArray(rows) ? rows : []);
        const keyField = manual ? "id" : (config.keyField || "id");
        const parentField = manual ? "parentId" : config.parentField;
        const visible = values.filter(item => { try { return (item?.visible !== false); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:values.filter@435', __javascriptError); throw __javascriptError; } });
        return parentField && visible.some(row => { try { return (valueFor(row, parentField) !== undefined && valueFor(row, parentField) !== null && String(valueFor(row, parentField)).trim() !== ""); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:visible.some@436', __javascriptError); throw __javascriptError; } })
            ? hierarchy(visible, keyField, parentField)
            : visible;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:menuItems@430', __javascriptError); throw __javascriptError; }}

    function renderBasicMenu(element, config, rows) { try {
        const root = document.createElement("div");
        root.className = `ps-basic-menu ps-basic-menu-${lower(config.orientation) === "vertical" ? "vertical" : "horizontal"}`;
        const currentRows = Array.isArray(rows) ? rows : [];
        let currentItems = menuItems(config, currentRows);
        const renderItems = (items, parent) => { try {
            for (const item of items || []) {
                const wrapper = document.createElement("div");
                wrapper.className = "ps-basic-menu-item";
                const button = document.createElement("button");
                button.type = "button";
                button.disabled = item?.disabled === true || item?.enabled === false;
                button.textContent = String(item?.text ?? item?.[config.displayField || config.textField || "text"] ?? "Menu item");
                button.addEventListener("click", event => { try {
                    event.stopPropagation();
                    let actions = actionsFor(config, "ItemClick");
                    if (!actions.length) actions = [{ trigger: "ItemClick", action: "Navigate", openInNewWindow: true }];
                    void executeActions(config, actions, eventContext(config, null, currentRows, { itemData: item, event }, item));
                 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:button.addEventListener@454', __javascriptError); throw __javascriptError; }});
                wrapper.append(button);
                if (Array.isArray(item?.items) && item.items.length) {
                    const children = document.createElement("div");
                    children.className = "ps-basic-menu-children";
                    renderItems(item.items, children);
                    wrapper.append(children);
                }
                parent.append(wrapper);
            }
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:renderItems@446', __javascriptError); throw __javascriptError; }};
        const repaint = (items = currentItems) => { try {
            root.replaceChildren();
            renderItems(items, root);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:repaint@470', __javascriptError); throw __javascriptError; }};
        repaint();
        element.replaceChildren(root);
        return {
            dispose() { try { root.remove();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:dispose@477', __javascriptError); throw __javascriptError; }},
            repaint() { try { repaint();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:repaint@478', __javascriptError); throw __javascriptError; }},
            option(name, value) { try {
                if (name === "items" && Array.isArray(value)) {
                    currentItems = value;
                    repaint();
                }
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:option@479', __javascriptError); throw __javascriptError; }}
        };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:renderBasicMenu@441', __javascriptError); throw __javascriptError; }}

    function fieldType(field) { try {
        switch (lower(field?.valueKind)) {
            case "number": return "number";
            case "boolean": return "boolean";
            case "datetime": return "date";
            default: return "string";
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:fieldType@488', __javascriptError); throw __javascriptError; }}

    function editorName(field) { try {
        const explicit = String(field?.editor || "Auto");
        if (lower(explicit) !== "auto") return `dx${explicit}`;
        switch (fieldType(field)) {
            case "number": return "dxNumberBox";
            case "boolean": return "dxCheckBox";
            case "date": return "dxDateBox";
            default: return "dxTextBox";
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:editorName@497', __javascriptError); throw __javascriptError; }}

    function lookupOptions(field) { try {
        const lookup = field?.lookup;
        if (!Array.isArray(lookup?.rows)) return null;
        return {
            dataSource: lookup.rows,
            valueExpr: lookup.valueExpr || field.lookupDataField || field.dataField,
            displayExpr: lookup.displayExpr || field.lookupDisplayField || field.lookupDataField || field.dataField
        };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:lookupOptions@508', __javascriptError); throw __javascriptError; }}

    function primaryField(config) { try {
        const visible = (config.fields || []).find(field => { try { return (field.visible !== false); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:(config.fields || []).find@519', __javascriptError); throw __javascriptError; } });
        return config.valueField || visible?.dataField || "value";
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:primaryField@518', __javascriptError); throw __javascriptError; }}

    function configuredValue(config) { try {
        if (config.initialValue !== undefined && config.initialValue !== null && String(config.initialValue) !== "") return config.initialValue;
        const row = Array.isArray(config.rows) ? config.rows[0] : null;
        return row?.[primaryField(config)] ?? null;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:configuredValue@523', __javascriptError); throw __javascriptError; }}

    function columns(config) { try {
        return (config.fields || []).filter(field => { try { return (field.visible !== false); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:(config.fields || []).filter@530', __javascriptError); throw __javascriptError; } }).map(field => { try {
            const column = {
                dataField: field.dataField,
                caption: field.caption || field.dataField,
                dataType: fieldType(field),
                allowEditing: field.editable !== false,
                validationRules: field.required ? [{ type: "required" }] : undefined,
                width: number(field.width) > 0 ? number(field.width) : undefined,
                format: field.format || undefined,
                editorType: editorName(field)
            };
            const lookup = lookupOptions(field);
            if (lookup) column.lookup = lookup;
            return column;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:(config.fields || []).filter(field => field.visible !== false).map@530', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:columns@529', __javascriptError); throw __javascriptError; }}

    function editOptions(config) { try {
        const mode = lower(config.editMode);
        if (mode === "readonly") return { allowAdding: false, allowUpdating: false, allowDeleting: false };
        const connection = config.connection || {};
        return {
            mode: mode === "cell" ? "cell" : mode === "batch" ? "batch" : mode === "form" ? "form" : mode === "popup" ? "popup" : "row",
            allowAdding: !!connection.allowInsert,
            allowUpdating: !!connection.allowUpdate,
            allowDeleting: !!connection.allowDelete,
            useIcons: true
        };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:editOptions@547', __javascriptError); throw __javascriptError; }}

    function selectionOptions(config) { try {
        const mode = lower(config.selectionMode);
        return { mode: mode === "multiple" ? "multiple" : mode === "single" ? "single" : "none", showCheckBoxesMode: mode === "multiple" ? "onClick" : "none" };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:selectionOptions@560', __javascriptError); throw __javascriptError; }}

    function actionsFor(config, trigger) { try {
        const normalized = lower(trigger);
        return (config.actions || []).filter(action => { try { return (lower(action.trigger) === normalized && lower(action.action) !== "none"); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:(config.actions || []).filter@567', __javascriptError); throw __javascriptError; } });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:actionsFor@565', __javascriptError); throw __javascriptError; }}

    function actionFor(config, trigger) { try {
        return actionsFor(config, trigger)[0] || null;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:actionFor@570', __javascriptError); throw __javascriptError; }}

    function template(value, data) { try {
        return String(value || "").replace(/\{\{\s*([^}]+?)\s*\}\}/g, (_, field) => { try {
            const key = Object.keys(data || {}).find(candidate => { try { return (candidate.toLowerCase() === String(field).toLowerCase()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:Object.keys(data || {}).find@576', __javascriptError); throw __javascriptError; } });
            return key ? String(data[key] ?? "") : "";
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:String(value || "").replace@575', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:template@574', __javascriptError); throw __javascriptError; }}

    function valueFor(data, field) { try {
        if (!data || !field) return undefined;
        if (Object.prototype.hasOwnProperty.call(data, field)) return data[field];
        const key = Object.keys(data).find(candidate => { try { return (lower(candidate) === lower(field)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:Object.keys(data).find@584', __javascriptError); throw __javascriptError; } });
        return key ? data[key] : undefined;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:valueFor@581', __javascriptError); throw __javascriptError; }}

    function navigateToPage(pageId, config, context) { try {
        if (pageId === undefined || pageId === null || String(pageId).trim() === "") return false;
        if (config?.designerMode) {
            const editorSurface = !!context?.host?.closest?.("#publisher-page");
            window.dispatchEvent(new CustomEvent("publisherstudio:navigate", { detail: { pageId, componentId: config.id, editorSurface } }));
            return true;
        }
        const api = window.PublisherStudioNavigation || window.PublisherStudioPresentation;
        if (api?.goToPage) return api.goToPage(pageId);
        window.dispatchEvent(new CustomEvent("publisherstudio:navigate", { detail: { pageId, componentId: config?.id } }));
        return true;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:navigateToPage@588', __javascriptError); throw __javascriptError; }}

    function openComponentUrl(url, openInNewWindow, config, context) { try {
        const value = String(url || "").trim();
        if (!/^(https?:|mailto:)/i.test(value)) throw new Error("Only http, https and mailto links are allowed.");
        if (config?.designerMode) {
            const editorSurface = !!context?.host?.closest?.("#publisher-page");
            window.dispatchEvent(new CustomEvent("publisherstudio:open-url", { detail: { url: value, openInNewWindow: openInNewWindow !== false, componentId: config.id, editorSurface } }));
            return true;
        }
        window.open(value, openInNewWindow === false ? "_self" : "_blank", "noopener");
        return true;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:openComponentUrl@601', __javascriptError); throw __javascriptError; }}

    function showError(message) { try {
        if (window.DevExpress?.ui?.notify) window.DevExpress.ui.notify(String(message || "Unknown error"), "error", 4500);
        else console.error(message);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:showError@613', __javascriptError); throw __javascriptError; }}

    function showSuccess(message) { try {
        if (window.DevExpress?.ui?.notify) window.DevExpress.ui.notify(String(message || "Done"), "success", 2500);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:showSuccess@618', __javascriptError); throw __javascriptError; }}

    async function executeAction(config, action, context) { try {
        if (!action || lower(action.action) === "none") return;
        if (action.confirmationText && !window.confirm(template(action.confirmationText, context.data))) return;
        const kind = lower(action.action);
        try {
            if (kind === "nextpage") (window.PublisherStudioNavigation || window.PublisherStudioPresentation)?.next?.();
            else if (kind === "previouspage") (window.PublisherStudioNavigation || window.PublisherStudioPresentation)?.previous?.();
            else if (kind === "navigate") {
                const pageField = config.targetPageField || "targetPageId";
                const urlField = config.urlField || "url";
                const targetPage = action.targetPageId || valueFor(context.data, pageField) || valueFor(context.itemData, pageField);
                const url = template(action.url || valueFor(context.data, urlField) || valueFor(context.itemData, urlField) || "", context.data);
                const itemWindow = valueFor(context.itemData, "openInNewWindow") ?? valueFor(context.data, "openInNewWindow");
                if (targetPage !== undefined && targetPage !== null && String(targetPage).trim() !== "") navigateToPage(targetPage, config, context);
                else if (url) openComponentUrl(url, itemWindow === undefined ? action.openInNewWindow : itemWindow !== false, config, context);
            } else if (kind === "gotopage") {
                const field = config.targetPageField || "targetPageId";
                navigateToPage(action.targetPageId || valueFor(context.data, field) || valueFor(context.itemData, field), config, context);
            } else if (kind === "openurl") {
                const field = config.urlField || "url";
                const url = template(action.url || valueFor(context.data, field) || valueFor(context.itemData, field) || "", context.data);
                openComponentUrl(url, action.openInNewWindow, config, context);
            } else if (kind === "mailto") {
                const recipient = template(action.mailTo, context.data);
                const subject = encodeURIComponent(template(action.mailSubject, context.data));
                const body = encodeURIComponent(template(action.mailBody, context.data));
                location.href = `mailto:${encodeURIComponent(recipient).replace(/%40/g, "@")}?subject=${subject}&body=${body}`;
            } else if (kind === "refresh") {
                const state = context.host ? states.get(context.host) : null;
                if (state) await refreshState(context.host, state);
                else {
                    await context.dataSource?.reload?.();
                    await context.instance?.refresh?.();
                    context.instance?.repaint?.();
                }
            } else if (["showelement", "hideelement", "toggleelement"].includes(kind)) {
                const target = document.querySelector(`[data-element-id="${CSS.escape(String(action.targetElementId || ""))}"]`);
                if (!target) return;
                if (kind === "showelement") target.classList.remove("ps-action-hidden");
                else if (kind === "hideelement") target.classList.add("ps-action-hidden");
                else target.classList.toggle("ps-action-hidden");
            } else if (["setvalue", "applyfilter", "clearfilter"].includes(kind)) {
                const sourceElement = context.instance?.element?.()?.get?.(0) || context.instance?.element?.()?.[0] || context.instance?.element?.();
                const sourcePage = sourceElement?.closest?.('.print-page,#publisher-page');
                const selector = `[data-element-id="${CSS.escape(String(action.targetElementId || ""))}"]`;
                const targetRoot = sourcePage?.querySelector?.(selector) || document.querySelector(selector);
                const targetHost = targetRoot?.matches?.("[data-ps-component-config]") ? targetRoot : targetRoot?.querySelector?.("[data-ps-component-config]");
                const targetState = targetHost ? states.get(targetHost) : null;
                if (!targetState?.instance) throw new Error("The target component is not available on the current page.");
                const sourceField = action.sourceField || "";
                const rawValue = sourceField ? (context.data?.[sourceField] ?? context.itemData?.[sourceField] ?? context.value) : context.value ?? context.data;
                const valueData = { ...(context.data || {}), value: rawValue };
                const valueTemplate = action.valueTemplate || "{{value}}";
                const value = /^\{\{\s*value\s*\}\}$/i.test(valueTemplate) ? rawValue : template(valueTemplate, valueData);
                if (kind === "setvalue") {
                    const optionName = action.targetField || "value";
                    targetState.instance.option?.(optionName, value);
                } else {
                    const dataSource = targetState.instance.getDataSource?.() || targetState.dataSource;
                    if (!dataSource) throw new Error("The target component does not expose a data source.");
                    if (kind === "clearfilter") dataSource.filter?.(null);
                    else dataSource.filter?.([action.targetField || sourceField, "=", rawValue]);
                    await dataSource.reload?.();
                    targetState.instance.refresh?.();
                }
            } else if (kind === "submitrest") {
                const connection = config.connection || {};
                const payload = context.data || {};
                const explicitUrl = template(action.url || "", payload).trim();
                if (!explicitUrl) {
                    const keyField = connection.keyField || config.keyField || "id";
                    const key = payload?.[keyField];
                    const hasKey = key !== undefined && key !== null && String(key) !== "";
                    if (connection.allowUpdate && hasKey && typeof context.dataSource?.update === "function") {
                        const values = { ...payload };
                        delete values[keyField];
                        await context.dataSource.update(key, values);
                        await context.dataSource.reload?.();
                    } else if (connection.allowInsert && typeof context.dataSource?.insert === "function") {
                        await context.dataSource.insert(payload);
                        await context.dataSource.reload?.();
                    } else {
                        throw new Error("Enable insert or update for this form, or configure an explicit submit URL.");
                    }
                } else {
                    const url = resolveUrl(explicitUrl || connection.insertUrl || connection.url);
                    if (!url) throw new Error("Submit endpoint is not configured.");
                    const response = await fetch(url, {
                        method: String(connection.insertMethod || "POST").toUpperCase(),
                        headers: { "Content-Type": "application/json", ...headersObject(connection.headers) },
                        body: JSON.stringify(payload),
                        credentials: connection.withCredentials ? "include" : "same-origin"
                    });
                    if (!response.ok) throw new Error(`Submit endpoint returned ${response.status} ${response.statusText}.`);
                }
                showSuccess("Data submitted.");
            } else if (kind === "customscript") {
                if (!config.allowCustomScript) throw new Error("Custom script is disabled for this component.");
                const handler = new Function("context", `"use strict";\n${action.script || ""}`);
                const publicationRuntime = window.PublisherStudioPublicationRuntime;
                const scriptContext = Object.freeze({
                    ...context,
                    config: clone(config),
                    objects: publicationRuntime?.objects?.(context.host) || null,
                    publication: publicationRuntime?.publication || null
                });
                await handler(scriptContext);
            }
        } catch (error) {
            showError(error?.message || String(error));
            throw error;
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:executeAction@622', __javascriptError); throw __javascriptError; }}

    async function executeActions(config, actions, context) { try {
        for (const action of actions || []) await executeAction(config, action, context);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:executeActions@729', __javascriptError); throw __javascriptError; }}

    function eventContext(config, instance, dataSource, event, data) { try {
        const instanceElement = instance?.element?.()?.get?.(0) || instance?.element?.()?.[0] || instance?.element?.();
        const host = instanceElement?.closest?.("[data-ps-component-config]")
            || document.querySelector(`[data-ps-component-id="${CSS.escape(String(config.id || ""))}"]`);
        let eventData = data || event?.data || event?.itemData || event?.message || event?.appointmentData || event?.selectedRowsData?.[0] || null;
        if (!eventData && event && Object.prototype.hasOwnProperty.call(event, "value")) {
            eventData = { [primaryField(config)]: event.value, value: event.value };
        }
        if (!eventData) eventData = Array.isArray(config.rows) ? clone(config.rows[0] || {}) : {};
        return { config, instance, dataSource, host, event, data: eventData, itemData: event?.itemData || null, value: event?.value };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:eventContext@733', __javascriptError); throw __javascriptError; }}

    function bindCommonActions(config, options, dataSource) { try {
        const handlers = [
            ["ItemClick", "onItemClick"],
            ["SelectionChanged", "onSelectionChanged"],
            ["ValueChanged", "onValueChanged"],
            ["RowInserted", "onRowInserted"],
            ["RowUpdated", "onRowUpdated"],
            ["RowRemoved", "onRowRemoved"],
            ["AppointmentAdded", "onAppointmentAdded"],
            ["AppointmentUpdated", "onAppointmentUpdated"],
            ["AppointmentDeleted", "onAppointmentDeleted"],
            ["MessageEntered", "onMessageEntered"]
        ];
        for (const [trigger, eventName] of handlers) {
            let actions = actionsFor(config, trigger);
            if (!actions.length && trigger === "ItemClick" && ["menu", "contextmenu"].includes(lower(config.kind)))
                actions = [{ trigger: "ItemClick", action: "Navigate", openInNewWindow: true }];
            if (!actions.length) continue;
            const prior = options[eventName];
            options[eventName] = event => { try {
                prior?.(event);
                executeActions(config, actions, eventContext(config, event.component, dataSource, event));
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:options[eventName]@764', __javascriptError); throw __javascriptError; }};
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:bindCommonActions@745', __javascriptError); throw __javascriptError; }}

    function mediaKind(item, config, source) { try {
        const explicit = lower(valueFor(item, config.mediaKindField || "mediaType"));
        if (["image", "video", "audio"].includes(explicit)) return explicit;
        const mime = lower(valueFor(item, "mimeType"));
        if (mime.startsWith("video")) return "video";
        if (mime.startsWith("audio")) return "audio";
        if (mime.startsWith("image")) return "image";
        const value = String(source || "").split(/[?#]/, 1)[0].toLowerCase();
        if (/\.(mp4|webm|ogv|mov|m4v)$/.test(value)) return "video";
        if (/\.(mp3|wav|ogg|oga|m4a|aac|flac)$/.test(value)) return "audio";
        return "image";
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:mediaKind@771', __javascriptError); throw __javascriptError; }}

    function allowedMediaSource(value) { try {
        const source = String(value || "").trim();
        if (!source) return "";
        if (/^(data:|blob:|https?:|\/|\.\.?\/)/i.test(source)) return source;
        return "";
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:allowedMediaSource@784', __javascriptError); throw __javascriptError; }}

    function renderCard(item, config, element, tile = false) { try {
        const image = valueFor(item, config.imageField || "image");
        const source = allowedMediaSource(valueFor(item, config.mediaSourceField || "source") || image);
        const poster = allowedMediaSource(valueFor(item, config.mediaPosterField || "poster") || image);
        const title = valueFor(item, config.displayField || config.textField || "text") ?? valueFor(item, config.valueField || "value") ?? "";
        const altText = valueFor(item, config.mediaAltTextField || "altText") || title || "Media";
        const kind = mediaKind(item, config, source);
        const ignoredFields = [config.imageField, config.mediaSourceField, config.mediaPosterField, config.mediaKindField, config.mediaAltTextField, config.displayField, config.textField];
        const subtitleField = (config.fields || []).find(field => { try { return (field.visible !== false && !ignoredFields.includes(field.dataField)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:(config.fields || []).find@799', __javascriptError); throw __javascriptError; } })?.dataField;
        const subtitle = subtitleField ? valueFor(item, subtitleField) : "";
        const wrapper = document.createElement("article");
        wrapper.className = `${tile ? "ps-component-tile" : "ps-component-gallery-item"} ps-component-media-${kind}`;
        const mediaFrame = document.createElement("div");
        mediaFrame.className = "ps-component-media-frame";
        if (source && kind === "video") {
            const video = document.createElement("video");
            video.src = source;
            if (poster) video.poster = poster;
            video.preload = "metadata";
            video.controls = config.mediaShowControls !== false;
            video.autoplay = false;
            video.muted = config.mediaMuted !== false;
            video.loop = config.mediaLoop !== false;
            video.playsInline = true;
            video.setAttribute("aria-label", String(altText));
            mediaFrame.append(video);
        } else if (source && kind === "audio") {
            const audioVisual = document.createElement("div");
            audioVisual.className = "ps-component-audio-visual";
            audioVisual.innerHTML = '<span class="dx-icon dx-icon-music" aria-hidden="true"></span>';
            const audio = document.createElement("audio");
            audio.src = source;
            audio.preload = "metadata";
            audio.controls = config.mediaShowControls !== false;
            audio.autoplay = false;
            audio.muted = config.mediaMuted !== false;
            audio.loop = config.mediaLoop !== false;
            audio.setAttribute("aria-label", String(altText));
            audioVisual.append(audio);
            mediaFrame.append(audioVisual);
        } else if (source) {
            const img = document.createElement("img");
            img.src = source;
            img.alt = String(altText);
            img.loading = "lazy";
            mediaFrame.append(img);
        } else {
            const empty = document.createElement("div");
            empty.className = "ps-component-media-empty";
            empty.innerHTML = '<span class="dx-icon dx-icon-imagethumbnail" aria-hidden="true"></span><span>No media source</span>';
            mediaFrame.append(empty);
        }
        wrapper.append(mediaFrame);
        const body = document.createElement("div");
        body.className = "ps-component-media-caption";
        const strong = document.createElement("strong");
        strong.textContent = String(title ?? "");
        body.append(strong);
        if (subtitle !== undefined && subtitle !== null && subtitle !== "") {
            const small = document.createElement("small");
            small.textContent = String(subtitle);
            body.append(small);
        }
        wrapper.append(body);
        element.append(wrapper);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:renderCard@791', __javascriptError); throw __javascriptError; }}

    function syncMediaPlayback(element, config, activeItem = null) { try {
        const media = [...(element?.querySelectorAll?.("video,audio") || [])];
        for (const node of media) {
            const active = activeItem ? activeItem.contains?.(node) : !!node.closest?.(".dx-gallery-item-selected");
            if (!active) {
                try { node.pause?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:suppressed-catch@863', __caughtJavaScriptError);  }
                continue;
            }
            if (config.mediaAutoPlay) {
                try { node.play?.().catch?.((__promiseError) => { try { publisherStudioDiagnostics.report('js/componentRuntime.js:promise-catch@867', __promiseError);  return (undefined); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:node.play?.().catch@867', __javascriptError); throw __javascriptError; } }); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:suppressed-catch@867', __caughtJavaScriptError);  }
            }
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:syncMediaPlayback@858', __javascriptError); throw __javascriptError; }}

    function activeChatPlatform(config, context = null) { try {
        const query = new URLSearchParams(location.search).get("publisherChatPlatform");
        const configuredValue = String(config.chatPlatform || "OutputContext");
        const runtimeContext = context || window.PublisherStudioOutputContext || {};
        const configured = query || (lower(configuredValue) === "outputcontext"
            ? (runtimeContext.platform || window.PublisherStudioChatPlatform || "Preview")
            : configuredValue);
        return String(configured).trim() || "Preview";
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:activeChatPlatform@872', __javascriptError); throw __javascriptError; }}

    function chatBroadcastMode() { try {
        return lower(window.PublisherStudioOutputContext?.mode) === "broadcast";
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:chatBroadcastMode@882', __javascriptError); throw __javascriptError; }}

    function activeChatChannel(config, context = null) { try {
        const query = new URLSearchParams(location.search).get("publisherChatChannel");
        const outputContext = lower(config.chatPlatform || "OutputContext") === "outputcontext";
        const runtimeContext = context || window.PublisherStudioOutputContext || {};
        const configured = query || (outputContext
            ? (runtimeContext.channel || window.PublisherStudioChatChannel || config.chatChannel || "")
            : (config.chatChannel || ""));
        return String(configured).trim();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:activeChatChannel@886', __javascriptError); throw __javascriptError; }}

    function chatUser(config) { try {
        return {
            id: String(config.chatCurrentUserId || "publisher"),
            name: String(config.chatCurrentUserName || "Streamer"),
            avatarUrl: allowedMediaSource(config.chatCurrentUserAvatar) || undefined
        };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:chatUser@896', __javascriptError); throw __javascriptError; }}

    function chatMessage(config, row, index = 0, context = null) { try {
        const text = valueFor(row, config.chatMessageField || "text") ?? valueFor(row, "message") ?? "";
        const authorId = valueFor(row, config.chatAuthorIdField || "authorId") ?? valueFor(row, "userId") ?? `viewer-${index + 1}`;
        const authorName = valueFor(row, config.chatAuthorNameField || "authorName") ?? valueFor(row, "userName") ?? valueFor(row, "author") ?? "Viewer";
        const avatar = allowedMediaSource(valueFor(row, config.chatAuthorAvatarField || "authorAvatar") || valueFor(row, "avatar"));
        const timestamp = validDate(valueFor(row, config.chatTimestampField || "timestamp")) || new Date();
        return {
            id: String(valueFor(row, config.keyField || "id") ?? valueFor(row, "id") ?? `message-${index + 1}`),
            text: String(text ?? ""),
            timestamp,
            author: { id: String(authorId), name: String(authorName), avatarUrl: avatar || undefined },
            platform: String(valueFor(row, config.chatPlatformField || "platform") || activeChatPlatform(config, context)),
            channel: String(valueFor(row, config.chatChannelField || "channel") || activeChatChannel(config, context))
        };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:chatMessage@904', __javascriptError); throw __javascriptError; }}

    function chatMessages(config, rows, context = null) { try {
        const platform = lower(activeChatPlatform(config, context));
        const channel = lower(activeChatChannel(config, context));
        return (Array.isArray(rows) ? rows : []).filter(row => { try {
            const rowPlatform = lower(valueFor(row, config.chatPlatformField || "platform"));
            const rowChannel = lower(valueFor(row, config.chatChannelField || "channel"));
            // Untagged preview rows are useful while designing, but never leak into a
            // Twitch/YouTube/Custom output. A selected channel also requires an exact match.
            const platformMatches = rowPlatform ? rowPlatform === platform : platform === "preview";
            const channelMatches = channel ? rowChannel === channel : true;
            return platformMatches && channelMatches;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:(Array.isArray(rows) ? rows : []).filter@923', __javascriptError); throw __javascriptError; }}).map((row, index) => { try { return (chatMessage(config, row, index, context)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:(Array.isArray(rows) ? rows : []).filter(row => { const rowPlatform = @931', __javascriptError); throw __javascriptError; } });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:chatMessages@920', __javascriptError); throw __javascriptError; }}

    function chatMessageId(message) { try {
        return String(message?.id || "").trim();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:chatMessageId@934', __javascriptError); throw __javascriptError; }}

    function chatDisplayMode(config) { try {
        const configured = lower(config?.chatDisplayMode || "auto");
        if (configured === "interactive") return "interactive";
        if (configured === "vieweronly") return "vieweronly";
        if (configured === "streamoverlay") return "streamoverlay";
        if (chatBroadcastMode()) return "streamoverlay";
        return config?.chatAllowSending === false ? "vieweronly" : "interactive";
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:chatDisplayMode@938', __javascriptError); throw __javascriptError; }}

    function chatAllowsSending(config) { try {
        return chatDisplayMode(config) === "interactive" && config?.chatAllowSending !== false && !chatBroadcastMode();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:chatAllowsSending@947', __javascriptError); throw __javascriptError; }}

    function chatMaximumMessages(config) { try {
        return Math.max(1, Math.min(100, Math.round(number(config?.chatMaxVisibleMessages, 12))));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:chatMaximumMessages@951', __javascriptError); throw __javascriptError; }}

    function chatSafeText(value, maximum = 1600) { try {
        const text = String(value ?? "").replace(/[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F]/g, "").trim();
        return text.length > maximum ? `${text.slice(0, Math.max(0, maximum - 1))}…` : text;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:chatSafeText@955', __javascriptError); throw __javascriptError; }}

    function chatInitials(name) { try {
        const parts = chatSafeText(name, 80).split(/\s+/).filter(Boolean);
        if (!parts.length) return "?";
        return `${parts[0][0] || ""}${parts.length > 1 ? parts[parts.length - 1][0] || "" : ""}`.toUpperCase();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:chatInitials@960', __javascriptError); throw __javascriptError; }}

    function formatChatTimestamp(value) { try {
        const timestamp = value instanceof Date ? value : validDate(value);
        if (!timestamp) return "";
        try { return timestamp.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }); } catch { return ""; }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:formatChatTimestamp@966', __javascriptError); throw __javascriptError; }}

    function renderChatOverlayContent(element, config, items) { try {
        const mode = chatDisplayMode(config);
        const maximum = chatMaximumMessages(config);
        const visible = (Array.isArray(items) ? items : []).slice(-maximum);
        element.classList.add("ps-chat-overlay-host", `ps-chat-mode-${mode}`);
        element.classList.toggle("ps-chat-compact", config.chatCompact === true);
        element.classList.toggle("ps-chat-fade-older", config.chatFadeOlderMessages !== false);
        element.style.setProperty("--ps-chat-background-opacity", String(Math.max(0, Math.min(1, number(config.chatBackgroundOpacity, .88)))));
        element.style.setProperty("--ps-chat-message-opacity", String(Math.max(0, Math.min(1, number(config.chatMessageOpacity, .78)))));

        const shell = document.createElement("section");
        shell.className = "ps-stream-chat";
        shell.setAttribute("aria-label", `${activeChatPlatform(config)} chat`);
        shell.dataset.chatMode = mode;

        if (config.chatShowPlatformBadge !== false) {
            const header = document.createElement("header");
            header.className = "ps-stream-chat-header";
            const live = document.createElement("span");
            live.className = "ps-stream-chat-live";
            live.setAttribute("aria-hidden", "true");
            const title = document.createElement("strong");
            title.textContent = activeChatPlatform(config);
            header.append(live, title);
            const channel = activeChatChannel(config);
            if (channel) {
                const label = document.createElement("span");
                label.className = "ps-stream-chat-channel";
                label.textContent = channel;
                header.append(label);
            }
            shell.append(header);
        }

        const list = document.createElement("div");
        list.className = "ps-stream-chat-list";
        list.setAttribute("role", "log");
        list.setAttribute("aria-live", "polite");
        list.setAttribute("aria-relevant", "additions");
        if (!visible.length) {
            const empty = document.createElement("div");
            empty.className = "ps-chat-empty";
            empty.innerHTML = `<span class="dx-icon dx-icon-chat" aria-hidden="true"></span><strong>${escapeHtml(activeChatPlatform(config))} chat</strong><small>Waiting for messages on the selected output.</small>`;
            list.append(empty);
        } else {
            visible.forEach((message, index) => { try {
                const row = document.createElement("article");
                row.className = "ps-stream-chat-message";
                row.dataset.messageId = chatMessageId(message);
                row.style.setProperty("--ps-chat-age", String(visible.length - index - 1));
                const authorName = chatSafeText(message?.author?.name || "Viewer", 120) || "Viewer";
                if (config.chatShowAvatar !== false) {
                    const avatar = document.createElement("span");
                    avatar.className = "ps-stream-chat-avatar";
                    const source = allowedMediaSource(message?.author?.avatarUrl);
                    if (source) {
                        const image = document.createElement("img");
                        image.src = source;
                        image.alt = "";
                        image.loading = "lazy";
                        image.referrerPolicy = "no-referrer";
                        image.addEventListener("error", () => { try { image.remove(); avatar.textContent = chatInitials(authorName);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:image.addEventListener@1033', __javascriptError); throw __javascriptError; }}, { once: true });
                        avatar.append(image);
                    } else avatar.textContent = chatInitials(authorName);
                    row.append(avatar);
                }
                const body = document.createElement("div");
                body.className = "ps-stream-chat-body";
                const meta = document.createElement("div");
                meta.className = "ps-stream-chat-meta";
                const author = document.createElement("strong");
                author.textContent = authorName;
                meta.append(author);
                if (config.chatShowTimestamp !== false) {
                    const time = document.createElement("time");
                    time.textContent = formatChatTimestamp(message?.timestamp);
                    if (message?.timestamp instanceof Date) time.dateTime = message.timestamp.toISOString();
                    meta.append(time);
                }
                const text = document.createElement("p");
                text.textContent = chatSafeText(message?.text, 1600);
                body.append(meta, text);
                row.append(body);
                list.append(row);
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:visible.forEach@1017', __javascriptError); throw __javascriptError; }});
        }
        shell.append(list);
        element.replaceChildren(shell);
        list.scrollTop = list.scrollHeight;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:renderChatOverlayContent@972', __javascriptError); throw __javascriptError; }}

    function renderChatOverlay(element, config, initialItems) { try {
        let disposed = false;
        let items = Array.isArray(initialItems) ? [...initialItems] : [];
        const repaint = () => { try { if (!disposed) renderChatOverlayContent(element, config, items);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:repaint@1066', __javascriptError); throw __javascriptError; }};
        repaint();
        return {
            option(name, value) { try {
                if (arguments.length === 1 && typeof name === "string") return name === "items" ? items : config[name];
                if (name && typeof name === "object") {
                    Object.assign(config, name);
                    if (Array.isArray(name.items)) items = [...name.items];
                } else if (name === "items") items = Array.isArray(value) ? [...value] : [];
                else if (typeof name === "string") config[name] = value;
                repaint();
                return undefined;
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:option@1069', __javascriptError); throw __javascriptError; }},
            renderMessage(message) { try {
                const id = chatMessageId(message);
                if (id && items.some(item => { try { return (chatMessageId(item) === id); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:items.some@1081', __javascriptError); throw __javascriptError; } })) return;
                items.push(message);
                if (items.length > chatMaximumMessages(config) * 4) items = items.slice(-chatMaximumMessages(config) * 4);
                repaint();
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:renderMessage@1079', __javascriptError); throw __javascriptError; }},
            repaint,
            updateDimensions: repaint,
            dispose() { try { disposed = true; element.replaceChildren();  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:dispose@1088', __javascriptError); throw __javascriptError; }}
        };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:renderChatOverlay@1063', __javascriptError); throw __javascriptError; }}

    function mergeChatItems(config, rows, transient = [], context = null) { try {
        const result = [];
        const ids = new Set();
        for (const message of [...chatMessages(config, rows, context), ...(transient || []).filter(message => { try {
            const platform = lower(activeChatPlatform(config, context));
            const channel = lower(activeChatChannel(config, context));
            const messagePlatform = lower(message?.platform);
            const messageChannel = lower(message?.channel);
            return (messagePlatform ? messagePlatform === platform : platform === "preview")
                && (channel ? messageChannel === channel : true);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:(transient || []).filter@1095', __javascriptError); throw __javascriptError; }})]) {
            const id = chatMessageId(message);
            if (id && ids.has(id)) continue;
            if (id) ids.add(id);
            result.push(message);
        }
        return result;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:mergeChatItems@1092', __javascriptError); throw __javascriptError; }}

    function renderChatMessage(state, message) { try {
        if (!state || !message) return false;
        state.chatMessageIds ||= new Set();
        state.chatTransient ||= [];
        const id = chatMessageId(message);
        if (id && state.chatMessageIds.has(id)) return false;
        if (id) state.chatMessageIds.add(id);
        state.chatTransient.push(message);
        state.instance?.renderMessage?.(message);
        return true;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:renderChatMessage@1111', __javascriptError); throw __javascriptError; }}

    function applyChatInputState(element, config) { try {
        const input = element?.querySelector?.(".dx-chat-messagebox textarea,.dx-chat-messagebox input,textarea.dx-texteditor-input,input.dx-texteditor-input");
        if (input && config.placeholder) input.setAttribute("placeholder", String(config.placeholder));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:applyChatInputState@1123', __javascriptError); throw __javascriptError; }}

    function chatUsesLocalGptAi(config) { try {
        return lower(config?.chatAiMode) === "localgptcouncil";
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:chatUsesLocalGptAi', __javascriptError); throw __javascriptError; }}

    function refreshChatItems(state) { try {
        if (!state?.instance) return;
        const config = state.config || {};
        const items = mergeChatItems(config, config.rows || [], state.chatTransient || []);
        state.instance.option?.("items", items);
        state.instance.repaint?.();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:refreshChatItems', __javascriptError); throw __javascriptError; }}

    async function publishLocalGptAiMessage(config, message, element) {
        const state = states.get(element);
        const timestamp = new Date();
        const responseMessage = {
            id: `localgpt-${Date.now()}-${Math.random().toString(36).slice(2)}`,
            text: "LocalGPT Council is thinking…",
            timestamp,
            author: { id: "localgpt-council", name: "LocalGPT Council", avatarUrl: "" },
            platform: activeChatPlatform(config),
            channel: activeChatChannel(config)
        };
        try {
            if (state) renderChatMessage(state, responseMessage);
            const endpoint = String(window.PublisherStudioAiEndpoint || "/api/publisher-ai/chat");
            const response = await fetch(endpoint, {
                method: "POST",
                headers: { "Content-Type": "application/json", "Accept": "application/json" },
                credentials: "same-origin",
                body: JSON.stringify({
                    prompt: String(message?.text || ""),
                    teamKey: String(config.chatAiTeamKey || "general"),
                    systemPrompt: String(config.chatAiSystemPrompt || ""),
                    includeMemory: config.chatAiIncludeMemory !== false,
                    saveToMemory: config.chatAiSaveToMemory !== false,
                    maxOutputTokens: Math.max(256, Math.min(262144, number(config.chatAiMaxOutputTokens, 8192)))
                })
            });
            let payload = null;
            try { payload = await response.json(); } catch { payload = null; }
            if (!response.ok) throw new Error(payload?.error || `LocalGPT AI bridge returned HTTP ${response.status}.`);
            responseMessage.text = String(payload?.text || "").trim() || "LocalGPT completed the Council run without a visible answer.";
        } catch (error) {
            responseMessage.text = `AI unavailable: ${String(error?.message || error || "LocalGPT could not be reached.")}`;
        } finally {
            if (state) refreshChatItems(state);
        }
    }

    function publishChatMessage(config, instance, message, element) { try {
        const detail = {
            componentId: String(config.id || ""),
            outputId: String(window.PublisherStudioOutputContext?.outputId || ""),
            platform: activeChatPlatform(config),
            channel: activeChatChannel(config),
            message
        };
        const state = states.get(element);
        if (config.chatOptimisticSend !== false) {
            if (state) renderChatMessage(state, message);
            else instance?.renderMessage?.(message);
        }
        if (chatUsesLocalGptAi(config)) {
            void publishLocalGptAiMessage(config, message, element);
        } else {
            try { window.PublisherStudioChatBridge?.send?.(detail); } catch (error) { showError(error?.message || String(error)); }
        }
        window.dispatchEvent(new CustomEvent("publisherstudio:chat-send", { detail }));
        element?.dispatchEvent?.(new CustomEvent("publisherstudio:chat-send", { detail, bubbles: true }));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:publishChatMessage@1128', __javascriptError); throw __javascriptError; }}

    function installChatSubscription(element, state) { try {
        const config = state.config;
        const accept = detail => { try {
            if (!detail) return;
            if (detail.componentId && String(detail.componentId) !== String(config.id || "")) return;
            if (lower(detail.platform) !== lower(activeChatPlatform(config))) return;
            const channel = lower(activeChatChannel(config));
            if (channel && lower(detail.channel) !== channel) return;
            const source = detail.message || detail;
            const message = source?.author
                ? { ...source, id: String(source.id || `message-${Date.now()}`), timestamp: validDate(source.timestamp) || new Date(), platform: String(source.platform || detail.platform || activeChatPlatform(config)), channel: String(source.channel || detail.channel || activeChatChannel(config)) }
                : chatMessage(config, source, Date.now());
            renderChatMessage(state, message);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:accept@1148', __javascriptError); throw __javascriptError; }};
        const handler = event => { try { return (accept(event.detail)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:handler@1160', __javascriptError); throw __javascriptError; } };
        window.addEventListener("publisherstudio:chat-message", handler);
        let bridgeUnsubscribe = null;
        try {
            const result = window.PublisherStudioChatBridge?.subscribe?.({
                componentId: String(config.id || ""),
                platform: activeChatPlatform(config),
                channel: activeChatChannel(config)
            }, accept);
            if (typeof result === "function") bridgeUnsubscribe = result;
        } catch (error) { showError(error?.message || String(error)); }
        state.chatUnsubscribe = () => { try {
            window.removeEventListener("publisherstudio:chat-message", handler);
            try { bridgeUnsubscribe?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:suppressed-catch@1173', __caughtJavaScriptError);  }
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:state.chatUnsubscribe@1171', __javascriptError); throw __javascriptError; }};
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:installChatSubscription@1146', __javascriptError); throw __javascriptError; }}

    function chatBroadcastLayers(context = {}, pageElementId = "publisher-page") { try {
        const page = document.getElementById(String(pageElementId || "publisher-page"));
        const pageRect = page?.getBoundingClientRect?.();
        if (!pageRect || pageRect.width <= 0 || pageRect.height <= 0) return [];
        const layers = [];
        for (const [element, state] of states.entries()) {
            const config = state?.config;
            if (!config || lower(config.kind) !== "chat" || !element?.isConnected) continue;
            const publicationElement = element.closest?.("[data-publication-element]") || element;
            const rect = publicationElement.getBoundingClientRect?.();
            if (!rect || rect.width <= 0 || rect.height <= 0) continue;
            const style = getComputedStyle(publicationElement);
            const hostStyle = getComputedStyle(element);
            const bridgeMessages = window.PublisherStudioChatBridge?.getMessages?.(
                activeChatPlatform(config, context),
                activeChatChannel(config, context)) || [];
            const items = mergeChatItems(config, config.rows || [], [...(state.chatTransient || []), ...bridgeMessages], context)
                .slice(-Math.max(1, number(config.chatMaxVisibleMessages, 12)))
                .map(message => { try { return (({
                    id: String(message.id || ""),
                    text: String(message.text || ""),
                    authorName: String(message.author?.name || "Viewer"),
                    authorAvatar: String(message.author?.avatarUrl || ""),
                    authorColor: String(message.color || ""),
                    badges: String(message.badges || ""),
                    timestamp: message.timestamp instanceof Date ? message.timestamp.toISOString() : String(message.timestamp || "")
                })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:mergeChatItems(config, config.rows || [], [...(state.chatTransient || @1195', __javascriptError); throw __javascriptError; } });
            layers.push({
                componentId: String(config.id || element.dataset.psComponentId || ""),
                x: rect.left - pageRect.left,
                y: rect.top - pageRect.top,
                width: rect.width,
                height: rect.height,
                pageWidth: pageRect.width,
                pageHeight: pageRect.height,
                background: style.backgroundColor || hostStyle.backgroundColor || "rgba(15,23,42,.88)",
                color: style.color || hostStyle.color || "#f8fafc",
                fontFamily: style.fontFamily || hostStyle.fontFamily || "system-ui",
                fontSize: parseFloat(style.fontSize) || 16,
                borderRadius: parseFloat(style.borderRadius) || 8,
                platform: activeChatPlatform(config, context),
                channel: activeChatChannel(config, context),
                showAvatar: config.chatShowAvatar !== false,
                showTimestamp: config.chatShowTimestamp !== false,
                showPlatformBadge: config.chatShowPlatformBadge !== false,
                compact: config.chatCompact === true,
                fadeOlder: config.chatFadeOlderMessages !== false,
                backgroundOpacity: Math.max(0, Math.min(1, number(config.chatBackgroundOpacity, .88))),
                messageOpacity: Math.max(0, Math.min(1, number(config.chatMessageOpacity, .78))),
                maxVisibleMessages: chatMaximumMessages(config),
                items
            });
        }
        return layers;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:chatBroadcastLayers@1177', __javascriptError); throw __javascriptError; }}

    function hierarchy(rows, keyField, parentField) { try {
        const byKey = new Map();
        const roots = [];
        for (const row of rows || []) byKey.set(String(valueFor(row, keyField) ?? ""), { ...row, items: [] });
        for (const item of byKey.values()) {
            const parent = String(valueFor(item, parentField) ?? "");
            const key = String(valueFor(item, keyField) ?? "");
            if (parent && byKey.has(parent) && parent !== key) byKey.get(parent).items.push(item);
            else roots.push(item);
        }
        return roots;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:hierarchy@1233', __javascriptError); throw __javascriptError; }}

    function formItems(config) { try {
        return (config.fields || []).filter(field => { try { return (field.visible !== false); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:(config.fields || []).filter@1247', __javascriptError); throw __javascriptError; } }).map(field => { try {
            const lookup = lookupOptions(field);
            const editorOptions = { placeholder: config.placeholder || undefined };
            if (lookup) {
                editorOptions.dataSource = lookup.dataSource;
                editorOptions.valueExpr = lookup.valueExpr;
                editorOptions.displayExpr = lookup.displayExpr;
                editorOptions.searchEnabled = true;
                editorOptions.showClearButton = !field.required;
            }
            return {
                dataField: field.dataField,
                label: { text: field.caption || field.dataField },
                editorType: editorName(field),
                editorOptions,
                isRequired: !!field.required,
                visible: field.visible !== false,
                validationRules: field.required ? [{ type: "required" }] : undefined
            };
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:(config.fields || []).filter(field => field.visible !== false).map@1247', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:formItems@1246', __javascriptError); throw __javascriptError; }}

    function createNestedConfig(parentConfig, panel) { try {
        return {
            ...parentConfig,
            id: panel.id,
            kind: panel.childKind || "DataGrid",
            title: panel.title,
            showTitle: false,
            fields: panel.fields || parentConfig.fields,
            rows: panel.rows || [],
            panels: [],
            actions: [],
            connection: panel.live ? { mode: "PublicationDataObject", dataObjectLive: panel.live } : { mode: "StaticSnapshot" },
            advancedOptionsJson: "{}"
        };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:createNestedConfig@1269', __javascriptError); throw __javascriptError; }}

    function renderPanelContent(parentConfig, panel, itemElement) { try {
        const target = itemElement?.jquery ? itemElement[0] : itemElement;
        if (!(target instanceof Element)) return;
        target.classList.add("ps-component-panel");
        target.replaceChildren();
        if (panel.contentHtml) {
            const content = document.createElement("div");
            content.className = "ps-component-panel-html";
            content.innerHTML = panel.contentHtml;
            target.append(content);
        }
        const host = document.createElement("div");
        host.className = "ps-component-panel-widget";
        target.append(host);
        render(host, createNestedConfig(parentConfig, panel), { polling: false, fetchNow: false });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:renderPanelContent@1285', __javascriptError); throw __javascriptError; }}

    function clearWidgetResidue(element) { try {
        if (!element) return;
        const classes = element.classList && typeof element.classList[Symbol.iterator] === "function"
            ? [...element.classList]
            : [];
        for (const name of classes) {
            if (name.startsWith("dx-") || name.startsWith("ps-dx-") || name.startsWith("ps-component-orientation-"))
                element.classList.remove(name);
        }
        for (const name of ["role", "tabindex", "aria-activedescendant", "aria-expanded", "aria-haspopup", "aria-owns"])
            element.removeAttribute?.(name);
        element.style?.removeProperty?.("width");
        element.style?.removeProperty?.("height");
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:clearWidgetResidue@1302', __javascriptError); throw __javascriptError; }}

    function applyLayoutClasses(element, config) { try {
        const kind = lower(config?.kind);
        const orientation = componentOrientation(config);
        element.classList?.remove?.("ps-component-orientation-horizontal", "ps-component-orientation-vertical");
        element.classList?.add?.(`ps-component-orientation-${orientation}`);
        element.dataset.psComponentKind = kind;
        element.dataset.psComponentOrientation = orientation;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:applyLayoutClasses@1317', __javascriptError); throw __javascriptError; }}

    function refreshNestedLayouts(element) { try {
        if (!element?.querySelectorAll) return;
        for (const child of element.querySelectorAll("[data-ps-component-runtime]")) {
            if (child === element) continue;
            const nested = states.get(child);
            nested?.layout?.schedule?.(true);
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:refreshNestedLayouts@1326', __javascriptError); throw __javascriptError; }}

    function installLayoutObserver(element, state) { try {
        if (!element || !state?.instance) return;
        let frame = null;
        let lastWidth = -1;
        let lastHeight = -1;
        const defer = typeof window.setTimeout === "function" ? window.setTimeout.bind(window) : (callback => { try { callback(); return 0;  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:ArrowFunction@1340', __javascriptError); throw __javascriptError; }});
        const clearDeferred = typeof window.clearTimeout === "function" ? window.clearTimeout.bind(window) : (() => { try { return (undefined); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:ArrowFunction@1341', __javascriptError); throw __javascriptError; } });
        const requestFrame = typeof window.requestAnimationFrame === "function" ? window.requestAnimationFrame.bind(window) : (callback => { try { return (defer(callback, 0)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:ArrowFunction@1342', __javascriptError); throw __javascriptError; } });
        const cancelFrame = typeof window.cancelAnimationFrame === "function" ? window.cancelAnimationFrame.bind(window) : clearDeferred;
        const run = force => { try {
            frame = null;
            const rect = element.getBoundingClientRect?.();
            const width = Math.round(number(rect?.width, element.clientWidth || 0) * 10) / 10;
            const height = Math.round(number(rect?.height, element.clientHeight || 0) * 10) / 10;
            if (!force && width === lastWidth && height === lastHeight) return;
            lastWidth = width;
            lastHeight = height;
            try { state.instance.updateDimensions?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:suppressed-catch@1352', __caughtJavaScriptError);  }
            try { state.instance.repaint?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:suppressed-catch@1353', __caughtJavaScriptError);  }
            refreshNestedLayouts(element);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:run@1344', __javascriptError); throw __javascriptError; }};
        const schedule = (force = false) => { try {
            if (frame !== null) cancelFrame(frame);
            frame = requestFrame(() => { try { return (run(force)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:requestFrame@1358', __javascriptError); throw __javascriptError; } });
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:schedule@1356', __javascriptError); throw __javascriptError; }};
        const observer = typeof window.ResizeObserver === "function"
            ? new window.ResizeObserver(() => { try { return (schedule(false)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:ArrowFunction@1361', __javascriptError); throw __javascriptError; } })
            : null;
        let delayed = null;
        observer?.observe(element);
        state.layout = {
            observer,
            schedule,
            cancel() { try {
                observer?.disconnect();
                if (frame !== null) cancelFrame(frame);
                if (delayed !== null) clearDeferred(delayed);
                frame = null;
                delayed = null;
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:cancel@1368', __javascriptError); throw __javascriptError; }}
        };
        schedule(true);
        delayed = defer(() => { try { return (schedule(true)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:defer@1377', __javascriptError); throw __javascriptError; } }, 60);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:installLayoutObserver@1335', __javascriptError); throw __javascriptError; }}

    function baseOptions(config, element) { try {
        return {
            width: "100%",
            height: "100%",
            disabled: false,
            elementAttr: { class: `ps-dx-${lower(config.kind)} ps-component-orientation-${componentOrientation(config)}` },
            hint: config.title || undefined
        };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:baseOptions@1380', __javascriptError); throw __javascriptError; }}

    function rowLocation(config, row) { try {
        const latitude = number(row?.[config.latitudeField], NaN);
        const longitude = number(row?.[config.longitudeField], NaN);
        if (Number.isFinite(latitude) && Number.isFinite(longitude)) return { lat: latitude, lng: longitude };
        const address = row?.[config.addressField];
        return address == null || String(address).trim() === "" ? null : String(address);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:rowLocation@1390', __javascriptError); throw __javascriptError; }}

    function mapMarkers(config, rows) { try {
        return (rows || []).map(row => { try {
            const location = rowLocation(config, row);
            if (!location) return null;
            const text = row?.[config.markerTooltipField] ?? row?.[config.vectorLabelField] ?? "";
            return { location, tooltip: text ? { text: String(text), isShown: false } : undefined };
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:(rows || []).map@1399', __javascriptError); throw __javascriptError; }}).filter(Boolean);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:mapMarkers@1398', __javascriptError); throw __javascriptError; }}

    function mapRoutes(config, rows) { try {
        if (config.mapShowRoutes === false) return [];
        const groups = new Map();
        for (const row of rows || []) {
            const location = rowLocation(config, row);
            if (!location) continue;
            const key = String(row?.[config.mapRouteField] ?? "default");
            if (!groups.has(key)) groups.set(key, []);
            groups.get(key).push({ location, order: number(row?.[config.mapOrderField], groups.get(key).length) });
        }
        return [...groups.values()].filter(group => { try { return (group.length > 1); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:[...groups.values()].filter@1417', __javascriptError); throw __javascriptError; } }).map(group => { try { return (({
            locations: group.sort((a, b) => { try { return (a.order - b.order); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:group.sort@1418', __javascriptError); throw __javascriptError; } }).map(item => { try { return (item.location); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:group.sort((a, b) => a.order - b.order).map@1418', __javascriptError); throw __javascriptError; } }),
            mode: "driving", opacity: .78, weight: 4
        })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:[...groups.values()].filter(group => group.length > 1).map@1417', __javascriptError); throw __javascriptError; } });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:mapRoutes@1407', __javascriptError); throw __javascriptError; }}

    function sourceName(value) { try {
        const name = String(value || "world").toLowerCase();
        return name === "usa" ? "usa" : name;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:sourceName@1423', __javascriptError); throw __javascriptError; }}

    function vectorGeoJson(config, rows, kind) { try {
        const features = [];
        for (const feature of config.vectorFeatures || []) {
            if (String(feature.kind || "").toLowerCase() !== kind) continue;
            const points = (feature.points || []).map(point => { try { return ([number(point.longitude), number(point.latitude)]); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:(feature.points || []).map@1432', __javascriptError); throw __javascriptError; } });
            if (!points.length) continue;
            let geometry;
            if (kind === "marker") geometry = { type: "Point", coordinates: points[0] };
            else if (kind === "line") geometry = { type: "LineString", coordinates: points };
            else {
                if (points.length < 3) continue;
                const ring = [...points];
                const first = ring[0], last = ring[ring.length - 1];
                if (first[0] !== last[0] || first[1] !== last[1]) ring.push([...first]);
                geometry = { type: "Polygon", coordinates: [ring] };
            }
            features.push({ type: "Feature", id: feature.id, properties: { ...feature }, geometry });
        }
        if (kind === "marker") {
            for (const row of rows || []) {
                const location = rowLocation(config, row);
                if (!location || typeof location === "string") continue;
                features.push({ type: "Feature", properties: {
                    name: row?.[config.vectorLabelField] ?? row?.[config.markerTooltipField] ?? "",
                    value: row?.[config.vectorValueField], color: row?.[config.vectorColorField]
                }, geometry: { type: "Point", coordinates: [location.lng, location.lat] } });
            }
        }
        return { type: "FeatureCollection", features };
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:vectorGeoJson@1428', __javascriptError); throw __javascriptError; }}

    function dataDrivenColor(value, fallback) { try {
        const text = value == null ? "" : String(value).trim();
        if (!text) return fallback;
        try { if (globalThis.CSS?.supports?.("color", text)) return text; } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:suppressed-catch:dataDrivenColor', __caughtJavaScriptError); }
        const palette = ["#2563eb", "#dc2626", "#16a34a", "#9333ea", "#ea580c", "#0891b2", "#ca8a04", "#db2777", "#4f46e5", "#059669", "#7c3aed", "#0284c7"];
        let hash = 2166136261;
        for (let index = 0; index < text.length; index++) { hash ^= text.charCodeAt(index); hash = Math.imul(hash, 16777619); }
        return palette[Math.abs(hash >>> 0) % palette.length] || fallback;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:dataDrivenColor', __javascriptError); return fallback; }}

    function vectorLayers(config, rows) { try {
        const layers = [];
        const preset = sourceName(config.vectorBaseLayer);
        const source = preset === "none" ? null : window.DevExpress?.viz?.map?.sources?.[preset];
        if (source) layers.push({
            name: "base", type: "area", dataSource: source,
            label: { enabled: config.vectorShowLabels !== false, dataField: "name" },
            hoverEnabled: true
        });
        const polygons = vectorGeoJson(config, rows, "polygon");
        if (polygons.features.length) layers.push({ name: "drawings", type: "area", dataSource: polygons,
            customize(elements) { try { elements.forEach(element => { try { const a = element.attribute("properties") || {}; element.applySettings({ color: dataDrivenColor(a.color, "#2563eb"), borderColor: dataDrivenColor(a.borderColor, "#1e3a8a"), opacity: number(a.opacity, .82) });  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:elements.forEach@1470', __javascriptError); throw __javascriptError; }});  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:customize@1470', __javascriptError); throw __javascriptError; }} });
        const lines = vectorGeoJson(config, rows, "line");
        if (lines.features.length) layers.push({ name: "lines", type: "line", dataSource: lines,
            customize(elements) { try { elements.forEach(element => { try { const a = element.attribute("properties") || {}; element.applySettings({ color: dataDrivenColor(a.color, "#2563eb"), width: number(a.width, 3), opacity: number(a.opacity, .9) });  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:elements.forEach@1473', __javascriptError); throw __javascriptError; }});  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:customize@1473', __javascriptError); throw __javascriptError; }} });
        const markers = vectorGeoJson(config, rows, "marker");
        if (markers.features.length) layers.push({ name: "markers", type: "marker", dataSource: markers,
            label: { enabled: config.vectorShowLabels !== false, dataField: "name" },
            customize(elements) { try { elements.forEach(element => { try { const a = element.attribute("properties") || {}; element.applySettings({ color: dataDrivenColor(a.color, "#ef4444"), size: number(a.size, 14) });  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:elements.forEach@1477', __javascriptError); throw __javascriptError; }});  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:customize@1477', __javascriptError); throw __javascriptError; }} });
        return layers;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:vectorLayers@1459', __javascriptError); throw __javascriptError; }}

    function buildOptions(config, element, data) { try {
        const dataSource = data.dataSource;
        const kind = String(config.kind || "DataGrid");
        const base = baseOptions(config, element);
        let options;
        switch (kind) {
            case "DataGrid":
                options = {
                    ...base,
                    dataSource,
                    keyExpr: data.store ? undefined : (config.keyField || undefined),
                    columns: columns(config),
                    showBorders: config.showBorders !== false,
                    columnAutoWidth: true,
                    wordWrapEnabled: !!config.wordWrap,
                    allowColumnReordering: config.allowReordering !== false,
                    allowColumnResizing: config.allowResizing !== false,
                    sorting: { mode: config.allowSorting === false ? "none" : "multiple" },
                    filterRow: { visible: config.allowFiltering !== false && !!config.showFilterRow },
                    headerFilter: { visible: config.allowFiltering !== false && !!config.showHeaderFilter },
                    searchPanel: { visible: !!config.showSearchPanel, width: 220 },
                    groupPanel: { visible: !!config.showGroupPanel },
                    columnChooser: { enabled: !!config.showColumnChooser },
                    paging: { enabled: config.allowPaging !== false, pageSize: Math.max(1, number(config.pageSize, 20)) },
                    pager: { visible: config.allowPaging !== false, showPageSizeSelector: true, allowedPageSizes: [10, 20, 50, 100] },
                    editing: editOptions(config),
                    selection: selectionOptions(config),
                    remoteOperations: lower(config.connection?.processingMode) === "remote"
                };
                break;
            case "TreeList":
                options = {
                    ...base,
                    dataSource,
                    keyExpr: config.keyField || "id",
                    parentIdExpr: config.parentField || "parentId",
                    rootValue: null,
                    columns: columns(config),
                    showBorders: config.showBorders !== false,
                    columnAutoWidth: true,
                    wordWrapEnabled: !!config.wordWrap,
                    autoExpandAll: !!config.autoExpandAll,
                    allowColumnReordering: config.allowReordering !== false,
                    allowColumnResizing: config.allowResizing !== false,
                    sorting: { mode: config.allowSorting === false ? "none" : "multiple" },
                    filterRow: { visible: config.allowFiltering !== false && !!config.showFilterRow },
                    headerFilter: { visible: config.allowFiltering !== false && !!config.showHeaderFilter },
                    searchPanel: { visible: !!config.showSearchPanel },
                    columnChooser: { enabled: !!config.showColumnChooser },
                    paging: { enabled: config.allowPaging !== false, pageSize: Math.max(1, number(config.pageSize, 20)) },
                    editing: editOptions(config),
                    selection: selectionOptions(config),
                    remoteOperations: lower(config.connection?.processingMode) === "remote"
                };
                break;
            case "Scheduler":
                options = {
                    ...base,
                    dataSource,
                    views: ["day", "week", "workWeek", "month", "agenda"],
                    currentView: config.currentView || "week",
                    currentDate: new Date(),
                    startDayHour: 0,
                    endDayHour: 24,
                    textExpr: config.textField || "text",
                    startDateExpr: config.startDateField || "startDate",
                    endDateExpr: config.endDateField || "endDate",
                    allDayExpr: config.allDayField || "allDay",
                    editing: {
                        allowAdding: !!config.connection?.allowInsert && lower(config.editMode) !== "readonly",
                        allowUpdating: !!config.connection?.allowUpdate && lower(config.editMode) !== "readonly",
                        allowDeleting: !!config.connection?.allowDelete && lower(config.editMode) !== "readonly",
                        allowDragging: !!config.connection?.allowUpdate && lower(config.editMode) !== "readonly",
                        allowResizing: !!config.connection?.allowUpdate && lower(config.editMode) !== "readonly"
                    },
                    remoteFiltering: lower(config.connection?.processingMode) === "remote"
                };
                break;
            case "Form": {
                const formData = clone(config.rows?.[0] || {});
                const submitActions = actionsFor(config, "Submit");
                const items = formItems(config);
                if (submitActions.length) items.push({
                    itemType: "button",
                    horizontalAlignment: "left",
                    buttonOptions: {
                        text: config.buttonText || "Submit",
                        type: "success",
                        useSubmitBehavior: false,
                        onClick: event => { try {
                            const form = window.jQuery(element).dxForm("instance");
                            const validation = form?.validate?.();
                            if (validation && validation.isValid === false) return;
                            const currentData = form?.option?.("formData") || formData;
                            return executeActions(config, submitActions, eventContext(config, form, dataSource, event, currentData));
                         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:onClick@1570', __javascriptError); throw __javascriptError; }}
                    }
                });
                options = { ...base, formData, items, colCount: Math.max(1, number(config.columnCount, 2)), labelLocation: "top", showColonAfterLabel: false };
                break;
            }
            case "TextBox": options = { ...base, value: configuredValue(config) ?? "", placeholder: config.placeholder || undefined, showClearButton: true }; break;
            case "TextArea": options = { ...base, value: configuredValue(config) ?? "", placeholder: config.placeholder || undefined, autoResizeEnabled: true }; break;
            case "NumberBox": options = { ...base, value: number(configuredValue(config), 0), placeholder: config.placeholder || undefined, showSpinButtons: true, showClearButton: true }; break;
            case "DateBox": {
                const value = configuredValue(config);
                options = { ...base, value: validDate(value), placeholder: config.placeholder || undefined, type: "datetime", showClearButton: true };
                break;
            }
            case "CheckBox": options = { ...base, value: bool(configuredValue(config)), text: config.title || config.buttonText || "Option" }; break;
            case "SelectBox": options = { ...base, dataSource, value: configuredValue(config), displayExpr: config.displayField || config.textField, valueExpr: config.valueField || config.keyField, placeholder: config.placeholder || undefined, searchEnabled: true, showClearButton: true }; break;
            case "TagBox": {
                const value = configuredValue(config);
                const values = Array.isArray(value) ? value : value === null || value === undefined || value === "" ? [] : String(value).split(",").map(item => { try { return (item.trim()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:String(value).split(",").map@1594', __javascriptError); throw __javascriptError; } }).filter(Boolean);
                options = { ...base, dataSource, value: values, displayExpr: config.displayField || config.textField, valueExpr: config.valueField || config.keyField, placeholder: config.placeholder || undefined, searchEnabled: true, showSelectionControls: true, applyValueMode: "useButtons" };
                break;
            }
            case "Gallery":
                options = {
                    ...base,
                    dataSource,
                    loop: true,
                    showIndicator: true,
                    showNavButtons: true,
                    stretchImages: true,
                    // In the designer the canvas owns drag gestures. Disabling Gallery
                    // swipe recognition prevents a tiny mouse movement on a nav button
                    // from being counted as an additional slide change. Exported HTML
                    // keeps native swipe navigation enabled.
                    swipeEnabled: !config.designerMode,
                    animationDuration: config.designerMode ? 0 : 400,
                    itemTemplate(item, index, itemElement) { try { renderCard(item, config, itemElement?.jquery ? itemElement[0] : itemElement, false);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:itemTemplate@1612', __javascriptError); throw __javascriptError; }},
                    onContentReady() { try { queueMicrotask(() => { try { return (syncMediaPlayback(element, config)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:queueMicrotask@1613', __javascriptError); throw __javascriptError; } });  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:onContentReady@1613', __javascriptError); throw __javascriptError; }},
                    onSelectionChanged() { try { queueMicrotask(() => { try { return (syncMediaPlayback(element, config)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:queueMicrotask@1614', __javascriptError); throw __javascriptError; } });  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:onSelectionChanged@1614', __javascriptError); throw __javascriptError; }}
                };
                break;
            case "TileView":
                options = {
                    ...base,
                    dataSource,
                    baseItemHeight: 120,
                    baseItemWidth: 180,
                    itemMargin: 8,
                    direction: componentOrientation(config),
                    itemTemplate(item, index, itemElement) { try { renderCard(item, config, itemElement?.jquery ? itemElement[0] : itemElement, true);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:itemTemplate@1625', __javascriptError); throw __javascriptError; }},
                    onItemClick(event) { try {
                        const itemElement = event?.itemElement?.jquery ? event.itemElement[0] : event?.itemElement;
                        syncMediaPlayback(element, config, itemElement || null);
                     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:onItemClick@1626', __javascriptError); throw __javascriptError; }}
                };
                break;
            case "Menu": {
                const items = menuItems(config, config.rows);
                const orientation = componentOrientation(config);
                options = {
                    ...base,
                    height: undefined,
                    items,
                    dataSource: undefined,
                    displayExpr: config.displayField || config.textField || "text",
                    orientation,
                    adaptivityEnabled: false,
                    hideSubmenuOnMouseLeave: true,
                    itemTemplate(itemData, itemIndex, itemElement) { try {
                        const target = itemElement?.jquery ? itemElement[0] : itemElement;
                        if (!target) return;
                        const icon = String(itemData?.icon || "").trim();
                        target.replaceChildren();
                        if (icon) {
                            const marker = document.createElement("span");
                            marker.className = icon;
                            marker.setAttribute("aria-hidden", "true");
                            target.append(marker);
                        }
                        const label = document.createElement("span");
                        label.textContent = String(itemData?.text ?? itemData?.[config.displayField || config.textField || "text"] ?? "Menu item");
                        target.append(label);
                     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:itemTemplate@1644', __javascriptError); throw __javascriptError; }}
                };
                break;
            }
            case "ContextMenu": {
                const items = menuItems(config, config.rows);
                options = { items, displayExpr: config.displayField || config.textField || "text", target: element, showEvent: "dxcontextmenu", width: 240 };
                break;
            }
            case "TabPanel":
                options = {
                    ...base,
                    items: config.panels || [],
                    itemTitleTemplate(item, index, itemElement) { try { const target = itemElement?.jquery ? itemElement[0] : itemElement; target.textContent = item.title || `Tab ${index + 1}`;  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:itemTitleTemplate@1671', __javascriptError); throw __javascriptError; }},
                    itemTemplate(item, index, itemElement) { try { renderPanelContent(config, item, itemElement);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:itemTemplate@1672', __javascriptError); throw __javascriptError; }},
                    animationEnabled: true,
                    swipeEnabled: true,
                    deferRendering: false,
                    onSelectionChanged() { try { setTimeout(() => { try { return (refreshNestedLayouts(element)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:setTimeout@1676', __javascriptError); throw __javascriptError; } }, 0);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:onSelectionChanged@1676', __javascriptError); throw __javascriptError; }}
                };
                break;
            case "MultiView":
                options = {
                    ...base,
                    items: config.panels || [],
                    itemTemplate(item, index, itemElement) { try { renderPanelContent(config, item, itemElement);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:itemTemplate@1683', __javascriptError); throw __javascriptError; }},
                    animationEnabled: true,
                    swipeEnabled: true,
                    loop: false,
                    deferRendering: false,
                    onSelectionChanged() { try { setTimeout(() => { try { return (refreshNestedLayouts(element)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:setTimeout@1688', __javascriptError); throw __javascriptError; } }, 0);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:onSelectionChanged@1688', __javascriptError); throw __javascriptError; }}
                };
                break;
            case "Splitter":
                options = {
                    ...base,
                    orientation: componentOrientation(config),
                    items: (config.panels || []).map(panel => { try { return (({
                        ...panel,
                        size: panel.size || undefined,
                        minSize: panel.minSize || undefined,
                        maxSize: panel.maxSize || undefined,
                        collapsible: panel.collapsible !== false,
                        collapsed: !!panel.collapsed,
                        template(itemData, itemIndex, itemElement) { try { renderPanelContent(config, panel, itemElement);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:template@1702', __javascriptError); throw __javascriptError; }}
                    })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:(config.panels || []).map@1695', __javascriptError); throw __javascriptError; } }),
                    onResize() { try { setTimeout(() => { try { return (refreshNestedLayouts(element)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:setTimeout@1704', __javascriptError); throw __javascriptError; } }, 0);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:onResize@1704', __javascriptError); throw __javascriptError; }},
                    onItemCollapsed() { try { setTimeout(() => { try { return (refreshNestedLayouts(element)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:setTimeout@1705', __javascriptError); throw __javascriptError; } }, 0);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:onItemCollapsed@1705', __javascriptError); throw __javascriptError; }},
                    onItemExpanded() { try { setTimeout(() => { try { return (refreshNestedLayouts(element)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:setTimeout@1706', __javascriptError); throw __javascriptError; } }, 0);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:onItemExpanded@1706', __javascriptError); throw __javascriptError; }}
                };
                break;
            case "ScrollView": {
                element.replaceChildren();
                const panel = config.panels?.[0];
                if (panel) renderPanelContent(config, panel, element);
                else {
                    const content = document.createElement("div");
                    content.className = "ps-component-scroll-content";
                    content.innerHTML = `<h3>${escapeHtml(config.title || "Scroll View")}</h3>`;
                    element.append(content);
                }
                options = { ...base, direction: componentOrientation(config, "vertical"), showScrollbar: "onHover", bounceEnabled: true, useNative: false };
                break;
            }
            case "PivotGrid": {
                const fields = (config.fields || []).filter(field => { try { return (field.visible !== false && lower(field.area) !== "none"); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:(config.fields || []).filter@1723', __javascriptError); throw __javascriptError; } }).map(field => { try { return (({
                    dataField: field.dataField,
                    caption: field.caption || field.dataField,
                    dataType: fieldType(field),
                    area: lower(field.area),
                    summaryType: lower(field.summaryType) || "sum",
                    format: field.format || undefined
                })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:(config.fields || []).filter(field => field.visible !== false && lower@1723', __javascriptError); throw __javascriptError; } });
                const pivotSource = new DevExpress.data.PivotGridDataSource({ fields, store: data.store || dataSource });
                data.pivotSource = pivotSource;
                options = { ...base, dataSource: pivotSource, allowSortingBySummary: true, allowFiltering: config.allowFiltering !== false, allowSorting: config.allowSorting !== false, allowExpandAll: true, showBorders: config.showBorders !== false, showColumnGrandTotals: true, showRowGrandTotals: true, fieldChooser: { enabled: true, height: 400 }, scrolling: { mode: "virtual" } };
                break;
            }
            case "Map": {
                const rows = config.rows || [];
                const provider = normalizedMapProvider(config);
                const apiKey = String(config.mapApiKey || "").trim();
                const mapId = String(config.mapId || "").trim();
                const mapContentEnabled = designerMapContentEnabled(config);
                const googleProvider = provider === "google" || provider === "googleStatic";
                options = { ...base, provider, type: config.mapType || "roadmap",
                    center: { lat: number(config.mapCenterLatitude, 51.1657), lng: number(config.mapCenterLongitude, 10.4515) },
                    zoom: Math.max(1, number(config.mapZoom, 4)), controls: config.mapControls !== false && mapContentEnabled,
                    autoAdjust: config.mapAutoAdjust !== false, markers: mapMarkers(config, rows), routes: mapRoutes(config, rows),
                    apiKey: { [provider]: apiKey },
                    providerConfig: googleProvider ? { mapId, useAdvancedMarkers: !!mapId } : undefined,
                    onReady() { try { element.__psMapReady = true;  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:onReady@1749', __javascriptError); throw __javascriptError; }},
                    onOptionChanged(event) { try {
                        if (!["center", "zoom"].includes(String(event?.name || ""))) return;
                        scheduleDesignerMapViewport(element, config, event.component?.option?.("center"), event.component?.option?.("zoom"));
                     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:onOptionChanged@1750', __javascriptError); throw __javascriptError; }}
                };
                break;
            }
            case "VectorMap": {
                const rows = config.rows || [];
                const mapContentEnabled = designerMapContentEnabled(config);
                options = { ...base, layers: vectorLayers(config, rows), projection: config.vectorProjection || "mercator",
                    center: [number(config.mapCenterLongitude, 10.4515), number(config.mapCenterLatitude, 51.1657)],
                    zoomFactor: Math.max(1, number(config.mapZoom, 1)), maxZoomFactor: 256,
                    panningEnabled: mapContentEnabled, zoomingEnabled: mapContentEnabled,
                    controlBar: { enabled: config.mapControls !== false && mapContentEnabled }, tooltip: { enabled: true, customizeTooltip(info) { try {
                        const a = info?.attribute?.("properties") || {}; return { text: String(a.label || a.name || a.value || "") };
                     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:customizeTooltip@1764', __javascriptError); throw __javascriptError; }} },
                    onDrawn() { try { element.__psMapReady = true;  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:onDrawn@1767', __javascriptError); throw __javascriptError; }},
                    onCenterChanged(event) { try {
                        scheduleDesignerMapViewport(element, config, event?.center, event?.component?.option?.("zoomFactor"));
                     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:onCenterChanged@1768', __javascriptError); throw __javascriptError; }},
                    onZoomFactorChanged(event) { try {
                        scheduleDesignerMapViewport(element, config, event?.component?.option?.("center"), event?.zoomFactor);
                     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:onZoomFactorChanged@1771', __javascriptError); throw __javascriptError; }},
                    onClick(event) { try {
                        if (!config.designerMode || !event?.component?.convertToGeo) return;
                        const sourceEvent = event.event || {};
                        const nativeEvent = sourceEvent.originalEvent || sourceEvent;
                        const rect = element.getBoundingClientRect();
                        const x = Number.isFinite(Number(sourceEvent.x)) ? Number(sourceEvent.x)
                            : Number.isFinite(Number(nativeEvent?.offsetX)) ? Number(nativeEvent.offsetX)
                            : number(nativeEvent?.clientX) - rect.left;
                        const y = Number.isFinite(Number(sourceEvent.y)) ? Number(sourceEvent.y)
                            : Number.isFinite(Number(nativeEvent?.offsetY)) ? Number(nativeEvent.offsetY)
                            : number(nativeEvent?.clientY) - rect.top;
                        const point = event.component.convertToGeo(x, y);
                        if (Array.isArray(point)) element.dispatchEvent(new CustomEvent("ps-vector-map-point", { detail: { longitude: point[0], latitude: point[1] } }));
                     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:onClick@1774', __javascriptError); throw __javascriptError; }}
                };
                break;
            }
            case "Chat": {
                const allowChatSending = chatAllowsSending(config);
                element.classList.toggle("ps-chat-readonly", !allowChatSending);
                options = {
                    ...base,
                    items: mergeChatItems(config, config.rows || []),
                    user: chatUser(config),
                    showAvatar: config.chatShowAvatar !== false,
                    showUserName: true,
                    showMessageTimestamp: config.chatShowTimestamp !== false,
                    showDayHeaders: config.chatShowTimestamp !== false,
                    messageTimestampFormat: config.chatShowTimestamp === false ? undefined : "shorttime",
                    editing: { allowDeleting: false, allowUpdating: false },
                    onContentReady() { try {
                        queueMicrotask(() => { try {
                            applyChatInputState(element, config);
                            if (!allowChatSending) element.querySelectorAll(".dx-chat-messagebox,.dx-chat-message-box,.dx-chat-input-container").forEach(node => { try { return (node.remove()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:element.querySelectorAll(".dx-chat-messagebox,.dx-chat-message-box,.dx@1807', __javascriptError); throw __javascriptError; } });
                         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:queueMicrotask@1805', __javascriptError); throw __javascriptError; }});
                     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:onContentReady@1804', __javascriptError); throw __javascriptError; }},
                    onMessageEntered(event) { try {
                        if (!allowChatSending) return;
                        const source = event?.message || {};
                        const message = {
                            ...source,
                            id: String(source.id || `outgoing-${Date.now()}`),
                            text: String(source.text || ""),
                            timestamp: validDate(source.timestamp) || new Date(),
                            author: chatUser(config),
                            platform: activeChatPlatform(config),
                            channel: activeChatChannel(config)
                        };
                        publishChatMessage(config, event.component, message, element);
                     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:onMessageEntered@1810', __javascriptError); throw __javascriptError; }},
                    emptyViewTemplate(data, itemElement) { try {
                        const target = itemElement?.jquery ? itemElement[0] : itemElement;
                        if (!target) return;
                        target.innerHTML = `<div class="ps-chat-empty"><span class="dx-icon dx-icon-chat" aria-hidden="true"></span><strong>${escapeHtml(activeChatPlatform(config))} chat</strong><small>Messages for other platforms stay hidden.</small></div>`;
                     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:emptyViewTemplate@1824', __javascriptError); throw __javascriptError; }}
                };
                break;
            }
            case "Button":
                options = { ...base, text: config.buttonText || config.title || "Run", type: "default", stylingMode: "contained", onClick: event => { try { return (executeActions(config, actionsFor(config, "Click"), eventContext(config, event.component, dataSource, event, clone(config.rows?.[0] || {})))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:onClick@1833', __javascriptError); throw __javascriptError; } } };
                break;
            default: throw new Error(`Unsupported PublisherStudio component: ${kind}`);
        }
        bindCommonActions(config, options, dataSource);
        return deepMerge(options, advancedOptions(config));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:buildOptions@1481', __javascriptError); throw __javascriptError; }}

    async function refreshState(element, state) { try {
        if (!state?.instance) return;
        const kind = String(state.config?.kind || "");
        if (["Map", "VectorMap"].includes(kind)) {
            let rows = null;
            if (state.config.connection?.dataObjectLive?.enabled) {
                rows = await fetchDataObjectLive(state.config.connection.dataObjectLive);
            }
            if (!rows) rows = await materializeRows(state.config, state.data);
            rows = normalizeDateRows(state.config, rows);
            state.config.rows = rows;
            if (kind === "Map") {
                state.instance.option?.("markers", mapMarkers(state.config, rows));
                state.instance.option?.("routes", mapRoutes(state.config, rows));
            } else {
                state.instance.option?.("layers", vectorLayers(state.config, rows));
            }
            state.instance.repaint?.();
            return;
        }
        if (["Form", "Menu", "ContextMenu", "TextBox", "TextArea", "NumberBox", "DateBox", "CheckBox", "Chat"].includes(kind)) {
            const rows = normalizeDateRows(state.config, await materializeRows(state.config, state.data));
            state.config.rows = rows;
            if (kind === "Form") state.instance.option?.("formData", clone(rows[0] || {}));
            else if (kind === "Menu" || kind === "ContextMenu") state.instance.option?.("items", menuItems(state.config, rows));
            else if (kind === "Chat") {
                const items = mergeChatItems(state.config, rows, state.chatTransient);
                state.chatMessageIds = new Set(items.map(chatMessageId).filter(Boolean));
                state.instance.option?.("items", items);
                queueMicrotask(() => { try { return (applyChatInputState(element, state.config)); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:queueMicrotask@1870', __javascriptError); throw __javascriptError; } });
            }
            else {
                const value = configuredValue(state.config);
                state.instance.option?.("value", kind === "DateBox" ? validDate(value) : kind === "NumberBox" ? number(value, 0) : kind === "CheckBox" ? bool(value) : value ?? "");
            }
            state.instance.repaint?.();
            return;
        }
        const source = state.instance.getDataSource?.() || state.dataSource;
        if (source?.reload) await source.reload();
        else if (source?.load) await source.load();
        await state.instance.refresh?.();
        state.instance.repaint?.();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:refreshState@1841', __javascriptError); throw __javascriptError; }}


    function installDesignerMapShield(element, config) { try {
        element.querySelector?.(':scope > .ps-component-designer-map-shield')?.remove?.();
        element.classList?.remove?.("ps-component-designer-object-mode", "ps-component-designer-content-mode");
        if (element.__psMapGestureCleanup) {
            try { element.__psMapGestureCleanup(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:suppressed-catch@1891', __caughtJavaScriptError);  }
            element.__psMapGestureCleanup = null;
        }
        if (!config?.designerMode || !isMapKind(config)) return;
        const contentMode = designerMapContentEnabled(config);
        element.classList?.add?.(contentMode ? "ps-component-designer-content-mode" : "ps-component-designer-object-mode");
        if (contentMode) {
            const controller = new AbortController();
            const signal = controller.signal;
            const eventTarget = typeof window !== "undefined" ? window : globalThis;
            const begin = event => { try {
                if (event.type === "pointerdown" && event.button !== 0) return;
                element.__psMapUserGesture = true;
                element.__psMapGestureActive = true;
                if (element.__psMapViewportTimer) clearTimeout(element.__psMapViewportTimer);
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:begin@1901', __javascriptError); throw __javascriptError; }};
            const finish = () => { try {
                element.__psMapGestureActive = false;
                commitDesignerMapViewport(element, config, 360);
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:finish@1907', __javascriptError); throw __javascriptError; }};
            const wheel = () => { try {
                begin({ type: "wheel" });
                if (element.__psMapWheelTimer) clearTimeout(element.__psMapWheelTimer);
                element.__psMapWheelTimer = setTimeout(() => { try {
                    element.__psMapWheelTimer = null;
                    finish();
                 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:setTimeout@1914', __javascriptError); throw __javascriptError; }}, 520);
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:wheel@1911', __javascriptError); throw __javascriptError; }};
            element.addEventListener?.("pointerdown", begin, { capture: true, passive: true, signal });
            element.addEventListener?.("mousedown", begin, { capture: true, passive: true, signal });
            element.addEventListener?.("touchstart", begin, { capture: true, passive: true, signal });
            element.addEventListener?.("wheel", wheel, { capture: true, passive: true, signal });
            eventTarget.addEventListener?.("pointerup", finish, { capture: true, passive: true, signal });
            eventTarget.addEventListener?.("pointercancel", finish, { capture: true, passive: true, signal });
            eventTarget.addEventListener?.("mouseup", finish, { capture: true, passive: true, signal });
            eventTarget.addEventListener?.("touchend", finish, { capture: true, passive: true, signal });
            eventTarget.addEventListener?.("touchcancel", finish, { capture: true, passive: true, signal });
            element.__psMapGestureCleanup = () => { try { return (controller.abort()); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:element.__psMapGestureCleanup@1928', __javascriptError); throw __javascriptError; } };
            return;
        }
        const shield = document.createElement("div");
        shield.className = "ps-component-designer-map-shield";
        shield.setAttribute("aria-label", "Move map object");
        shield.title = "Move map object. Switch the Mouse mode to Pan map to move or zoom the map content.";
        const blockMapGesture = event => { try {
            event.preventDefault?.();
            event.stopImmediatePropagation?.();
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:blockMapGesture@1935', __javascriptError); throw __javascriptError; }};
        for (const type of ["pointerdown", "pointermove", "pointerup", "pointercancel", "mousedown", "mousemove", "mouseup", "click", "dblclick", "contextmenu", "wheel", "touchstart", "touchmove", "touchend"])
            shield.addEventListener(type, blockMapGesture, { passive: false });
        element.append(shield);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:installDesignerMapShield@1887', __javascriptError); throw __javascriptError; }}

    function dispose(element) { try {
        const state = states.get(element);
        if (state?.timer) clearInterval(state.timer);
        if (element.__psMapViewportTimer) clearTimeout(element.__psMapViewportTimer);
        if (element.__psMapWheelTimer) clearTimeout(element.__psMapWheelTimer);
        try { element.__psMapGestureCleanup?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:suppressed-catch@1949', __caughtJavaScriptError);  }
        element.__psMapGestureCleanup = null;
        element.__psMapViewportTimer = null;
        element.__psMapWheelTimer = null;
        element.__psMapViewportSnapshot = null;
        element.__psMapGestureActive = false;
        element.__psMapUserGesture = false;
        element.__psMapReady = false;
        state?.layout?.cancel?.();
        try { state?.chatUnsubscribe?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:suppressed-catch@1958', __caughtJavaScriptError);  }
        for (const child of [...element.querySelectorAll("[data-ps-component-runtime]")]) {
            if (child !== element) dispose(child);
        }
        try { state?.pivotSource?.dispose?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:suppressed-catch@1962', __caughtJavaScriptError);  }
        try { state?.instance?.dispose?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:suppressed-catch@1963', __caughtJavaScriptError);  }
        states.delete(element);
        element.replaceChildren();
        clearWidgetResidue(element);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:dispose@1944', __javascriptError); throw __javascriptError; }}

    async function render(element, rawConfig, options = {}) { try {
        if (!element) return null;
        const config = decodeConfig(rawConfig || element.dataset.psComponentConfig);
        if (!config) return null;
        const previousState = states.get(element);
        const previousGalleryIndex = lower(previousState?.config?.kind) === "gallery"
            && String(previousState?.config?.id || "") === String(config.id || "")
            ? number(previousState?.instance?.option?.("selectedIndex"), -1)
            : -1;
        dispose(element);
        element.dataset.psComponentRuntime = "true";
        element.dataset.psComponentId = String(config.id || "");
        element.classList.add("ps-component-runtime");
        applyLayoutClasses(element, config);
        if (String(config.kind || "") === "Map" && !hasMapProviderConfiguration(config)) {
            renderMapConfigurationPlaceholder(element, config);
            states.set(element, { config, instance: null, data: null, dataSource: null, timer: null, fallback: true });
            return null;
        }
        const nativeChatOverlay = config.kind === "Chat" && chatDisplayMode(config) !== "interactive";
        if (!nativeChatOverlay && (!window.jQuery || !window.DevExpress)) {
            element.innerHTML = '<div class="ps-component-error">DevExtreme browser assets are not loaded.</div>';
            return null;
        }
        config.rows = normalizeDateRows(config, Array.isArray(config.rows) ? config.rows : []);
        let data = createData(config);
        if (options.fetchNow && config.connection?.dataObjectLive?.enabled) {
            try {
                const rows = await fetchDataObjectLive(config.connection.dataObjectLive);
                if (rows) {
                    config.rows = normalizeDateRows(config, rows);
                    data = createData({ ...config, connection: { ...config.connection, dataObjectLive: null } });
                }
            } catch (error) { showError(error?.message || String(error)); }
        }
        try {
            if (["Form", "Menu", "ContextMenu", "TextBox", "TextArea", "NumberBox", "DateBox", "CheckBox", "Map", "VectorMap", "Scheduler", "Chat"].includes(String(config.kind || "")))
                config.rows = normalizeDateRows(config, await materializeRows(config, data));
            if (config.kind === "Chat" && chatDisplayMode(config) !== "interactive") {
                const items = mergeChatItems(config, config.rows || []);
                const instance = renderChatOverlay(element, config, items);
                const state = {
                    config, instance, data, dataSource: data.dataSource, pivotSource: null, timer: null,
                    chatMessageIds: new Set(items.map(chatMessageId).filter(Boolean)), chatTransient: [], fallback: true
                };
                states.set(element, state);
                installChatSubscription(element, state);
                installLayoutObserver(element, state);
                const interval = number(config.connection?.dataObjectLive?.refreshIntervalSeconds, 0);
                if (options.polling !== false && interval > 0) {
                    state.timer = setInterval(async () => {
                        try { await refreshState(element, state); }
                        catch (error) { console.error("PublisherStudio chat refresh failed.", error); }
                    }, Math.max(1, interval) * 1000);
                }
                return instance;
            }
            const plugin = pluginNames[config.kind];
            if (!plugin || typeof window.jQuery.fn[plugin] !== "function") throw new Error(`${config.kind} is not available in the bundled DevExtreme runtime.`);
            const optionsValue = buildOptions(config, element, data);
            if (config.kind === "Gallery" && previousGalleryIndex >= 0)
                optionsValue.selectedIndex = previousGalleryIndex;
            const $element = window.jQuery(element);
            $element[plugin](optionsValue);
            const instance = $element[plugin]("instance");
            if (config.kind === "Menu") {
                const expectedItems = menuItems(config, config.rows || []);
                await Promise.resolve();
                if (expectedItems.length && typeof element.querySelector === "function" && !element.querySelector(".dx-menu-item")) {
                    try { instance?.dispose?.(); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:suppressed-catch@2038', __caughtJavaScriptError);  }
                    const fallback = renderBasicMenu(element, config, config.rows || []);
                    const fallbackState = { config, instance: fallback, data, dataSource: data.dataSource, pivotSource: null, timer: null, fallback: true };
                    states.set(element, fallbackState);
                    installLayoutObserver(element, fallbackState);
                    return fallback;
                }
            }
            const state = { config, instance, data, dataSource: instance?.getDataSource?.() || data.dataSource, pivotSource: data.pivotSource, timer: null };
            if (config.kind === "Chat") {
                const items = mergeChatItems(config, config.rows || []);
                state.chatMessageIds = new Set(items.map(chatMessageId).filter(Boolean));
                state.chatTransient = [];
            }
            states.set(element, state);
            if (config.kind === "Chat") installChatSubscription(element, state);
            installDesignerMapShield(element, config);
            installLayoutObserver(element, state);
            const interval = number(config.connection?.dataObjectLive?.refreshIntervalSeconds, 0);
            if (options.polling !== false && interval > 0) {
                state.timer = setInterval(async () => {
                    try {
                        await refreshState(element, state);
                    } catch (error) { console.error("PublisherStudio component refresh failed.", error); }
                }, Math.max(1, interval) * 1000);
            }
            return instance;
        } catch (error) {
            console.error("PublisherStudio component rendering failed.", error);
            if (["Menu", "ContextMenu"].includes(String(config.kind || ""))) {
                try {
                    const instance = renderBasicMenu(element, config, config.rows || []);
                    const fallbackState = { config, instance, data, dataSource: data?.dataSource, pivotSource: null, timer: null, fallback: true };
                    states.set(element, fallbackState);
                    installLayoutObserver(element, fallbackState);
                    return instance;
                } catch (fallbackError) {
                    console.error("PublisherStudio menu fallback failed.", fallbackError);
                }
            }
            element.innerHTML = `<div class="ps-component-error"><strong>${escapeHtml(config.title || config.kind)}</strong><span>${escapeHtml(error?.message || String(error))}</span></div>`;
            return null;
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:render@1969', __javascriptError); throw __javascriptError; }}

    async function probeConnection(connection) { try {
        const mode = lower(connection?.mode);
        let url = resolveUrl(connection?.url);
        if (!url) throw new Error("Endpoint URL is not valid or cannot be resolved in this browser.");
        if (mode === "odata" && !/[?&]\$top=/i.test(url)) {
            const probe = new URL(url);
            probe.searchParams.set("$top", "10");
            url = probe.toString();
        }
        const method = mode === "odata" ? "GET" : String(connection?.loadMethod || "GET").toUpperCase();
        const response = await fetch(url, {
            method,
            headers: headersObject(connection?.headers),
            body: ["GET", "HEAD"].includes(method) ? undefined : (connection?.loadBody || ""),
            credentials: connection?.withCredentials ? "include" : "same-origin",
            cache: "no-store"
        });
        const result = await readResponse(response, connection?.jsonPath);
        return JSON.stringify(result.rows || []);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:probeConnection@2083', __javascriptError); throw __javascriptError; }}


    const panelBindings = new WeakMap();

    function panelMedia(element) { try {
        return element?.querySelector?.("video,audio") || null;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:panelMedia@2107', __javascriptError); throw __javascriptError; }}

    function setPanelView(panel, viewId) { try {
        const requested = String(viewId || "");
        let activated = false;
        for (const view of panel.querySelectorAll(":scope > .publication-panel-viewport > [data-panel-canvas-region] > [data-panel-view]")) {
            const active = String(view.dataset.panelView || "") === requested;
            view.hidden = !active;
            view.setAttribute("aria-hidden", active ? "false" : "true");
            activated ||= active;
        }
        if (!activated) return false;
        panel.dataset.panelActiveView = requested;
        for (const button of panel.querySelectorAll(":scope > .publication-panel-navigation [data-panel-target]")) {
            const active = String(button.dataset.panelTarget || "") === requested;
            button.classList.toggle("active", active);
            if (active) button.setAttribute("aria-current", "page");
            else button.removeAttribute("aria-current");
        }
        panel.dispatchEvent(new CustomEvent("publisherstudio:panel-view-changed", {
            bubbles: true,
            detail: { panelId: panel.dataset.panelId || "", viewId: requested }
        }));
        refreshAll(panel, { polling: false, fetchNow: false });
        return true;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:setPanelView@2111', __javascriptError); throw __javascriptError; }}

    function runPanelInteraction(panel, node, interaction) { try {
        const action = lower(interaction?.action || node?.dataset?.interactionAction);
        if (!action || action === "none") return false;
        if (action === "nextpage") return Boolean(window.PublisherStudioNavigation?.next?.());
        if (action === "previouspage") return Boolean(window.PublisherStudioNavigation?.previous?.());
        if (action === "gotopage") return Boolean(window.PublisherStudioNavigation?.goToPage?.(interaction.targetPageId));
        if (action === "openurl") {
            const url = String(interaction.url || "").trim();
            if (!/^(https?:|mailto:)/i.test(url)) return false;
            window.open(url, interaction.openInNewWindow === false ? "_self" : "_blank", "noopener");
            return true;
        }
        const targetId = String(interaction.targetElementId || node.dataset.elementId || "");
        const target = panel.querySelector(`[data-panel-element][data-element-id="${CSS.escape(targetId)}"]`);
        if (!target) return false;
        if (action === "togglevisibility") target.classList.toggle("ps-action-hidden");
        else if (action === "show") target.classList.remove("ps-action-hidden");
        else if (action === "hide") target.classList.add("ps-action-hidden");
        else if (action === "playmedia") panelMedia(target)?.play?.().catch?.((__promiseError) => { try { publisherStudioDiagnostics.report('js/componentRuntime.js:promise-catch@2154', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:panelMedia(target)?.play?.().catch@2154', __javascriptError); throw __javascriptError; }});
        else if (action === "pausemedia") panelMedia(target)?.pause?.();
        else if (action === "togglemediaplayback") {
            const media = panelMedia(target);
            if (media?.paused) media.play?.().catch?.((__promiseError) => { try { publisherStudioDiagnostics.report('js/componentRuntime.js:promise-catch@2158', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:media.play?.().catch@2158', __javascriptError); throw __javascriptError; }}); else media?.pause?.();
        } else return false;
        return true;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:runPanelInteraction@2136', __javascriptError); throw __javascriptError; }}

    function bindPanel(panel) { try {
        if (!panel || panelBindings.has(panel)) return;
        // Panels embedded in a presentation own pointer input. This prevents the surrounding
        // slide click-to-advance handler from consuming clicks intended for panel content.
        panel.classList.add("ps-pointer-owner");
        const controller = new AbortController();
        const options = { signal: controller.signal };
        panelBindings.set(panel, controller);
        for (const button of panel.querySelectorAll(":scope > .publication-panel-navigation [data-panel-target]")) {
            button.addEventListener("click", event => { try {
                event.preventDefault();
                event.stopPropagation();
                setPanelView(panel, button.dataset.panelTarget);
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:button.addEventListener@2169', __javascriptError); throw __javascriptError; }}, options);
        }
        for (const node of panel.querySelectorAll(":scope > .publication-panel-viewport > [data-panel-canvas-region] > [data-panel-view] > [data-panel-element]")) {
            let interaction = {};
            try { interaction = JSON.parse(node.dataset.interaction || "{}"); } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:suppressed-catch@2177', __caughtJavaScriptError);  }
            const action = lower(interaction.action || node.dataset.interactionAction);
            const media = panelMedia(node);
            const kind = lower(node.dataset.elementKind);
            const nativeInteractive = Boolean(media)
                || ["datavisual", "devextremecomponent", "livesource"].includes(kind)
                || Boolean(node.querySelector("video,audio,[data-ps-visual-config],[data-ps-component-config],button,a[href],input,select,textarea,[contenteditable=true]"));
            if (nativeInteractive || (action && action !== "none")) node.classList.add("ps-pointer-owner");
            if (media && lower(node.dataset.mediaTrigger) === "onclick" && (!action || action === "none")) {
                node.addEventListener("click", event => { try {
                    if (event.target?.closest?.("video,audio,button,a,input,select,textarea")) return;
                    event.preventDefault();
                    event.stopPropagation();
                    if (media.paused) media.play?.().catch?.((__promiseError) => { try { publisherStudioDiagnostics.report('js/componentRuntime.js:promise-catch@2186', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:media.play?.().catch@2186', __javascriptError); throw __javascriptError; }}); else media.pause?.();
                 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:node.addEventListener@2182', __javascriptError); throw __javascriptError; }}, options);
            }
            if (action && action !== "none") {
                node.classList.add("ps-interactive");
                node.addEventListener("click", event => { try {
                    if (event.target?.closest?.("button,a,input,select,textarea,[contenteditable=true]") && event.target !== node) return;
                    if (!runPanelInteraction(panel, node, interaction)) return;
                    event.preventDefault();
                    event.stopPropagation();
                 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:node.addEventListener@2191', __javascriptError); throw __javascriptError; }}, options);
            }
        }
        for (const nested of panel.querySelectorAll(":scope > .publication-panel-viewport > [data-panel-canvas-region] > [data-panel-view] > [data-panel-element] > [data-panel-root]")) bindPanel(nested);
        const active = panel.dataset.panelActiveView || panel.querySelector("[data-panel-view]:not([hidden])")?.dataset.panelView;
        if (active) setPanelView(panel, active);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:bindPanel@2163', __javascriptError); throw __javascriptError; }}

    function startPanels(root) { try {
        const scope = root || document;
        const panels = scope.matches?.("[data-panel-root]") ? [scope] : [...scope.querySelectorAll?.("[data-panel-root]") || []];
        panels.forEach(bindPanel);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:startPanels@2204', __javascriptError); throw __javascriptError; }}

    function disposePanels(root) { try {
        if (!root) return;
        const panels = root.matches?.("[data-panel-root]") ? [root] : [...root.querySelectorAll?.("[data-panel-root]") || []];
        panels.forEach(panel => { try {
            panelBindings.get(panel)?.abort?.();
            panelBindings.delete(panel);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:panels.forEach@2213', __javascriptError); throw __javascriptError; }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:disposePanels@2210', __javascriptError); throw __javascriptError; }}


    function resolvePublicationObject(targetOrAddress, root = document) { try {
        if (targetOrAddress instanceof Element) return targetOrAddress;
        const value = String(targetOrAddress || "").trim();
        if (!value) return null;
        const scopes = [];
        if (root instanceof Element || root instanceof Document) scopes.push(root);
        if (document && !scopes.includes(document)) scopes.push(document);
        for (const scope of scopes) {
            try {
                const byAddress = scope.querySelector?.(`[data-object-address="${CSS.escape(value)}"]`);
                if (byAddress) return byAddress;
            } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:suppressed-catch:resolvePublicationObject-address', __caughtJavaScriptError); }
        }
        const match = /\/element\/([0-9a-f-]{36})$/i.exec(value);
        const elementId = match?.[1] || (/^[0-9a-f-]{36}$/i.test(value) ? value : "");
        if (!elementId) return null;
        for (const scope of scopes) {
            try {
                const byId = scope.querySelector?.(`[data-element-id="${CSS.escape(elementId)}"]`);
                if (byId) return byId;
            } catch (__caughtJavaScriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:suppressed-catch:resolvePublicationObject-id', __caughtJavaScriptError); }
        }
        return null;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:resolvePublicationObject', __javascriptError); throw __javascriptError; }}

    function publicationObjectComponentHost(target) { try {
        if (!target) return null;
        if (target.matches?.("[data-ps-component-config]")) return target;
        return target.querySelector?.("[data-ps-component-config]") || null;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:publicationObjectComponentHost', __javascriptError); throw __javascriptError; }}

    function setPublicationObjectVisible(target, visible) { try {
        if (!target) return false;
        if (visible) {
            const previous = target.dataset?.psBehaviorDisplay;
            if (target.style) target.style.display = previous ?? "";
            if (target.dataset) delete target.dataset.psBehaviorDisplay;
            target.removeAttribute?.("aria-hidden");
        } else {
            if (target.dataset && target.dataset.psBehaviorDisplay === undefined) {
                target.dataset.psBehaviorDisplay = target.style?.display || "";
            }
            if (target.style) target.style.display = "none";
            target.setAttribute?.("aria-hidden", "true");
        }
        return true;
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:setPublicationObjectVisible', __javascriptError); throw __javascriptError; }}

    async function invokeObjectMethod(targetOrAddress, method, value, root = document) { try {
        const target = resolvePublicationObject(targetOrAddress, root);
        if (!target) throw new Error(`Publication object '${String(targetOrAddress || "")}' is not available.`);
        const name = lower(method);
        const componentHost = publicationObjectComponentHost(target);
        const state = componentHost ? states.get(componentHost) : null;
        const instance = state?.instance || null;
        const media = target.matches?.("video,audio") ? target : target.querySelector?.("video,audio");
        const focusTarget = target.matches?.("button,a,input,select,textarea,[tabindex]") ? target : target.querySelector?.("button,a,input,select,textarea,[tabindex]");

        if (name === "click") {
            target.click?.();
            return true;
        }
        if (name === "focus") {
            (focusTarget || target).focus?.();
            return true;
        }
        if (name === "blur") {
            (focusTarget || target).blur?.();
            return true;
        }
        if (name === "show") return setPublicationObjectVisible(target, true);
        if (name === "hide") return setPublicationObjectVisible(target, false);
        if (name === "togglevisibility") {
            const hidden = target.style?.display === "none" || target.getAttribute?.("aria-hidden") === "true";
            return setPublicationObjectVisible(target, hidden);
        }
        if (name === "enable" || name === "disable") {
            const disabled = name === "disable";
            if (instance?.option) instance.option("disabled", disabled);
            for (const control of [target, ...target.querySelectorAll?.("button,input,select,textarea") || []]) {
                if ("disabled" in control) control.disabled = disabled;
            }
            target.setAttribute?.("aria-disabled", disabled ? "true" : "false");
            return true;
        }
        if (name === "refresh" || name === "refreshdata") {
            if (state && componentHost) await refreshState(componentHost, state);
            else await window.PublisherStudioLiveDataRuntime?.refreshAll?.(target, { polling: false, fetchNow: true });
            instance?.repaint?.();
            return true;
        }
        if (name === "repaint") {
            instance?.repaint?.();
            return true;
        }
        if (name === "reset") {
            instance?.reset?.();
            return true;
        }
        if (name === "setvalue") {
            if (instance?.option) instance.option("value", value);
            else {
                const input = target.matches?.("input,select,textarea") ? target : target.querySelector?.("input,select,textarea");
                if (input) {
                    input.value = value ?? "";
                    input.dispatchEvent(new Event("change", { bubbles: true }));
                } else target.textContent = value ?? "";
            }
            return true;
        }
        if (name === "settext") {
            if (instance?.option) {
                if (instance.option("text") !== undefined) instance.option("text", value ?? "");
                else if (instance.option("value") !== undefined) instance.option("value", value ?? "");
                else target.textContent = value ?? "";
            } else target.textContent = value ?? "";
            return true;
        }
        if (name === "clearfilter") {
            const dataSource = instance?.getDataSource?.() || state?.dataSource;
            dataSource?.filter?.(null);
            await dataSource?.reload?.();
            instance?.refresh?.();
            return true;
        }
        if (name === "clearselection") {
            await instance?.clearSelection?.();
            return true;
        }
        if (name === "selectall") {
            await instance?.selectAll?.();
            return true;
        }
        if (name === "scrolltotime") {
            instance?.scrollToTime?.(value ?? new Date());
            return true;
        }
        if (name === "play") {
            await media?.play?.();
            return true;
        }
        if (name === "pause") {
            media?.pause?.();
            return true;
        }
        throw new Error(`Publication object method '${String(method || "")}' is not supported.`);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:invokeObjectMethod', __javascriptError); throw __javascriptError; }}


    const publicationBehaviorBindings = new WeakMap();
    const activePublicationBehaviorIds = new Set();

    function publicationObjectApi(targetOrAddress, host = null) { try {
        const root = host?.closest?.(".print-page,.publication-panel-view,.ps-slide,.ps-site-page") || document;
        const element = resolvePublicationObject(targetOrAddress, root);
        if (!element) return null;
        return Object.freeze({
            address: element.dataset?.objectAddress || String(targetOrAddress || ""),
            elementId: element.dataset?.elementId || "",
            call(method, value) { try { return invokeObjectMethod(element, method, value, root); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:publicationObjectApi.call', __javascriptError); throw __javascriptError; } }
        });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:publicationObjectApi', __javascriptError); throw __javascriptError; }}

    function publicationObjectsApi(host = null) { try {
        const current = host?.closest?.("[data-publication-element],[data-panel-element]") || host || null;
        return Object.freeze({
            get(address) { try { return publicationObjectApi(address, host); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:publicationObjectsApi.get', __javascriptError); throw __javascriptError; } },
            current(currentHost = current) { try { return publicationObjectApi(currentHost, host); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:publicationObjectsApi.current', __javascriptError); throw __javascriptError; } },
            byId(elementId) { try { return publicationObjectApi(String(elementId || ""), host); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:publicationObjectsApi.byId', __javascriptError); throw __javascriptError; } }
        });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:publicationObjectsApi', __javascriptError); throw __javascriptError; }}

    const publicationNavigationApi = Object.freeze({
        goToPage(pageId) { try {
            const value = String(pageId || "").trim();
            if (!value) return false;
            const api = window.PublisherStudioNavigation || window.PublisherStudioPresentation;
            if (api?.goToPage) return api.goToPage(value);
            window.dispatchEvent(new CustomEvent("publisherstudio:navigate", { detail: { pageId: value } }));
            return true;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:publicationNavigationApi.goToPage', __javascriptError); throw __javascriptError; }},
        nextPage() { try {
            const api = window.PublisherStudioNavigation || window.PublisherStudioPresentation;
            if (api?.nextPage) return api.nextPage();
            if (api?.next) return api.next();
            window.dispatchEvent(new CustomEvent("publisherstudio:navigate-next"));
            return true;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:publicationNavigationApi.nextPage', __javascriptError); throw __javascriptError; }},
        previousPage() { try {
            const api = window.PublisherStudioNavigation || window.PublisherStudioPresentation;
            if (api?.previousPage) return api.previousPage();
            if (api?.previous) return api.previous();
            window.dispatchEvent(new CustomEvent("publisherstudio:navigate-previous"));
            return true;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:publicationNavigationApi.previousPage', __javascriptError); throw __javascriptError; }}
    });

    function normalizedBehaviorName(value) { try {
        return String(value || "").replace(/[^a-z0-9]/gi, "").toLowerCase();
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:normalizedBehaviorName', __javascriptError); throw __javascriptError; }}

    function parsePublicationBehaviors(element) { try {
        const source = String(element?.dataset?.behaviors || "").trim();
        if (!source) return [];
        const parsed = JSON.parse(source);
        return Array.isArray(parsed) ? parsed.filter(rule => rule && rule.enabled !== false) : [];
     } catch (__javascriptError) {
        publisherStudioDiagnostics.report('js/componentRuntime.js:parsePublicationBehaviors', __javascriptError);
        return [];
     }}

    function publicationBehaviorTarget(source, rule) { try {
        const targetId = String(rule?.targetElementId || "").trim();
        if (!targetId) return source;
        const page = source.closest?.(".print-page,.publication-panel-view,.ps-slide,.ps-site-page");
        return resolvePublicationObject(targetId, page || document) || resolvePublicationObject(targetId, document);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:publicationBehaviorTarget', __javascriptError); throw __javascriptError; }}

    async function executePublicationBehavior(source, rule, event = null) { try {
        const ruleId = String(rule?.id || "");
        if (ruleId && activePublicationBehaviorIds.has(ruleId)) return false;
        if (ruleId) activePublicationBehaviorIds.add(ruleId);
        try {
            const action = normalizedBehaviorName(rule?.action);
            const target = publicationBehaviorTarget(source, rule);
            if (action === "none") return false;
            if (action === "click") return await invokeObjectMethod(target, "click");
            if (action === "focus") return await invokeObjectMethod(target, "focus");
            if (action === "blur") return await invokeObjectMethod(target, "blur");
            if (action === "refreshdata") return await invokeObjectMethod(target, "refreshData");
            if (action === "show") return await invokeObjectMethod(target, "show");
            if (action === "hide") return await invokeObjectMethod(target, "hide");
            if (action === "togglevisibility") return await invokeObjectMethod(target, "toggleVisibility");
            if (action === "enable") return await invokeObjectMethod(target, "enable");
            if (action === "disable") return await invokeObjectMethod(target, "disable");
            if (action === "settext") return await invokeObjectMethod(target, "setText", rule?.value ?? "");
            if (action === "setvalue") return await invokeObjectMethod(target, "setValue", rule?.value ?? "");
            if (action === "callmethod") return await invokeObjectMethod(target, String(rule?.method || ""), rule?.value ?? "");
            if (action === "gotopage") return publicationNavigationApi.goToPage(rule?.targetPageId);
            if (action === "nextpage") return publicationNavigationApi.nextPage();
            if (action === "previouspage") return publicationNavigationApi.previousPage();
            if (action === "openurl") {
                const url = String(rule?.url || "").trim();
                if (!/^(https?:|mailto:)/i.test(url)) throw new Error("Only http, https and mailto publication behavior addresses are allowed.");
                window.open(url, rule?.openInNewWindow === false ? "_self" : "_blank", "noopener");
                return true;
            }
            return false;
        } finally {
            if (ruleId) activePublicationBehaviorIds.delete(ruleId);
        }
     } catch (__javascriptError) {
        publisherStudioDiagnostics.report('js/componentRuntime.js:executePublicationBehavior', __javascriptError);
        showError(__javascriptError?.message || String(__javascriptError));
        return false;
     }}

    function behaviorEventName(trigger) { try {
        const name = normalizedBehaviorName(trigger);
        if (name === "click") return "click";
        if (name === "doubleclick") return "dblclick";
        if (name === "change") return "change";
        if (name === "focus") return "focusin";
        if (name === "blur") return "focusout";
        if (name === "pointerenter") return "pointerenter";
        if (name === "pointerleave") return "pointerleave";
        if (name === "load") return "load";
        return "";
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:behaviorEventName', __javascriptError); throw __javascriptError; }}

    function bindPublicationBehaviors(element) { try {
        const source = String(element?.dataset?.behaviors || "").trim();
        const previous = publicationBehaviorBindings.get(element);
        if (previous?.source === source) return;
        previous?.controller?.abort?.();

        const rules = parsePublicationBehaviors(element);
        if (!rules.length) {
            publicationBehaviorBindings.delete(element);
            return;
        }

        const controller = new AbortController();
        publicationBehaviorBindings.set(element, { controller, source });
        const byEvent = new Map();
        for (const rule of rules) {
            const eventName = behaviorEventName(rule.trigger);
            if (!eventName) continue;
            if (!byEvent.has(eventName)) byEvent.set(eventName, []);
            byEvent.get(eventName).push(rule);
        }

        const arranged = () => element.closest?.('.panel-studio-canvas[data-panel-studio-arrange="true"]') !== null;

        for (const [eventName, eventRules] of byEvent.entries()) {
            if (eventName === "load") {
                queueMicrotask(() => { try {
                    if (!element.isConnected || arranged()) return;
                    for (const rule of eventRules) executePublicationBehavior(element, rule, null);
                 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:publication-behavior-load', __javascriptError); }});
                continue;
            }
            element.addEventListener(eventName, event => { try {
                if (arranged()) return;
                for (const rule of eventRules) executePublicationBehavior(element, rule, event);
             } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:publication-behavior-event', __javascriptError); }}, { signal: controller.signal });
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:bindPublicationBehaviors', __javascriptError); throw __javascriptError; }}

    function startPublicationBehaviors(root) { try {
        const scope = root || document;
        const elements = scope.matches?.("[data-behaviors]") ? [scope] : [...scope.querySelectorAll?.("[data-behaviors]") || []];
        elements.forEach(bindPublicationBehaviors);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:startPublicationBehaviors', __javascriptError); throw __javascriptError; }}

    function disposePublicationBehaviors(root) { try {
        if (!root) return;
        const elements = root.matches?.("[data-behaviors]") ? [root] : [...root.querySelectorAll?.("[data-behaviors]") || []];
        elements.forEach(element => { try {
            publicationBehaviorBindings.get(element)?.controller?.abort?.();
            publicationBehaviorBindings.delete(element);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:disposePublicationBehaviors-item', __javascriptError); }});
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:disposePublicationBehaviors', __javascriptError); throw __javascriptError; }}

    async function refreshAll(root, options = {}) { try {
        const scope = root || document;
        startPanels(scope);
        startPublicationBehaviors(scope);
        const elements = scope.matches?.("[data-ps-component-config]") ? [scope] : [...scope.querySelectorAll?.("[data-ps-component-config]") || []];
        await Promise.all(elements.map(element => { try { return (render(element, element.dataset.psComponentConfig, { polling: options.polling, fetchNow: options.fetchNow !== false })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:elements.map@2223', __javascriptError); throw __javascriptError; } }));
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:refreshAll@2219', __javascriptError); throw __javascriptError; }}

    function start(root, options = {}) { try {
        const scope = root || document;
        startPanels(scope);
        startPublicationBehaviors(scope);
        const elements = scope.matches?.("[data-ps-component-config]") ? [scope] : [...scope.querySelectorAll?.("[data-ps-component-config]") || []];
        elements.forEach(element => { try { return (render(element, element.dataset.psComponentConfig, { polling: options.polling !== false, fetchNow: options.fetchNow !== false })); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:elements.forEach@2230', __javascriptError); throw __javascriptError; } });
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:start@2226', __javascriptError); throw __javascriptError; }}

    window.addEventListener("publisherstudio:output-context-changed", () => { try {
        for (const [element, state] of states.entries()) {
            if (lower(state?.config?.kind) !== "chat") continue;
            render(element, state.config, { polling: false, fetchNow: false });
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:callback:window.addEventListener@2233', __javascriptError); throw __javascriptError; }});

    window.PublisherStudioChatRuntime = window.PublisherStudioChatRuntime || {
        push(message) { try { window.dispatchEvent(new CustomEvent("publisherstudio:chat-message", { detail: message }));  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:push@2241', __javascriptError); throw __javascriptError; }},
        setPlatform(platform) { try { window.PublisherStudioChatPlatform = String(platform || "Preview"); return refreshAll(document, { fetchNow: false });  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:setPlatform@2242', __javascriptError); throw __javascriptError; }},
        setChannel(channel) { try { window.PublisherStudioChatChannel = String(channel || ""); return refreshAll(document, { fetchNow: false });  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:setChannel@2243', __javascriptError); throw __javascriptError; }},
        getBroadcastLayers(context, pageElementId) { try { return chatBroadcastLayers(context || {}, pageElementId);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:getBroadcastLayers@2244', __javascriptError); throw __javascriptError; }}
    };

    window.PublisherStudioPublicationRuntime = {
        objects: publicationObjectsApi,
        publication: publicationNavigationApi,
        resolveObject: resolvePublicationObject,
        call(address, method, value) { try { return invokeObjectMethod(address, method, value); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:PublisherStudioPublicationRuntime.call', __javascriptError); throw __javascriptError; } },
        start(root) { try { startPublicationBehaviors(root || document); return true; } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:PublisherStudioPublicationRuntime.start', __javascriptError); throw __javascriptError; } },
        refresh(root) { try { startPublicationBehaviors(root || document); return true; } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:PublisherStudioPublicationRuntime.refresh', __javascriptError); throw __javascriptError; } }
    };

    window.PublisherStudioComponentRuntime = {
        render,
        renderById(id, config) { try { return render(document.getElementById(id), config, { polling: false, fetchNow: false });  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:renderById@2249', __javascriptError); throw __javascriptError; }},
        disposeById(id) { try { const element = document.getElementById(id); if (element) dispose(element);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:disposeById@2250', __javascriptError); throw __javascriptError; }},
        refreshAll,
        probeConnection,
        resolvePublicationObject,
        invokeObjectMethod,
        attachVectorDesigner(id, dotnet) { try {
            const element = document.getElementById(id);
            if (!element) return false;
            if (element.__psVectorHandler) element.removeEventListener("ps-vector-map-point", element.__psVectorHandler);
            element.__psVectorHandler = event => { try { return (dotnet?.invokeMethodAsync?.("AddVectorDesignerPoint", number(event.detail?.longitude), number(event.detail?.latitude))); } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:element.__psVectorHandler@2257', __javascriptError); throw __javascriptError; } };
            element.addEventListener("ps-vector-map-point", element.__psVectorHandler);
            return true;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:attachVectorDesigner@2253', __javascriptError); throw __javascriptError; }},
        detachVectorDesigner(id) { try {
            const element = document.getElementById(id);
            if (!element?.__psVectorHandler) return;
            element.removeEventListener("ps-vector-map-point", element.__psVectorHandler);
            delete element.__psVectorHandler;
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:detachVectorDesigner@2261', __javascriptError); throw __javascriptError; }},
        start,
        dispose(root) { try {
            if (!root) return;
            const elements = root.matches?.("[data-ps-component-config]") ? [root] : [...root.querySelectorAll?.("[data-ps-component-config]") || []];
            elements.forEach(dispose);
            disposePanels(root);
            disposePublicationBehaviors(root);
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:dispose@2268', __javascriptError); throw __javascriptError; }},
        refreshPanels(root) { try { startPanels(root || document);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:refreshPanels@2274', __javascriptError); throw __javascriptError; }}
    };
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/componentRuntime.js:ArrowFunction@2', __javascriptError); throw __javascriptError; }})();

// Guard exported browser namespaces after the file has initialized.
publisherStudioDiagnostics.guardObject("PublisherStudioPublicationRuntime", window.PublisherStudioPublicationRuntime);
publisherStudioDiagnostics.guardObject("PublisherStudioChatRuntime", window.PublisherStudioChatRuntime);
publisherStudioDiagnostics.guardObject("PublisherStudioComponentRuntime", window.PublisherStudioComponentRuntime);
