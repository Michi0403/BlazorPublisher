using DevExpress.Spreadsheet;

namespace PublisherStudio.Models;

/// <summary>
/// Represents a spreadsheet editor view model.
/// </summary>
public sealed class SpreadsheetEditorViewModel
{
    /// <summary>
    /// Gets or sets session identifier.
    /// </summary>
    public Guid SessionId { get; init; }
    /// <summary>
    /// Gets or sets document identifier.
    /// </summary>
    public string DocumentId { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets file name.
    /// </summary>
    public string FileName { get; init; } = "Spreadsheet.xlsx";
    /// <summary>
    /// Gets or sets content.
    /// </summary>
    public byte[] Content { get; init; } = [];
    /// <summary>
    /// Gets or sets document format.
    /// </summary>
    public DocumentFormat DocumentFormat { get; init; } = DocumentFormat.Xlsx;
    /// <summary>
    /// Gets content accessor.
    /// </summary>
    public Func<byte[]> ContentAccessor => () => Content.ToArray();
}
