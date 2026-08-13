using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Components.Editor;

/// <summary>
/// Represents the outcome of spreadsheet editor, carrying the data and status produced by the corresponding application operation.
/// </summary>
/// <param name="Content">Content value supplied to the spreadsheet editor operation and used when producing its result.</param>
/// <param name="FileName">File name value supplied to the spreadsheet editor operation and used when producing its result.</param>
/// <param name="StorageFormat">Storage format value supplied to the spreadsheet editor operation and used when producing its result.</param>
/// <param name="PreviewHtml">Preview html value supplied to the spreadsheet editor operation and used when producing its result.</param>
/// <param name="ActiveSheetName">Active sheet name value supplied to the spreadsheet editor operation and used when producing its result.</param>
public sealed record SpreadsheetEditorResult(
    byte[] Content,
    string FileName,
    SpreadsheetStorageFormat StorageFormat,
    string PreviewHtml,
    string ActiveSheetName);

/// <summary>
/// Represents a spreadsheet data selection application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class SpreadsheetDataSelection
{
    /// <summary>
    /// Gets or sets the sheet name value that forms part of the spreadsheet data selection state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sheet name value exposed by <see cref="SpreadsheetDataSelection"/>.</value>
    public string SheetName { get; set; } = "Sheet1";
    /// <summary>
    /// Gets or sets the range address that identifies the network or application endpoint associated with this spreadsheet data selection state.
    /// </summary>
    /// <value>The range address value exposed by <see cref="SpreadsheetDataSelection"/>.</value>
    public string RangeAddress { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the rows collection maintained or exposed by this spreadsheet data selection instance for downstream processing.
    /// </summary>
    /// <value>The rows value exposed by <see cref="SpreadsheetDataSelection"/>.</value>
    public List<List<string>> Rows { get; set; } = [];
}

/// <summary>
/// Represents the outcome of spreadsheet data object, carrying the data and status produced by the corresponding application operation.
/// </summary>
public sealed class SpreadsheetDataObjectResult
{
    /// <summary>
    /// Gets or sets the name value that forms part of the spreadsheet data object state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="SpreadsheetDataObjectResult"/>.</value>
    public string Name { get; set; } = "Spreadsheet Data";
    /// <summary>
    /// Gets or sets the workbook file name used by this spreadsheet data object instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The workbook file name value exposed by <see cref="SpreadsheetDataObjectResult"/>.</value>
    public string WorkbookFileName { get; set; } = "Spreadsheet.xlsx";
    /// <summary>
    /// Gets or sets the sheet name value that forms part of the spreadsheet data object state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sheet name value exposed by <see cref="SpreadsheetDataObjectResult"/>.</value>
    public string SheetName { get; set; } = "Sheet1";
    /// <summary>
    /// Gets or sets the range address that identifies the network or application endpoint associated with this spreadsheet data object state.
    /// </summary>
    /// <value>The range address value exposed by <see cref="SpreadsheetDataObjectResult"/>.</value>
    public string RangeAddress { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether first row contains headers applies to the spreadsheet data object state.
    /// </summary>
    /// <value>The first row contains headers value exposed by <see cref="SpreadsheetDataObjectResult"/>.</value>
    public bool FirstRowContainsHeaders { get; set; }
    /// <summary>
    /// Gets or sets the column names collection maintained or exposed by this spreadsheet data object instance for downstream processing.
    /// </summary>
    /// <value>The column names value exposed by <see cref="SpreadsheetDataObjectResult"/>.</value>
    public List<string> ColumnNames { get; set; } = [];
    /// <summary>Gets or sets the typed column schema selected or inferred while creating the data object.</summary>
    /// <value>The typed publication-data columns.</value>
    public List<PublicationDataColumn> Columns { get; set; } = [];
    /// <summary>
    /// Gets or sets the rows collection maintained or exposed by this spreadsheet data object instance for downstream processing.
    /// </summary>
    /// <value>The rows value exposed by <see cref="SpreadsheetDataObjectResult"/>.</value>
    public List<List<string>> Rows { get; set; } = [];
}
