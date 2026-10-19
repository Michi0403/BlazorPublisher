# PublisherStudio 4.2.7 — source validation

## LocalGPT parity review

Source baseline: `LocalGPT-6.0.8-source.zip`, `build/Build-Documentation.ps1`.

PublisherStudio's `Invoke-PublisherStudioBrowserPdf`, retry function, `Invoke-PublisherStudioChunkedBrowserPdf`, and Windows browser path preflight were ported from that LocalGPT source, with only the following intentional differences:

- names are adapted to PublisherStudio's existing PowerShell build owner;
- the NuGet tool is executed using `dotnet <assembly.dll>` because PublisherStudio extracts a verified .NET tool package rather than receiving an executable application host;
- browser stdout/stderr draining after forced termination is bounded;
- browser print progress is emitted every five seconds during a pending operation and reports the current part totals.

The prior local PDF preflight, 48-entry index slices, profile-priority mutation and renderer flags that differed from LocalGPT have been removed. The renderer again uses `--virtual-time-budget=180000`, short `bp/p<guid>/c` profiles, output/profile path-length preflight on Windows, `v2-p<n>-n<n>` durable PDF chunks, and `pdf-merge --input-list`.

## Checks performed without PowerShell/.NET

- Compared ported PowerShell function blocks with the accompanying working LocalGPT source.
- Checked only the PublisherStudio documentation build script, version/cache-busting references and this release's documentation were changed.
- Verified the version transition `4.2.6 -> 4.2.7` and that the unchanged `1.1.2` NuGet tool's *source* supports both `capabilities` and `pdf-merge --input-list`.
- Checked unique invocation/function definitions, provenance of the renderer flags, progress messages, short path conventions, and no bypass to mandatory PDF validation.
- Checked the complete ZIP for archive errors and ensured the output is the full source repository.

## Not verified

The Windows PowerShell 5.1 parser/audit suite, licensed .NET 10 compile, Microsoft Edge browser printing and the end-to-end 741-document PDF creation cannot be executed here. The next owner's Debug build is required to verify the runtime failure is resolved; the status must not be reported as compiler- or browser-tested.
