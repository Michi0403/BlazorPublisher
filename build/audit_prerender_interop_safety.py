#!/usr/bin/env python3
"""Enforces PublisherStudio prerender-safe JavaScript interop across Razor component lifecycle methods."""
from __future__ import annotations

import argparse
import importlib.util
import re
import sys
from pathlib import Path


def load(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


def line_of(text: str, position: int) -> int:
    return text.count('\n', 0, position) + 1


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument('--root', required=True, type=Path)
    args = ap.parse_args()
    root = args.root.resolve()
    app = root / 'src/PublisherStudio.Web'
    components = app / 'Components'
    resilience = load('publisher_prerender_component_parser', root / 'build/audit_component_resilience.py')
    parser = resilience.load_parser(root / 'build/audit_application_architecture.py')

    failures: list[str] = []
    checked = 0
    dispose_js = 0
    unsafe_lifecycle_names = {'OnInitialized', 'OnInitializedAsync', 'OnParametersSet', 'OnParametersSetAsync'}
    guard_tokens = (
        '_interactiveAttached', '_browserAttached', '_designerAttached', '_active', '_dropBound',
        '_dropSurfaceBound', '_module is not null', '_mediaModule is not null', '_initialized'
    )

    for path in sorted(components.rglob('*.razor')):
        if path.name == '_Imports.razor':
            continue
        text = path.read_text(encoding='utf-8-sig', errors='replace')
        relative = path.relative_to(app).as_posix()
        for match, body, start in resilience.iter_razor_methods(text, parser):
            checked += 1
            name = match.group('name')
            has_js = bool(re.search(r'\b(?:JS|[A-Za-z_]\w*Module|_module|_mediaModule)\s*\.\s*Invoke(?:Async|VoidAsync)\b', body))
            if name in unsafe_lifecycle_names and has_js:
                failures.append(f'{relative}:{line_of(text,start)} {name}: JavaScript interop is forbidden before OnAfterRenderAsync when prerendering is enabled.')
            if name in {'Dispose', 'DisposeAsync'} and has_js:
                dispose_js += 1
                if name == 'Dispose':
                    # Synchronous disposal may never probe IJSRuntime on a prerender-only instance. Editor is allowed
                    # only because its call is scheduled strictly under the interactive-attachment flag.
                    if '_interactiveAttached' not in body:
                        failures.append(f'{relative}:{line_of(text,start)} {name}: synchronous disposal contains JavaScript interop without an interactive-attachment gate.')
                elif not any(token in body for token in guard_tokens):
                    failures.append(f'{relative}:{line_of(text,start)} {name}: asynchronous JavaScript disposal is not gated by state established during interactive attachment.')

    # Component code-behind is subject to the same lifecycle rule.
    for path in sorted(components.rglob('*.razor.cs')):
        text = path.read_text(encoding='utf-8-sig', errors='replace')
        relative = path.relative_to(app).as_posix()
        for method in parser.parse_methods(text):
            checked += 1
            body = text[method.start:method.end]
            has_js = bool(re.search(r'\b(?:JS|[A-Za-z_]\w*Module|_module|_mediaModule)\s*\.\s*Invoke(?:Async|VoidAsync)\b', body))
            if method.name in unsafe_lifecycle_names and has_js:
                failures.append(f'{relative}:{line_of(text,method.start)} {method.name}: JavaScript interop is forbidden before OnAfterRenderAsync when prerendering is enabled.')
            if method.name in {'Dispose', 'DisposeAsync'} and has_js:
                dispose_js += 1
                if method.name == 'Dispose' and '_interactiveAttached' not in body:
                    failures.append(f'{relative}:{line_of(text,method.start)} {method.name}: synchronous disposal contains JavaScript interop without an interactive-attachment gate.')
                elif method.name == 'DisposeAsync' and not any(token in body for token in guard_tokens):
                    failures.append(f'{relative}:{line_of(text,method.start)} {method.name}: asynchronous JavaScript disposal is not gated by interactive attachment/module state.')

    if failures:
        print('Prerender JavaScript interop safety audit failed:')
        for failure in failures:
            print(f'  - {failure}')
        return 1
    print(f'Prerender JavaScript interop safety audit passed: {checked} component method(s) checked; {dispose_js} JavaScript-aware disposal method(s) are attachment-gated; pre-render lifecycle interop is forbidden.')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
