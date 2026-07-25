# PublisherStudio v1.0.76 release

See `CHANGELOG-v1.0.76.md`, `SOURCE-CHANGES-v1.0.76.txt`, `TEST-RESULTS-v1.0.76.txt`, and `VALIDATION.md`.

v1.0.76 repairs the remaining VideoStudio interaction boundary. Temporal selections survive pointer/touch release and late media metadata, can be applied directly to the selected effect layer, and remain distinct from saved cut sections. Chroma and other layer-filter changes refresh the live renderer. Browser play/pause cancellation is handled inside JavaScript, so the expected interrupted-`play()` promise no longer escapes into Blazor Server as a JSInterop or RemoteRenderer exception.

Application and installer version is `1.0.76`. Publication format is `1.53`; Picture Studio format remains `1.4`. Dependency names and versions are unchanged.
