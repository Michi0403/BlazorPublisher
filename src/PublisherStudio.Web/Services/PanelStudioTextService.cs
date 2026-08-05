using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using PublisherStudio.Services.Configuration;
using System.Globalization;
using System.Net;

namespace PublisherStudio.Services;

/// <summary>
/// Provides panel studio text service operations.
/// </summary>
public sealed class PanelStudioTextService(IPanelStudioTextPatternDataService patterns)
{
    private readonly IPanelStudioTextPatternDataService _patterns = patterns;

    /// <summary>
    /// Determines whether expected interaction shutdown.
    /// </summary>
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
    /// Reads number.
    /// </summary>
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
    /// Runs the plain text operation.
    /// </summary>
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
    /// Parses list.
    /// </summary>
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
    /// Runs the safe file name operation.
    /// </summary>
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
    /// Runs the as bool operation.
    /// </summary>
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
    /// Runs the text operation.
    /// </summary>
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
    /// Runs the invariant operation.
    /// </summary>
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
    /// Runs the change number operation.
    /// </summary>
    public void ChangeNumber(ChangeEventArgs args, Action<double> update, ILogger logger)
    {
        try
        {
            if (double.TryParse(args.Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) update(value);
        }
        catch (Exception exception) { logger.LogError(exception, $"{nameof(ChangeNumber)} failed while updating a panel number."); }
    }

    /// <summary>
    /// Runs the change int operation.
    /// </summary>
    public void ChangeInt(ChangeEventArgs args, Action<int> update, ILogger logger)
    {
        try
        {
            if (int.TryParse(args.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)) update(value);
        }
        catch (Exception exception) { logger.LogError(exception, $"{nameof(ChangeInt)} failed while updating a panel integer."); }
    }
}
