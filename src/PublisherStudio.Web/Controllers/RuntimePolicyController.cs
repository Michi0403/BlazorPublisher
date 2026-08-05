using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Controllers;

/// <summary>
/// Provides runtime policy controller operations.
/// </summary>
[ApiController]
[Route("api/runtime-policy")]
public sealed class RuntimePolicyController(
    IPublisherRuntimePolicyDataService runtimePolicy,
    ILogger<RuntimePolicyController> logger) : ControllerBase
{
    /// <summary>
    /// Runs the get operation.
    /// </summary>
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
