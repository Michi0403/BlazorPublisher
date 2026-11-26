using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>Returns a native publication file through the approved peer file delivery path.</summary>
public sealed class PublisherPublicationPrepareOrganicHandler(
    PublicationPreparationService preparation,
    OrganicMediaToolSupport support,
    OrganicFileTransferStore transfers,
    ILogger<PublisherPublicationPrepareOrganicHandler> logger) : IOrganicExecutableCapabilityHandler
{
    /// <inheritdoc />
    public string CapabilityKey => "publisher.publication.prepare";

    /// <inheritdoc />
    public Task<object> ExecuteAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parameters = support.ReadParameters(envelope);
            if (!parameters.TryGetProperty("publication", out var publication))
                throw new ArgumentException("publication is required.");

            parameters.TryGetProperty("edits", out var edits);
            var result = preparation.Prepare(publication, edits);
            cancellationToken.ThrowIfCancellationRequested();
            var offered = transfers.Offer(envelope.SourcePeerId, result.DocumentBytes!, result.Kind, result.MimeType, result.FileName);
            offered["PageCount"] = result.PageCount;
            offered["Message"] = result.Message;
            logger.LogInformation("Offered an approved native publication with {PageCount} pages for peer delivery.", result.PageCount);
            return Task.FromResult<object>(offered);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning("Native publication delivery failed ({ErrorType}).", exception.GetType().Name);
            throw;
        }
    }
}
