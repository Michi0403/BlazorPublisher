using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Documentation;

/// <summary>Coordinates one focus-managed same-origin documentation modal per Blazor circuit.</summary>
public interface IPublisherDocumentationViewerService
{
    /// <summary>Raised when the viewer state changes.</summary>
    event Action? StateChanged;

    /// <summary>Gets the current viewer state.</summary>
    PublisherDocumentationViewerState State { get; }

    /// <summary>Opens one approved application-relative documentation route.</summary>
    void Open(PublisherDocumentationViewerRequest request);

    /// <summary>Closes the current documentation view.</summary>
    void Close();
}

/// <summary>Scoped implementation of the PublisherStudio documentation viewer coordinator.</summary>
public sealed class PublisherDocumentationViewerService(ILogger<PublisherDocumentationViewerService> logger) : IPublisherDocumentationViewerService
{
    private long revision;

    /// <inheritdoc />
    public event Action? StateChanged;

    /// <inheritdoc />
    public PublisherDocumentationViewerState State { get; private set; } = new();

    /// <inheritdoc />
    public void Open(PublisherDocumentationViewerRequest request)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            var url = NormalizeUrl(request.Url);
            var title = string.IsNullOrWhiteSpace(request.Title)
                ? "PublisherStudio documentation"
                : request.Title.Trim();

            State = new PublisherDocumentationViewerState
            {
                IsOpen = true,
                Url = url,
                Title = title,
                Revision = Interlocked.Increment(ref revision)
            };

            logger.LogInformation("Opened the PublisherStudio documentation viewer for {DocumentationUrl}.", url);
            StateChanged?.Invoke();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Opening the PublisherStudio documentation viewer failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public void Close()
    {
        try
        {
            if (!State.IsOpen)
                return;

            State = new PublisherDocumentationViewerState
            {
                IsOpen = false,
                Revision = Interlocked.Increment(ref revision)
            };

            logger.LogDebug("Closed the PublisherStudio documentation viewer.");
            StateChanged?.Invoke();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Closing the PublisherStudio documentation viewer failed.");
            throw;
        }
    }

    private string NormalizeUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        var normalized = url.Trim();
        if (!normalized.StartsWith("/", StringComparison.Ordinal) ||
            normalized.StartsWith("//", StringComparison.Ordinal) ||
            normalized.Contains('\\'))
        {
            throw new ArgumentException(
                "Documentation viewer URLs must be same-origin application-relative paths.",
                nameof(url));
        }

        if (normalized.Any(char.IsControl))
            throw new ArgumentException("Documentation viewer URLs may not contain control characters.", nameof(url));

        return normalized;
    }
}
