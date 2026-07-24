import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import vm from 'node:vm';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

const catalog = read('src/PublisherStudio.Web/Services/SystemFontCatalog.cs');
const program = read('src/PublisherStudio.Web/Program.cs');
const picker = read('src/PublisherStudio.Web/Components/Editor/SystemFontPicker.razor');
const inspector = read('src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor');
const picture = read('src/PublisherStudio.Web/Components/Editor/PictureEditor.razor');
const pictureCode = read('src/PublisherStudio.Web/Components/Editor/PictureEditor.razor.cs');
const story = read('src/PublisherStudio.Web/Components/Editor/StoryEditor.razor');
const spreadsheet = read('src/PublisherStudio.Web/Views/Spreadsheet/Editor.cshtml');
const webProject = read('src/PublisherStudio.Web/PublisherStudio.Web.csproj');
const installerProject = read('src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj');
const packageJson = JSON.parse(read('src/PublisherStudio.Web/package.json'));
const runtimeCapabilities = read('src/PublisherStudio.Web/Services/Streaming/UseCases/Runtime/StreamingRuntimeUseCases.cs');

assert.match(webProject, /<Version>1\.0\.62<\/Version>/);
assert.match(installerProject, /<Version>1\.0\.62<\/Version>/);
assert.equal(packageJson.version, '1.0.62');
assert.match(runtimeCapabilities, /Version = "1\.0\.62"/);

assert.match(program, /AddSingleton<SystemFontCatalog>\(\)/);
assert.match(catalog, /fc-list/);
assert.match(catalog, /\/usr\/share\/fonts/);
assert.match(catalog, /System\/Library\/Fonts/);
assert.match(catalog, /LocalApplicationData[\s\S]*Microsoft[\s\S]*Windows[\s\S]*Fonts/);
assert.match(catalog, /\.ttf/);
assert.match(catalog, /\.otf/);
assert.match(catalog, /\.ttc/);
assert.match(catalog, /\.otc/);
assert.match(catalog, /0x6E616D65/);
assert.match(catalog, /record\.NameId is 1 or 16/);
assert.match(catalog, /IgnoreInaccessible = true/);
assert.doesNotMatch(catalog, /https?:\/\//, 'The offline system-font catalog must never fetch remote fonts.');

assert.match(picker, /<input type="text"/);
assert.match(picker, /list="@_listId"/);
assert.match(picker, /<datalist/);
assert.match(picker, /SystemFonts\.FontFamilies/);
assert.match(picker, /HandleChanged/);
assert.doesNotMatch(picker, /<select/, 'Manual font entry must remain available.');

assert.match(inspector, /<SystemFontPicker Value="@wordArt\.FontFamily"/);
assert.doesNotMatch(inspector, /WordArtFonts/);
assert.match(picture, /<SystemFontPicker Value="@textLayer\.FontFamily"/);
assert.match(picture, /!PictureFonts\.Contains\(pictureText\.FontFamily/);
assert.match(pictureCode, /PictureFonts => SystemFonts\.FontFamilies/);
assert.doesNotMatch(pictureCode, /new\[\][\s\S]{0,300}(Arial|Calibri|Courier New)/, 'Picture Studio must not reintroduce a fixed font dropdown.');

assert.match(story, /CustomizeRibbon="CustomizeRichEditRibbon"/);
assert.match(story, /SystemFonts\.FontFamilies/);
assert.match(story, /fontNameComboBox\.Items\.Clear\(\)/);
assert.match(story, /fontNameComboBox\.Items\.Add\(font, font\)/);
assert.match(story, /fontNameComboBox\.AllowUserInput = true/);

assert.match(spreadsheet, /publisherSystemFontFamilies/);
assert.match(spreadsheet, /getRibbonItemByName\?\.\("FormatFontName"\)/);
assert.match(spreadsheet, /for \(const ribbonItem of item\?\.items \|\| \[\]\)/);
assert.match(spreadsheet, /dataSource: publisherSystemFontFamilies/);
assert.match(spreadsheet, /acceptCustomValue: true/);
assert.match(spreadsheet, /onCustomItemCreating/);
assert.match(spreadsheet, /event\.customItem = value \|\| null/);


function extractFunction(source, name) {
  const start = source.indexOf(`function ${name}`);
  assert.notEqual(start, -1, `${name} was not found.`);
  const bodyStart = source.indexOf('{', start);
  let depth = 0;
  for (let index = bodyStart; index < source.length; index++) {
    if (source[index] === '{') depth++;
    else if (source[index] === '}' && --depth === 0) return source.slice(start, index + 1);
  }
  throw new Error(`${name} has no closing brace.`);
}

const appliedOptions = [];
const spreadsheetContext = {
  publisherSystemFontFamilies: ['Alpha System', 'Beta System'],
  console,
  control: {
    getRibbonManager() {
      return {
        getRibbonItemByName(name) {
          assert.equal(name, 'FormatFontName');
          return {
            items: [
              { widget: { option(value) { appliedOptions.push(value); } } },
              { widget: { option(value) { appliedOptions.push(value); } } }
            ]
          };
        }
      };
    }
  }
};
vm.runInNewContext(`${extractFunction(spreadsheet, 'publisherSpreadsheetApplySystemFonts')}
publisherSpreadsheetApplySystemFonts(control);`, spreadsheetContext);
assert.equal(appliedOptions.length, 2, 'Every Spreadsheet font-name ribbon instance must be updated.');
for (const options of appliedOptions) {
  assert.deepEqual(Array.from(options.dataSource), ['Alpha System', 'Beta System']);
  assert.equal(options.acceptCustomValue, true);
  const custom = { text: '  Manual Font  ', customItem: null };
  options.onCustomItemCreating(custom);
  assert.equal(custom.customItem, 'Manual Font');
}

console.log('offline system-font discovery and manual-entry integration contracts passed');
