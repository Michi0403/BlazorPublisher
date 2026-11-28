# PublisherStudio 3.5.4 source validation

Static source validation only. No `dotnet build`, `dotnet publish`, native package build, or GitHub operation was performed in the assistant environment.

The Windows updater now performs an explicit installed-runtime ownership/termination step after both release archives have downloaded and passed archive validation but before either archive is extracted. Ownership requires the installed `PublisherStudio.Web.exe` path, either from the runtime endpoint executable identity or from the live Windows process executable path. Debug and alternate-host executables are left untouched. The owned endpoint is removed before extraction, and startup remains after successful installation.
