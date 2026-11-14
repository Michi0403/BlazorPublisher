using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>Lists the open PublisherStudio editor sessions and their spreadsheet/text objects so a peer never has to ask a human for ids.</summary>
/// <param name="registry">Registry of open interactive editor sessions.</param>
/// <param name="logger">Service-owned structured diagnostics for the capability operation.</param>
public sealed class PublisherSessionsListOrganicHandler(IPublisherEditorSessionRegistry registry, ILogger<PublisherSessionsListOrganicHandler> logger) : IOrganicExecutableCapabilityHandler
{
    /// <summary>Gets the catalog identifier served by this execution handler.</summary>
    /// <value>Exact capability key.</value>
    public string CapabilityKey => "publisher.sessions.list";

    /// <summary>Reads the registered editor sessions without changing any publication.</summary>
    /// <param name="envelope">Permission-checked work envelope.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The open sessions (id, title, page count, spreadsheet/text objects) and a note about the registry's scope.</returns>
    public async Task<object> ExecuteAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessions = await registry.GetSessionsAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Listed {Count} open PublisherStudio editor session(s) for an organic request.", sessions.Count);
            return new
            {
                Sessions = sessions,
                SessionCount = sessions.Count,
                Note = sessions.Count == 0
                    ? "No PublisherStudio editor is open. Ask the user to open PublisherStudio in the browser, then call publisher.sessions.list again."
                    : "Sessions are the interactive PublisherStudio browser windows currently connected to this server; objects listed are the spreadsheet and text objects of each open publication. Use publisher.document.create to add a new one."
            };
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Listing PublisherStudio editor sessions was canceled.");
            else
                logger.LogError(exception, "Listing PublisherStudio editor sessions for an organic request failed.");
            throw;
        }
    }
}
