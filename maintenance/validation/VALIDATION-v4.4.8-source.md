# PublisherStudio 4.4.8 source validation

## Incident evidence

The provided customer log shows a successful 4.4.7.0 process bootstrap followed by an interactive circuit exception: `InspectorPanel` rejects the incoming parameter `OnInsertAiChat`. The 4.4.7 Editor markup sends that parameter to both `PublicationRibbon` and `InspectorPanel`; only `PublicationRibbon` declares it. This is a first-render contract defect and is independent of LocalGPT availability.

## Repair and regression prevention

- Removed the invalid `InspectorPanel.OnInsertAiChat` attribute; preserved `PublicationRibbon.OnInsertAiChat` and the AI Chat quick-insert.
- Added `build/Assert-EditorComponentParameterContracts.ps1`, which resolves application Razor components from source and checks named bindings against `[Parameter]` properties, including split code-behind files. It rejects missing or duplicate types and unexpectedly low coverage.
- Wired the guard to the ordinary MSBuild Debug/Release pipeline through `Directory.Build.targets` and the top-level `Build-Release.ps1` entrypoint.
- Added a JS regression that independently checks component parameter declarations and the historical invalid `OnInsertAiChat` binding as a negative control.

## Executed here

- Eight Node browser/source regression scripts passed (5 `.cjs` and 3 `.mjs`, including the new contract test).
- Independent source-contract scan: **101 application-owned component invocations**, **383 named bindings**, across **52 Razor source files**, with **zero unresolved bindings**.
- ZIP source change comparison verified no removed baseline source files. All unmodified files remain byte-for-byte unchanged.
- Updated version values for `PublisherStudio.Web` and `PublisherStudio.InstallerConsole` to **4.4.8**.

## Not executed here

- `pwsh` / Windows PowerShell compatibility preflight (PowerShell runtime unavailable).
- .NET 10 compilation / DocFX PDF generation (dotnet SDK unavailable).
- Installed Windows/macOS interactive render and browser smoke test (no compiled application/runtime available).

**Release gate:** Do not publish 4.4.8 to external users based only on these source checks. Build it with the existing architecture guards and verify a packaged installation actually renders the editor and inserts both ordinary DevExtreme Chat and AI Chat with and without LocalGPT before public release. This repair is targeted to the documented 4.4.7 incident; it is not a statement that the entire product passed live acceptance testing.
