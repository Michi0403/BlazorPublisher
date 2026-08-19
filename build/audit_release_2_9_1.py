#!/usr/bin/env python3
"""Static source audit for PublisherStudio 2.9.1 XML documentation completeness."""
from __future__ import annotations
from pathlib import Path
import hashlib, subprocess, sys

root=Path(__file__).resolve().parents[1]
checks=0

def read(rel):
    p=root/rel
    if not p.is_file(): raise AssertionError(f'missing {rel}')
    return p.read_text(encoding='utf-8-sig',errors='replace')
def require(rel,*tokens):
    global checks
    data=read(rel)
    for token in tokens:
        checks+=1
        if token not in data: raise AssertionError(f'{rel} missing {token!r}')

def tree_digest(path):
    h=hashlib.sha256()
    if not path.exists(): return 'missing'
    for p in sorted(x for x in path.rglob('*') if x.is_file()):
        h.update(p.relative_to(path).as_posix().encode()); h.update(b'\0'); h.update(p.read_bytes()); h.update(b'\0')
    return h.hexdigest()

try:
    for rel in ('src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'):
        require(rel,'<Version>2.9.1</Version>')
    require('src/PublisherStudio.Web/PublisherStudio.Web.csproj','<DevExpressVersion>25.2.9</DevExpressVersion>')
    require('Directory.Build.props','<LocalGptWireProtocolVersion>2.1.1</LocalGptWireProtocolVersion>')
    require('build/xml_documentation.py','scan_enum_members','enum_member_summary','tag_text','empty param','empty returns','empty value')
    require('build/razor_xml_documentation.py','component_summary','validate_component_type','direct @code member declaration','empty param','empty returns','empty value')
    require('build/Assert-XmlDocumentationCoverage.py','csharp_exit = run_csharp(args.root','razor_exit = run_razor(args.root', 'raise SystemExit(csharp_exit or razor_exit)')
    require('CHANGELOG-v2.9.1-XML-DOCUMENTATION-COMPLETENESS.md','6,064','3,311','345 enum values','2.1.1')
    require('VALIDATION-v2.9.1-source.md','No `dotnet`','6,064','3,311','idempotent')
    require('RELEASE.md','# PublisherStudio 2.9.1','documentation-completeness release')
    result=subprocess.run([sys.executable,str(root/'build/Assert-XmlDocumentationCoverage.py'),str(root/'src')],cwd=root,text=True,capture_output=True)
    checks+=1
    if result.returncode:
        raise AssertionError('XML documentation audit failed:\n'+result.stdout+result.stderr)
    if '6064 direct C# declarations' not in result.stdout or '3311 direct @code member declaration' not in result.stdout:
        raise AssertionError('unexpected XML documentation counts:\n'+result.stdout)
    # No schema migration is part of this documentation release; record the current tree deterministically.
    migrations=root/'src/PublisherStudio.Web/Migrations'
    print(f'PublisherStudio 2.9.1 XML documentation source audit passed: {checks} checks; migrations digest {tree_digest(migrations)}.')
except Exception as exc:
    print(f'PublisherStudio 2.9.1 source audit failed: {exc}',file=sys.stderr)
    raise SystemExit(1)
