# PublisherStudio 2.5.2 source validation

Source-only validation. No GitHub/network repository access was used. No `dotnet`, MSBuild, restore, build, test, publish, or DocFX build was executed in this packaging environment.

## Static/source checks performed

- Application architecture policy audit: **PASS**.
- Service resilience audit: **1,243 service methods** with owned try/catch + diagnostics; 4 yield methods and 4 direct Program/Startup methods skipped by policy — PASS.
- Documentation/1-Wire static contract audit: **PASS**.
- Panel Studio persistence source audit: **PASS**.
- XML documentation coverage: **4,891 maintained C# type/method and public API declarations PASS**.
- `publisherInterop.js` ECMAScript module syntax checked with Node `--check`: **PASS**.
- JavaScript diagnostics SHA-256 manifest independently verified with LF-normalized hashing for **16 maintained browser files**: **PASS**.
- Project XML parsed with Python XML tooling: **2 csproj files, 0 parse failures**.
- PublisherStudio EN/DE localization catalogs parsed: **3,036 keys each**, key sets equal; affected Panel Studio/3D German strings verified — PASS.
- Version fields checked: PublisherStudio Web and InstallerConsole are **2.5.2**.

## Panel Studio persistence contracts checked in source

The new source audit verifies:

1. The Editor apply path determines whether a single HTML object is still panel-canvas-equivalent by checking local X, Y, width, height and rotation.
2. Authored local geometry routes through `PromoteSelectedHtmlEmbedToPanel(draft)` instead of the content-only standalone HTML apply path.
3. Promotion preserves the selected object's outer Mainframe bounds while replacing its content with the normalized panel graph.
4. `SaveSelectedAsTemplate` waits for `FlushPanelStudioInteractionsAsync()` before cloning the selected module.
5. The main Panel Studio `Save()` also waits for the interaction queue before cloning/applying the graph.
6. `publisherInterop.js` exposes `flushPanelStudioInteractions`, waits for the serialized `binding.invokeQueue`, and exposes the function through `window.publisherStudio` for Blazor interop.
7. The JavaScript diagnostics inventory matches the modified browser source.

## Render-mode source check

Routed PublisherStudio pages were enumerated directly from `Components/Pages`. `Editor`, `Help`, `Localization`, and `OrganicPlugins` carry explicit InteractiveServer directives. `Error.razor` is intentionally static. The JavaScript diagnostics bridge remains an explicit non-prerendered InteractiveServer island. The render-mode validator now covers Help as well as the existing required pages/island.

## Build authority

A real Windows/.NET build remains the authority for C# compilation, generated DocFX output and runtime behavior. This source package intentionally does not claim a compiler pass because no .NET build was run here.
