using Microsoft.AspNetCore.Mvc;
using PublisherStudio.Domain;
using PublisherStudio.Services.Automation;

namespace PublisherStudio.Controllers;

[ApiController]
[Route("api/automation/input")]
public sealed class AutomationInputController(IUserInputAutomationService input) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<BrowserAutomationCommand>> List() => Ok(input.GetAll());

    [HttpPost]
    public ActionResult<BrowserAutomationCommand> Enqueue([FromBody] BrowserAutomationCommand command) => Accepted(input.Enqueue(command));

    [HttpGet("pending")]
    public ActionResult<IReadOnlyList<BrowserAutomationCommand>> Pending([FromQuery] int maximum = 25) => Ok(input.ClaimPending(maximum));

    [HttpPost("{id:guid}/complete")]
    public IActionResult Complete(Guid id, [FromBody] AutomationCompletion completion) => input.Complete(id, completion) ? NoContent() : NotFound();

    [HttpDelete("{id:guid}")]
    public IActionResult Cancel(Guid id) => input.Cancel(id) ? NoContent() : NotFound();
}

[ApiController]
[Route("api/automation/screenshots")]
public sealed class AutomationScreenshotController(IScreenshotCaptureService screenshots) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<BrowserScreenshotRequest>> List() => Ok(screenshots.GetAll());

    [HttpPost]
    public ActionResult<BrowserScreenshotRequest> Enqueue([FromBody] BrowserScreenshotRequest request) => Accepted(screenshots.Enqueue(request));

    [HttpGet("pending")]
    public ActionResult<IReadOnlyList<BrowserScreenshotRequest>> Pending([FromQuery] int maximum = 5) => Ok(screenshots.ClaimPending(maximum));

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
[Route("api/domain-context")]
public sealed class DomainContextController(IBusinessObjectContextService context) : ControllerBase
{
    [HttpGet]
    public ActionResult<BusinessObjectContextSnapshot> Get() => Ok(context.CreateSnapshot());
}
