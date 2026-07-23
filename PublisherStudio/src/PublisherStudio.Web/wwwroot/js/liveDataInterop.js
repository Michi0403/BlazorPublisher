(function () {
    "use strict";

    const states = new Map();
    const decoder = new TextDecoder();
    let pointerOwnershipBound = false;

    function clearVisualInteraction(state) {
        const instance = state?.instance;
        if (!instance) return;
        try { instance.hideTooltip?.(); } catch { }
        try { instance.clearHover?.(); } catch { }
        try {
            instance.getAllSeries?.().forEach(series => {
                try { series.clearHover?.(); } catch { }
                try { series.getAllPoints?.().forEach(point => point.clearHover?.()); } catch { }
            });
        } catch { }
    }

    function eventBelongsTo(element, event) {
        if (!element || !event) return false;
        const path = typeof event.composedPath === "function" ? event.composedPath() : [];
        if (path.includes(element)) return true;
        return event.target instanceof Node && element.contains(event.target);
    }

    function clearVisualsOutside(event) {
        states.forEach((state, element) => {
            if (!eventBelongsTo(element, event)) clearVisualInteraction(state);
        });
    }

    function clearAllVisualInteractions() {
        states.forEach(clearVisualInteraction);
    }

    function bindPointerOwnership() {
        if (pointerOwnershipBound || typeof document === "undefined") return;
        pointerOwnershipBound = true;
        document.addEventListener("pointerover", clearVisualsOutside, true);
        document.addEventListener("pointerdown", clearVisualsOutside, true);
        document.addEventListener("pointerout", event => {
            if (!event.relatedTarget) clearAllVisualInteractions();
        }, true);
        window.addEventListener("blur", clearAllVisualInteractions);
    }

    function decodeConfig(value) {
        if (!value) return null;
        if (typeof value === "object") return value;
        try {
            const binary = atob(value);
            const bytes = Uint8Array.from(binary, character => character.charCodeAt(0));
            return JSON.parse(decoder.decode(bytes));
        } catch {
            try { return JSON.parse(value); } catch { return null; }
        }
    }

    function number(value, declaredKind) {
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
            normalized = parts.length > 2 && parts.slice(1).every(part => part.length === 3)
                ? parts.join("")
                : `${parts.slice(0, -1).join("")}.${parts.at(-1)}`;
        } else if ((normalized.match(/\./g) || []).length > 1) {
            const parts = normalized.split(".");
            normalized = parts.slice(1).every(part => part.length === 3)
                ? parts.join("")
                : `${parts.slice(0, -1).join("")}.${parts.at(-1)}`;
        }
        const parsed = Number(normalized);
        if (!Number.isFinite(parsed)) return raw ? 1 : 0;
        return negativeParentheses ? -Math.abs(parsed) : parsed;
    }

    function get(row, field) {
        if (!row || !field) return "";
        if (Object.prototype.hasOwnProperty.call(row, field)) return row[field];
        const wanted = field.toLowerCase();
        const key = Object.keys(row).find(candidate => candidate.toLowerCase() === wanted);
        return key ? row[key] : "";
    }

    function friendly(value) {
        return String(value || "").replace(/([a-z0-9])([A-Z])/g, "$1 $2");
    }

    function visualRoot(element) {
        if (!element) return null;
        return element.matches?.("[data-ps-visual-config]") ? element : element.querySelector?.("[data-ps-visual-config]");
    }

    function dataBaseUrl() {
        const query = new URLSearchParams(location.search).get("publisherApi");
        let stored = "";
        try { stored = localStorage.getItem("PublisherStudioDataBaseUrl") || ""; } catch { }
        const configured = query || window.PublisherStudioDataBaseUrl || stored;
        if (configured) return String(configured).replace(/\/$/, "");
        if (/^https?:$/.test(location.protocol)) return location.origin;
        return "";
    }

    function resolveUrl(url) {
        if (!url) return "";
        try { return new URL(url).toString(); } catch { }
        const base = dataBaseUrl();
        return base ? new URL(url.replace(/^\//, ""), base + "/").toString() : "";
    }

    function selectJsonPath(value, path) {
        if (!path) return value;
        return path.split(".").filter(Boolean).reduce((current, segment) => {
            if (current == null) return undefined;
            if (Array.isArray(current) && /^\d+$/.test(segment)) return current[Number(segment)];
            if (typeof current !== "object") return undefined;
            if (Object.prototype.hasOwnProperty.call(current, segment)) return current[segment];
            const key = Object.keys(current).find(candidate => candidate.toLowerCase() === segment.toLowerCase());
            return key ? current[key] : undefined;
        }, value);
    }

    function unwrapJsonString(value) {
        if (typeof value !== "string") return value;
        const trimmed = value.trim();
        if (!trimmed.startsWith("{") && !trimmed.startsWith("[")) return value;
        try { return JSON.parse(trimmed); } catch { return value; }
    }

    function findJsonRows(value, depth) {
        if (Array.isArray(value)) return value;
        if (!value || typeof value !== "object" || depth > 4) return null;

        const entries = Object.entries(value);
        const wrappers = new Set(["data", "items", "results", "records", "rows"]);
        for (const [name, nested] of entries.filter(([name]) => wrappers.has(name.toLowerCase()))) {
            if (Array.isArray(nested)) return nested;
            const rows = findJsonRows(nested, depth + 1);
            if (rows) return rows;
        }

        const arrays = entries.map(([, nested]) => nested).filter(Array.isArray);
        if (arrays.length === 1) return arrays[0];
        const objects = entries.map(([, nested]) => nested).filter(nested => nested && typeof nested === "object" && !Array.isArray(nested));
        return objects.length === 1 ? findJsonRows(objects[0], depth + 1) : null;
    }

    function flattenJsonRow(source, prefix, target) {
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
    }

    function parseDelimited(text, delimiter, hasHeaders) {
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
                if (row.some(value => value !== "")) rows.push(row);
                row = [];
            } else field += character;
        }
        if (!rows.length) return [];
        const width = Math.max(...rows.map(item => item.length));
        const rawHeaders = hasHeaders
            ? rows.shift().map((value, index) => value || `Column ${index + 1}`)
            : Array.from({ length: width }, (_, index) => `Column ${index + 1}`);
        const usedHeaders = new Set();
        const headers = rawHeaders.map((raw, index) => {
            const basis = String(raw || `Column ${index + 1}`).trim() || `Column ${index + 1}`;
            let candidate = basis, suffix = 2;
            while (usedHeaders.has(candidate.toLowerCase())) candidate = `${basis} ${suffix++}`;
            usedHeaders.add(candidate.toLowerCase());
            return candidate;
        });
        return rows.map(values => Object.fromEntries(headers.map((header, index) => [header, values[index] ?? ""])));
    }

    function parseXml(text) {
        const document = new DOMParser().parseFromString(text, "application/xml");
        if (document.querySelector("parsererror")) throw new Error("The endpoint returned invalid XML.");
        const root = document.documentElement;
        let nodes = Array.from(root.children);
        const groups = new Map();
        nodes.forEach(node => groups.set(node.localName, [...(groups.get(node.localName) || []), node]));
        const repeated = [...groups.values()].sort((a, b) => b.length - a.length)[0];
        if (repeated?.length > 1) nodes = repeated;
        else if (nodes.length === 1 && nodes[0].children.length) nodes = Array.from(nodes[0].children);
        else if (!nodes.length) nodes = [root];

        return nodes.map(node => {
            const result = {};
            const add = (name, value, attribute) => {
                const basis = String(name || (attribute ? "Attribute" : "Value")).trim() || (attribute ? "Attribute" : "Value");
                let candidate = basis;
                if (Object.prototype.hasOwnProperty.call(result, candidate) && attribute) candidate = `@${basis}`;
                let suffix = 2;
                while (Object.prototype.hasOwnProperty.call(result, candidate)) candidate = `${basis} ${suffix++}`;
                result[candidate] = String(value ?? "").trim();
            };
            Array.from(node.attributes).forEach(attribute => add(attribute.localName, attribute.value, true));
            const children = Array.from(node.children);
            if (!children.length) {
                add(node === root ? root.localName : "Value", node.textContent || "", false);
            } else {
                children.forEach(child => {
                    add(child.localName, child.textContent || "", false);
                    Array.from(child.attributes).forEach(attribute => add(`${child.localName}.@${attribute.localName}`, attribute.value, true));
                });
            }
            return result;
        });
    }

    function parseResponse(text, contentType, live) {
        contentType = String(contentType || "").toLowerCase();
        let format = String(live.responseFormat || "Auto").toLowerCase();
        if (format === "auto") {
            const trimmed = text.trimStart();
            let encodedJson = false;
            if (trimmed.startsWith('"')) {
                try {
                    const decoded = JSON.parse(trimmed);
                    encodedJson = typeof decoded === "string" && /^[\s]*[\[{]/.test(decoded);
                } catch { }
            }
            format = contentType.includes("json") || trimmed.startsWith("{") || trimmed.startsWith("[") || encodedJson ? "json"
                : contentType.includes("xml") || trimmed.startsWith("<") ? "xml" : "delimitedtext";
        }
        if (format === "json") {
            let value = unwrapJsonString(JSON.parse(text));
            value = unwrapJsonString(selectJsonPath(value, live.jsonPath));
            const rows = findJsonRows(value, 0) || (value && typeof value === "object" && !Array.isArray(value) ? [value] : []);
            if (rows.some(item => !item || typeof item !== "object" || Array.isArray(item)))
                throw new Error("Every JSON row must be an object with named properties.");
            return rows.map(row => flattenJsonRow(row, "", {}));
        }
        if (format === "xml") return parseXml(text);
        if (format === "text") return [{ Value: text }];
        return parseDelimited(text, live.delimiter || ",", live.firstRowContainsHeaders !== false);
    }

    async function fetchRows(config) {
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
        (live.headers || []).forEach(header => { if (header.name) headers[header.name] = header.value || ""; });
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
    }

    function disposeWidget(element) {
        const state = states.get(element);
        if (state?.timer) clearInterval(state.timer);
        clearVisualInteraction(state);
        try {
            const instance = state?.instance;
            if (instance?.dispose) instance.dispose();
        } catch { }
        states.delete(element);
        element.replaceChildren();
    }

    function fallback(element, config, rows, error) {
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
        const columns = [...new Set((rows || []).flatMap(row => Object.keys(row || {})))].slice(0, 12);
        if (columns.length) {
            const head = document.createElement("thead");
            const tr = document.createElement("tr");
            columns.forEach(column => { const th = document.createElement("th"); th.textContent = column; tr.append(th); });
            head.append(tr); table.append(head);
            const body = document.createElement("tbody");
            (rows || []).slice(0, config.rowLimit || 20).forEach(row => {
                const tr = document.createElement("tr");
                columns.forEach(column => { const td = document.createElement("td"); td.textContent = get(row, column); tr.append(td); });
                body.append(tr);
            });
            table.append(body);
        }
        wrapper.append(table);
        element.append(wrapper);
    }

    function columnKind(config, field) {
        return String(config?.columnKinds?.[field] || config?.columnKinds?.[String(field || "").toLowerCase()] || "").toLowerCase();
    }

    function measure(config, row, field) {
        return number(get(row, field), columnKind(config, field));
    }

    function argumentValue(config, row, index) {
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
    }

    function argumentKey(value) {
        if (value instanceof Date) return `date:${value.getTime()}`;
        return `${typeof value}:${String(value)}`;
    }

    function argumentAxis(config) {
        const mode = String(config.argumentMode || "Auto").toLowerCase();
        const kind = columnKind(config, config.argumentField);
        if (mode === "datetime" || (mode === "auto" && kind === "datetime")) return { type: "continuous", argumentType: "datetime" };
        if (mode === "continuous" || (mode === "auto" && kind === "number")) return { type: "continuous", argumentType: "numeric" };
        return { type: "discrete", discreteAxisDivisionMode: "crossLabels" };
    }

    function rangeScale(config) {
        const mode = String(config.argumentMode || "Auto").toLowerCase();
        const kind = columnKind(config, config.argumentField);
        if (mode === "datetime" || (mode === "auto" && kind === "datetime")) return { type: "continuous", valueType: "datetime" };
        if (mode === "continuous" || (mode === "auto" && kind === "number")) return { type: "continuous", valueType: "numeric" };
        return { type: "discrete", valueType: "string", discreteAxisDivisionMode: "crossLabels" };
    }

    function sortVisualPoints(config, points) {
        const mode = String(config.sortMode || "DataOrder").toLowerCase();
        if (mode === "dataorder") return points;
        const direction = mode.endsWith("descending") ? -1 : 1;
        const byValue = mode.startsWith("value");
        return [...points].sort((left, right) => {
            const a = byValue ? number(left.value) : left.argument;
            const b = byValue ? number(right.value) : right.argument;
            const av = a instanceof Date ? a.getTime() : a;
            const bv = b instanceof Date ? b.getTime() : b;
            if (typeof av === "number" && typeof bv === "number") return (av - bv) * direction;
            return String(av).localeCompare(String(bv), undefined, { numeric: true, sensitivity: "base" }) * direction;
        });
    }

    function aggregateVisualPoints(config, points, enabled = true) {
        if (!enabled) return sortVisualPoints(config, points);
        const mode = String(config.aggregationMode || "Auto").toLowerCase();
        if (mode === "none") return sortVisualPoints(config, points);
        const groups = new Map();
        points.forEach((point, index) => {
            const key = `${String(point.series || "")}\u0000${argumentKey(point.argument)}`;
            const group = groups.get(key) || { ...point, value: 0, __values: [], __first: index };
            group.__values.push(number(point.value));
            groups.set(key, group);
        });
        const aggregated = [...groups.values()].map(group => {
            const values = group.__values;
            let value;
            switch (mode) {
                case "average": value = values.reduce((sum, item) => sum + item, 0) / Math.max(1, values.length); break;
                case "minimum": value = Math.min(...values); break;
                case "maximum": value = Math.max(...values); break;
                case "count": value = values.length; break;
                case "sum":
                case "auto":
                default: value = values.reduce((sum, item) => sum + item, 0); break;
            }
            const result = { ...group, value };
            delete result.__values;
            delete result.__first;
            return result;
        });
        return sortVisualPoints(config, aggregated);
    }

    function visualTooltip(config) {
        const element = config?.__element;
        const exportedOwner = element?.closest?.(".website-publication [data-publication-element]");
        return exportedOwner
            ? { enabled: true, container: exportedOwner }
            : { enabled: true };
    }

    function common(config) {
        return {
            title: config.showTitle ? { text: config.title || "" } : undefined,
            legend: { visible: config.showLegend !== false },
            tooltip: visualTooltip(config),
            animation: { enabled: false },
            size: { width: elementSize(config).width, height: elementSize(config).height }
        };
    }

    function elementSize(config) {
        const element = config.__element;
        return { width: Math.max(1, element?.clientWidth || 1), height: Math.max(1, element?.clientHeight || 1) };
    }

    function seriesData(config, rows, oneValuePerGroup) {
        const argument = config.argumentField;
        const seriesField = config.seriesField;
        let values = config.valueFields?.length ? config.valueFields : [config.highValueField || config.closeValueField || "Value"];
        if (oneValuePerGroup) values = [values[0]];
        const result = [];
        if (seriesField) {
            const names = [...new Set(rows.map(row => String(get(row, seriesField))))];
            names.forEach(name => values.forEach(field => result.push({ name: values.length > 1 ? `${name} · ${field}` : name, field, series: name })));
        } else values.forEach(field => result.push({ name: values.length > 1 ? field : (config.title || field), field, series: null }));
        return { argument, seriesField, result };
    }

    function mapCartesianType(style) {
        const map = {
            Bar: "bar", Line: "line", Spline: "spline", Scatter: "scatter", Area: "area", SplineArea: "splinearea",
            StepLine: "stepline", StepArea: "steparea", StackedBar: "stackedbar", FullStackedBar: "fullstackedbar",
            StackedArea: "stackedarea", FullStackedArea: "fullstackedarea", StackedLine: "stackedline",
            FullStackedLine: "fullstackedline", StackedSpline: "stackedspline", FullStackedSpline: "fullstackedspline",
            StackedSplineArea: "stackedsplinearea", FullStackedSplineArea: "fullstackedsplinearea",
            RangeArea: "rangearea", RangeBar: "rangebar", Bubble: "bubble", Candlestick: "candlestick", Stock: "stock"
        };
        return map[style] || "bar";
    }

    function chartOptions(config, rows) {
        const type = mapCartesianType(config.cartesianStyle);
        const financial = ["candlestick", "stock"].includes(type);
        const range = ["rangearea", "rangebar"].includes(type);
        const bubble = type === "bubble";
        const { argument, result } = seriesData(config, rows, financial || range || bubble);
        const normalized = [];
        result.forEach(definition => rows.forEach((row, index) => {
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
        }));
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
    }

    function polarOptions(config, rows) {
        const typeMap = { Line: "line", Area: "area", Bar: "bar", StackedBar: "stackedbar", Scatter: "scatter" };
        const type = typeMap[config.polarStyle] || "line";
        const { argument, result } = seriesData(config, rows, false);
        const normalized = [];
        result.forEach(definition => rows.forEach((row, index) => {
            if (definition.series !== null && String(get(row, config.seriesField)) !== definition.series) return;
            normalized.push({
                argument: argumentValue(config, row, index),
                series: definition.name,
                value: measure(config, row, definition.field)
            });
        }));
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
    }

    function renderWidget(element, config, rows) {
        config.__element = element;
        if (!window.jQuery || !window.DevExpress) { fallback(element, config, rows, "DevExtreme browser assets are not loaded."); return null; }
        const $element = window.jQuery(element);
        const kind = String(config.kind || "CartesianChart");
        const valueField = config.valueFields?.[0] || config.highValueField || config.closeValueField || "Value";
        const rawPoints = rows.map((row, index) => ({ argument: argumentValue(config, row, index), value: measure(config, row, valueField) }));
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
                plugin = "dxBarGauge"; options = Object.assign(common(config), { startValue: config.minimumValue, endValue: config.maximumValue, values: points.map(point => point.value) }); break;
            case "CircularGauge":
                plugin = "dxCircularGauge"; options = Object.assign(common(config), { value: points[0]?.value || 0, subvalues: points.slice(1).map(point => point.value), scale: { startValue: config.minimumValue, endValue: config.maximumValue }, title: config.showTitle ? { text: config.title || "" } : undefined }); break;
            case "LinearGauge":
                plugin = "dxLinearGauge"; options = Object.assign(common(config), { value: points[0]?.value || 0, subvalues: points.slice(1).map(point => point.value), scale: { startValue: config.minimumValue, endValue: config.maximumValue }, title: config.showTitle ? { text: config.title || "" } : undefined }); break;
            case "RangeSelector":
                plugin = "dxRangeSelector"; options = { dataSource: points, chart: { series: { argumentField: "argument", valueField: "value", type: mapCartesianType(config.cartesianStyle) } }, scale: rangeScale(config), size: elementSize(config), title: config.showTitle ? config.title : undefined }; break;
            case "Sankey":
                plugin = "dxSankey"; options = Object.assign(common(config), { dataSource: rows.map(row => ({ source: String(get(row, config.argumentField)), target: String(get(row, config.targetField)), weight: measure(config, row, valueField) })), sourceField: "source", targetField: "target", weightField: "weight", label: { visible: !!config.showLabels } }); break;
            case "Funnel":
            case "Pyramid":
                plugin = "dxFunnel"; options = Object.assign(common(config), { dataSource: points, argumentField: "argument", valueField: "value", inverted: kind === "Pyramid", label: { visible: !!config.showLabels } }); break;
            case "TreeMap":
                plugin = "dxTreeMap"; options = Object.assign(common(config), { dataSource: rows.map((row, index) => ({ id: String(get(row, config.argumentField) || index), parent: String(get(row, config.parentField)), label: String(get(row, config.argumentField)), value: measure(config, row, valueField) })), idField: "id", parentField: "parent", labelField: "label", valueField: "value", tooltip: visualTooltip(config) }); break;
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
    }

    function escapeHtml(value) {
        return String(value ?? "").replace(/[&<>"']/g, character => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[character]);
    }

    async function render(element, rawConfig, options) {
        element = visualRoot(element) || element;
        if (!element) return;
        const config = decodeConfig(rawConfig || element.dataset.psVisualConfig);
        if (!config) return;
        const prior = states.get(element);
        if (prior?.timer) clearInterval(prior.timer);
        clearVisualInteraction(prior);
        try { prior?.instance?.dispose?.(); } catch { }
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
            state.timer = setInterval(async () => {
                try {
                    const nextRows = await fetchRows(config);
                    if (!nextRows) return;
                    state.rows = nextRows;
                    try { state.instance?.dispose?.(); } catch { }
                    element.replaceChildren();
                    state.instance = renderWidget(element, config, nextRows);
                } catch (exception) {
                    if (!config.live.useSnapshotOnFailure) fallback(element, config, state.rows, exception?.message || String(exception));
                }
            }, Math.max(1, interval) * 1000);
        }
    }

    async function refreshAll(root, options) {
        const elements = [...(root || document).querySelectorAll("[data-ps-visual-config]")];
        await Promise.all(elements.map(element => render(element, element.dataset.psVisualConfig, { polling: options?.polling, fetchNow: true })));
    }

    function start(root, options) {
        bindPointerOwnership();
        const scope = root || document;
        scope.querySelectorAll("[data-ps-visual-config]").forEach(element => render(element, element.dataset.psVisualConfig, { polling: options?.polling !== false, fetchNow: options?.fetchNow !== false }));
    }

    window.PublisherStudioLiveDataRuntime = {
        renderVisualById(id, config) { bindPointerOwnership(); return render(document.getElementById(id), config, { polling: false, fetchNow: false }); },
        disposeById(id) { const element = document.getElementById(id); if (element) disposeWidget(element); },
        render,
        start,
        refreshAll,
        dispose(root) {
            if (!root) return;
            const elements = root.matches?.("[data-ps-visual-config]") ? [root] : [...root.querySelectorAll?.("[data-ps-visual-config]") || []];
            elements.forEach(disposeWidget);
        },
        setDataBaseUrl(value) {
            window.PublisherStudioDataBaseUrl = value || "";
            try {
                if (value) localStorage.setItem("PublisherStudioDataBaseUrl", value);
                else localStorage.removeItem("PublisherStudioDataBaseUrl");
            } catch { }
        }
    };
})();
