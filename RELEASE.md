# PublisherStudio 4.0.3

PublisherStudio 4.0.3 repairs the repository-local build-storage preflight introduced in 4.0.2. Fresh clones require no cache-path configuration: heavy build state defaults to `artifacts/.build-storage`, which is ignored by Git. Optional environment/parameter values only override that default.

The PowerShell compatibility guard now treats source-code variable names literally when validating documentation browser-profile placement, avoiding the StrictMode `$documentationToolCacheRoot` failure seen on macOS. Application/editor runtime behavior is unchanged.
