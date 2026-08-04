using PublisherStudio.BusinessObjects;
using System.Text.Json;
using System.Text.Json.Serialization;

// logging-policy: pure-helper
namespace PublisherStudio.Services;

public sealed class PublicationAnimationData(ILogger<PublicationAnimationData> logger)
{
    private readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public string Animations(PublicationElement element)
    {
        logger.LogTrace("Serializing publication animations for element {ElementId}.", element.Id);
        return JsonSerializer.Serialize(
        element.Animations.OrderBy(item => item.Order), Options);
    }

    public string Interaction(PublicationElement element) => JsonSerializer.Serialize(
        element.Interaction ?? new PublicationInteraction(), Options);

    public string Signal(ConnectorElement connector) => JsonSerializer.Serialize(
        connector.Signal ?? new SignalConnectorSettings(), Options);

}
