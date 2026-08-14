using System.Diagnostics;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using PublisherStudio.Services;

namespace PublisherStudio.Controllers;

/// <summary>
/// Loopback-first data API for publication objects. The DTO boundary is deliberately
/// transport-neutral so the same routes can later feed LAN viewers, VLC-compatible
/// streams, or configured streaming providers without coupling them to Blazor state.
/// </summary>
[ApiController]
[Route("api/publisher")]
public sealed class WebDataController : ControllerBase
{
    /// <summary>
    /// Stores the publication live data registry dependency used by <see cref="WebDataController"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly PublicationLiveDataRegistry _registry;
    /// <summary>
    /// Stores the publication webhook store dependency used by <see cref="WebDataController"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly PublicationWebhookStore _webhooks;

    /// <summary>
    /// Initializes a new <see cref="WebDataController"/> instance and captures the dependencies or initial state required by its web data workflow.
    /// </summary>
    /// <param name="registry">Publication live data registry dependency used by the web data workflow to provide the corresponding application capability.</param>
    /// <param name="webhooks">Publication webhook store dependency used by the web data workflow to provide the corresponding application capability.</param>
    public WebDataController(PublicationLiveDataRegistry registry, PublicationWebhookStore webhooks)
    {
        _registry = registry;
        _webhooks = webhooks;
    }

    /// <summary>
    /// Returns the status projection for the web data API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("system/status")]
    public IActionResult Status()
    {
        var process = Process.GetCurrentProcess();
        return Ok(new[]
        {
            new Dictionary<string, object?>
            {
                ["Name"] = "PublisherStudio",
                ["State"] = "Running",
                ["TimestampUtc"] = DateTimeOffset.UtcNow,
                ["ProcessId"] = Environment.ProcessId,
                ["Machine"] = Environment.MachineName,
                ["WorkingSetMb"] = Math.Round(process.WorkingSet64 / 1024d / 1024d, 2),
                ["Publications"] = _registry.Summaries().Count
            }
        });
    }

    /// <summary>
    /// Returns the publications projection for the web data API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("publications")]
    public IActionResult Publications() => Ok(_registry.Summaries());

    /// <summary>
    /// Returns the publication projection for the web data API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="documentId">Identifier of the document to use for this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("publications/{documentId:guid}")]
    public IActionResult Publication(Guid documentId)
        => _registry.TryGet(documentId, out var publication) ? Ok(publication) : NotFound();

    /// <summary>
    /// Returns the data projection for the web data API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="documentId">Identifier of the document to use for this operation.</param>
    /// <param name="dataId">Identifier of the data to use for this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("publications/{documentId:guid}/data/{dataId:guid}")]
    public IActionResult Data(Guid documentId, Guid dataId)
    {
        if (!_registry.TryGet(documentId, out var publication)) return NotFound();
        return publication.DataObjects.TryGetValue(dataId, out var data) ? Ok(data) : NotFound();
    }

    /// <summary>
    /// Returns the rows projection for the web data API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="documentId">Identifier of the document to use for this operation.</param>
    /// <param name="dataId">Identifier of the data to use for this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("publications/{documentId:guid}/data/{dataId:guid}/rows")]
    public IActionResult Rows(Guid documentId, Guid dataId)
    {
        if (!_registry.TryGet(documentId, out var publication)) return NotFound();
        return publication.DataObjects.TryGetValue(dataId, out var data) ? Ok(data.Rows) : NotFound();
    }

    /// <summary>
    /// Returns the pages projection for the web data API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="documentId">Identifier of the document to use for this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("publications/{documentId:guid}/pages")]
    public IActionResult Pages(Guid documentId)
        => _registry.TryGet(documentId, out var publication) ? Ok(publication.Pages) : NotFound();

    // A tokenized, read-only CORS route lets a file:// or separately hosted HTML export
    // reconnect to the user's local monolith without exposing every open publication.
    /// <summary>
    /// Exports rows for one token-authorized publication data source.
    /// </summary>
    /// <param name="documentId">Identifier of the document to use for this operation.</param>
    /// <param name="dataId">Identifier of the data to use for this operation.</param>
    /// <param name="token">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("exports/{documentId:guid}/data/{dataId:guid}/{token}/rows")]
    [EnableCors("PublisherExport")]
    public IActionResult ExportRows(Guid documentId, Guid dataId, string token)
        => _registry.TryGetExportRows(documentId, dataId, token, out var rows) ? Ok(rows) : NotFound();

    /// <summary>
    /// Returns the webhook projection for the web data API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="bindingId">Identifier of the binding to use for this operation.</param>
    /// <param name="token">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("webhooks/{bindingId:guid}/{token}")]
    [HttpPut("webhooks/{bindingId:guid}/{token}")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Webhook(Guid bindingId, string token, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, detectEncodingFromByteOrderMarks: true);
        var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var contentType = Request.ContentType ?? "application/octet-stream";
        return _webhooks.TryPut(bindingId, token, content, contentType)
            ? Accepted(new { bindingId, receivedUtc = DateTimeOffset.UtcNow })
            : NotFound(new { message = "The webhook binding is unknown or its token is invalid." });
    }

    /// <summary>
    /// Returns the webhook status projection for the web data API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="bindingId">Identifier of the binding to use for this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("webhooks/{bindingId:guid}/status")]
    public IActionResult WebhookStatus(Guid bindingId)
    {
        if (!_webhooks.IsRegistered(bindingId)) return NotFound();
        return _webhooks.TryGet(bindingId, out var payload)
            ? Ok(new { bindingId, payload.ReceivedUtc, payload.ContentType, characterCount = payload.Content.Length })
            : Ok(new { bindingId, receivedUtc = (DateTimeOffset?)null, contentType = string.Empty, characterCount = 0 });
    }
}
