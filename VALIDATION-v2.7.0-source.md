# PublisherStudio 2.7.0 source validation

Validation is source-only by design. No `dotnet`, MSBuild, Visual Studio build, GitHub access, restore, publish, or executable launch was performed.

Checked statically:

- PublisherStudio.Web and PublisherStudio.InstallerConsole report version 2.7.0 and obey the single-digit minor/patch slot policy;
- browser module cache-busters use 2.7.0;
- wire protocol remains 2.1.1;
- all six built-in localization catalogs contain the maintained Local Chat phrase/text keys without `ChatGPT` values;
- obsolete `LocalChatGPT` localization aliases are absent;
- all six localization catalogs have identical key sets;
- PublisherStudio still contains 5 `@rendermode` directives, matching the prior source release exactly;
- repository Python regression audits were run where they do not invoke .NET tooling;
- generated Python bytecode/cache output is removed before packaging.

The Windows build and runtime test remain authoritative.
