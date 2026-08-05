using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Documentation;

namespace PublisherStudio.Controllers;

/// <summary>
/// Exposes generated PublisherStudio documentation, status information, and searchable XML comments.
/// </summary>
[ApiController]
[Route("api/documentation")]
public sealed class DocumentationController(
    IPublisherDocumentationCatalogService documentation,
    ILogger<DocumentationController> logger) : ControllerBase
{

    /// <summary>Returns availability and generation details for the documentation shipped with the running build.</summary>
    [HttpGet("status")]
    public ActionResult<PublisherDocumentationStatus> Status()
    {
        try
        {
            return Ok(documentation.GetStatus());
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading the PublisherStudio documentation status failed.");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Documentation status failed");
        }
    }

    /// <summary>Searches the compiler XML documentation shipped with the running build.</summary>
    /// <param name="query">Optional case-insensitive member, summary, or remarks text.</param>
    /// <param name="limit">Maximum number of matching members to return.</param>
    [HttpGet("comments")]
    public ActionResult<IReadOnlyList<PublisherDocumentationComment>> Comments(
        [FromQuery] string? query = null,
        [FromQuery] int limit = 100)
    {
        try
        {
            return Ok(documentation.SearchComments(query, limit));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Searching PublisherStudio XML documentation failed.");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Documentation search failed");
        }
    }

    /// <summary>Opens the versioned PublisherStudio PDF generated for the running build.</summary>
    [HttpGet("pdf")]
    public IActionResult Pdf()
    {
        try
        {
            var path = documentation.GetPdfPath();
            if (path is null) return NotFound(new { error = "The documentation PDF is not available for this build." });
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{Path.GetFileName(path)}\"";
            return new PhysicalFileResult(path, "application/pdf") { EnableRangeProcessing = true };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Opening the PublisherStudio documentation PDF failed.");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Documentation PDF failed");
        }
    }
}
