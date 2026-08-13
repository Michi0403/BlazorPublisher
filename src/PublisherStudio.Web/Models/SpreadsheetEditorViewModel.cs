using DevExpress.Spreadsheet;

namespace PublisherStudio.Models;

/// <summary>
/// Represents spreadsheet editor view state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed class SpreadsheetEditorViewModel
{
    /// <summary>
    /// Gets or sets the stable session identifier used to identify or correlate this spreadsheet editor view instance with related application state.
    /// </summary>
    /// <value>The session identifier value exposed by <see cref="SpreadsheetEditorViewModel"/>.</value>
    public Guid SessionId { get; init; }
    /// <summary>
    /// Gets or sets the stable document identifier used to identify or correlate this spreadsheet editor view instance with related application state.
    /// </summary>
    /// <value>The document identifier value exposed by <see cref="SpreadsheetEditorViewModel"/>.</value>
    public string DocumentId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the file name used by this spreadsheet editor view instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The file name value exposed by <see cref="SpreadsheetEditorViewModel"/>.</value>
    public string FileName { get; init; } = "Spreadsheet.xlsx";
    /// <summary>
    /// Gets or sets the content value that forms part of the spreadsheet editor view state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content value exposed by <see cref="SpreadsheetEditorViewModel"/>.</value>
    public byte[] Content { get; init; } = [];
    /// <summary>
    /// Gets or sets the document format value that forms part of the spreadsheet editor view state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The document format value exposed by <see cref="SpreadsheetEditorViewModel"/>.</value>
    public DocumentFormat DocumentFormat { get; init; } = DocumentFormat.Xlsx;
    /// <summary>
    /// Gets the content accessor value that forms part of the spreadsheet editor view state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The content accessor value exposed by <see cref="SpreadsheetEditorViewModel"/>.</value>
    public Func<byte[]> ContentAccessor => () => Content.ToArray();
}
