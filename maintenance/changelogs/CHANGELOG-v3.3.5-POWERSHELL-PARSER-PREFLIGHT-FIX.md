# PublisherStudio 3.3.5 — PowerShell parser preflight fix

Version advanced from 3.3.4 to 3.3.5 because release packaging and preflight scripts changed.

## Fix

- Fixed the same two invalid Apple-notarization interpolations present in PublisherStudio's `NativeReleasePackaging.ps1` (`${SubmissionId}:` and `${ArtifactPath}:`).
- Added an early repository-wide PowerShell parser/compatibility preflight to PublisherStudio, which previously did not have LocalGPT's parser gate and could therefore spend substantial time in source/build checks before reaching a malformed packaging script.
- The PublisherStudio preflight deliberately does not impose LocalGPT's stricter style-only Join-Path separator rule on the existing reviewed PublisherStudio scripts; it enforces parser correctness, unsupported Windows PowerShell 5.1 `String.Contains(..., StringComparison)` usage, and protected platform-variable assignments without creating a broad unrelated path rewrite.
- Added static release-audit coverage that rejects future unbraced variable-plus-colon interpolation regressions.
- Replaced the release-complete message's hard-coded product version with the resolved project version.
- Existing signing/notarization resume logic, compressed embedded documentation, Full/self-contained packaging, Homebrew Ghostscript/RPM support, and render-mode boundaries remain unchanged.
