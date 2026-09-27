# PublisherStudio 3.7.8 source validation

## Constraints

- PublisherStudio 3.7.6 is the authoritative source baseline.
- The supplied personal 3.7.7 tree is used only as a donor/reference for its framework, package and release-script updates.
- Baseline documentation/help content is retained instead of copying the reduced documentation payload from the donor tree.
- No GitHub/network repository content is used.
- No `dotnet`, restore, NuGet, MSBuild, build, publish or installer command is invoked in this environment.
- Validation is static/source-level and does not claim a compiled runtime result.

## Verified source contracts

- Web, installer and active npm identities are synchronized at 3.7.8 and satisfy the version-slot policy.
- Active browser asset/module cache-busters are synchronized at 3.7.8.
- The supplied .NET/DevExpress security/dependency upgrades are carried forward without importing generated `node_modules`, `bin` or `obj` content.
- Existing 3.7.6 editor, keyboard, pointer, context-menu, Mainframe/Studio, controller and build-guard behavior remains the baseline implementation.
- Maintained JSON/XML/JavaScript and archive structure are checked statically before delivery.
