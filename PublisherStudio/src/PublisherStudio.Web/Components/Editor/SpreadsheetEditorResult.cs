using PublisherStudio.Domain;

namespace PublisherStudio.Components.Editor;

/// <summary>
/// The workbook payload returned by Spreadsheet Studio when a user applies an edit session.
/// Kept in a normal C# source file so both Razor components resolve the same public type.
/// </summary>
public sealed record SpreadsheetEditorResult(
    byte[] Content,
    string FileName,
    SpreadsheetStorageFormat StorageFormat,
    string PreviewHtml,
    string ActiveSheetName);
