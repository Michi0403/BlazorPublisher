# PublisherStudio 4.2.5 — bounded PDF cover/index rendering

## Reproduction

A Windows Debug build of PublisherStudio 4.2.4 compiled its assembly and passed the PowerShell architecture gates, XML-documentation coverage, DocFX cache restoration and HTML preflight (745 HTML files, 722 API HTML files). The documentation PDF step then stalled while Edge printed a single complete cover/index for 741 reference pages. After the 480-second browser timeout and renderer-profile recovery attempts, it retried the same unbounded cover/index rather than advancing to the already-chunked body.

## Repair

- `build/Build-Documentation.ps1` prints the **complete** cover/index as bounded parts of at most 48 source-page entries, never one giant browser job. Every reference is still indexed; namespace/type/member groupings and the existing print theme remain intact.
- The existing `New-PublisherStudioHtmlPrintBook` page slicing and stable source-page ordering now also apply to front matter. Only the first part contains the document cover; subsequent parts contain their portion of the table of contents.
- The existing isolated-browser execution, bounded retries, PDF header/EOF validation, durable hash-scoped part cache, managed PDF merge, DocFX HTML generation/preflight and strict required-PDF policy remain in force. Completed index parts survive a failed subsequent part and are reused on the next build.
- An explicit indexed-entry count check refuses to merge or publish if the print slices do not cover every reference page.
- Only small cover/index parts use an appropriate 16-KiB minimum PDF size; body chunks and the merged full PDF retain their existing minimum sizes and structural header/EOF validation.
- The print log identifies the exact cover/index part that is rendering, reused, or failed instead of apparently hanging on a single unnamed operation.

No architecture guard, localization catalog, managed C# API or application behavior was changed for this documentation-only fix. All changes stay in the repository's PowerShell documentation pipeline, with no additional dependency or Python build step.
