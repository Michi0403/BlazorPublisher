# PublisherStudio 2.4.3 source validation

Scope: source-only validation. The requested workflow explicitly excludes .NET compilation and online GitHub/repository access.

## Evidence reviewed

- The supplied live DOM capture shows the PublisherStudio documentation modal and iframe using `/help-docs/index.html`.
- The supplied `winx64.zip` release payload contains `wwwroot/help-docs/index.html`, `api/index.html`, `documentation-status.json`, DocFX assets, Kawaii assets, and the versioned documentation PDF. This isolates the observed failure to in-application delivery/routing rather than absence of the generated release payload.

## Version and routing checks

- PASS — `PublisherStudio.Web` version is 2.4.3.
- PASS — `PublisherStudio.InstallerConsole` version is 2.4.3.
- PASS — minor and patch slots remain single-digit.
- PASS — Help ribbon HTML and API viewer commands use `/api/documentation/html/...`.
- PASS — Organic/1-Wire documentation profile advertises `/api/documentation/html/...`.
- PASS — the scoped documentation viewer rewrites legacy `/help-docs` requests to the canonical controller-backed route.
- PASS — no direct `Url = "/help-docs..."`, `HtmlRoute = "/help-docs..."`, or `ApiRoute = "/help-docs..."` remains in PublisherStudio UI/capability source.

## Deployment guards

- PASS — `wwwroot/help-docs/**` is explicitly marked for output and publish copying.
- PASS — ordinary publish validation requires the HTML index, API index, status metadata, DocFX CSS/JS, and PublisherStudio Kawaii CSS/JS.
- PASS — release-archive validation rejects missing, empty, or truncated core documentation entries.
- PASS — installer archive validation rejects missing, empty, or truncated core documentation entries.
- PASS — post-extraction installer validation rejects missing/truncated documentation and a documentation manifest version that differs from the installed application assembly version.
- PASS — documentation controller responses disable browser caching to prevent a stale prior missing response from surviving an update.

## Repository static audits

- PASS — `build/audit_application_architecture.py --root <repo> --product publisherstudio --mode all`
- PASS — `build/audit_documentation_onewire_contracts.py`
- PASS — `build/audit_service_resilience.py --root <repo> --product publisherstudio`
- PASS — PublisherStudio project files parse as XML.
- PASS — static route-regression scan found no direct in-app viewer/profile `/help-docs` route.

## Validation boundary

No `dotnet`, MSBuild, restore, publish, runtime browser test, GitHub call, or network repository access was performed. Therefore this source package deliberately makes no compiler-clean or runtime-tested claim.
