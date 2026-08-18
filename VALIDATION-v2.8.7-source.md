# PublisherStudio 2.8.7 source validation

This release was validated statically without invoking the .NET toolchain.

## LocalGPT/network lifecycle validation

- Shipped configuration requires explicit frontend discovery activation and suspends discovery while connected.
- The hosted discovery startup path contains no `UdpClient` construction or UDP bind.
- UDP socket construction/bind exists only inside the bounded active discovery session.
- The old discovery `CreateLinkedTokenSource` / `CancelAfter` / `receiveCancellation` polling path is absent.
- The LocalGPT frontend workflow owns activation request/release state; connection-state changes wake the hosted listener so it can release its socket promptly.
- LAN/RTSP streaming listeners remain media-session-owned; no LAN/RTSP listener is registered as an application-start hosted service.

## XML documentation validation

- XML documentation coverage/quality passed for **5,673 direct C# declarations across 197 maintained source files**: 359 classes, 46 constructors, 9 delegates, 122 enums, 27 events, 514 fields, 70 interfaces, 2,185 methods, 2,250 properties, 81 records, and 10 structs.
- `GenerateDocumentationFile` remains enabled and the XML documentation audit remains a mandatory pre-build gate.
- The PublisherStudio XML documentation parser retains its stricter `.razor.cs` inclusion and orphan-comment validation while using the same audit entry script as LocalGPT.

## Strict architecture validation

- Application architecture audit passed.
- Service resilience audit passed: **1,308 service methods** own `try/catch + diagnostics`; **4 iterator/yield methods** own logged `try/finally` without `catch`; zero exemptions/skips.
- Component resilience audit passed: **2,615 component methods** own method-local diagnostics boundaries; zero legacy exemptions.
- Async continuation audit passed for **78 source files / 1,087 await tokens**: 457 `ConfigureAwait(false)`, 576 reviewed renderer-affine `ConfigureAwait(true)`, 49 explicitly configured async disposals (26 false / 23 true), and 5 configured async streams.
- Panel Studio persistence and documentation/1-Wire contract audits passed.
- Exact explicit `@rendermode` set remains the five reviewed files.
- LocalGPT 1-Wire dependency remains `2.1.1`.

## UI/source regression validation

- Interactive-presentation export uses PublisherStudio-owned native dialog buttons.
- Disabled primary and secondary dialog buttons keep explicit readable text color/text-fill and opacity 1.
- Release regression audits passed: 2.8.1 **62 checks**, 2.8.2 **167**, 2.8.3 **48**, 2.8.4 **53**, 2.8.5 **393**, 2.8.6 **54**, and the new 2.8.7 audit **100 checks**.
- JavaScript syntax and the maintained diagnostics SHA-256 inventory were validated.
- Project XML and JSON configuration files were parsed structurally.

## Not performed

No `dotnet`, MSBuild, NuGet restore, build, publish, or pack command was executed. Runtime/browser behavior and final C# compiler/reference resolution must be confirmed by the consumer's authoritative local build.
