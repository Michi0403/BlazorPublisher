using PublisherStudio.Services.OrganicPlugins;
using System.Text.Json;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Defines the publisher DevExpress function catalog data service contract.
/// </summary>
public interface IPublisherDxFunctionCatalogDataService
{
    /// <summary>
    /// Gets functions async.
    /// </summary>
    Task<IReadOnlyList<OrganicCapabilityDescriptor>> GetFunctionsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a publisher DevExpress function catalog document.
/// </summary>
public sealed class PublisherDxFunctionCatalogDocument
{
    /// <summary>
    /// Gets or sets schema version.
    /// </summary>
    public int SchemaVersion { get; set; }
    /// <summary>
    /// Gets or sets functions.
    /// </summary>
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
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    /// <summary>
    /// Gets functions async.
    /// </summary>
    public async Task<IReadOnlyList<OrganicCapabilityDescriptor>> GetFunctionsAsync(CancellationToken cancellationToken = default)
    {
        try
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
                    $"The user-local PublisherStudio DX function catalog could not be used. The deployed catalog remains active; paths and catalog content were omitted from logs.");
            }

            var result = merged.Values
                .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
            ValidateFunctions(result, "merged");
            logger.LogInformation(
                $"Loaded {result.Count} PublisherStudio DX function descriptors from the serializable catalog; schemas and paths were omitted from logs.");
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not load the PublisherStudio DX function catalog.");
            throw;
        }
    }

    /// <summary>
    /// Reads required document async.
    /// </summary>
    private async Task<PublisherDxFunctionCatalogDocument> ReadRequiredDocumentAsync(
        string path,
        string source,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"The {source} PublisherStudio DX function catalog is missing.", path);

            var document = await ReadOptionalDocumentAsync(path, cancellationToken).ConfigureAwait(false);
            logger.LogTrace($"Read the required {source} PublisherStudio DX function catalog.");
            return document ?? throw new InvalidDataException($"The {source} PublisherStudio DX function catalog is empty.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not read the required {source} PublisherStudio DX function catalog.");
            throw;
        }
    }

    /// <summary>
    /// Reads optional document async.
    /// </summary>
    private async Task<PublisherDxFunctionCatalogDocument?> ReadOptionalDocumentAsync(
        string path,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            var document = JsonSerializer.Deserialize<PublisherDxFunctionCatalogDocument>(json, JsonOptions);
            logger.LogTrace($"Read an optional PublisherStudio DX function catalog document.");
            return document;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not read an optional PublisherStudio DX function catalog document.");
            throw;
        }
    }

    /// <summary>
    /// Validates document.
    /// </summary>
    private void ValidateDocument(PublisherDxFunctionCatalogDocument document, string source)
    {
        try
        {
            if (document.SchemaVersion <= 0)
                throw new InvalidDataException($"The {source} PublisherStudio DX function catalog has no valid schema version.");
            if (document.Functions.Count == 0)
                throw new InvalidDataException($"The {source} PublisherStudio DX function catalog contains no functions.");
            ValidateFunctions(document.Functions, source);
            logger.LogTrace($"Validated the {source} PublisherStudio DX function catalog document.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not validate the {source} PublisherStudio DX function catalog document.");
            throw;
        }
    }

    /// <summary>
    /// Validates functions.
    /// </summary>
    private void ValidateFunctions(IEnumerable<OrganicCapabilityDescriptor> functions, string source)
    {
        try
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
            logger.LogTrace($"Validated PublisherStudio DX function descriptors from the {source} catalog.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not validate PublisherStudio DX function descriptors from the {source} catalog.");
            throw;
        }
    }

    /// <summary>
    /// Gets storage key.
    /// </summary>
    private string GetStorageKey(OrganicCapabilityDescriptor item)
    {
        try
        {
            var key = string.IsNullOrWhiteSpace(item.ConfigurationKey) ? item.Key : item.ConfigurationKey;
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidDataException("A PublisherStudio DX function has no storage key.");
            logger.LogTrace($"Resolved the storage key for PublisherStudio DX function {item.Key}.");
            return key;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not resolve the storage key for PublisherStudio DX function {item?.Key}.");
            throw;
        }
    }

    /// <summary>
    /// Gets user catalog path.
    /// </summary>
    private string GetUserCatalogPath()
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PublisherStudio",
                "Configuration",
                "publisher-dx-functions.json");
            logger.LogTrace($"Resolved the user-local PublisherStudio DX function catalog path; path content omitted from logs.");
            return path;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not resolve the user-local PublisherStudio DX function catalog path.");
            throw;
        }
    }

    /// <summary>
    /// Writes initial user catalog async.
    /// </summary>
    private async Task WriteInitialUserCatalogAsync(
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
            logger.LogInformation($"Created the initial user-local PublisherStudio DX function catalog; path content omitted from logs.");
        }
        catch (IOException) when (File.Exists(userPath))
        {
            logger.LogDebug($"Another process created the initial user-local PublisherStudio DX function catalog first.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not create the initial user-local PublisherStudio DX function catalog.");
            throw;
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
