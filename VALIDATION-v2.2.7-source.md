# PublisherStudio 2.2.7 source validation

This source package is based on the working 2.2.6 service-resilience and DocFX repair baseline.

Additional 2.2.7 changes:

- Preserves the compiler-confirmed `NormalizeUrl` fix: the single leading slash check uses `StartsWith('/')`; the double-slash check remains ordinal.
- Expands the shared PublisherStudio documentation viewer to use the viewport directly (`inset: 2dvh 2vw`) instead of relying on a dialog width that browsers/global CSS could constrain.
- Keeps the viewer modal/focus-managed and retains the browser-tab escape action.
- Mobile layout still uses the full viewport.
- No DocFX/API-reference content or LocalGPT-shaped documentation shell behavior is removed.
- No generated binary output is included in the source package.

The checked-in generated documentation snapshot is intentionally not relabeled; an owner-side .NET/DocFX build regenerates the versioned documentation.
