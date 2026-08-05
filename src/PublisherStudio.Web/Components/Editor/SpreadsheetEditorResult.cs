using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Components.Editor;

/// <summary>
/// Represents a spreadsheet editor result.
/// </summary>
public sealed record SpreadsheetEditorResult(
    byte[] Content,
    string FileName,
    SpreadsheetStorageFormat StorageFormat,
    string PreviewHtml,
    string ActiveSheetName);

/// <summary>
/// Represents a spreadsheet data selection.
/// </summary>
public sealed class SpreadsheetDataSelection
{
    /// <summary>
    /// Gets or sets sheet name.
    /// </summary>
    public string SheetName { get; set; } = "Sheet1";
    /// <summary>
    /// Gets or sets range address.
    /// </summary>
    public string RangeAddress { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets rows.
    /// </summary>
    public List<List<string>> Rows { get; set; } = [];
}

/// <summary>
/// Represents a spreadsheet data object result.
/// </summary>
public sealed class SpreadsheetDataObjectResult
{
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "Spreadsheet Data";
    /// <summary>
    /// Gets or sets workbook file name.
    /// </summary>
    public string WorkbookFileName { get; set; } = "Spreadsheet.xlsx";
    /// <summary>
    /// Gets or sets sheet name.
    /// </summary>
    public string SheetName { get; set; } = "Sheet1";
    /// <summary>
    /// Gets or sets range address.
    /// </summary>
    public string RangeAddress { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets first row contains headers.
    /// </summary>
    public bool FirstRowContainsHeaders { get; set; }
    /// <summary>
    /// Gets or sets column names.
    /// </summary>
    public List<string> ColumnNames { get; set; } = [];
    /// <summary>
    /// Gets or sets rows.
    /// </summary>
    public List<List<string>> Rows { get; set; } = [];
}
