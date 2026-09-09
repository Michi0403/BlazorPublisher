# PublisherStudio 3.5.0

PublisherStudio 3.5.0 repairs the Windows PowerShell 5.1 documentation-cache failure reproduced after a successful DocFX HTML build and accessibility/local-link preflight. It ports the already proven LocalGPT nested `Join-Path` cache fix and adds an early compatibility guard so the same regression is rejected before the long documentation pipeline.

Because this was the same failure sequence previously seen in LocalGPT, 3.5.0 also ports the proven follow-up Debug documentation contract repair: Debug remains HTML/API/XML-only unless a complete PDF was actually generated, while Release continues to require and validate the versioned PDF.

Runtime application behavior, Panel Studio, FFmpeg/media behavior, organic/1-Wire protocols, persistence, deployment layout, and existing InteractiveServer ownership remain unchanged.

See `CHANGELOG-v3.5.0-POWERSHELL51-DOCUMENTATION-CACHE-REPAIR.md` and `VALIDATION-v3.5.0-source.md`.
