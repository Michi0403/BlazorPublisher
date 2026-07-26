using Microsoft.AspNetCore.Mvc;
using PublisherStudio.Domain;
using PublisherStudio.Services.CodeEditing;

namespace PublisherStudio.Controllers;

[ApiController]
[Route("api/code")]
public sealed class CodeController(ICodeLanguageService languages, ICodeFormattingService formatting) : ControllerBase
{
    [HttpGet("languages")]
    public ActionResult<IReadOnlyList<CodeLanguageProfile>> Languages() => Ok(languages.GetProfiles());

    [HttpGet("detect")]
    public ActionResult<CodeLanguageProfile> Detect([FromQuery] string fileName, [FromQuery] string? sample = null) => Ok(languages.Detect(fileName, sample));

    [HttpPost("format")]
    public ActionResult<CodeTextResult> Format([FromBody] CodeTextRequest request) => Ok(formatting.Format(request));

    [HttpPost("comment")]
    public ActionResult<CodeTextResult> Comment([FromBody] CodeCommentRequest request) => Ok(formatting.ToggleComment(request));

    [HttpPost("analyze")]
    public ActionResult<CodeTextResult> Analyze([FromBody] CodeTextRequest request) => Ok(formatting.Analyze(request));
}
