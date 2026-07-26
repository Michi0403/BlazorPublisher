import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

const models = read('src/PublisherStudio.Web/Domain/PublicationMediaModels.cs');
const publicationModels = read('src/PublisherStudio.Web/Domain/PublicationModels.cs');
const importer = read('src/PublisherStudio.Web/Services/VideoStudio/Import/VideoProjectImportService.cs');
const timeline = read('src/PublisherStudio.Web/Services/MediaStudio/UseCases/MediaTimelineEditService.cs');
const studio = read('src/PublisherStudio.Web/Components/Editor/MediaStudio.razor');
const editor = read('src/PublisherStudio.Web/Components/Pages/Editor.razor');
const persistence = read('src/PublisherStudio.Web/Services/PublicationFileService.cs');
const program = read('src/PublisherStudio.Web/Program.cs');
const applicationComposition = read('src/PublisherStudio.Web/PublisherStudioServiceCollectionExtensions.cs');
const interop = read('src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js');
const packageJson = JSON.parse(read('src/PublisherStudio.Web/package.json'));
const lockJson = JSON.parse(read('src/PublisherStudio.Web/package-lock.json'));
const webProject = read('src/PublisherStudio.Web/PublisherStudio.Web.csproj');
const installerProject = read('src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj');
const doctrine = read('docs/architecture/video-project-import-doctrine.md');

assert.match(models, /class VideoProjectDocument/);
assert.match(models, /class MediaTimelineTrack/);
assert.match(models, /class MediaTimelineTransition/);
assert.match(models, /class MediaSourceReference/);
assert.match(models, /MediaTimelineTrackKind \{ Video, Audio, Subtitle, Data \}/);
assert.match(models, /TimelineStartSeconds/);
assert.match(models, /TimelineDurationSeconds/);
assert.match(models, /SourceRate/);
assert.match(models, /Speed/);
assert.match(models, /ImportMetadata/);
assert.match(publicationModels, /VideoProjectDocument\? VideoProject/);
assert.match(publicationModels, /FormatVersion \{ get; set; \} = "1\.55"/);

assert.match(importer, /SupportedExtensions[\s\S]*\.otio[\s\S]*\.otioz[\s\S]*\.mlt[\s\S]*\.kdenlive[\s\S]*\.xges[\s\S]*\.osp[\s\S]*\.edl/);
assert.match(importer, /ImportOtioBundleAsync/);
assert.match(importer, /content\.otio/);
assert.match(importer, /ImportMlt/);
assert.match(importer, /ImportXges/);
assert.match(importer, /ImportOpenShot/);
assert.match(importer, /ImportEdl/);
assert.match(importer, /DtdProcessing = DtdProcessing\.Prohibit/);
assert.match(importer, /XmlResolver = null/);
assert.match(importer, /MaxArchiveEntries/);
assert.doesNotMatch(importer, /MaxArchiveUncompressedBytes|MaxInputBytes/, "Local-first project imports must not impose an application-defined byte ceiling.");
assert.match(importer, /NormalizeArchivePath/);
assert.match(importer, /PROJECT_MEDIA_RELINK_REQUIRED/);
assert.match(importer, /InterchangeIssueSeverity\.Loss/);
assert.doesNotMatch(importer, /HttpClient|WebRequest|WebClient/);

assert.match(timeline, /CloneVideoProject/);
assert.match(timeline, /CreateTrackProjection/);
assert.match(timeline, /ReplaceTrackProjection/);
assert.match(timeline, /segment\.IsGap/);
assert.match(timeline, /segment\.SourceReference/);

assert.match(studio, /@inject VideoProjectImportService VideoProjectImporter/);
assert.match(studio, /Import open project…/);
assert.match(studio, /Relink project media…/);
assert.match(studio, /media-studio-project-input/);
assert.match(studio, /media-studio-project-media-input/);
assert.match(studio, /LoadVideoProject/);
assert.match(studio, /RelinkVideoProjectMedia/);
assert.match(studio, /Editable video track/);
assert.match(studio, /Compatibility report/);
assert.match(studio, /CommitProjectTrack/);
assert.match(studio, /VideoProject = IsVideo/);
assert.match(editor, /video\.VideoProject = result\.VideoProject/);
assert.match(persistence, /video\.VideoProject is \{ Tracks\.Count: > 0 \}/);
assert.match(persistence, /document\.FormatVersion = "1\.55"/);
assert.match(applicationComposition, /AddSingleton<VideoProjectImportService, VideoProjectImportService>/);
assert.match(interop, /\(otio\|otioz\|mlt\|kdenlive\|xges\|osp\|edl\)/);
assert.match(interop, /actualKind === 'project'/);

assert.equal(packageJson.version, '2.0.1');
assert.equal(lockJson.version, '2.0.1');
assert.equal(lockJson.packages[''].version, '2.0.1');
assert.match(webProject, /<Version>2\.0\.1<\/Version>/);
assert.match(installerProject, /<Version>2\.0\.1<\/Version>/);

assert.match(doctrine, /OpenTimelineIO/);
assert.match(doctrine, /MLT XML/);
assert.match(doctrine, /Kdenlive/);
assert.match(doctrine, /Shotcut/);
assert.match(doctrine, /XGES/);
assert.match(doctrine, /OpenShot/);
assert.match(doctrine, /CMX 3600/);
assert.match(doctrine, /OBS Scene Collection/);
assert.match(doctrine, /Project format is not a media codec or container/);
assert.match(doctrine, /active video-track projection/);
assert.match(doctrine, /not full multitrack compositing/);

const packageRefs = [...webProject.matchAll(/<PackageReference Include="([^"]+)"/g)].map(match => match[1]).sort();
assert.deepEqual(packageRefs, [
  'DevExpress.AspNetCore.Spreadsheet',
  'DevExpress.Blazor',
  'DevExpress.Blazor.RichEdit',
  'DevExpress.Blazor.RichEdit.de',
  'DevExpress.Blazor.RichEdit.es',
  'DevExpress.Blazor.RichEdit.ja',
  'DevExpress.Blazor.de',
  'DevExpress.Blazor.es',
  'DevExpress.Blazor.ja',
  'LocalGPT.WireProtocolVersion'
]);

console.log('open video-project canonical model, safe adapters, relinking UI, project persistence, and doctrine contracts passed');
