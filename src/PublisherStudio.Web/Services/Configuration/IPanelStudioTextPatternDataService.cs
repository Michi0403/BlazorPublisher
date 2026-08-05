using System.Text.RegularExpressions;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Supplies panel text patterns from the configured serializable object store.
/// Components and runtime services must not own regex literals, flags, or timeouts.
/// </summary>
public interface IPanelStudioTextPatternDataService
{
    Regex ShutdownPattern { get; }
    Regex HtmlBreakPattern { get; }
    Regex HtmlTagPattern { get; }
    Regex UnsafeFileNamePattern { get; }
}
