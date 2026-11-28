# PublisherStudio 3.5.2 — release parser and runtime rendezvous repair

## Why this release exists

The first macOS release attempt from 3.5.1 failed while PowerShell parsed `Build-Release.ps1`. A double-quoted published-assembly identity diagnostic used `$mode:`; the literal colon must follow a delimited variable (`${mode}:`) in PowerShell.

The 3.5.1 macOS lifecycle hardening also inherited an endpoint-ownership rule that was too strict for the existing `server.json` rendezvous pattern. Alternate/debug hosting must remain possible without the packaged launcher treating every different executable as stale.

## Changes

- **PowerShell release parser repair**
  - Published-assembly identity diagnostics now use `${mode}:`.
  - The source release audit rejects future unscoped `$name:` interpolation in `Build-Release.ps1`.

- **Runtime rendezvous compatibility**
  - A stale endpoint owned by `/Applications/PublisherStudio.app` is still rejected when its packaged identity is obsolete.
  - A live endpoint owned by another executable is preserved as an alternate host and can be reused when its loopback endpoint responds.
  - Alternate hosts are not killed solely because their version/executable differs from the packaged application.

- **PKG ownership-safe endpoint cleanup**
  - Installer lifecycle scripts remove the runtime endpoint only when it belongs to the installed PublisherStudio app or when its recorded PID is dead.
  - Active alternate/debug endpoint records survive packaged application replacement.
  - Process termination remains restricted to the installed app binary path.

## Preserved behavior

- 3.5.1 release/source identity stamping and macOS updater handoff remain intact.
- 3.5.0 PowerShell 5.1 documentation-cache and Debug-PDF repairs remain intact.
- Durable user-data/log paths, runtime identity logging, InteractiveServer ownership and existing PublisherStudio application behavior are unchanged.

## Validation boundary

This release is source/static validated only. No `dotnet build`, `dotnet publish`, native PKG execution, signing/notarization or GitHub access was performed here.
