using Microsoft.AspNetCore.Mvc;
using PublisherStudio.Domain;
using PublisherStudio.Services.OrganicPlugins;

namespace PublisherStudio.Controllers;

[ApiController]
[Route("api/organic")]
public sealed class OrganicPluginController(
    ILocalGptDiscoveryRegistry discovery,
    IOrganicCapabilityCatalog capabilities,
    IOrganicPermissionStore permissions,
    IOrganicWorkCoordinator work,
    IOrganicResultStore results,
    ILocalGptConnectionService connection,
    ILogger<OrganicPluginController> logger) : ControllerBase
{
    [HttpGet("status")]
    public ActionResult<OrganicConnectionState> Status() => Ok(connection.State);

    [HttpGet("peers")]
    public ActionResult<IReadOnlyList<OrganicPeerAdvertisement>> Peers() => Ok(discovery.GetPeers());

    [HttpGet("capabilities")]
    public async Task<ActionResult<IReadOnlyList<OrganicCapabilityDescriptor>>> Capabilities(CancellationToken cancellationToken) =>
        Ok(await capabilities.GetCapabilitiesAsync(cancellationToken));

    [HttpGet("permissions")]
    public ActionResult<IReadOnlyList<OrganicPermissionRule>> Permissions() => Ok(permissions.GetRules());

    [HttpPost("permissions")]
    public ActionResult<OrganicPermissionRule> SavePermission([FromBody] OrganicPermissionRule rule) => Ok(permissions.Save(rule));

    [HttpDelete("permissions")]
    public IActionResult DeletePermission([FromQuery] string peerId, [FromQuery] string capabilityKey, [FromQuery] string organ = "") =>
        permissions.Delete(peerId, capabilityKey, organ) ? NoContent() : NotFound();

    [HttpGet("work")]
    public ActionResult<IReadOnlyList<OrganicPluginWorkItem>> Work() => Ok(work.GetWork());

    [HttpGet("results")]
    public ActionResult<IReadOnlyList<OrganicPluginWorkItem>> Results() => Ok(results.GetResults());

    [HttpGet("text-proposals")]
    public ActionResult<IReadOnlyList<OrganicTextInsertionProposal>> TextProposals() => Ok(results.GetTextProposals());

    [HttpPost("connect/{peerId}")]
    public async Task<ActionResult<OrganicConnectionState>> Connect(string peerId, CancellationToken cancellationToken)
    {
        var state = await connection.ConnectAsync(peerId, cancellationToken);
        return state.IsConnected ? Ok(state) : Problem(state.LastError, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    [HttpPost("disconnect")]
    public async Task<IActionResult> Disconnect()
    {
        await connection.DisconnectAsync();
        return NoContent();
    }

    [HttpPost("council")]
    public async Task<IActionResult> Council([FromBody] OrganicCouncilPromptRequest request, CancellationToken cancellationToken)
    {
        if (!connection.State.IsConnected) return Conflict("PublisherStudio is not connected to LocalGPT.");
        var messageId = await connection.SendCouncilRequestAsync(request, cancellationToken);
        logger.LogInformation("Submitted organic council request {MessageId} for team {TeamKey}.", messageId, request.TeamKey);
        return Accepted(new { MessageId = messageId, request.TeamKey });
    }

    [HttpPost("work/{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        var item = await work.ApproveAsync(id, cancellationToken);
        if (item is null) return NotFound();
        await connection.SendWorkResultAsync(item, cancellationToken);
        return Ok(item);
    }

    [HttpPost("work/{id:guid}/decline")]
    public async Task<IActionResult> Decline(Guid id, [FromBody] OrganicWorkDecisionRequest? decision, CancellationToken cancellationToken)
    {
        if (!work.Decline(id, decision?.Reason ?? string.Empty)) return NotFound();
        var item = work.Get(id)!;
        await connection.SendWorkResultAsync(item, cancellationToken);
        return Ok(item);
    }
}
