# PublisherStudio 2.7.8 source validation

Source-only validation. No .NET SDK/build/restore/publish command was run.

Validation performed for this source package:

- Parsed both project files and confirmed version `2.7.8`.
- Parsed PublisherStudio JSON configuration files used by the changed paths.
- Ran `build/audit_release_2_7_8.py` for the default-template source, preview-backed recovery, saved-content precedence, active cache-busters, reviewed InteractiveServer boundaries, live LocalGPT text/Council capability contracts, and wire protocol pin.
- Ran the maintained application architecture source audit.
- Ran the maintained service resilience source audit against the changed service methods.
- Ran the maintained async-continuation source audit.
- Ran the maintained Panel Studio persistence source audit.
- Ran the maintained XML documentation coverage/quality audit.
- Ran the maintained documentation/1-Wire contract source audit.
- Re-ran the maintained 2.6.8 through 2.7.7 release regression audits plus the current AI preview/export, picture/page-effect, Media Studio/localization, and strict-async regression audits with their current-version/cache assertions rolled forward to 2.7.8.
- Compared the supplied publication fixtures: the failing initial fixture contains visible `previewHtml` with an empty `documentContent`, while the edited fixture contains both the edited preview and a non-empty OpenXML payload. The 2.7.8 recovery path directly addresses that split without overwriting non-empty saved payloads.
- Confirmed the five reviewed InteractiveServer render-mode directives are unchanged.
- Wire protocol remains `2.1.1`.
