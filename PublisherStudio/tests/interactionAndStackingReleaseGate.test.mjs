import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

const panelStudio = read('src/PublisherStudio.Web/Components/Editor/PanelStudio.razor');
const pageSurface = read('src/PublisherStudio.Web/Components/Editor/PageSurface.razor');
const panelView = read('src/PublisherStudio.Web/Components/Editor/PanelView.razor');
const htmlView = read('src/PublisherStudio.Web/Components/Editor/HtmlEmbedView.razor');
const interop = read('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js');
const css = read('src/PublisherStudio.Web/wwwroot/css/site.css');
const layout = read('src/PublisherStudio.Web/Services/Publication/PublicationElementLayoutService.cs');
const layoutController = read('src/PublisherStudio.Web/Controllers/PublicationLayoutController.cs');
const automation = read('src/PublisherStudio.Web/Services/Automation/BrowserAutomationServices.cs');
const services = read('src/PublisherStudio.Web/PublisherStudioServiceCollectionExtensions.cs');
const editor = read('src/PublisherStudio.Web/Components/Pages/Editor.razor');
const changelog = read('CHANGELOG-v1.0.88.md');
const contributorRules = read('AGENTS.md');

// The drop transaction must survive dragend and circuit timing without dereferencing cleared component state.
assert.match(panelStudio, /var draft = _draft;[\s\S]*var view = CurrentView;[\s\S]*var prototype = _dragPrototype;[\s\S]*var point = await ResolveCanvasPoint/);
assert.match(panelStudio, /var element = Files\.CloneElement\(prototype\);/);
assert.doesNotMatch(panelStudio, /var element = _dragPrototype;/);
assert.match(panelStudio, /catch \(JSDisconnectedException/);
assert.match(panelStudio, /finally[\s\S]*EndDrag\(\)/);

// Web/3D content is a normal movable publication object in Mainframe and Panel Studio arrangement mode.
assert.match(pageSurface, /<HtmlEmbedView[^>]*DesignerMode="true"/);
assert.match(panelView, /<HtmlEmbedView[^>]*DesignerMode="@DesignerMode"/);
assert.match(htmlView, /data-designer-object-shield/);
assert.match(css, /\.publication-html-design-shield\{[^}]*z-index:4/);

// One shared service owns geometry and layer ordering and is exposed through DI/API.
assert.match(layout, /interface IPublicationElementLayoutService/);
for (const method of ['ApplyBounds', 'Nudge', 'NormalizeZOrder', 'MoveLayer', 'Reorder'])
  assert.match(layout, new RegExp(`\\b${method}\\b`));
assert.match(layoutController, /Route\("api\/publication\/layout"\)/);
assert.match(services, /AddSingleton<IPublicationElementLayoutService, PublicationElementLayoutService>/);
assert.match(panelStudio, /@inject IPublicationElementLayoutService Layout/);
assert.match(pageSurface, /KeyboardLayerMove/);

// Mouse, pen, touch, keyboard and Steam Deck/gamepad commands share the same commit surface.
assert.match(interop, /element\.addEventListener\('pointerdown'/);
assert.match(interop, /element\.addEventListener\('pointermove'/);
assert.match(interop, /element\.addEventListener\('pointerup'/);
assert.match(css, /touch-action:none/);
assert.match(interop, /navigator\.getGamepads/);
assert.match(interop, /KeyboardLayerMove/);
assert.match(interop, /PanelStudioCommand/);
assert.match(panelStudio, /tabindex="0"/);

// Local overlays stay inside their owning stacking context instead of overpowering ribbons/dialogs.
assert.match(css, /\.panel-studio-canvas\{[^}]*isolation:isolate/);
assert.match(css, /\.panel-studio-hit-layer\{z-index:40/);
assert.match(css, /\.panel-studio-drop-layer\{z-index:50/);
assert.match(css, /\.ps-component-designer-map-shield[\s\S]*?z-index:4/);
for (const forbidden of ['z-index:9000', 'z-index:10000', 'z-index:10001', 'z-index:10010', 'z-index:2147483000'])
  assert.ok(!css.includes(forbidden), `Local interaction overlay still uses forbidden global z-index ${forbidden}.`);
const applicationZValues = [...`${css}\n${interop}`.matchAll(/z-index\s*:\s*(\d+)/g)].map(match => Number(match[1]));
assert.ok(applicationZValues.every(value => value <= 5000), `Application CSS/JS contains an unbounded z-index: ${Math.max(...applicationZValues)}`);
assert.match(css, /--publisher-z-modal:\s*1040/);
assert.match(css, /--publisher-z-export:\s*1900/);
assert.doesNotMatch(panelStudio, /ZIndex \+ 10000/);

// Frontend failures are logged and surfaced through the shared notifier; automation services log too.
assert.match(panelStudio, /@inject ILogger<PanelStudio> Logger/);
assert.match(panelStudio, /@inject IUserNotificationService Notifications/);
assert.match(pageSurface, /ReportCanvasInteractionError/);
assert.match(pageSurface, /Notifications\.Warning/);
assert.match(automation, /ILogger<UserInputAutomationService>/);
assert.match(automation, /ILogger<ScreenshotCaptureService>/);
assert.match(editor, /<UserNotificationHost\s*\/>/);

// This recurring release gate must be recorded before it can be marked complete.
for (const item of ['Mainframe', 'Panel / Div Studio', 'mouse', 'touch', 'keyboard', 'controller', 'z-order', 'HTML export', 'render export', 'logging', 'notification'])
  assert.match(changelog, new RegExp(item.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'i'));
assert.match(changelog, /Interaction and stacking release gate[\s\S]*\[x\]/i);
assert.match(contributorRules, /Interaction, stacking, input and frontend-failure release gate/);
assert.match(contributorRules, /Every release that adds or changes a visual object/);

console.log('Cross-surface interaction, circuit-safe drag, local stacking, multimodal input, API layout, logging and notification release-gate contracts passed.');
