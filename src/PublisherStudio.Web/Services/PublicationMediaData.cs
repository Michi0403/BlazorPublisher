using PublisherStudio.BusinessObjects;
// logging-policy: pure-helper
namespace PublisherStudio.Services;

/// <summary>
/// Represents a publication media data.
/// </summary>
public sealed class PublicationMediaData(ILogger<PublicationMediaData> logger)
{
    /// <summary>
    /// Normalizes mime type.
    /// </summary>
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
    /// Normalizes data URL.
    /// </summary>
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
