# PublisherStudio 4.3.4 — Stable goal correlation with LocalGPT 6.1.6

## Base and scope

Based on the supplied PublisherStudio 4.3.3 source ZIP. PublisherStudio stays a client of the existing secure 1-Wire Council request/response path. Goal planning, execution and checkpoint persistence remain exclusively owned by LocalGPT.

## Implemented

- Added optional `GoalId` to `PublisherAiChatResponse`. The bridge safely reads the nested `GoalExecution.GoalId` from the existing structured Council result, preserving the separate Council `RunId` and the full `ResultJson`. This distinguishes a stable original goal from its potentially multiple Council invocations.
- Older LocalGPT peers without a goal summary continue to return an empty `GoalId` without affecting normal response parsing. No new browser authority, file operation, direct database action or unapproved install path is introduced.
- Added a source-level contract test and advanced the Web/InstallerConsole versions **4.3.3 → 4.3.4**.

## Limits and validation

- This bridge does not expose LocalGPT's operator recovery endpoint as an automatic PublisherStudio retry command. Interrupted external operations still require review and approval on the LocalGPT side. No claim is made for universal cross-app media adapters or a complete event/artifact bus.
- Python source tests are source-level assertions only. No .NET/DevExpress build, PowerShell gate, installed 1-Wire interoperability test, or live AI execution was performed.
- PowerShell architecture scripts and build targets remain unchanged.
