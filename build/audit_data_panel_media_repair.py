#!/usr/bin/env python3
"""Static source audit for the 2.6.0 data/panel/media maintenance boundary."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]

def read(rel: str) -> str:
    path = ROOT / rel
    if not path.is_file():
        raise AssertionError(f"missing {rel}")
    return path.read_text(encoding="utf-8")

def require(rel: str, *tokens: str) -> None:
    text = read(rel)
    missing = [token for token in tokens if token not in text]
    if missing:
        raise AssertionError(f"{rel} missing: {', '.join(missing)}")

try:
    require("src/PublisherStudio.Web/BusinessObjects/PublicationDataModels.cs",
            "ValueKindExplicit", "PublicationDataValidationSeverity", "PublicationDataValidationResult")
    require("src/PublisherStudio.Web/Services/PublicationDataService.cs",
            "TryParseNumber", "UnicodeCategory.CurrencySymbol", "ValueKindExplicit",
            "public PublicationDataValidationResult Validate", "SetColumnKind", "SetCellValue",
            "AddRow", "RemoveRow", "AddColumn", "RemoveColumn", "EnsureManagedSnapshot", "WriteManagedSnapshot")
    require("src/PublisherStudio.Web/Components/Editor/SpreadsheetEditor.razor",
            "Column names and data types", "_selectionColumnTypeKeys", "SelectionValidation",
            "PublicationDataValidationSeverity.Success", "InvokeAsync(() =>")
    require("src/PublisherStudio.Web/Components/Editor/DataManager.razor",
            "Schema &amp; row maintenance", "Detach current rows for editing", "ChangeColumnKind",
            "ChangeCell", "RemoveRow", "RemoveColumn", "CurrentValidation")
    require("src/PublisherStudio.Web/Components/Editor/PanelStudio.razor",
            "Preview size", "_previewPresets", "InvokeAsync(() => Saved.InvokeAsync(committed))")
    require("src/PublisherStudio.Web/Components/Editor/PanelView.razor", "data-panel-canvas-region", "--panel-canvas-aspect")
    require("src/PublisherStudio.Web/Components/Editor/DevExtremeComponentEditor.razor",
            "Data color / group field", "component-preview-size", "Available space")
    require("src/PublisherStudio.Web/wwwroot/js/componentRuntime.js", "dataDrivenColor", "vectorColorField")
    require("src/PublisherStudio.Web/wwwroot/js/publisherInterop.js",
            "shouldIgnoreHtml2CanvasCloneElement", "ignoreElements", "namedMediaDragStart",
            "DownloadURL", "duplicateSuffixPattern", "compressArchive", "jpegQuality")
    require("src/PublisherStudio.Web/Components/Editor/SpreadsheetEditor.razor",
            "catch (JSDisconnectedException", "catch (TaskCanceledException", "catch (ObjectDisposedException")
    require("src/PublisherStudio.Web/Services/Panels/PanelDocumentService.cs",
            "Width = 120", "Height = 67.5", "CanvasWidth = 160", "CanvasHeight = 90",
            "LayoutMode = PublicationPanelLayoutMode.FixedCanvas")
    require("src/PublisherStudio.Web/BusinessObjects/PublicationModels.cs", 'FormatVersion { get; set; } = "1.58"', "RasterJpegQuality", "CompressPageArchives")
    print("PublisherStudio 2.6.0 data/panel/media source audit passed: typed data maintenance, validation UI, dispatcher safety, capture/disposal guards, named media drag-out, export compression controls, and aspect-safe previews are wired.")
except AssertionError as exc:
    print(f"PublisherStudio 2.6.0 data/panel/media source audit failed: {exc}", file=sys.stderr)
    sys.exit(1)
