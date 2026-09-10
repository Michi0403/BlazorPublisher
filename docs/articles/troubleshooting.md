# Troubleshooting

## The source builds but the browser UI is incomplete

Run `Prepare-DevExpressAssets.cmd`. A clean source package intentionally omits generated licensed DevExpress runtime assets.

## The application reports a port override warning

PublisherStudio prefers `127.0.0.1:58071`, but packaged startup preflights the requested loopback port and falls back to an OS-assigned port when Windows rejects it (for example because the port belongs to an excluded/reserved range). The actual endpoint written to `runtime/server.json` is authoritative. Remove conflicting `ASPNETCORE_URLS` or launch-profile addresses when diagnosing custom-host startup.

## LocalGPT is not discovered

Confirm that both applications are running, UDP port `51141` is available, and the firewall allows local discovery. PublisherStudio remains usable while discovery is unavailable.

## A browser circuit disconnects

`OperationCanceledException` and `JSDisconnectedException` can occur while a tab reloads or closes. They should be logged as expected disconnect diagnostics, not presented as data-loss errors. Reopen the page and check the application log for a preceding operational failure.

## Recording preview becomes black

Check the saved recording first. If the file is complete, reopen or reselect the preview source. The preview-rebind watchdog is designed to repair a replaced video element without changing the saved recording pipeline.

## Documentation is missing

A normal release build generates `wwwroot/help-docs`. The in-app Documentation page shows HTML, PDF, and XML-comment availability. Developer builds can diagnose the documentation lane separately with the build properties described in [Documentation system](documentation-system.md).
