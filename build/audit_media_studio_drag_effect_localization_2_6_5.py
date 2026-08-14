#!/usr/bin/env python3
"""Static regression audit for PublisherStudio 2.6.5 media-studio integration work."""
from pathlib import Path
import sys
root = Path(__file__).resolve().parents[1]

def text(rel):
    p = root / rel
    if not p.is_file():
        raise AssertionError(f"missing {rel}")
    return p.read_text(encoding="utf-8")

def require(rel, *needles):
    value = text(rel)
    missing = [needle for needle in needles if needle not in value]
    if missing:
        raise AssertionError(f"{rel} missing: {', '.join(missing)}")

try:
    require('src/PublisherStudio.Web/Components/Pages/Editor.razor',
            'ExportPictureStudioMergedSelection',
            'downloadPictureStudioMergedSvg',
            'downloadPictureStudioSvg',
            './js/pictureStudioInterop.js?v=2.6.5')
    require('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js',
            'configureStudioDragTransfer',
            'readStudioDragTransfer',
            'studioMediaDescriptorFromTransfer',
            'fileFromStudioDragTransfer')
    require('src/PublisherStudio.Web/Components/Editor/PictureEditor.razor',
            'data-picture-layer-id', 'draggable="true"')
    require('src/PublisherStudio.Web/Components/Editor/PictureEditor.razor.cs',
            'PictureStudioLayerDropped', 'MoveLayerToIndex', './js/pictureStudioInterop.js?v=2.6.5')
    require('src/PublisherStudio.Web/wwwroot/js/pictureStudioInterop.js',
            "internalKind: 'picture-layer'", 'PictureStudioLayerDropped',
            'pointerover', 'fileFromStudioDragTransfer')
    require('src/PublisherStudio.Web/Components/Editor/MediaStudio.razor',
            '<DxRibbonTab Text=\'@LT("Home")\'>',
            '<DxRibbonTab Text=\'@LT("Edit")\'>',
            '<DxRibbonTab Text=\'@LT("Layers")\'>',
            '<DxRibbonTab Text=\'@LT("Effects")\'>',
            '<DxRibbonTab Text=\'@LT("Output")\'>',
            'data-media-segment-id', 'data-video-layer-id', 'data-video-filter-id',
            'MediaStudioTimelineSegmentDropped', 'MediaStudioVideoLayerDropped', 'MediaStudioVideoFilterDropped',
            './js/mediaStudioInterop.js?v=2.6.8')
    require('src/PublisherStudio.Web/Services/MediaStudio/UseCases/MediaTimelineEditService.cs',
            'MoveAt(List<PublicationMediaSegment>', 'InsertAt(')
    require('src/PublisherStudio.Web/wwwroot/js/mediaStudioInterop.js',
            'finiteMediaTime', 'Number.isFinite(duration)',
            "internalKind: 'media-segment'", "internalKind: 'video-effect-layer'", "internalKind: 'video-effect-filter'",
            'MediaStudioDropTimelinePointSelected')
    require('src/PublisherStudio.Web/wwwroot/js/videoEffectRuntime.js',
            'enumRuntimeName', 'filterKindNames', "case 'blur'", 'applyChroma', 'applyVignette',
            'willReadFrequently: true', 'requestRender', 'const side = context.createLinearGradient', 'drawBlobDepth')
    require('src/PublisherStudio.Web/Services/VideoStudio/Export/BrowserRuntimeTemplateService.cs',
            'const side=ctx.createLinearGradient', 'front=pixels(points),back=pixels', 'const depth=')
    require('src/PublisherStudio.Web/Services/Configuration/IApplicationConfigurationServices.cs',
            'string GetText(string englishText, string? culture = null);')
    require('src/PublisherStudio.Web/Services/Configuration/FileLocalizationService.cs',
            '_englishKeysByText', 'public string GetText(string englishText, string? culture = null)')
    require('src/PublisherStudio.Web/Components/Editor/PublicationRibbon.razor', 'IFileLocalizationService Localization', 'private string LT(')
    require('src/PublisherStudio.Web/Components/Editor/InspectorPanel.razor', 'KindIconCss', 'private string LT(')
    require('src/PublisherStudio.Web/wwwroot/css/site.css',
            'font-family: "Segoe UI Symbol", "Noto Sans Symbols 2", "DejaVu Sans", sans-serif;',
            'v2.6.5 shared studio drag/layer affordances')
    require('src/PublisherStudio.Web/Components/App.razor',
            'videoEffectRuntime.js?v=2.6.5', 'publisherInterop.js?v=2.6.8')
    require('src/PublisherStudio.Web/PublisherStudio.Web.csproj', '<Version>2.6.8</Version>')
    require('src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj', '<Version>2.6.8</Version>')
    require('src/PublisherStudio.Web/BusinessObjects/PublicationModels.cs', 'FormatVersion { get; set; } = "1.58"')
    print('PublisherStudio 2.6.5 media-studio/drag/effect/localization source audit passed.')
except AssertionError as exc:
    print(f'PublisherStudio 2.6.5 media-studio/drag/effect/localization source audit failed: {exc}', file=sys.stderr)
    sys.exit(1)
