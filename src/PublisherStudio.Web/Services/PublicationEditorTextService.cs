using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services;

/// <summary>
/// Owns compact text keys used by the publication editor when synchronizing browser-side canvas state.
/// </summary>
public sealed class PublicationEditorTextService
{
    /// <summary>
    /// Stores the logger used to record failures while deterministic editor text keys are assembled.
    /// </summary>
    private readonly ILogger<PublicationEditorTextService> _logger;

    /// <summary>
    /// Initializes a new <see cref="PublicationEditorTextService"/> instance.
    /// </summary>
    /// <param name="logger">Logger used to record diagnostics produced while editor text keys are created.</param>
    public PublicationEditorTextService(ILogger<PublicationEditorTextService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Builds the browser-safe ordered publication element identifier array used by canvas initialization.
    /// </summary>
    /// <param name="selectionVisualsEnabled">Whether mainframe selection visuals are currently enabled.</param>
    /// <param name="selectedElementIds">Selected publication element identifiers.</param>
    /// <returns>Deterministically ordered identifier strings, or an empty array while selection visuals are suspended.</returns>
    public string[] BuildCanvasSelectedElementIds(bool selectionVisualsEnabled, IEnumerable<Guid>? selectedElementIds)
    {
        try
        {
            if (!selectionVisualsEnabled) return [];
            return (selectedElementIds ?? Array.Empty<Guid>())
                .OrderBy(id => id)
                .Select(id => id.ToString("D"))
                .ToArray();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Building the publication canvas selected-element identifier array failed; selected identifiers were omitted from diagnostics.");
            return [];
        }
    }

    /// <summary>
    /// Builds the stable selection portion of the publication canvas initialization key.
    /// </summary>
    /// <param name="selectionVisualsEnabled">Whether mainframe selection visuals are currently enabled.</param>
    /// <param name="interactionEnabled">Whether mainframe canvas interaction is currently enabled.</param>
    /// <param name="selectedElementIds">Already-normalized selected publication element identifier strings.</param>
    /// <returns>A deterministic key that changes when canvas selection or interaction state changes.</returns>
    public string BuildCanvasSelectionKey(bool selectionVisualsEnabled, bool interactionEnabled, IReadOnlyList<string>? selectedElementIds)
    {
        try
        {
            return $"{selectionVisualsEnabled}|{interactionEnabled}|{string.Join(",", selectedElementIds ?? Array.Empty<string>())}";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Building the publication canvas selection key failed; selected identifiers were omitted from diagnostics.");
            return $"{selectionVisualsEnabled}|{interactionEnabled}|";
        }
    }
    /// <summary>
    /// Expands a camel- or Pascal-cased identifier into a compact user-facing label.
    /// </summary>
    /// <param name="value">Identifier text to humanize.</param>
    /// <returns>The identifier with word boundaries separated by spaces.</returns>
    public string HumanizeIdentifier(string value)
    {
        try
        {
            return Regex.Replace(value ?? string.Empty, "([a-z])([A-Z])", "$1 $2");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Humanizing an editor identifier failed; source text was omitted from diagnostics.");
            return value ?? string.Empty;
        }
    }

    /// <summary>
    /// Parses newline-separated HTTP-style header text into publication web-header objects.
    /// </summary>
    /// <param name="value">Header editor text containing one name/value pair per line.</param>
    /// <returns>Parsed header objects. Invalid or incomplete lines are ignored.</returns>
    public List<PublicationWebHeader> ParseWebHeaders(string value)
    {
        try
        {
            return (value ?? string.Empty)
                .Replace("\r", string.Empty, StringComparison.Ordinal)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Split(':', 2))
                .Where(parts => parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]))
                .Select(parts => new PublicationWebHeader { Name = parts[0].Trim(), Value = parts[1].Trim() })
                .ToList();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Parsing publication web headers failed; header content was omitted from diagnostics.");
            return [];
        }
    }

    /// <summary>
    /// Formats publication web headers as newline-separated name/value pairs for an editor field.
    /// </summary>
    /// <param name="headers">Headers to format.</param>
    /// <returns>Editable newline-separated header text.</returns>
    public string FormatWebHeaders(IEnumerable<PublicationWebHeader>? headers)
    {
        try
        {
            return string.Join(Environment.NewLine, (headers ?? Array.Empty<PublicationWebHeader>()).Select(header => $"{header.Name}: {header.Value}"));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Formatting publication web headers failed; header content was omitted from diagnostics.");
            return string.Empty;
        }
    }

    /// <summary>
    /// Escapes a closing script tag so embedded user script cannot terminate PublisherStudio's generated script element.
    /// </summary>
    /// <param name="value">Raw embedded script text.</param>
    /// <returns>Script text with closing script-tag starts escaped.</returns>
    public string EscapeEmbeddedScriptClosingTag(string value)
    {
        try
        {
            return (value ?? string.Empty).Replace("</script", "<\\/script", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Escaping embedded script content failed; script text was omitted from diagnostics.");
            return value ?? string.Empty;
        }
    }

    /// <summary>
    /// Builds the CSS polygon used by Media Studio's normalized frame editor.
    /// </summary>
    /// <param name="points">Normalized media-frame points.</param>
    /// <returns>A CSS polygon matching the existing Media Studio formatting semantics.</returns>
    public string BuildMediaStudioFramePolygonCss(IEnumerable<MediaFramePoint>? points)
    {
        try
        {
            return $"polygon({string.Join(',', (points ?? Array.Empty<MediaFramePoint>()).Select(point => $"{point.X * 100:0.###}% {point.Y * 100:0.###}%"))})";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Building Media Studio frame polygon CSS failed; point values were omitted from diagnostics.");
            return "polygon()";
        }
    }

    /// <summary>
    /// Builds an invariant CSS polygon for publication playback and print surfaces.
    /// </summary>
    /// <param name="points">Normalized media-frame points.</param>
    /// <returns>An invariant CSS polygon with coordinates constrained to the normalized frame.</returns>
    public string BuildPublicationFramePolygonCss(IEnumerable<MediaFramePoint>? points)
    {
        try
        {
            var formatted = (points ?? Array.Empty<MediaFramePoint>())
                .Select(point =>
                    $"{(Math.Clamp(point.X, 0, 1) * 100).ToString("0.###", CultureInfo.InvariantCulture)}% " +
                    $"{(Math.Clamp(point.Y, 0, 1) * 100).ToString("0.###", CultureInfo.InvariantCulture)}%");
            return $"polygon({string.Join(',', formatted)})";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Building publication frame polygon CSS failed; point values were omitted from diagnostics.");
            return "polygon()";
        }
    }

}
