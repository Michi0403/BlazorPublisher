#!/usr/bin/env python3
"""Audit documentation accessibility and PublisherStudio/LocalGPT 1-Wire discoverability contracts."""
from pathlib import Path
import json, subprocess, sys, tempfile
ROOT=Path(__file__).resolve().parents[1]
failures=[]
def text(rel):
 p=ROOT/rel
 if not p.is_file(): failures.append(f"missing file: {rel}"); return ""
 return p.read_text(encoding="utf-8-sig")
def require(rel,*needles):
 data=text(rel)
 for n in needles:
  if n not in data: failures.append(f"{rel}: missing {n!r}")

def forbid(rel,*needles):
 data=text(rel)
 for n in needles:
  if n in data: failures.append(f"{rel}: forbidden {n!r}")

require('src/PublisherStudio.Web/BusinessObjects/DocumentationModels.cs','PublisherDocumentationViewerRequest','PublisherDocumentationProfile')
require('src/PublisherStudio.Web/BusinessObjects/OrganicPluginModels.cs','OrganicProtocolProfile','OrganicProtocolSettings')
require('src/PublisherStudio.Web/Controllers/DocumentationController.cs','[HttpGet("profile")]','ActionResult<PublisherDocumentationProfile>','[HttpGet("/help-docs/{**relativePath}")]')
require('src/PublisherStudio.Web/Controllers/OrganicWireHttpController.cs','[HttpGet("profile")]','ActionResult<OrganicProtocolProfile>')
require('src/PublisherStudio.Web/Components/Shared/DocumentationViewerHost.razor','<dialog','role="dialog"','aria-modal="true"','aria-labelledby','aria-label="Close documentation viewer"','target="_blank"','CloseFromBrowser')
require('src/PublisherStudio.Web/wwwroot/js/documentationViewer.js','showModal()','cancel','previous.focus()')
require('src/PublisherStudio.Web/Components/Pages/Help.razor','IPublisherDocumentationViewerService','Viewer.Open')
require('src/PublisherStudio.Web/Components/Editor/PublicationRibbon.razor','IPublisherDocumentationViewerService','OpenDocumentation','Url = "/api/documentation/html/index.html"','Url = "/api/documentation/html/api/index.html"')
forbid('src/PublisherStudio.Web/Components/Editor/PublicationRibbon.razor','NavigateTo("/help-docs','NavigateTo("/api/documentation/pdf','Url = "/help-docs')
require('src/PublisherStudio.Web/Services/Documentation/PublisherDocumentationViewerService.cs','normalized = "/api/documentation/html/index.html"','normalized = "/api/documentation/html/" + normalized["/help-docs/".Length..]')
require('src/PublisherStudio.Web/PublisherStudio.Web.csproj','Content Update="wwwroot\\help-docs\\**\\*"','ValidatePublisherStudioDocumentationFilesForPublish')
require('src/PublisherStudio.Web/Configuration/publisher-dx-functions.json','publisher.documentation.profile','/api/documentation/profile')
require('src/PublisherStudio.Web/Services/OrganicPlugins/OrganicCapabilityAndExecutionServices.cs','publisher.documentation.profile','HtmlRoute = "/api/documentation/html/index.html"','ApiRoute = "/api/documentation/html/api/index.html"','publisherstudio.picture.ocr','localgpt.vision.ocr')
forbid('src/PublisherStudio.Web/Services/OrganicPlugins/OrganicCapabilityAndExecutionServices.cs','HtmlRoute = "/help-docs/index.html"','ApiRoute = "/help-docs/api/index.html"')
require('src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor','Method and route','Configuration','Runtime','MaximumMessageBytes','PeerExpirySeconds','AutoConnectDiscoveredPeer','RemoteCapabilities','localgpt.vision.ocr','/api/organic/onewire/http-json/profile')
require('src/PublisherStudio.Web/Components/Editor/PictureEditor.razor.cs','CapabilityKey = "localgpt.vision.ocr"','modelName = "deepseek-ocr"')
require('docs/styles/publisherstudio-kawaii.js','mountThemeControl','publisherstudio-docs-theme','publisherstudio-cursor-paw')
require('docs/styles/publisherstudio-kawaii.css','--kawaii-docs-rail-width: clamp(15rem, 13vw, 20rem)','--kawaii-docs-panel-gap: clamp(1rem, 1.35vw, 2.4rem)','publisherstudio-snapshot-layout','--publisherstudio-dark-contrast-ring:','publisherstudio-kawaii-star-drift','publisherstudio-hover-sprinkle')
require('build/Build-Documentation.ps1','New-PublisherStudioHtmlPrintBook','Convert-PublisherStudioApiKawaiiDetails','html-browser-print','publisherstudio-kawaii-docs','Copy-Item -Path (Join-Path $siteRoot "*") -Destination $publishRoot -Recurse -Force')
forbid('build/Build-Documentation.ps1','html-browser-compact-handbook')
require('build/Update-GitHubPagesSnapshot.ps1','publisherstudio-kawaii-docs.zip','--expected-version')
forbid('build/Update-GitHubPagesSnapshot.ps1','BranchPagesRoot','docs mirror','branch-publishing mirror')
# The authored docs tree must not be a generated Pages mirror.
for p in (ROOT/'docs').rglob('*.html'):
 if p.name!='pdf-cover.html': failures.append(f"generated HTML in authored docs tree: {p.relative_to(ROOT)}")
# JSON and Pages artifact validation.
try: json.loads(text('src/PublisherStudio.Web/Configuration/publisher-dx-functions.json'))
except Exception as e: failures.append(f"publisher-dx-functions.json invalid: {e}")
validator=ROOT/'.github/scripts/prepare-pages-artifact.py'
archive=ROOT/'.github/pages/publisherstudio-kawaii-docs.zip'
with tempfile.TemporaryDirectory(prefix='publisher-contract-audit-') as tmp:
 result=subprocess.run([sys.executable,str(validator),'--archive',str(archive),'--output',tmp,'--expected-version','2.3.6'],capture_output=True,text=True)
 if result.returncode: failures.append(result.stderr.strip() or result.stdout.strip())
if failures:
 print('PublisherStudio documentation/1-Wire contract audit failed:')
 for failure in failures: print(' -',failure)
 raise SystemExit(1)
print('PublisherStudio documentation/1-Wire contract audit passed: modal access, Kawaii Pages, tagged PDF, protocol profile, method/settings disclosure and DeepSeek OCR handoff are wired.')
