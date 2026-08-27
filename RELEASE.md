# PublisherStudio 3.0.5

PublisherStudio 3.0.5 is the **Native Discovery Resolution Hardening** release.

It keeps the 3.0.4 compiler repairs and hardens them so the native discovery code no longer relies on a namespace import for `PublisherRuntimePattern`. The two runtime-pattern references are fully qualified, and the Unix file-mode code now rejects Windows before entering the Unix-only API call, giving the platform analyzer an explicit supported-path boundary.

This handoff is source-only. No .NET build and no GitHub/network source access were used while preparing it. See `CHANGELOG-v3.0.5-NATIVE-DISCOVERY-RESOLUTION-HARDENING.md` and `VALIDATION-v3.0.5-source.md`.
