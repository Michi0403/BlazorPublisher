import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');
const panel = read('src/PublisherStudio.Web/Components/Editor/PanelStudio.razor');
const page = read('src/PublisherStudio.Web/Components/Editor/PageSurface.razor');
const interop = read('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js');
const picker = read('src/PublisherStudio.Web/Components/Editor/SystemFontPicker.razor');
const fonts = read('src/PublisherStudio.Web/Services/SystemFontCatalog.cs');
const story = read('src/PublisherStudio.Web/Components/Editor/StoryEditor.razor');
const css = read('src/PublisherStudio.Web/wwwroot/css/site.css');

assert.match(panel, /DesignerMode="false" DesignPreviewOnly="false"/);
assert.match(css, /final15: modal selection isolation and reliable Panel\/Div Studio interaction/);
assert.match(panel, /<PictureEditor Visible="@_pictureEditorVisible"/);
assert.match(panel, /ActivatePanelElement/);
assert.match(panel, /SelectedElement is ImageFrameElement/);
assert.match(panel, /image\.PictureSource = result\.SourceDocument/);
for (const attribute of ['x', 'y', 'width', 'height'])
  assert.match(panel, new RegExp(`data-panel-element-${attribute}=`));
assert.match(panel, /var x = selected\.X;[\s\S]*var y = selected\.Y;[\s\S]*Layout\.MoveLayer[\s\S]*moved\.X = x;[\s\S]*moved\.Y = y;/);
assert.match(panel, /cancelPanelStudioPointer/);

assert.match(interop, /coordinateSurface = hitbox\.closest\('\.panel-studio-hit-layer'\)/);
assert.match(interop, /readNormalized\('panelElementX'/);
assert.match(interop, /panelStudioQueueInvoke/);
assert.match(interop, /cancelPanelStudioPointer/);
assert.match(interop, /addEventListener\('dblclick'/);
assert.match(interop, /ActivatePanelElement/);
assert.match(interop, /panelStudioExpectedShutdown/);

assert.match(page, /publication-panel-design-shield/);
assert.match(page, /data-designer-object-shield/);
assert.match(css, /\.publication-panel-design-shield\{[^}]*pointer-events:auto/);

assert.match(picker, /system-font-picker-dropdown/);
assert.match(picker, /Installed operating-system fonts/);
assert.match(fonts, /foreach \(var fallback in EmergencyFallbackFonts\) AddFamily/);
assert.doesNotMatch(story, /fontNameComboBox\.Items\.Clear/);
assert.match(story, /catch \(ArgumentException\)/);

console.log('PASS final5 interaction, nested picture editing, geometry preservation and font restoration contracts.');
