# PublisherStudio 2.8.9 — .NET/DevExpress upgrade integration and component-diagnostics repair

## User-supplied toolchain upgrades retained

This release starts from the upgraded PublisherStudio source supplied after the 2.8.8 handoff and preserves those changes rather than replacing them with an older tree.

- DevExpress is explicitly pinned to **25.2.9** for the Blazor, RichEdit, localization, and Spreadsheet packages through `DevExpressVersion`.
- The local Entity Framework CLI manifest is **dotnet-ef 10.0.11**.
- The installer console uses **Microsoft.Extensions.Logging 10.0.11**.
- PublisherStudio still targets **`net10.0`**. Its `global.json` keeps the existing `10.0.301` minimum SDK plus `latestFeature` roll-forward, so an installed newer .NET 10 feature band can be selected without unnecessarily raising the repository's minimum SDK requirement.

No downgrade of the user's upgraded package/tool versions was performed.

## Build repair after Razor XML-documentation companions

2.8.8 added XML-documentation companion partials such as `AnimationPanel.razor.cs`. These files contain only a documented, empty `partial class` declaration so compiler/DocFX XML output can describe the generated Razor class. The older component-diagnostics gate incorrectly classified all 46 of those documentation-only files as new operational components and demanded runtime catch/log/notification calls from files that intentionally contain no executable members.

`Assert-ComponentDiagnostics.ps1` now distinguishes that exact documentation-only shape from operational code:

- the file must be a `*.razor.cs` companion with a real sibling `.razor` file;
- the complete C# file must contain only a file-scoped namespace, XML documentation, and an empty `public partial class` body;
- any field, property, method, constructor, attribute, base type, or other member makes the strict pattern fail and the file is audited normally;
- the existing operational `PictureEditor.razor.cs` remains under the diagnostics baseline and is not exempted.

This repairs the 46 pre-build failures without weakening component runtime-safety enforcement or adding a permissive legacy baseline.

## Authored documentation source restored

The supplied upgrade archive contained the generated in-application help output but not the authored `docs/` tree. The latest authored PublisherStudio DocFX/Kawaii source from the 2.8.8 source baseline is restored so a normal repository checkout can regenerate the same documentation style instead of depending on generated `wwwroot/help-docs` files. Generated documentation remains ignored by `.gitignore`.

## Preserved architecture

- InteractiveServer/prerender boundaries from 2.8.8 are unchanged.
- Razor method-local diagnostics, service resilience, iterator `try/finally`, ConfigureAwait policy, XML-documentation enforcement, and text-service ownership remain enabled.
- LocalGPT 1-Wire protocol remains **2.1.1**.
- Existing Video Studio, media rendering, Converter Studio, Panel Studio, selection, layer, and export behavior is untouched by this build-policy repair.
