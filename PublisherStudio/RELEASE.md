# PublisherStudio 2.0.5 Windows build and release-script correction

See `CHANGELOG-v2.0.5.md` and `docs/architecture/task-ledger.md`.

PublisherStudio 2.0.5 corrects the Windows-only failures exposed after the 2.0.4 service-owned compiler repair. `SpreadsheetDocumentService` now has one DI constructor, `Build-Release.ps1` uses Windows PowerShell 5.1-safe variable delimiting, and deterministic instance services are explicitly classified by the maintained logging policy instead of receiving artificial catch-and-rethrow blocks. No application statics were introduced; shared data contracts remain under `PublisherStudio.BusinessObjects`.
