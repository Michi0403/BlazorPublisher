# PublisherStudio 3.9.3

PublisherStudio 3.9.3 is a narrow maintenance correction on top of the 3.9.2 frontend recovery. The bounded Panel Studio DxPopup portal-materialization retry remains in place, but its renderer-affine `InvokeAsync(StateHasChanged)` continuation now explicitly uses `ConfigureAwait(true)`, satisfying the repository's strict async-continuation policy.

The popup sizing/scrolling recovery, Page Effects repair, Mainframe/rulers, Data Visual presentation fixes, corner-radius rendering, presentation website controls, DevExpress ownership and InteractiveServer boundaries are otherwise unchanged.

No `dotnet`, MSBuild, NuGet restore/publish, GitHub or online repository access was used for this source-only handoff.

See `CHANGELOG-v3.9.3-ASYNC-MAINTENANCE-GUARD-REPAIR.md` and `VALIDATION-v3.9.3-source.md`.
