using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Services;

public interface IPublicationMarkupService
{
    string SafeFileName(string value);
    string NormalizeCssBackground(string? value);
    string SanitizePreviewHtml(string html);
}

public sealed class PublicationMarkupService(
    IPublisherRuntimePatternService runtimePatterns,
    ILogger<PublicationMarkupService> logger) : IPublicationMarkupService
{
    public string SafeFileName(string value)
    {
        try
        {
            logger.LogTrace($"Sanitizing a publication file name.");
            var invalid = Path.GetInvalidFileNameChars();
            var safe = new string((value ?? string.Empty).Select(character => invalid.Contains(character) ? '_' : character).ToArray()).Trim();
            return string.IsNullOrWhiteSpace(safe) ? "publication" : safe;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not sanitize a publication file name: {exception.Message}");
            throw;
        }
    }

    public string NormalizeCssBackground(string? value)
    {
        try
        {
            logger.LogTrace($"Normalizing a publication CSS background value.");
            if (string.IsNullOrWhiteSpace(value)) return "transparent";
            var normalized = value.Trim();
            if (normalized.Length > 512) return "transparent";
            if (normalized.IndexOfAny([';', '"', '\'', '<', '>', '{', '}']) >= 0
                || normalized.Contains("javascript:", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("expression(", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("url(", StringComparison.OrdinalIgnoreCase))
            {
                return "transparent";
            }
            return normalized;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not normalize a publication CSS background value: {exception.Message}");
            throw;
        }
    }

    public string SanitizePreviewHtml(string html)
    {
        try
        {
            logger.LogTrace($"Sanitizing publication preview HTML.");
            if (string.IsNullOrWhiteSpace(html)) return "<p></p>";
            var value = runtimePatterns.GetRegex(PublisherRuntimePattern.PublicationDangerousElements).Replace(html, string.Empty);
            value = runtimePatterns.GetRegex(PublisherRuntimePattern.PublicationEventAttribute).Replace(value, string.Empty);
            value = runtimePatterns.GetRegex(PublisherRuntimePattern.PublicationJavascriptUrl).Replace(value, "$1=\"#\"");
            return value;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not sanitize publication preview HTML: {exception.Message}");
            throw;
        }
    }
}
