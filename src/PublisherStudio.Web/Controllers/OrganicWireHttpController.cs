using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.OrganicPlugins;
using PublisherStudio.Services.Automation;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PublisherStudio.Controllers;

/// <summary>
/// Transport-neutral HTTP/JSON adapter for user-built organic add-ons and small gateway devices.
/// The endpoint shares PublisherStudio's capability catalog, permission store, approval queue and runtime trust service.
/// </summary>
/// <param name="codec">Organic plugin protocol codec dependency used by the organic wire HTTP workflow to provide the corresponding application capability.</param>
/// <param name="security">Organic runtime security service dependency used by the organic wire HTTP workflow to provide the corresponding application capability.</param>
/// <param name="capabilities">Organic capability catalog dependency used by the organic wire HTTP workflow to provide the corresponding application capability.</param>
/// <param name="work">Organic work coordinator dependency used by the organic wire HTTP workflow to provide the corresponding application capability.</param>
/// <param name="replayGuard">Organic replay guard dependency used by the organic wire HTTP workflow to provide the corresponding application capability.</param>
/// <param name="transportSecurityPolicy">Organic transport security policy dependency used by the organic wire HTTP workflow to provide the corresponding application capability.</param>
/// <param name="envelopeFactory">Organic wire envelope factory dependency used by the organic wire HTTP workflow to provide the corresponding application capability.</param>
/// <param name="apiSurfaces">Api surface catalog service dependency used by the organic wire HTTP workflow to provide the corresponding application capability.</param>
/// <param name="configuredOptions">Organic plugin options dependency used by the organic wire HTTP workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/organic/onewire/http-json")]
public sealed class OrganicWireHttpController(
    IOrganicPluginProtocolCodec codec,
    IOrganicRuntimeSecurityService security,
    IOrganicCapabilityCatalog capabilities,
    IOrganicWorkCoordinator work,
    IOrganicReplayGuard replayGuard,
    IOrganicTransportSecurityPolicy transportSecurityPolicy,
    IOrganicWireEnvelopeFactory envelopeFactory,
    IApiSurfaceCatalogService apiSurfaces,
    IOptions<OrganicPluginOptions> configuredOptions,
    ILogger<OrganicWireHttpController> logger) : ControllerBase
{
    /// <summary>
    /// Returns the profile projection for the organic wire HTTP API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("profile")]
    public async Task<ActionResult<OrganicProtocolProfile>> Profile(CancellationToken cancellationToken)
    {
        try
        {
            var options = configuredOptions.Value;
            return Ok(new OrganicProtocolProfile
            {
                Security = await security.GetPublicDescriptorAsync(cancellationToken).ConfigureAwait(false),
                Capabilities = (await capabilities.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false)).ToList(),
                Skills = (await capabilities.GetSkillsAsync(cancellationToken).ConfigureAwait(false)).ToList(),
                UiFeatures = (await capabilities.GetUiFeaturesAsync(cancellationToken).ConfigureAwait(false)).ToList(),
                Hardware = (await capabilities.GetHardwareAsync(cancellationToken).ConfigureAwait(false)).ToList(),
                ControllerSurfaces = apiSurfaces.GetSurfaces().ToList(),
                Settings = new OrganicProtocolSettings
                {
                    Enabled = options.Enabled,
                    DiscoveryEnabled = options.EnableDiscovery,
                    AutoConnectDiscoveredPeer = options.AutoConnectDiscoveredPeer,
                    DiscoveryPort = options.DiscoveryPort,
                    PeerExpirySeconds = options.PeerExpirySeconds,
                    MaximumMessageBytes = options.MaximumMessageBytes,
                    MinimumRecurringScreenReaderIntervalSeconds = options.MinimumRecurringScreenReaderIntervalSeconds
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not return the PublisherStudio organic HTTP/JSON profile.");
            return Problem(ex.Message);
        }
    }

    /// <summary>
    /// Returns the dispatch projection for the organic wire HTTP API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="body">Body value supplied to the organic wire HTTP operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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
            if (!transportSecurityPolicy.IsProtected(envelope))
                throw new CryptographicException("PublisherStudio HTTP/JSON Invoke requests require an MFA-verified signed and encrypted peer.");
            if (!replayGuard.TryAccept(envelope.SourcePeerId, envelope.MessageId, envelope.CreatedUtc))
                throw new InvalidDataException("This organic 1-Wire message id has already been processed or is outside the accepted replay window.");

            var item = await work.ReceiveAsync(envelope, cancellationToken).ConfigureAwait(false);
            var response = envelopeFactory.CreateWorkEnvelope(item, $"publisherstudio:{Environment.MachineName}");
            await security.ProtectOutgoingAsync(response, cancellationToken).ConfigureAwait(false);
            if (transportSecurityPolicy.RequiresProtectedTransport(response.MessageType) &&
                !transportSecurityPolicy.IsProtected(response))
                throw new CryptographicException("The PublisherStudio HTTP/JSON response requires an MFA-verified peer before application data can be returned.");
            return Content(codec.Serialize(response), "application/json", Encoding.UTF8);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
#if DEBUG
            logger.LogInformation("Cancelled a PublisherStudio organic HTTP/JSON request at the caller's request in a Debug build.");
#endif
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

    /// <summary>
    /// Returns the work projection for the organic wire HTTP API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="correlationId">Identifier of the correlation to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("work/{correlationId:guid}")]
    public async Task<IActionResult> Work(Guid correlationId, CancellationToken cancellationToken)
    {
        try
        {
            var item = work.GetWork().FirstOrDefault(candidate => candidate.CorrelationId == correlationId);
            if (item is null)
                return NotFound(new { CorrelationId = correlationId, Status = "NotFoundOrNotQueuedYet" });
            var response = envelopeFactory.CreateWorkEnvelope(item, $"publisherstudio:{Environment.MachineName}");
            await security.ProtectOutgoingAsync(response, cancellationToken).ConfigureAwait(false);
            if (transportSecurityPolicy.RequiresProtectedTransport(response.MessageType) &&
                !transportSecurityPolicy.IsProtected(response))
                throw new CryptographicException("The PublisherStudio HTTP/JSON work response requires an MFA-verified peer before application data can be returned.");
            return Content(codec.Serialize(response), "application/json", Encoding.UTF8);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not read PublisherStudio organic HTTP work for correlation {CorrelationId}.", correlationId);
            return Problem(ex.Message);
        }
    }

}
