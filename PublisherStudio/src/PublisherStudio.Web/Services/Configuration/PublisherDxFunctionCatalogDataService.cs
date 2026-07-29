using PublisherStudio.Services.OrganicPlugins;
using System.Text.Json;

namespace PublisherStudio.Services.Configuration;

public interface IPublisherDxFunctionCatalogDataService
{
    Task<IReadOnlyList<OrganicCapabilityDescriptor>> GetFunctionsAsync(CancellationToken cancellationToken = default);
}

public sealed class PublisherDxFunctionCatalogDocument
{
    public int SchemaVersion { get; set; }
    public List<OrganicCapabilityDescriptor> Functions { get; set; } = [];
}

/// <summary>
/// Loads the deployed PublisherStudio function catalog and merges optional user-local overrides.
/// A malformed user override is ignored with a warning so the shipped catalog remains usable.
/// </summary>
public sealed class PublisherDxFunctionCatalogDataService(
    IWebHostEnvironment environment,
    ILogger<PublisherDxFunctionCatalogDataService> logger) : IPublisherDxFunctionCatalogDataService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public async Task<IReadOnlyList<OrganicCapabilityDescriptor>> GetFunctionsAsync(CancellationToken cancellationToken = default)
    {
        var seedPath = Path.Combine(environment.ContentRootPath, "Configuration", "publisher-dx-functions.json");
        var seed = await ReadRequiredDocumentAsync(seedPath, "deployed", cancellationToken).ConfigureAwait(false);
        ValidateDocument(seed, "deployed");

        var merged = seed.Functions.ToDictionary(GetStorageKey, StringComparer.OrdinalIgnoreCase);
        var userPath = GetUserCatalogPath();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(userPath)!);
            if (!File.Exists(userPath))
                await WriteInitialUserCatalogAsync(userPath, seed, cancellationToken).ConfigureAwait(false);

            var user = await ReadOptionalDocumentAsync(userPath, cancellationToken).ConfigureAwait(false);
            if (user is not null)
            {
                ValidateDocument(user, "user-local");
                foreach (var item in user.Functions)
                    merged[GetStorageKey(item)] = item;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "The user-local PublisherStudio DX function catalog could not be used. The deployed catalog remains active; paths and catalog content were omitted from logs.");
        }

        var result = merged.Values
            .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
        ValidateFunctions(result, "merged");
        logger.LogInformation(
            "Loaded {Count} PublisherStudio DX function descriptors from the serializable catalog; schemas and paths were omitted from logs.",
            result.Count);
        return result;
    }

    private static async Task<PublisherDxFunctionCatalogDocument> ReadRequiredDocumentAsync(
        string path,
        string source,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"The {source} PublisherStudio DX function catalog is missing.", path);

        var document = await ReadOptionalDocumentAsync(path, cancellationToken).ConfigureAwait(false);
        return document ?? throw new InvalidDataException($"The {source} PublisherStudio DX function catalog is empty.");
    }

    private static async Task<PublisherDxFunctionCatalogDocument?> ReadOptionalDocumentAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<PublisherDxFunctionCatalogDocument>(json, JsonOptions);
    }

    private static void ValidateDocument(PublisherDxFunctionCatalogDocument document, string source)
    {
        if (document.SchemaVersion <= 0)
            throw new InvalidDataException($"The {source} PublisherStudio DX function catalog has no valid schema version.");
        if (document.Functions.Count == 0)
            throw new InvalidDataException($"The {source} PublisherStudio DX function catalog contains no functions.");
        ValidateFunctions(document.Functions, source);
    }

    private static void ValidateFunctions(IEnumerable<OrganicCapabilityDescriptor> functions, string source)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var configurationKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in functions)
        {
            if (string.IsNullOrWhiteSpace(item.Key) || !keys.Add(item.Key))
                throw new InvalidDataException($"The {source} catalog contains a missing or duplicate function key.");
            if (string.IsNullOrWhiteSpace(item.ConfigurationKey) || !configurationKeys.Add(item.ConfigurationKey))
                throw new InvalidDataException($"The {source} catalog contains a missing or duplicate configuration key.");
            if (string.IsNullOrWhiteSpace(item.Controller) || string.IsNullOrWhiteSpace(item.Method) || string.IsNullOrWhiteSpace(item.Route))
                throw new InvalidDataException($"DX function {item.Key} has no executable controller, method, or route contract.");

            using var parameterSchema = JsonDocument.Parse(item.ParameterSchemaJson);
            using var interactionSchema = JsonDocument.Parse(item.InteractionValueSchemaJson);
        }
    }

    private static string GetStorageKey(OrganicCapabilityDescriptor item)
    {
        var key = string.IsNullOrWhiteSpace(item.ConfigurationKey) ? item.Key : item.ConfigurationKey;
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidDataException("A PublisherStudio DX function has no storage key.");
        return key;
    }

    private static string GetUserCatalogPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PublisherStudio",
        "Configuration",
        "publisher-dx-functions.json");

    private static async Task WriteInitialUserCatalogAsync(
        string userPath,
        PublisherDxFunctionCatalogDocument seed,
        CancellationToken cancellationToken)
    {
        var temporaryPath = $"{userPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            var json = JsonSerializer.Serialize(seed, JsonOptions);
            await File.WriteAllTextAsync(temporaryPath, json, cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, userPath, overwrite: false);
        }
        catch (IOException) when (File.Exists(userPath))
        {
            // Another process created the initial catalog first.
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
