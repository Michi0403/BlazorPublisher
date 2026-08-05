using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.OpenScad;

namespace PublisherStudio.Controllers;

/// <summary>
/// Provides open scad controller operations.
/// </summary>
[ApiController]
[Route("api/openscad")]
public sealed class OpenScadController(IOpenScadCatalogService catalog, IOpenScadDocumentService documents, IOpenScadVideoLayerAdapter videoLayers, IOpenScadValueFormatter values, IOpenScadNodeFactoryService nodes) : ControllerBase
{
    /// <summary>
    /// Runs the default node operation.
    /// </summary>
    [HttpGet("nodes/{kind}/default")]
    public ActionResult<OpenScadNode> DefaultNode(string kind)
    {
        try { return Ok(nodes.Create(kind)); }
        catch (ArgumentException exception) { return BadRequest(exception.Message); }
    }

    /// <summary>
    /// Runs the catalog operation.
    /// </summary>
    [HttpGet("catalog")]
    public ActionResult<IReadOnlyList<OpenScadNodeDefinition>> Catalog() => Ok(catalog.GetDefinitions());


    /// <summary>
    /// Runs the format value operation.
    /// </summary>
    [HttpPost("format-value")]
    public ActionResult<string> FormatValue([FromBody] OpenScadValue value) => Ok(values.Format(value));

    /// <summary>
    /// Runs the identifier operation.
    /// </summary>
    [HttpGet("identifier")]
    public ActionResult<string> Identifier([FromQuery] string value, [FromQuery] string fallback = "part") => Ok(values.Identifier(value, fallback));

    /// <summary>
    /// Runs the quote operation.
    /// </summary>
    [HttpGet("quote")]
    public ActionResult<string> Quote([FromQuery] string value) => Ok(values.Quote(value));

    /// <summary>
    /// Runs the example operation.
    /// </summary>
    [HttpGet("example")]
    public ActionResult<OpenScadDocument> Example() => Ok(documents.CreateExampleDocument());

    /// <summary>
    /// Runs the validate operation.
    /// </summary>
    [HttpPost("validate")]
    public ActionResult<OpenScadValidationResult> Validate([FromBody] OpenScadDocument document) => Ok(documents.Validate(document));

    /// <summary>
    /// Runs the generate operation.
    /// </summary>
    [HttpPost("generate")]
    public ActionResult<OpenScadGenerationResult> Generate([FromBody] OpenScadDocument document) => Ok(documents.Generate(document));

    /// <summary>
    /// Runs the generate video layer operation.
    /// </summary>
    [HttpPost("video-layer")]
    public ActionResult<OpenScadGenerationResult> GenerateVideoLayer([FromBody] VideoEffectLayer layer) => Ok(new OpenScadGenerationResult
    {
        Script = videoLayers.CreateScript(layer), UsesAnimation = layer.AnimateMorph, RequiresNativeRender = true
    });
}
