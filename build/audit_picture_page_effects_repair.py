#!/usr/bin/env python3
"""Static source audit for PublisherStudio 2.6.2 picture/page-effect maintenance boundary."""
from pathlib import Path
import sys
root=Path(__file__).resolve().parents[1]

def text(rel):
    p=root/rel
    if not p.exists(): raise AssertionError(f"missing {rel}")
    return p.read_text(encoding="utf-8")

def require(rel,*needles):
    value=text(rel)
    missing=[needle for needle in needles if needle not in value]
    if missing: raise AssertionError(f"{rel} missing: {', '.join(missing)}")

try:
    require('src/PublisherStudio.Web/BusinessObjects/PictureStudioModels.cs',
            'FormatVersion { get; set; } = "1.5"', 'PictureRasterColorizeMode',
            'ColorizeSourceColor', 'ColorizeTargetColor', 'BrushPath', 'EraserPath',
            'PictureEditorResult(string DataUrl, PictureDocument? SourceDocument, string Name, bool PreserveLayers)')
    require('src/PublisherStudio.Web/BusinessObjects/PublicationModels.cs',
            'FormatVersion { get; set; } = "1.58"', 'List<PublicationPageEffectLayer> EffectLayers',
            'PublicationPageEffectPlacement', 'PublicationPageEffectBlendMode', 'AnimationEnabled')
    require('src/PublisherStudio.Web/Components/Editor/PictureEditor.razor',
            'White / light → red', 'Apply layered', 'Apply merged', 'Download layered SVG',
            'Brush path', 'Eraser path')
    require('src/PublisherStudio.Web/Components/Editor/PictureEditor.razor.cs',
            'RasterWhiteToRed', 'ApplyLayered()', 'ApplyMerged()', '_pictureExportPreserveLayers',
            'PictureDrawTool.BrushPath', 'PictureDrawTool.EraserPath')
    require('src/PublisherStudio.Web/wwwroot/js/pictureStudioInterop.js',
            'rasterColorizeModes', 'applyRasterColorize', 'replacecolor', 'luminosity',
            'brushpath', 'eraserpath', 'data-publisherstudio-picture="1.5"')
    require('src/PublisherStudio.Web/Components/Editor/PageEffectLayerRenderer.razor',
            'publication-page-effect', 'page-effect-from', 'page-effect-to', '--page-effect-direction:', 'PublicationAnimationEasing.BounceOut')
    require('src/PublisherStudio.Web/Components/Editor/PageEffectStudio.razor',
            'Page appearance & effects', 'Custom page color', 'Animate from → to')
    require('src/PublisherStudio.Web/Components/Editor/PageSurface.razor',
            'PageEffectLayerRenderer Page="State.CurrentPage" Placement="PublicationPageEffectPlacement.Background"',
            'PageEffectLayerRenderer Page="State.CurrentPage" Placement="PublicationPageEffectPlacement.Overlay"')
    require('src/PublisherStudio.Web/Components/Editor/PrintPublication.razor',
            'PageEffectLayerRenderer Page="publicationPage" Placement="PublicationPageEffectPlacement.Background"',
            'PageEffectLayerRenderer Page="publicationPage" Placement="PublicationPageEffectPlacement.Overlay"')
    require('src/PublisherStudio.Web/Components/Pages/Editor.razor',
            'Export Picture Studio object', 'ExportSelectedLayeredChoice', 'ExportSelectedMergedChoice',
            'result.PreserveLayers ? result.SourceDocument : null')
    require('src/PublisherStudio.Web/wwwroot/js/publisherInterop.js',
            "querySelectorAll('[data-publication-element], .publication-page-effect')",
            'image.data[row + x * 4 + 3] <= 1', 'freezePageEffectsForRaster')
    require('src/PublisherStudio.Web/appsettings.json',
            '"PublicationFormatVersion": "1.58"', '"PictureFormatVersion": "1.5"')
    require('src/PublisherStudio.Web/Localization/de-DE.json',
            '"Text.White␠/␠light␠→␠red": "Weiß / hell → rot"',
            '"Text.Page␠appearance␠&␠effects": "Seitendarstellung & Effekte"')
    require('src/PublisherStudio.Web/PublisherStudio.Web.csproj','<Version>2.6.7</Version>')
    require('src/PublisherStudio.InstallerConsole/PublisherStudio.InstallerConsole.csproj','<Version>2.6.7</Version>')
    print('PublisherStudio 2.6.2 picture/page-effect source audit passed: recolor, paint paths, layered/merged application/export, alpha-safe selected export, page color/effect layers, animation, and format/version wiring are present.')
except AssertionError as exc:
    print(f'PublisherStudio 2.6.2 picture/page-effect source audit failed: {exc}', file=sys.stderr)
    sys.exit(1)
