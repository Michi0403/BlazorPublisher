using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    /// <param name="publication">Complete native publication submitted for preparation.</param>
    /// <param name="edits">Optional ordered replacements for top-level elements.</param>
    public PublisherPublicationExportResult Prepare(JsonElement publication, JsonElement edits = default)
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
            var editCharacters = edits.ValueKind == JsonValueKind.Undefined ? 0 : edits.GetRawText().Length;
            if ((long)json.Length + editCharacters > maximumCharacters)
                throw new ArgumentException($"The publication and edits exceed the configured limit of {maximumCharacters} characters.");
            if (files.HasEmbeddedStreamingSettings(json))
                throw new ArgumentException("Remove the streaming property before preparing a publication.");

            if (edits.ValueKind != JsonValueKind.Undefined)
                json = ApplyEdits(publication, edits);

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

    private string ApplyEdits(JsonElement publication, JsonElement edits)
    {
        try
        {
            if (edits.ValueKind != JsonValueKind.Array || edits.GetArrayLength() is < 1 or > 32)
                throw new ArgumentException("Provide between 1 and 32 element replacements.");

            var draft = JsonNode.Parse(publication.GetRawText())!.AsObject();
            var pages = draft["pages"]!.AsArray();
            var replaced = new HashSet<(Guid Page, Guid Element)>();
            foreach (var edit in edits.EnumerateArray())
            {
                if (edit.ValueKind != JsonValueKind.Object ||
                    !edit.TryGetProperty("pageId", out var pageIdValue) || pageIdValue.ValueKind != JsonValueKind.String ||
                    !pageIdValue.TryGetGuid(out var pageId) || pageId == Guid.Empty ||
                    !edit.TryGetProperty("elementId", out var elementIdValue) || elementIdValue.ValueKind != JsonValueKind.String ||
                    !elementIdValue.TryGetGuid(out var elementId) || elementId == Guid.Empty ||
                    !edit.TryGetProperty("element", out var element) || element.ValueKind != JsonValueKind.Object ||
                    !element.TryGetProperty("$type", out var type) || type.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(type.GetString()))
                    throw new ArgumentException("Each edit requires pageId, elementId and a complete native element with a $type.");

                if (!replaced.Add((pageId, elementId)))
                    throw new ArgumentException("A publication element can only be replaced once per request.");

                var matchingPages = pages.OfType<JsonObject>().Where(page => HasId(page, pageId)).ToArray();
                if (matchingPages.Length != 1 || matchingPages[0]["elements"] is not JsonArray elements)
                    throw new ArgumentException($"The publication does not contain a unique page with ID {pageId} and an element list.");

                var matches = elements.OfType<JsonObject>().Where(candidate => HasId(candidate, elementId)).ToArray();
                if (matches.Length != 1)
                    throw new ArgumentException($"The page does not contain a unique top-level element with ID {elementId}.");

                var replacement = new JsonObject
                {
                    ["$type"] = type.GetString(),
                    ["id"] = elementId.ToString()
                };
                foreach (var property in element.EnumerateObject())
                {
                    if (!property.Name.Equals("id", StringComparison.OrdinalIgnoreCase) && property.Name != "$type")
                        replacement.Add(property.Name, JsonNode.Parse(property.Value.GetRawText()));
                }

                elements[elements.IndexOf(matches[0])] = replacement;
            }

            return draft.ToJsonString();
        }
        catch (InvalidOperationException exception)
        {
            logger.LogDebug("Rejected an invalid native publication element structure.");
            throw new InvalidDataException("The publication contains an invalid element structure.", exception);
        }
    }

    private bool HasId(JsonObject item, Guid id)
    {
        try
        {
            return item["id"] is JsonValue value && value.TryGetValue<string>(out var text) &&
                   Guid.TryParse(text, out var parsed) && parsed == id;
        }
        catch (InvalidOperationException)
        {
            logger.LogDebug("Rejected an invalid native publication identifier.");
            throw;
        }
    }

}
