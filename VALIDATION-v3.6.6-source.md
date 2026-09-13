# PublisherStudio 3.6.6 source validation

Source-only validation for metadata-backed console identity.

The 3.6.6 release gate verifies that:

- PublisherStudio and PublisherStudio Setup are versioned 3.6.6;
- both executable project files define a standard MSBuild `Product` display name;
- repository-level `Authors`, `RepositoryUrl`, and `PackageLicenseExpression` are emitted as assembly metadata;
- the shared console identity helper resolves product/version/repository/owner/license from generated assembly metadata and contains no PublisherStudio-specific identity values;
- both application and setup entry points print the identity header before normal startup diagnostics;
- PublisherStudio Setup derives its release repository slug from the metadata-backed repository URL rather than a duplicate repository constant;
- the 3.6.5 macOS bundle-inspection batching and signing inventory reuse remain intact.

Maintained release, architecture, async-affinity, component/service resilience, prerender, iterator, Panel Studio persistence, cross-platform, PowerShell interpolation, and XML-documentation audits are re-run against the delivered source tree.

`dotnet`, signing, notarization, and native release publishing are not executed in this source-validation environment; the user's release environment remains authoritative for those stages.
