# PublisherStudio 4.2.6 — documentation browser startup and package verification

## Incident

PublisherStudio 4.2.5 built its application assembly, passed the existing PowerShell architecture and DocFX HTML validations, and consistently stopped at `Printing PDF cover/index part 1/16`. Reducing the index from 741 entries to 48 had not fixed the hang. The documentation renderer is owned by `build/Build-Documentation.ps1`; `LocalGPT.ReleasePackaging` is invoked **later** to merge validated PDF sections, not to print HTML in Edge.

## Changes

- Run a standalone, one-page HTML-to-PDF browser preflight before the full print queue. Both tagged and compatibility paths are evaluated with bounded headless startup times; the error identifies browser startup/printing rather than incorrectly implicating DocFX HTML or the downstream PDF merger.
- Drop Chromium's 180-second virtual-time budget from the static print-book route. Retain PDF tagging as the preferred option, with compatibility rendering clearly identified when required.
- Reuse a successfully probed PDF profile rather than repeatedly privileging a profile the installed browser cannot run.
- Emit browser progress while an individual print attempt is running, including the current render mode, produced PDF bytes and remaining timeout. Bound browser diagnostic pipe draining after a terminated browser process; orphaned children can no longer hold the build indefinitely on `.Result`.
- Bound each small cover/index part to at most two minutes per rendering attempt (or the user's shorter configured timeout), while keeping the pre-existing adaptive main documentation chunk timeout. Preserve durable chunk reuse and the mandatory final PDF checks.
- When automatic browser discovery is in use, verify a second installed browser if the preferred Edge/Chromium cannot print a one-page probe or fails a documentation chunk; never override an explicitly selected `PUBLISHERSTUDIO_DOCUMENTATION_BROWSER`.
- Validate the cached `LocalGPT.ReleasePackaging` tool package's **embedded NuGet ID/version**, mandatory .NET 10 executable artifacts and legal notices before reuse. Log the SHA-256 of the accepted cached tool. The current companion LocalGPT 6.0.8 sources declare the same `1.1.2` tool version; no unsupported package upgrade or new `PackageReference` has been introduced.

## Preserved

The full DocFX HTML documentation, 741-entry cover/index coverage, accessibility preflight, strict required-PDF behavior, durable documentation cache, existing PDFsharp-based managed merge, release packaging ownership, and every PowerShell architecture assertion remain in place. No application service or UI behavior changed.

## Verification boundary

Source-level and package-identity checks can be executed without .NET, but Microsoft Edge on Windows, PowerShell 5.1 parser/architecture gates, successful generation of all 741 documentation pages and final PDF merging require the owner's actual Windows build. This release does not claim those runtime checks passed. It changes the renderer lifecycle and makes subsequent failures diagnosable rather than silently masking them.
