# PublisherStudio 2.8.6 source changelog

## Scope

PublisherStudio 2.8.6 is a focused compile and text-ownership repair over 2.8.5. It keeps the strict LocalGPT-style component/service resilience and ConfigureAwait architecture intact while repairing compiler-invalid method rewrites and moving the newly surfaced component text transformations behind the existing editor text service.

## Compile repairs

- Repaired eight compiler-reported `CS1997` methods in `Components/Pages/Editor.razor`. Non-generic `async Task` methods now `await` their work and complete normally instead of using `return await ...`.
- Static source review found and repaired five additional instances of the same architecture-conversion defect before handoff:
  - `OrganicSecurityPanel.OnInitializedAsync`
  - `PictureEditor.DownloadPng`
  - `PictureEditor.DownloadJpeg`
  - `PublicationTimeline.SeekFromRuler`
  - `PublicationTimeline.SeekFromTrack`
- `PageSurface.FitPageAsync()` remains present and public; the two `Editor.razor` call sites retained by 2.8.5 still resolve to it by source inspection.
- Added a 2.8.6 release audit that rejects `return await` inside non-generic component `async Task` methods so this exact conversion defect is caught before a future handoff.

## Text-service ownership

The build reported eight direct component string/regex operations after the strict 2.8.5 component conversion changed their source identities. They are no longer added to a baseline and the guard was not weakened.

`PublicationEditorTextService` now owns the corresponding transformations:

- camel/Pascal identifier humanization used by Animation Panel and Publication Timeline;
- DevExtreme web-header parsing and formatting;
- HTML embed closing-script escaping;
- Media Studio frame polygon CSS formatting;
- publication/print frame polygon CSS formatting.

The component/controller text-ownership rule therefore has zero new violations against the maintained baseline.

## Preserved architecture

- Every discovered PublisherStudio component method remains method-locally guarded with structured logging; zero legacy component exemptions.
- Every discovered PublisherStudio service method remains guarded; iterator/yield methods continue to use logged `try/finally` without `catch`.
- The LocalGPT-compatible ConfigureAwait policy is unchanged: explicit continuation configuration everywhere, `false` by default, and reviewed renderer-affine `true` only in Components.
- `ISupervisedTaskRunner` ownership rules remain unchanged.
- The five reviewed `InteractiveServer` render boundaries are unchanged.
- LocalGPT 1-Wire remains `2.1.1`.
- No editor/layer/media feature redesign was made. Video Studio effects/layers/rendered export, Converter Studio guidance, slider coalescing, modal canvas interaction suspension, adaptive media, Story Editor recovery, and Panel Studio remain present.
- No EF migration or storage-schema change was introduced.

## Build scope

No `dotnet`, MSBuild, NuGet restore, build, publish, or pack command was run while preparing this source release. The consumer's .NET build remains authoritative for compiler/reference validation.
