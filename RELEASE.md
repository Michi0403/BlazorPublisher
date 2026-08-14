# PublisherStudio 2.6.8 panel/media/localization interaction repair

PublisherStudio 2.6.8 repairs the blank Panel Studio authoring layout, restores nested-panel pointer ownership in presentation output, makes notification toasts expire after ten seconds, exposes direct Picture Studio layer-order controls, gives Audio Studio the same shared sequence drag/drop surface as Video Studio, improves export-dialog button contrast, and makes browser WebM export substantially more compression-oriented without duplicating a successfully converted source video.

The built-in localization set is now `de-DE`, `en-US`, `es-ES`, `fr-FR`, `ja-JP`, and `uk-UA`, with key parity across all six catalogs. Product names, protocol names, format names, command-line literals, units, and other technical identifiers remain canonical where translation would be misleading.

No GitHub access or .NET/MSBuild invocation was used to prepare this source release.

## Compatibility

- PublisherStudio.Web and PublisherStudio.InstallerConsole are 2.6.8.
- 1-Wire protocol remains 2.1.1.
- Publication format remains 1.58.
- Picture Studio format remains 1.5.
- The five reviewed InteractiveServer render-mode directives are unchanged.
- No database migration is required.
