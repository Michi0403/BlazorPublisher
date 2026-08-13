using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Automation;
using PublisherStudio.Services.OrganicPlugins;

namespace PublisherStudio.Controllers;

/// <summary>
/// Exposes the automation input application operations through PublisherStudio's web/API boundary and delegates domain work to the corresponding services.
/// </summary>
/// <param name="input">User input automation service dependency used by the automation input workflow to provide the corresponding application capability.</param>
/// <param name="connection">Local gpt connection service dependency used by the automation input workflow to provide the corresponding application capability.</param>
[ApiController]
[Route("api/automation/input")]
public sealed class AutomationInputController(IUserInputAutomationService input, ILocalGptConnectionService connection) : ControllerBase
{
    /// <summary>
    /// Returns the list projection for the automation input API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet]
    public ActionResult<IReadOnlyList<BrowserAutomationCommand>> List() => Ok(input.GetAll());

    /// <summary>
    /// Returns the enqueue projection for the automation input API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="command">Command value supplied to the automation input operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost]
    public ActionResult<BrowserAutomationCommand> Enqueue([FromBody] BrowserAutomationCommand command)
    {
        if (!connection.State.IsLinked) return Conflict("Browser automation requires an explicitly linked LocalGPT peer.");
        return Accepted(input.Enqueue(command));
    }

    /// <summary>
    /// Returns the pending projection for the automation input API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="maximum">Maximum value supplied to the automation input operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("pending")]
    public ActionResult<IReadOnlyList<BrowserAutomationCommand>> Pending([FromQuery] int maximum = 25) =>
        connection.State.IsLinked ? Ok(input.ClaimPending(maximum)) : Ok(Array.Empty<BrowserAutomationCommand>());

    /// <summary>
    /// Returns the complete projection for the automation input API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("{id:guid}/complete")]
    public IActionResult Complete(Guid id, [FromBody] AutomationCompletion completion) => input.Complete(id, completion) ? NoContent() : NotFound();

    /// <summary>
    /// Determines whether cel for the automation input API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpDelete("{id:guid}")]
    public IActionResult Cancel(Guid id) => input.Cancel(id) ? NoContent() : NotFound();
}

/// <summary>
/// Exposes the automation screenshot application operations through PublisherStudio's web/API boundary and delegates domain work to the corresponding services.
/// </summary>
/// <param name="screenshots">Screenshot capture service dependency used by the automation screenshot workflow to provide the corresponding application capability.</param>
/// <param name="connection">Local gpt connection service dependency used by the automation screenshot workflow to provide the corresponding application capability.</param>
[ApiController]
[Route("api/automation/screenshots")]
public sealed class AutomationScreenshotController(IScreenshotCaptureService screenshots, ILocalGptConnectionService connection) : ControllerBase
{
    /// <summary>
    /// Returns the list projection for the automation screenshot API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet]
    public ActionResult<IReadOnlyList<BrowserScreenshotRequest>> List() => Ok(screenshots.GetAll());

    /// <summary>
    /// Returns the enqueue projection for the automation screenshot API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost]
    public ActionResult<BrowserScreenshotRequest> Enqueue([FromBody] BrowserScreenshotRequest request)
    {
        if (!connection.State.IsLinked) return Conflict("Browser screenshot automation requires an explicitly linked LocalGPT peer.");
        return Accepted(screenshots.Enqueue(request));
    }

    /// <summary>
    /// Returns the pending projection for the automation screenshot API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="maximum">Maximum value supplied to the automation screenshot operation and used when producing its result.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("pending")]
    public ActionResult<IReadOnlyList<BrowserScreenshotRequest>> Pending([FromQuery] int maximum = 5) =>
        connection.State.IsLinked ? Ok(screenshots.ClaimPending(maximum)) : Ok(Array.Empty<BrowserScreenshotRequest>());

    /// <summary>
    /// Returns the complete projection for the automation screenshot API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("{id:guid}/complete")]
    [DisableRequestSizeLimit]
    public IActionResult Complete(Guid id, [FromBody] ScreenshotCompletion completion) => screenshots.Complete(id, completion) ? NoContent() : NotFound();

    /// <summary>
    /// Returns the get projection for the automation screenshot API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("{id:guid}")]
    public ActionResult<BrowserScreenshotRequest> Get(Guid id) => screenshots.TryGet(id, out var request) ? Ok(request) : NotFound();

    /// <summary>
    /// Returns the download projection for the automation screenshot API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("{id:guid}/file")]
    public IActionResult Download(Guid id)
    {
        if (!screenshots.TryGet(id, out var request) || request.Status != AutomationRequestStatus.Completed || string.IsNullOrWhiteSpace(request.DataUrl)) return NotFound();
        var separator = request.DataUrl.IndexOf(',');
        if (separator < 0) return BadRequest("The browser returned an invalid screenshot data URL.");
        var metadata = request.DataUrl[..separator];
        var mimeType = metadata.Split(';', 2)[0].Replace("data:", string.Empty, StringComparison.OrdinalIgnoreCase);
        var bytes = Convert.FromBase64String(request.DataUrl[(separator + 1)..]);
        return File(bytes, mimeType, $"PublisherStudio-{id}.{(mimeType.Contains("jpeg", StringComparison.OrdinalIgnoreCase) ? "jpg" : "png")}");
    }

    /// <summary>
    /// Determines whether cel for the automation screenshot API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpDelete("{id:guid}")]
    public IActionResult Cancel(Guid id) => screenshots.Cancel(id) ? NoContent() : NotFound();
}


/// <summary>
/// Exposes the automation runtime application operations through PublisherStudio's web/API boundary and delegates domain work to the corresponding services.
/// </summary>
/// <param name="connection">Local gpt connection service dependency used by the automation runtime workflow to provide the corresponding application capability.</param>
[ApiController]
[Route("api/automation/runtime")]
public sealed class AutomationRuntimeController(ILocalGptConnectionService connection) : ControllerBase
{
    /// <summary>
    /// Runs the status operation.
    /// </summary>
    [HttpGet("status")]
    public IActionResult Status() => Ok(new
    {
        Linked = connection.State.IsLinked,
        Connected = connection.State.IsConnected,
        PeerId = connection.State.PeerId
    });
}

/// <summary>
/// Exposes the domain context application operations through PublisherStudio's web/API boundary and delegates domain work to the corresponding services.
/// </summary>
/// <param name="context">Business object context service dependency used by the domain context workflow to provide the corresponding application capability.</param>
[ApiController]
[Route("api/domain-context")]
public sealed class DomainContextController(IBusinessObjectContextService context) : ControllerBase
{
    /// <summary>
    /// Returns the get projection for the domain context API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet]
    public ActionResult<BusinessObjectContextSnapshot> Get() => Ok(context.CreateSnapshot());
}
