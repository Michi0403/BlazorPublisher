'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const read = p => fs.readFileSync(path.join(root, p), 'utf8');
const ribbon = read('src/PublisherStudio.Web/Components/Editor/PublicationRibbon.razor');
const editor = read('src/PublisherStudio.Web/Components/Pages/Editor.razor');
const library = read('src/PublisherStudio.Web/Components/Editor/PanelLibrary.razor');
const css = read('src/PublisherStudio.Web/wwwroot/css/site.css');
const segment = (start, end) => ribbon.slice(ribbon.indexOf(start), ribbon.indexOf(end, ribbon.indexOf(start)));
const objectMenu = segment('<DxRibbonItem Text=\'@LT("Objects")\'', '<DxRibbonItem Text=\'@LT("Media")\'');
const mediaMenu = segment('<DxRibbonItem Text=\'@LT("Media")\'', '</DxRibbonGroup>');
const panelGroup = segment('<DxRibbonGroup Text=\'@LT("Panels")\'', '</DxRibbonGroup>');
for (const name of ['Picture', 'Create picture', 'Create picture with AI', 'Create video with AI', 'Create audio with AI']) {
  assert.match(mediaMenu, new RegExp(name));
}
assert.doesNotMatch(objectMenu, /Create picture|Interactive 3D blob|Text='@LT\("Picture"\)'/);
assert.match(mediaMenu, /@if \(CanShowAiCouncil\)/);
for (const name of ['Panel library', 'Insert blank panel', 'Interactive 3D blob', 'Create panel template with AI']) {
  assert.ok(panelGroup.includes(name));
}
assert.ok(panelGroup.includes('DropDownCssClass'));
assert.ok(library.includes('Create templates by prompt') && library.includes('AiAvailable'));
assert.ok(css.includes('width:min(1080px'));
assert.ok(editor.includes('RequestAiTemplate="OpenAiPanelTemplateAsync"'));
for (const kind of ['picture', 'video', 'audio', 'template', 'blob3d']) {
  assert.ok(editor.includes(`case "${kind}"`));
}
assert.match(editor, /ResolvePublisherMediaTeamKey/);
assert.match(editor, /AiBridge\.ChatAsync/);
assert.match(editor, /initial\.setup\.canirun\.recommendations/);
assert.match(editor, /No media is inserted without a verified result/);
assert.match(editor, /_mediaAiBusy/);
console.log('PASS: connected-only native media AI, panel dropdown, expanded library, team negotiation and existing security bridge');
