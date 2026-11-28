# PublisherStudio 3.9.8 — Razor helper directive collision repair

## Compile repair

- Fixed the authoritative Razor compiler error `RZ1002` in `PanelStudio.razor` caused by the 3.9.7 JavaScript-helper `DxButton` binding `Text="@helper.Label"`.
- `helper` is a reserved Razor directive token. The loop variables are now named `scriptHelper`, and component text is emitted through the explicit `@(scriptHelper.Label)` expression so the markup cannot be parsed as the unsupported `@helper` directive.
- Applied the same non-reserved loop-variable naming to the HTML-script helper list for consistency while preserving its behavior and existing explicit Razor expressions.

## Regression prevention

- Extended `Assert-RazorComponentAttributeExpressions.ps1`, which is already wired into the repository build architecture, with `RZARCH0003`.
- The guard now rejects component attributes that begin a direct Razor expression with reserved directive tokens such as `@helper`, `@page`, `@code`, `@functions`, `@section`, `@using`, `@inject`, `@layout` or `@rendermode`.
- This closes the gap left by the older 2.9.5 parser-compatibility check, which protected the then-known native markup forms but did not cover a later DevExpress component attribute such as `Text="@helper.Label"`.

## Retained 3.9.7 repairs

- Panel Studio overlap-aware insertion, DevExpress wrapper-depth geometry, inspector containment and DevExpress button styling are unchanged.
- The shared mouse/controller/keyboard/editor ownership repair for text navigation, sliders, RichEdit and other focused/dragged controls is unchanged.
- No LocalGPT source was changed.

No `dotnet`, MSBuild, restore, publish, GitHub, or online repository access was used for this source handoff.
