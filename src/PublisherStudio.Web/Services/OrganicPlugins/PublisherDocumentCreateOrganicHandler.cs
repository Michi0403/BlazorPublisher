using System.Text.Json;
using Microsoft.Extensions.Options;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>Creates a new native spreadsheet or text object in an open publication after the user approved the request in PublisherStudio.</summary>
/// <param name="registry">Registry of open interactive editor sessions that owns the circuit-scoped editor state.</param>
/// <param name="options">Organic plugin settings; the document creation limits are read from <c>OrganicPlugins:DocumentCreation</c>.</param>
/// <param name="logger">Service-owned structured diagnostics for the capability operation.</param>
public sealed class PublisherDocumentCreateOrganicHandler(IPublisherEditorSessionRegistry registry, IOptions<OrganicPluginOptions> options, ILogger<PublisherDocumentCreateOrganicHandler> logger) : IOrganicExecutableCapabilityHandler
{
    /// <summary>Gets the catalog identifier served by this execution handler.</summary>
    /// <value>Exact capability key.</value>
    public string CapabilityKey => "publisher.document.create";

    /// <summary>Validates the typed parameters and hands the creation to the owning editor circuit.</summary>
    /// <param name="envelope">Already approved work envelope containing typed Parameters.</param>
    /// <param name="cancellationToken">Cancels the wait for the editor.</param>
    /// <returns>The created object id, kind, title, page and presentation. A failure is raised as an error so the work item reports it.</returns>
    public async Task<object> ExecuteAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            if (envelope.Properties is null ||
                !envelope.Properties.TryGetValue("Parameters", out var parameters) ||
                parameters.ValueKind != JsonValueKind.Object)
                throw new ArgumentException("Document creation requires typed Parameters (kind, title, content).");
            var request = new PublisherDocumentCreateRequest
            {
                Kind = ReadString(parameters, "kind"),
                Title = ReadString(parameters, "title"),
                Content = ReadString(parameters, "content"),
                Presentation = ReadString(parameters, "presentation"),
                TargetPage = parameters.TryGetProperty("page", out var page) && page.ValueKind == JsonValueKind.Number && page.TryGetInt32(out var pageNumber) ? pageNumber : null
            };
            var limits = options.Value.DocumentCreation;
            var maximumContentCharacters = Math.Max(1, limits.MaximumContentCharacters);
            if (request.Content.Length > maximumContentCharacters)
                throw new ArgumentException($"The content has {request.Content.Length} characters; the configured limit OrganicPlugins:DocumentCreation:MaximumContentCharacters is {maximumContentCharacters}.");
            var maximumTitleCharacters = Math.Max(1, limits.MaximumTitleCharacters);
            if (request.Title.Length > maximumTitleCharacters) request.Title = request.Title[..maximumTitleCharacters];
            var result = await registry.CreateAsync(ReadString(parameters, "sessionId"), request, cancellationToken).ConfigureAwait(false);
            if (!result.Success)
                throw new InvalidOperationException(result.Message);
            logger.LogInformation("Approved organic document creation produced a {Kind} object on page {Page}.", result.Kind, result.Page);
            return result;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Approved organic document creation was canceled.");
            else
                logger.LogError(exception, "Approved organic document creation failed.");
            throw;
        }
    }

    /// <summary>Reads an optional string parameter.</summary>
    /// <param name="parameters">The Parameters object.</param>
    /// <param name="name">Property name to read.</param>
    /// <returns>The string value, or an empty string when absent.</returns>
    private string ReadString(JsonElement parameters, string name)
    {
        try
        {
            return parameters.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not read organic document creation parameter {Name}.", name);
            throw;
        }
    }
}
