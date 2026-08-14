using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Automation;
using PublisherStudio.Services.OrganicPlugins;

namespace PublisherStudio.Controllers;

/// <summary>
/// Exposes the organic plugin application operations through PublisherStudio's web/API boundary and delegates domain work to the corresponding services.
/// </summary>
/// <param name="discovery">Local gpt discovery registry dependency used by the organic plugin workflow to provide the corresponding application capability.</param>
/// <param name="capabilities">Organic capability catalog dependency used by the organic plugin workflow to provide the corresponding application capability.</param>
/// <param name="permissions">Organic permission store dependency used by the organic plugin workflow to provide the corresponding application capability.</param>
/// <param name="work">Organic work coordinator dependency used by the organic plugin workflow to provide the corresponding application capability.</param>
/// <param name="results">Organic result store dependency used by the organic plugin workflow to provide the corresponding application capability.</param>
/// <param name="connection">Local gpt connection service dependency used by the organic plugin workflow to provide the corresponding application capability.</param>
/// <param name="runtimeState">Organic connection runtime state dependency used by the organic plugin workflow to provide the corresponding application capability.</param>
/// <param name="replayPolicy">Organic replay policy data service dependency used by the organic plugin workflow to provide the corresponding application capability.</param>
/// <param name="apiSurfaces">Api surface catalog service dependency used by the organic plugin workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
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
    /// Returns the status projection for the organic plugin API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("status")]
    public ActionResult<OrganicConnectionState> Status() => Ok(connection.State);


    /// <summary>
    /// Returns the transport projection for the organic plugin API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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
    /// Returns the replay policy projection for the organic plugin API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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
    /// Adds on manifest for the organic plugin API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
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
    /// Returns the peers projection for the organic plugin API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("peers")]
    public ActionResult<IReadOnlyList<OrganicPeerAdvertisement>> Peers() => Ok(discovery.GetPeers());

    /// <summary>
    /// Returns the capabilities projection for the organic plugin API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("capabilities")]
    public async Task<ActionResult<IReadOnlyList<OrganicCapabilityDescriptor>>> Capabilities(CancellationToken cancellationToken) =>
        Ok(await capabilities.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Returns the permissions projection for the organic plugin API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("permissions")]
    public ActionResult<IReadOnlyList<OrganicPermissionRule>> Permissions() => Ok(permissions.GetRules());

    /// <summary>
    /// Persists permission for the organic plugin API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="rule">Rule value supplied to the organic plugin operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("permissions")]
    public ActionResult<OrganicPermissionRule> SavePermission([FromBody] OrganicPermissionRule rule) => Ok(permissions.Save(rule));

    /// <summary>
    /// Deletes permission for the organic plugin API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="capabilityKey">Capability key value supplied to the organic plugin operation and used when producing its result.</param>
    /// <param name="organ">Organ value supplied to the organic plugin operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpDelete("permissions")]
    public IActionResult DeletePermission([FromQuery] string peerId, [FromQuery] string capabilityKey, [FromQuery] string organ = "") =>
        permissions.Delete(peerId, capabilityKey, organ) ? NoContent() : NotFound();

    /// <summary>
    /// Returns the work projection for the organic plugin API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("work")]
    public ActionResult<IReadOnlyList<OrganicPluginWorkItem>> Work() => Ok(work.GetWork());

    /// <summary>
    /// Returns the results projection for the organic plugin API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("results")]
    public ActionResult<IReadOnlyList<OrganicPluginWorkItem>> Results() => Ok(results.GetResults());

    /// <summary>
    /// Returns the text proposals projection for the organic plugin API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("text-proposals")]
    public ActionResult<IReadOnlyList<OrganicTextInsertionProposal>> TextProposals() => Ok(results.GetTextProposals());

    /// <summary>
    /// Returns the connect projection for the organic plugin API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("connect/{peerId}")]
    public async Task<ActionResult<OrganicConnectionState>> Connect(string peerId, CancellationToken cancellationToken)
    {
        var state = await connection.ConnectAsync(peerId, cancellationToken).ConfigureAwait(false);
        return state.IsConnected ? Ok(state) : Problem(state.LastError, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    /// <summary>
    /// Returns the disconnect projection for the organic plugin API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("disconnect")]
    public async Task<IActionResult> Disconnect()
    {
        await connection.DisconnectAsync().ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Returns the council projection for the organic plugin API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("council")]
    public async Task<IActionResult> Council([FromBody] OrganicCouncilPromptRequest request, CancellationToken cancellationToken)
    {
        if (!connection.State.IsConnected) return Conflict("PublisherStudio is not connected to LocalGPT.");
        var messageId = await connection.SendCouncilRequestAsync(request, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Submitted organic council request {MessageId} for team {TeamKey}.", messageId, request.TeamKey);
        return Accepted(new { MessageId = messageId, request.TeamKey });
    }

    /// <summary>
    /// Returns the approve projection for the organic plugin API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("work/{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        var item = await work.ApproveAsync(id, cancellationToken).ConfigureAwait(false);
        if (item is null) return NotFound();
        await connection.SendWorkResultAsync(item, cancellationToken).ConfigureAwait(false);
        return Ok(item);
    }

    /// <summary>
    /// Returns the decline projection for the organic plugin API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="decision">Decision value supplied to the organic plugin operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("work/{id:guid}/decline")]
    public async Task<IActionResult> Decline(Guid id, [FromBody] OrganicWorkDecisionRequest? decision, CancellationToken cancellationToken)
    {
        if (!work.Decline(id, decision?.Reason ?? string.Empty)) return NotFound();
        var item = work.Get(id)!;
        await connection.SendWorkResultAsync(item, cancellationToken).ConfigureAwait(false);
        return Ok(item);
    }
}
