// javascript-diagnostics: guarded
const publisherStudioDiagnostics = globalThis.publisherStudioJavaScriptDiagnostics || {
  report(context, error) {
    try { console.error(`PublisherStudio JavaScript error in ${String(context || "documentation-viewer")}.`, error); }
    catch (reportError) { console.error("PublisherStudio fallback JavaScript diagnostics failed.", reportError); }
  }
};

const previousFocus = new WeakMap();
const callbacks = new WeakMap();
const handlers = new WeakMap();

function report(context, error) {
  try { publisherStudioDiagnostics.report(`js/documentationViewer.js:${context}`, error); }
  catch (reportError) { console.error("PublisherStudio documentation viewer diagnostics failed.", reportError); }
}

function focusCloseButton(dialog) {
  try {
    window.requestAnimationFrame(() => {
      try { dialog?.querySelector("[data-documentation-viewer-close]")?.focus(); }
      catch (error) { report("focusCloseButton.callback", error); }
    });
  } catch (error) {
    report("focusCloseButton", error);
    throw error;
  }
}

export function connect(dialog, callback) {
  try {
    if (!dialog || !callback || callbacks.has(dialog)) return;
    callbacks.set(dialog, callback);

    const requestClose = context => {
      try {
        void callback.invokeMethodAsync("CloseFromBrowser").catch(error => report(`${context}.invoke`, error));
      } catch (error) {
        report(context, error);
      }
    };

    const cancelHandler = event => {
      event.preventDefault();
      requestClose("cancel");
    };
    const backdropHandler = event => {
      if (event.target === dialog) requestClose("backdrop");
    };
    const closeButton = dialog.querySelector("[data-documentation-viewer-close]");
    const closeButtonHandler = () => requestClose("closeButton");

    dialog.addEventListener("cancel", cancelHandler);
    dialog.addEventListener("click", backdropHandler);
    closeButton?.addEventListener("click", closeButtonHandler);
    handlers.set(dialog, { cancelHandler, backdropHandler, closeButton, closeButtonHandler });
  } catch (error) {
    report("connect", error);
    throw error;
  }
}

export function show(dialog) {
  try {
    if (!dialog) return;
    if (!dialog.open) {
      previousFocus.set(dialog, document.activeElement);
      dialog.showModal();
    }
    focusCloseButton(dialog);
  } catch (error) {
    report("show", error);
    throw error;
  }
}

export function close(dialog) {
  try {
    if (!dialog) return;
    if (dialog.open) dialog.close();
    const previous = previousFocus.get(dialog);
    previousFocus.delete(dialog);
    if (previous instanceof HTMLElement && previous.isConnected) previous.focus();
  } catch (error) {
    report("close", error);
    throw error;
  }
}

export function disconnect(dialog) {
  try {
    const registered = handlers.get(dialog);
    if (registered) {
      dialog?.removeEventListener("cancel", registered.cancelHandler);
      dialog?.removeEventListener("click", registered.backdropHandler);
      registered.closeButton?.removeEventListener("click", registered.closeButtonHandler);
      handlers.delete(dialog);
    }
    callbacks.delete(dialog);
    previousFocus.delete(dialog);
  } catch (error) {
    report("disconnect", error);
    throw error;
  }
}
