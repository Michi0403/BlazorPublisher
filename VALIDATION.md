# PublisherStudio 3.7.3 source validation

Source-only validation; no .NET command was run. The 3.7.3 check is deliberately narrow: documentation pointer decorations are viewport-contained, scroll-neutral, synchronized across authored/embedded/Pages assets, and protected by the new build guard. Chromium geometry regression checks confirmed that adding many paw-trail elements no longer changes document scroll dimensions. See `VALIDATION-v3.7.3-source.md`.
