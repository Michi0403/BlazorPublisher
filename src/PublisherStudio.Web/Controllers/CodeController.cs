using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.CodeEditing;

namespace PublisherStudio.Controllers;

/// <summary>
/// Exposes the code application operations through PublisherStudio's web/API boundary and delegates domain work to the corresponding services.
/// </summary>
/// <param name="languages">Code language service dependency used by the code workflow to provide the corresponding application capability.</param>
/// <param name="formatting">Code formatting service dependency used by the code workflow to provide the corresponding application capability.</param>
[ApiController]
[Route("api/code")]
public sealed class CodeController(ICodeLanguageService languages, ICodeFormattingService formatting) : ControllerBase
{
    /// <summary>
    /// Returns the languages projection for the code API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpGet("languages")]
    public ActionResult<IReadOnlyList<CodeLanguageProfile>> Languages() => Ok(languages.GetProfiles());

    /// <summary>
    /// Returns the detect projection for the code API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    /// <param name="fileName">File name value supplied to the code operation and used when producing its result.</param>
    /// <param name="sample">Sample value supplied to the code operation and used when producing its result.</param>
    [HttpGet("detect")]
    public ActionResult<CodeLanguageProfile> Detect([FromQuery] string fileName, [FromQuery] string? sample = null) => Ok(languages.Detect(fileName, sample));

    /// <summary>
    /// Returns the format projection for the code API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    [HttpPost("format")]
    public ActionResult<CodeTextResult> Format([FromBody] CodeTextRequest request) => Ok(formatting.Format(request));

    /// <summary>
    /// Returns the comment projection for the code API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    [HttpPost("comment")]
    public ActionResult<CodeTextResult> Comment([FromBody] CodeCommentRequest request) => Ok(formatting.ToggleComment(request));

    /// <summary>
    /// Returns the analyze projection for the code API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <returns>The HTTP-facing result produced for the caller.</returns>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    [HttpPost("analyze")]
    public ActionResult<CodeTextResult> Analyze([FromBody] CodeTextRequest request) => Ok(formatting.Analyze(request));
}
