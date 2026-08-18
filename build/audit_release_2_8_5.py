#!/usr/bin/env python3
"""Static source audit for PublisherStudio 2.8.6 strict LocalGPT architecture maintenance."""
from pathlib import Path
import json,re,sys
ROOT=Path(__file__).resolve().parents[1]
checks=0

def read(rel): return (ROOT/rel).read_text(encoding='utf-8-sig',errors='replace')
def req(rel,*tokens):
 global checks
 t=read(rel)
 for x in tokens:
  if x not in t: raise AssertionError(f'{rel}: missing {x!r}')
  checks+=1
def forbid(rel,*tokens):
 global checks
 t=read(rel)
 for x in tokens:
  if x in t: raise AssertionError(f'{rel}: forbidden {x!r}')
  checks+=1

try:
 for rel in ['src/PublisherStudio.Web/PublisherStudio.Web.csproj','src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj']:
  req(rel,'<Version>2.8.6</Version>')
 req('Directory.Build.props','<LocalGptWireProtocolVersion>2.1.1</LocalGptWireProtocolVersion>')
 req('src/PublisherStudio.Web/Components/App.razor','css/site.css?v=20260818-286','videoEffectRuntime.js?v=2.8.6','publisherInterop.js?v=2.8.6')
 for rel in ['src/PublisherStudio.Web/Components/Pages/Editor.razor','src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor','src/PublisherStudio.Web/Components/Editor/MediaStudio.razor']:
  req(rel,'mediaStudioInterop.js?v=2.8.6')

 # Exact reviewed render boundary set.
 actual=[]
 for p in (ROOT/'src/PublisherStudio.Web/Components').rglob('*.razor'):
  if '@rendermode' in p.read_text(encoding='utf-8-sig',errors='replace'): actual.append(p.relative_to(ROOT).as_posix())
 expected=sorted(['src/PublisherStudio.Web/Components/Layout/JavaScriptDiagnosticsBridge.razor','src/PublisherStudio.Web/Components/Pages/Editor.razor','src/PublisherStudio.Web/Components/Pages/Help.razor','src/PublisherStudio.Web/Components/Pages/Localization.razor','src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor'])
 if sorted(actual)!=expected: raise AssertionError(f'render-mode set changed: {actual!r}')
 checks+=len(expected)

 # Zero-exemption component/service/iterator architecture.
 if (ROOT/'build/component-method-resilience-baseline.json').exists(): raise AssertionError('component resilience baseline must not exist')
 if (ROOT/'build/iterator-exception-baseline.json').exists(): raise AssertionError('iterator exception baseline must not exist')
 checks+=2
 req('build/audit_component_resilience.py','no legacy exemption list is permitted','0 legacy exemptions',"missing.append('try/catch')",'structured logging')
 req('build/Assert-ComponentSafety.ps1','every component method is method-locally guarded','no legacy exemptions are permitted','Python 3 is required for strict method-granular component resilience')
 req('build/audit_service_resilience.py',"'publisherstudio': set()",'exemptions/skips: {skipped_boot}','missing try/catch boundary','missing ILogger/Trace diagnostics')
 req('build/audit_iterator_exception_policy.py','iterator contains catch','iterator requires try/finally','iterator requires structured logging','exemptions: 0')
 req('build/Assert-IteratorExceptionPolicy.ps1','no baseline fallback is permitted')

 # LocalGPT-compatible strict ConfigureAwait architecture.
 policy=json.loads(read('build/async-continuation-policy.json'))
 if policy.get('schemaVersion')!=6 or policy.get('sourceRoot')!='src/PublisherStudio.Web': raise AssertionError('async policy schema/source root mismatch')
 if policy.get('requiredDefault')!='ConfigureAwait(false)' or policy.get('maxUnconfiguredAwaitExpressionCount')!=0 or policy.get('maxConfigureAwaitTrueOutsideComponents')!=0: raise AssertionError('async defaults weakened')
 if policy.get('rendererAffineLifecycleMethods')!=['OnInitializedAsync','OnParametersSetAsync','OnAfterRenderAsync']: raise AssertionError('renderer lifecycle list changed')
 helpers=policy.get('rendererAffineHelperMethods') or {}
 if not helpers or any('*' in k for k in helpers): raise AssertionError('renderer helper allowlist must be explicit and nonempty')
 checks+=8+sum(len(v) for v in helpers.values())
 req('build/audit_async_continuations.py','ConfigureAwait(true) is forbidden outside Components','Await foreach must configure its async enumerable with ConfigureAwait(false).','Renderer-affine helper baseline method')
 req('build/Assert-AsyncContinuationPolicy.ps1','Python 3 is required for the syntax-aware zero-tolerance async-continuation audit')

 # Supervised async ownership and DI wiring.
 req('src/PublisherStudio.Web/Services/ISupervisedTaskRunner.cs','interface ISupervisedTaskRunner','void Run(')
 req('src/PublisherStudio.Web/Services/SupervisedTaskRunner.cs','class SupervisedTaskRunner','Task.Run(','ObserveAsync(','ConfigureAwait(false)')
 req('src/PublisherStudio.Web/PublisherStudioServiceCollectionExtensions.cs','AddSingleton<ISupervisedTaskRunner, SupervisedTaskRunner>(services);')
 req('build/Assert-ServiceArchitecture.ps1','await it or use ISupervisedTaskRunner','AddSingleton<ISupervisedTaskRunner, SupervisedTaskRunner>(services);','SupervisedTaskRunner must be resolved through DI')
 # Reject direct discarded Task-returning work.
 discard=re.compile(r'(?m)^\s*_\s*=(?!>)\s*(?!await\b)[^;\r\n]*(?:Async\s*\(|InvokeAsync\s*\(|Task\.Run\s*\()')
 for p in (ROOT/'src/PublisherStudio.Web').rglob('*'):
  if not p.is_file() or p.suffix not in {'.cs','.razor'} or any(x in {'bin','obj'} for x in p.parts): continue
  if discard.search(p.read_text(encoding='utf-8-sig',errors='replace')): raise AssertionError(f'discarded async work remains: {p.relative_to(ROOT)}')
 checks+=1

 # Reported compile regression is statically guarded.
 page=read('src/PublisherStudio.Web/Components/Editor/PageSurface.razor'); editor=read('src/PublisherStudio.Web/Components/Pages/Editor.razor')
 if not re.search(r'public\s+async\s+Task\s+FitPageAsync\s*\(',page): raise AssertionError('PageSurface.FitPageAsync is missing')
 refs=editor.count('_pageSurface.FitPageAsync()')
 if refs<2: raise AssertionError(f'expected both Editor FitPageAsync call sites, found {refs}')
 checks+=3

 # Every Razor component receives a typed local logger.
 razor=[p for p in (ROOT/'src/PublisherStudio.Web/Components').rglob('*.razor') if p.name!='_Imports.razor']
 for p in razor:
  expected=f'@inject ILogger<{p.stem}> Logger'
  if p.read_text(encoding='utf-8-sig',errors='replace').count(expected)!=1: raise AssertionError(f'{p.relative_to(ROOT)} typed Logger injection mismatch')
 checks+=len(razor)

 # Preserve earlier media/editor work.
 req('src/PublisherStudio.Web/Components/Editor/MediaStudio.razor','Render selected range to video','QueueVideoEffectsRefresh','TaskRunner.Run')
 req('src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js','publisherVideoEffects','render')
 req('src/PublisherStudio.Web/Components/Editor/MediaConverterStudio.razor','Pixel format','Encoder preset')
 req('src/PublisherStudio.Web/Components/Editor/PageSurface.razor','InteractionEnabled','SelectionVisualsEnabled')

 print(f'PublisherStudio 2.8.6 strict architecture source audit passed: {checks} checks; explicit renderer helpers: {sum(len(v) for v in helpers.values())}.')
except Exception as e:
 print(f'PublisherStudio 2.8.6 source audit failed: {e}',file=sys.stderr); raise SystemExit(1)
