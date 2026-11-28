# PublisherStudio 4.4.3 — source validation

- Source baseline: PublisherStudio 4.4.2 macOS build-fix.
- Production JS: Node syntax checks pass for `componentRuntime.js` and `automationInterop.js`.
- Existing tests: browser-view inspection, Picture Studio canvas regression, and GIF-frame regression pass.
- New tests: panel module browser consent/typed invocation/denial and C#/catalog permission-contract checks pass.
- Real headless Chromium with the actual production componentRuntime.js: a mounted module is inventoried, local hide/show operates, arbitrary JS method names fail; no uncaught browser errors.
- Both maintained JS files have normalized SHA-256 manifest entries; original PowerShell architecture gates and macOS build fixes are unchanged.
- 1-Wire catalog includes two new registered executable module capabilities. The generic input route rejects both dedicated module commands and every module invocation uses fresh human approval.
- Version metadata in both application projects is 4.4.3; full source ZIP CRC test passes.
- **Not performed:** `dotnet build`, PowerShell architecture execution, live DevExpress licensed component initialization, complete LocalGPT Council-to-PublisherStudio workflow, exported website deployment, visual Report Designer integration, or reproduction of the simultaneous VS debugger issue. No claim of compiler-clean or end-to-end functional status.
