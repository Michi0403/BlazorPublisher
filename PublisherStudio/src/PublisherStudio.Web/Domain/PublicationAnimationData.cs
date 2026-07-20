using System.Text.Json;
using System.Text.Json.Serialization;

namespace PublisherStudio.Domain;

public static class PublicationAnimationData
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Animations(PublicationElement element) => JsonSerializer.Serialize(
        element.Animations.OrderBy(item => item.Order), Options);

    public static string Interaction(PublicationElement element) => JsonSerializer.Serialize(
        element.Interaction ?? new PublicationInteraction(), Options);

    public static string Signal(ConnectorElement connector) => JsonSerializer.Serialize(
        connector.Signal ?? new SignalConnectorSettings(), Options);

}
