# PublisherStudio 2.6.7 source compile repair

PublisherStudio 2.6.7 repairs the malformed generic JavaScript interop expression reported by the authoritative Windows .NET build and removes the two accompanying warnings without changing runtime ownership. Existing InteractiveServer render-mode boundaries are preserved. No GitHub access or .NET build was used to prepare this source release.

## Compatibility

- PublisherStudio.Web and PublisherStudio.InstallerConsole are 2.6.7.
- 1-Wire protocol remains 2.1.1.
- Publication format remains 1.58.
- Picture Studio format remains 1.5.
- No database migration is required.
