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
require('src/PublisherStudio.Web/Components/Editor/PublicationRibbon.razor','IPublisherDocumentationViewerService','OpenDocumentation')
forbid('src/PublisherStudio.Web/Components/Editor/PublicationRibbon.razor','NavigateTo("/help-docs','NavigateTo("/api/documentation/pdf')
require('src/PublisherStudio.Web/Configuration/publisher-dx-functions.json','publisher.documentation.profile','/api/documentation/profile')
require('src/PublisherStudio.Web/Services/OrganicPlugins/OrganicCapabilityAndExecutionServices.cs','publisher.documentation.profile','publisherstudio.picture.ocr','localgpt.vision.ocr')
require('src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor','Method and route','Configuration','Runtime','MaximumMessageBytes','PeerExpirySeconds','AutoConnectDiscoveredPeer','RemoteCapabilities','localgpt.vision.ocr','/api/organic/onewire/http-json/profile')
require('src/PublisherStudio.Web/Components/Editor/PictureEditor.razor.cs','CapabilityKey = "localgpt.vision.ocr"','modelName = "deepseek-ocr"')
require('docs/styles/publisherstudio-kawaii.js','ensureSnapshotMobileNavigation','ensureRootDocumentationRail')
require('docs/styles/publisherstudio-kawaii.css','publisherstudio-mobile-navigation','padding: 1rem 1.15rem 1.2rem','body > header.navbar .navbar-nav','display: none !important')
require('build/Build-Documentation.ps1','@page { size: A4 portrait','Complete API page inventory','html-browser-compact-handbook','https://michi0403.github.io/BlazorPublisher/')
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
 result=subprocess.run([sys.executable,str(validator),'--archive',str(archive),'--output',tmp,'--expected-version','2.2.3'],capture_output=True,text=True)
 if result.returncode: failures.append(result.stderr.strip() or result.stdout.strip())
if failures:
 print('PublisherStudio documentation/1-Wire contract audit failed:')
 for failure in failures: print(' -',failure)
 raise SystemExit(1)
print('PublisherStudio documentation/1-Wire contract audit passed: modal access, mobile Pages, tagged PDF, protocol profile, method/settings disclosure and DeepSeek OCR handoff are wired.')
