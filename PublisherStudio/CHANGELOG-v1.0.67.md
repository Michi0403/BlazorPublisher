# PublisherStudio 1.0.67

## Compiler correction

- Fixed the remaining `CS0103` in `RtspLanServer`. `global::System.Text.Encoding` was placed directly inside an interpolated-string hole, where C# interprets the colon as the interpolation format separator.
- Added the explicit file-level alias `TextEncoding = global::System.Text.Encoding` to RTSP and Platform Chat.
- Calculated the SDP byte length before constructing the `Content-Length` header, keeping the formatted string simple and deterministic.

## Prevention

- Updated `AGENTS.md`, architecture documentation, ADR-008 and validation guidance with the interpolation grammar rule.
- Extended `csharpCompilationSafety.test.mjs` to reject unparenthesized `{global::...}` interpolation holes and require the deliberate Encoding alias at the known collision sites.

Application and installer version: `1.0.67`. Publication format remains `1.48`; Picture Studio format remains `1.3`. No dependency changed.
