#!/usr/bin/env python3
"""Enriches and validates XML documentation for explicit PublisherStudio Razor component members."""
from __future__ import annotations

import argparse
import importlib.util
import re
import sys
import tempfile
from pathlib import Path
from collections import Counter


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


def component_namespace(path: Path, components_root: Path, text: str) -> str:
    match = re.search(r'(?m)^\s*@namespace\s+([^\s]+)\s*$', text)
    if match:
        return match.group(1)
    rel = path.relative_to(components_root)
    parts = list(rel.parent.parts)
    return 'PublisherStudio.Components' + ('.' + '.'.join(parts) if parts else '')


def class_summary(name: str, xml) -> str:
    words = xml.words(name)
    return f'Represents the {words} Razor component and owns its rendered UI state, event handlers, and interactive lifecycle.'


def ensure_component_codebehind(path: Path, components_root: Path, xml, mode: str) -> list[str]:
    if path.name == '_Imports.razor':
        return []
    text = path.read_text(encoding='utf-8-sig', errors='replace')
    name = path.stem
    namespace = component_namespace(path, components_root, text)
    codebehind = path.with_suffix(path.suffix + '.cs')
    if not codebehind.exists():
        if mode == 'enhance':
            summary = class_summary(name, xml)
            codebehind.write_text(
                f'namespace {namespace};\n\n'
                '/// <summary>\n'
                f'/// {summary}\n'
                '/// </summary>\n'
                f'public partial class {name}\n'
                '{\n'
                '}\n',
                encoding='utf-8')
            return []
        return [f'{path}: missing component code-behind XML documentation shell {codebehind.name}']
    # The direct C# XML validator owns the quality/content of the partial class and any code-behind members.
    codebehind_text = codebehind.read_text(encoding='utf-8-sig', errors='replace')
    if not re.search(rf'(?m)^\s*public\s+partial\s+class\s+{re.escape(name)}\b', codebehind_text):
        return [f'{codebehind}: missing public partial class {name} required for component XML documentation']
    return []


def synthetic_source(namespace: str, name: str, block: str) -> tuple[str, int]:
    # The synthetic class itself has a fixed documentation block so validation findings correspond only to
    # explicit members authored in the Razor @code/@functions block.
    prefix = (
        f'namespace {namespace};\n'
        '/// <summary>\n'
        f'/// Documents the explicit members authored by the {name} Razor component.\n'
        '/// </summary>\n'
        f'public partial class {name}\n'
        '{\n'
    )
    return prefix + block + '\n}\n', prefix.count('\n')


def extract_class_body(source: str, name: str, parser) -> str:
    match = re.search(rf'(?m)^\s*public\s+partial\s+class\s+{re.escape(name)}\b', source)
    if not match:
        raise RuntimeError(f'Could not find synthetic class {name}.')
    opening = source.find('{', match.end())
    if opening < 0:
        raise RuntimeError(f'Could not find synthetic class opening brace for {name}.')
    tail = source[opening:]
    masked = parser.mask_csharp(tail)
    closing = parser.match_brace(masked, 0)
    if closing <= 0:
        raise RuntimeError(f'Could not find synthetic class closing brace for {name}.')
    return tail[1:closing]


def map_failure(failure: str, temp_path: Path, razor_path: Path, block_line: int, prefix_lines: int) -> str:
    # xml_documentation reports <path>:<line>: message. Convert the synthetic line to the Razor file line.
    pattern = re.compile(rf'^{re.escape(str(temp_path))}:(\d+):(.*)$')
    match = pattern.match(failure)
    if not match:
        return failure.replace(str(temp_path), str(razor_path))
    synthetic_line = int(match.group(1))
    razor_line = max(1, block_line + synthetic_line - prefix_lines - 1)
    return f'{razor_path}:{razor_line}:{match.group(2)}'


