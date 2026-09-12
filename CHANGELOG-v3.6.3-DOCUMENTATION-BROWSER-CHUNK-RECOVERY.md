# PublisherStudio 3.6.3 — Documentation Browser Chunk Recovery

PublisherStudio 3.6.3 applies the same bounded documentation-renderer recovery model as LocalGPT so both release pipelines behave consistently on constrained Macs and other low-memory build hosts. Publisher runtime/editor behavior is unchanged.

## Changes

- Preserves the 3.6.2 adaptive memory profile, including 8-page chunks, 768 MiB Chromium heap, 1 GiB Node heap, and DocFX parallelism 1 on 8–10 GiB hosts.
- Uses a 90-second default browser-render timeout in low-memory mode unless `FUTURE2_DOCUMENTATION_BROWSER_PDF_TIMEOUT` is explicitly provided.
- Caps low-memory post-render stability polling at 15 seconds.
- Keeps browser user data, disk cache, and crash dumps inside a unique temporary profile for every render attempt.
- Removes surviving browser children associated with that unique profile without targeting normal user browser sessions.
- Uses only modern headless mode on constrained hosts; high-memory hosts retain the compatibility headless retry.
- Retries failed cover/index and page chunks once with a fresh isolated profile by default; `FUTURE2_DOCUMENTATION_BROWSER_PDF_CHUNK_RETRIES` supports 0–5 retries.
- Retains durable completed PDF chunks for resume.
- Refuses monolithic DocFX/Playwright fallback for chunked documentation and for low-memory builds, with the explicit emergency override `FUTURE2_DOCUMENTATION_ALLOW_MONOLITHIC_PDF_FALLBACK=1`.
- Records the renderer timeout/retry/fallback policy in `documentation-status.json`.

## Preserved contracts

The 3.6.2 adaptive memory policy, 3.6.1 Matrix installer operator, single-writer logging baseline, overlay/in-place deployment, InteractiveServer renderer affinity, localization, Panel Studio persistence, macOS entitlement handling, and prior PDF/version safeguards remain unchanged.
