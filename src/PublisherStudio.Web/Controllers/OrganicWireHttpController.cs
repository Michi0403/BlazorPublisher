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
/// <param name="traffic">1-Wire traffic journal that logs and shows every HTTP/JSON packet this endpoint receives or returns.</param>
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
    IOrganicWireTrafficJournal traffic,
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
            var profile = new OrganicProtocolProfile
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
            };
            JournalProfile(profile);
            return Ok(profile);
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
        var json = string.Empty;
        OrganicWireEnvelope? received = null;
        var requestJournaled = false;
        try
        {
            json = body.GetRawText();
            if (Encoding.UTF8.GetByteCount(json) > OrganicWireProtocol.MaximumMessageBytes)
            {
                requestJournaled = JournalHttp(OrganicWireTrafficDirections.Received, json, null, string.Empty, "The organic HTTP/JSON envelope is too large.");
                return BadRequest(new { Error = "The organic HTTP/JSON envelope is too large." });
            }

            var envelope = codec.DeserializeAndValidate(json);
            received = envelope;
            await security.UnprotectIncomingAsync(envelope, cancellationToken).ConfigureAwait(false);
            if (envelope.MessageType is not OrganicWireMessageType.Invoke)
            {
                requestJournaled = JournalHttp(OrganicWireTrafficDirections.Received, json, envelope, string.Empty, "The PublisherStudio HTTP/JSON adapter currently accepts Invoke envelopes; use profile for catalog discovery.");
                return BadRequest(new { Error = "The PublisherStudio HTTP/JSON adapter currently accepts Invoke envelopes; use profile for catalog discovery." });
            }
            if (!transportSecurityPolicy.IsProtected(envelope))
                throw new CryptographicException("PublisherStudio HTTP/JSON Invoke requests require an MFA-verified signed and encrypted peer.");
            if (!replayGuard.TryAccept(envelope.SourcePeerId, envelope.MessageId, envelope.CreatedUtc))
                throw new InvalidDataException("This organic 1-Wire message id has already been processed or is outside the accepted replay window.");
            requestJournaled = JournalHttp(OrganicWireTrafficDirections.Received, json, envelope, string.Empty, string.Empty);

            var item = await work.ReceiveAsync(envelope, cancellationToken).ConfigureAwait(false);
            var response = envelopeFactory.CreateWorkEnvelope(item, $"publisherstudio:{Environment.MachineName}");
            return await RespondAsync(response, "The PublisherStudio HTTP/JSON response requires an MFA-verified peer before application data can be returned.", cancellationToken).ConfigureAwait(false);
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
            if (!requestJournaled)
                JournalHttp(OrganicWireTrafficDirections.Received, json, received, string.Empty, ex.Message);
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
            return await RespondAsync(response, "The PublisherStudio HTTP/JSON work response requires an MFA-verified peer before application data can be returned.", cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not read PublisherStudio organic HTTP work for correlation {CorrelationId}.", correlationId);
            return Problem(ex.Message);
        }
    }

    /// <summary>Protects, serializes and journals one HTTP/JSON response envelope.</summary>
    /// <param name="response">The response envelope in plaintext.</param>
    /// <param name="protectionRequiredMessage">Error raised when the response type needs protection the peer does not have.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The JSON content result carrying the protected frame.</returns>
    private async Task<IActionResult> RespondAsync(OrganicWireEnvelope response, string protectionRequiredMessage, CancellationToken cancellationToken)
    {
        var plaintext = string.Empty;
        var frame = string.Empty;
        try
        {
            plaintext = traffic.CapturePlaintext(response);
            await security.ProtectOutgoingAsync(response, cancellationToken).ConfigureAwait(false);
            if (transportSecurityPolicy.RequiresProtectedTransport(response.MessageType) &&
                !transportSecurityPolicy.IsProtected(response))
                throw new CryptographicException(protectionRequiredMessage);
            frame = codec.Serialize(response);
            JournalHttp(OrganicWireTrafficDirections.Sent, frame, response, plaintext, string.Empty);
            return Content(frame, "application/json", Encoding.UTF8);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            JournalHttp(OrganicWireTrafficDirections.Sent, frame, response, plaintext, exception.Message);
            logger.LogDebug(exception, "Sending a PublisherStudio organic HTTP/JSON response failed for message {MessageId}.", response.MessageId);
            throw;
        }
    }

    /// <summary>
    /// Journals a profile request and the profile returned for it. MVC writes the response frame itself, so the journal keeps
    /// the profile serialized with the web defaults MVC uses as the payload and leaves the raw frame empty.
    /// </summary>
    /// <param name="profile">The profile about to be returned.</param>
    private void JournalProfile(OrganicProtocolProfile profile)
    {
        try
        {
            var remoteEndpoint = $"{HttpContext.Connection.RemoteIpAddress}:{HttpContext.Connection.RemotePort}";
            traffic.Record(new OrganicWireTrafficRecord
            {
                Direction = OrganicWireTrafficDirections.Received,
                Transport = OrganicWireTrafficTransports.HttpJson,
                RemoteEndpoint = remoteEndpoint,
                RawFrame = $"GET {Request.Path}",
                MessageType = "Profile"
            });
            var payload = traffic.CurrentOptions.Enabled ? JsonSerializer.Serialize(profile, JsonSerializerOptions.Web) : string.Empty;
            traffic.Record(new OrganicWireTrafficRecord
            {
                Direction = OrganicWireTrafficDirections.Sent,
                Transport = OrganicWireTrafficTransports.HttpJson,
                RemoteEndpoint = remoteEndpoint,
                PlaintextJson = payload,
                MessageType = "Profile"
            });
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Journaling the PublisherStudio organic HTTP/JSON profile exchange failed.");
        }
    }

    /// <summary>Journals one HTTP/JSON packet with this request's remote endpoint.</summary>
    /// <param name="direction">Received or Sent.</param>
    /// <param name="frame">The frame as it crossed the wire.</param>
    /// <param name="envelope">The envelope, decrypted when received, when one was parsed.</param>
    /// <param name="plaintext">The plaintext captured before protection, for sent packets.</param>
    /// <param name="error">Rejection or failure reason; empty on success.</param>
    /// <returns>Always <see langword="true"/>, so callers can remember that the packet was journaled.</returns>
    private bool JournalHttp(string direction, string frame, OrganicWireEnvelope? envelope, string plaintext, string error)
    {
        try
        {
            traffic.Record(new OrganicWireTrafficRecord
            {
                Direction = direction,
                Transport = OrganicWireTrafficTransports.HttpJson,
                RemoteEndpoint = $"{HttpContext.Connection.RemoteIpAddress}:{HttpContext.Connection.RemotePort}",
                RawFrame = frame,
                Envelope = envelope,
                PlaintextJson = plaintext,
                Error = error
            });
            return true;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Journaling a PublisherStudio organic HTTP/JSON packet failed.");
            return true;
        }
    }
}
