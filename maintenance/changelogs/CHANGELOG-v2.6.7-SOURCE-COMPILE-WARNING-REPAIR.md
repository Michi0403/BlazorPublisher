# PublisherStudio 2.6.7 — Source compile and warning repair

## Build repair

- Repairs `StreamingStudio.RefreshDevices` so the generic type argument belongs to `IJSRuntime.InvokeAsync<TValue>` and `ConfigureAwait(true)` is applied to the returned awaitable. This addresses the reported CS0119 errors while preserving renderer-affine continuation behavior.
- Removes the Panel Studio CS8602 warning by making the existing outer `_draft is not null` invariant explicit to Razor's nullable analysis; runtime behavior is unchanged.
- Removes the CS0067 warning from the immutable legacy `OrganicCapabilityCatalog` by exposing a no-op custom event implementation. The active mutable object-store catalog and its real `Changed` notifications are untouched.

## Interactive rendering

- Existing PublisherStudio InteractiveServer directives are preserved exactly; no component/page render-mode directive was added, removed, or changed.

## Compatibility

- PublisherStudio.Web: 2.6.7.
- PublisherStudio.InstallerConsole: 2.6.7.
- 1-Wire protocol: 2.1.1 (unchanged).
- Publication format: 1.58 (unchanged).
- Picture Studio format: 1.5 (unchanged).
- No database migration is required.
