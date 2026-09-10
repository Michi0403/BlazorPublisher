# PublisherStudio 3.5.6 - installer workflow guard repair

- Repairs the maintained Windows installer workflow validation so it validates the transactional staged install introduced in 3.5.5 instead of requiring the superseded direct overlay extraction calls.
- Keeps `%LOCALAPPDATA%\\PublisherStudio` as the canonical Windows product root.
- Requires application/setup staging, release identity matching, assembly-version validation, packaged-runtime ownership shutdown, rollback-capable wrapper replacement, and preservation of user data beside the runtime/setup wrappers.
- Preserves durable PublisherStudio and setup logging, dynamic loopback-port recovery, macOS package validation/notarization, cross-host `server.json` rendezvous behavior, and the 3.5.3 frontend race/gallery fixes.
