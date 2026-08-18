#!/usr/bin/env python3
from __future__ import annotations
from pathlib import Path
import argparse, importlib.util, re, sys

def load(path: Path, name: str):
    spec=importlib.util.spec_from_file_location(name,path); mod=importlib.util.module_from_spec(spec)
    assert spec and spec.loader; sys.modules[name]=mod; spec.loader.exec_module(mod); return mod

def has_logging(body: str)->bool:
    return bool(re.search(r'\b(?:Logger|logger|_logger|[A-Za-z_]\w*Logger)\s*\.\s*Log(?:Trace|Debug|Information|Warning|Error|Critical)\s*\(',body)
                or re.search(r'\b(?:System\.Diagnostics\.)?Trace\s*\.\s*Trace(?:Information|Warning|Error)\s*\(',body))

def line(text:str,off:int)->int:return text.count('\n',0,off)+1

def main()->int:
    ap=argparse.ArgumentParser(); ap.add_argument('--root',required=True,type=Path); a=ap.parse_args(); root=a.root.resolve(); app=root/'src/PublisherStudio.Web'
    comp=load(root/'build/audit_component_resilience.py','iterator_comp'); svc=load(root/'build/audit_service_resilience.py','iterator_svc')
    parser=comp.load_parser(root/'build/audit_application_architecture.py')
    failures=[]; count=0
    for p in sorted(app.rglob('*')):
        if not p.is_file() or p.suffix.lower() not in {'.cs','.razor'} or any(x in {'bin','obj','Migrations'} for x in p.parts) or p.name.endswith('.Designer.cs'): continue
        text=p.read_text(encoding='utf-8-sig',errors='replace'); rel=p.relative_to(root).as_posix()
        records=[]
        if p.suffix.lower()=='.razor':
            for m,body,start in comp.iter_razor_methods(text,parser): records.append((m.group('name'),body,start))
        else:
            for m in svc.parse_methods_including_records(text,parser): records.append((m.name,text[m.body_start:m.end],m.start))
        for name,body,start in records:
            masked=parser.mask_csharp(body)
            if not re.search(r'\byield\s+(?:return|break)\b',masked): continue
            count+=1; ident=f'{rel}:{line(text,start)} {name}'
            if re.search(r'\bcatch\b',masked): failures.append(f'{ident}: iterator contains catch')
            if not re.search(r'\btry\b',masked) or not re.search(r'\bfinally\b',masked): failures.append(f'{ident}: iterator requires try/finally')
            if not has_logging(body): failures.append(f'{ident}: iterator requires structured logging')
    if failures:
        print('Iterator exception policy validation failed:'); [print('  - '+x) for x in failures]
        print(f'Checked {count} iterator/yield method(s); exemptions: 0.'); return 1
    print(f'Iterator exception policy validation passed: {count} iterator/yield method(s) use logged try/finally without catch; exemptions: 0.'); return 0
if __name__=='__main__':raise SystemExit(main())
