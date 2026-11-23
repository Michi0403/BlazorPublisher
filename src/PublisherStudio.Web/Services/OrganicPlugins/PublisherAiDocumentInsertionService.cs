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
/// <param name="organicOptions">Configured 1-Wire transport limits.</param>
/// <param name="logger">Structured diagnostics for insertion and skipped proposals.</param>
public sealed class PublisherAiDocumentInsertionService(
    EditorStateService editor,
    RichTextDocumentFactory richText,
    SpreadsheetDocumentService spreadsheets,
    Microsoft.Extensions.Options.IOptions<OrganicPluginOptions> organicOptions,
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

    /// <summary>
    /// Builds a snapshot of this editor session (publication, pages and the spreadsheet/text objects) for the 1-Wire session list.
    /// Must be called on the owning circuit's synchronization context.
    /// </summary>
    /// <param name="sessionId">Identifier under which the session is registered.</param>
    /// <returns>The current session snapshot.</returns>
    public PublisherEditorSessionSnapshot DescribeSession(Guid sessionId)
    {
        try
        {
            var document = editor.Document;
            var snapshot = new PublisherEditorSessionSnapshot
            {
                Id = sessionId,
                PublicationId = document.Id,
                Title = document.Name,
                PageCount = document.Pages.Count,
                CurrentPage = document.Pages.FindIndex(page => page.Id == editor.SelectedPageId) + 1
            };
            for (var pageIndex = 0; pageIndex < document.Pages.Count; pageIndex++)
            {
                foreach (var element in document.Pages[pageIndex].Elements)
                {
                    var kind = element is SpreadsheetElement ? "spreadsheet" : element is TextFrameElement ? "text" : string.Empty;
                    if (kind.Length == 0) continue;
                    snapshot.Objects.Add(new PublisherEditorObjectSnapshot { Id = element.Id, Kind = kind, Title = element.Name, Page = pageIndex + 1 });
                }
            }
            return snapshot;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not describe the editor session for the 1-Wire session list.");
            throw;
        }
    }

    /// <summary>
    /// Creates one new native spreadsheet or text object from an already approved 1-Wire request, using the same
    /// validators and undoable editor operations as the user-initiated AI Assist path. Must be called on the owning circuit's
    /// synchronization context. "visible" selects the new object (and its page) for the user; "silent" restores the previous page and selection.
    /// </summary>
    /// <param name="request">The approved creation request.</param>
    /// <returns>A result describing the created object, or the reason nothing was created.</returns>
    public PublisherDocumentCreateResult CreateDocument(PublisherDocumentCreateRequest request)
    {
        try
        {
            var kind = (request.Kind ?? string.Empty).Trim().ToLowerInvariant();
            var presentation = string.Equals((request.Presentation ?? string.Empty).Trim(), "silent", StringComparison.OrdinalIgnoreCase) ? "silent" : "visible";
            var pages = editor.Document.Pages;
            var targetIndex = request.TargetPage is { } requested ? requested - 1 : pages.FindIndex(page => page.Id == editor.SelectedPageId);
            if (kind is not ("spreadsheet" or "text" or "pdf"))
                return CreateFailure("kind must be 'spreadsheet', 'text', or 'pdf'.");
            if (string.IsNullOrWhiteSpace(request.Content))
                return CreateFailure("content is required.");
            if (targetIndex < 0 || targetIndex >= pages.Count)
                return CreateFailure($"The publication has {pages.Count} page(s); page {request.TargetPage} does not exist.");

            var title = string.IsNullOrWhiteSpace(request.Title) ? (kind == "spreadsheet" ? "AI spreadsheet" : "AI text") : request.Title.Trim();
            byte[] bytes = [];
            byte[] storyContent = [];
            var storyPreview = string.Empty;
            // Validate and build the document bytes before mutating the publication.
            if (kind == "spreadsheet")
            {
                var spreadsheetFormat = SpreadsheetStorageFormat.Xlsx;
                bytes = spreadsheets.CreateXlsxFromCsv(NormalizeCsv(request.Content));
                spreadsheets.ValidateWorkbookContent(bytes, spreadsheetFormat);
            }
            else
            {
                storyContent = richText.CreateOpenXmlFromPlainText(request.Content);
                storyPreview = "<p>" + WebUtility.HtmlEncode(request.Content).Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", "<br/>", StringComparison.Ordinal) + "</p>";
            }

            var documentBytes = kind switch
            {
                "spreadsheet" => bytes,
                "pdf" => richText.ExportPdf(storyContent),
                _ => storyContent
            };
            var wireLimit = Math.Min(OneWireProtocol.MaximumMessageBytes, organicOptions.Value.MaximumMessageBytes);
            if (documentBytes.Length > wireLimit / 4)
                return CreateFailure("The document exceeds the 1-Wire file budget. Reduce the content or review OrganicPlugins:MaximumMessageBytes.");

            var previousPageId = editor.SelectedPageId;
            var previousElementId = editor.SelectedElementId;
            if (pages[targetIndex].Id != editor.SelectedPageId) editor.SelectPage(pages[targetIndex].Id);
            PublicationElement created;
            if (kind == "spreadsheet")
            {
                var fileName = new string(title.Where(character => char.IsLetterOrDigit(character) || character is ' ' or '-' or '_').ToArray()).Trim();
                created = editor.AddSpreadsheet(bytes, (fileName.Length == 0 ? "AI spreadsheet" : fileName) + ".xlsx", SpreadsheetStorageFormat.Xlsx);
            }
            else
            {
                created = editor.AddText();
                editor.UpdateTextDocument(storyContent, storyPreview, "transparent");
            }
            created.Name = title;
            if (presentation == "silent")
            {
                if (previousPageId != editor.SelectedPageId) editor.SelectPage(previousPageId);
                editor.SelectElement(previousElementId);
            }
            logger.LogInformation("Approved 1-Wire document creation added a {Kind} object on page {Page} ({Presentation}).", kind, targetIndex + 1, presentation);
            return new PublisherDocumentCreateResult
            {
                Success = true, ObjectId = created.Id, Kind = kind, Title = title, Page = targetIndex + 1, Presentation = presentation,
                DocumentBytes = documentBytes,
                Message = presentation == "visible"
                    ? "Created and selected in the open publication. The native editor dialog is not opened automatically."
                    : "Created silently on the requested page; the user's page and selection were kept."
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Approved 1-Wire document creation failed.");
            return CreateFailure("The content was rejected or could not be inserted: " + exception.Message);
        }
    }

    /// <summary>Builds a failed creation result.</summary>
    /// <param name="message">Explanation of why nothing was created.</param>
    /// <returns>A failure result carrying the message.</returns>
    private PublisherDocumentCreateResult CreateFailure(string message)
    {
        try
        {
            return new PublisherDocumentCreateResult { Success = false, Message = message };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not build the document creation failure result.");
            throw;
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
