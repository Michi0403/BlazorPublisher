# PublisherStudio 4.4.7 source validation

- Confirmed from supplied macOS log: 753 source pages, 8 per batch, 94 completed batches, final one-page slice; PowerShell StrictMode `.Count` failure.
- Reviewed original 4.4.6 final-batch assignment and LocalGPT 6.2.8 safe array construction.
- Source contract test added for 1/8-page scenarios; PowerShell runtime not present here, so not executed.
- Seven Node browser suites passed; existing build and architecture guard files unchanged.
- Full .NET/pwsh build and a complete PDF render **not performed**. The macOS 95th-chunk recovery requires the user's release build to verify end-to-end.

Browser retries do not skip invalid chunks, omit pages, fall back to an unsafe monolithic render on low-memory hosts, or delete verified earlier parts. Existing metadata/API completeness checks remain authoritative.
