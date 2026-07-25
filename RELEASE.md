# PublisherStudio v1.0.75 release

The complete source release is in `PublisherStudio/`.

See `PublisherStudio/CHANGELOG-v1.0.75.md`, `PublisherStudio/SOURCE-CHANGES-v1.0.75.txt`, `PublisherStudio/TEST-RESULTS-v1.0.75.txt`, `PublisherStudio/RELEASE.md`, and `PublisherStudio/VALIDATION.md`.

v1.0.75 fixes Razor compiler error `RZ1010` in the Inspector streaming-effects editor by removing an invalid nested `@{ ... }` block from an existing `@if` control-flow body. A regression contract now protects the corrected structure.

Application and installer version is `1.0.75`. Publication format remains `1.52`; Picture Studio format remains `1.4`; dependency versions and sets are unchanged.
