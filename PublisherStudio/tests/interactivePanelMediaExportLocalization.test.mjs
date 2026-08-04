import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = (...parts) => fs.readFileSync(path.join(root, ...parts), 'utf8');
const json = (...parts) => JSON.parse(read(...parts));

const interop = read('src','PublisherStudio.Web','wwwroot','js','publisherInterop.js');
const mediaInterop = read('src','PublisherStudio.Web','wwwroot','js','mediaStudioInterop.js');
const mediaStudio = read('src','PublisherStudio.Web','Components','Editor','MediaStudio.razor');
const inspector = read('src','PublisherStudio.Web','Components','Editor','InspectorPanel.razor');
const state = read('src','PublisherStudio.Web','Services','EditorStateService.cs');
const css = read('src','PublisherStudio.Web','wwwroot','css','site.css');
const localizationRuntime = read('src','PublisherStudio.Web','wwwroot','js','localizationRuntime.js');
const en = json('src','PublisherStudio.Web','Localization','en-US.json');
const de = json('src','PublisherStudio.Web','Localization','de-DE.json');
const webProject = read('src','PublisherStudio.Web','PublisherStudio.Web.csproj');
const installerProject = read('src','PublisherStudio.InstallerConsole','PublisherStudio.InstallerConsole.csproj');
const packageJson = json('src','PublisherStudio.Web','package.json');

assert.match(webProject, /<Version>2\.0\.3<\/Version>/);
assert.match(installerProject, /<Version>2\.0\.3<\/Version>/);
assert.equal(packageJson.version, '2.0.3');

assert.match(interop, /const liveElement = Array\.from\(element\.querySelectorAll\('\.publication-panel-element\[data-element-id\]'\)\)/);
for (const property of ['left','top','width','height'])
  assert.ok(interop.includes(`operation.liveElement.style.${property}`), `Panel Studio must update live content ${property} while dragging.`);
assert.match(interop, /CommitPanelElementBounds/);

assert.match(mediaStudio, /data-sequence-selection-handle="start"/);
assert.match(mediaStudio, /data-sequence-selection-handle="end"/);
assert.match(mediaStudio, /initializeMediaStudio/);
assert.doesNotMatch(mediaStudio, /_initializedSession\s*!=\s*SessionId/, 'Timeline binding must be refreshed after the source and selection controls render.');
assert.match(mediaInterop, /bindSequenceSelectionHandles/);
assert.match(mediaInterop, /VideoTimeSelectionCommitted/);
assert.match(css, /\.media-sequence-segment\{pointer-events:auto;cursor:pointer\}/);
assert.match(css, /\.media-sequence-timeline\{overflow:visible\}/);
assert.match(css, /media-sequence-selection-boundary::after/);
assert.match(css, /content:"S"/);
assert.match(css, /content:"E"/);

assert.match(css, /panel-layout-responsive \.publication-panel-element\{display:flex;flex-direction:column/);
assert.match(css, /container-type:inline-size/);
assert.match(css, /grid-template-columns:repeat\(12,minmax\(0,1fr\)\)/);

assert.match(interop, /PublisherStudioTooltips\?\.refresh\(document\)/);
assert.match(interop, /__publisherSignalRuntime\?\.startPage/);
assert.match(interop, /new MutationObserver\(\(\)=>requestAnimationFrame\(refresh\)\)/);
assert.match(interop, /setTimeout\(refresh,1000\)/);
assert.match(interop, /<html lang="\$\{escapeHtml\(exportCulture\)\}">/);

assert.match(inspector, /Application language/);
assert.match(inspector, /Publication language/);
assert.match(inspector, /ChangeApplicationCulture/);
assert.match(inspector, /ChangePublicationCulture/);
assert.match(inspector, /Edit all application strings/);
assert.match(state, /public void SetPublicationCulture\(string culture\)/);
assert.match(localizationRuntime, /data-i18n-key/);
assert.deepEqual(Object.keys(en).sort(), Object.keys(de).sort(), 'English and German application dictionaries must have identical keys.');
assert.equal(de['Text.Application language'], 'Anwendungssprache');
assert.equal(de['Text.Publication language'], 'Publikationssprache');
assert.equal(de['Text.Drag selection start'], 'Auswahlstart ziehen');
assert.equal(de['Text.Drag selection end'], 'Auswahlende ziehen');

console.log('PublisherStudio interactive Panel/Media/export/localization source contracts passed.');
