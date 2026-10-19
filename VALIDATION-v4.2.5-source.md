# PublisherStudio 4.2.5 source validation

The source change is confined to the existing documentation PowerShell pipeline and version/cache-busting references. The supplied 4.2.4 Windows log independently verifies that C# compilation, localization, DevExpress retention, service architecture, JavaScript diagnostics, XML-documentation coverage, and DocFX HTML preflight already completed successfully. It does **not** verify that the 4.2.5 PDF repair runs on Windows.

## Design checks completed without PowerShell/.NET

- Front matter is partitioned into consecutive, nonoverlapping slices, with a maximum of 48 references per browser call. The 741-source-page case in the incident yields 16 parts (15 x 48 + 21); no reference is dropped.
- Every generated front-matter part is included in the existing PDF merge list, before the regular body chunks. Only the first index part includes the cover; subsequent parts preserve the namespace/type/member table of contents for their slice.
- A runtime completeness assertion checks that the summed index entries equal `TotalPageCount` before body printing begins.
- Existing hash-scoped PDF reuse, browser retry/cleanup and full final PDF requirements remain active.
- PDF header and %%EOF checks remain active; 16 KiB is only the minimum size for smaller front-matter shards. Full PDF and body size thresholds are unchanged.
- PowerShell maintenance code remains PowerShell; no build guards were disabled or new packages added.

## Not verified here

Windows PowerShell 5.1 syntax/execution, the repository's PowerShell architecture scripts, MSBuild/.NET, installed Edge printing, PDFsharp merge and DocFX output under the user's Windows setup could not be executed. The release is source-only; the Windows Debug build and documentation PDF run are the authoritative acceptance tests. If a bounded part still stalls, its `part N/M` log line and browser diagnostics will distinguish the failing input from the formerly unbounded cover/index.
