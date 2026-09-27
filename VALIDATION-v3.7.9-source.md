# PublisherStudio 3.7.9 source validation

Source-only handoff validation. No `dotnet`, MSBuild, NuGet, publish, installer or GitHub operation was executed.

Checked statically:

- `PublisherStudio.Web` and `PublisherStudio.InstallerConsole` report 3.7.9.
- `DevExpressVersion` remains 25.2.10.
- `package.json` and `package-lock.json` target `devextreme-dist` 25.2.10 and `devexpress-aspnetcore-spreadsheet` 25.2.10.
- active application, Spreadsheet and localization cache-busters no longer request DevExtreme 25.2.9.
- the publish-time runtime-key/version guard remains intact.
- the asset manifest permits npm's no-SRI lock form only through schema 5 with exact-version/resolved-URL checks plus prepared asset SHA-256 values.
- maintained JavaScript was syntax-checked with the available Node.js runtime; this is not a .NET build.
