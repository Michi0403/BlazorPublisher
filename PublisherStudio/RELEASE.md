# PublisherStudio v1.0.75 release

See `CHANGELOG-v1.0.75.md`, `SOURCE-CHANGES-v1.0.75.txt`, `TEST-RESULTS-v1.0.75.txt`, `VALIDATION.md`, and the v1.0.74 structured-export documentation.

v1.0.75 is a focused Razor compilation hotfix for the layered live-video effects editor. `InspectorPanel.razor` no longer opens an explicit `@{ ... }` block inside the existing `@if (liveSource.IsVisual) { ... }` body, eliminating compiler error `RZ1010`. The three streaming-layer locals are declared at the start of the existing control-flow block, before its markup.

The C# compilation-safety suite now guards that exact Razor ownership rule. Structured website export, VideoStudio layers, chroma key, open video-project import, and all other v1.0.74 behavior remain unchanged.

Application and installer version is `1.0.75`. Publication format remains `1.52`; Picture Studio format remains `1.4`; dependency versions and sets are unchanged.
