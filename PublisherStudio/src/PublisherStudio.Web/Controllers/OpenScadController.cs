using Microsoft.AspNetCore.Mvc;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.OpenScad;

namespace PublisherStudio.Controllers;

[ApiController]
[Route("api/openscad")]
public sealed class OpenScadController(IOpenScadCatalogService catalog, IOpenScadDocumentService documents, IOpenScadVideoLayerAdapter videoLayers, IOpenScadValueFormatter values, IOpenScadNodeFactoryService nodes) : ControllerBase
{
    [HttpGet("nodes/{kind}/default")]
    public ActionResult<OpenScadNode> DefaultNode(string kind)
    {
        try { return Ok(nodes.Create(kind)); }
        catch (ArgumentException exception) { return BadRequest(exception.Message); }
    }

    [HttpGet("catalog")]
    public ActionResult<IReadOnlyList<OpenScadNodeDefinition>> Catalog() => Ok(catalog.GetDefinitions());


    [HttpPost("format-value")]
    public ActionResult<string> FormatValue([FromBody] OpenScadValue value) => Ok(values.Format(value));

    [HttpGet("identifier")]
    public ActionResult<string> Identifier([FromQuery] string value, [FromQuery] string fallback = "part") => Ok(values.Identifier(value, fallback));

    [HttpGet("quote")]
    public ActionResult<string> Quote([FromQuery] string value) => Ok(values.Quote(value));

    [HttpGet("example")]
    public ActionResult<OpenScadDocument> Example() => Ok(documents.CreateExampleDocument());

    [HttpPost("validate")]
    public ActionResult<OpenScadValidationResult> Validate([FromBody] OpenScadDocument document) => Ok(documents.Validate(document));

    [HttpPost("generate")]
    public ActionResult<OpenScadGenerationResult> Generate([FromBody] OpenScadDocument document) => Ok(documents.Generate(document));

    [HttpPost("video-layer")]
    public ActionResult<OpenScadGenerationResult> GenerateVideoLayer([FromBody] VideoEffectLayer layer) => Ok(new OpenScadGenerationResult
    {
        Script = videoLayers.CreateScript(layer), UsesAnimation = layer.AnimateMorph, RequiresNativeRender = true
    });
}
