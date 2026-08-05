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
    private readonly PublicationLiveDataRegistry _registry;
    private readonly PublicationWebhookStore _webhooks;

    /// <summary>
    /// Runs the web data controller operation.
    /// </summary>
    public WebDataController(PublicationLiveDataRegistry registry, PublicationWebhookStore webhooks)
    {
        _registry = registry;
        _webhooks = webhooks;
    }

    /// <summary>
    /// Runs the status operation.
    /// </summary>
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
    /// Runs the publications operation.
    /// </summary>
    [HttpGet("publications")]
    public IActionResult Publications() => Ok(_registry.Summaries());

    /// <summary>
    /// Runs the publication operation.
    /// </summary>
    [HttpGet("publications/{documentId:guid}")]
    public IActionResult Publication(Guid documentId)
        => _registry.TryGet(documentId, out var publication) ? Ok(publication) : NotFound();

    /// <summary>
    /// Runs the data operation.
    /// </summary>
    [HttpGet("publications/{documentId:guid}/data/{dataId:guid}")]
    public IActionResult Data(Guid documentId, Guid dataId)
    {
        if (!_registry.TryGet(documentId, out var publication)) return NotFound();
        return publication.DataObjects.TryGetValue(dataId, out var data) ? Ok(data) : NotFound();
    }

    /// <summary>
    /// Runs the rows operation.
    /// </summary>
    [HttpGet("publications/{documentId:guid}/data/{dataId:guid}/rows")]
    public IActionResult Rows(Guid documentId, Guid dataId)
    {
        if (!_registry.TryGet(documentId, out var publication)) return NotFound();
        return publication.DataObjects.TryGetValue(dataId, out var data) ? Ok(data.Rows) : NotFound();
    }

    /// <summary>
    /// Runs the pages operation.
    /// </summary>
    [HttpGet("publications/{documentId:guid}/pages")]
    public IActionResult Pages(Guid documentId)
        => _registry.TryGet(documentId, out var publication) ? Ok(publication.Pages) : NotFound();

    /// <summary>
    /// Exports rows.
    /// </summary>
    // A tokenized, read-only CORS route lets a file:// or separately hosted HTML export
    // reconnect to the user's local monolith without exposing every open publication.
    /// <summary>
    /// Exports rows.
    /// </summary>
    [HttpGet("exports/{documentId:guid}/data/{dataId:guid}/{token}/rows")]
    [EnableCors("PublisherExport")]
    public IActionResult ExportRows(Guid documentId, Guid dataId, string token)
        => _registry.TryGetExportRows(documentId, dataId, token, out var rows) ? Ok(rows) : NotFound();

    /// <summary>
    /// Runs the webhook operation.
    /// </summary>
    [HttpPost("webhooks/{bindingId:guid}/{token}")]
    [HttpPut("webhooks/{bindingId:guid}/{token}")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Webhook(Guid bindingId, string token, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, detectEncodingFromByteOrderMarks: true);
        var content = await reader.ReadToEndAsync(cancellationToken);
        var contentType = Request.ContentType ?? "application/octet-stream";
        return _webhooks.TryPut(bindingId, token, content, contentType)
            ? Accepted(new { bindingId, receivedUtc = DateTimeOffset.UtcNow })
            : NotFound(new { message = "The webhook binding is unknown or its token is invalid." });
    }

    /// <summary>
    /// Runs the webhook status operation.
    /// </summary>
    [HttpGet("webhooks/{bindingId:guid}/status")]
    public IActionResult WebhookStatus(Guid bindingId)
    {
        if (!_webhooks.IsRegistered(bindingId)) return NotFound();
        return _webhooks.TryGet(bindingId, out var payload)
            ? Ok(new { bindingId, payload.ReceivedUtc, payload.ContentType, characterCount = payload.Content.Length })
            : Ok(new { bindingId, receivedUtc = (DateTimeOffset?)null, contentType = string.Empty, characterCount = 0 });
    }
}
