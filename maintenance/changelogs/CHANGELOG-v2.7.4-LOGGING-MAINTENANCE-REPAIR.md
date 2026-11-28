# PublisherStudio 2.7.4 — Logging maintenance repair

- Restored the file logger's runtime-directory fallback to the same behavior used by LocalGPT: when `LoggingCore:FileCore:FilePath` is blank, `PublisherStudio.log` is written beside the running PublisherStudio application (for example the installed `winx64` runtime folder).
- Re-aligned file logging infrastructure with the LocalGPT layout: logging provider implementations live in the dedicated `Logging` infrastructure boundary while options/state remain under `BusinessObjects` and startup configuration remains DI-owned by `LoggingConfigurationService`.
- Removed the 2.7.3-only LocalApplicationData path resolver and bounded-provider additions that diverged from LocalGPT.
- Completed XML documentation for the restored logging business objects and infrastructure so the maintained documentation coverage gate passes.
- Preserved the 2.7.3 screen-recording stop/finalization and reconnect recovery behavior without additional media changes.
- LocalGPT and the LocalGPT 1-Wire protocol are unchanged by this PublisherStudio-only release.
