# PublisherStudio 3.8.0 source validation

Source-only validation was performed without invoking `dotnet`, MSBuild, NuGet, publish tooling, installer tooling, or GitHub.

Validated in the handoff environment:

- `package.json` and npm lock metadata resolve DevExtreme/Spreadsheet 25.2.10;
- the Node lock probe handles the npm v3 empty root-package key and reports exact 25.2.10 resolved URLs;
- changed JavaScript parses with Node.js;
- PublisherStudio version/cache-buster sources identify 3.8.0;
- optional GUID parsing is guarded by `JsonValueKind.String`;
- source policy audits available without .NET are run and recorded by the handoff process;
- the source archive is checked for ZIP integrity after packaging.

The actual Windows PowerShell 5.1 preparation and .NET release pipeline remain authoritative on the target development machine because this environment does not provide Windows PowerShell or the licensed DevExpress runtime-key generation context.
