from __future__ import annotations
from pathlib import Path
import hashlib, json, re, subprocess, sys, xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[1]
failures=[]
def read(rel):
    p=ROOT/rel
    if not p.is_file(): failures.append(f'missing {rel}'); return ''
    return p.read_text(encoding='utf-8-sig')
def require(rel,token,label):
    if token not in read(rel): failures.append(f'{label} missing from {rel}: {token}')
for script,args in (
 ('build/audit_repository_build_storage.py',[]),
 ('build/audit_transient_ui_state_ownership.py',['--root',str(ROOT),'--product','publisherstudio']),
):
    r=subprocess.run([sys.executable,str(ROOT/script),*args],text=True,capture_output=True)
    if r.returncode: failures.append(f'{script} failed: '+(r.stderr or r.stdout).strip())
for rel,token,label in (
 ('build/RepositoryBuildStorage.Common.ps1',"Join-Path $repository 'artifacts/.build-storage'",'repository-local default storage'),
 ('Build-Release.ps1','Initialize-Future2RepositoryBuildStorage','release storage initialization'),
 ('build/Build-Documentation.ps1','Initialize-Future2RepositoryBuildStorage','documentation storage initialization'),
 ('build/Assert-PowerShellCompatibility.ps1','repository build-storage contract','maintenance guard'),
 ('AGENTS.md','## Repository-local release-build storage','architecture documentation'),
 ('CHANGELOG-v4.0.2-REPOSITORY-BUILD-STORAGE.md','# PublisherStudio 4.0.2','changelog'),
 ('RELEASE.md','# PublisherStudio 4.0.2','release notes'),
 ('VALIDATION-v4.0.2-source.md','# PublisherStudio 4.0.2 source validation','validation'),
): require(rel,token,label)
version='4.0.2'
version_files=(
 'src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj',
 'src/PublisherStudio.Web/package.json','src/PublisherStudio.Web/package-lock.json','src/PublisherStudio.Web/Components/App.razor',
 'src/PublisherStudio.Web/Components/Pages/Editor.razor','src/PublisherStudio.Web/Components/Editor/PageSurface.razor',
 'src/PublisherStudio.Web/Components/Editor/BarcodeEditor.razor','src/PublisherStudio.Web/Components/Editor/MediaStudio.razor',
 'src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor','src/PublisherStudio.Web/Components/Editor/WordArtPathEditor.razor')
for rel in version_files:
    if version not in read(rel): failures.append(f'{version} active identity missing from {rel}')
m=re.fullmatch(r'(\d+)\.(\d+)\.(\d+)',version)
if not m or int(m.group(2))>9 or int(m.group(3))>9: failures.append('version violates single-digit minor/patch policy')
for rel in ('src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj'):
    try:
        root=ET.fromstring((ROOT/rel).read_text(encoding='utf-8'))
        if root.findtext('.//Version')!=version: failures.append(f'{rel} version mismatch')
    except Exception as exc: failures.append(f'{rel} XML parse failure: {exc}')
for rel in ('src/PublisherStudio.Web/package.json','src/PublisherStudio.Web/package-lock.json'):
    try:
        data=json.loads((ROOT/rel).read_text(encoding='utf-8'))
        if data.get('version')!=version: failures.append(f'{rel} root version mismatch')
        if rel.endswith('package-lock.json') and data.get('packages',{}).get('',{}).get('version')!=version: failures.append('package-lock root package version mismatch')
    except Exception as exc: failures.append(f'{rel} JSON parse failure: {exc}')
try: ET.parse(ROOT/'Directory.Build.targets')
except Exception as exc: failures.append(f'Directory.Build.targets XML invalid: {exc}')
manifest=read('build/javascript-diagnostics-files.sha256')
for line in manifest.splitlines():
    m=re.fullmatch(r'([0-9a-f]{64})  (.+\.js)',line)
    if not m: continue
    p=ROOT/m.group(2)
    if not p.is_file(): failures.append(f'manifest file missing: {m.group(2)}'); continue
    actual=hashlib.sha256(p.read_bytes().replace(b'\r\n',b'\n').replace(b'\r',b'\n')).hexdigest()
    if actual!=m.group(1): failures.append(f'JavaScript diagnostics hash mismatch: {m.group(2)}')
if failures:
    for f in failures: print('FAIL: '+f,file=sys.stderr)
    raise SystemExit(1)
print('PublisherStudio 4.0.2 repository-build-storage audit passed: heavy release caches/temp follow the repository, maintenance guards protect the contract, transient UI ownership remains intact, JavaScript diagnostics remain valid, and active identity is aligned.')
