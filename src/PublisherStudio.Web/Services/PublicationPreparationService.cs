using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services;

/// <summary>Normalizes an independent native publication for download or later editing.</summary>
public sealed class PublicationPreparationService(
    PublicationFileService files,
    IOptions<OrganicPluginOptions> options,
    ILogger<PublicationPreparationService> logger)
{
    /// <summary>Prepares the supplied document using the same native format as the editor.</summary>
    public PublisherPublicationExportResult Prepare(JsonElement publication)
    {
        try
        {
            if (publication.ValueKind != JsonValueKind.Object ||
                !publication.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(name.GetString()) ||
                !publication.TryGetProperty("pages", out var pages) || pages.ValueKind != JsonValueKind.Array || pages.GetArrayLength() == 0 ||
                pages.EnumerateArray().Any(page => page.ValueKind != JsonValueKind.Object))
                throw new ArgumentException("Provide a native publication object with a name and at least one page object.");

            var json = publication.GetRawText();
            var maximumCharacters = Math.Max(1, options.Value.DocumentCreation.MaximumContentCharacters);
            if (json.Length > maximumCharacters)
                throw new ArgumentException($"The publication exceeds the configured limit of {maximumCharacters} characters.");
            if (files.HasEmbeddedStreamingSettings(json))
                throw new ArgumentException("Remove the streaming property before preparing a publication.");

            var document = files.Deserialize(json);
            var fileName = files.SafeFileName(document.Name) + ".pubstudio.json";
            var bytes = Encoding.UTF8.GetBytes(files.Serialize(document));
            logger.LogDebug("Prepared a native publication with {PageCount} pages and {Length} bytes.", document.Pages.Count, bytes.Length);
            return new PublisherPublicationExportResult
            {
                Success = true,
                Scope = "publication",
                Kind = "json",
                MimeType = "application/json",
                FileName = fileName,
                PageCount = document.Pages.Count,
                DocumentBytes = bytes,
                Message = $"Prepared {fileName}."
            };
        }
        catch (Exception exception) when (exception is ArgumentException or JsonException or InvalidDataException or NotSupportedException)
        {
            logger.LogWarning("Rejected native publication input ({ErrorType}).", exception.GetType().Name);
            throw;
        }
    }
}
