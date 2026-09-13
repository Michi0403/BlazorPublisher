# PublisherStudio 3.6.6

PublisherStudio 3.6.6 adds metadata-backed product identity to the normal application console and PublisherStudio Setup console. Their first lines now expose product/version, canonical repository URL, owner, and project license without duplicating those values in startup code.

Repository-level `Authors`, `RepositoryUrl`, and `PackageLicenseExpression` remain canonical in `Directory.Build.props`; generated assembly metadata carries them into the executable, and a shared console identity helper renders them. PublisherStudio Setup also derives its GitHub owner/name release slug from that same repository URL instead of maintaining a separate runtime repository constant.

Application/editor behavior is otherwise unchanged from 3.6.5, and the macOS bundle-inspection batching/signing inventory reuse remains intact.

See `CHANGELOG-v3.6.6-METADATA-CONSOLE-IDENTITY.md` and `VALIDATION-v3.6.6-source.md`.
