# PublisherStudio 2.3.0 — documentation viewer close repair

- Rolls the version from 2.2.10 to 2.3.0 so the maintained three-part version scheme keeps minor and patch segments single-digit before rolling over.
- Keeps the working full-viewport Kawaii DocFX HTML/PDF/API viewer unchanged visually.
- Adds an explicit native click listener for the documentation viewer Close button. The listener invokes the existing `CloseFromBrowser` .NET callback, so closing does not depend only on Blazor's delegated click event inside the native `<dialog>`.
- Tracks and removes the JavaScript event listeners during component disposal.
- Retains the existing Escape/cancel and backdrop-close paths, the normal `@onclick` close path as a fallback, same-origin URL validation, PDF/API routes, and console release wiring.
