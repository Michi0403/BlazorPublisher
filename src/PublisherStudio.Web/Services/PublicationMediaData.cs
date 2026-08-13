using PublisherStudio.BusinessObjects;
// logging-policy: pure-helper
namespace PublisherStudio.Services;

/// <summary>
/// Represents a publication media data application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class PublicationMediaData(ILogger<PublicationMediaData> logger)
{
    /// <summary>
    /// Normalizes MIME type for <see cref="PublicationMediaData"/>, keeping the operation consistent with the state and invariants of the surrounding publication media data workflow.
    /// </summary>
    /// <param name="mimeType">Mime type value supplied to the publication media data operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the publication media data operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string NormalizeMimeType(string? mimeType, string fallback)
    {
    try
    {
            logger.LogTrace("Normalizing publication media MIME type.");
            var value = mimeType?.Trim() ?? string.Empty;
            var separator = value.IndexOf(';');
            if (separator >= 0) value = value[..separator].Trim();
            return value.Contains('/') ? value.ToLowerInvariant() : fallback;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublicationMediaData)}.{nameof(NormalizeMimeType)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublicationMediaData)}.{nameof(NormalizeMimeType)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes data URL for <see cref="PublicationMediaData"/>, keeping the operation consistent with the state and invariants of the surrounding publication media data workflow.
    /// </summary>
    /// <param name="dataUrl">Data url value supplied to the publication media data operation and used when producing its result.</param>
    /// <param name="fallbackMimeType">Fallback mime type value supplied to the publication media data operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string NormalizeDataUrl(string? dataUrl, string fallbackMimeType)
    {
    try
    {
            if (string.IsNullOrWhiteSpace(dataUrl) || !dataUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                return dataUrl ?? string.Empty;

            var marker = dataUrl.LastIndexOf(";base64,", StringComparison.OrdinalIgnoreCase);
            if (marker < 0) return dataUrl;

            var header = dataUrl.Substring(5, marker - 5);
            var mimeType = NormalizeMimeType(header, fallbackMimeType);
            return $"data:{mimeType};base64,{dataUrl[(marker + 8)..]}";
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublicationMediaData)}.{nameof(NormalizeDataUrl)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublicationMediaData)}.{nameof(NormalizeDataUrl)} failed.");
        throw;
    }
}
}
