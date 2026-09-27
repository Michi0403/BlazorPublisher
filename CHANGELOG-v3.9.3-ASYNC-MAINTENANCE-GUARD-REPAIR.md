# PublisherStudio 3.9.3 — async maintenance-guard repair

## Fixed

- Repaired the remaining strict async-continuation violation in `Components/Editor/PanelStudio.razor` introduced by the bounded DxPopup portal-materialization retry in 3.9.2.
- The retry-triggered `InvokeAsync(StateHasChanged)` now explicitly uses `ConfigureAwait(true)`, matching the renderer-affine continuation policy already used by the surrounding Panel Studio lifecycle code.
- This is intentionally a narrow maintenance correction: the 3.9.2 popup/lifecycle recovery is retained rather than redesigned again.

## Preserved

- Page Effects and the other large DevExpress popup surfaces keep the 3.9.2 viewport sizing, direct popup-body ownership and internal scrolling repairs.
- Panel Studio keeps the bounded interaction-surface retry that avoids reporting a transient DxPopup portal-materialization race as a permanent failure.
- The recovered Mainframe/rulers, Data Visual contrast/checkbox alignment, shape corner-radius rendering and presentation-export control bar remain unchanged.
- DevExpress component ownership, localization, publication/export services and InteractiveServer render boundaries are unchanged by this correction.

## Maintenance validation

- The strict async-continuation audit passes for all 82 maintained PublisherStudio source files after this repair.
- Razor maintenance architecture, application architecture, service resilience, component resilience, prerender interop safety, iterator policy and XML documentation coverage audits pass.
- No `dotnet`, MSBuild, NuGet restore/publish, GitHub or online repository access was used.

## Version

`3.9.2 -> 3.9.3`.

The version-number rollover rule remains satisfied; neither the minor nor patch slot reaches two digits.
