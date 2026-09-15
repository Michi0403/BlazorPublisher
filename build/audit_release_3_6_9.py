from pathlib import Path
import json, re, sys, xml.etree.ElementTree as ET, zipfile
root=Path(__file__).resolve().parents[1]; failures=[]; version='3.6.9'
def text(rel):
 p=root/rel
 if not p.is_file(): failures.append(f'missing {rel}'); return ''
 return p.read_text(encoding='utf-8-sig')
def need(rel,token):
 if token not in text(rel): failures.append(f'{rel}: missing {token!r}')
for rel in ['src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj']:
 t=text(rel); need(rel,f'<Version>{version}</Version>'); m=re.search(r'<Version>(\d+)\.(\d+)\.(\d+)</Version>',t)
 if not m or int(m.group(2))>9 or int(m.group(3))>9: failures.append(f'{rel}: invalid one-digit release version')
for rel,tok in [('src/PublisherStudio.Web/package.json','"version": "3.6.9"'),('src/PublisherStudio.Web/package-lock.json','"version": "3.6.9"'),('docs/index.md','**Version 3.6.9**'),('docs/pdf/toc.yml','PublisherStudio-3.6.9.pdf'),('docs/docfx.json','"publisherstudioVersion": "3.6.9"'),('RELEASE.md','# PublisherStudio 3.6.9'),('CHANGELOG-v3.6.9-DOCUMENTATION-PDF-MERMAID-GLASS-RECOVERY.md','PublisherStudio 3.6.9')]: need(rel,tok)
targets=text('Directory.Build.targets')
if '<RequirePublisherStudioDocumentationPdf Condition="\'$(RequirePublisherStudioDocumentationPdf)\' == \'\'">true</RequirePublisherStudioDocumentationPdf>' not in targets: failures.append('PDF is not required by default')
if "RequirePublisherStudioDocumentationPdf)' == '' and '$(Configuration)' == 'Release'" in targets: failures.append('Release-only PDF default remains')
js=text('docs/templates/publisherstudio/public/main.js'); css=text('docs/templates/publisherstudio/public/main.css')
for tok in ['const starCount = compact ? 48 : 112;','const nebulaCount = compact ? 3 : 5;','recoverMermaidDiagrams()','scheduleMermaidRecovery();','mermaid.core-PFJTYFYY.min.js']:
 if tok not in js: failures.append(f'JS missing {tok}')
for tok in ['3.6.9: generated deep-space/glass recovery','background-image: none !important','--publisherstudio-panel-glass: rgba(55, 36, 62, .48);','--kawaii-docs-viewport-gutter:clamp(1.75rem,2.8vw,3.75rem);','.publisherstudio-kawaii-nebula']:
 if tok not in css: failures.append(f'CSS missing {tok}')
cssb=(root/'docs/templates/publisherstudio/public/main.css').read_bytes(); jsb=(root/'docs/templates/publisherstudio/public/main.js').read_bytes()
for rel,expected in [('src/PublisherStudio.Web/wwwroot/help-docs/public/main.css',cssb),('src/PublisherStudio.Web/wwwroot/help-docs/public/main.js',jsb),('src/PublisherStudio.Web/wwwroot/help-docs/styles/publisherstudio-kawaii.css',cssb),('src/PublisherStudio.Web/wwwroot/help-docs/styles/publisherstudio-kawaii.js',jsb)]:
 if not (root/rel).is_file() or (root/rel).read_bytes()!=expected: failures.append(f'asset parity failed {rel}')
with zipfile.ZipFile(root/'.github/pages/publisherstudio-kawaii-docs.zip') as z:
 if z.read('public/main.css')!=cssb or z.read('styles/publisherstudio-kawaii.css')!=cssb: failures.append('Pages CSS parity failed')
 if z.read('public/main.js')!=jsb or z.read('styles/publisherstudio-kawaii.js')!=jsb: failures.append('Pages JS parity failed')
expected_modes={'src/PublisherStudio.Web/Components/Pages/Help.razor','src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor','src/PublisherStudio.Web/Components/Pages/Editor.razor','src/PublisherStudio.Web/Components/Pages/Localization.razor','src/PublisherStudio.Web/Components/Layout/JavaScriptDiagnosticsBridge.razor'}
actual={p.relative_to(root).as_posix() for p in (root/'src').rglob('*.razor') if '@rendermode' in p.read_text(encoding='utf-8-sig')}
if actual!=expected_modes: failures.append(f'render-mode boundary set changed: {sorted(actual^expected_modes)}')
if '@rendermode' in text('src/PublisherStudio.Web/Components/Pages/Error.razor'): failures.append('Error page is no longer static')
for rel in ['Directory.Build.targets','src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj']:
 try: ET.parse(root/rel)
 except Exception as e: failures.append(f'{rel}: XML parse failed: {e}')
for rel in ['docs/docfx.json','src/PublisherStudio.Web/package.json','src/PublisherStudio.Web/package-lock.json']:
 try: json.loads(text(rel))
 except Exception as e: failures.append(f'{rel}: JSON parse failed: {e}')
if failures:
 print('PublisherStudio 3.6.9 source audit failed:'); [print('-',f) for f in failures]; sys.exit(1)
print('PublisherStudio 3.6.9 source audit passed.')
