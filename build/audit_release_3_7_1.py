from pathlib import Path
import json, re, sys, xml.etree.ElementTree as ET, zipfile

root = Path(__file__).resolve().parents[1]
failures = []
version = '3.7.1'

def text(rel):
    path = root / rel
    if not path.is_file():
        failures.append(f'missing {rel}')
        return ''
    return path.read_text(encoding='utf-8-sig')

def need(rel, token):
    if token not in text(rel):
        failures.append(f'{rel}: missing {token!r}')

for rel in [
    'src/PublisherStudio.Web/PublisherStudio.Web.csproj',
    'src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj',
]:
    content = text(rel)
    need(rel, f'<Version>{version}</Version>')
    match = re.search(r'<Version>(\d+)\.(\d+)\.(\d+)</Version>', content)
    if not match or int(match.group(2)) > 9 or int(match.group(3)) > 9:
        failures.append(f'{rel}: invalid one-digit release version')

for rel, token in [
    ('src/PublisherStudio.Web/package.json', '"version": "3.7.1"'),
    ('src/PublisherStudio.Web/package-lock.json', '"version": "3.7.1"'),
    ('docs/index.md', '**Version 3.7.1**'),
    ('docs/pdf/toc.yml', 'PublisherStudio-3.7.1.pdf'),
    ('docs/docfx.json', '"publisherstudioVersion": "3.7.1"'),
    ('RELEASE.md', '# PublisherStudio 3.7.1'),
    ('CHANGELOG-v3.7.1-DOCUMENTATION-SKY-MERMAID-STABILITY.md', 'PublisherStudio 3.7.1'),
    ('VALIDATION-v3.7.1-source.md', '# PublisherStudio 3.7.1 source validation'),
]:
    need(rel, token)

if '<RequirePublisherStudioDocumentationPdf Condition="\'$(RequirePublisherStudioDocumentationPdf)\' == \'\'">true</RequirePublisherStudioDocumentationPdf>' not in text('Directory.Build.targets'):
    failures.append('PDF is not required by default')

js = text('docs/templates/publisherstudio/public/main.js')
css = text('docs/templates/publisherstudio/public/main.css')
for token in [
    'const starCount = compact ? 48 : 112;',
    'const satelliteCount = compact ? 1 : 2;',
    'document.documentElement.dataset.publisherstudioDynamicSky = "ready";',
    'await mermaid.run({ nodes: pendingBlocks, suppressErrors: false });',
    'flowchart: { htmlLabels: false }',
    'const retryDelays = [0, 300, 900, 1800];',
    'mermaid.core-PFJTYFYY.min.js',
]:
    if token not in js:
        failures.append(f'JS missing {token}')
for forbidden in ['mermaid.render(', 'offsetParent', 'const nebulaCount =']:
    if forbidden in js:
        failures.append(f'JS still contains superseded path {forbidden}')
for token in [
    '3.7.1: compositor-safe dynamic sky handoff and bounded viewport effects.',
    '.publisherstudio-kawaii-sky {',
    'contain: strict !important;',
    '.publisherstudio-kawaii-nebula { display: none !important; filter: none !important; }',
    'data-publisherstudio-dynamic-sky="ready"',
    'backdrop-filter: blur(12px) saturate(1.12) !important;',
    '--kawaii-docs-viewport-gutter:clamp(1.75rem,2.8vw,3.75rem);',
]:
    if token not in css:
        failures.append(f'CSS missing {token}')

for rel in ['docs/index.md', 'docs/articles/architecture.md']:
    content = text(rel)
    if '<div class="mermaid publisherstudio-mermaid-diagram">' not in content or 'flowchart ' not in content:
        failures.append(f'{rel}: attached Mermaid source block missing')
    if '```mermaid' in content:
        failures.append(f'{rel}: legacy Mermaid fence remains')

css_bytes = (root / 'docs/templates/publisherstudio/public/main.css').read_bytes()
js_bytes = (root / 'docs/templates/publisherstudio/public/main.js').read_bytes()
for rel, expected in [
    ('src/PublisherStudio.Web/wwwroot/help-docs/public/main.css', css_bytes),
    ('src/PublisherStudio.Web/wwwroot/help-docs/public/main.js', js_bytes),
    ('src/PublisherStudio.Web/wwwroot/help-docs/styles/publisherstudio-kawaii.css', css_bytes),
    ('src/PublisherStudio.Web/wwwroot/help-docs/styles/publisherstudio-kawaii.js', js_bytes),
]:
    path = root / rel
    if not path.is_file() or path.read_bytes() != expected:
        failures.append(f'asset parity failed {rel}')
with zipfile.ZipFile(root / '.github/pages/publisherstudio-kawaii-docs.zip') as archive:
    for name, expected in [
        ('public/main.css', css_bytes),
        ('styles/publisherstudio-kawaii.css', css_bytes),
        ('public/main.js', js_bytes),
        ('styles/publisherstudio-kawaii.js', js_bytes),
    ]:
        if name not in archive.namelist() or archive.read(name) != expected:
            failures.append(f'Pages asset parity failed {name}')
    if archive.testzip() is not None:
        failures.append('Pages archive integrity failed')

expected_modes = {
    'src/PublisherStudio.Web/Components/Pages/Help.razor',
    'src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor',
    'src/PublisherStudio.Web/Components/Pages/Editor.razor',
    'src/PublisherStudio.Web/Components/Pages/Localization.razor',
    'src/PublisherStudio.Web/Components/Layout/JavaScriptDiagnosticsBridge.razor',
}
actual_modes = {
    path.relative_to(root).as_posix()
    for path in (root / 'src').rglob('*.razor')
    if '@rendermode' in path.read_text(encoding='utf-8-sig')
}
if actual_modes != expected_modes:
    failures.append(f'render-mode boundary set changed: {sorted(actual_modes ^ expected_modes)}')
if '@rendermode' in text('src/PublisherStudio.Web/Components/Pages/Error.razor'):
    failures.append('Error page is no longer static')

for rel in [
    'Directory.Build.targets',
    'src/PublisherStudio.Web/PublisherStudio.Web.csproj',
    'src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj',
]:
    try:
        ET.parse(root / rel)
    except Exception as exc:
        failures.append(f'{rel}: XML parse failed: {exc}')
for rel in ['docs/docfx.json', 'src/PublisherStudio.Web/package.json', 'src/PublisherStudio.Web/package-lock.json']:
    try:
        json.loads(text(rel))
    except Exception as exc:
        failures.append(f'{rel}: JSON parse failed: {exc}')

if failures:
    print('PublisherStudio 3.7.1 source audit failed:')
    for failure in failures:
        print('-', failure)
    sys.exit(1)
print('PublisherStudio 3.7.1 source audit passed.')
