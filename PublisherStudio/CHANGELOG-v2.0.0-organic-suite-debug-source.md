# PublisherStudio 2.0.0 organic suite debug source candidate

## Purpose

PublisherStudio and LocalGPT now share major version 2 and use LocalGPT's release package as the authoritative 1-Wire contract. This is a source candidate for maintainer compilation and runtime debugging.

## Compiler and startup fixes

- Fixed `Localization.razor` CS0542 by renaming the injected service member to `LocalizationService`.
- Added structured `ILogger<Localization>` diagnostics and `IUserNotificationService` feedback to localization load/save failures.
- Retained the startup repair that skips factory registrations without a concrete reflection `ImplementationType` and constructs non-null architecture descriptors.
- Retained launch-profile port behavior so an explicit installer/CLI port wins, while ordinary Visual Studio launch remains on `127.0.0.1:5198`.
- All maintained Razor pages have typed logging and frontend notification injection.

## Authoritative protocol package

- Removed the competing PublisherStudio protocol source project from `src` and the solution.
- `PublisherStudio.Web` consumes `LocalGPT.WireProtocolVersion` version `2.0.0` through PackageReference.
- Normal release builds download the matching package from the LocalGPT v2.0.0 release before publishing.
- `-UseBundledWireProtocolPackage` is an explicit offline/debug fallback only.
- The package is copied beside every application/setup release in a `protocol` folder and as a release asset.

## Paired application workflow

- Transport-connected and user-approved linked states remain separate.
- PublisherStudio initiates a link; LocalGPT's frontend user must approve it before Council/organic features enable.
- Per-peer/capability/organ settings control exposure, invocation, frontend confirmation, editor type, approval mode and work-order scope.
- Incoming human-input requests return through the exact correlation ID after the PublisherStudio frontend user confirms or supplies text/JSON.
- Screenshot work explicitly records both PublisherStudio frontend confirmation and the browser's current-session user-gesture/permission requirement.

## Story Editor AI Council integration

- The AI Council ribbon tab is visible only while PublisherStudio is securely linked and LocalGPT advertises Council support.
- The user selects a Council team, enters a story request and sends with Ctrl+Enter or the button.
- The request includes publication/page/story context and asks LocalGPT to return a `publisher.text.proposal.request` proposal.
- The Council never writes directly into the publication. The proposal is retained, reviewable and inserted at the current caret only when the PublisherStudio user presses **Insert at caret**.
- Council processing may continue after the Story Editor closes; reopening and refreshing restores pending proposals.

## Retained interactive/editor repairs

Repository contract tests retain Panel/Div live-content movement, two temporal selection boundaries, clickable sequence editing, responsive liveboards, standalone HTML hover/tooltips/signals, configurable localization and browser/app language switching.

## Validation performed here

- Complete PublisherStudio npm/source-contract suite: recorded in `TEST-RESULTS-v2.0.0-organic-suite-debug-source.txt`.
- Package authority, release-download, Story Editor, frontend confirmation, runtime bootstrap, localization, media interaction and workspace-preservation checks are included.
- JSON/XML/project-reference/archive checks are recorded in the delivery verification report.

## Build truth

A native .NET 10/DevExpress build was not possible in this workspace. This is a **debug source candidate**, not a compiler-verified release.
