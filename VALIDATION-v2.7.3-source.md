# PublisherStudio 2.7.3 source validation

Source-only validation is performed without `dotnet`, MSBuild, Visual Studio or GitHub access.

Checks include JavaScript syntax, strict async-continuation policy, service-resilience policy (normal methods use `try/catch + diagnostics`; iterator/yield methods use `try/finally + diagnostics`), existing Media/Picture/AI-preview/application-architecture regressions including Razor static-declaration enforcement, the 2.7.3 logging/recording recovery audit, XML project parsing, render-mode comparison and ZIP integrity.

The Windows .NET build remains authoritative for compilation, browser capture behavior and runtime logging.
