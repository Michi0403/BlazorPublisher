# PublisherStudio v1.0.31 — Spreadsheet hibernation startup fix

## Fixed

- PublisherStudio now creates the DevExpress Spreadsheet hibernation directory before the Spreadsheet service is registered.
- Prevents `DirectoryNotFoundException` during application startup when `%LOCALAPPDATA%\PublisherStudio\SpreadsheetHibernation` does not exist yet.
- The directory check is idempotent: an existing hibernation directory is reused and is not cleared or recreated.
- The configured DevExpress hibernation timeouts and application-shutdown behavior are unchanged.

## Runtime behavior

The application creates this user-local directory automatically when required:

```text
%LOCALAPPDATA%\PublisherStudio\SpreadsheetHibernation
```

No manual folder creation is necessary. Node.js remains a development/release-build requirement only; published PublisherStudio installations do not require Node.js or npm at runtime.

## Validation

- Confirmed directory creation occurs before `AddDevExpressControls` and before DevExpress can initialize hibernation storage.
- Confirmed the exact created path is the same path assigned to `hibernation.StoragePath`.
- Confirmed package JSON and lockfile versions were advanced to `1.0.31`.
- A complete licensed DevExpress build could not be run in this environment because the .NET SDK and licensed package feed are unavailable.
