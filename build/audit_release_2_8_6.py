#!/usr/bin/env python3
"""Static source audit for PublisherStudio 2.8.8 compile/text-ownership repair."""
from __future__ import annotations
from pathlib import Path
import argparse, importlib.util, json, re, sys

ap = argparse.ArgumentParser()
ap.add_argument('--root', required=True, type=Path)
args = ap.parse_args()
root = args.root.resolve()
app = root / 'src/PublisherStudio.Web'
checks = 0

def require(rel: str, *needles: str) -> str:
    global checks
    text = (root / rel).read_text(encoding='utf-8-sig', errors='replace')
    for needle in needles:
        if needle not in text:
            raise AssertionError(f'{rel}: missing {needle!r}')
        checks += 1
    return text

def forbid(rel: str, *needles: str) -> str:
    global checks
    text = (root / rel).read_text(encoding='utf-8-sig', errors='replace')
    for needle in needles:
        if needle in text:
            raise AssertionError(f'{rel}: forbidden {needle!r}')
        checks += 1
    return text

try:
    for rel in (
        'src/PublisherStudio.Web/PublisherStudio.Web.csproj',
        'src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'):
        require(rel, '<Version>2.8.8</Version>')

    require('src/PublisherStudio.Web/Components/App.razor',
            'css/site.css?v=20260818-288',
            'videoEffectRuntime.js?v=2.8.8',
            'publisherInterop.js?v=2.8.8')
    for rel in (
        'src/PublisherStudio.Web/Components/Pages/Editor.razor',
        'src/PublisherStudio.Web/Components/Editor/MediaStudio.razor',
        'src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor'):
        require(rel, 'mediaStudioInterop.js?v=2.8.8')

    service = require('src/PublisherStudio.Web/Services/PublicationEditorTextService.cs',
        'HumanizeIdentifier', 'ParseWebHeaders', 'FormatWebHeaders',
        'EscapeEmbeddedScriptClosingTag', 'BuildMediaStudioFramePolygonCss',
        'BuildPublicationFramePolygonCss')
    require('src/PublisherStudio.Web/Components/Editor/AnimationPanel.razor',
            '@inject PublicationEditorTextService EditorText', 'EditorText.HumanizeIdentifier(value)')
    require('src/PublisherStudio.Web/Components/Editor/PublicationTimeline.razor',
            '@inject PublicationEditorTextService EditorText', 'EditorText.HumanizeIdentifier(value)')
    require('src/PublisherStudio.Web/Components/Editor/DevExtremeComponentEditor.razor',
            '@inject PublicationEditorTextService EditorText', 'EditorText.ParseWebHeaders(value)', 'EditorText.FormatWebHeaders(headers)')
    require('src/PublisherStudio.Web/Components/Editor/HtmlEmbedView.razor',
            '@inject PublicationEditorTextService EditorText', 'EditorText.EscapeEmbeddedScriptClosingTag(value)')
    require('src/PublisherStudio.Web/Components/Editor/MediaStudio.razor',
            '@inject PublicationEditorTextService EditorText', 'EditorText.BuildMediaStudioFramePolygonCss(points)')
    require('src/PublisherStudio.Web/Components/Editor/PageSurface.razor',
            '@inject PublicationEditorTextService EditorText', 'EditorText.BuildPublicationFramePolygonCss(points)', 'public async Task FitPageAsync()')
    require('src/PublisherStudio.Web/Components/Editor/PrintPublication.razor',
            '@inject PublicationEditorTextService EditorText', 'EditorText.BuildPublicationFramePolygonCss(points)')

    # Exact copy of the build guard's direct text-operation ownership rule.
    baseline = set(json.loads((root / 'build/text-service-ownership-baseline.json').read_text(encoding='utf-8-sig')))
    pattern = re.compile(r'(?m)^(?P<line>.*(?:\bRegex\s*\.|\bnew\s+Regex\s*\(|\.Replace\s*\(|\.Split\s*\(|\bstring\.Join\s*\(|\bWebUtility\.HtmlDecode\s*\().*)$')
    new_violations: list[str] = []
    for folder in ('Components', 'Controllers', 'Controller'):
        base = app / folder
        if not base.exists():
            continue
        for path in base.rglob('*'):
            if path.suffix not in ('.cs', '.razor'):
                continue
            rel = path.relative_to(root).as_posix()
            text = path.read_text(encoding='utf-8-sig', errors='replace')
            for match in pattern.finditer(text):
                line = ' '.join(match.group('line').strip().split())
                if re.search(r'(?:CouncilText|PanelText|TextService|RegexService|StringService)\.', line):
                    continue
                identity = f'{rel}|{line}'
                if identity not in baseline:
                    new_violations.append(identity)
    checks += 1
    if new_violations:
        raise AssertionError('new text-service ownership violations:\n' + '\n'.join(sorted(set(new_violations))))

    # The 2.8.5 architecture conversion accidentally generated `return await` in
    # non-generic async Task methods. Scan Razor and component code-behind using the
    # maintained syntax masker and reject that compile-invalid shape everywhere.
    comp_spec = importlib.util.spec_from_file_location('ps_component_resilience', root / 'build/audit_component_resilience.py')
    comp = importlib.util.module_from_spec(comp_spec)
    assert comp_spec and comp_spec.loader
    sys.modules[comp_spec.name] = comp
    comp_spec.loader.exec_module(comp)
    parser = comp.load_parser(root / 'build/audit_application_architecture.py')
    invalid_async_returns: list[str] = []
    for path in sorted((app / 'Components').rglob('*.razor')):
        text = path.read_text(encoding='utf-8-sig', errors='replace')
        for match, body, start in comp.iter_razor_methods(text, parser):
            return_type = ' '.join(match.group('return').split())
            modifiers = match.group('mods').split()
            if 'async' in modifiers and return_type == 'Task' and re.search(r'\breturn\s+await\b', parser.mask_csharp(body)):
                invalid_async_returns.append(f'{path.relative_to(app).as_posix()}:{comp.line_of(text,start)} {match.group("name")}')
    for path in sorted((app / 'Components').rglob('*.razor.cs')):
        text = path.read_text(encoding='utf-8-sig', errors='replace')
        for method in parser.parse_methods(text):
            opening = text.find('{', method.start, method.end)
            signature = text[method.start:opening if opening >= 0 else method.end]
            if not re.search(r'\basync\s+Task\s+' + re.escape(method.name) + r'\s*\(', signature):
                continue
            body = text[method.start:method.end]
            if re.search(r'\breturn\s+await\b', parser.mask_csharp(body)):
                invalid_async_returns.append(f'{path.relative_to(app).as_posix()}:{text.count(chr(10),0,method.start)+1} {method.name}')
    checks += 1
    if invalid_async_returns:
        raise AssertionError('compile-invalid return-await in async Task methods:\n' + '\n'.join(invalid_async_returns))

    editor = require('src/PublisherStudio.Web/Components/Pages/Editor.razor',
        'await ExportRasterPages("png").ConfigureAwait(true);',
        'await ExportRasterPages("jpeg").ConfigureAwait(true);',
        'await ExportPage("svg").ConfigureAwait(true);',
        'await ExportSelectedObject("png").ConfigureAwait(true);',
        'await ExportSelectedObject("svg").ConfigureAwait(true);',
        'await JS.InvokeVoidAsync("publisherStudio.printPublication").ConfigureAwait(true);',
        'await InsertMediaFile(args, PublicationElementKind.Video).ConfigureAwait(true);',
        'await InsertMediaFile(args, PublicationElementKind.Audio).ConfigureAwait(true);')
    forbid('src/PublisherStudio.Web/Components/Pages/Editor.razor',
        'return await ExportRasterPages("png")', 'return await ExportRasterPages("jpeg")',
        'return await ExportPage("svg")', 'return await ExportSelectedObject("png")',
        'return await ExportSelectedObject("svg")', 'return await JS.InvokeVoidAsync("publisherStudio.printPublication")',
        'return await InsertMediaFile(args, PublicationElementKind.Video)',
        'return await InsertMediaFile(args, PublicationElementKind.Audio)')
    forbid('src/PublisherStudio.Web/Components/OrganicPlugins/OrganicSecurityPanel.razor', 'return await RefreshAsync()')
    forbid('src/PublisherStudio.Web/Components/Editor/PictureEditor.razor.cs',
           'return await Download("image/png"', 'return await Download("image/jpeg"')
    forbid('src/PublisherStudio.Web/Components/Editor/PublicationTimeline.razor', 'return await SeekFromPointer(args)')

    # Preserve reviewed architecture boundaries and protocol contract.
    render_paths=[]
    for path in (app / 'Components').rglob('*.razor'):
        if '@rendermode' in path.read_text(encoding='utf-8-sig', errors='replace'):
            render_paths.append(path.relative_to(app).as_posix())
    checks += 1
    if len(render_paths) != 5:
        raise AssertionError(f'expected 5 explicit InteractiveServer render-mode files, found {len(render_paths)}: {render_paths}')
    require('Directory.Build.props', '<LocalGptWireProtocolVersion>2.1.1</LocalGptWireProtocolVersion>')

    print(f'PublisherStudio 2.8.8 compile/text-ownership source audit passed: {checks} checks.')
except Exception as exc:
    print(f'PublisherStudio 2.8.8 source audit failed: {exc}', file=sys.stderr)
    raise SystemExit(1)
