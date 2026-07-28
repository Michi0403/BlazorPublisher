// javascript-diagnostics: guarded
var publisherStudioDiagnostics = globalThis.publisherStudioJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`PublisherStudio JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("PublisherStudio fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`PublisherStudio fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
const registrations = new Map();

export function initialize(iframeId, sessionId, dotnetReference) { try {
    dispose(iframeId);

    const registration = { handler: null, timer: 0 };
    const startTimeout = message => { try {
        window.clearTimeout(registration.timer);
        registration.timer = window.setTimeout(() => { try {
            registration.timer = 0;
            dotnetReference.invokeMethodAsync("SpreadsheetFailed", message).catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/spreadsheetEditorInterop.js:promise-catch@13', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/spreadsheetEditorInterop.js:callback:dotnetReference.invokeMethodAsync("SpreadsheetFailed", message).catch@13', __javascriptError); throw __javascriptError; }});
         } catch (__javascriptError) { publisherStudioDiagnostics.report('js/spreadsheetEditorInterop.js:callback:window.setTimeout@11', __javascriptError); throw __javascriptError; }}, 25000);
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/spreadsheetEditorInterop.js:startTimeout@9', __javascriptError); throw __javascriptError; }};
    registration.handler = event => { try {
        if (event.origin !== window.location.origin || event.data?.sessionId !== sessionId) return;
        const source = document.getElementById(iframeId)?.contentWindow;
        if (source && event.source !== source) return;

        if (event.data.type === "publisher-spreadsheet-ready") {
            window.clearTimeout(registration.timer);
            registration.timer = 0;
            dotnetReference.invokeMethodAsync("SpreadsheetReady", event.data.fileName || null).catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/spreadsheetEditorInterop.js:promise-catch@24', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/spreadsheetEditorInterop.js:callback:dotnetReference.invokeMethodAsync("SpreadsheetReady", event.data.fileN@24', __javascriptError); throw __javascriptError; }});
        } else if (event.data.type === "publisher-spreadsheet-opening") {
            startTimeout("The selected workbook did not finish loading in Spreadsheet Studio.");
            dotnetReference.invokeMethodAsync("SpreadsheetOpening").catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/spreadsheetEditorInterop.js:promise-catch@27', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/spreadsheetEditorInterop.js:callback:dotnetReference.invokeMethodAsync("SpreadsheetOpening").catch@27', __javascriptError); throw __javascriptError; }});
        } else if (event.data.type === "publisher-spreadsheet-saved") {
            dotnetReference.invokeMethodAsync("SpreadsheetSaved", event.data.intent || "apply").catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/spreadsheetEditorInterop.js:promise-catch@29', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/spreadsheetEditorInterop.js:callback:dotnetReference.invokeMethodAsync("SpreadsheetSaved", event.data.inten@29', __javascriptError); throw __javascriptError; }});
        } else if (event.data.type === "publisher-spreadsheet-data-selection") {
            dotnetReference.invokeMethodAsync("SpreadsheetDataSelectionReceived", {
                sheetName: event.data.sheetName || "Sheet1",
                rangeAddress: event.data.rangeAddress || "",
                rows: Array.isArray(event.data.rows) ? event.data.rows : []
            }).catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/spreadsheetEditorInterop.js:promise-catch@31', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/spreadsheetEditorInterop.js:callback:dotnetReference.invokeMethodAsync("SpreadsheetDataSelectionReceived", @35', __javascriptError); throw __javascriptError; }});
        } else if (event.data.type === "publisher-spreadsheet-error") {
            window.clearTimeout(registration.timer);
            registration.timer = 0;
            dotnetReference.invokeMethodAsync("SpreadsheetFailed", event.data.message || "The spreadsheet could not be saved.").catch((__promiseError) => { try { publisherStudioDiagnostics.report('js/spreadsheetEditorInterop.js:promise-catch@39', __promiseError);  } catch (__javascriptError) { publisherStudioDiagnostics.report('js/spreadsheetEditorInterop.js:callback:dotnetReference.invokeMethodAsync("SpreadsheetFailed", event.data.mess@39', __javascriptError); throw __javascriptError; }});
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/spreadsheetEditorInterop.js:registration.handler@16', __javascriptError); throw __javascriptError; }};

    window.addEventListener("message", registration.handler);
    startTimeout("Spreadsheet Studio did not finish loading. Verify the DevExpress Spreadsheet client assets and server package license.");
    registrations.set(iframeId, registration);
    const frame = document.getElementById(iframeId);
    frame?.contentWindow?.postMessage({ type: "publisher-spreadsheet-probe", sessionId }, window.location.origin);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/spreadsheetEditorInterop.js:initialize@5', __javascriptError); throw __javascriptError; }}

export function requestSave(iframeId, sessionId, intent) { try {
    const frame = document.getElementById(iframeId);
    frame?.contentWindow?.postMessage({ type: "publisher-spreadsheet-save", sessionId, intent }, window.location.origin);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/spreadsheetEditorInterop.js:requestSave@50', __javascriptError); throw __javascriptError; }}

export function requestDataSelection(iframeId, sessionId) { try {
    const frame = document.getElementById(iframeId);
    frame?.contentWindow?.postMessage({ type: "publisher-spreadsheet-create-data-object", sessionId }, window.location.origin);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/spreadsheetEditorInterop.js:requestDataSelection@55', __javascriptError); throw __javascriptError; }}

export function focus(iframeId, sessionId) { try {
    const frame = document.getElementById(iframeId);
    frame?.contentWindow?.postMessage({ type: "publisher-spreadsheet-focus", sessionId }, window.location.origin);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/spreadsheetEditorInterop.js:focus@60', __javascriptError); throw __javascriptError; }}

export function dispose(iframeId) { try {
    const registration = registrations.get(iframeId);
    if (registration?.handler) window.removeEventListener("message", registration.handler);
    if (registration?.timer) window.clearTimeout(registration.timer);
    registrations.delete(iframeId);
 } catch (__javascriptError) { publisherStudioDiagnostics.report('js/spreadsheetEditorInterop.js:dispose@65', __javascriptError); throw __javascriptError; }}

