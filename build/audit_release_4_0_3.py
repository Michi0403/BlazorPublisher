from __future__ import annotations
from pathlib import Path
import json, subprocess, sys, xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[1]
fail=[]
for script in ('build/audit_repository_build_storage.py',):
    r=subprocess.run([sys.executable,str(ROOT/script)],cwd=ROOT)
    if r.returncode: fail.append(f'{script} failed')
for rel in ('src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'):
    if (ET.parse(ROOT/rel).findtext('.//Version') or '').strip()!='4.0.3': fail.append(f'{rel} version mismatch')
for rel in ('src/PublisherStudio.Web/package.json','src/PublisherStudio.Web/package-lock.json'):
    data=json.loads((ROOT/rel).read_text(encoding='utf-8-sig'))
    if data.get('version')!='4.0.3': fail.append(f'{rel} version mismatch')
for rel in ('CHANGELOG-v4.0.3-ZERO-CONFIG-BUILD-STORAGE-PREFLIGHT.md','VALIDATION-v4.0.3-source.md','RELEASE.md'):
    if not (ROOT/rel).is_file(): fail.append(f'missing {rel}')
if fail:
    print('\n'.join('FAIL: '+x for x in fail),file=sys.stderr); raise SystemExit(1)
print('PublisherStudio 4.0.3 zero-configuration build-storage preflight audit passed.')
