#!/usr/bin/env python3
"""Source-only paired release audit for PublisherStudio 2.7.2."""
from pathlib import Path
import re
import sys
root=Path(__file__).resolve().parents[1]

def read(rel):
    p=root/rel
    if not p.is_file(): raise AssertionError(f"missing {rel}")
    return p.read_text(encoding='utf-8')

def require(rel,*needles):
    text=read(rel)
    missing=[n for n in needles if n not in text]
    if missing: raise AssertionError(f"{rel} missing {missing}")
try:
    for rel in ['src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj']:
        require(rel,'<Version>2.7.3</Version>')
        m=re.search(r'<Version>(\d+)\.(\d+)\.(\d+)</Version>',read(rel))
        if not m or int(m.group(2))>9 or int(m.group(3))>9:
            raise AssertionError(f'version-slot policy failed for {rel}')
    require('Directory.Build.props','<LocalGptWireProtocolVersion>2.1.1</LocalGptWireProtocolVersion>')
    require('src/PublisherStudio.Web/Components/App.razor','publisherInterop.js?v=2.7.3')
    for rel in ['src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor','src/PublisherStudio.Web/Components/Pages/Editor.razor','src/PublisherStudio.Web/Components/Editor/MediaStudio.razor']:
        require(rel,'mediaStudioInterop.js?v=2.7.3')
    modes=[]
    for p in (root/'src').rglob('*.razor'):
        for line in p.read_text(encoding='utf-8').splitlines():
            if '@rendermode' in line: modes.append((str(p.relative_to(root)),line.strip()))
    if len(modes)!=5: raise AssertionError(f'expected 5 PublisherStudio rendermode directives, found {len(modes)}')
    print('PublisherStudio 2.7.2 paired Council rejoin source audit passed.')
except (AssertionError,OSError) as exc:
    print(f'PublisherStudio 2.7.2 source audit failed: {exc}',file=sys.stderr)
    sys.exit(1)
