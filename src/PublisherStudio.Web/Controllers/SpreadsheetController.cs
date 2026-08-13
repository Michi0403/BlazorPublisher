using DevExpress.AspNetCore.Spreadsheet;
using DevExpress.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Models;
using PublisherStudio.Services;

namespace PublisherStudio.Controllers;

/// <summary>
/// Exposes the spreadsheet application operations through PublisherStudio's web/API boundary and delegates domain work to the corresponding services.
/// </summary>
[Route("spreadsheet")]
[AutoValidateAntiforgeryToken]
public sealed class SpreadsheetController : Controller
{
    /// <summary>
    /// Stores the spreadsheet session store dependency used by <see cref="SpreadsheetController"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly SpreadsheetSessionStore _sessions;
    /// <summary>
    /// Stores the spreadsheet document service dependency used by <see cref="SpreadsheetController"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly SpreadsheetDocumentService _documents;

    /// <summary>
    /// Initializes a new <see cref="SpreadsheetController"/> instance and captures the dependencies or initial state required by its spreadsheet workflow.
    /// </summary>
    /// <param name="sessions">Spreadsheet session store dependency used by the spreadsheet workflow to provide the corresponding application capability.</param>
    /// <param name="documents">Spreadsheet document service dependency used by the spreadsheet workflow to provide the corresponding application capability.</param>
    public SpreadsheetController(SpreadsheetSessionStore sessions, SpreadsheetDocumentService documents)
    {
        _sessions = sessions;
        _documents = documents;
    }

    /// <summary>
    /// Returns the editor projection for the spreadsheet API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("editor/{sessionId:guid}")]
    [IgnoreAntiforgeryToken]
    public IActionResult Editor(Guid sessionId)
    {
        if (!_sessions.TryGet(sessionId, out var session)) return NotFound("Spreadsheet editing session expired.");
        return View(new SpreadsheetEditorViewModel
        {
            SessionId = session.Id,
            DocumentId = session.DocumentId,
            FileName = session.FileName,
            Content = session.Content,
            DocumentFormat = ToDevExpressFormat(session.SourceFormat)
        });
    }

    /// <summary>
    /// Returns the request handler projection for the spreadsheet API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [AcceptVerbs("GET", "POST")]
    [Route("request")]
    public IActionResult RequestHandler() => SpreadsheetRequestProcessor.GetResponse(HttpContext);

    /// <summary>
    /// Returns the open projection for the spreadsheet API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="workbook">Form file dependency used by the spreadsheet workflow to provide the corresponding application capability.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("open/{sessionId:guid}")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Open(Guid sessionId, IFormFile? workbook, CancellationToken cancellationToken)
    {
        if (!_sessions.TryGet(sessionId, out _))
            return NotFound(new { success = false, message = "Spreadsheet editing session expired." });
        if (workbook is null)
            return BadRequest(new { success = false, message = "Select a spreadsheet file to open." });
        if (workbook.Length <= 0)
            return BadRequest(new { success = false, message = "The selected spreadsheet is empty." });

        try
        {
            var format = _documents.DetectFormat(workbook.FileName, workbook.ContentType);
            await using var input = workbook.OpenReadStream();
            using var output = workbook.Length <= int.MaxValue
                ? new MemoryStream((int)workbook.Length)
                : new MemoryStream();
            await input.CopyToAsync(output, cancellationToken);
            var replaced = _sessions.Replace(sessionId, workbook.FileName, format, output.ToArray());
            return Ok(new
            {
                success = true,
                fileName = replaced.FileName,
                activeSheetName = replaced.ActiveSheetName,
                reloadUrl = Url.Action(nameof(Editor), new
                {
                    sessionId,
                    revision = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                })
            });
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or OperationCanceledException)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Returns the new projection for the spreadsheet API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("new/{sessionId:guid}")]
    public IActionResult New(Guid sessionId)
    {
        if (!_sessions.TryGet(sessionId, out _))
            return NotFound(new { success = false, message = "Spreadsheet editing session expired." });

        try
        {
            var replaced = _sessions.Replace(
                sessionId,
                "Spreadsheet.xlsx",
                SpreadsheetStorageFormat.Xlsx,
                _documents.CreateBlankXlsx());
            return Ok(new
            {
                success = true,
                fileName = replaced.FileName,
                activeSheetName = replaced.ActiveSheetName,
                reloadUrl = Url.Action(nameof(Editor), new
                {
                    sessionId,
                    revision = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                })
            });
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Returns the save projection for the spreadsheet API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <param name="spreadsheetState">Spreadsheet state value supplied to the spreadsheet operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("save/{sessionId:guid}")]
    public IActionResult Save(Guid sessionId, SpreadsheetClientState spreadsheetState)
    {
        if (!_sessions.TryGet(sessionId, out var existing)) return NotFound(new { success = false, message = "Spreadsheet editing session expired." });
        try
        {
            var clientDocumentId = SpreadsheetRequestProcessor.GetDocumentIdFromState(spreadsheetState);
            if (!string.Equals(clientDocumentId, existing.DocumentId, StringComparison.Ordinal))
                return BadRequest(new { success = false, message = "The spreadsheet state does not belong to this editing session." });

            var spreadsheet = SpreadsheetRequestProcessor.GetSpreadsheetFromState(spreadsheetState);
            var storageFormat = existing.SourceFormat == SpreadsheetStorageFormat.Xlsm
                ? SpreadsheetStorageFormat.Xlsm
                : SpreadsheetStorageFormat.Xlsx;
            var documentFormat = storageFormat == SpreadsheetStorageFormat.Xlsm ? DocumentFormat.Xlsm : DocumentFormat.Xlsx;
            var bytes = spreadsheet.SaveCopy(documentFormat);
            var sheetName = spreadsheet.Document.Worksheets.ActiveWorksheet.Name;
            var saved = _sessions.Update(sessionId, bytes, storageFormat, sheetName);
            return Ok(new
            {
                success = true,
                fileName = saved.FileName,
                activeSheetName = saved.ActiveSheetName,
                downloadUrl = Url.Action(nameof(Download), new { sessionId })
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Returns the download projection for the spreadsheet API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("download/{sessionId:guid}")]
    [IgnoreAntiforgeryToken]
    public IActionResult Download(Guid sessionId)
    {
        if (!_sessions.TryGet(sessionId, out var session)) return NotFound();
        return File(session.Content, ContentType(session.SourceFormat), session.FileName);
    }

    /// <summary>
    /// Runs the content type operation.
    /// </summary>
    private string ContentType(SpreadsheetStorageFormat format) => format switch
    {
        SpreadsheetStorageFormat.Xlsm => "application/vnd.ms-excel.sheet.macroEnabled.12",
        SpreadsheetStorageFormat.Xls => "application/vnd.ms-excel",
        SpreadsheetStorageFormat.Csv => "text/csv; charset=utf-8",
        SpreadsheetStorageFormat.Text => "text/plain; charset=utf-8",
        _ => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    };

    /// <summary>
    /// Runs the to DevExpress format operation.
    /// </summary>
    private DocumentFormat ToDevExpressFormat(SpreadsheetStorageFormat format) => format switch
    {
        SpreadsheetStorageFormat.Xlsm => DocumentFormat.Xlsm,
        SpreadsheetStorageFormat.Xls => DocumentFormat.Xls,
        SpreadsheetStorageFormat.Csv => DocumentFormat.Csv,
        SpreadsheetStorageFormat.Text => DocumentFormat.Text,
        _ => DocumentFormat.Xlsx
    };
}
