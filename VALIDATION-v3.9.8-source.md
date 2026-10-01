# PublisherStudio 3.9.8 source validation

## Scope

Focused source-only validation of the Razor `RZ1002` compile repair reported by the authoritative Windows build. No .NET compilation, MSBuild, restore, publish, GitHub access, or online repository access was performed.

## Reported compiler failure and repair

The reported failure was:

- `PublisherStudio.Web/Components/Editor/PanelStudio.razor(459): RZ1002 The helper directive is not supported.`

Inspection of the 3.9.7 source at that line showed a DevExpress button attribute using `Text="@helper.Label"`. The repository already contains a 2.9.5 parser-compatibility history documenting that direct `@helper` and `@page` member expressions collide with Razor directives. The 3.9.7 change reintroduced that same parser class through a new DevExpress component attribute that the older narrow audit did not cover.

The repair:

- renames the markup loop variable from `helper` to `scriptHelper` in both JavaScript-helper lists;
- emits the DevExpress button caption as `Text="@(scriptHelper.Label)"`;
- retains the existing helper keys, callbacks and behavior generation;
- extends `Assert-RazorComponentAttributeExpressions.ps1` with `RZARCH0003`, rejecting component attributes that begin direct expressions with reserved Razor directive tokens.

## Static verification

Source validation confirms:

- no `Text="@helper.` or direct markup `@helper.` expression remains in `PanelStudio.razor`;
- both JavaScript-helper loops use the non-reserved `scriptHelper` name;
- the repaired `DxButton` uses `Text="@(scriptHelper.Label)"`;
- the build-wired Razor component-attribute guard contains the new reserved-directive rule;
- project, installer, npm package/lock and browser cache-busting references are aligned at 3.9.8;
- the maintained Python architecture/Razor/async/component/service/persistence audits used for 3.9.7 continue to pass after this focused repair;
- generated Python cache files and build outputs are excluded from the source archive.

The user's Windows .NET 10 + DevExpress build remains authoritative for Razor compilation.
