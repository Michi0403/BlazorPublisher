# PublisherStudio 3.6.2 — adaptive low-memory documentation build

PublisherStudio 3.6.2 reduces peak memory pressure in the documentation/PDF stage without changing the authoring application, logging architecture, installer overlay behavior, InteractiveServer render affinity, or the 3.6.1 Matrix installer operator controls.

## Changes

- Documentation build policy now detects physical system memory on macOS, Linux, and Windows, with `GC.GetGCMemoryInfo()` as a fallback.
- Browser PDF chunk size is memory-adaptive. Hosts around 8–10 GiB use 8-page chunks; 64 GiB-class hosts remain at 100 pages per chunk.
- Chromium JavaScript and Node.js heap ceilings scale with system memory instead of always permitting 4096 MiB heaps on small machines.
- DocFX HTML generation uses the supported `--maxParallelism` switch with memory-sensitive limits; low-memory mode uses one worker.
- `FUTURE2_DOCUMENTATION_LOW_MEMORY=1` forces the conservative profile, while explicit chunk/heap/parallelism/memory overrides remain available.
- On low-memory hosts, PublisherStudio and LocalGPT share a heavyweight-documentation lock so their DocFX/Chromium stages cannot accidentally overlap.
- The browser print-book builder no longer retains an unused second full HTML copy per page.
- Front-matter/TOC generation retains metadata only, not every page body.
- Temporary chunk HTML is removed immediately after each durable PDF part and low-memory mode trims managed transient memory between chunks.
- Documentation status records the selected memory policy for diagnostics.

## Compatibility

The PublisherStudio shared single-writer logger, logging baseline, overlay/in-place installer, FFmpeg setup flow, 619 renderer-affine continuations, localization, PDF durability/validation, macOS signing protections, and 3.6.1 installer operator controls remain intact.
