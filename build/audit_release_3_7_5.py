#!/usr/bin/env python3
from pathlib import Path
import sys, zipfile
ROOT=Path(__file__).resolve().parents[1]; VERSION='3.7.5'
def text(rel):
 p=ROOT/rel
 if not p.is_file(): raise SystemExit(f'PublisherStudio {VERSION} audit failed: missing {rel}')
 return p.read_text(encoding='utf-8',errors='strict')
def req(rel,*markers):
 s=text(rel)
 for m in markers:
  if m not in s: raise SystemExit(f'PublisherStudio {VERSION} audit failed: {rel} missing {m!r}')
def forbid(rel,*markers):
 s=text(rel)
 for m in markers:
  if m in s: raise SystemExit(f'PublisherStudio {VERSION} audit failed: {rel} contains forbidden {m!r}')
def main():
 parts=[int(x) for x in VERSION.split('.')]
 if len(parts)!=3 or parts[1]>=10 or parts[2]>=10: raise SystemExit('version-slot policy failed')
 for rel in ['src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj']: req(rel,f'<Version>{VERSION}</Version>')
 req('RELEASE.md',f'# PublisherStudio {VERSION}'); req('VALIDATION.md',f'# PublisherStudio {VERSION} source validation')
 req('CHANGELOG-v3.7.5-ONEWIRE-FORMAT-CONTROLLER-MODES.md',f'# PublisherStudio {VERSION}','Interaction release gate checklist')
 req('VALIDATION-v3.7.5-source.md',f'# PublisherStudio {VERSION} source validation')
 req('docs/index.md','**Version 3.7.5**'); req('docs/docfx.json','"publisherstudioVersion": "3.7.5"'); req('docs/pdf/toc.yml','PublisherStudio-3.7.5.pdf'); req('src/PublisherStudio.Web/Components/App.razor','site.css?v=3.7.5')
 # format service/controller/1-wire capability
 req('src/PublisherStudio.Web/PublisherStudioServiceCollectionExtensions.cs','IPublisherFileFormatCapabilityService','PublisherFileFormatCapabilityService')
 req('src/PublisherStudio.Web/Controllers/PublisherFileFormatsController.cs','[Route("api/publisher/file-formats")]','IPublisherFileFormatCapabilityService')
 s='src/PublisherStudio.Web/Services/OrganicPlugins/OrganicCapabilityAndExecutionServices.cs'; req(s,'publisher.file.formats','x-publisher-format-families','IPublisherFileFormatCapabilityService')
 req('src/PublisherStudio.Web/Services/OrganicPlugins/ObjectStoreOrganicCapabilityCatalog.cs','publisher.media.capabilities','media.Available')
 req('src/PublisherStudio.Web/Configuration/OrganicAddons/PublisherStudio.json','publisher.file.formats')
 req('src/PublisherStudio.Web/Configuration/publisher-dx-functions.json','publisher.file.formats')
 # controller modes are additive to existing semantic/context systems
 js='src/PublisherStudio.Web/wwwroot/js/publisherInterop.js'; req(js,'controllerMode','cursor','control','contextmenu','publisherControllerButtonEdge(gamepad, 8)','publisherControllerButtonEdge(gamepad, 9)','ControllerNudge','PanelStudioControllerCommand')
 forbid(js,'document.body.style.pointerEvents = \'none\'')
 req('src/PublisherStudio.Web/Components/Editor/PageSurface.razor','@oncontextmenu','ControllerNudge(double dx, double dy)','ControllerDuplicate','ControllerClearSelection')
 req('src/PublisherStudio.Web/Components/Editor/PanelStudio.razor','@oncontextmenu','PanelStudioControllerCommand(string command, double amount = 1)','pointer, keyboard, context-menu, and gamepad interaction binding is active')
 with zipfile.ZipFile(ROOT/'.github/pages/publisherstudio-kawaii-docs.zip') as z:
  if z.testzip() is not None or b'PublisherStudio-3.7.5.pdf' not in z.read('index.html'): raise SystemExit('PublisherStudio 3.7.5 audit failed: tracked Pages snapshot is not synchronized')
  css=(ROOT/'docs/templates/publisherstudio/public/main.css').read_bytes()
  if z.read('public/main.css')!=css or z.read('styles/publisherstudio-kawaii.css')!=css: raise SystemExit('PublisherStudio 3.7.5 audit failed: tracked Pages snapshot CSS differs')
 print('PublisherStudio 3.7.5 release audit passed: live format capability routing, media availability, additive controller modes and release identity are wired.')
 return 0
if __name__=='__main__': sys.exit(main())
