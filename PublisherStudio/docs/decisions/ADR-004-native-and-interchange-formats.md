# ADR-004: Native and interchange formats remain separate

**Status:** Accepted

PublisherStudio native documents remain authoritative. Common/open formats are implemented as import/export adapters with validation and a visible loss report. An adapter may flatten unsupported features, but it must not silently redefine the native model around another application's file structure.
