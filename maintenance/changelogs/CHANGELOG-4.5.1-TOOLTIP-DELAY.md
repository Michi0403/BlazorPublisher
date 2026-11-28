# PublisherStudio 4.5.1 — Delayed tooltips

Release source base: PublisherStudio 4.5.0.

## Closed (source changes)

- Tripled application-owned tooltip display delay for mouse hover from **420 ms** to **1,260 ms**.
- Tripled application-owned tooltip display delay for keyboard focus from **220 ms** to **660 ms**.
- Added remaining native HTML `title` hints to the application tooltip runtime's selector, so these hints are removed from the element and obey the same delayed popup mechanism instead of appearing immediately with a browser-defined delay.
- Kept the previous hide/cancel timings and keyboard/pointer dismissal behavior unchanged.
- Increased version in both PublisherStudio Web and Setup projects from **4.5.0** to **4.5.1**.
- Updated the JavaScript diagnostics SHA-256 inventory for the modified tooltip runtime.

## Validation

- Node `--check` passes for the updated runtime.
- 11/11 JavaScript regression tests pass, including the new event-driven `tests/js/tooltip-delay-4.5.1.test.cjs` for mouse, keyboard, and native title removal.
- Offline ZIP integrity verified.

## Remaining verification

- **Not built with .NET** (requested source-only workflow).
- Live Browser/DevExpress appearance timing is not verified; vendor-rendered tooltips that bypass PublisherStudio's runtime may have separate vendor timing.
