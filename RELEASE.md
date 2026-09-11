# PublisherStudio 3.6.1

PublisherStudio 3.6.1 adds a dependency-light Matrix-style operator control plane to the setup console so long install/update operations remain controllable without opening a separate OS terminal window.

The setup ASCII wall now accepts `:operator on|off`, `:shells`, `:shell`, `:jobs`, `:cancel`, `:signal`, `:clear`, and ordinary shell command lines through redirected host shells. Auto shell resolution is host-aware and can use zsh, bash, sh, pwsh/PowerShell, or cmd when available. Meta controls remain reachable even when ordinary shell forwarding is switched off.

Ctrl+C and `:cancel` request a shared setup cancellation token. Download streams, retry delays, process waits, FFmpeg provisioning and main install/update stages observe that token. Setup-owned child processes are registered for inspection/signaling and best-effort process-tree cleanup; human-started shell repair jobs remain separately classified so setup cancellation does not silently terminate them.

The existing overlay/in-place installer contract, logging single-writer design, InteractiveServer renderer-affinity, 3.5.9 XML-documentation repair, and 3.6.0 macOS signing/PDF-render protections remain intact.

See `CHANGELOG-v3.6.1-MATRIX-INSTALLER-OPERATOR-CONTROL.md` and `VALIDATION-v3.6.1-source.md`.
