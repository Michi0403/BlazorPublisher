(() => {
    "use strict";

    const states = new WeakMap();
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
        Button: "dxButton"
    };

    const lower = value => String(value ?? "").replace(/[^a-z0-9]/gi, "").toLowerCase();
    const bool = value => value === true || String(value).toLowerCase() === "true";
    const number = (value, fallback = 0) => {
        const parsed = Number(value);
        return Number.isFinite(parsed) ? parsed : fallback;
    };
    const escapeHtml = value => String(value ?? "").replace(/[&<>"']/g, character => ({
        "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
    })[character]);

    function decodeConfig(value) {
        if (!value) return null;
        if (typeof value === "object") return value;
        const source = String(value);
        try {
            const binary = atob(source);
            const bytes = Uint8Array.from(binary, character => character.charCodeAt(0));
            return JSON.parse(new TextDecoder().decode(bytes));
        } catch {
            try { return JSON.parse(source); } catch { return null; }
        }
    }

    function clone(value) {
        if (value === undefined) return undefined;
        if (typeof structuredClone === "function") {
            try { return structuredClone(value); } catch { }
        }
        return JSON.parse(JSON.stringify(value));
    }

    function deepMerge(target, source) {
        if (!source || typeof source !== "object" || Array.isArray(source)) return target;
        for (const [key, value] of Object.entries(source)) {
            if (value && typeof value === "object" && !Array.isArray(value)) {
                const current = target[key] && typeof target[key] === "object" && !Array.isArray(target[key]) ? target[key] : {};
                target[key] = deepMerge(current, value);
            } else target[key] = value;
        }
        return target;
    }

    function advancedOptions(config) {
        try {
            const value = JSON.parse(config.advancedOptionsJson || "{}");
            return value && typeof value === "object" && !Array.isArray(value) ? value : {};
        } catch { return {}; }
    }

    function dataBaseUrl() {
        const query = new URLSearchParams(location.search).get("publisherApi");
        let stored = "";
        try { stored = localStorage.getItem("PublisherStudioDataBaseUrl") || ""; } catch { }
        const configured = query || window.PublisherStudioDataBaseUrl || stored;
        if (configured) return String(configured).replace(/\/$/, "");
        return /^https?:$/.test(location.protocol) ? location.origin : "";
    }

    function resolveUrl(value) {
        const url = String(value || "").trim();
        if (!url) return "";
        try { return new URL(url).toString(); } catch { }
        const base = dataBaseUrl();
        if (!base) return "";
        try { return new URL(url.replace(/^\//, ""), base + "/").toString(); } catch { return ""; }
    }

    function headersObject(headers) {
        const result = {};
        for (const header of headers || []) {
            const name = String(header.name || "").trim();
            if (name) result[name] = String(header.value ?? "");
        }
        return result;
    }

    function jsonPath(value, path) {
        if (!path) return value;
        return String(path).split(".").filter(Boolean).reduce((current, segment) => {
            if (current == null) return undefined;
            if (Array.isArray(current) && /^\d+$/.test(segment)) return current[Number(segment)];
            if (typeof current !== "object") return undefined;
            if (Object.prototype.hasOwnProperty.call(current, segment)) return current[segment];
            const key = Object.keys(current).find(candidate => candidate.toLowerCase() === segment.toLowerCase());
            return key ? current[key] : undefined;
        }, value);
    }

    function unwrapJson(value) {
        let current = value;
        for (let index = 0; index < 3 && typeof current === "string"; index++) {
            const trimmed = current.trim();
            if (!trimmed.startsWith("{") && !trimmed.startsWith("[")) break;
            try { current = JSON.parse(trimmed); } catch { break; }
        }
        return current;
    }

    function normalizeRows(value, path = "") {
        let current = unwrapJson(jsonPath(value, path));
        if (Array.isArray(current)) return current;
        if (!current || typeof current !== "object") return [];
        for (const name of ["data", "items", "results", "records", "rows", "value"]) {
            const key = Object.keys(current).find(candidate => candidate.toLowerCase() === name);
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
    }

    function appendLoadOptions(url, loadOptions) {
        const result = new URL(url);
        const keys = ["filter", "group", "groupSummary", "parentIds", "requireGroupCount", "requireTotalCount", "searchExpr", "searchOperation", "searchValue", "select", "sort", "skip", "take", "totalSummary", "userData"];
        for (const key of keys) {
            const value = loadOptions?.[key];
            if (value === undefined || value === null || value === "") continue;
            result.searchParams.set(key, typeof value === "string" ? value : JSON.stringify(value));
        }
        return result.toString();
    }

    async function readResponse(response, path = "") {
        const text = await response.text();
        if (!response.ok) throw new Error(`Endpoint returned ${response.status} ${response.statusText}.`);
        if (!text.trim()) return { raw: null, rows: [] };
        const contentType = String(response.headers.get("content-type") || "").toLowerCase();
        if (contentType.includes("json") || /^[\s]*[\[{\"]/.test(text)) {
            const value = unwrapJson(JSON.parse(text));
            return { raw: value, rows: normalizeRows(value, path) };
        }
        return { raw: text, rows: [{ Value: text }] };
    }

    async function fetchDataObjectLive(live) {
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
    }

    function endpointWithKey(url, key, appendKey) {
        const resolved = resolveUrl(url);
        if (!resolved || !appendKey || key === undefined || key === null || key === "") return resolved;
        return resolved.replace(/\/$/, "") + "/" + encodeURIComponent(typeof key === "object" ? JSON.stringify(key) : String(key));
    }

    function requestBody(values) {
        return JSON.stringify(values ?? {});
    }

    function createRestStore(config, rows) {
        const connection = config.connection || {};
        const key = connection.keyField || config.keyField || "id";
        const rawMode = lower(connection.processingMode) !== "remote";
        const storeOptions = {
            key,
            loadMode: rawMode ? "raw" : "processed",
            cacheRawData: false,
            async load(loadOptions) {
                if (connection.allowLoad === false) return rows;
                const liveRows = await fetchDataObjectLive(connection.dataObjectLive);
                if (liveRows) return liveRows;
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
                    return { data: result.rows, totalCount: number(result.raw.totalCount, result.rows.length), summary: result.raw.summary, groupCount: result.raw.groupCount };
                }
                return result.rows;
            }
        };
        if (connection.allowInsert) storeOptions.insert = async values => {
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
        };
        if (connection.allowUpdate) storeOptions.update = async (itemKey, values) => {
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
        };
        if (connection.allowDelete) storeOptions.remove = async itemKey => {
            const url = endpointWithKey(connection.deleteUrl || connection.url, itemKey, connection.appendKeyToWriteUrl !== false);
            if (!url) throw new Error("Delete endpoint is not configured.");
            const response = await fetch(url, {
                method: String(connection.deleteMethod || "DELETE").toUpperCase(),
                headers: headersObject(connection.headers),
                credentials: connection.withCredentials ? "include" : "same-origin"
            });
            if (!response.ok) throw new Error(`Delete endpoint returned ${response.status} ${response.statusText}.`);
            return itemKey;
        };
        return new DevExpress.data.CustomStore(storeOptions);
    }

    function createODataStore(config) {
        const connection = config.connection || {};
        const url = resolveUrl(connection.url);
        if (!url) return null;
        const headers = headersObject(connection.headers);
        return new DevExpress.data.ODataStore({
            url,
            key: connection.keyField || config.keyField || "id",
            keyType: connection.keyType || "Int32",
            version: number(connection.oDataVersion, 4),
            beforeSend(request) {
                request.headers = { ...(request.headers || {}), ...headers };
                request.withCredentials = !!connection.withCredentials;
            },
            errorHandler(error) { showError(error?.message || String(error)); }
        });
    }

    function createData(config) {
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
        if (key && rows.every(row => row && Object.prototype.hasOwnProperty.call(row, key))) {
            const store = new DevExpress.data.ArrayStore({ key, data: rows });
            return { dataSource: store, store };
        }
        return { dataSource: rows, store: null };
    }

    async function materializeRows(config, data) {
        const mode = lower(config.connection?.mode);
        const requiresBrowserLoad = mode === "rest" || mode === "odata" || config.connection?.dataObjectLive?.enabled;
        if (!requiresBrowserLoad) return Array.isArray(config.rows) ? clone(config.rows) : [];
        const source = data?.store || data?.dataSource;
        if (!source?.load) return Array.isArray(config.rows) ? clone(config.rows) : [];
        const loaded = await source.load({ take: Math.max(1, number(config.pageSize, 100)) });
        if (Array.isArray(loaded)) return loaded;
        if (Array.isArray(loaded?.data)) return loaded.data;
        return normalizeRows(loaded, config.connection?.jsonPath || "");
    }

    function menuItems(config, rows) {
        const values = Array.isArray(rows) ? rows : [];
        return config.parentField && values.some(row => row?.[config.parentField] !== undefined)
            ? hierarchy(values, config.keyField || "id", config.parentField)
            : values;
    }

    function fieldType(field) {
        switch (lower(field?.valueKind)) {
            case "number": return "number";
            case "boolean": return "boolean";
            case "datetime": return "date";
            default: return "string";
        }
    }

    function editorName(field) {
        const explicit = String(field?.editor || "Auto");
        if (lower(explicit) !== "auto") return `dx${explicit}`;
        switch (fieldType(field)) {
            case "number": return "dxNumberBox";
            case "boolean": return "dxCheckBox";
            case "date": return "dxDateBox";
            default: return "dxTextBox";
        }
    }

    function lookupOptions(field) {
        const lookup = field?.lookup;
        if (!Array.isArray(lookup?.rows)) return null;
        return {
            dataSource: lookup.rows,
            valueExpr: lookup.valueExpr || field.lookupDataField || field.dataField,
            displayExpr: lookup.displayExpr || field.lookupDisplayField || field.lookupDataField || field.dataField
        };
    }

    function primaryField(config) {
        const visible = (config.fields || []).find(field => field.visible !== false);
        return config.valueField || visible?.dataField || "value";
    }

    function configuredValue(config) {
        if (config.initialValue !== undefined && config.initialValue !== null && String(config.initialValue) !== "") return config.initialValue;
        const row = Array.isArray(config.rows) ? config.rows[0] : null;
        return row?.[primaryField(config)] ?? null;
    }

    function columns(config) {
        return (config.fields || []).filter(field => field.visible !== false).map(field => {
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
        });
    }

    function editOptions(config) {
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
    }

    function selectionOptions(config) {
        const mode = lower(config.selectionMode);
        return { mode: mode === "multiple" ? "multiple" : mode === "single" ? "single" : "none", showCheckBoxesMode: mode === "multiple" ? "onClick" : "none" };
    }

    function actionsFor(config, trigger) {
        const normalized = lower(trigger);
        return (config.actions || []).filter(action => lower(action.trigger) === normalized && lower(action.action) !== "none");
    }

    function actionFor(config, trigger) {
        return actionsFor(config, trigger)[0] || null;
    }

    function template(value, data) {
        return String(value || "").replace(/\{\{\s*([^}]+?)\s*\}\}/g, (_, field) => {
            const key = Object.keys(data || {}).find(candidate => candidate.toLowerCase() === String(field).toLowerCase());
            return key ? String(data[key] ?? "") : "";
        });
    }

    function navigateToPage(pageId) {
        const api = window.PublisherStudioNavigation || window.PublisherStudioPresentation;
        if (api?.goToPage) return api.goToPage(pageId);
        window.dispatchEvent(new CustomEvent("publisherstudio:navigate", { detail: { pageId } }));
    }

    function showError(message) {
        if (window.DevExpress?.ui?.notify) window.DevExpress.ui.notify(String(message || "Unknown error"), "error", 4500);
        else console.error(message);
    }

    function showSuccess(message) {
        if (window.DevExpress?.ui?.notify) window.DevExpress.ui.notify(String(message || "Done"), "success", 2500);
    }

    async function executeAction(config, action, context) {
        if (!action || lower(action.action) === "none") return;
        if (action.confirmationText && !window.confirm(template(action.confirmationText, context.data))) return;
        const kind = lower(action.action);
        try {
            if (kind === "nextpage") (window.PublisherStudioNavigation || window.PublisherStudioPresentation)?.next?.();
            else if (kind === "previouspage") (window.PublisherStudioNavigation || window.PublisherStudioPresentation)?.previous?.();
            else if (kind === "gotopage") {
                const field = config.targetPageField || "targetPageId";
                navigateToPage(action.targetPageId || context.data?.[field] || context.itemData?.[field]);
            } else if (kind === "openurl") {
                const field = config.urlField || "url";
                const url = template(action.url || context.data?.[field] || context.itemData?.[field] || "", context.data);
                if (/^(https?:|mailto:)/i.test(url)) window.open(url, action.openInNewWindow === false ? "_self" : "_blank", "noopener");
                else throw new Error("Only http, https and mailto links are allowed.");
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
                await handler(Object.freeze({ ...context, config: clone(config) }));
            }
        } catch (error) {
            showError(error?.message || String(error));
            throw error;
        }
    }

    async function executeActions(config, actions, context) {
        for (const action of actions || []) await executeAction(config, action, context);
    }

    function eventContext(config, instance, dataSource, event, data) {
        const instanceElement = instance?.element?.()?.get?.(0) || instance?.element?.()?.[0] || instance?.element?.();
        const host = instanceElement?.closest?.("[data-ps-component-config]")
            || document.querySelector(`[data-ps-component-id="${CSS.escape(String(config.id || ""))}"]`);
        let eventData = data || event?.data || event?.itemData || event?.appointmentData || event?.selectedRowsData?.[0] || null;
        if (!eventData && event && Object.prototype.hasOwnProperty.call(event, "value")) {
            eventData = { [primaryField(config)]: event.value, value: event.value };
        }
        if (!eventData) eventData = Array.isArray(config.rows) ? clone(config.rows[0] || {}) : {};
        return { config, instance, dataSource, host, event, data: eventData, itemData: event?.itemData || null, value: event?.value };
    }

    function bindCommonActions(config, options, dataSource) {
        const handlers = [
            ["ItemClick", "onItemClick"],
            ["SelectionChanged", "onSelectionChanged"],
            ["ValueChanged", "onValueChanged"],
            ["RowInserted", "onRowInserted"],
            ["RowUpdated", "onRowUpdated"],
            ["RowRemoved", "onRowRemoved"],
            ["AppointmentAdded", "onAppointmentAdded"],
            ["AppointmentUpdated", "onAppointmentUpdated"],
            ["AppointmentDeleted", "onAppointmentDeleted"]
        ];
        for (const [trigger, eventName] of handlers) {
            const actions = actionsFor(config, trigger);
            if (!actions.length) continue;
            const prior = options[eventName];
            options[eventName] = event => {
                prior?.(event);
                executeActions(config, actions, eventContext(config, event.component, dataSource, event));
            };
        }
    }

    function renderCard(item, config, element, tile = false) {
        const image = item?.[config.imageField || "image"];
        const title = item?.[config.displayField || config.textField || "text"] ?? item?.[config.valueField || "value"] ?? "";
        const subtitleField = (config.fields || []).find(field => field.visible !== false && ![config.imageField, config.displayField, config.textField].includes(field.dataField))?.dataField;
        const subtitle = subtitleField ? item?.[subtitleField] : "";
        const wrapper = document.createElement("article");
        wrapper.className = tile ? "ps-component-tile" : "ps-component-gallery-item";
        if (image) {
            const img = document.createElement("img");
            img.src = String(image);
            img.alt = String(title || "Image");
            wrapper.append(img);
        }
        const body = document.createElement("div");
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
    }

    function hierarchy(rows, keyField, parentField) {
        const byKey = new Map();
        const roots = [];
        for (const row of rows || []) byKey.set(String(row?.[keyField] ?? ""), { ...row, items: [] });
        for (const item of byKey.values()) {
            const parent = String(item[parentField] ?? "");
            if (parent && byKey.has(parent) && parent !== String(item[keyField] ?? "")) byKey.get(parent).items.push(item);
            else roots.push(item);
        }
        return roots;
    }

    function formItems(config) {
        return (config.fields || []).filter(field => field.visible !== false).map(field => {
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
        });
    }

    function createNestedConfig(parentConfig, panel) {
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
    }

    function renderPanelContent(parentConfig, panel, itemElement) {
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
    }

    function baseOptions(config, element) {
        return {
            width: "100%",
            height: "100%",
            disabled: false,
            elementAttr: { class: `ps-dx-${lower(config.kind)}` },
            hint: config.title || undefined
        };
    }

    function buildOptions(config, element, data) {
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
                        onClick: event => {
                            const form = window.jQuery(element).dxForm("instance");
                            const validation = form?.validate?.();
                            if (validation && validation.isValid === false) return;
                            const currentData = form?.option?.("formData") || formData;
                            return executeActions(config, submitActions, eventContext(config, form, dataSource, event, currentData));
                        }
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
                options = { ...base, value: value ? new Date(value) : null, placeholder: config.placeholder || undefined, type: "datetime", showClearButton: true };
                break;
            }
            case "CheckBox": options = { ...base, value: bool(configuredValue(config)), text: config.title || config.buttonText || "Option" }; break;
            case "SelectBox": options = { ...base, dataSource, value: configuredValue(config), displayExpr: config.displayField || config.textField, valueExpr: config.valueField || config.keyField, placeholder: config.placeholder || undefined, searchEnabled: true, showClearButton: true }; break;
            case "TagBox": {
                const value = configuredValue(config);
                const values = Array.isArray(value) ? value : value === null || value === undefined || value === "" ? [] : String(value).split(",").map(item => item.trim()).filter(Boolean);
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
                    itemTemplate(item, index, itemElement) { renderCard(item, config, itemElement?.jquery ? itemElement[0] : itemElement, false); }
                };
                break;
            case "TileView":
                options = {
                    ...base,
                    dataSource,
                    baseItemHeight: 120,
                    baseItemWidth: 180,
                    itemMargin: 8,
                    direction: "horizontal",
                    itemTemplate(item, index, itemElement) { renderCard(item, config, itemElement?.jquery ? itemElement[0] : itemElement, true); }
                };
                break;
            case "Menu": {
                const items = menuItems(config, config.rows);
                options = { ...base, height: undefined, items, dataSource: undefined, displayExpr: config.displayField || config.textField || "text", orientation: config.orientation || "horizontal", hideSubmenuOnMouseLeave: true };
                break;
            }
            case "ContextMenu": {
                const items = menuItems(config, config.rows);
                options = { items, displayExpr: config.displayField || config.textField || "text", target: element, showEvent: "dxcontextmenu", width: 240 };
                break;
            }
            case "TabPanel":
                options = { ...base, items: config.panels || [], itemTitleTemplate(item, index, itemElement) { const target = itemElement?.jquery ? itemElement[0] : itemElement; target.textContent = item.title || `Tab ${index + 1}`; }, itemTemplate(item, index, itemElement) { renderPanelContent(config, item, itemElement); }, animationEnabled: true, swipeEnabled: true, deferRendering: false };
                break;
            case "MultiView":
                options = { ...base, items: config.panels || [], itemTemplate(item, index, itemElement) { renderPanelContent(config, item, itemElement); }, animationEnabled: true, swipeEnabled: true, loop: false, deferRendering: false };
                break;
            case "Splitter":
                options = {
                    ...base,
                    orientation: config.orientation || "horizontal",
                    items: (config.panels || []).map(panel => ({
                        ...panel,
                        size: panel.size || undefined,
                        minSize: panel.minSize || undefined,
                        maxSize: panel.maxSize || undefined,
                        collapsible: panel.collapsible !== false,
                        collapsed: !!panel.collapsed,
                        template(itemData, itemIndex, itemElement) { renderPanelContent(config, panel, itemElement); }
                    }))
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
                options = { ...base, direction: config.orientation === "horizontal" ? "horizontal" : "vertical", showScrollbar: "onHover", bounceEnabled: true, useNative: false };
                break;
            }
            case "PivotGrid": {
                const fields = (config.fields || []).filter(field => field.visible !== false && lower(field.area) !== "none").map(field => ({
                    dataField: field.dataField,
                    caption: field.caption || field.dataField,
                    dataType: fieldType(field),
                    area: lower(field.area),
                    summaryType: lower(field.summaryType) || "sum",
                    format: field.format || undefined
                }));
                const pivotSource = new DevExpress.data.PivotGridDataSource({ fields, store: data.store || dataSource });
                data.pivotSource = pivotSource;
                options = { ...base, dataSource: pivotSource, allowSortingBySummary: true, allowFiltering: config.allowFiltering !== false, allowSorting: config.allowSorting !== false, allowExpandAll: true, showBorders: config.showBorders !== false, showColumnGrandTotals: true, showRowGrandTotals: true, fieldChooser: { enabled: true, height: 400 }, scrolling: { mode: "virtual" } };
                break;
            }
            case "Button":
                options = { ...base, text: config.buttonText || config.title || "Run", type: "default", stylingMode: "contained", onClick: event => executeActions(config, actionsFor(config, "Click"), eventContext(config, event.component, dataSource, event, clone(config.rows?.[0] || {}))) };
                break;
            default: throw new Error(`Unsupported PublisherStudio component: ${kind}`);
        }
        bindCommonActions(config, options, dataSource);
        return deepMerge(options, advancedOptions(config));
    }

    async function refreshState(element, state) {
        if (!state?.instance) return;
        const kind = String(state.config?.kind || "");
        if (["Form", "Menu", "ContextMenu", "TextBox", "TextArea", "NumberBox", "DateBox", "CheckBox"].includes(kind)) {
            const rows = await materializeRows(state.config, state.data);
            state.config.rows = rows;
            if (kind === "Form") state.instance.option?.("formData", clone(rows[0] || {}));
            else if (kind === "Menu" || kind === "ContextMenu") state.instance.option?.("items", menuItems(state.config, rows));
            else {
                const value = configuredValue(state.config);
                state.instance.option?.("value", kind === "DateBox" && value ? new Date(value) : kind === "NumberBox" ? number(value, 0) : kind === "CheckBox" ? bool(value) : value ?? "");
            }
            state.instance.repaint?.();
            return;
        }
        const source = state.instance.getDataSource?.() || state.dataSource;
        if (source?.reload) await source.reload();
        else if (source?.load) await source.load();
        await state.instance.refresh?.();
        state.instance.repaint?.();
    }

    function dispose(element) {
        const state = states.get(element);
        if (state?.timer) clearInterval(state.timer);
        for (const child of [...element.querySelectorAll("[data-ps-component-runtime]")]) {
            if (child !== element) dispose(child);
        }
        try { state?.pivotSource?.dispose?.(); } catch { }
        try { state?.instance?.dispose?.(); } catch { }
        states.delete(element);
        element.replaceChildren();
    }

    async function render(element, rawConfig, options = {}) {
        if (!element) return null;
        const config = decodeConfig(rawConfig || element.dataset.psComponentConfig);
        if (!config) return null;
        dispose(element);
        element.dataset.psComponentRuntime = "true";
        element.dataset.psComponentId = String(config.id || "");
        element.classList.add("ps-component-runtime");
        if (!window.jQuery || !window.DevExpress) {
            element.innerHTML = '<div class="ps-component-error">DevExtreme browser assets are not loaded.</div>';
            return null;
        }
        let data = createData(config);
        if (options.fetchNow && config.connection?.dataObjectLive?.enabled) {
            try {
                const rows = await fetchDataObjectLive(config.connection.dataObjectLive);
                if (rows) {
                    config.rows = rows;
                    data = createData({ ...config, connection: { ...config.connection, dataObjectLive: null } });
                }
            } catch (error) { showError(error?.message || String(error)); }
        }
        try {
            if (["Form", "Menu", "ContextMenu", "TextBox", "TextArea", "NumberBox", "DateBox", "CheckBox"].includes(String(config.kind || "")))
                config.rows = await materializeRows(config, data);
            const plugin = pluginNames[config.kind];
            if (!plugin || typeof window.jQuery.fn[plugin] !== "function") throw new Error(`${config.kind} is not available in the bundled DevExtreme runtime.`);
            const optionsValue = buildOptions(config, element, data);
            const $element = window.jQuery(element);
            $element[plugin](optionsValue);
            const instance = $element[plugin]("instance");
            const state = { config, instance, data, dataSource: instance?.getDataSource?.() || data.dataSource, pivotSource: data.pivotSource, timer: null };
            states.set(element, state);
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
            element.innerHTML = `<div class="ps-component-error"><strong>${escapeHtml(config.title || config.kind)}</strong><span>${escapeHtml(error?.message || String(error))}</span></div>`;
            return null;
        }
    }

    async function probeConnection(connection) {
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
    }

    async function refreshAll(root, options = {}) {
        const scope = root || document;
        const elements = scope.matches?.("[data-ps-component-config]") ? [scope] : [...scope.querySelectorAll?.("[data-ps-component-config]") || []];
        await Promise.all(elements.map(element => render(element, element.dataset.psComponentConfig, { polling: options.polling, fetchNow: options.fetchNow !== false })));
    }

    function start(root, options = {}) {
        const scope = root || document;
        const elements = scope.matches?.("[data-ps-component-config]") ? [scope] : [...scope.querySelectorAll?.("[data-ps-component-config]") || []];
        elements.forEach(element => render(element, element.dataset.psComponentConfig, { polling: options.polling !== false, fetchNow: options.fetchNow !== false }));
    }

    window.PublisherStudioComponentRuntime = {
        render,
        renderById(id, config) { return render(document.getElementById(id), config, { polling: false, fetchNow: false }); },
        disposeById(id) { const element = document.getElementById(id); if (element) dispose(element); },
        refreshAll,
        probeConnection,
        start,
        dispose(root) {
            if (!root) return;
            const elements = root.matches?.("[data-ps-component-config]") ? [root] : [...root.querySelectorAll?.("[data-ps-component-config]") || []];
            elements.forEach(dispose);
        }
    };
})();
