#!/usr/bin/env python3
"""Static source audit for PublisherStudio 2.8.5 compile repair and architecture maintenance."""
from pathlib import Path
import re, sys

ROOT = Path(__file__).resolve().parents[1]
checks = 0

def read(rel: str) -> str:
    return (ROOT / rel).read_text(encoding='utf-8-sig', errors='replace')

def require(rel: str, *tokens: str) -> None:
    global checks
    text = read(rel)
    for token in tokens:
        if token not in text:
            raise AssertionError(f"{rel}: missing {token!r}")
        checks += 1

def forbid(rel: str, *tokens: str) -> None:
    global checks
    text = read(rel)
    for token in tokens:
        if token in text:
            raise AssertionError(f"{rel}: forbidden token present: {token!r}")
        checks += 1

try:
    for rel in (
        'src/PublisherStudio.Web/PublisherStudio.Web.csproj',
        'src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'):
        require(rel, '<Version>2.8.5</Version>')

    require('src/PublisherStudio.Web/Components/App.razor',
            'css/site.css?v=20260818-285',
            'videoEffectRuntime.js?v=2.8.5',
            'publisherInterop.js?v=2.8.5')
    for rel in (
        'src/PublisherStudio.Web/Components/Pages/Editor.razor',
        'src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor',
        'src/PublisherStudio.Web/Components/Editor/MediaStudio.razor'):
        require(rel, 'mediaStudioInterop.js?v=2.8.5')

    page = 'src/PublisherStudio.Web/Components/Editor/PageSurface.razor'
    require(page,
            'var selectedElementIds = EditorText.BuildCanvasSelectedElementIds(SelectionVisualsEnabled, State.SelectedElementIds);',
            'var selectionKey = EditorText.BuildCanvasSelectionKey(SelectionVisualsEnabled, InteractionEnabled, selectedElementIds);',
            'selectedElementIds,',
            'catch (Exception exception)',
            'Logger.LogError(exception, "Publisher canvas initialization failed unexpectedly.")')
    if read(page).index('var selectedElementIds =') > read(page).index('selectedElementIds,'):
        raise AssertionError('PageSurface selectedElementIds must be declared before the JS initialization payload.')
    checks += 1

    service = 'src/PublisherStudio.Web/Services/PublicationEditorTextService.cs'
    require(service,
            'public string[] BuildCanvasSelectedElementIds',
            'public string BuildCanvasSelectionKey',
            'try', 'catch (Exception exception)', '_logger.LogError')

    require('src/PublisherStudio.Web/Components/Shared/OperationalErrorBoundary.cs',
            'try', 'catch (Exception boundaryException)', 'Logger.LogCritical', 'Notifications.Error(')

    require('Directory.Build.targets',
            '<ComponentSafetyScript>$(MSBuildThisFileDirectory)build\\Assert-ComponentSafety.ps1</ComponentSafetyScript>',
            '<ServiceArchitectureScript>$(MSBuildThisFileDirectory)build\\Assert-ServiceArchitecture.ps1</ServiceArchitectureScript>',
            '<Target Name="AssertPublisherComponentSafety"',
            '<Target Name="AssertPublisherServiceArchitecture"',
            'AfterTargets="AssertPublisherComponentSafety"',
            'AfterTargets="AssertPublisherServiceArchitecture"')

    require('build/Assert-ComponentSafety.ps1',
            'audit_component_resilience.py',
            '@inject ILoggerFactory OperationalLoggerFactory',
            '@inject IUserNotificationService OperationalNotifications',
            '<OperationalErrorBoundary',
            "Python 3 is required for strict method-granular component resilience")

    require('build/Assert-ServiceArchitecture.ps1',
            'audit_service_resilience.py',
            '--product publisherstudio',
            'Discarded asynchronous work is forbidden',
            'Runtime services/clients/registries/runners must be DI instances')

    require('build/audit_component_resilience.py',
            'no legacy exemption list is permitted',
            '0 legacy exemptions',
            "missing.append('try/catch')",
            "missing.append('structured logging')")
    if (ROOT / 'build/component-method-resilience-baseline.json').exists():
        raise AssertionError('Component resilience baseline must not exist.')
    checks += 1

    # Existing broad all-service enforcement must remain unchanged and active.
    require('build/Assert-MethodDiagnostics.ps1', 'audit_service_resilience.py', '--product publisherstudio')
    require('build/audit_service_resilience.py', 'missing try/catch boundary', 'missing ILogger/Trace diagnostics')

    # Render-mode boundaries are not part of this repair and must remain the reviewed five.
    render_files = []
    for path in (ROOT / 'src/PublisherStudio.Web/Components').rglob('*.razor'):
        text = path.read_text(encoding='utf-8-sig', errors='replace')
        if '@rendermode' in text:
            render_files.append(path.relative_to(ROOT).as_posix())
    expected = sorted([
        'src/PublisherStudio.Web/Components/Pages/Editor.razor',
        'src/PublisherStudio.Web/Components/Pages/Help.razor',
        'src/PublisherStudio.Web/Components/Pages/Localization.razor',
        'src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor',
        'src/PublisherStudio.Web/Components/Layout/JavaScriptDiagnosticsBridge.razor',
    ])
    if sorted(render_files) != expected:
        raise AssertionError(f'render-mode file set changed: {sorted(render_files)!r}')
    checks += len(expected)

    require('Directory.Build.props', '<LocalGptWireProtocolVersion>2.1.1</LocalGptWireProtocolVersion>')

    print(f'PublisherStudio 2.8.5 compile/architecture source audit passed: {checks} checks.')
except Exception as exc:
    print(f'PublisherStudio 2.8.5 source audit failed: {exc}', file=sys.stderr)
    raise SystemExit(1)
