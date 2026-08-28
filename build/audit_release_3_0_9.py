#!/usr/bin/env python3
from pathlib import Path
import json, re, sys, xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]
FAIL=[]
def require(cond,msg):
    if not cond: FAIL.append(msg)
def text(rel): return (ROOT/rel).read_text(encoding='utf-8-sig', errors='replace')

for rel in [
    'src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj',
    'src/PublisherStudio.Web/package.json','src/PublisherStudio.Web/package-lock.json','docs/docfx.json','docs/index.md',
    'docs/pdf/toc.yml','docs/pdf-cover.html','src/PublisherStudio.Web/Components/App.razor',
    'src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor','src/PublisherStudio.Web/Components/Editor/MediaStudio.razor',
    'src/PublisherStudio.Web/Components/Pages/Editor.razor','RELEASE.md']:
    require('3.0.9' in text(rel), f'{rel}: current 3.0.9 identity missing')

targets=text('Directory.Build.targets')
try: ET.parse(ROOT/'Directory.Build.targets')
except Exception as e: FAIL.append(f'Directory.Build.targets is not valid XML: {e}')
for line in targets.splitlines():
    if 'Windows_NT' in line and 'RepositoryPowerShell' not in line:
        FAIL.append('Directory.Build.targets still contains a Windows-only active build condition: '+line.strip())
require('<RepositoryPowerShell Condition="\'$(RepositoryPowerShell)\' == \'\' and \'$(OS)\' != \'Windows_NT\'">pwsh</RepositoryPowerShell>' in targets,
        'non-Windows pwsh host selection missing')
require(re.search(r'<RequirePublisherStudioDocumentationPdf[^>]*Configuration[^>]*Release[^>]*>true</RequirePublisherStudioDocumentationPdf>', targets) is not None,
        'Release PDF default missing')
require("<RequirePublisherStudioDocumentationPdf Condition=\"'$(RequirePublisherStudioDocumentationPdf)' == ''\">false</RequirePublisherStudioDocumentationPdf>" in targets,
        'Debug/non-Release PDF default is not false')

# Logging baseline must match the already-delegated facade and policy must exist.
base=json.loads(text('build/logging-baseline.json'))
entry=base['files'].get('src/PublisherStudio.Web/Services/Streaming/Capture/NativeDeviceDiscovery.cs')
require(entry == {'loggerReferences':0,'logCalls':0,'catchBlocks':2}, f'NativeDeviceDiscovery committed logging baseline changed unexpectedly: {entry!r}')
policy=text('docs/LOGGING_INTEGRITY.md')
require('Logging removal is not cleanup' in policy, 'required logging integrity policy sentence missing')
source=text('src/PublisherStudio.Web/Services/Streaming/Capture/NativeDeviceDiscovery.cs')
actual={'loggerReferences':len(re.findall(r'\bILogger(?:<[^>]+>)?\b',source)),
        'logCalls':len(re.findall(r'\.Log(?:Trace|Debug|Information|Warning|Error|Critical)\s*\(',source)),
        'catchBlocks':len(re.findall(r'\bcatch\b',source))}
require(actual['loggerReferences'] >= entry['loggerReferences'] and actual['logCalls'] >= entry['logCalls'] and actual['catchBlocks'] >= entry['catchBlocks'],
        f'NativeDeviceDiscovery source metrics {actual!r} fall below baseline {entry!r}')
require('catch (OperationCanceledException exception)' in source, 'NativeDeviceDiscovery cancellation catch boundary missing')

service_guard=text('build/Assert-ServiceArchitecture.ps1')
for token in ["'audit_service_resilience.py'", "'--product'", "'publisherstudio'"]:
    require(token in service_guard, f'service architecture token check missing: {token}')
method_guard=text('build/Assert-MethodDiagnostics.ps1')
require("@('--root', $repoRoot, '--product', 'publisherstudio')" in method_guard, 'actual tokenized publisherstudio service-audit invocation changed/missing')

node=text('build/NodeRuntime.Common.ps1')
pos_existing=node.find('if ($null -ne $nodeInfo)')
pos_provision=node.find('if ($AllowProvisioning)', pos_existing+1)
require(pos_existing >= 0 and pos_provision > pos_existing, 'existing Node reuse must precede provisioning')
require('no additional Node.js runtime will be provisioned' in node, 'newer existing Node reuse diagnostic missing')

doc=text('build/Build-Documentation.ps1')
for token in ['Google Chrome.app/Contents/MacOS/Google Chrome','Microsoft Edge.app/Contents/MacOS/Microsoft Edge','google-chrome-stable','$maximumBrowserPrintSourcePages = 1500',"$progressState.ContainsKey($key)"]:
    require(token in doc, f'documentation runtime repair missing token: {token}')
release=text('Build-Release.ps1')
require('-RequirePdf' in release, 'Build-Release.ps1 no longer requires the complete PDF')
require('-p:BuildPublisherStudioDocumentation=false' in release, 'release assembly build no longer suppresses duplicate documentation generation')

if FAIL:
    print('PublisherStudio 3.0.9 static release audit failed:')
    for f in FAIL: print('  -',f)
    sys.exit(1)
print('PublisherStudio 3.0.9 static release audit passed.')
