using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
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

    /// <summary>Returns the documentation routes and accessible viewer features exposed by the running application.</summary>
    [HttpGet("profile")]
    public ActionResult<PublisherDocumentationProfile> Profile()
    {
        try
        {
            var status = documentation.GetStatus();
            return Ok(new PublisherDocumentationProfile
            {
                Status = status,
                HtmlRoute = status.HtmlUrl,
                PdfRoute = status.PdfUrl
            });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reading the PublisherStudio documentation profile failed.");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Documentation profile failed");
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

    /// <summary>Serves generated DocFX HTML and supporting assets from the installed documentation root.</summary>
    /// <param name="relativePath">Optional path below the generated documentation root.</param>
    [HttpGet("/help-docs")]
    [HttpGet("/help-docs/{**relativePath}")]
    [HttpGet("html")]
    [HttpGet("html/{**relativePath}")]
    public IActionResult Html([FromRoute] string? relativePath = null)
    {
        try
        {
            var path = documentation.GetHtmlFilePath(relativePath);
            if (path is null)
                return NotFound(new { error = "Generated PublisherStudio documentation was not found in this build." });

            var contentTypes = new FileExtensionContentTypeProvider();
            if (!contentTypes.TryGetContentType(path, out var contentType))
                contentType = "application/octet-stream";
            return new PhysicalFileResult(path, contentType) { EnableRangeProcessing = true };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Serving a PublisherStudio documentation asset failed.");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Documentation asset failed");
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
