# PublisherStudio 2.8.9 source validation

This is a source-only validation record. No `dotnet`, MSBuild, NuGet restore, build, publish, or pack command was executed while preparing this archive.

Validated statically:

- project versions are 2.8.9;
- DevExpress remains 25.2.9 from the user's upgraded source;
- dotnet-ef and installer logging remain 10.0.11;
- the component-diagnostics gate recognizes exactly the documentation-only empty Razor partial shape and does not classify operational code as documentation-only;
- all 46 documentation companions from the reported build failure match that strict shape, while `PictureEditor.razor.cs` does not;
- retained release, architecture, component, service, async-continuation, prerender, XML-documentation, Panel Studio, and 1-Wire source audits were re-run where supported without invoking .NET;
- the authored `docs/` DocFX/Kawaii source is present even though the supplied upgrade archive omitted it; generated help output remains non-source;
- generated IDE/build/cache directories are excluded from the returned source archive;
- the tracked GitHub Pages snapshot was not regenerated manually; the normal documentation build can refresh it from the restored authored source and compiler XML.
