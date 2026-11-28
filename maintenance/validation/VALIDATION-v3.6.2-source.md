# PublisherStudio 3.6.2 source validation

This is a source-only validation record. No `dotnet build`, Visual Studio build, PowerShell execution, Apple signing/notarization, or GitHub action is claimed from this environment.

Validated source contracts include:

- active release identity is 3.6.2 and preserves the one-digit minor/patch rule;
- 8–10 GiB documentation hosts select an 8-page browser-PDF chunk, 768 MiB browser JS heap, 1024 MiB Node heap, and DocFX max parallelism 1;
- 64 GiB-class hosts select 100-page chunks;
- explicit low-memory and resource-specific environment overrides remain available;
- DocFX build is invoked with `--maxParallelism`;
- front-matter generation does not retain page bodies and normal chunks do not retain duplicate source HTML;
- low-memory mode removes temporary chunk HTML promptly, trims managed transient memory, and serializes heavy documentation work across PublisherStudio and LocalGPT;
- existing logging, overlay installer, 3.6.1 Matrix operator/cancellation, 3.6.0 signing/PDF latency, async-affinity, component/prerender resilience, localization, and Panel Studio guards remain enabled.

The user’s Visual Studio/macOS build remains the authoritative compiler/runtime validation.
