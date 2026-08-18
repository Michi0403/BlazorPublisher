#!/usr/bin/env python3
"""Static source audit for PublisherStudio 2.8.8 InteractiveServer prerender and Razor XML architecture."""
from __future__ import annotations
from pathlib import Path
import re, sys

root=Path(__file__).resolve().parents[1]
checks=0

def text(rel:str)->str:
    p=root/rel
    if not p.is_file(): raise AssertionError(f'missing {rel}')
    return p.read_text(encoding='utf-8-sig',errors='replace')

def require(rel:str,*tokens:str):
    global checks
    data=text(rel)
    for token in tokens:
        checks+=1
        if token not in data: raise AssertionError(f'{rel} missing {token!r}')

def forbid(rel:str,*tokens:str):
    global checks
    data=text(rel)
    for token in tokens:
        checks+=1
        if token in data: raise AssertionError(f'{rel} unexpectedly contains {token!r}')

try:
    for rel in ['src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj']:
        require(rel,'<Version>2.8.8</Version>')
    require('src/PublisherStudio.Web/Components/App.razor','css/site.css?v=20260818-288','videoEffectRuntime.js?v=2.8.8','publisherInterop.js?v=2.8.8','<Routes />')
    for rel in ['src/PublisherStudio.Web/Components/Pages/Editor.razor','src/PublisherStudio.Web/Components/Editor/MediaStudio.razor','src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor']:
        require(rel,'mediaStudioInterop.js?v=2.8.8') if 'App.razor' not in rel else None

    expected={
        'src/PublisherStudio.Web/Components/Pages/Editor.razor':'@rendermode InteractiveServer',
        'src/PublisherStudio.Web/Components/Pages/Help.razor':'@rendermode InteractiveServer',
        'src/PublisherStudio.Web/Components/Pages/Localization.razor':'@rendermode InteractiveServer',
        'src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor':'@rendermode InteractiveServer',
        'src/PublisherStudio.Web/Components/Layout/JavaScriptDiagnosticsBridge.razor':'@rendermode @(new InteractiveServerRenderMode(prerender: false))',
    }
    actual=[]
    components=root/'src/PublisherStudio.Web/Components'
    for path in components.rglob('*.razor'):
        data=path.read_text(encoding='utf-8-sig',errors='replace')
        if '@rendermode' in data:
            actual.append(path.relative_to(root).as_posix())
    checks+=1
    if set(actual)!=set(expected): raise AssertionError(f'unexpected render-mode set: {sorted(actual)}')
    for rel,directive in expected.items():
        first=next(line.strip() for line in text(rel).splitlines() if line.strip())
        checks+=1
        if first!=directive: raise AssertionError(f'{rel} first directive is {first!r}, expected {directive!r}')
    forbid('src/PublisherStudio.Web/Components/Pages/Error.razor','@rendermode')
    require('src/PublisherStudio.Web/Program.cs','AddInteractiveServerComponents()','AddInteractiveServerRenderMode()')
    require('src/PublisherStudio.Web/Components/_Imports.razor','@using static Microsoft.AspNetCore.Components.Web.RenderMode')

    editor='src/PublisherStudio.Web/Components/Pages/Editor.razor'
    require(editor,
        'private bool _interactiveAttached;',
        '_interactiveAttached = true;',
        'if (_interactiveAttached)',
        'Editor disposal skipped browser hotkey cleanup because this component instance never attached to an interactive browser circuit',
        'if (_interactiveAttached)\n                            await JS.InvokeVoidAsync("publisherStudio.setDocumentDirty"')
    # Static prerender disposal must not schedule JS unconditionally.
    data=text(editor)
    dispose=data[data.index('public void Dispose()'):]
    checks+=1
    if dispose.index('if (_interactiveAttached)') > dispose.index('JS.InvokeVoidAsync("publisherStreaming.unbindHotkeys")'):
        raise AssertionError('Editor hotkey disposal is not attachment-gated')

    guarded={
        'src/PublisherStudio.Web/Components/Editor/DataVisualClientHost.razor':'_browserAttached',
        'src/PublisherStudio.Web/Components/Editor/DevExtremeComponentView.razor':'_browserAttached',
        'src/PublisherStudio.Web/Components/Editor/DevExtremeComponentEditor.razor':'_designerAttached',
        'src/PublisherStudio.Web/Components/Editor/LiveSourceView.razor':'_active',
        'src/PublisherStudio.Web/Components/Editor/MediaConverterStudio.razor':'_dropBound',
        'src/PublisherStudio.Web/Components/Editor/PanelStudio.razor':'_dropSurfaceBound',
        'src/PublisherStudio.Web/Components/Editor/VideoMediaView.razor':'_browserAttached',
    }
    for rel,token in guarded.items():
        require(rel,token,'DisposeAsync','JS.Invoke')

    require('build/audit_prerender_interop_safety.py','OnInitializedAsync','DisposeAsync','interactive-attachment','prerender')
    require('build/Assert-ComponentSafety.ps1','audit_prerender_interop_safety.py','Prerender JavaScript interop safety audit failed.')
    require('build/razor_xml_documentation.py','Razor XML documentation','ensure_component_codebehind','process_razor')
    require('build/Assert-XmlDocumentationCoverage.py','run_razor','run_csharp')
    require('build/Add-XmlDocumentation.py','run_razor','run_csharp')

    razor_files=[p for p in components.rglob('*.razor') if p.name!='_Imports.razor']
    checks+=1
    if len(razor_files)!=47: raise AssertionError(f'expected 47 Razor components, found {len(razor_files)}')
    for path in razor_files:
        cb=path.with_suffix(path.suffix+'.cs')
        checks+=1
        if not cb.is_file(): raise AssertionError(f'missing Razor class documentation shell: {cb.relative_to(root)}')
        cbtext=cb.read_text(encoding='utf-8-sig',errors='replace')
        checks+=1
        if '/// <summary>' not in cbtext or f'public partial class {path.stem}' not in cbtext:
            raise AssertionError(f'Razor class XML documentation missing in {cb.relative_to(root)}')

    # Existing 1-Wire compatibility remains unchanged.
    require('Directory.Build.props','<LocalGptWireProtocolVersion>2.1.1</LocalGptWireProtocolVersion>')

    print(f'PublisherStudio 2.8.8 InteractiveServer prerender/Razor XML source audit passed: {checks} checks.')
except Exception as exc:
    print(f'PublisherStudio 2.8.8 source audit failed: {exc}',file=sys.stderr)
    raise SystemExit(1)
