using PublisherStudio.BusinessObjects;
using System.Text.Json;
using System.Text.Json.Serialization;

// logging-policy: pure-helper
namespace PublisherStudio.Services;

/// <summary>
/// Represents a publication animation data.
/// </summary>
public sealed class PublicationAnimationData(ILogger<PublicationAnimationData> logger)
{
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Runs the animations operation.
    /// </summary>
    public string Animations(PublicationElement element)
    {
    try
    {
            logger.LogTrace("Serializing publication animations for element {ElementId}.", element.Id);
            return JsonSerializer.Serialize(
            element.Animations.OrderBy(item => item.Order), Options);
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublicationAnimationData)}.{nameof(Animations)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublicationAnimationData)}.{nameof(Animations)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the interaction operation.
    /// </summary>
    public string Interaction(PublicationElement element) {
    try
    {
        return JsonSerializer.Serialize(
        element.Interaction ?? new PublicationInteraction(), Options);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublicationAnimationData)}.{nameof(Interaction)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublicationAnimationData)}.{nameof(Interaction)} failed.");
        throw;
    }
}

    /// <summary>
    /// Runs the signal operation.
    /// </summary>
    public string Signal(ConnectorElement connector) {
    try
    {
        return JsonSerializer.Serialize(
        connector.Signal ?? new SignalConnectorSettings(), Options);
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublicationAnimationData)}.{nameof(Signal)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublicationAnimationData)}.{nameof(Signal)} failed.");
        throw;
    }
}

}
