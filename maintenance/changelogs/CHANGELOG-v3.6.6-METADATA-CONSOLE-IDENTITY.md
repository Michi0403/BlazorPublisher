# PublisherStudio 3.6.6 - metadata-backed console identity

PublisherStudio 3.6.6 adds a compact identity header to the normal PublisherStudio application console and PublisherStudio Setup console. The header is generated from project/assembly metadata instead of duplicated product constants.

## Startup identity

The first application/installer lines now show:

- product display name and semantic version;
- canonical repository URL;
- project owner/author;
- SPDX project license expression.

`Directory.Build.props` remains the canonical repository-level source for `Authors`, `RepositoryUrl`, `RepositoryType`, and `PackageLicenseExpression`. Those values are emitted as assembly metadata and consumed by the shared `ConsoleProductIdentity` helper. Each executable supplies only its standard MSBuild `Product` display name.

PublisherStudio Setup now also derives the release repository owner/name slug from the same metadata-backed repository URL instead of the previous separate `Michi0403/BlazorPublisher` runtime constant.

No version, repository URL, owner, or license value is hardcoded in the console writer.

The 3.6.5 macOS native-bundle batching/signing inventory repair remains unchanged.
