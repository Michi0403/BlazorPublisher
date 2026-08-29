# PublisherStudio 3.1.5

PublisherStudio 3.1.5 is the **Shared Packaging Pipeline Contract** maintenance release.

It applies the same single-value PowerShell helper contract to PublisherStudio's consumption of the LocalGPT-owned release-packaging tool, preventing `dotnet tool install` progress output from becoming part of the executable-path value.

PublisherStudio continues to use its Windows one-click installer console while Linux and macOS use the shared native package lanes rather than a Unix setup console. Existing application behavior is otherwise preserved. See `CHANGELOG-v3.1.5-SHARED-PACKAGING-PIPELINE-CONTRACT.md` and `VALIDATION-v3.1.5-source.md`.
