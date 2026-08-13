using PublisherStudio.BusinessObjects;
using System.Text.Json;
using System.Text.Json.Serialization;

// logging-policy: pure-helper
namespace PublisherStudio.Services;

/// <summary>
/// Represents a publication animation data application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
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
    /// Performs animations for <see cref="PublicationAnimationData"/>, keeping the operation consistent with the state and invariants of the surrounding publication animation data workflow.
    /// </summary>
    /// <param name="element">Element value supplied to the publication animation data operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
    /// Performs interaction for <see cref="PublicationAnimationData"/>, keeping the operation consistent with the state and invariants of the surrounding publication animation data workflow.
    /// </summary>
    /// <param name="element">Element value supplied to the publication animation data operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
    /// Performs signal for <see cref="PublicationAnimationData"/>, keeping the operation consistent with the state and invariants of the surrounding publication animation data workflow.
    /// </summary>
    /// <param name="connector">Connector value supplied to the publication animation data operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
