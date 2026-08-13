using PublisherStudio.Services.OrganicPlugins;
using System.Text.Json;

namespace PublisherStudio.Services.Configuration;

/// <summary>
/// Defines the contract for publisher DevExpress function catalog behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IPublisherDxFunctionCatalogDataService
{
    /// <summary>
    /// Occurs when either the deployed or user-local function catalog changes on disk.
    /// </summary>
    event Action? Changed;
    /// <summary>
    /// Retrieves functions as part of the publisher DevExpress function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    Task<IReadOnlyList<OrganicCapabilityDescriptor>> GetFunctionsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents publisher DevExpress function catalog state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
public sealed class PublisherDxFunctionCatalogDocument
{
    /// <summary>
    /// Gets or sets the schema version value that forms part of the publisher DevExpress function catalog state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The schema version value exposed by <see cref="PublisherDxFunctionCatalogDocument"/>.</value>
    public int SchemaVersion { get; set; }
    /// <summary>
    /// Gets or sets the functions collection maintained or exposed by this publisher DevExpress function catalog instance for downstream processing.
    /// </summary>
    /// <value>The functions value exposed by <see cref="PublisherDxFunctionCatalogDocument"/>.</value>
    public List<OrganicCapabilityDescriptor> Functions { get; set; } = [];
}

/// <summary>
/// Loads the deployed PublisherStudio function catalog and merges optional user-local overrides.
/// A malformed user override is ignored with a warning so the shipped catalog remains usable.
/// </summary>
/// <param name="environment">Web host environment dependency used by the publisher DevExpress function catalog workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class PublisherDxFunctionCatalogDataService(
    IWebHostEnvironment environment,
    ILogger<PublisherDxFunctionCatalogDataService> logger) : IPublisherDxFunctionCatalogDataService, IDisposable
{
    /// <summary>Serializes initialization and disposal of exact-catalog file watchers.</summary>
    private readonly object watcherGate = new();
    /// <summary>
    /// Stores the internal deployed watcher state used by <see cref="PublisherDxFunctionCatalogDataService"/> while executing its surrounding workflow.
    /// </summary>
    private FileSystemWatcher? deployedWatcher;
    /// <summary>
    /// Stores the internal user watcher state used by <see cref="PublisherDxFunctionCatalogDataService"/> while executing its surrounding workflow.
    /// </summary>
    private FileSystemWatcher? userWatcher;
    /// <summary>
    /// Stores the internal watchers initialized state used by <see cref="PublisherDxFunctionCatalogDataService"/> while executing its surrounding workflow.
    /// </summary>
    private bool watchersInitialized;

    /// <summary>
    /// Occurs when changed changes or completes in <see cref="PublisherDxFunctionCatalogDataService"/>, allowing interested callers to react without polling internal state.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    /// <summary>
    /// Retrieves functions as part of the publisher DevExpress function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    public async Task<IReadOnlyList<OrganicCapabilityDescriptor>> GetFunctionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureWatchers();
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
    /// Ensures file-system notifications are active for both serializable capability catalogs.
    /// </summary>
    private void EnsureWatchers()
    {
        try
        {
            lock (watcherGate)
            {
                if (watchersInitialized)
                    return;

                var deployedPath = Path.Combine(environment.ContentRootPath, "Configuration", "publisher-dx-functions.json");
                var userPath = GetUserCatalogPath();
                Directory.CreateDirectory(Path.GetDirectoryName(userPath)!);
                deployedWatcher = CreateWatcher(deployedPath);
                userWatcher = CreateWatcher(userPath);
                watchersInitialized = true;
            }
            logger.LogDebug("PublisherStudio DX function catalog live-change watchers are active.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not initialize PublisherStudio DX function catalog live-change watchers.");
            throw;
        }
    }

    /// <summary>
    /// Creates a watcher for one exact serializable catalog file.
    /// </summary>
    /// <param name="filePath">File path value supplied to the publisher DevExpress function catalog operation and used when producing its result.</param>
    /// <returns>The file system watcher produced by the operation.</returns>
    private FileSystemWatcher CreateWatcher(string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException("A PublisherStudio DX function catalog has no directory.");
            Directory.CreateDirectory(directory);
            var watcher = new FileSystemWatcher(directory, Path.GetFileName(filePath))
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
                IncludeSubdirectories = false,
                EnableRaisingEvents = false
            };
            watcher.Changed += OnCatalogFileChanged;
            watcher.Created += OnCatalogFileChanged;
            watcher.Deleted += OnCatalogFileChanged;
            watcher.Renamed += OnCatalogFileRenamed;
            watcher.EnableRaisingEvents = true;
            return watcher;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not create a PublisherStudio DX function catalog watcher; path omitted from logs.");
            throw;
        }
    }

    /// <summary>
    /// Signals a live capability-directory refresh after an exact catalog file change.
    /// </summary>
    /// <param name="sender">Sender value supplied to the publisher DevExpress function catalog operation and used when producing its result.</param>
    /// <param name="args">Args value supplied to the publisher DevExpress function catalog operation and used when producing its result.</param>
    private void OnCatalogFileChanged(object sender, FileSystemEventArgs args)
    {
        try
        {
            logger.LogInformation("PublisherStudio DX function catalog changed; linked 1-Wire peers will receive a refreshed organic directory.");
            Changed?.Invoke();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not publish a PublisherStudio DX function catalog change notification.");
        }
    }

    /// <summary>
    /// Signals a live capability-directory refresh after an exact catalog file rename or replacement.
    /// </summary>
    /// <param name="sender">Sender value supplied to the publisher DevExpress function catalog operation and used when producing its result.</param>
    /// <param name="args">Args value supplied to the publisher DevExpress function catalog operation and used when producing its result.</param>
    private void OnCatalogFileRenamed(object sender, RenamedEventArgs args)
    {
        try
        {
            logger.LogInformation("PublisherStudio DX function catalog was replaced; linked 1-Wire peers will receive a refreshed organic directory.");
            Changed?.Invoke();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not publish a replaced PublisherStudio DX function catalog notification.");
        }
    }

    /// <summary>
    /// Releases the live serializable-catalog file watchers.
    /// </summary>
    public void Dispose()
    {
        try
        {
            lock (watcherGate)
            {
                deployedWatcher?.Dispose();
                userWatcher?.Dispose();
                deployedWatcher = null;
                userWatcher = null;
                watchersInitialized = false;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not dispose PublisherStudio DX function catalog watchers.");
            throw;
        }
    }

    /// <summary>
    /// Reads required document as part of the publisher DevExpress function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the publisher DevExpress function catalog operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the publisher DevExpress function catalog operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The publisher DevExpress function catalog document produced by the operation.</returns>
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
    /// Reads optional document as part of the publisher DevExpress function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="path">Path value supplied to the publisher DevExpress function catalog operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The publisher DevExpress function catalog document produced by the operation.</returns>
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
    /// Validates document as part of the publisher DevExpress function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="document">Document value supplied to the publisher DevExpress function catalog operation and used when producing its result.</param>
    /// <param name="source">Source value supplied to the publisher DevExpress function catalog operation and used when producing its result.</param>
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
    /// Validates functions as part of the publisher DevExpress function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="functions">Organic capability descriptor dependency used by the publisher DevExpress function catalog workflow to provide the corresponding application capability.</param>
    /// <param name="source">Source value supplied to the publisher DevExpress function catalog operation and used when producing its result.</param>
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
    /// Retrieves storage key as part of the publisher DevExpress function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="item">Item value supplied to the publisher DevExpress function catalog operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
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
    /// Retrieves user catalog path as part of the publisher DevExpress function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The string produced by the operation.</returns>
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
    /// Writes initial user catalog as part of the publisher DevExpress function catalog service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="userPath">User path value supplied to the publisher DevExpress function catalog operation and used when producing its result.</param>
    /// <param name="seed">Seed value supplied to the publisher DevExpress function catalog operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
