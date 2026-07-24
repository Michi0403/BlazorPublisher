# PublisherStudio v1.0.67 release

See `CHANGELOG-v1.0.67.md`, `AGENTS.md`, ADR-008, `docs/ARCHITECTURE.md`, `docs/architecture/system-overview.md`, and `VALIDATION.md`.

This release corrects the final compiler issue introduced by the v1.0.66 namespace-collision repair. `global::System.Text.Encoding` must not begin an interpolated-string expression because the interpolation grammar treats the colon as a format separator. RTSP and Platform Chat now use a deliberate `TextEncoding` alias, and the RTSP SDP byte length is computed before the header is formatted.

The compilation-safety contract now rejects the exact `{global::...}` pattern and verifies the aliases. This supplements a real .NET build; it does not replace one.

Application and installer version is `1.0.67`. Publication format remains `1.48`, Picture Studio format remains `1.3`, and no NuGet/npm/native dependency changed.
