# PublisherStudio 2.8.7 source changelog

## Scope

PublisherStudio 2.8.7 removes unsolicited LocalGPT discovery socket activity, removes the recurring cancellation-exception polling pattern that exposed `OperationCanceledException` in the debugger, keeps XML documentation a build-blocking architecture requirement, and repairs PublisherStudio export-dialog button contrast/style. The strict 2.8.5/2.8.6 service/component resilience and ConfigureAwait architecture remains in force.

## LocalGPT discovery is frontend-on-demand

- PublisherStudio no longer binds UDP discovery port 51141 merely because the application has started.
- `LocalGptDiscoveryHostedService` remains application-hosted so its lifetime is supervised, but its startup state is network-idle. A UDP socket is created only while `ILocalGptDiscoveryActivationService` reports explicit frontend demand.
- Opening the LocalGPT connection workflow from the main PublisherStudio ribbon requests discovery. Leaving the workflow releases that request.
- When `SuspendDiscoveryWhileConnected` is enabled (the shipped default), successful connection state releases the discovery socket because an already connected LocalGPT session does not need continued discovery traffic.
- The behavior is configurable with `RequireFrontendDiscoveryActivation` and `SuspendDiscoveryWhileConnected`; both ship as `true`. `AutoConnectDiscoveredPeer` remains `false` by default.
- LAN/RTSP streaming listeners remain session-owned and are started only when a media session explicitly enables LAN output; this release does not turn those listeners into background hosted services.

## Cancellation-noise repair

The previous discovery listener used a linked `CancellationTokenSource` plus `CancelAfter` as its receive-poll timer. That design intentionally threw `OperationCanceledException` every poll interval even though the exception was caught. Debuggers configured to break on thrown cancellation exceptions therefore surfaced repeated exceptions during completely normal idle discovery.

2.8.7 removes that polling mechanism. Discovery now uses an event signal plus timeout waits and checks `UdpClient.Available` before receiving. Normal polling no longer relies on cancellation. Cancellation remains meaningful only for application shutdown or an explicitly canceled connection operation.

## XML documentation architecture

- `GenerateDocumentationFile` remains enabled.
- `AssertPublisherXmlDocumentationCoverage` remains a mandatory `BeforeBuild` gate.
- PublisherStudio uses the same `Assert-XmlDocumentationCoverage.py` entry script as LocalGPT and the PublisherStudio parser additionally includes maintained `.razor.cs` files and rejects orphan XML-doc blocks.
- The rule checks direct maintained C# declarations for XML documentation and quality, including types/classes/records/interfaces, constructors, methods, properties, events, fields, delegates and enums; method parameters/returns/type parameters and property `<value>` tags are validated where applicable.
- No missing declaration was added to an exemption list.

Raw `.razor` `@code` methods continue to be governed by the separate zero-exemption component resilience/diagnostics architecture, matching LocalGPT's separation between XML API documentation and Razor component implementation methods.

## Export dialog UI

- Interactive-presentation and structured-website export footers now use PublisherStudio-owned native dialog buttons rather than the DevExpress disabled-button styling that produced nearly invisible captions.
- Disabled buttons explicitly retain readable text and `-webkit-text-fill-color` with opacity 1.
- The export dialog header now uses the same PublisherStudio blue title-bar gradient and readable light subtitle treatment as the main application chrome.
- Primary export actions use the PublisherStudio blue chrome; secondary/cancel actions use the existing light PublisherStudio surface treatment.
- Selected-picture export choices use the same dialog button vocabulary for visual consistency.

## Preserved architecture and features

- Strict component method-local `try/catch + structured logging` remains enforced with zero legacy exemptions.
- Strict service method-local resilience remains enforced; iterator/yield methods continue to use logged `try/finally` without `catch`.
- The LocalGPT-compatible ConfigureAwait policy remains unchanged: explicit continuation configuration everywhere, `false` by default, reviewed renderer-affine `true` only inside Components.
- Five reviewed `InteractiveServer` boundaries remain unchanged.
- LocalGPT 1-Wire remains `2.1.1`.
- Video Studio effects/layers/rendered-video export, adaptive media, Converter Studio guidance, slider coalescing, modal canvas suspension, Story Editor recovery, and Panel Studio remain intact.
- No EF migration or storage-schema change was introduced.

## Build scope

No `dotnet`, MSBuild, NuGet restore, build, publish, or pack command was run while preparing this source release. The consumer's local .NET build remains authoritative for compiler/reference validation.
