using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.CodeEditing;

namespace PublisherStudio.Controllers;

/// <summary>
/// Provides code controller operations.
/// </summary>
[ApiController]
[Route("api/code")]
public sealed class CodeController(ICodeLanguageService languages, ICodeFormattingService formatting) : ControllerBase
{
    /// <summary>
    /// Runs the languages operation.
    /// </summary>
    [HttpGet("languages")]
    public ActionResult<IReadOnlyList<CodeLanguageProfile>> Languages() => Ok(languages.GetProfiles());

    /// <summary>
    /// Runs the detect operation.
    /// </summary>
    [HttpGet("detect")]
    public ActionResult<CodeLanguageProfile> Detect([FromQuery] string fileName, [FromQuery] string? sample = null) => Ok(languages.Detect(fileName, sample));

    /// <summary>
    /// Runs the format operation.
    /// </summary>
    [HttpPost("format")]
    public ActionResult<CodeTextResult> Format([FromBody] CodeTextRequest request) => Ok(formatting.Format(request));

    /// <summary>
    /// Runs the comment operation.
    /// </summary>
    [HttpPost("comment")]
    public ActionResult<CodeTextResult> Comment([FromBody] CodeCommentRequest request) => Ok(formatting.ToggleComment(request));

    /// <summary>
    /// Runs the analyze operation.
    /// </summary>
    [HttpPost("analyze")]
    public ActionResult<CodeTextResult> Analyze([FromBody] CodeTextRequest request) => Ok(formatting.Analyze(request));
}
