# PublisherStudio 1.0.54

## Resilient installer and FFmpeg provisioning

- WinGet FFmpeg installation is pinned to the community `winget` source so an unrelated Microsoft Store source timeout cannot block the package lookup.
- FFmpeg package-manager processes now have visible 30-second heartbeat messages, bounded execution times, process-tree termination on timeout, and a 15-minute total provisioning budget.
- WinGet receives one automatic retry so its existing package/download cache can recover from a transient GitHub or CDN failure.
- FFmpeg remains optional: a failed or timed-out provision no longer invalidates the PublisherStudio installation.
- FFmpeg discovery now also checks the WinGet package directory when the WinGet link or the current process PATH has not refreshed yet.
- FFmpeg executables are verified with `ffmpeg -version` before provisioning is reported as successful.

## Resilient PublisherStudio release downloads

- GitHub release metadata lookup now retries transient failures.
- Release asset downloads retain `.part` files, resume with HTTP range requests, report transfer rate, retry up to five times, and abort/retry a connection that produces no data for two minutes.
- A complete cached release archive is reused after ZIP validation instead of being downloaded again on a second installer run.
- Both application and setup archives are downloaded and validated before an existing installation is deleted or modified.
- Permanent download or archive failures now return a non-zero installer result instead of continuing with a missing or partial payload.

Application and installer version: `1.0.54`. Publication format remains `1.45`; picture format remains `1.2`.
