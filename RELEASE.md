# PublisherStudio 3.5.9

PublisherStudio 3.5.9 is a narrow maintenance release over 3.5.8 that fixes documentation contract drift exposed by the real Visual Studio rebuild.

The installer update helper now documents the three parameters it actually accepts, removing the stale parameter tags that produced CS1572/CS1573 warnings. The two shared-file-logging `Dispose` methods now describe queue draining, provider ownership, and single-writer shutdown semantics in enough detail to satisfy the existing XML documentation quality policy without weakening that policy.

No runtime behavior was intentionally changed. The 3.5.8 installer compile repair, maintained logging baseline, single shared writer, renderer-affinity work, Story Editor attachment guard, and LocalGPT-aligned overlay installer are retained.

See `CHANGELOG-v3.5.9-XML-DOCUMENTATION-CONTRACT-REPAIR.md` and `VALIDATION-v3.5.9-source.md`.
