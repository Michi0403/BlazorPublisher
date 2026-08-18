#!/usr/bin/env python3
from __future__ import annotations
from pathlib import Path
import argparse, importlib.util, re, sys

METHOD_RE = re.compile(
    r'(?m)^[ \t]*(?P<attrs>(?:\[[^\]\n]+\][ \t]*(?:\r?\n[ \t]*)?)*)'
    r'(?P<access>public|private|protected|internal)\s+'
    r'(?P<mods>(?:(?:static|async|override|virtual|sealed|new|partial|unsafe|extern|required)\s+)*)'
    r'(?P<return>[A-Za-z_][\w<>,\.\[\]\? \t:]*(?:\s*\*)?)\s+'
    r'(?P<name>[A-Za-z_]\w*)\s*\((?P<args>[^;{}]*?)\)\s*'
    r'(?:where\s+[^\{=>\r\n]+\s*)?(?P<body>\{|=>)')

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

def depth_at(masked: str, position: int) -> int:
    depth = 0
    for char in masked[:position]:
        if char == '{': depth += 1
        elif char == '}': depth = max(0, depth - 1)
    return depth

def expression_end(masked: str, start: int) -> int:
    paren = bracket = brace = 0
    for index in range(start, len(masked)):
        char = masked[index]
        if char == '(': paren += 1
        elif char == ')': paren = max(0, paren - 1)
        elif char == '[': bracket += 1
        elif char == ']': bracket = max(0, bracket - 1)
        elif char == '{': brace += 1
        elif char == '}' and brace: brace -= 1
        elif char == ';' and paren == bracket == brace == 0: return index + 1
    return -1

def iter_code_blocks(text: str, parser):
    # Locate Razor code blocks in the raw Razor text first. Masking the entire .razor
    # file as C# can treat HTML attributes as unterminated C# strings and hide @code.
    for marker in re.finditer(r'(?m)^\s*@(code|functions)\s*\{', text):
        opening = text.find('{', marker.start(), marker.end())
        if opening < 0:
            continue
        tail = text[opening:]
        masked_tail = parser.mask_csharp(tail)
        closing_relative = parser.match_brace(masked_tail, 0)
        if closing_relative > 0:
            closing = opening + closing_relative
            yield opening + 1, text[opening + 1:closing], masked_tail[1:closing_relative]

def iter_razor_methods(text: str, parser):
    for block_start, block, masked in iter_code_blocks(text, parser):
        for match in METHOD_RE.finditer(masked):
            if depth_at(masked, match.start()) != 0:
                continue
            body_start = match.start('body')
            if match.group('body') == '{':
                end = parser.match_brace(masked, body_start)
                if end < 0: continue
                end += 1
            else:
                end = expression_end(masked, body_start + 2)
                if end < 0: continue
            yield match, block[match.start():end], block_start + match.start()

def has_logging(body: str) -> bool:
    return bool(
        re.search(r'\b(?:Logger|logger|_logger|[A-Za-z_]\w*Logger)\s*\.\s*Log(?:Trace|Debug|Information|Warning|Error|Critical)\s*\(', body)
        or re.search(r'\bOperationalLoggerFactory\s*\.\s*CreateLogger\s*\(', body)
        or re.search(r'\b(?:System\.Diagnostics\.)?Trace\s*\.\s*Trace(?:Information|Warning|Error)\s*\(', body)
    )

def line_of(text: str, position: int) -> int:
    return text.count('\n', 0, position) + 1

def main() -> int:
    ap = argparse.ArgumentParser(description='Require complete method-local resilience for every PublisherStudio Razor component method.')
    ap.add_argument('--root', required=True, type=Path)
    args = ap.parse_args()
    root = args.root.resolve()
    app = root / 'src/PublisherStudio.Web'
    components = app / 'Components'
    parser = load_parser(root / 'build/audit_application_architecture.py')
    failures: list[str] = []
    checked = 0
    checked_iterators = 0

    for path in sorted(components.rglob('*.razor')):
        if path.name == '_Imports.razor': continue
        text = path.read_text(encoding='utf-8-sig', errors='replace')
        relative = path.relative_to(app).as_posix()
        for match, body, start in iter_razor_methods(text, parser):
            checked += 1
            signature = normalize_signature(match)
            masked_body = parser.mask_csharp(body)
            ident = f'{relative}:{line_of(text, start)} {signature}'
            is_iterator = bool(re.search(r'\byield\s+(?:return|break)\b', masked_body))
            if is_iterator:
                checked_iterators += 1
                missing = []
                if not re.search(r'\btry\b', masked_body) or not re.search(r'\bfinally\b', masked_body): missing.append('try/finally')
                if re.search(r'\bcatch\b', masked_body): missing.append('iterator contains catch')
                if not has_logging(body): missing.append('structured logging')
            else:
                missing = []
                if not re.search(r'\btry\b', masked_body) or not re.search(r'\bcatch\b', masked_body): missing.append('try/catch')
                if not has_logging(body): missing.append('structured logging')
            if missing: failures.append(f"{ident}: missing/invalid {' and '.join(missing)}")

    # Component code-behind is part of the same component contract.
    for path in sorted(components.rglob('*.razor.cs')):
        text = path.read_text(encoding='utf-8-sig', errors='replace')
        relative = path.relative_to(app).as_posix()
        for method in parser.parse_methods(text):
            checked += 1
            body = text[method.start:method.end]
            masked_body = parser.mask_csharp(body)
            ident = f'{relative}:{line_of(text, method.start)} {method.type_name}.{method.name}'
            is_iterator = bool(re.search(r'\byield\s+(?:return|break)\b', masked_body))
            if is_iterator:
                checked_iterators += 1
                missing=[]
                if not re.search(r'\btry\b', masked_body) or not re.search(r'\bfinally\b', masked_body): missing.append('try/finally')
                if re.search(r'\bcatch\b', masked_body): missing.append('iterator contains catch')
                if not has_logging(body): missing.append('structured logging')
            else:
                missing=[]
                if not re.search(r'\btry\b', masked_body) or not re.search(r'\bcatch\b', masked_body): missing.append('try/catch')
                if not has_logging(body): missing.append('structured logging')
            if missing: failures.append(f"{ident}: missing/invalid {' and '.join(missing)}")

    if failures:
        print('Component method resilience audit failed:')
        for failure in failures: print(f'  - {failure}')
        print(f'Checked {checked} component methods ({checked_iterators} iterator/yield methods); no legacy exemption list is permitted.')
        return 1
    print(f'Component method resilience audit passed: {checked} component method(s) own method-local diagnostics boundaries; {checked_iterators} iterator/yield method(s) use logged try/finally without catch; 0 legacy exemptions.')
    return 0

if __name__ == '__main__': raise SystemExit(main())
