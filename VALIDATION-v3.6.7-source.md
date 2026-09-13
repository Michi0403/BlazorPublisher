# PublisherStudio 3.6.7 source validation

Validation is source/static only in this handoff environment. No `dotnet` build, publish, signing, notarization or GitHub operation was performed.

Required checks include the 3.6.7 release audit, JavaScript syntax/diagnostics manifest, application architecture, service resilience, cross-platform boundaries, PowerShell interpolation and XML/Razor documentation coverage. The delivered ZIP must pass archive integrity and a fresh-extraction rerun.

3.6.7-specific assertions cover deferred generic object selection commit, HTML/Panel transform-compatible editor zoom, demand-driven focus/visibility-aware canvas/Panel Studio gamepad polling with preserved controller mappings, listener cleanup, and preservation of the existing DevExtreme specialized pointer path.
