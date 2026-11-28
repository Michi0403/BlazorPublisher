# PublisherStudio 2.3.0 source validation

Source-only validation performed in the repair environment:

- `documentationViewer.js` passes Node syntax validation.
- The documentation/1-Wire static contract audit passes and still requires the focus-managed native dialog and `CloseFromBrowser` callback.
- Architecture and service-resilience static audits pass.
- The existing user-confirmed `NormalizeUrl` char overload fix remains unchanged.
- No `bin`, `obj`, compiled DLL, EXE or PDB artifacts are included in the source package.

A real .NET/DocFX compile remains owner-side validation because this repair environment does not provide the .NET SDK.
