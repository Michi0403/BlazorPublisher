# PublisherStudio 3.6.2

PublisherStudio 3.6.2 makes the expensive DocFX/browser-PDF release stage adaptive to available system memory. The application runtime is unchanged: Publisher logging, overlay installation, Panel Studio behavior, localization, InteractiveServer render affinity, and the 3.6.1 Matrix installer operator controls are preserved.

On an 8–10 GiB machine the documentation pipeline selects 8-page PDF chunks, a 768 MiB Chromium JavaScript heap, a 1024 MiB Node heap, and DocFX max parallelism 1. A 64 GiB-class machine keeps 100-page chunks and the existing high-memory heap budget. Low-memory hosts also serialize PublisherStudio and LocalGPT heavyweight documentation stages.

The print-book builder now avoids duplicate full-page HTML retention, keeps metadata-only front matter, removes temporary chunk HTML immediately, and trims transient managed memory between chunks in low-memory mode.

See `CHANGELOG-v3.6.2-ADAPTIVE-LOW-MEMORY-DOCUMENTATION-BUILD.md` and `VALIDATION-v3.6.2-source.md`.
