import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

const editor = read('src/PublisherStudio.Web/Components/Pages/Editor.razor');
const ribbon = read('src/PublisherStudio.Web/Components/Editor/PublicationRibbon.razor');
const interop = read('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js');
const css = read('src/PublisherStudio.Web/wwwroot/css/site.css');
const doctrine = read('docs/architecture/structured-website-export.md');
const packageJson = JSON.parse(read('src/PublisherStudio.Web/package.json'));
const lockJson = JSON.parse(read('src/PublisherStudio.Web/package-lock.json'));
const webProject = read('src/PublisherStudio.Web/PublisherStudio.Web.csproj');
const installerProject = read('src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj');
const streamingRuntime = read('src/PublisherStudio.Web/Services/Streaming/UseCases/Runtime/StreamingRuntimeUseCases.cs');

assert.match(ribbon, /Export structured website \(ZIP\)/);
assert.match(ribbon, /OnExportStructuredSite/);
assert.match(editor, /OnExportStructuredSite="OpenStructuredWebsiteExport"/);
assert.match(editor, /Structured website export/);
assert.match(editor, /Preserve source — exact\/lossless/);
assert.match(editor, /PNG — lossless pixels/);
assert.match(editor, /WebP — smaller, lossy/);
assert.match(editor, /AVIF — smallest/);
assert.match(editor, /WebM VP9\/VP8 \+ Opus/);
assert.match(editor, /publisherStudio\.exportStructuredWebsite/);
assert.match(editor, /StructuredWebsiteExportOptions/);
assert.match(editor, /KeepVideoFallback/);
assert.match(editor, /CompressArchive/);

assert.match(interop, /async function buildPublisherStructuredSite/);
assert.match(interop, /buildPublisherSingleHtml\(options\.mode, title\)/);
assert.match(interop, /css\/site\.css/);
assert.match(interop, /js\/publisher-runtime\.js/);
assert.match(interop, /assets\/images/);
assert.match(interop, /assets\/video/);
assert.match(interop, /structuredBlobHash/);
assert.match(interop, /crypto\.subtle\.digest\('SHA-256'/);
assert.match(interop, /image\/png/);
assert.match(interop, /image\/webp/);
assert.match(interop, /image\/avif/);
assert.match(interop, /video\/webm;codecs=vp9,opus/);
assert.match(interop, /captureStream/);
assert.match(interop, /data-publisher-original-src/);
assert.match(interop, /publisherOriginalSrc/);
assert.match(interop, /CompressionStream\('deflate-raw'\)/);
assert.match(interop, /createZip\(result\.files/);
assert.match(interop, /publisherstudio-export\.json/);
assert.match(interop, /README\.txt/);
assert.match(interop, /exportStructuredWebsite\(fileName, title, options/);
assert.match(css, /PublisherStudio v1\.0\.74: structured website export/);

assert.match(doctrine, /Base64 overhead/);
assert.match(doctrine, /Preserve source/);
assert.match(doctrine, /PNG/);
assert.match(doctrine, /WebP/);
assert.match(doctrine, /AVIF/);
assert.match(doctrine, /WebM/);
assert.match(doctrine, /FFV1/);
assert.match(doctrine, /FFmpeg\.wasm/);
assert.match(doctrine, /Blazor.*JavaScript interop/is);

assert.equal(packageJson.version, '1.0.75');
assert.equal(lockJson.version, '1.0.75');
assert.equal(lockJson.packages[''].version, '1.0.75');
assert.match(webProject, /<Version>1\.0\.75<\/Version>/);
assert.match(installerProject, /<Version>1\.0\.75<\/Version>/);
assert.match(streamingRuntime, /Version = "1\.0\.75"/);

console.log('structured offline website, media externalization, browser-safe optimization, fallback, and version contracts passed');
