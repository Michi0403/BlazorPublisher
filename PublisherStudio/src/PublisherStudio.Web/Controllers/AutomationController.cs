using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Automation;
using PublisherStudio.Services.OrganicPlugins;

namespace PublisherStudio.Controllers;

[ApiController]
[Route("api/automation/input")]
public sealed class AutomationInputController(IUserInputAutomationService input, ILocalGptConnectionService connection) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<BrowserAutomationCommand>> List() => Ok(input.GetAll());

    [HttpPost]
    public ActionResult<BrowserAutomationCommand> Enqueue([FromBody] BrowserAutomationCommand command)
    {
        if (!connection.State.IsLinked) return Conflict("Browser automation requires an explicitly linked LocalGPT peer.");
        return Accepted(input.Enqueue(command));
    }

    [HttpGet("pending")]
    public ActionResult<IReadOnlyList<BrowserAutomationCommand>> Pending([FromQuery] int maximum = 25) =>
        connection.State.IsLinked ? Ok(input.ClaimPending(maximum)) : Ok(Array.Empty<BrowserAutomationCommand>());

    [HttpPost("{id:guid}/complete")]
    public IActionResult Complete(Guid id, [FromBody] AutomationCompletion completion) => input.Complete(id, completion) ? NoContent() : NotFound();

    [HttpDelete("{id:guid}")]
    public IActionResult Cancel(Guid id) => input.Cancel(id) ? NoContent() : NotFound();
}

[ApiController]
[Route("api/automation/screenshots")]
public sealed class AutomationScreenshotController(IScreenshotCaptureService screenshots, ILocalGptConnectionService connection) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<BrowserScreenshotRequest>> List() => Ok(screenshots.GetAll());

    [HttpPost]
    public ActionResult<BrowserScreenshotRequest> Enqueue([FromBody] BrowserScreenshotRequest request)
    {
        if (!connection.State.IsLinked) return Conflict("Browser screenshot automation requires an explicitly linked LocalGPT peer.");
        return Accepted(screenshots.Enqueue(request));
    }

    [HttpGet("pending")]
    public ActionResult<IReadOnlyList<BrowserScreenshotRequest>> Pending([FromQuery] int maximum = 5) =>
        connection.State.IsLinked ? Ok(screenshots.ClaimPending(maximum)) : Ok(Array.Empty<BrowserScreenshotRequest>());

    [HttpPost("{id:guid}/complete")]
    [DisableRequestSizeLimit]
    public IActionResult Complete(Guid id, [FromBody] ScreenshotCompletion completion) => screenshots.Complete(id, completion) ? NoContent() : NotFound();

    [HttpGet("{id:guid}")]
    public ActionResult<BrowserScreenshotRequest> Get(Guid id) => screenshots.TryGet(id, out var request) ? Ok(request) : NotFound();

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

    [HttpDelete("{id:guid}")]
    public IActionResult Cancel(Guid id) => screenshots.Cancel(id) ? NoContent() : NotFound();
}


[ApiController]
[Route("api/automation/runtime")]
public sealed class AutomationRuntimeController(ILocalGptConnectionService connection) : ControllerBase
{
    [HttpGet("status")]
    public IActionResult Status() => Ok(new
    {
        Linked = connection.State.IsLinked,
        Connected = connection.State.IsConnected,
        PeerId = connection.State.PeerId
    });
}

[ApiController]
[Route("api/domain-context")]
public sealed class DomainContextController(IBusinessObjectContextService context) : ControllerBase
{
    [HttpGet]
    public ActionResult<BusinessObjectContextSnapshot> Get() => Ok(context.CreateSnapshot());
}
