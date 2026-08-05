// javascript-diagnostics: guarded
var publisherStudioDiagnostics = globalThis.publisherStudioJavaScriptDiagnostics || {
    report(context, error) { try { console.error(`PublisherStudio JavaScript error in ${String(context || "browser-runtime")}.`, error); } catch (reportError) { console.error("PublisherStudio fallback JavaScript diagnostics failed.", reportError); } },
    guard(context, callback) { try { return callback; } catch (error) { console.error(`PublisherStudio fallback guard failed in ${String(context || "browser-runtime")}.`, error); return callback; } },
    guardObject(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback object guard failed in ${String(context || "browser-runtime")}.`, error); return value; } },
    guardClass(context, value) { try { return value; } catch (error) { console.error(`PublisherStudio fallback class guard failed in ${String(context || "browser-runtime")}.`, error); return value; } }
};
window.publisherOrganicSecurity = {
    renderQr(elementId, value, label) { try {
        const host = document.getElementById(elementId);
        if (!host) return;
        host.replaceChildren();
        if (!value) return;
        try {
            const qr = qrcode(0, 'M');
            qr.addData(String(value), 'Byte');
            qr.make();
            host.innerHTML = qr.createSvgTag({ cellSize: 4, margin: 2, scalable: true, alt: { text: label || '1-Wire security QR code' } });
        } catch (error) {
            const message = document.createElement('span');
            message.className = 'organic-error';
            message.textContent = `QR generation failed: ${error?.message || error}`;
            host.appendChild(message);
        }
     } catch (__javascriptError) { publisherStudioDiagnostics.report('js/organicSecurityQr.js:renderQr@3', __javascriptError); throw __javascriptError; }}
};

// Guard exported browser namespaces after the file has initialized.
publisherStudioDiagnostics.guardObject("publisherOrganicSecurity", window.publisherOrganicSecurity);
