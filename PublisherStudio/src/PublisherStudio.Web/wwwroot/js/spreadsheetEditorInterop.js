const registrations = new Map();

export function initialize(iframeId, sessionId, dotnetReference) {
    dispose(iframeId);

    const registration = { handler: null, timer: 0 };
    registration.handler = event => {
        if (event.origin !== window.location.origin || event.data?.sessionId !== sessionId) return;
        const source = document.getElementById(iframeId)?.contentWindow;
        if (source && event.source !== source) return;

        if (event.data.type === "publisher-spreadsheet-ready") {
            window.clearTimeout(registration.timer);
            registration.timer = 0;
            dotnetReference.invokeMethodAsync("SpreadsheetReady").catch(() => {});
        } else if (event.data.type === "publisher-spreadsheet-saved") {
            dotnetReference.invokeMethodAsync("SpreadsheetSaved", event.data.intent || "apply").catch(() => {});
        } else if (event.data.type === "publisher-spreadsheet-error") {
            dotnetReference.invokeMethodAsync("SpreadsheetFailed", event.data.message || "The spreadsheet could not be saved.").catch(() => {});
        }
    };

    window.addEventListener("message", registration.handler);
    registration.timer = window.setTimeout(() => {
        registration.timer = 0;
        dotnetReference.invokeMethodAsync(
            "SpreadsheetFailed",
            "Spreadsheet Studio did not finish loading. Verify the DevExpress Spreadsheet client assets and server package license."
        ).catch(() => {});
    }, 25000);
    registrations.set(iframeId, registration);
    const frame = document.getElementById(iframeId);
    frame?.contentWindow?.postMessage({ type: "publisher-spreadsheet-probe", sessionId }, window.location.origin);
}

export function requestSave(iframeId, sessionId, intent) {
    const frame = document.getElementById(iframeId);
    frame?.contentWindow?.postMessage({ type: "publisher-spreadsheet-save", sessionId, intent }, window.location.origin);
}

export function focus(iframeId, sessionId) {
    const frame = document.getElementById(iframeId);
    frame?.contentWindow?.postMessage({ type: "publisher-spreadsheet-focus", sessionId }, window.location.origin);
}

export function dispose(iframeId) {
    const registration = registrations.get(iframeId);
    if (registration?.handler) window.removeEventListener("message", registration.handler);
    if (registration?.timer) window.clearTimeout(registration.timer);
    registrations.delete(iframeId);
}
