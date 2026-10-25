const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..');
const get = rel => fs.readFileSync(path.join(root, rel), 'utf8');
const pic = get('src/PublisherStudio.Web/Components/Editor/PictureEditor.razor');
const editor = get('src/PublisherStudio.Web/Components/Pages/Editor.razor');
const plugins = get('src/PublisherStudio.Web/Components/Pages/OrganicPlugins.razor');
const text = get('src/PublisherStudio.Web/Services/PublicationEditorTextService.cs');
assert.match(pic, /picture-gif-frame-action/);
assert.match(pic, /<DxButton CssClass="picture-gif-frame-action"/);
assert.doesNotMatch(pic, /<button type="button"\s+class="picture-gif-frame/);
for (const action of ['CloseMediaAiAsync', 'CloseObjectAiHelpAsync']) {
    assert.match(editor, new RegExp(`<DxButton CssClass="publisher-ai-close" Text="×" Click="${action}"`));
}
assert.doesNotMatch(editor, /<button type="button" class="publisher-ai-close"/);
for (const page of [editor, plugins]) {
    assert.match(page, /EditorText\.ParsePreferredAiModels\(_globalAiModels\)/);
    assert.doesNotMatch(page, /_globalAiModels\.Split\(/);
}
assert.match(text, /public List<string> ParsePreferredAiModels\(/);
const catalogs = ['en-US','de-DE','es-ES','fr-FR','ja-JP','uk-UA'].map(lang => JSON.parse(get(`src/PublisherStudio.Web/Localization/${lang}.json`)));
const key = 'Phrase.Editable␠GIF␠frames';
for(const dict of catalogs) assert.ok(dict[key], `Missing ${key}`);
assert.equal(catalogs[0][key], 'Editable GIF frames');
assert.equal(catalogs[1][key], 'Bearbeitbare GIF-Einzelbilder');
console.log('PublisherStudio 4.5.2 architecture: DevExpress, service-owned preference parsing and all culture catalogs: passed.');
