# PublisherStudio 2.6.9 LocalGPT session-durability release

PublisherStudio 2.6.9 keeps Publisher-started Council work attached to the normal LocalGPT `/chat` history contract. Council requests sent through the existing 1-Wire bridge are now always marked for durable LocalGPT session persistence, allowing LocalGPT 2.8.6 to retain provider-supplied reasoning, function traces, team/round output, and partial/final Council state consistently with locally started sessions.

No other working PublisherStudio editor, media, Panel Studio, export, localization, render-mode, publication-format, or installer behavior is reverted.

No GitHub access or .NET/MSBuild invocation was used to prepare this source release.

## Compatibility

- PublisherStudio.Web and PublisherStudio.InstallerConsole are 2.6.9.
- 1-Wire protocol remains 2.1.1.
- Publication format remains 1.58.
- Picture Studio format remains 1.5.
- The five reviewed InteractiveServer render-mode directives are unchanged.
- No database migration is required.
