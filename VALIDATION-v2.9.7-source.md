# PublisherStudio 2.9.7 source validation

This archive is **source-only and not compiled** in the preparation environment. No `dotnet`, MSBuild, NuGet restore/publish, Visual Studio build or DevExpress licensed build step was run.

## Authoritative build feedback addressed

The user's Windows build exposed two concrete 2.9.6 defects after the repository gates ran:

1. `Assert-ComponentDiagnostics.ps1` rejected `Components/Editor/NewPublicationDialog.razor` because the new component had catch/log handling but no user-facing `IUserNotificationService` call.
2. C# compilation then rejected `Components/Editor/PanelStudio.razor` with CS0136 because `templateId` was declared in the Div-template branch while an `out var templateId` declaration existed in the sibling component-template condition.

2.9.7 fixes both source defects directly. The chooser now reports recoverable discovery failures through `Notifications.Error(...)`, and the two Panel Studio locals are named `divTemplateId` and `componentTemplateId`.

## Source checks run

- `audit_application_architecture.py --root . --product publisherstudio --mode all`
- `audit_async_continuations.py --source-root src/PublisherStudio.Web`
- `audit_component_resilience.py --root .`
- `audit_prerender_interop_safety.py --root .`
- `audit_service_resilience.py --root . --product publisherstudio`
- `audit_iterator_exception_policy.py --root .`
- `audit_panelstudio_persistence.py`
- Node syntax checks for `componentRuntime.js` and `publisherInterop.js`
- repository-equivalent `Assert-ComponentDiagnostics.ps1` new-component rule: no missing catch/log/notification boundary remains
- `audit_release_2_9_7.py` release-contract checks

## Release-contract evidence

- Web, installer and npm metadata resolve to **2.9.7** and the minor/patch slots remain single digit.
- DevExpress/DevExtreme remains **25.2.9**.
- `NewPublicationDialog.razor` contains explicit logger and notification handling for recoverable template discovery failures.
- `PanelStudio.razor` no longer contains the compiler-rejected sibling `templateId` declarations.
- Local complete-publication and Div template libraries, starter files, identity regeneration/reference remapping and publication/Panel integration from 2.9.6 remain present.
- `Configuration\**\*` continues to be copied to output and publish, covering the shipped template seeds.
- Native media/form controls remain excluded from wrapper/signal click ownership, and signal media recursion suppression remains present.
- Six localization catalogs remain in exact key parity.

The user's licensed Windows .NET 10 + DevExpress build remains authoritative for final Razor/C# compilation and runtime validation.
