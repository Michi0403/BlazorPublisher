# PublisherStudio 3.5.1 — macOS updater handoff and runtime identity

## Why this release exists

The companion LocalGPT macOS test demonstrated a general native-package lifecycle hazard that also existed in PublisherStudio: an installer can replace an `.app` bundle while the old managed process remains alive, and the launcher can reconnect to that old process through a still-healthy `server.json` endpoint. PublisherStudio used the same permissive endpoint-first launcher shape, so the fix is applied here before field testing exposes the same failure.

## Changes

- **Version/source identity before packaging**
  - PublisherStudio Web and InstallerConsole move from `3.5.0` to `3.5.1`.
  - Release builds compute a source fingerprint and clear same-version artifacts when the fingerprint changes.
  - Unix publish verifies `PublisherStudio.Web.dll` semantic version before native packaging.
  - Unix payloads carry `RELEASE-VERSION.txt` and `SOURCE-SHA256.txt`.

- **Runtime endpoint identity**
  - `server.json` now includes `Version` and `ExecutablePath` with the existing PID/base URL/port fields.
  - The macOS launcher embeds the expected version and validates the packaged release/source stamps.
  - A responding endpoint is reused only when its PID is alive, its version matches the installed launcher and its executable path matches the packaged apphost.
  - Older/mismatched endpoint files are deleted and their owner process is asked to stop before a replacement runtime starts.

- **macOS PKG update handoff**
  - Native PKG generation now includes `preinstall` and `postinstall` lifecycle scripts.
  - `preinstall` stops a running `/Applications/PublisherStudio.app` process before bundle replacement and removes the logged-in user's stale runtime endpoint.
  - `postinstall` clears the endpoint again and reopens the newly installed application in the logged-in user's launch context when PublisherStudio had been running before the update.

- **Stable writable runtime context**
  - Packaged PublisherStudio starts from `~/Library/Application Support/PublisherStudio/runtime` instead of inside the application bundle.
  - `PWD` is exported to that durable directory.
  - `LoggingCore__FileCore__FilePath` is routed to `~/Library/Application Support/PublisherStudio/logs/PublisherStudio.log`.
  - Application startup logs assembly version, executable path, base directory and working directory.
  - PublisherStudio also repairs an invalid inherited current directory to its per-user runtime directory before normal startup composition.

## Preserved behavior

- The 3.5.0 PowerShell 5.1 nested `Join-Path` documentation-cache repair remains intact.
- Debug documentation remains HTML/API/XML-only unless a PDF was actually generated; Release still requires the complete versioned PDF.
- Existing PublisherStudio interactive render-mode ownership, component/service diagnostics, publishing workflows and Windows installer behavior are unchanged.

## Validation boundary

This handoff is source/static validation only. No `dotnet build`, `dotnet publish`, native macOS packaging, signing/notarization, PKG execution or GitHub access was performed here.
