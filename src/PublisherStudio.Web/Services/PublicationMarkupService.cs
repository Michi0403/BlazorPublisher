using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Services;

/// <summary>
/// Defines the contract for publication markup behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IPublicationMarkupService
{
    /// <summary>
    /// Performs safe file name as part of the publication markup service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the publication markup operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string SafeFileName(string value);
    /// <summary>
    /// Normalizes CSS background as part of the publication markup service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the publication markup operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string NormalizeCssBackground(string? value);
    /// <summary>
    /// Performs sanitize preview HTML as part of the publication markup service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="html">Html value supplied to the publication markup operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    string SanitizePreviewHtml(string html);
}

/// <summary>
/// Coordinates publication markup behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="runtimePatterns">Publisher runtime pattern service dependency used by the publication markup workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class PublicationMarkupService(
    IPublisherRuntimePatternService runtimePatterns,
    ILogger<PublicationMarkupService> logger) : IPublicationMarkupService
{
    /// <summary>
    /// Performs safe file name as part of the publication markup service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the publication markup operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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

    /// <summary>
    /// Normalizes CSS background as part of the publication markup service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the publication markup operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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

    /// <summary>
    /// Performs sanitize preview HTML as part of the publication markup service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="html">Html value supplied to the publication markup operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
