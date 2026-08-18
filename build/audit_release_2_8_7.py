#!/usr/bin/env python3
"""Static source audit for PublisherStudio 2.9.0 on-demand networking, XML docs, and export UI."""
from pathlib import Path
import argparse, json, re, sys

ap = argparse.ArgumentParser()
ap.add_argument('--root', required=True, type=Path)
args = ap.parse_args()
root = args.root.resolve()
app = root / 'src/PublisherStudio.Web'
checks = 0

def text(rel: str) -> str:
    return (root / rel).read_text(encoding='utf-8-sig', errors='replace')

def require(rel: str, *tokens: str) -> str:
    global checks
    value = text(rel)
    for token in tokens:
        if token not in value:
            raise AssertionError(f'{rel}: missing {token!r}')
        checks += 1
    return value

def forbid(rel: str, *tokens: str) -> str:
    global checks
    value = text(rel)
    for token in tokens:
        if token in value:
            raise AssertionError(f'{rel}: forbidden {token!r}')
        checks += 1
    return value

try:
    for rel in (
        'src/PublisherStudio.Web/PublisherStudio.Web.csproj',
        'src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'):
        require(rel, '<Version>2.9.0</Version>')

    require('src/PublisherStudio.Web/Components/App.razor',
            'css/site.css?v=20260818-290',
            'videoEffectRuntime.js?v=2.9.0',
            'publisherInterop.js?v=2.9.0')
    for rel in (
        'src/PublisherStudio.Web/Components/Pages/Editor.razor',
        'src/PublisherStudio.Web/Components/Editor/MediaStudio.razor',
        'src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor'):
        require(rel, 'mediaStudioInterop.js?v=2.9.0')

    # Discovery is application-hosted but network-idle until an explicit frontend workflow requests it.
    require('src/PublisherStudio.Web/Services/OrganicPlugins/IOrganicPluginServices.cs',
            'public interface ILocalGptDiscoveryActivationService',
            'const string FrontendConnectionWorkflowOwner = "PublisherStudio.OrganicPlugins.Frontend";',
            'bool IsRequested { get; }',
            'event Action? Changed;',
            'void Request(string owner);',
            'void Release(string owner);')
    require('src/PublisherStudio.Web/Services/OrganicPlugins/OrganicPluginStateServices.cs',
            'public sealed class LocalGptDiscoveryActivationService',
            'HashSet<string> owners',
            'public bool IsRequested',
            'public void Request(string owner)',
            'public void Release(string owner)',
            'Changed?.Invoke();')
    require('src/PublisherStudio.Web/PublisherStudioServiceCollectionExtensions.cs',
            'AddSingleton<ILocalGptDiscoveryActivationService, LocalGptDiscoveryActivationService>(services);',
            'services.AddHostedService<LocalGptDiscoveryHostedService>();')
    require('src/PublisherStudio.Web/BusinessObjects/OrganicPluginModels.cs',
            'public bool RequireFrontendDiscoveryActivation { get; set; } = true;',
            'public bool SuspendDiscoveryWhileConnected { get; set; } = true;')
    settings = json.loads(text('src/PublisherStudio.Web/appsettings.json'))
    organic = settings['OrganicPlugins']
    if organic.get('RequireFrontendDiscoveryActivation') is not True:
        raise AssertionError('shipped LocalGPT discovery must require frontend activation')
    checks += 1
    if organic.get('SuspendDiscoveryWhileConnected') is not True:
        raise AssertionError('shipped LocalGPT discovery must suspend while connected')
    checks += 1
    if organic.get('AutoConnectDiscoveredPeer') is not False:
        raise AssertionError('shipped LocalGPT discovery must not auto-connect without user configuration')
    checks += 1

    discovery = require('src/PublisherStudio.Web/HostedServices/OrganicPlugins/LocalGptDiscoveryHostedService.cs',
        'if (!ShouldListen())',
        'await discoveryStateSignal.WaitAsync(stoppingToken).ConfigureAwait(false);',
        'if (options.Value.RequireFrontendDiscoveryActivation && !activation.IsRequested)',
        'if (options.Value.SuspendDiscoveryWhileConnected && connection.State.IsConnected)',
        'await RunDiscoverySessionAsync(stoppingToken).ConfigureAwait(false);',
        'private async Task RunDiscoverySessionAsync(CancellationToken stoppingToken)',
        'using var udp = new UdpClient(AddressFamily.InterNetwork);',
        'udp.Client.Bind(new IPEndPoint(IPAddress.Any, options.Value.DiscoveryPort));',
        'await discoveryStateSignal.WaitAsync(receivePoll, stoppingToken).ConfigureAwait(false);',
        'received = await udp.ReceiveAsync(stoppingToken).ConfigureAwait(false);',
        'activation.Changed += SignalDiscoveryStateChanged;',
        'connection.Changed += SignalDiscoveryStateChanged;',
        'activation.Changed -= SignalDiscoveryStateChanged;',
        'connection.Changed -= SignalDiscoveryStateChanged;')
    forbid('src/PublisherStudio.Web/HostedServices/OrganicPlugins/LocalGptDiscoveryHostedService.cs',
           'CreateLinkedTokenSource', 'CancelAfter(', 'receiveCancellation')
    # Guard against regressing the socket back into ExecuteAsync/startup ownership.
    execute_slice = discovery[discovery.index('protected override async Task ExecuteAsync'):discovery.index('private bool ShouldListen')]
    checks += 1
    if 'new UdpClient' in execute_slice or '.Bind(' in execute_slice:
        raise AssertionError('LocalGPT discovery startup path owns a UDP socket before frontend activation')

    require('src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor',
            '@inject ILocalGptDiscoveryActivationService DiscoveryActivation',
            'private const string DiscoveryOwner = ILocalGptDiscoveryActivationService.FrontendConnectionWorkflowOwner;',
            'SynchronizeDiscoveryActivation();',
            'DiscoveryActivation.Request(DiscoveryOwner);',
            'DiscoveryActivation.Release(DiscoveryOwner);',
            'LocalGPT discovery is idle until the frontend connection workflow requests it')
    require('src/PublisherStudio.Web/Components/Editor/PublicationRibbon.razor',
            '@inject ILocalGptDiscoveryActivationService LocalGptDiscoveryActivation',
            'LocalGptDiscoveryActivation.Request(ILocalGptDiscoveryActivationService.FrontendConnectionWorkflowOwner);',
            'Navigation.NavigateTo("/organic-plugins");')

    # LAN/RTSP listeners remain session-owned rather than application-start hosted listeners.
    require('src/PublisherStudio.Web/Services/Streaming/Sessions/MediaSessionRegistry.cs',
            'if (session.LanEnabled)',
            'session.LanServer = lanServerFactory.Create(session);',
            'session.LanServer.Start();')
    require('src/PublisherStudio.Web/Services/Streaming/Lan/LanStreamingServer.cs',
            'builder.WebHost.ConfigureKestrel(options => options.Listen(address, _session.LanDefinition.Port));')
    registrations = text('src/PublisherStudio.Web/StreamingServiceCollectionExtensions.cs') + text('src/PublisherStudio.Web/PublisherStudioServiceCollectionExtensions.cs')
    checks += 1
    if 'AddHostedService<LanStreamingServer' in registrations or 'AddHostedService<RtspLanServer' in registrations:
        raise AssertionError('LAN/RTSP streaming listener regressed to application-start hosted lifetime')

    # Export dialogs use PublisherStudio-owned native button styling with explicit disabled contrast.
    require('src/PublisherStudio.Web/Components/Pages/Editor.razor',
            'class="publisher-dialog-button" @onclick="CloseSingleWebsiteExport"',
            'class="publisher-dialog-button primary" @onclick="ExportSingleWebsite"',
            'class="publisher-dialog-button" @onclick="CloseStructuredWebsiteExport"',
            'class="publisher-dialog-button primary" @onclick="ExportStructuredWebsite"',
            'class="publisher-dialog-button primary" @onclick="ExportSelectedMergedChoice"')
    forbid('src/PublisherStudio.Web/Components/Pages/Editor.razor',
           '<DxButton Text="Cancel" Enabled="@(!_singleWebsiteExporting)"')
    require('src/PublisherStudio.Web/wwwroot/css/site.css',
            '.publisher-dialog > header {',
            'background: linear-gradient(90deg,#0f4b78,#1768a0);',
            '.publisher-dialog > header p {',
            '.publisher-dialog-button {',
            '.publisher-dialog-button.primary {',
            '.publisher-dialog-button:disabled {',
            'color: #657386;',
            '-webkit-text-fill-color: #657386;',
            'opacity: 1;',
            '.publisher-dialog-button.primary:disabled {',
            '.publisher-dialog footer .publisher-dialog-button:disabled{opacity:1!important}')

    # XML documentation remains a build-blocking, LocalGPT-compatible source architecture rule.
    require('src/PublisherStudio.Web/PublisherStudio.Web.csproj',
            '<GenerateDocumentationFile>true</GenerateDocumentationFile>')
    require('Directory.Build.targets',
            '<XmlDocumentationCoverageScript>$(MSBuildThisFileDirectory)build\\Assert-XmlDocumentationCoverage.ps1</XmlDocumentationCoverageScript>',
            '<Target Name="AssertPublisherXmlDocumentationCoverage"',
            'BeforeTargets="BeforeBuild" AfterTargets="AssertPublisherInstallerWorkflow"')
    require('build/Assert-XmlDocumentationCoverage.ps1',
            'Assert-XmlDocumentationCoverage.py',
            'throw "XML documentation coverage validation failed with exit code $exitCode."')
    require('build/Assert-XmlDocumentationCoverage.py', 'from xml_documentation import run as run_csharp', 'from razor_xml_documentation import run as run_razor', "raise SystemExit(run_razor(repository_root, 'validate'))")
    require('build/xml_documentation.py',
            "for p in sorted(root.rglob('*.cs')):",
            'GENERIC_SUMMARIES=',
            'missing XML documentation',
            'missing value tag for property')

    require('docs/articles/localgpt-and-onewire.md',
            'frontend connection workflow',
            'does not bind the LocalGPT discovery UDP socket')
    require('build/Assert-OneWireArchitecture.ps1',
            'ILocalGptDiscoveryActivationService',
            'RequireFrontendDiscoveryActivation',
            'receiveCancellation\\.CancelAfter',
            "Shipped LocalGPT discovery policy is not frontend-on-demand.")

    expected_render_files = {
        'src/PublisherStudio.Web/Components/Layout/JavaScriptDiagnosticsBridge.razor',
        'src/PublisherStudio.Web/Components/Pages/Editor.razor',
        'src/PublisherStudio.Web/Components/Pages/Help.razor',
        'src/PublisherStudio.Web/Components/Pages/Localization.razor',
        'src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor',
    }
    actual_render_files = {
        p.relative_to(root).as_posix()
        for p in app.rglob('*.razor')
        if '@rendermode' in p.read_text(encoding='utf-8-sig', errors='replace')
    }
    if actual_render_files != expected_render_files:
        raise AssertionError(f'render-mode boundary set changed: {sorted(actual_render_files)}')
    checks += len(expected_render_files)
    require('Directory.Build.props', '<LocalGptWireProtocolVersion>2.1.1</LocalGptWireProtocolVersion>')

    print(f'PublisherStudio 2.9.0 on-demand discovery/XML-doc/export-UI source audit passed: {checks} checks.')
except Exception as exc:
    print(f'PublisherStudio 2.9.0 source audit failed: {exc}', file=sys.stderr)
    raise SystemExit(1)
