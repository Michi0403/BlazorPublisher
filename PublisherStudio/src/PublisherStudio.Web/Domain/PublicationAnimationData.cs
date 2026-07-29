using System.Text.Json;
using System.Text.Json.Serialization;

namespace PublisherStudio.Domain;

public sealed class PublicationAnimationData(ILogger<PublicationAnimationData> logger)
{
    private readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public string Animations(PublicationElement element) => JsonSerializer.Serialize(
        element.Animations.OrderBy(item => item.Order), Options);

    public string Interaction(PublicationElement element) => JsonSerializer.Serialize(
        element.Interaction ?? new PublicationInteraction(), Options);

    public string Signal(ConnectorElement connector) => JsonSerializer.Serialize(
        connector.Signal ?? new SignalConnectorSettings(), Options);

}
