using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Automation;
using PublisherStudio.Services.OrganicPlugins;

namespace PublisherStudio.Controllers;

/// <summary>
/// Provides organic plugin controller operations.
/// </summary>
[ApiController]
[Route("api/organic")]
public sealed class OrganicPluginController(
    ILocalGptDiscoveryRegistry discovery,
    IOrganicCapabilityCatalog capabilities,
    IOrganicPermissionStore permissions,
    IOrganicWorkCoordinator work,
    IOrganicResultStore results,
    ILocalGptConnectionService connection,
    IOrganicConnectionRuntimeState runtimeState,
    IOrganicReplayPolicyDataService replayPolicy,
    IApiSurfaceCatalogService apiSurfaces,
    ILogger<OrganicPluginController> logger) : ControllerBase
{
    /// <summary>
    /// Runs the status operation.
    /// </summary>
    [HttpGet("status")]
    public ActionResult<OrganicConnectionState> Status() => Ok(connection.State);


    /// <summary>
    /// Runs the transport operation.
    /// </summary>
    [HttpGet("transport")]
    public ActionResult<OrganicConnectionRuntimeSnapshot> Transport()
    {
        try
        {
            var snapshot = runtimeState.GetSnapshot();
            logger.LogDebug($"Returned organic transport state for connection {snapshot.ConnectionId}.");
            return Ok(snapshot);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not return the organic transport state.");
            return Problem(ex.Message);
        }
    }


    /// <summary>
    /// Runs the replay policy operation.
    /// </summary>
    [HttpGet("replay-policy")]
    public ActionResult<OrganicReplayPolicySnapshot> ReplayPolicy()
    {
        try
        {
            var snapshot = replayPolicy.GetSnapshot();
            logger.LogDebug($"Returned the configured organic replay policy.");
            return Ok(snapshot);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not return the configured organic replay policy.");
            return Problem(ex.Message);
        }
    }

    /// <summary>
    /// Adds on manifest.
    /// </summary>
    [HttpGet("addon-manifest")]
    public ActionResult<object> AddonManifest()
    {
        try
        {
            var surfaces = apiSurfaces.GetSurfaces();
            logger.LogInformation("Returned the Publisher Studio organic add-on manifest with {ControllerCount} controller surface(s).", surfaces.Count);
            return Ok(new
            {
                Key = "publisherstudio",
                DisplayName = "Publisher Studio / BlazorPublisher",
                SourcePeerId = "publisherstudio",
                IsOnline = connection.State.IsConnected,
                ControllerSurfaces = surfaces
            });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not return the Publisher Studio organic add-on manifest.");
            return Problem(exception.Message);
        }
    }

    /// <summary>
    /// Runs the peers operation.
    /// </summary>
    [HttpGet("peers")]
    public ActionResult<IReadOnlyList<OrganicPeerAdvertisement>> Peers() => Ok(discovery.GetPeers());

    /// <summary>
    /// Runs the capabilities operation.
    /// </summary>
    [HttpGet("capabilities")]
    public async Task<ActionResult<IReadOnlyList<OrganicCapabilityDescriptor>>> Capabilities(CancellationToken cancellationToken) =>
        Ok(await capabilities.GetCapabilitiesAsync(cancellationToken));

    /// <summary>
    /// Runs the permissions operation.
    /// </summary>
    [HttpGet("permissions")]
    public ActionResult<IReadOnlyList<OrganicPermissionRule>> Permissions() => Ok(permissions.GetRules());

    /// <summary>
    /// Saves permission.
    /// </summary>
    [HttpPost("permissions")]
    public ActionResult<OrganicPermissionRule> SavePermission([FromBody] OrganicPermissionRule rule) => Ok(permissions.Save(rule));

    /// <summary>
    /// Deletes permission.
    /// </summary>
    [HttpDelete("permissions")]
    public IActionResult DeletePermission([FromQuery] string peerId, [FromQuery] string capabilityKey, [FromQuery] string organ = "") =>
        permissions.Delete(peerId, capabilityKey, organ) ? NoContent() : NotFound();

    /// <summary>
    /// Runs the work operation.
    /// </summary>
    [HttpGet("work")]
    public ActionResult<IReadOnlyList<OrganicPluginWorkItem>> Work() => Ok(work.GetWork());

    /// <summary>
    /// Runs the results operation.
    /// </summary>
    [HttpGet("results")]
    public ActionResult<IReadOnlyList<OrganicPluginWorkItem>> Results() => Ok(results.GetResults());

    /// <summary>
    /// Runs the text proposals operation.
    /// </summary>
    [HttpGet("text-proposals")]
    public ActionResult<IReadOnlyList<OrganicTextInsertionProposal>> TextProposals() => Ok(results.GetTextProposals());

    /// <summary>
    /// Runs the connect operation.
    /// </summary>
    [HttpPost("connect/{peerId}")]
    public async Task<ActionResult<OrganicConnectionState>> Connect(string peerId, CancellationToken cancellationToken)
    {
        var state = await connection.ConnectAsync(peerId, cancellationToken);
        return state.IsConnected ? Ok(state) : Problem(state.LastError, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    /// <summary>
    /// Runs the disconnect operation.
    /// </summary>
    [HttpPost("disconnect")]
    public async Task<IActionResult> Disconnect()
    {
        await connection.DisconnectAsync();
        return NoContent();
    }

    /// <summary>
    /// Runs the council operation.
    /// </summary>
    [HttpPost("council")]
    public async Task<IActionResult> Council([FromBody] OrganicCouncilPromptRequest request, CancellationToken cancellationToken)
    {
        if (!connection.State.IsConnected) return Conflict("PublisherStudio is not connected to LocalGPT.");
        var messageId = await connection.SendCouncilRequestAsync(request, cancellationToken);
        logger.LogInformation("Submitted organic council request {MessageId} for team {TeamKey}.", messageId, request.TeamKey);
        return Accepted(new { MessageId = messageId, request.TeamKey });
    }

    /// <summary>
    /// Runs the approve operation.
    /// </summary>
    [HttpPost("work/{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        var item = await work.ApproveAsync(id, cancellationToken);
        if (item is null) return NotFound();
        await connection.SendWorkResultAsync(item, cancellationToken);
        return Ok(item);
    }

    /// <summary>
    /// Runs the decline operation.
    /// </summary>
    [HttpPost("work/{id:guid}/decline")]
    public async Task<IActionResult> Decline(Guid id, [FromBody] OrganicWorkDecisionRequest? decision, CancellationToken cancellationToken)
    {
        if (!work.Decline(id, decision?.Reason ?? string.Empty)) return NotFound();
        var item = work.Get(id)!;
        await connection.SendWorkResultAsync(item, cancellationToken);
        return Ok(item);
    }
}
