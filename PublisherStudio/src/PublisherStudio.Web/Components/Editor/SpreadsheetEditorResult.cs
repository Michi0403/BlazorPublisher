using PublisherStudio.Domain;

namespace PublisherStudio.Components.Editor;

public sealed record SpreadsheetEditorResult(
    byte[] Content,
    string FileName,
    SpreadsheetStorageFormat StorageFormat,
    string PreviewHtml,
    string ActiveSheetName);

public sealed class SpreadsheetDataSelection
{
    public string SheetName { get; set; } = "Sheet1";
    public string RangeAddress { get; set; } = string.Empty;
    public List<List<string>> Rows { get; set; } = [];
}

public sealed class SpreadsheetDataObjectResult
{
    public string Name { get; set; } = "Spreadsheet Data";
    public string WorkbookFileName { get; set; } = "Spreadsheet.xlsx";
    public string SheetName { get; set; } = "Sheet1";
    public string RangeAddress { get; set; } = string.Empty;
    public bool FirstRowContainsHeaders { get; set; }
    public List<string> ColumnNames { get; set; } = [];
    public List<List<string>> Rows { get; set; } = [];
}
