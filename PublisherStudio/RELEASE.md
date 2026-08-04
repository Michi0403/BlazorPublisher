# PublisherStudio 2.0.4 service-owned compiler repair

See `CHANGELOG-v2.0.4.md` and `docs/architecture/task-ledger.md`.

PublisherStudio 2.0.4 fixes the remaining compiler and composition errors reported by the maintainer after 2.0.3. The implementation follows the LocalGPT-led architecture: data contracts are owned by `PublisherStudio.BusinessObjects`, reusable behavior is owned by injected services and factories, startup passes an explicit bootstrap logger, and no new application statics were introduced. The publish-mapping guard now validates the existing release map semantically instead of misreading PowerShell array output.
