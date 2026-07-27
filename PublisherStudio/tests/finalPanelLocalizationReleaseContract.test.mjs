import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

const release = read('Build-Release.ps1');
assert.match(release, /assets\\PublisherStudio\.ico/);
assert.match(release, /Copy-Item -LiteralPath \$publisherIcon -Destination \(Join-Path \$setupFolder "PublisherStudio\.ico"\)/);
assert.ok(release.indexOf('Copy-Item -LiteralPath $publisherIcon') < release.indexOf('$requiredSetupFiles'), 'Icon copy must happen before setup completeness validation.');

const studio = read('src/PublisherStudio.Web/Components/Editor/PanelStudio.razor');
assert.match(studio, /DesignPreviewOnly="false"/);
assert.match(studio, /<button @key="element\.Id"/);
assert.match(studio, /var committed = \(PanelElement\)Files\.CloneElement\(_draft\)/);
assert.match(studio, /_loadedId = InitialPanel\?\.Id/);
assert.match(studio, /ExportJsonCanvasAsync/);
assert.match(studio, /\.publisher-panel\.json/);
assert.match(studio, /publisherStudioElement/);

const panelView = read('src/PublisherStudio.Web/Components/Editor/PanelView.razor');
assert.match(panelView, /<section @key="view\.Id"/);
assert.match(panelView, /<div @key="element\.Id" class="publication-panel-element/);

const editor = read('src/PublisherStudio.Web/Components/Pages/Editor.razor');
assert.match(editor, /embeddedHtml\.Id = Guid\.NewGuid\(\)/);
assert.match(editor, /embeddedHtml\.X = 0/);
assert.match(editor, /embeddedHtml\.Width = 160/);
assert.match(editor, /applied = remainsStandaloneHtml/);
assert.match(editor, /if \(!applied\)/);

const state = read('src/PublisherStudio.Web/Services/EditorStateService.cs');
assert.ok((state.match(/FindIndex\(element => element\.Id == selected\.Id\)/g) || []).length >= 2, 'Panel replacements must locate the target by stable id, not stale object reference.');
assert.match(state, /PromoteSelectedHtmlEmbedToPanel/);

const panels = read('src/PublisherStudio.Web/Services/Panels/PanelDocumentService.cs');
assert.match(panels, /new HashSet<Guid>\(\)/);
assert.match(panels, /EnsureUniqueId\(element\.Id, usedElementIds\)/);
assert.match(panels, /panelIdRegistered: true/);

const htmlView = read('src/PublisherStudio.Web/Components/Editor/HtmlEmbedView.razor');
assert.match(htmlView, /loading="eager"/);
const css = read('src/PublisherStudio.Web/wwwroot/css/site.css');
assert.match(css, /final Panel\/Div Studio graph persistence and true arrange preview/);
assert.match(css, /arrange-preview \.publication-panel-element>\*/);
assert.match(css, /visibility:visible!important/);

const en = JSON.parse(read('src/PublisherStudio.Web/Localization/en-US.json'));
const de = JSON.parse(read('src/PublisherStudio.Web/Localization/de-DE.json'));
const assertNoCaseInsensitiveCatalogDuplicates = (catalog, label) => {
  const seen = new Map();
  for (const key of Object.keys(catalog)) {
    const normalized = key.toLocaleLowerCase('en-US');
    assert.ok(!seen.has(normalized), `${label} catalog contains case-insensitive duplicate keys: ${seen.get(normalized)} and ${key}`);
    seen.set(normalized, key);
  }
};
assertNoCaseInsensitiveCatalogDuplicates(en, 'English');
assertNoCaseInsensitiveCatalogDuplicates(de, 'German');
assert.deepEqual(Object.keys(en).sort(), Object.keys(de).sort());
assert.ok(Object.keys(en).length >= 770);
assert.equal(de['Text.Panel␠/␠Div␠Studio'], 'Panel-/DIV-Studio');
assert.equal(de['Text.Save␠panel'], 'Panel speichern');
const localizationRuntime = read('src/PublisherStudio.Web/wwwroot/js/localizationRuntime.js');
assert.match(localizationRuntime, /return complete \? translated : value/);

const wordArt = read('src/PublisherStudio.Web/Components/Editor/SvgWordArtText.cs');
assert.doesNotMatch(wordArt, /sequence\+\+/);
assert.match(wordArt, /builder\.OpenElement\(0, "text"\)/);
const capture = read('src/PublisherStudio.Web/Services/Streaming/Capture/WindowsProcessLoopbackCapture.cs');
assert.match(capture, /\[SupportedOSPlatform\("windows"\)\]/);


// Catalog every statically identifiable maintained Razor label/tooltip. A new hardcoded
// string must be added to the external dictionaries before this release gate passes.
const attrPattern = /\b(?:Text|Title|Tooltip|title|placeholder|aria-label|DropDownCaption)="([^"@{}]+)"/g;
const nodePattern = />\s*([^<@{}][^<{}]{1,240}?)\s*</g;
const componentRoot = path.join(root, 'src/PublisherStudio.Web/Components');
const visit = directory => {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const full = path.join(directory, entry.name);
    if (entry.isDirectory()) visit(full);
    else if (entry.name.endsWith('.razor')) {
      const markup = fs.readFileSync(full, 'utf8').split('@code', 1)[0];
      const attributeValues = [...markup.matchAll(attrPattern)].map(match => match[1]);
      const nodeValues = [...markup.matchAll(nodePattern)]
        .filter(match => {
          const tagStart = markup.lastIndexOf('<', match.index);
          const tagEnd = tagStart >= 0 ? markup.indexOf('>', tagStart) : -1;
          const openingTag = tagStart >= 0 && tagEnd >= tagStart ? markup.slice(tagStart, tagEnd + 1) : '';
          return !/^<(?:code|pre)\b/i.test(openingTag);
        })
        .map(match => match[1]);
      const values = [...attributeValues, ...nodeValues];
      for (const raw of values) {
        const source = raw.replace(/&amp;/g, '&').replace(/&quot;/g, '"').replace(/\s+/g, ' ').trim();
        if (source.length < 2 || source.length > 240 || !/[A-Za-z]/.test(source)) continue;
        if (['@','{','}','=>','="',');','?.','??'].some(token => source.includes(token)) || source.includes('=') || source.includes(';')) continue;
        const key = `Text.${source.replaceAll(' ', '␠')}`;
        assert.ok(Object.hasOwn(en, key), `Uncatalogued PublisherStudio UI text in ${path.relative(root, full)}: ${source}`);
      }
    }
  }
};
visit(componentRoot);

console.log('PASS Panel/Div Studio persistence, open interchange, localization and release packaging contracts.');
