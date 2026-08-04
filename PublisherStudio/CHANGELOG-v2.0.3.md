# PublisherStudio 2.0.3 — Windows build-policy compatibility repair

PublisherStudio 2.0.3 keeps the 2.0.2 preservation-first installer and runtime repairs, while correcting the Windows build gates exposed by the first maintainer rebuild.

## Closed

- Architecture audit wrappers now preserve Python output for Visual Studio diagnostics while returning only the integer process exit code. Successful output can no longer be interpreted as an error-code value.
- Iterator policy parsing excludes class, struct, record, interface and enum declarations, including primary-constructor classes.
- `PublicationElementTraversal` now returns a logged materialized traversal instead of an unguarded iterator.
- WinGet FFmpeg executable discovery now returns a logged materialized collection, allowing bounded per-directory exception handling without a `catch` inside an iterator.
- All fourteen maintained publish profiles explicitly declare Release configuration, filesystem protocol, platform and profile-owned output. Setup profiles remain self-contained single-file payloads; application profiles remain self-contained multi-file payloads.
- Installer workflow validation now checks every maintained Visual Studio launch profile by exact name using Windows PowerShell 5.1-safe property enumeration.
- The new regression test protects these five failures from returning.

## Partial

- C# and Razor compile-safety remains source-scanned in this environment. A native .NET 10/DevExpress build is still maintainer-run because the required SDK and licensed feed are unavailable here.

## Deferred

- DocFX/XML-comment expansion remains deferred as requested.
- Broader optional 1-Wire behavior remains a later step after installer and publish acceptance.
