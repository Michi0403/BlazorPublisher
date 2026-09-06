# PublisherStudio 3.3.2 source validation

Static validation covers version consistency, self-contained Full-only Unix/macOS packaging, SHA-bound persisted Apple submission resume state, standards-correct notarization resume by polling `notarytool info`, same-version release reuse, HTML-only runtime documentation, configurable documentation/release roots, browser-PDF timeout/fallback behavior, Ghostscript compression with a 256 MiB default ceiling (including cached PDFs), Homebrew RPM provisioning, cleanup of generated build state, architecture/signing markers, and the four reviewed InteractiveServer boundaries.

The prior draft used a nonexistent `notarytool wait` subcommand for resuming an already-uploaded submission. 3.3.2 corrects that before release: new uploads persist the submission ID and the build polls Apple's supported `notarytool info` command until Accepted, failure, or the local timeout. A later run resumes the exact SHA-256-bound submission without re-uploading unchanged bytes.

Runtime .NET builds, PowerShell execution on macOS, Homebrew installs, native package production, Developer ID signing, and Apple notarization are intentionally not claimed in this source-only environment.
