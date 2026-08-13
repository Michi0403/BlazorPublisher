using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Controllers;

/// <summary>
/// Exposes the runtime policy application operations through PublisherStudio's web/API boundary and delegates domain work to the corresponding services.
/// </summary>
/// <param name="runtimePolicy">Publisher runtime policy data service dependency used by the runtime policy workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
[ApiController]
[Route("api/runtime-policy")]
public sealed class RuntimePolicyController(
    IPublisherRuntimePolicyDataService runtimePolicy,
    ILogger<RuntimePolicyController> logger) : ControllerBase
{
    /// <summary>
    /// Returns the get projection for the runtime policy API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet]
    public ActionResult<PublisherRuntimePolicySnapshot> Get()
    {
        try
        {
            var snapshot = runtimePolicy.GetSnapshot();
            logger.LogDebug($"Returned the PublisherStudio runtime policy controller snapshot.");
            return Ok(snapshot);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not return the PublisherStudio runtime policy controller snapshot.");
            return Problem(ex.Message);
        }
    }
}
