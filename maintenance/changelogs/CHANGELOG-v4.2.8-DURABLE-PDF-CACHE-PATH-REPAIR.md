# PublisherStudio 4.2.8 — Windows documentation PDF cache-path repair

## Reproduced failure

The 4.2.7 Windows Debug transcript shows that the application compiled, architecture audits passed, DocFX generated its API documentation successfully, and the HTML accessibility/link preflight passed. Browser rendering then stopped **before starting Microsoft Edge**, because the retained LocalGPT Windows path guard rejected a 258-character output filename for `front-matter.pdf` (240-character PDF-output safety threshold; profile path was 153 characters). The build correctly failed because the mandatory PDF had not been generated.

## Root cause and correction

Unlike LocalGPT, PublisherStudio had nested its content-addressed browser PDF chunks under the DocFX HTML payload cache:

`<documentation>/PublisherStudio/DocumentationTools/payload-cache/PublisherStudio/<sha256>/browser-pdf-chunks/v2-p100-n741/front-matter.pdf`

The new location is directly under the existing repository-owned documentation cache:

`<documentation>/PublisherStudio/p/<sha256>/v2-p100-n741/front-matter.pdf`

The 64-character full documentation hash is unchanged and remains the isolation/resume key. The `v2` page-batch folder naming is unchanged. Under the exact repository path in the supplied build log, this reduces the first output path from **258** to **192** characters. It retains ample clearance for other PDF part filenames and still respects `FUTURE2_BUILD_STORAGE_ROOT` and `FUTURE2_DOCUMENTATION_CACHE_ROOT` as supported by the repository's build-storage helper.

The location of the HTML cache and its manifest is not changed; only the durable browser PDF parts are relocated. Existing immutable generated HTML and validated whole-PDF cache handling remains intact. Failed/partial parts in the old directory are not reused as valid output. The newly created directory is persistent across bin/obj cleanup, and later builds can resume correctly from validated PDFs.

## Architecture / behavior

- The original browser path safety guard remains unchanged. No MAX_PATH exemptions, silent skipping, or PDF fallback policy bypasses.
- Browser profiles, flags, print retry, progress heartbeat, part-level integrity verification, UTF-8 merge input manifest, and `LocalGPT.ReleasePackaging` version `1.1.2` stay unchanged.
- The build still requires a complete generated PDF, plus all DocFX HTML and link checks.
- No Editor/DevExpress controls, localization catalogs, business services, runtime state, or LocalGPT source changes.
- Added one informational line showing the resolved durable PDF chunk cache path, so the actual path is visible in Visual Studio build output.

## Verification boundary

The path computation was checked against the exact Windows paths and key in the supplied build failure; the new path is 192 characters vs. the unchanged 240-character guard. Source and ZIP integrity checks were performed without GitHub, PowerShell or .NET. Real Windows Edge rendering remains unverified and should not be reported as successful until the next Debug build completes.
