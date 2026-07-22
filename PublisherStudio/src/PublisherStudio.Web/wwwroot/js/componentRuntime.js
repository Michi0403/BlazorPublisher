(() => {
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

    const lower = value => String(value ?? "").replace(/[^a-z0-9]/gi, "").toLowerCase();
    const bool = value => value === true || String(value).toLowerCase() === "true";
    const number = (value, fallback = 0) => {
        const parsed = Number(value);
        return Number.isFinite(parsed) ? parsed : fallback;
    };
    const validDate = value => {
        if (value === null || value === undefined || value === "") return null;
        if (value instanceof Date) return Number.isFinite(value.getTime()) ? value : null;
        const parsed = new Date(value);
        return Number.isFinite(parsed.getTime()) ? parsed : null;
    };
    const componentOrientation = (config, fallback = "horizontal") => {
        const value = lower(config?.orientation);
        if (value === "vertical") return "vertical";
        if (value === "horizontal") return "horizontal";
        return fallback;
    };

    function normalizeDateRows(config, rows) {
        if (!Array.isArray(rows)) return [];
        const dateFields = new Set((config.fields || [])
            .filter(column => lower(column?.valueKind) === "datetime")
            .map(column => String(column.dataField || "").trim())
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
        return rows.map(source => {
            if (!source || typeof source !== "object") return source;
            const row = { ...source };
            for (const name of dateFields) {
                const key = Object.keys(row).find(candidate => lower(candidate) === lower(name));
                if (!key) continue;
                row[key] = validDate(row[key]);
            }
            if (scheduler) {
                const startKey = Object.keys(row).find(candidate => lower(candidate) === lower(startField));
                const endKey = Object.keys(row).find(candidate => lower(candidate) === lower(endField));
                if (!startKey || !row[startKey]) return null;
                if (endKey && !row[endKey]) row[endKey] = new Date(row[startKey].getTime() + 60 * 60 * 1000);
            }
            return row;
        }).filter(row => row !== null);
    }
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
        const manual = lower(config.menuSourceMode) === "manualitems";
        const values = manual ? clone(config.menuItems || []) : (Array.isArray(rows) ? rows : []);
        const keyField = manual ? "id" : (config.keyField || "id");
        const parentField = manual ? "parentId" : config.parentField;
        const visible = values.filter(item => item?.visible !== false);
        return parentField && visible.some(row => valueFor(row, parentField) !== undefined && valueFor(row, parentField) !== null && String(valueFor(row, parentField)).trim() !== "")
            ? hierarchy(visible, keyField, parentField)
            : visible;
    }

    function renderBasicMenu(element, config, rows) {
        const root = document.createElement("div");
        root.className = `ps-basic-menu ps-basic-menu-${lower(config.orientation) === "vertical" ? "vertical" : "horizontal"}`;
        const currentRows = Array.isArray(rows) ? rows : [];
        let currentItems = menuItems(config, currentRows);
        const renderItems = (items, parent) => {
            for (const item of items || []) {
                const wrapper = document.createElement("div");
                wrapper.className = "ps-basic-menu-item";
                const button = document.createElement("button");
                button.type = "button";
                button.disabled = item?.disabled === true || item?.enabled === false;
                button.textContent = String(item?.text ?? item?.[config.displayField || config.textField || "text"] ?? "Menu item");
                button.addEventListener("click", event => {
                    event.stopPropagation();
                    let actions = actionsFor(config, "ItemClick");
                    if (!actions.length) actions = [{ trigger: "ItemClick", action: "Navigate", openInNewWindow: true }];
                    void executeActions(config, actions, eventContext(config, null, currentRows, { itemData: item, event }, item));
                });
                wrapper.append(button);
                if (Array.isArray(item?.items) && item.items.length) {
                    const children = document.createElement("div");
                    children.className = "ps-basic-menu-children";
                    renderItems(item.items, children);
                    wrapper.append(children);
                }
                parent.append(wrapper);
            }
        };
        const repaint = (items = currentItems) => {
            root.replaceChildren();
            renderItems(items, root);
        };
        repaint();
        element.replaceChildren(root);
        return {
            dispose() { root.remove(); },
            repaint() { repaint(); },
            option(name, value) {
                if (name === "items" && Array.isArray(value)) {
                    currentItems = value;
                    repaint();
                }
            }
        };
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

    function valueFor(data, field) {
        if (!data || !field) return undefined;
        if (Object.prototype.hasOwnProperty.call(data, field)) return data[field];
        const key = Object.keys(data).find(candidate => lower(candidate) === lower(field));
        return key ? data[key] : undefined;
    }

    function navigateToPage(pageId, config, context) {
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
    }

    function openComponentUrl(url, openInNewWindow, config, context) {
        const value = String(url || "").trim();
        if (!/^(https?:|mailto:)/i.test(value)) throw new Error("Only http, https and mailto links are allowed.");
        if (config?.designerMode) {
            const editorSurface = !!context?.host?.closest?.("#publisher-page");
            window.dispatchEvent(new CustomEvent("publisherstudio:open-url", { detail: { url: value, openInNewWindow: openInNewWindow !== false, componentId: config.id, editorSurface } }));
            return true;
        }
        window.open(value, openInNewWindow === false ? "_self" : "_blank", "noopener");
        return true;
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
        let eventData = data || event?.data || event?.itemData || event?.message || event?.appointmentData || event?.selectedRowsData?.[0] || null;
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
            ["AppointmentDeleted", "onAppointmentDeleted"],
            ["MessageEntered", "onMessageEntered"]
        ];
        for (const [trigger, eventName] of handlers) {
            let actions = actionsFor(config, trigger);
            if (!actions.length && trigger === "ItemClick" && ["menu", "contextmenu"].includes(lower(config.kind)))
                actions = [{ trigger: "ItemClick", action: "Navigate", openInNewWindow: true }];
            if (!actions.length) continue;
            const prior = options[eventName];
            options[eventName] = event => {
                prior?.(event);
                executeActions(config, actions, eventContext(config, event.component, dataSource, event));
            };
        }
    }

    function mediaKind(item, config, source) {
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
    }

    function allowedMediaSource(value) {
        const source = String(value || "").trim();
        if (!source) return "";
        if (/^(data:|blob:|https?:|\/|\.\.?\/)/i.test(source)) return source;
        return "";
    }

    function renderCard(item, config, element, tile = false) {
        const image = valueFor(item, config.imageField || "image");
        const source = allowedMediaSource(valueFor(item, config.mediaSourceField || "source") || image);
        const poster = allowedMediaSource(valueFor(item, config.mediaPosterField || "poster") || image);
        const title = valueFor(item, config.displayField || config.textField || "text") ?? valueFor(item, config.valueField || "value") ?? "";
        const altText = valueFor(item, config.mediaAltTextField || "altText") || title || "Media";
        const kind = mediaKind(item, config, source);
        const ignoredFields = [config.imageField, config.mediaSourceField, config.mediaPosterField, config.mediaKindField, config.mediaAltTextField, config.displayField, config.textField];
        const subtitleField = (config.fields || []).find(field => field.visible !== false && !ignoredFields.includes(field.dataField))?.dataField;
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
    }

    function syncMediaPlayback(element, config, activeItem = null) {
        const media = [...(element?.querySelectorAll?.("video,audio") || [])];
        for (const node of media) {
            const active = activeItem ? activeItem.contains?.(node) : !!node.closest?.(".dx-gallery-item-selected");
            if (!active) {
                try { node.pause?.(); } catch { }
                continue;
            }
            if (config.mediaAutoPlay) {
                try { node.play?.().catch?.(() => undefined); } catch { }
            }
        }
    }

    function activeChatPlatform(config, context = null) {
        const query = new URLSearchParams(location.search).get("publisherChatPlatform");
        const configuredValue = String(config.chatPlatform || "OutputContext");
        const runtimeContext = context || window.PublisherStudioOutputContext || {};
        const configured = query || (lower(configuredValue) === "outputcontext"
            ? (runtimeContext.platform || window.PublisherStudioChatPlatform || "Preview")
            : configuredValue);
        return String(configured).trim() || "Preview";
    }

    function chatBroadcastMode() {
        return lower(window.PublisherStudioOutputContext?.mode) === "broadcast";
    }

    function activeChatChannel(config, context = null) {
        const query = new URLSearchParams(location.search).get("publisherChatChannel");
        const outputContext = lower(config.chatPlatform || "OutputContext") === "outputcontext";
        const runtimeContext = context || window.PublisherStudioOutputContext || {};
        const configured = query || (outputContext
            ? (runtimeContext.channel || window.PublisherStudioChatChannel || config.chatChannel || "")
            : (config.chatChannel || ""));
        return String(configured).trim();
    }

    function chatUser(config) {
        return {
            id: String(config.chatCurrentUserId || "publisher"),
            name: String(config.chatCurrentUserName || "Streamer"),
            avatarUrl: allowedMediaSource(config.chatCurrentUserAvatar) || undefined
        };
    }

    function chatMessage(config, row, index = 0, context = null) {
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
    }

    function chatMessages(config, rows, context = null) {
        const platform = lower(activeChatPlatform(config, context));
        const channel = lower(activeChatChannel(config, context));
        return (Array.isArray(rows) ? rows : []).filter(row => {
            const rowPlatform = lower(valueFor(row, config.chatPlatformField || "platform"));
            const rowChannel = lower(valueFor(row, config.chatChannelField || "channel"));
            // Untagged preview rows are useful while designing, but never leak into a
            // Twitch/YouTube/Custom output. A selected channel also requires an exact match.
            const platformMatches = rowPlatform ? rowPlatform === platform : platform === "preview";
            const channelMatches = channel ? rowChannel === channel : true;
            return platformMatches && channelMatches;
        }).map((row, index) => chatMessage(config, row, index, context));
    }

    function chatMessageId(message) {
        return String(message?.id || "").trim();
    }

    function mergeChatItems(config, rows, transient = [], context = null) {
        const result = [];
        const ids = new Set();
        for (const message of [...chatMessages(config, rows, context), ...(transient || []).filter(message => {
            const platform = lower(activeChatPlatform(config, context));
            const channel = lower(activeChatChannel(config, context));
            const messagePlatform = lower(message?.platform);
            const messageChannel = lower(message?.channel);
            return (messagePlatform ? messagePlatform === platform : platform === "preview")
                && (channel ? messageChannel === channel : true);
        })]) {
            const id = chatMessageId(message);
            if (id && ids.has(id)) continue;
            if (id) ids.add(id);
            result.push(message);
        }
        return result;
    }

    function renderChatMessage(state, message) {
        if (!state || !message) return false;
        state.chatMessageIds ||= new Set();
        state.chatTransient ||= [];
        const id = chatMessageId(message);
        if (id && state.chatMessageIds.has(id)) return false;
        if (id) state.chatMessageIds.add(id);
        state.chatTransient.push(message);
        state.instance?.renderMessage?.(message);
        return true;
    }

    function applyChatInputState(element, config) {
        const input = element?.querySelector?.(".dx-chat-messagebox textarea,.dx-chat-messagebox input,textarea.dx-texteditor-input,input.dx-texteditor-input");
        if (input && config.placeholder) input.setAttribute("placeholder", String(config.placeholder));
    }

    function publishChatMessage(config, instance, message, element) {
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
        try { window.PublisherStudioChatBridge?.send?.(detail); } catch (error) { showError(error?.message || String(error)); }
        window.dispatchEvent(new CustomEvent("publisherstudio:chat-send", { detail }));
        element?.dispatchEvent?.(new CustomEvent("publisherstudio:chat-send", { detail, bubbles: true }));
    }

    function installChatSubscription(element, state) {
        const config = state.config;
        const accept = detail => {
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
        };
        const handler = event => accept(event.detail);
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
        state.chatUnsubscribe = () => {
            window.removeEventListener("publisherstudio:chat-message", handler);
            try { bridgeUnsubscribe?.(); } catch { }
        };
    }

    function chatBroadcastLayers(context = {}, pageElementId = "publisher-page") {
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
                .map(message => ({
                    id: String(message.id || ""),
                    text: String(message.text || ""),
                    authorName: String(message.author?.name || "Viewer"),
                    authorAvatar: String(message.author?.avatarUrl || ""),
                    timestamp: message.timestamp instanceof Date ? message.timestamp.toISOString() : String(message.timestamp || "")
                }));
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
                items
            });
        }
        return layers;
    }

    function hierarchy(rows, keyField, parentField) {
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

    function clearWidgetResidue(element) {
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
    }

    function applyLayoutClasses(element, config) {
        const kind = lower(config?.kind);
        const orientation = componentOrientation(config);
        element.classList?.remove?.("ps-component-orientation-horizontal", "ps-component-orientation-vertical");
        element.classList?.add?.(`ps-component-orientation-${orientation}`);
        element.dataset.psComponentKind = kind;
        element.dataset.psComponentOrientation = orientation;
    }

    function refreshNestedLayouts(element) {
        if (!element?.querySelectorAll) return;
        for (const child of element.querySelectorAll("[data-ps-component-runtime]")) {
            if (child === element) continue;
            const nested = states.get(child);
            nested?.layout?.schedule?.(true);
        }
    }

    function installLayoutObserver(element, state) {
        if (!element || !state?.instance) return;
        let frame = null;
        let lastWidth = -1;
        let lastHeight = -1;
        const defer = typeof window.setTimeout === "function" ? window.setTimeout.bind(window) : (callback => { callback(); return 0; });
        const clearDeferred = typeof window.clearTimeout === "function" ? window.clearTimeout.bind(window) : (() => undefined);
        const requestFrame = typeof window.requestAnimationFrame === "function" ? window.requestAnimationFrame.bind(window) : (callback => defer(callback, 0));
        const cancelFrame = typeof window.cancelAnimationFrame === "function" ? window.cancelAnimationFrame.bind(window) : clearDeferred;
        const run = force => {
            frame = null;
            const rect = element.getBoundingClientRect?.();
            const width = Math.round(number(rect?.width, element.clientWidth || 0) * 10) / 10;
            const height = Math.round(number(rect?.height, element.clientHeight || 0) * 10) / 10;
            if (!force && width === lastWidth && height === lastHeight) return;
            lastWidth = width;
            lastHeight = height;
            try { state.instance.updateDimensions?.(); } catch { }
            try { state.instance.repaint?.(); } catch { }
            refreshNestedLayouts(element);
        };
        const schedule = (force = false) => {
            if (frame !== null) cancelFrame(frame);
            frame = requestFrame(() => run(force));
        };
        const observer = typeof window.ResizeObserver === "function"
            ? new window.ResizeObserver(() => schedule(false))
            : null;
        let delayed = null;
        observer?.observe(element);
        state.layout = {
            observer,
            schedule,
            cancel() {
                observer?.disconnect();
                if (frame !== null) cancelFrame(frame);
                if (delayed !== null) clearDeferred(delayed);
                frame = null;
                delayed = null;
            }
        };
        schedule(true);
        delayed = defer(() => schedule(true), 60);
    }

    function baseOptions(config, element) {
        return {
            width: "100%",
            height: "100%",
            disabled: false,
            elementAttr: { class: `ps-dx-${lower(config.kind)} ps-component-orientation-${componentOrientation(config)}` },
            hint: config.title || undefined
        };
    }

    function rowLocation(config, row) {
        const latitude = number(row?.[config.latitudeField], NaN);
        const longitude = number(row?.[config.longitudeField], NaN);
        if (Number.isFinite(latitude) && Number.isFinite(longitude)) return { lat: latitude, lng: longitude };
        const address = row?.[config.addressField];
        return address == null || String(address).trim() === "" ? null : String(address);
    }

    function mapMarkers(config, rows) {
        return (rows || []).map(row => {
            const location = rowLocation(config, row);
            if (!location) return null;
            const text = row?.[config.markerTooltipField] ?? row?.[config.vectorLabelField] ?? "";
            return { location, tooltip: text ? { text: String(text), isShown: false } : undefined };
        }).filter(Boolean);
    }

    function mapRoutes(config, rows) {
        if (config.mapShowRoutes === false) return [];
        const groups = new Map();
        for (const row of rows || []) {
            const location = rowLocation(config, row);
            if (!location) continue;
            const key = String(row?.[config.mapRouteField] ?? "default");
            if (!groups.has(key)) groups.set(key, []);
            groups.get(key).push({ location, order: number(row?.[config.mapOrderField], groups.get(key).length) });
        }
        return [...groups.values()].filter(group => group.length > 1).map(group => ({
            locations: group.sort((a, b) => a.order - b.order).map(item => item.location),
            mode: "driving", opacity: .78, weight: 4
        }));
    }

    function sourceName(value) {
        const name = String(value || "world").toLowerCase();
        return name === "usa" ? "usa" : name;
    }

    function vectorGeoJson(config, rows, kind) {
        const features = [];
        for (const feature of config.vectorFeatures || []) {
            if (String(feature.kind || "").toLowerCase() !== kind) continue;
            const points = (feature.points || []).map(point => [number(point.longitude), number(point.latitude)]);
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
    }

    function vectorLayers(config, rows) {
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
            customize(elements) { elements.forEach(element => { const a = element.attribute("properties") || {}; element.applySettings({ color: a.color || "#2563eb", borderColor: a.borderColor || "#1e3a8a", opacity: number(a.opacity, .82) }); }); } });
        const lines = vectorGeoJson(config, rows, "line");
        if (lines.features.length) layers.push({ name: "lines", type: "line", dataSource: lines,
            customize(elements) { elements.forEach(element => { const a = element.attribute("properties") || {}; element.applySettings({ color: a.color || "#2563eb", width: number(a.width, 3), opacity: number(a.opacity, .9) }); }); } });
        const markers = vectorGeoJson(config, rows, "marker");
        if (markers.features.length) layers.push({ name: "markers", type: "marker", dataSource: markers,
            label: { enabled: config.vectorShowLabels !== false, dataField: "name" },
            customize(elements) { elements.forEach(element => { const a = element.attribute("properties") || {}; element.applySettings({ color: a.color || "#ef4444", size: number(a.size, 14) }); }); } });
        return layers;
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
                options = { ...base, value: validDate(value), placeholder: config.placeholder || undefined, type: "datetime", showClearButton: true };
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
                    itemTemplate(item, index, itemElement) { renderCard(item, config, itemElement?.jquery ? itemElement[0] : itemElement, false); },
                    onContentReady() { queueMicrotask(() => syncMediaPlayback(element, config)); },
                    onSelectionChanged() { queueMicrotask(() => syncMediaPlayback(element, config)); }
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
                    itemTemplate(item, index, itemElement) { renderCard(item, config, itemElement?.jquery ? itemElement[0] : itemElement, true); },
                    onItemClick(event) {
                        const itemElement = event?.itemElement?.jquery ? event.itemElement[0] : event?.itemElement;
                        syncMediaPlayback(element, config, itemElement || null);
                    }
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
                    itemTemplate(itemData, itemIndex, itemElement) {
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
                    }
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
                    itemTitleTemplate(item, index, itemElement) { const target = itemElement?.jquery ? itemElement[0] : itemElement; target.textContent = item.title || `Tab ${index + 1}`; },
                    itemTemplate(item, index, itemElement) { renderPanelContent(config, item, itemElement); },
                    animationEnabled: true,
                    swipeEnabled: true,
                    deferRendering: false,
                    onSelectionChanged() { setTimeout(() => refreshNestedLayouts(element), 0); }
                };
                break;
            case "MultiView":
                options = {
                    ...base,
                    items: config.panels || [],
                    itemTemplate(item, index, itemElement) { renderPanelContent(config, item, itemElement); },
                    animationEnabled: true,
                    swipeEnabled: true,
                    loop: false,
                    deferRendering: false,
                    onSelectionChanged() { setTimeout(() => refreshNestedLayouts(element), 0); }
                };
                break;
            case "Splitter":
                options = {
                    ...base,
                    orientation: componentOrientation(config),
                    items: (config.panels || []).map(panel => ({
                        ...panel,
                        size: panel.size || undefined,
                        minSize: panel.minSize || undefined,
                        maxSize: panel.maxSize || undefined,
                        collapsible: panel.collapsible !== false,
                        collapsed: !!panel.collapsed,
                        template(itemData, itemIndex, itemElement) { renderPanelContent(config, panel, itemElement); }
                    })),
                    onResize() { setTimeout(() => refreshNestedLayouts(element), 0); },
                    onItemCollapsed() { setTimeout(() => refreshNestedLayouts(element), 0); },
                    onItemExpanded() { setTimeout(() => refreshNestedLayouts(element), 0); }
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
            case "Map": {
                const rows = config.rows || [];
                const provider = String(config.mapProvider || "google");
                const apiKey = String(config.mapApiKey || "").trim();
                options = { ...base, provider, type: config.mapType || "roadmap",
                    center: { lat: number(config.mapCenterLatitude, 51.1657), lng: number(config.mapCenterLongitude, 10.4515) },
                    zoom: Math.max(1, number(config.mapZoom, 4)), controls: config.mapControls !== false,
                    autoAdjust: config.mapAutoAdjust !== false, markers: mapMarkers(config, rows), routes: mapRoutes(config, rows),
                    apiKey: apiKey ? { [provider]: apiKey } : undefined
                };
                break;
            }
            case "VectorMap": {
                const rows = config.rows || [];
                options = { ...base, layers: vectorLayers(config, rows), projection: config.vectorProjection || "mercator",
                    center: [number(config.mapCenterLongitude, 10.4515), number(config.mapCenterLatitude, 51.1657)],
                    zoomFactor: Math.max(1, number(config.mapZoom, 1)), maxZoomFactor: 256, panningEnabled: true, zoomingEnabled: true,
                    controlBar: { enabled: config.mapControls !== false }, tooltip: { enabled: true, customizeTooltip(info) {
                        const a = info?.attribute?.("properties") || {}; return { text: String(a.label || a.name || a.value || "") };
                    } },
                    onClick(event) {
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
                    }
                };
                break;
            }
            case "Chat": {
                const allowChatSending = config.chatAllowSending !== false && !chatBroadcastMode();
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
                    onContentReady() {
                        queueMicrotask(() => {
                            applyChatInputState(element, config);
                            if (!allowChatSending) element.querySelectorAll(".dx-chat-messagebox,.dx-chat-message-box,.dx-chat-input-container").forEach(node => node.remove());
                        });
                    },
                    onMessageEntered(event) {
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
                    },
                    emptyViewTemplate(data, itemElement) {
                        const target = itemElement?.jquery ? itemElement[0] : itemElement;
                        if (!target) return;
                        target.innerHTML = `<div class="ps-chat-empty"><span class="dx-icon dx-icon-chat" aria-hidden="true"></span><strong>${escapeHtml(activeChatPlatform(config))} chat</strong><small>Messages for other platforms stay hidden.</small></div>`;
                    }
                };
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
                queueMicrotask(() => applyChatInputState(element, state.config));
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
    }


    function installDesignerMapShield(element, config) {
        element.querySelector?.(':scope > .ps-component-designer-map-shield')?.remove?.();
        if (!config?.designerMode || lower(config.kind) !== "map") return;
        const shield = document.createElement("div");
        shield.className = "ps-component-designer-map-shield";
        shield.setAttribute("aria-hidden", "true");
        const blockMapGesture = event => {
            event.preventDefault?.();
            event.stopImmediatePropagation?.();
        };
        for (const type of ["pointerdown", "pointermove", "pointerup", "pointercancel", "mousedown", "mousemove", "mouseup", "click", "dblclick", "contextmenu", "wheel", "touchstart", "touchmove", "touchend"])
            shield.addEventListener(type, blockMapGesture, { passive: false });
        element.append(shield);
    }

    function dispose(element) {
        const state = states.get(element);
        if (state?.timer) clearInterval(state.timer);
        state?.layout?.cancel?.();
        try { state?.chatUnsubscribe?.(); } catch { }
        for (const child of [...element.querySelectorAll("[data-ps-component-runtime]")]) {
            if (child !== element) dispose(child);
        }
        try { state?.pivotSource?.dispose?.(); } catch { }
        try { state?.instance?.dispose?.(); } catch { }
        states.delete(element);
        element.replaceChildren();
        clearWidgetResidue(element);
    }

    async function render(element, rawConfig, options = {}) {
        if (!element) return null;
        const config = decodeConfig(rawConfig || element.dataset.psComponentConfig);
        if (!config) return null;
        dispose(element);
        element.dataset.psComponentRuntime = "true";
        element.dataset.psComponentId = String(config.id || "");
        element.classList.add("ps-component-runtime");
        applyLayoutClasses(element, config);
        if (!window.jQuery || !window.DevExpress) {
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
            const plugin = pluginNames[config.kind];
            if (!plugin || typeof window.jQuery.fn[plugin] !== "function") throw new Error(`${config.kind} is not available in the bundled DevExtreme runtime.`);
            const optionsValue = buildOptions(config, element, data);
            const $element = window.jQuery(element);
            $element[plugin](optionsValue);
            const instance = $element[plugin]("instance");
            if (config.kind === "Menu") {
                const expectedItems = menuItems(config, config.rows || []);
                await Promise.resolve();
                if (expectedItems.length && typeof element.querySelector === "function" && !element.querySelector(".dx-menu-item")) {
                    try { instance?.dispose?.(); } catch { }
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

    window.addEventListener("publisherstudio:output-context-changed", () => {
        for (const [element, state] of states.entries()) {
            if (lower(state?.config?.kind) !== "chat") continue;
            render(element, state.config, { polling: false, fetchNow: false });
        }
    });

    window.PublisherStudioChatRuntime = window.PublisherStudioChatRuntime || {
        push(message) { window.dispatchEvent(new CustomEvent("publisherstudio:chat-message", { detail: message })); },
        setPlatform(platform) { window.PublisherStudioChatPlatform = String(platform || "Preview"); return refreshAll(document, { fetchNow: false }); },
        setChannel(channel) { window.PublisherStudioChatChannel = String(channel || ""); return refreshAll(document, { fetchNow: false }); },
        getBroadcastLayers(context, pageElementId) { return chatBroadcastLayers(context || {}, pageElementId); }
    };

    window.PublisherStudioComponentRuntime = {
        render,
        renderById(id, config) { return render(document.getElementById(id), config, { polling: false, fetchNow: false }); },
        disposeById(id) { const element = document.getElementById(id); if (element) dispose(element); },
        refreshAll,
        probeConnection,
        attachVectorDesigner(id, dotnet) {
            const element = document.getElementById(id);
            if (!element) return false;
            if (element.__psVectorHandler) element.removeEventListener("ps-vector-map-point", element.__psVectorHandler);
            element.__psVectorHandler = event => dotnet?.invokeMethodAsync?.("AddVectorDesignerPoint", number(event.detail?.longitude), number(event.detail?.latitude));
            element.addEventListener("ps-vector-map-point", element.__psVectorHandler);
            return true;
        },
        detachVectorDesigner(id) {
            const element = document.getElementById(id);
            if (!element?.__psVectorHandler) return;
            element.removeEventListener("ps-vector-map-point", element.__psVectorHandler);
            delete element.__psVectorHandler;
        },
        start,
        dispose(root) {
            if (!root) return;
            const elements = root.matches?.("[data-ps-component-config]") ? [root] : [...root.querySelectorAll?.("[data-ps-component-config]") || []];
            elements.forEach(dispose);
        }
    };
})();
