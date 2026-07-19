using DevExpress.AspNetCore.Spreadsheet;
using DevExpress.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using PublisherStudio.Domain;
using PublisherStudio.Models;
using PublisherStudio.Services;

namespace PublisherStudio.Controllers;

[Route("spreadsheet")]
[AutoValidateAntiforgeryToken]
public sealed class SpreadsheetController : Controller
{
    private readonly SpreadsheetSessionStore _sessions;

    public SpreadsheetController(SpreadsheetSessionStore sessions) => _sessions = sessions;

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

    [AcceptVerbs("GET", "POST")]
    [Route("request")]
    public IActionResult RequestHandler() => SpreadsheetRequestProcessor.GetResponse(HttpContext);

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

    [HttpGet("download/{sessionId:guid}")]
    [IgnoreAntiforgeryToken]
    public IActionResult Download(Guid sessionId)
    {
        if (!_sessions.TryGet(sessionId, out var session)) return NotFound();
        return File(session.Content, ContentType(session.SourceFormat), session.FileName);
    }

    private static string ContentType(SpreadsheetStorageFormat format) => format switch
    {
        SpreadsheetStorageFormat.Xlsm => "application/vnd.ms-excel.sheet.macroEnabled.12",
        SpreadsheetStorageFormat.Xls => "application/vnd.ms-excel",
        SpreadsheetStorageFormat.Csv => "text/csv; charset=utf-8",
        SpreadsheetStorageFormat.Text => "text/plain; charset=utf-8",
        _ => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    };

    private static DocumentFormat ToDevExpressFormat(SpreadsheetStorageFormat format) => format switch
    {
        SpreadsheetStorageFormat.Xlsm => DocumentFormat.Xlsm,
        SpreadsheetStorageFormat.Xls => DocumentFormat.Xls,
        SpreadsheetStorageFormat.Csv => DocumentFormat.Csv,
        SpreadsheetStorageFormat.Text => DocumentFormat.Text,
        _ => DocumentFormat.Xlsx
    };
}
