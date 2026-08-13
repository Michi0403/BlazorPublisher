using System.Text.RegularExpressions;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Supplies panel text patterns from the configured serializable object store.
/// Components and runtime services must not own regex literals, flags, or timeouts.
/// </summary>
public interface IPanelStudioTextPatternDataService
{
    /// <summary>
    /// Gets the shutdown pattern value that forms part of the panel studio text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The shutdown pattern value exposed by <see cref="IPanelStudioTextPatternDataService"/>.</value>
    Regex ShutdownPattern { get; }
    /// <summary>
    /// Gets the HTML break pattern value that forms part of the panel studio text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The HTML break pattern value exposed by <see cref="IPanelStudioTextPatternDataService"/>.</value>
    Regex HtmlBreakPattern { get; }
    /// <summary>
    /// Gets the HTML tag pattern value that forms part of the panel studio text pattern state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The HTML tag pattern value exposed by <see cref="IPanelStudioTextPatternDataService"/>.</value>
    Regex HtmlTagPattern { get; }
    /// <summary>
    /// Gets the unsafe file name pattern used by this panel studio text pattern instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The unsafe file name pattern value exposed by <see cref="IPanelStudioTextPatternDataService"/>.</value>
    Regex UnsafeFileNamePattern { get; }
}
