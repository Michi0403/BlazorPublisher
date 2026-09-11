# PublisherStudio 3.6.1 - Matrix installer operator control

## Added

- Added a Matrix-style operator input layer to the existing setup console without opening Terminal, PowerShell, cmd, or another external terminal window.
- Added `:operator on|off`, `:shells`, `:shell <auto|zsh|bash|sh|pwsh|cmd>`, `:jobs`, `:cancel`, `:signal`, `:clear`, and `:help` controls.
- Ordinary human-entered lines run through a selected redirected shell backend with `CreateNoWindow=true`; shell command text is not written to setup diagnostics.
- Auto shell selection follows the current host and installed tools, including zsh/bash/sh/pwsh on Unix/macOS and pwsh/cmd on Windows.
- Added setup-child process tracking so the operator can inspect PIDs and send INT/TERM/KILL/HUP where supported. Explicit `pid:<n>` remains a deliberate operator escape hatch; bare numeric PIDs must already be tracked.

## Cancellation and installer ownership

- Ctrl+C now routes into the same setup-wide cooperative cancellation token as `:cancel`.
- Application/setup downloads and retry delays observe operator cancellation rather than continuing after a visible cancel request.
- Cancellation now bypasses legacy generic error/retry catches in release lookup, install/update, FFmpeg, file-move, and child-process paths so the dedicated setup-cancel exit remains authoritative.
- Spawned setup processes are registered with the operator layer and killed as a process tree on cancellation when necessary.
- FFmpeg provisioning participates in the same setup-process registry and cancellation token.
- Human-started operator shell jobs are tracked separately from setup-owned children and are not automatically killed by setup cancellation.

## Preserved

- Normal install/update remains overlay/in-place under the canonical product root; this change does not introduce whole-root replacement or transactional deployment behavior.
- Existing launcher/shortcut behavior, FFmpeg workflow, localization, application runtime behavior and deployment dialect are unchanged.
- PublisherStudio's shared bounded file logger remains one provider-owned sink with one writer thread; the logging baseline is not weakened.
- InteractiveServer continuation affinity remains unchanged.
- The 3.5.9 XML-documentation contract repair and 3.6.0 macOS entitlement/PDF-render protections remain intact.
