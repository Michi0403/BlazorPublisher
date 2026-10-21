using System.Net;
using System.Text;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>
/// Applies results of an explicit, locally initiated AI Assist operation to the
/// original document through the normal undoable EditorStateService operations.
/// Unsolicited 1-Wire proposals never receive an insertion intent.
/// </summary>
/// <param name="editor">Original scoped publication state and undo operations.</param>
/// <param name="richText">Native OpenXML story creator.</param>
/// <param name="spreadsheets">Native spreadsheet parser and validator.</param>
/// <param name="logger">Structured diagnostics for insertion and skipped proposals.</param>
public sealed class PublisherAiDocumentInsertionService(
    EditorStateService editor,
    RichTextDocumentFactory richText,
    SpreadsheetDocumentService spreadsheets,
    ILogger<PublisherAiDocumentInsertionService> logger)
{
    private sealed record PendingInsertion(Guid DocumentId, Guid PageId, Guid? ElementId, string Kind);
    private readonly Dictionary<string, PendingInsertion> pending = new(StringComparer.Ordinal);

    /// <summary>Records the actual local document and selection before the user sends a Council request.</summary>
    /// <param name="target">Opaque result correlation token from the initiating user action.</param>
    /// <param name="kind">The native document format selected by the user.</param>
    public void Authorize(string target, string kind)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(target)) throw new ArgumentException("An insertion target is required.", nameof(target));
            pending[target] = new PendingInsertion(editor.Document.Id, editor.CurrentPage.Id,
                editor.SelectedElement?.Id, kind);
            logger.LogInformation("Authorized locally initiated AI Assist result for {Kind}.", kind);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not authorize AI Assist insertion intent.");
            throw;
        }
    }

    /// <summary>Withdraws an intent when the associated request could not be sent.</summary>
    /// <param name="target">Opaque correlation token to invalidate.</param>
    public void Cancel(string target)
    {
        try { pending.Remove(target); }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not cancel AI Assist insertion intent.");
            throw;
        }
    }

    /// <summary>
    /// Applies only a correlated local intent. Keeps proposals reviewable when the
    /// active document/page changed or the intended element is missing/locked.
    /// </summary>
    /// <param name="proposal">A text proposal received through the approved 1-Wire function.</param>
    /// <returns>True only when the exact matching user-authorized result was committed.</returns>
    public bool TryApply(OrganicTextInsertionProposal proposal)
    {
        try
        {
            if (!pending.TryGetValue(proposal.Target, out var intent)) return false;
            if (editor.Document.Id != intent.DocumentId || editor.CurrentPage.Id != intent.PageId)
            {
                logger.LogWarning("AI Assist result {ProposalId} was not inserted: document or page changed.", proposal.Id);
                return false;
            }
            if (string.IsNullOrWhiteSpace(proposal.Text)) return false;
            if (intent.Kind is not ("writing" or "spreadsheet")) return false;
            if (intent.ElementId is { } elementId)
            {
                var original = editor.CurrentPage.Elements.FirstOrDefault(item => item.Id == elementId);
                if (original is null || original.Locked)
                {
                    logger.LogWarning("AI Assist result {ProposalId} was not inserted: target unavailable.", proposal.Id);
                    return false;
                }
                editor.SetPrimarySelection(elementId);
            }
            if (string.Equals(intent.Kind, "spreadsheet", StringComparison.OrdinalIgnoreCase))
            {
                var csv = NormalizeCsv(proposal.Text);
                var bytes = Encoding.UTF8.GetBytes(csv);
                spreadsheets.ValidateWorkbookContent(bytes, SpreadsheetStorageFormat.Csv);
                var preview = spreadsheets.RenderPreviewHtml(bytes, SpreadsheetStorageFormat.Csv, out var sheetName);
                if (intent.ElementId.HasValue && editor.SelectedElement is SpreadsheetElement)
                    editor.UpdateSpreadsheetDocument(bytes, "AI spreadsheet.csv", SpreadsheetStorageFormat.Csv, preview, sheetName);
                else
                    editor.AddSpreadsheet(bytes, "AI spreadsheet.csv", SpreadsheetStorageFormat.Csv);
            }
            else if (string.Equals(intent.Kind, "writing", StringComparison.OrdinalIgnoreCase))
            {
                var text = proposal.Text;
                // Create/validate the document bytes before mutating the publication.
                var content = richText.CreateOpenXmlFromPlainText(text);
                var preview = "<p>" + WebUtility.HtmlEncode(text).Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", "<br/>", StringComparison.Ordinal) + "</p>";
                var background = editor.SelectedElement is TextFrameElement existing ? existing.DocumentBackground : "transparent";
                if (intent.ElementId is null || editor.SelectedElement is not TextFrameElement)
                    editor.AddText();
                editor.UpdateTextDocument(content, preview, background);
            }
            else return false;
            pending.Remove(proposal.Target);
            logger.LogInformation("AI Assist proposal {ProposalId} inserted into publication through editor history.", proposal.Id);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "AI Assist proposal {ProposalId} could not be inserted.", proposal.Id);
            // The proposal and intent remain available for review/retry; never mark an unsuccessful insert complete.
            return false;
        }
    }

    /// <summary>Normalizes CSV or an ordinary Markdown table to a CSV document, without running AI-supplied code.</summary>
    /// <param name="value">Raw proposed tabular content.</param>
    /// <returns>CSV document text for the existing spreadsheet validator.</returns>
    private string NormalizeCsv(string value)
    {
        try
        {
            var trimmed = value.Trim();
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                var line = trimmed.IndexOf('\n');
                var last = trimmed.LastIndexOf("```", StringComparison.Ordinal);
                if (line >= 0 && last > line) trimmed = trimmed[(line + 1)..last].Trim();
            }
            var lines = trimmed.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            if (lines.Length > 1 && lines.All(line => line.TrimStart().StartsWith('|')))
            {
                var rows = lines.Select(line => line.Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToArray())
                    .Where(cells => !cells.All(cell => cell.Length > 0 && cell.All(c => c is '-' or ':' or ' ')))
                    .Select(cells => string.Join(",", cells.Select(cell => "\"" + cell.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"")));
                trimmed = string.Join("\n", rows);
            }
            if (string.IsNullOrWhiteSpace(trimmed)) throw new InvalidDataException("AI spreadsheet result was empty.");
            return trimmed;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not normalize AI spreadsheet output.");
            throw;
        }
    }
}
