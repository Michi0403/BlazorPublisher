using Microsoft.AspNetCore.Mvc;
using PublisherStudio.Services.OrganicPlugins;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PublisherStudio.Controllers;

/// <summary>
/// Transport-neutral HTTP/JSON adapter for user-built organic add-ons and small gateway devices.
/// The endpoint shares PublisherStudio's capability catalog, permission store, approval queue and runtime trust service.
/// </summary>
[ApiController]
[Route("api/organic/onewire/http-json")]
public sealed class OrganicWireHttpController(
    IOrganicPluginProtocolCodec codec,
    IOrganicRuntimeSecurityService security,
    IOrganicCapabilityCatalog capabilities,
    IOrganicWorkCoordinator work,
    ILogger<OrganicWireHttpController> logger) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<ActionResult<object>> Profile(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(new
            {
                ProtocolVersion = OrganicWireProtocol.Version,
                OrganicWireProtocol.MinimumCompatibleVersion,
                Transport = "http-json",
                PostEnvelope = "/api/organic/onewire/http-json",
                PollWork = "/api/organic/onewire/http-json/work/{correlationId}",
                MaximumMessageBytes = OrganicWireProtocol.MaximumMessageBytes,
                Security = await security.GetPublicDescriptorAsync(cancellationToken).ConfigureAwait(false),
                Capabilities = await capabilities.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false),
                Skills = await capabilities.GetSkillsAsync(cancellationToken).ConfigureAwait(false),
                UiFeatures = await capabilities.GetUiFeaturesAsync(cancellationToken).ConfigureAwait(false)
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not return the PublisherStudio organic HTTP/JSON profile.");
            return Problem(ex.Message);
        }
    }

    [HttpPost]
    [RequestSizeLimit(OrganicWireProtocol.MaximumMessageBytes)]
    public async Task<IActionResult> Dispatch([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        try
        {
            var json = body.GetRawText();
            if (Encoding.UTF8.GetByteCount(json) > OrganicWireProtocol.MaximumMessageBytes)
                return BadRequest(new { Error = "The organic HTTP/JSON envelope is too large." });

            var envelope = codec.DeserializeAndValidate(json);
            await security.UnprotectIncomingAsync(envelope, cancellationToken).ConfigureAwait(false);
            if (envelope.MessageType is not OrganicWireMessageType.Invoke)
                return BadRequest(new { Error = "The PublisherStudio HTTP/JSON adapter currently accepts Invoke envelopes; use profile for catalog discovery." });

            var item = await work.ReceiveAsync(envelope, cancellationToken).ConfigureAwait(false);
            var response = CreateWorkEnvelope(item);
            await security.ProtectOutgoingAsync(response, cancellationToken).ConfigureAwait(false);
            return Content(codec.Serialize(response), "application/json", Encoding.UTF8);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Cancelled a PublisherStudio organic HTTP/JSON request at the caller's request.");
            return StatusCode(499);
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or CryptographicException or FormatException or ArgumentException)
        {
            logger.LogWarning(ex, "Rejected an invalid PublisherStudio organic HTTP/JSON request.");
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PublisherStudio organic HTTP/JSON dispatch failed.");
            return Problem(ex.Message);
        }
    }

    [HttpGet("work/{correlationId:guid}")]
    public async Task<IActionResult> Work(Guid correlationId, CancellationToken cancellationToken)
    {
        try
        {
            var item = work.GetWork().FirstOrDefault(candidate => candidate.CorrelationId == correlationId);
            if (item is null)
                return NotFound(new { CorrelationId = correlationId, Status = "NotFoundOrNotQueuedYet" });
            var response = CreateWorkEnvelope(item);
            await security.ProtectOutgoingAsync(response, cancellationToken).ConfigureAwait(false);
            return Content(codec.Serialize(response), "application/json", Encoding.UTF8);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not read PublisherStudio organic HTTP work for correlation {CorrelationId}.", correlationId);
            return Problem(ex.Message);
        }
    }

    private static OrganicWireEnvelope CreateWorkEnvelope(PublisherStudio.Domain.OrganicPluginWorkItem item) => new()
    {
        MessageType = item.Status == OrganicWorkStatus.PendingApproval
            ? OrganicWireMessageType.ApprovalRequired
            : item.Status is OrganicWorkStatus.Queued or OrganicWorkStatus.Running
                ? OrganicWireMessageType.WorkAccepted
                : OrganicWireMessageType.WorkResult,
        CorrelationId = item.CorrelationId,
        ReplyToMessageId = item.MessageId,
        SourcePeerId = "publisherstudio",
        TargetPeerId = item.PeerId,
        CapabilityKey = item.CapabilityKey,
        Error = item.Error,
        InteractionValueJson = item.Request.InteractionValueJson,
        InteractionValueContentType = item.Request.InteractionValueContentType,
        Properties = new Dictionary<string, JsonElement>
        {
            ["WorkItemId"] = JsonSerializer.SerializeToElement(item.Id),
            ["Status"] = JsonSerializer.SerializeToElement(item.Status.ToString()),
            ["ResultJson"] = JsonSerializer.SerializeToElement(item.ResultJson),
            ["InteractionValueJson"] = JsonSerializer.SerializeToElement(item.Request.InteractionValueJson),
            ["InteractionValueContentType"] = JsonSerializer.SerializeToElement(item.Request.InteractionValueContentType)
        }
    };
}
