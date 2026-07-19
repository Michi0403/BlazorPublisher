(function () {
    "use strict";

    const states = new Map();
    const decoder = new TextDecoder();

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

    function number(value) {
        if (typeof value === "number") return Number.isFinite(value) ? value : 0;
        let normalized = String(value ?? "").trim().replace(/[\s'’]/g, "");
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
        if (!Number.isFinite(parsed)) return 0;
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
            return current[segment];
        }, value);
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
        let format = String(live.responseFormat || "Auto").toLowerCase();
        if (format === "auto") {
            const trimmed = text.trimStart();
            format = contentType.includes("json") || trimmed.startsWith("{") || trimmed.startsWith("[") ? "json"
                : contentType.includes("xml") || trimmed.startsWith("<") ? "xml" : "delimitedtext";
        }
        if (format === "json") {
            let value = selectJsonPath(JSON.parse(text), live.jsonPath);
            if (value && !Array.isArray(value) && Array.isArray(value.data)) value = value.data;
            else if (value && !Array.isArray(value) && Array.isArray(value.rows)) value = value.rows;
            if (!Array.isArray(value)) value = [value];
            return value.filter(item => item && typeof item === "object");
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

    function common(config) {
        return {
            title: config.showTitle ? { text: config.title || "" } : undefined,
            legend: { visible: config.showLegend !== false },
            tooltip: { enabled: true },
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
        result.forEach(definition => rows.forEach(row => {
            if (definition.series !== null && String(get(row, config.seriesField)) !== definition.series) return;
            normalized.push({
                argument: get(row, argument),
                series: definition.name,
                value: number(get(row, definition.field)),
                low: number(get(row, config.lowValueField || definition.field)),
                high: number(get(row, config.highValueField || definition.field)),
                open: number(get(row, config.openValueField || definition.field)),
                close: number(get(row, config.closeValueField || definition.field)),
                size: Math.max(0, number(get(row, config.sizeField || definition.field)))
            });
        }));
        const commonSeriesSettings = { type, argumentField: "argument", label: { visible: !!config.showLabels } };
        if (financial) Object.assign(commonSeriesSettings, { openValueField: "open", highValueField: "high", lowValueField: "low", closeValueField: "close" });
        else if (range) Object.assign(commonSeriesSettings, { rangeValue1Field: "low", rangeValue2Field: "high" });
        else if (bubble) Object.assign(commonSeriesSettings, { valueField: "value", sizeField: "size" });
        else commonSeriesSettings.valueField = "value";
        return Object.assign(common(config), {
            dataSource: normalized,
            commonSeriesSettings,
            seriesTemplate: { nameField: "series" },
            argumentAxis: { discreteAxisDivisionMode: "crossLabels" }
        });
    }

    function polarOptions(config, rows) {
        const typeMap = { Line: "line", Area: "area", Bar: "bar", StackedBar: "stackedbar", Scatter: "scatter" };
        const type = typeMap[config.polarStyle] || "line";
        const { argument, result } = seriesData(config, rows, false);
        const normalized = [];
        result.forEach(definition => rows.forEach(row => {
            if (definition.series !== null && String(get(row, config.seriesField)) !== definition.series) return;
            normalized.push({
                argument: get(row, argument),
                series: definition.name,
                value: number(get(row, definition.field))
            });
        }));
        return Object.assign(common(config), {
            dataSource: normalized,
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
        const points = rows.map(row => ({ argument: get(row, config.argumentField), value: number(get(row, valueField)) }));
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
                options = { dataSource: points, argumentField: "argument", valueField: "value", type: typeMap[config.sparklineStyle] || "line", tooltip: { enabled: true }, size: elementSize(config) };
                break;
            }
            case "BarGauge":
                plugin = "dxBarGauge"; options = Object.assign(common(config), { startValue: config.minimumValue, endValue: config.maximumValue, values: points.map(point => point.value) }); break;
            case "CircularGauge":
                plugin = "dxCircularGauge"; options = Object.assign(common(config), { value: points[0]?.value || 0, subvalues: points.slice(1).map(point => point.value), scale: { startValue: config.minimumValue, endValue: config.maximumValue }, title: config.showTitle ? { text: config.title || "" } : undefined }); break;
            case "LinearGauge":
                plugin = "dxLinearGauge"; options = Object.assign(common(config), { value: points[0]?.value || 0, subvalues: points.slice(1).map(point => point.value), scale: { startValue: config.minimumValue, endValue: config.maximumValue }, title: config.showTitle ? { text: config.title || "" } : undefined }); break;
            case "RangeSelector":
                plugin = "dxRangeSelector"; options = { dataSource: points, chart: { series: { argumentField: "argument", valueField: "value", type: mapCartesianType(config.cartesianStyle) } }, scale: {}, size: elementSize(config), title: config.showTitle ? config.title : undefined }; break;
            case "Sankey":
                plugin = "dxSankey"; options = Object.assign(common(config), { dataSource: rows.map(row => ({ source: String(get(row, config.argumentField)), target: String(get(row, config.targetField)), weight: number(get(row, valueField)) })), sourceField: "source", targetField: "target", weightField: "weight", label: { visible: !!config.showLabels } }); break;
            case "Funnel":
            case "Pyramid":
                plugin = "dxFunnel"; options = Object.assign(common(config), { dataSource: points, argumentField: "argument", valueField: "value", inverted: kind === "Pyramid", label: { visible: !!config.showLabels } }); break;
            case "TreeMap":
                plugin = "dxTreeMap"; options = Object.assign(common(config), { dataSource: rows.map((row, index) => ({ id: String(get(row, config.argumentField) || index), parent: String(get(row, config.parentField)), label: String(get(row, config.argumentField)), value: number(get(row, valueField)) })), idField: "id", parentField: "parent", labelField: "label", valueField: "value", tooltip: { enabled: true } }); break;
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
        const scope = root || document;
        scope.querySelectorAll("[data-ps-visual-config]").forEach(element => render(element, element.dataset.psVisualConfig, { polling: options?.polling !== false, fetchNow: options?.fetchNow !== false }));
    }

    window.PublisherStudioLiveDataRuntime = {
        renderVisualById(id, config) { return render(document.getElementById(id), config, { polling: false, fetchNow: false }); },
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