def process_razor(path: Path, components_root: Path, xml, component_parser, csharp_parser, mode: str) -> tuple[int, int, list[str], Counter]:
    text = path.read_text(encoding='utf-8-sig', errors='replace')
    namespace = component_namespace(path, components_root, text)
    name = path.stem
    blocks = list(component_parser.iter_code_blocks(text, csharp_parser))
    failures: list[str] = []
    counts = Counter()
    changed = 0
    declaration_count = 0
    replacements: list[tuple[int, int, str]] = []

    for block_start, block, _ in blocks:
        source, prefix_lines = synthetic_source(namespace, name, block)
        block_line = text.count('\n', 0, block_start) + 1
        with tempfile.TemporaryDirectory(prefix='publisherstudio-razor-xml-') as td:
            temp_path = Path(td) / f'{name}.cs'
            temp_path.write_text(source, encoding='utf-8')
            _, declarations = xml.scan_file(temp_path)
            explicit_declarations = [d for d in declarations if not (d.kind == 'class' and d.name == name and d.containing_type is None)]
            declaration_count += len(explicit_declarations)
            for declaration in explicit_declarations:
                counts[declaration.kind] += 1
            if mode == 'enhance':
                adds, rewrites, _ = xml.process_file(temp_path)
                changed += adds + rewrites
                enriched = temp_path.read_text(encoding='utf-8')
                replacements.append((block_start, block_start + len(block), extract_class_body(enriched, name, csharp_parser)))
            else:
                file_failures, _ = xml.validate_file(temp_path)
                for failure in file_failures:
                    # Ignore the synthetic wrapper class if a parser version still reports it.
                    if f'missing XML documentation for class {name}' in failure:
                        continue
                    failures.append(map_failure(failure, temp_path, path, block_line, prefix_lines))

    if mode == 'enhance' and replacements:
        for start, end, replacement in reversed(replacements):
            text = text[:start] + replacement + text[end:]
        path.write_text(text, encoding='utf-8')
    return declaration_count, changed, failures, counts


def run(root: Path, mode: str) -> int:
    repository_root = root.resolve()
    components_root = repository_root / 'src/PublisherStudio.Web/Components'
    xml = load_module('publisherstudio_xml_documentation', repository_root / 'build/xml_documentation.py')
    component_parser = load_module('publisherstudio_component_resilience', repository_root / 'build/audit_component_resilience.py')
    csharp_parser = component_parser.load_parser(repository_root / 'build/audit_application_architecture.py')

    failures: list[str] = []
    total_declarations = 0
    total_changed = 0
    counts = Counter()
    files = 0
    class_shells = 0

    for path in sorted(components_root.rglob('*.razor')):
        if path.name == '_Imports.razor':
            continue
        files += 1
        before = path.with_suffix(path.suffix + '.cs').exists()
        failures.extend(ensure_component_codebehind(path, components_root, xml, mode))
        if mode == 'enhance' and not before and path.with_suffix(path.suffix + '.cs').exists():
            class_shells += 1
        declarations, changed, razor_failures, file_counts = process_razor(
            path, components_root, xml, component_parser, csharp_parser, mode)
        total_declarations += declarations
        total_changed += changed
        failures.extend(razor_failures)
        counts.update(file_counts)

    if failures:
        print(f'Razor XML documentation validation failed with {len(failures)} finding(s):')
        for failure in failures:
            print(f'  - {failure}')
        return 1

    details = ', '.join(f'{key}={value}' for key, value in sorted(counts.items()))
    if mode == 'enhance':
        print(f'Razor XML documentation enrichment completed for {total_declarations} explicit member declaration(s) across {files} component(s); changed {total_changed} documentation block(s) and created {class_shells} component class documentation shell(s).')
    else:
        print(f'Razor XML documentation coverage and quality passed for {total_declarations} explicit member declaration(s) across {files} component(s), with one documented partial component class per Razor file: {details}.')
    return 0


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', required=True, type=Path)
    parser.add_argument('--mode', choices=('enhance', 'validate'), required=True)
    args = parser.parse_args()
    return run(args.root, args.mode)


if __name__ == '__main__':
    raise SystemExit(main())
