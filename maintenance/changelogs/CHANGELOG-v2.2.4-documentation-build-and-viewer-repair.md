# PublisherStudio 2.2.4 — documentation build and viewer repair

- Restores the complete DocFX HTML-backed Kawaii PDF path instead of the incomplete inventory-only PDF.
- Preserves generated Kawaii HTML in the application output before the PDF phase begins.
- Adds the required logged exception boundaries to the documentation viewer service and component.
- Keeps explicit renderer-affine `ConfigureAwait(true)` calls under a reviewed per-component allowance.
- Restores reliable in-app HTML, API, PDF, browser-tab, focus, Escape, and focus-return behavior.
- Retains the installer, release, Pages, 1-Wire, and application architecture from 2.2.3.
