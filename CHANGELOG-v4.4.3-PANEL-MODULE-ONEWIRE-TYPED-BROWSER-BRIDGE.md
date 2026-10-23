# PublisherStudio 4.4.3 — Panel/Div 1-Wire organic functions and typed browser bridge

## Scope and baseline

Built **only** on the complete delivered PublisherStudio 4.4.2 macOS build-fix release. Its stricter Debug PowerShell compatibility checks and repository-local browser profile correction remain unchanged.

## Features

- Two executable catalog capabilities, exposed through the existing 1-Wire peer permission coordinator: `publisher.panel.modules.inspect` and `publisher.panel.module.invoke`.
- `publisher.panel.modules.inspect`: queues discovery through the currently opted-in browser tab. Responses contain bounded mounted-module IDs, kinds, configured grid field names, method names, argument descriptions and geometry; no row contents, entered field values, credentials or arbitrary configuration are exported. Retrieve completion via `publisher.input.result`.
- `publisher.panel.module.invoke`: accepts one validated module UUID, one supported method and a bounded JSON argument. PublisherStudio's user must approve **each invocation**; trusted peer permissions do not silently bypass that approval. No arbitrary JavaScript execution or unbounded DevExtreme `.option` access.
- The legacy `publisher.input.execute` path explicitly rejects `InspectModules` and `InvokeModule`, preventing it from bypassing the dedicated approval boundary.
- A JS/TypeScript-friendly browser API, `window.PublisherStudioOrganicModuleRuntime`, provides `list`, `describe` and `invoke`. The runtime is within `componentRuntime.js`, which standalone website exports already bundle, so deterministic module operations remain functional **without LocalGPT**.
- `publisherStudioOrganicModules.d.ts` documents the typed browser module/function contract without adding an npm or NuGet dependency.
- DataGrid / TreeList exposes `sortByColumn({field,direction})` when a live component instance and authored field list support it; unknown columns and arbitrary direction values are rejected.
- LocalGPT Council-backed DevExtreme `dxChat` requests include up to 12 visible module descriptors with a bounded context budget, enabling contextual suggestions without publishing data rows or executing unapproved operations.

## Security and limitations

- Mounted component instances are discovered inside the currently enabled PublisherStudio browser tab, **not** placed into a global server-side catalog that might expose a different user's document. The top-level 1-Wire catalog contains only the two generic capability keys.
- The standalone website API executes deterministic functions locally. It does not ship a 1-Wire listener, embedded LLM, remote AI provider credentials or an automatic Council tool-call agent.
- Chat does not currently automatically execute AI-proposed actions or stream token-level responses into component state. Use the separately approved `publisher.panel.module.invoke` capability for remote changes.
- DevExpress proprietary AI extensions, Report Designer/Report Viewer asset licensing/runtime, and the simultaneous Visual Studio debugger issue were not implemented or diagnosed conclusively in this change. The two applications' recorded listener ports are different; their logs do not establish a documentation-builder-related debug conflict.

## Diagnostics / regression

- Browser JavaScript errors continue through the maintained diagnostics bridge; normalized JS checksums are updated.
- See `VALIDATION-v4.4.3-source.md` and `tests/js/panel-module-organic.test.cjs`, `tests/js/panel-organic-contract.test.cjs`.
- No .NET SDK or PowerShell runtime was used to build this release. A macOS/Windows build and live 1-Wire/DevExtreme test are still required.
