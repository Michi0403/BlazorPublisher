# PublisherStudio 3.0.2 source validation

This handoff is validated statically. No `dotnet` restore/build/test/publish and no PowerShell release build were executed in the packaging environment.

Validated source contracts:

- PublisherStudio Web and installer identities are `3.0.2`; browser cache identities and npm package identities match.
- The release preflight resolves Python 3 as `python`, `python3`, or Windows `py -3` before long-running build work starts.
- Maintained Python-backed PowerShell guards do not contain a hard-coded bare `& python` invocation.
- Panel Studio persistence, XML documentation, architecture, iterator, async, service-resilience and component/prerender audits use host-neutral Python resolution.
- XML documentation coverage/quality validation passes for the maintained C# and Razor source after documenting the new platform-boundary declarations.
- The 3.0.1 Windows/Unix backend boundaries remain intact and the cross-platform boundary audit still rejects System.Drawing/GDI and common-service OS leaks.
- InteractiveServer render-mode ownership is unchanged from the 3.0.1 source baseline.
- Source ZIP validation requires repository-root layout, explicit directory entries, duplicate/case/Unicode collision checks and `unzip -t` integrity verification.

Run the authoritative project build/release scripts on licensed Windows/macOS/Linux build hosts before publication.
