using PublisherStudio.BusinessObjects;
// logging-policy: pure-helper
namespace PublisherStudio.Services;

public sealed class PublicationMediaData(ILogger<PublicationMediaData> logger)
{
    public string NormalizeMimeType(string? mimeType, string fallback)
    {
        logger.LogTrace("Normalizing publication media MIME type.");
        var value = mimeType?.Trim() ?? string.Empty;
        var separator = value.IndexOf(';');
        if (separator >= 0) value = value[..separator].Trim();
        return value.Contains('/') ? value.ToLowerInvariant() : fallback;
    }

    public string NormalizeDataUrl(string? dataUrl, string fallbackMimeType)
    {
        if (string.IsNullOrWhiteSpace(dataUrl) || !dataUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return dataUrl ?? string.Empty;

        var marker = dataUrl.LastIndexOf(";base64,", StringComparison.OrdinalIgnoreCase);
        if (marker < 0) return dataUrl;

        var header = dataUrl.Substring(5, marker - 5);
        var mimeType = NormalizeMimeType(header, fallbackMimeType);
        return $"data:{mimeType};base64,{dataUrl[(marker + 8)..]}";
    }
}
