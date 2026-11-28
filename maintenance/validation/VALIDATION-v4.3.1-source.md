# PublisherStudio 4.3.1 source validation

Baseline: PublisherStudio 4.3.0 full source archive. This is an organizational refactor with no intentional behavior change.

## Checks performed here

- No baseline source files removed; complete repository archive made from the baseline plus the reviewed edits.
- Original `MediaStudio.razor` C# `@code` body reconstructs **byte-for-byte** from the eight `MediaStudio.*.razor.cs` partials, omitting only the three new presentation binding properties.
- Every new MediaStudio C# partial contains fewer than 1,500 lines and has balanced C# braces under lexical analysis.
- The existing Media Studio DevExpress controls are retained across the parent and footer; native close button moved unchanged to the header. The retained source diagnostic catch count and existing text operations are mapped to the new paths.
- The titlebar/footer preserve original classes/DOM semantics and delegate actions to the same parent callbacks. The main JS modules, media Services, BusinessObjects, schemas, migrations and config remain byte-identical to 4.3.0.
- ZIP CRC integrity tested.

## Not run here

Windows PowerShell architecture guards, `dotnet build`, DevExpress compiler, Blazor runtime, interactive media editing, recording, or playback. A static lexical check is not a C# compilation result.

## Recommended Windows acceptance checks

Run `dotnet build` with the licensed DevExpress feed (and existing PowerShell build gates); open both audio/video Media Studio, use import, preview, seek, trim, timeline, effects, record, cancel, apply, and export; reopen existing publication media. Confirm localized title/footer labels, footer button enablement, and popup sizing. Compare those actions to 4.3.0 using the same media. No user-data migration is needed.
