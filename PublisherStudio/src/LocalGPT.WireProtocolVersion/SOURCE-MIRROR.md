# LocalGPT Wire Protocol source-build mirror

This project is synchronized from the authoritative `LocalGPT.WireProtocolVersion` project in LocalGPT. It is included in the PublisherStudio source package so restore, debug, and RID-specific publish do not depend on a previously downloaded or source-only NuGet package.

Release builds explicitly pack this project into a normal DLL-backed NuGet package. Protocol changes should still be made in LocalGPT first and then synchronized here.
