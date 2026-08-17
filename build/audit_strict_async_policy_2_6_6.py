#!/usr/bin/env python3
from pathlib import Path
import subprocess, sys
root=Path(__file__).resolve().parents[1]
def require(path, needle):
    text=(root/path).read_text(encoding='utf-8-sig',errors='replace')
    if needle not in text: raise RuntimeError(f'{path} missing: {needle}')
if (root/'build/async-continuation-baseline.json').exists():
    raise SystemExit('Legacy async-continuation-baseline.json must not exist; raw-await grandfathering is forbidden.')
require('src/PublisherStudio.Web/PublisherStudio.Web.csproj','<Version>2.7.9</Version>')
require('src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj','<Version>2.7.9</Version>')
require('build/Assert-AsyncContinuationPolicy.ps1','No raw-await baseline fallback is permitted')
require('src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor','Components.JoinDisplayValues(dataVisual.ValueFields)')
result=subprocess.run([sys.executable,str(root/'build/audit_async_continuations.py'),'--source-root',str(root/'src/PublisherStudio.Web')],text=True,capture_output=True)
print(result.stdout,end='')
if result.returncode:
    print(result.stderr,end='',file=sys.stderr); raise SystemExit(result.returncode)
print('PublisherStudio 2.6.6 strict async/build-policy regression audit passed.')
