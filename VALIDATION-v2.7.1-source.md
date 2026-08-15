# PublisherStudio 2.7.1 source validation

This paired release was validated statically without invoking `dotnet`, MSBuild, Visual Studio, package restore, or GitHub.

Checks cover the PublisherStudio.Web and installer version, single-digit version-slot policy, browser module cache-busters, existing strict async/regression audits, service resilience, and preservation of all 5 existing `@rendermode` directives against the 2.7.0 source baseline.

The Windows build/runtime test remains authoritative.
