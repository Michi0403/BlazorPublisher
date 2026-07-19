using DevExpress.Spreadsheet;

namespace PublisherStudio.Models;

public sealed class SpreadsheetEditorViewModel
{
    public Guid SessionId { get; init; }
    public string DocumentId { get; init; } = string.Empty;
    public string FileName { get; init; } = "Spreadsheet.xlsx";
    public byte[] Content { get; init; } = [];
    public DocumentFormat DocumentFormat { get; init; } = DocumentFormat.Xlsx;
    public Func<byte[]> ContentAccessor => () => Content.ToArray();
}
