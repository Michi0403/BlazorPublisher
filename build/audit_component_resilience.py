#!/usr/bin/env python3
from __future__ import annotations
from pathlib import Path
import argparse, importlib.util, json, re, sys

METHOD_RE = re.compile(
    r'(?m)^[ \t]*(?P<access>public|private|protected|internal)\s+'
    r'(?P<mods>(?:(?:async|override|virtual|sealed|new|partial|unsafe)\s+)*)'
    r'(?P<return>[A-Za-z_][\w<>,\.\[\]\? \t]*)\s+'
    r'(?P<name>[A-Za-z_]\w*)\s*\((?P<args>[^;{}]*)\)\s*'
    r'(?P<body>\{|=>)')


def load_parser(path: Path):
    spec = importlib.util.spec_from_file_location('publisher_component_resilience_parser', path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def normalize_signature(match: re.Match[str]) -> str:
    args = re.sub(r'\s+', ' ', match.group('args')).strip()
    return_type = re.sub(r'\s+', ' ', match.group('return')).strip()
    mods = re.sub(r'\s+', ' ', match.group('mods')).strip()
    prefix = f"{match.group('access')} {mods} {return_type}" if mods else f"{match.group('access')} {return_type}"
    return f"{prefix} {match.group('name')}({args})"


def brace_depth(masked: str, position: int) -> int:
    depth = 0
    for char in masked[:position]:
        if char == '{': depth += 1
        elif char == '}': depth = max(0, depth - 1)
    return depth


def iter_code_blocks(text: str, parser):
    masked = parser.mask_csharp(text)
    for marker in re.finditer(r'@(code|functions)\s*\{', masked):
        open_brace = masked.find('{', marker.start())
        close_brace = parser.match_brace(masked, open_brace)
        if open_brace >= 0 and close_brace > open_brace:
            yield open_brace, close_brace + 1, text[open_brace:close_brace + 1], masked[open_brace:close_brace + 1]


def iter_methods(text: str, parser):
    for block_start, _, block, masked in iter_code_blocks(text, parser):
        for match in METHOD_RE.finditer(masked):
            if brace_depth(masked, match.start()) != 1:
                continue
            body_start = match.start('body')
            if match.group('body') == '{':
                body_end = parser.match_brace(masked, body_start)
                if body_end < 0:
                    continue
                end = body_end + 1
            else:
                end = masked.find(';', body_start + 2)
                if end < 0:
                    continue
                end += 1
            yield match, block[match.start():end], block_start + match.start()


def has_boundary(body: str, parser) -> bool:
    masked = parser.mask_csharp(body)
    return bool(re.search(r'\btry\b', masked) and re.search(r'\bcatch\b', masked))


def has_logging(body: str) -> bool:
    return bool(
        re.search(r'\b[A-Za-z_]\w*\s*\.\s*Log(?:Trace|Debug|Information|Warning|Error|Critical)\s*\(', body)
        or re.search(r'\bOperationalLoggerFactory\s*\.\s*CreateLogger\s*\(', body)
    )


def line_of(text: str, position: int) -> int:
    return text.count('\n', 0, position) + 1


def collect(root: Path, parser):
    components = root / 'src/PublisherStudio.Web/Components'
    records = []
    for path in sorted(components.rglob('*.razor')):
        if path.name == '_Imports.razor':
            continue
        text = path.read_text(encoding='utf-8-sig', errors='replace')
        relative = path.relative_to(root / 'src/PublisherStudio.Web').as_posix()
        for match, body, absolute_start in iter_methods(text, parser):
            signature = normalize_signature(match)
            records.append({
                'id': f'{relative}|{signature}',
                'relative': relative,
                'signature': signature,
                'line': line_of(text, absolute_start),
                'boundary': has_boundary(body, parser),
                'logging': has_logging(body),
                'expression': match.group('body') == '=>',
            })
    return records


def main() -> int:
    ap = argparse.ArgumentParser(description='Require method-granular resilience for PublisherStudio Razor components.')
    ap.add_argument('--root', required=True, type=Path)
    ap.add_argument('--write-baseline', action='store_true')
    args = ap.parse_args()
    root = args.root.resolve()
    parser = load_parser(root / 'build/audit_application_architecture.py')
    records = collect(root, parser)
    baseline_path = root / 'build/component-method-resilience-baseline.json'

    if args.write_baseline:
        legacy = sorted(record['id'] for record in records if not (record['boundary'] and record['logging']))
        baseline = {
            'schemaVersion': 1,
            'description': 'Legacy Razor methods without a complete try/catch plus structured logging boundary. New methods are not allowed to join this list.',
            'legacyWithoutBoundary': legacy,
        }
        baseline_path.write_text(json.dumps(baseline, indent=2) + '\n', encoding='utf-8')
        print(f'Wrote PublisherStudio component-method resilience baseline with {len(legacy)} legacy method(s).')
        return 0

    if not baseline_path.exists():
        print(f'Component resilience audit failed: baseline is missing: {baseline_path}')
        return 1
    baseline = json.loads(baseline_path.read_text(encoding='utf-8'))
    legacy = set(baseline.get('legacyWithoutBoundary', []))
    failures = []
    protected = 0
    for record in records:
        complete = record['boundary'] and record['logging']
        if record['id'] in legacy:
            continue
        protected += 1
        if not complete:
            missing = []
            if not record['boundary']: missing.append('try/catch')
            if not record['logging']: missing.append('structured logging')
            failures.append(f"{record['relative']}:{record['line']} {record['signature']}: missing {' and '.join(missing)}")

    current_ids = {record['id'] for record in records}
    stale = sorted(entry for entry in legacy if entry not in current_ids)
    if stale:
        failures.append(f'Component resilience baseline contains {len(stale)} stale method signature(s); remove resolved/deleted legacy entries instead of carrying dead exemptions.')

    if failures:
        print('Component method resilience audit failed:')
        for failure in failures:
            print(f'  - {failure}')
        return 1
    print(f'Component method resilience audit passed: {len(records)} Razor method(s) inventoried; {protected} method(s) are protected by the no-new-unguarded-method rule; {len(legacy)} explicit legacy method(s) remain tracked.')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
