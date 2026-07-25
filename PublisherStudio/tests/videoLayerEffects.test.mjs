import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = (...parts) => fs.readFileSync(path.join(root, ...parts), 'utf8');

const mediaModels = read('src', 'PublisherStudio.Web', 'Domain', 'PublicationMediaModels.cs');
const streamingModels = read('src', 'PublisherStudio.Web', 'Domain', 'PublicationStreamingModels.cs');
const timeline = read('src', 'PublisherStudio.Web', 'Services', 'MediaStudio', 'UseCases', 'MediaTimelineEditService.cs');
const studio = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'MediaStudio.razor');
const interop = read('src', 'PublisherStudio.Web', 'wwwroot', 'js', 'mediaStudioInterop.js');
const effects = read('src', 'PublisherStudio.Web', 'wwwroot', 'js', 'videoEffectRuntime.js');
const streaming = read('src', 'PublisherStudio.Web', 'wwwroot', 'js', 'streamingInterop.js');
const liveSource = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'LiveSourceView.razor');
const inspector = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'InspectorPanel.razor');
const videoView = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'VideoMediaView.razor');
const pageSurface = read('src', 'PublisherStudio.Web', 'Components', 'Editor', 'PageSurface.razor');
const files = read('src', 'PublisherStudio.Web', 'Services', 'PublicationFileService.cs');
const app = read('src', 'PublisherStudio.Web', 'Components', 'App.razor');
const css = read('src', 'PublisherStudio.Web', 'wwwroot', 'css', 'site.css');

assert.match(mediaModels, /List<MediaTemporalSection> CutSections/);
assert.match(mediaModels, /bool HasTemporalSelection/);
assert.match(studio, /SaveSelectedRangeAsCutSection/);
assert.match(studio, /SelectedCutSections/);
assert.match(studio, /VideoTimeSelectionCommitted[\s\S]*CommitSelectedSegment\(\)/);
assert.match(timeline, /NormalizeCutSections\(segment\)/);
assert.match(timeline, /Take\(128\)/);

assert.match(mediaModels, /class VideoEffectLayer/);
assert.match(mediaModels, /VideoFrameRegion Region/);
assert.match(mediaModels, /List<VideoEffectFilter> Filters/);
assert.match(mediaModels, /ChromaKey/);
assert.match(studio, /Video layers/);
assert.match(studio, /AddVideoLayer/);
assert.match(studio, /MoveVideoLayer/);
assert.match(studio, /CommitFrameRegionToSelectedLayer/);
assert.match(studio, /VideoFramePointCommitted/);
assert.match(interop, /VideoFramePointCommitted/);
assert.match(interop, /configureVideoEffects/);

assert.match(effects, /function applyChroma/);
assert.match(effects, /function applyVignette/);
assert.match(effects, /function applyGrain/);
assert.match(effects, /function installById/);
assert.match(effects, /globalCompositeOperation = layer\.blendMode/);
assert.match(app, /videoEffectRuntime\.js/);
assert.match(videoView, /publisherVideoEffects\.installById/);
assert.match(videoView, /segment\.VideoLayers/);
assert.match(pageSurface, /<VideoMediaView/);
assert.match(css, /publication-video-effect-canvas\.active/);

assert.match(streamingModels, /List<VideoEffectLayer> VideoLayers/);
assert.match(timeline, /SynchronizeLiveSourceLayer/);
assert.match(timeline, /Live input controls/);
assert.match(inspector, /TimelineEdits\.SynchronizeLiveSourceLayer/);
assert.match(inspector, /Layered streaming effects/);
assert.match(inspector, /AddLiveEffectLayer/);
assert.match(inspector, /AddLiveEffectFilter/);
assert.match(inspector, /Enum\.GetValues<VideoEffectFilterKind>/);
assert.match(inspector, /ChangeNumberLive[\s\S]*SynchronizeLiveSourceLayer\(live\)/);
assert.match(inspector, /ChangeColor[\s\S]*SynchronizeLiveSourceLayer\(live\)/);
assert.match(liveSource, /videoLayers = VideoLayerPayload\(\)/);
assert.match(streaming, /legacyVideoLayers/);
assert.match(streaming, /publisherVideoEffects\.install/);
assert.match(streaming, /updateSourceEffects/);

assert.match(files, /_mediaTimeline\.Normalize\(media\.Segments, media is VideoElement\)/);
assert.match(files, /Migrate it into/);
assert.match(files, /SynchronizeLiveSourceLayer\(source\)/);
assert.match(files, /document\.FormatVersion = "1\.54"/);

console.log('PublisherStudio persistent selections, multiple cut sections, layered video filters, chroma key, editable regions, Mainframe preview, and streaming input filter contracts passed.');
