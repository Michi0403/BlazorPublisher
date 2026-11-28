# PublisherStudio 2.2.7 — documentation viewer viewport polish

## Documentation viewer

The focus-managed in-app HTML/API/PDF documentation viewer now occupies almost the full desktop viewport while remaining a modal rather than becoming a separate full-screen page. The native dialog is pinned to a 2vw / 2dvh inset so the embedded three-rail DocFX site has enough horizontal room for navigation, article content, and the `In this article` rail.

## Compiler fix preserved

`PublisherDocumentationViewerService.NormalizeUrl` keeps the verified single-character overload for the leading slash check:

```csharp
normalized.StartsWith('/')
```

The `//` same-origin rejection remains an ordinal string comparison, and backslashes/control characters remain rejected.

## Compatibility

No API-reference generation, service resilience, logging, publishing, installer, or LocalGPT-compatible documentation behavior from 2.2.6 is removed.
