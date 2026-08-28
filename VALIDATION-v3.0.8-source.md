# PublisherStudio 3.0.8 source validation

This handoff was validated without invoking `dotnet` and without GitHub/network source access.

Scope is limited to the supplied macOS documentation/PDF behavior:

- documentation generation and the complete PDF remain mandatory in the release pipeline;
- the existing Windows browser probes remain unchanged;
- standard `/Applications` and per-user `~/Applications` Chrome, Edge, and Chromium bundles are now considered on Darwin hosts;
- the DocFX fallback streams output instead of buffering the entire process until exit;
- redirected carriage-return `Removed`/`Copied` transfer-counter redraws are omitted from display while raw output remains available to diagnostics;
- the strict generated-HTML accessibility/link preflight remains unchanged;
- version identity remains within the single-digit minor/patch policy.

Repository source audits and ZIP integrity checks were run locally. A real .NET/PowerShell release build still has to be run on the target build machine.
