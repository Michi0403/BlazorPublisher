# PublisherStudio 4.6.2 — Reusable form boundaries and all-form source audit

- Audited all Razor components in both provided applications; see `FORM_AUDIT_ALL_COMPONENTS.md` and CSV for 96 files that contain form/layout boundaries (out of 112 scanned).
- The mandatory read-only `build/Assert-RazorFormEncapsulation.ps1` is wired to normal app builds and release preflight. It must not rewrite implementations or suppress other guards.
- Extracted the advanced Council request from `OrganicPlugins` into `OrganicCouncilRequestForm`, reusing the existing request BusinessObject, Council team inventory, transport service, user approval and notifier.
- Introduced a DevExpress EditForm/DxFormLayout boundary for this workflow and kept `OrganicPluginController` as the existing HTTP entry point.

## Validation limit

Source static and JS tests run offline. `pwsh` and `dotnet` were not available in the environment, so the mandatory PowerShell gates and .NET Razor compiler could not be executed. Treat this as a source patch pending the next Windows build; do not call it a successful build.

- Introduced a DataAnnotations OrganicCouncilRequestFormModel BusinessObject shared by the child form and the existing HTTP Council endpoint. Invalid HTTP input receives ValidationProblem(ModelState), without altering 1-Wire transport.
