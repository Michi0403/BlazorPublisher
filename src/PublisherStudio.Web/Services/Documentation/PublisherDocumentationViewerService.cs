using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Documentation;

/// <summary>Coordinates one focus-managed same-origin documentation modal per Blazor circuit.</summary>
public interface IPublisherDocumentationViewerService
{
    /// <summary>Raised when the viewer state changes.</summary>
    event Action? StateChanged;

    /// <summary>Gets the current viewer state.</summary>
    /// <value>The state value exposed by <see cref="IPublisherDocumentationViewerService"/>.</value>
    PublisherDocumentationViewerState State { get; }

    /// <summary>
    /// Performs open as part of the publisher documentation viewer service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    void Open(PublisherDocumentationViewerRequest request);

    /// <summary>
    /// Performs close as part of the publisher documentation viewer service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    void Close();
}

/// <summary>Scoped implementation of the PublisherStudio documentation viewer coordinator.</summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class PublisherDocumentationViewerService(ILogger<PublisherDocumentationViewerService> logger) : IPublisherDocumentationViewerService
{
    /// <summary>
    /// Stores the internal revision state used by <see cref="PublisherDocumentationViewerService"/> while executing its surrounding workflow.
    /// </summary>
    private long revision;

    /// <summary>
    /// Occurs when state changed changes or completes in <see cref="PublisherDocumentationViewerService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    /// <inheritdoc />
    public event Action? StateChanged;

    /// <summary>
    /// Gets or sets the state value that forms part of the publisher documentation viewer state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <inheritdoc />
    public PublisherDocumentationViewerState State { get; private set; } = new();

    /// <summary>
    /// Performs open as part of the publisher documentation viewer service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
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

    /// <summary>
    /// Performs close as part of the publisher documentation viewer service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
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

    /// <summary>
    /// Normalizes URL as part of the publisher documentation viewer service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="url">Url value supplied to the publisher documentation viewer operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeUrl(string url)
    {
    try
    {
            ArgumentException.ThrowIfNullOrWhiteSpace(url);
            var normalized = url.Trim();
            if (!normalized.StartsWith('/') ||
                normalized.StartsWith("//", StringComparison.Ordinal) ||
                normalized.Contains('\\'))
            {
                throw new ArgumentException(
                    "Documentation viewer URLs must be same-origin application-relative paths.",
                    nameof(url));
            }

            if (normalized.Any(char.IsControl))
                throw new ArgumentException("Documentation viewer URLs may not contain control characters.", nameof(url));

            // The controller route resolves the installed documentation root from AppContext as well as
            // IWebHostEnvironment. This deliberately avoids depending on the process working directory or
            // static-web-root discovery in customer installations. Keep /help-docs as a public compatibility
            // route, but normalize every in-app viewer request to the canonical controller-backed route.
            if (string.Equals(normalized, "/help-docs", StringComparison.OrdinalIgnoreCase))
                normalized = "/api/documentation/html/index.html";
            else if (normalized.StartsWith("/help-docs/", StringComparison.OrdinalIgnoreCase))
                normalized = "/api/documentation/html/" + normalized["/help-docs/".Length..];

            return normalized;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(PublisherDocumentationViewerService)}.{nameof(NormalizeUrl)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(PublisherDocumentationViewerService)}.{nameof(NormalizeUrl)} failed.");
        throw;
    }
}
}
