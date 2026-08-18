# PublisherStudio 2.8.8

Source-only InteractiveServer prerender, prerender-safe JavaScript interop, and comprehensive Razor XML-documentation architecture release.

The routed PublisherStudio pages use `@rendermode InteractiveServer`, whose default is prerender enabled. Nested editor components inherit the owning page circuit rather than declaring competing render modes. Browser-only diagnostics remains an intentional non-prerendered island.

No .NET build/publish/pack was run while preparing this source archive.
