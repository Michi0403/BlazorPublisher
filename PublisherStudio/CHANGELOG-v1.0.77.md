# PublisherStudio v1.0.77 changelog

## C# compilation hotfix

- Fixed `CS0136` in `MediaTimelineEditService.NormalizeTemporalSelection`.
- Renamed the provisional temporal-selection end value from `end` to `candidateEnd` so it no longer conflicts with the final normalized `end` local declared later in the method.
- Preserved the v1.0.76 temporal-selection behavior; this is a naming/scope correction only and does not change range normalization semantics.

## Regression protection

- Extended the C# compilation-safety contract with a targeted guard for the `NormalizeTemporalSelection` local-variable declaration space.
- The guard requires the provisional value to use a distinct name and rejects reintroducing `var end` inside the earlier nested block.
- Kept all existing VideoStudio selection, layer timing, chroma-key, playback, project-import, and structured-export suites enabled.

## Release alignment

- Advanced web application, installer, npm package, lock file, streaming runtime capability, and structured-export manifest versions to `1.0.77`.
- Publication format remains `1.53`; Picture Studio format remains `1.4`.
- Dependency names and versions are unchanged.
