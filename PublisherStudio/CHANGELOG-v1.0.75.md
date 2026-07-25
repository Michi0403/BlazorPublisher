# PublisherStudio v1.0.75 changelog

## Razor compilation hotfix

- Fixed `RZ1010` in `Components/Editor/InspectorPanel.razor`.
- The authored streaming-layer locals are now declared directly at the beginning of the existing `@if (liveSource.IsVisual) { ... }` body.
- Removed the invalid nested `@{ ... }` transition. Razor control-flow bodies are already C# code blocks, so a second explicit code block at that location is illegal.
- The layered streaming-effects editor, filter selection, chroma controls, and live-source behavior are otherwise unchanged.

## Regression protection

- Extended the C# compilation-safety suite with a Razor contract that requires the three live-layer locals inside the existing visual-source control-flow body.
- The same contract rejects reintroducing an explicit nested `@{ ... }` block around those declarations.

## Release alignment

- Advanced the web application, installer, npm package, lock file, streaming runtime capability, and structured-export manifest version to `1.0.75`.
- Corrected the outer release descriptor so it points to the current release metadata.
- Publication format remains `1.52`; Picture Studio format remains `1.4`.
- NuGet and npm dependency versions and sets are unchanged.
