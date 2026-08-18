using Microsoft.Extensions.Logging;

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
}
