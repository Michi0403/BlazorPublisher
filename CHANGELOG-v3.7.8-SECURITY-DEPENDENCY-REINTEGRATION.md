# PublisherStudio 3.7.8 — security dependency reintegration

## Changed

- Uses PublisherStudio 3.7.6 as the source baseline and selectively carries forward the reviewed dependency/tooling updates from the personal 3.7.7 tree.
- Updates the pinned .NET SDK from 10.0.301 to 10.0.401.
- Updates DevExpress packages from 25.2.9 to 25.2.10.
- Updates Microsoft.Extensions.DependencyModel, System.CodeDom, System.Configuration.ConfigurationManager, System.Security.Cryptography.Pkcs and installer logging packages from 10.0.11 to 10.0.12 where present in the supplied 3.7.7 tree.
- Preserves the existing PublisherStudio source/documentation payload from 3.7.6 instead of carrying forward the personal ZIP's size-reduction deletions.
- Retains the compile-only release-validation entry point from the supplied 3.7.7 tooling so maintainers can run the repository-owned compile validation intentionally.

## Versioning

- PublisherStudio.Web and PublisherStudio.InstallerConsole now report 3.7.8, advancing beyond both the 3.7.6 baseline and the supplied 3.7.7 comparison tree.

## Validation boundary

Source-only validation is used for this handoff. No GitHub access, `dotnet`, NuGet restore, MSBuild, build, publish or installer execution is performed. JSON/XML/source-policy checks and archive integrity are validated independently.
