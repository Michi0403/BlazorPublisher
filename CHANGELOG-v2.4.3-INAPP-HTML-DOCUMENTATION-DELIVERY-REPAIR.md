# PublisherStudio 2.4.3 — in-app HTML documentation delivery repair

## Scope

This release changes only PublisherStudio documentation delivery and its release/install guards. No editor, publishing, media, streaming, panel, Data Visual, LocalGPT, or render-mode behavior was intentionally changed.

## Root cause fixed

- The generated documentation payload was present in the release application archive, but the Help ribbon's in-app viewer still opened the direct static URL `/help-docs/index.html` while the maintained documentation catalog already exposed the controller-backed route `/api/documentation/html/index.html`.
- That split left the customer viewer dependent on static-web-root resolution and allowed the UI, Help page, API profile, and release payload to disagree about the canonical installed route.

## Changes

- Advanced `PublisherStudio.Web` and `PublisherStudio.Setup` from 2.4.2 to 2.4.3.
- Changed the Help ribbon HTML guide and API reference commands to `/api/documentation/html/...`.
- Changed the Organic/1-Wire documentation profile to advertise the same controller-backed HTML routes.
- Added central viewer normalization so any legacy in-app `/help-docs/...` request is automatically rewritten to `/api/documentation/html/...` before the iframe or browser-tab link is rendered.
- Kept `/help-docs/...` controller routes as backwards-compatible public aliases.
- Added `no-store`/`no-cache` headers to controller-served documentation so an older missing/stale response cannot mask newly installed documentation after an in-place update.
- Explicitly marks `wwwroot/help-docs/**` for output/publish copying in `PublisherStudio.Web.csproj` and fails ordinary publish output validation if the core HTML, API index, DocFX runtime, Kawaii stylesheet/script, or status file is absent.
- Strengthened release ZIP and installer validation so required HTML documentation files must be non-empty/non-truncated, not merely named in the archive.
- After extraction, the installer now verifies the documentation status version matches the installed PublisherStudio application assembly version.
- Extended the documentation/1-Wire source audit so direct in-app `/help-docs` viewer routes are treated as a regression.

## Validation boundary

Source-only delivery by request. No `dotnet`, MSBuild, restore, publish, runtime compilation, GitHub access, or online repository access was performed. Static validation details are recorded in `VALIDATION-v2.4.3-source.md`.
