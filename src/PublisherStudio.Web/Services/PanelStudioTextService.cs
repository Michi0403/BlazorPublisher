using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using PublisherStudio.Services.Configuration;
using System.Globalization;
using System.Net;

namespace PublisherStudio.Services;

/// <summary>
/// Coordinates panel studio text behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="patterns">Panel studio text pattern data service dependency used by the panel studio text workflow to provide the corresponding application capability.</param>
public sealed class PanelStudioTextService(IPanelStudioTextPatternDataService patterns)
{
    /// <summary>
    /// Stores the panel studio text pattern data service dependency used by <see cref="PanelStudioTextService"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IPanelStudioTextPatternDataService _patterns = patterns;

    /// <summary>
    /// Determines whether expected interaction shutdown as part of the panel studio text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="message">Message value supplied to the panel studio text operation and used when producing its result.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool IsExpectedInteractionShutdown(string? message, ILogger logger)
    {
        try
        {
            var expected = _patterns.ShutdownPattern.IsMatch(message ?? string.Empty);
            logger.LogDebug($"{nameof(IsExpectedInteractionShutdown)} classified the interaction shutdown state as {expected}.");
            return expected;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"{nameof(IsExpectedInteractionShutdown)} failed while classifying an interaction shutdown.");
            return false;
        }
    }

    /// <summary>
    /// Reads number as part of the panel studio text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="node">Node value supplied to the panel studio text operation and used when producing its result.</param>
    /// <param name="propertyName">Property name value supplied to the panel studio text operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the panel studio text operation and used when producing its result.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <returns>The double produced by the operation.</returns>
    public double ReadNumber(System.Text.Json.JsonElement node, string propertyName, double fallback, ILogger logger)
    {
        try
        {
            return node.TryGetProperty(propertyName, out var value) && value.TryGetDouble(out var result) ? result : fallback;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"{nameof(ReadNumber)} failed for property {propertyName}.");
            return fallback;
        }
    }

    /// <summary>
    /// Performs plain text as part of the panel studio text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="markup">Markup value supplied to the panel studio text operation and used when producing its result.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <returns>The string produced by the operation.</returns>
    public string PlainText(string? markup, ILogger logger)
    {
        try
        {
            var text = _patterns.HtmlBreakPattern.Replace(markup ?? string.Empty, Environment.NewLine);
            text = _patterns.HtmlTagPattern.Replace(text, string.Empty);
            return WebUtility.HtmlDecode(text).Trim();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"{nameof(PlainText)} failed while creating a panel text preview.");
            return markup?.Trim() ?? string.Empty;
        }
    }

    /// <summary>
    /// Parses list as part of the panel studio text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the panel studio text operation and used when producing its result.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<string> ParseList(string? value, ILogger logger)
    {
        try
        {
            var items = (value ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            logger.LogDebug($"{nameof(ParseList)} parsed {items.Count} distinct values without logging their content.");
            return items;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"{nameof(ParseList)} failed; values were omitted from logs.");
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Performs safe file name as part of the panel studio text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the panel studio text operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the panel studio text operation and used when producing its result.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <returns>The string produced by the operation.</returns>
    public string SafeFileName(string? value, string fallback, ILogger logger)
    {
        try
        {
            var normalized = _patterns.UnsafeFileNamePattern.Replace(value ?? string.Empty, "-").Trim('-', '.');
            return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"{nameof(SafeFileName)} failed while normalizing a panel export file name.");
            return fallback;
        }
    }

    /// <summary>
    /// Performs as bool as part of the panel studio text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="args">Args value supplied to the panel studio text operation and used when producing its result.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool AsBool(ChangeEventArgs args, ILogger logger)
    {
        try
        {
            return args.Value is bool value ? value : bool.TryParse(args.Value?.ToString(), out var parsed) && parsed;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"{nameof(AsBool)} failed while converting a panel form value.");
            return false;
        }
    }

    /// <summary>
    /// Performs text as part of the panel studio text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="args">Args value supplied to the panel studio text operation and used when producing its result.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <returns>The string produced by the operation.</returns>
    public string Text(ChangeEventArgs args, ILogger logger)
    {
        try { return args.Value?.ToString() ?? string.Empty; }
        catch (Exception exception)
        {
            logger.LogError(exception, $"{nameof(Text)} failed while converting a panel form value.");
            return string.Empty;
        }
    }

    /// <summary>
    /// Performs invariant as part of the panel studio text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the panel studio text operation and used when producing its result.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <returns>The string produced by the operation.</returns>
    public string Invariant(double value, ILogger logger)
    {
        try { return value.ToString("0.###", CultureInfo.InvariantCulture); }
        catch (Exception exception)
        {
            logger.LogError(exception, $"{nameof(Invariant)} failed while formatting a panel number.");
            return "0";
        }
    }

    /// <summary>
    /// Performs change number as part of the panel studio text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="args">Args value supplied to the panel studio text operation and used when producing its result.</param>
    /// <param name="update">Update value supplied to the panel studio text operation and used when producing its result.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    public void ChangeNumber(ChangeEventArgs args, Action<double> update, ILogger logger)
    {
        try
        {
            if (double.TryParse(args.Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) update(value);
        }
        catch (Exception exception) { logger.LogError(exception, $"{nameof(ChangeNumber)} failed while updating a panel number."); }
    }

    /// <summary>
    /// Performs change int as part of the panel studio text service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="args">Args value supplied to the panel studio text operation and used when producing its result.</param>
    /// <param name="update">Update value supplied to the panel studio text operation and used when producing its result.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    public void ChangeInt(ChangeEventArgs args, Action<int> update, ILogger logger)
    {
        try
        {
            if (int.TryParse(args.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)) update(value);
        }
        catch (Exception exception) { logger.LogError(exception, $"{nameof(ChangeInt)} failed while updating a panel integer."); }
    }
}
