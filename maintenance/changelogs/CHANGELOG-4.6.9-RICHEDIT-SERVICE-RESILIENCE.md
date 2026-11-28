# PublisherStudio 4.6.9 — RichEdit markup resilience build fix

- Fix the 4.6.8 `Assert-ServiceMethodResilience.ps1` failure for `PublicationMarkupService.FormatInline` by adding the repository-required method-local `try/catch`, service `ILogger` diagnostics, and exception propagation.
- Add the same method-local service resilience policy to the helper `ReplaceMarker` used for inline formatting so it cannot become a subsequent build failure.
- Preserve HTML encoding, safe rich-text formatting, approved proposal insertion and existing functionality. Do not weaken, bypass or suppress architecture guards.
- Do not add Python, Node or release-version-pinned tests. Existing checks remain authoritative and versions are advanced in both PublisherStudio projects.

## Validation limits

- Source review, project-XML validation and archive CRC can be completed offline. The environment does not include PowerShell or .NET 10 and therefore cannot confirm a successful Windows Visual Studio build or live DevExpress RichEdit insertion.
