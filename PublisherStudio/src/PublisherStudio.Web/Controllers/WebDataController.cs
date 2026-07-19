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

    public WebDataController(PublicationLiveDataRegistry registry, PublicationWebhookStore webhooks)
    {
        _registry = registry;
        _webhooks = webhooks;
    }

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

    [HttpGet("publications")]
    public IActionResult Publications() => Ok(_registry.Summaries());

    [HttpGet("publications/{documentId:guid}")]
    public IActionResult Publication(Guid documentId)
        => _registry.TryGet(documentId, out var publication) ? Ok(publication) : NotFound();

    [HttpGet("publications/{documentId:guid}/data/{dataId:guid}")]
    public IActionResult Data(Guid documentId, Guid dataId)
    {
        if (!_registry.TryGet(documentId, out var publication)) return NotFound();
        return publication.DataObjects.TryGetValue(dataId, out var data) ? Ok(data) : NotFound();
    }

    [HttpGet("publications/{documentId:guid}/data/{dataId:guid}/rows")]
    public IActionResult Rows(Guid documentId, Guid dataId)
    {
        if (!_registry.TryGet(documentId, out var publication)) return NotFound();
        return publication.DataObjects.TryGetValue(dataId, out var data) ? Ok(data.Rows) : NotFound();
    }

    [HttpGet("publications/{documentId:guid}/pages")]
    public IActionResult Pages(Guid documentId)
        => _registry.TryGet(documentId, out var publication) ? Ok(publication.Pages) : NotFound();

    // A tokenized, read-only CORS route lets a file:// or separately hosted HTML export
    // reconnect to the user's local monolith without exposing every open publication.
    [HttpGet("exports/{documentId:guid}/data/{dataId:guid}/{token}/rows")]
    [EnableCors("PublisherExport")]
    public IActionResult ExportRows(Guid documentId, Guid dataId, string token)
        => _registry.TryGetExportRows(documentId, dataId, token, out var rows) ? Ok(rows) : NotFound();

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

    [HttpGet("webhooks/{bindingId:guid}/status")]
    public IActionResult WebhookStatus(Guid bindingId)
    {
        if (!_webhooks.IsRegistered(bindingId)) return NotFound();
        return _webhooks.TryGet(bindingId, out var payload)
            ? Ok(new { bindingId, payload.ReceivedUtc, payload.ContentType, characterCount = payload.Content.Length })
            : Ok(new { bindingId, receivedUtc = (DateTimeOffset?)null, contentType = string.Empty, characterCount = 0 });
    }
}
