# PublisherStudio 2.8.6 source validation

This release was validated statically without invoking the .NET toolchain.

## Reported blockers repaired

- The eight `CS1997` sites reported in `Editor.razor` no longer contain `return await` in non-generic `async Task` methods.
- Static scan found and repaired five more equivalent non-generic `async Task` sites in Organic Security, Picture Editor, and Publication Timeline.
- A syntax-aware component scan now reports no `return await` in any non-generic component `async Task` method.
- The exact text-service ownership pattern/baseline logic reports zero new direct component/controller string/regex operations.

## Strict architecture results

- Component resilience: 2,614 component methods own method-local diagnostics boundaries; zero legacy exemptions.
- Service resilience: 1,306 service methods own `try/catch + diagnostics`; 4 service iterator/yield methods own `try/finally + diagnostics`; zero service exemptions/skips.
- Async continuation policy: 78 maintained source files; 1,082 await tokens; 452 `ConfigureAwait(false)`; 576 reviewed renderer-affine `ConfigureAwait(true)`; 49 explicitly configured await-using disposals (26 false / 23 true); 5 configured async streams.
- Application architecture audit passed.
- Panel Studio persistence audit passed.
- Documentation/1-Wire contract audit passed.
- XML documentation coverage passed for 5,654 direct C# declarations across 197 maintained source files.

## Release regression audits

- 2.8.1 adaptive-media/XML-doc audit: 62 checks passed.
- 2.8.2 interaction/converter/rendered-video audit: 167 checks passed.
- 2.8.3/current text-service ownership audit: 48 checks passed.
- 2.8.4/current architecture audit: 53 checks passed.
- 2.8.5/current strict architecture audit: 393 checks passed, including 279 reviewed renderer-affine helper names.
- 2.8.6 compile/text-ownership audit: 54 checks passed.
- Exact explicit `@rendermode` file count remains five reviewed files.
- LocalGPT 1-Wire dependency remains `2.1.1`.

## Not performed

No `dotnet`, MSBuild, NuGet restore, build, publish, or pack command was executed. Runtime/browser behavior and final C# compiler/reference resolution must be confirmed by the consumer's authoritative local build.
