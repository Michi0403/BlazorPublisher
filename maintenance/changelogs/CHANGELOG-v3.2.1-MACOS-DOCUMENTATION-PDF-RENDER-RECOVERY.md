# PublisherStudio 3.2.1 - macOS documentation PDF render recovery

## Fixed from the macOS 3.1.9 failure log

- The failing run remained at 83% during DocFX PDF production, warned that an API page timed out, and finally raised `System.TimeoutException: Timeout 1800000ms exceeded` while navigating to `PublisherTwitchEndpointPolicy.html`.
- Edge also emitted `Printing failed`, after which the DocFX PDF plug-in exited without a usable candidate. The release correctly failed because the required complete PDF did not exist, but the fallback strategy was too expensive and fragile on macOS.
- Browser PDF production now validates each candidate before accepting it and retries the complete print book with a compatibility profile when the tagged/outlined renderer fails.
- The compatibility profile omits the expensive tagged-PDF/document-outline switches, keeps the same complete DocFX HTML source set, and declares `html-accessibility-fallback` rather than pretending the PDF is tagged.
- The macOS DocFX Playwright fallback timeout now defaults to five minutes. Windows/Linux keep the existing thirty-minute default. Any positive `DOCFX_PDF_TIMEOUT` supplied by the operator is respected.
- PublisherStudio release validation now recognizes `html-browser-print-compatibility` and applies the same complete-source-page/API coverage checks to both browser PDF modes.

## Preserved

- PublisherStudio 3.2.0 WSL2 headless Linux release backend and clean no-WSL fallback.
- LocalGPT-owned `LocalGPT.ReleasePackaging` 1.0.1 local-first consumption.
- DevExpress parent preparation/license boundary.
- Windows setup contract and explicit InteractiveServer boundaries.
