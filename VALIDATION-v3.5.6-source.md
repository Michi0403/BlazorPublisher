# PublisherStudio 3.5.6 source validation

This is a source/static validation. No `dotnet build`, `dotnet test`, `dotnet publish`, native Windows installer execution, or native macOS release build was run in this environment.

Validated source contracts include: version 3.5.6 on maintained version surfaces; canonical `%LOCALAPPDATA%\PublisherStudio` ownership; staged application/setup identity matching; transactional wrapper replacement with rollback; full setup exception logging; durable application/bootstrap logging; Windows preferred-port probe with OS-assigned fallback; frontend race guards; invariant website-export range binding; multi-picture Gallery insertion; server endpoint Version/ExecutablePath identity; macOS bundle/runtime identity and PKG readback guards; PowerShell ambiguous-variable parser guard; render-mode ownership; architecture/cross-platform boundaries; async continuation policy; component/prerender/service resilience; iterator policy; and XML/Razor documentation coverage.

The supplied Windows failure evidence showed `SocketException (10013)` while Kestrel attempted to bind the configured loopback endpoint. 3.5.6 therefore retains 58071 as preferred but does not make startup depend on that exact port being bindable.

The first field test should verify both `%LOCALAPPDATA%\PublisherStudio\PublisherStudio.Setup.log` and `%LOCALAPPDATA%\PublisherStudio\PublisherStudio.log`, then run install/update/start on Windows and package/update/start on macOS.
