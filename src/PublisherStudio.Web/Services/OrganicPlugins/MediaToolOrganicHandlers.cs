using System.Text.Json;
using Microsoft.Extensions.Options;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;
using PublisherStudio.Services.MediaConversion;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>
/// Shared parameter reading, file budget and catalog lookups for the 1-Wire media and export tools.
/// Accepted values come from the deployed function catalog and the owning services, never from handler literals.
/// </summary>
/// <param name="functionCatalog">Object-store function catalog whose parameter schemas define the accepted values.</param>
/// <param name="codec">1-Wire codec that owns the JSON contract.</param>
/// <param name="options">Organic plugin settings; message size and media tool limits are read from them.</param>
/// <param name="logger">Structured diagnostics for parameter and catalog failures.</param>
public sealed class OrganicMediaToolSupport(
    IPublisherDxFunctionCatalogDataService functionCatalog,
    IOrganicPluginProtocolCodec codec,
    IOptions<OrganicPluginOptions> options,
    ILogger<OrganicMediaToolSupport> logger)
{
    /// <summary>Gets the media tool settings (section <c>OrganicPlugins:MediaTools</c>).</summary>
    /// <value>The current media tool settings.</value>
    public OrganicMediaToolOptions Settings => options.Value.MediaTools;

    /// <summary>Gets the JSON options of the 1-Wire contract.</summary>
    /// <value>The codec's serializer options.</value>
    public JsonSerializerOptions JsonOptions => codec.JsonOptions;

    /// <summary>Reads the typed Parameters object of an approved work envelope.</summary>
    /// <param name="envelope">Approved work envelope.</param>
    /// <returns>The Parameters object, or an empty object when the request has none.</returns>
    public JsonElement ReadParameters(OrganicWireEnvelope envelope)
    {
        try
        {
            if (envelope.Properties is not null &&
                envelope.Properties.TryGetValue("Parameters", out var parameters) &&
                parameters.ValueKind == JsonValueKind.Object)
                return parameters;
            logger.LogDebug("Organic media tool request {CapabilityKey} carried no typed Parameters.", envelope.CapabilityKey);
            return JsonSerializer.SerializeToElement(new { });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not read organic media tool parameters.");
            throw;
        }
    }

    /// <summary>Reads an optional string parameter.</summary>
    /// <param name="parameters">The Parameters object.</param>
    /// <param name="name">Property name.</param>
    /// <returns>The trimmed value, or an empty string.</returns>
    public string ReadString(JsonElement parameters, string name)
    {
        try
        {
            return parameters.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                ? (value.GetString() ?? string.Empty).Trim()
                : string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not read organic media tool parameter {Name}.", name);
            throw;
        }
    }

    /// <summary>Reads an optional integer parameter.</summary>
    /// <param name="parameters">The Parameters object.</param>
    /// <param name="name">Property name.</param>
    /// <returns>The value, or <see langword="null"/> when absent or not an integer.</returns>
    public int? ReadInt(JsonElement parameters, string name)
    {
        try
        {
            return parameters.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)
                ? number
                : null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not read organic media tool parameter {Name}.", name);
            throw;
        }
    }

    /// <summary>Reads the required UUID-addressed conversion job.</summary>
    /// <param name="parameters">The Parameters object.</param>
    /// <returns>The job identifier.</returns>
    public Guid ReadJobId(JsonElement parameters)
    {
        try
        {
            if (Guid.TryParse(ReadString(parameters, "jobId"), out var jobId))
                return jobId;
            throw new ArgumentException("A valid jobId returned by publisher.media.convert or publisher.media.jobs is required.");
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Organic media tool request had no valid jobId.");
            throw;
        }
    }

    /// <summary>Gets the largest file one 1-Wire result may carry, leaving room for Base64 and the JSON envelope.</summary>
    /// <returns>The file budget in bytes, derived from <c>OrganicPlugins:MaximumMessageBytes</c>.</returns>
    public int GetFileBudgetBytes()
    {
        try
        {
            // Same budget as document creation: a quarter of the frame covers Base64 growth plus the envelope.
            return Math.Max(1, Math.Min(OrganicWireProtocol.MaximumMessageBytes, options.Value.MaximumMessageBytes) / 4);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not compute the 1-Wire file budget.");
            throw;
        }
    }

    /// <summary>Reads the values a catalog function advertises for one parameter, so handlers validate against the deployed catalog.</summary>
    /// <param name="capabilityKey">Catalog function key.</param>
    /// <param name="propertyName">Parameter whose <c>enum</c> is read.</param>
    /// <param name="cancellationToken">Cancels the catalog read.</param>
    /// <returns>The advertised values, or an empty list when the catalog declares none.</returns>
    public async Task<IReadOnlyList<string>> ReadAdvertisedValuesAsync(string capabilityKey, string propertyName, CancellationToken cancellationToken)
    {
        try
        {
            var functions = await functionCatalog.GetFunctionsAsync(cancellationToken).ConfigureAwait(false);
            var function = functions.FirstOrDefault(item => string.Equals(item.Key, capabilityKey, StringComparison.OrdinalIgnoreCase));
            if (function is null || string.IsNullOrWhiteSpace(function.ParameterSchemaJson))
            {
                logger.LogWarning("The function catalog does not describe {CapabilityKey}.", capabilityKey);
                return [];
            }
            using var schema = JsonDocument.Parse(function.ParameterSchemaJson);
            if (!schema.RootElement.TryGetProperty("properties", out var properties) ||
                !properties.TryGetProperty(propertyName, out var property) ||
                !property.TryGetProperty("enum", out var values) || values.ValueKind != JsonValueKind.Array)
                return [];
            return values.EnumerateArray()
                .Where(value => value.ValueKind == JsonValueKind.String)
                .Select(value => value.GetString() ?? string.Empty)
                .Where(value => value.Length > 0)
                .ToList();
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Reading advertised values for {CapabilityKey} was canceled.", capabilityKey);
            else
                logger.LogError(exception, "Could not read advertised values for {CapabilityKey}.{Property}.", capabilityKey, propertyName);
            throw;
        }
    }
}

/// <summary>
/// Queues one FFmpeg conversion of a file sent by the linked peer, using an installed preset or an operator-saved profile.
/// </summary>
/// <param name="media">Authoritative media conversion service.</param>
/// <param name="support">Shared 1-Wire media tool support.</param>
/// <param name="logger">Structured diagnostics for the capability operation.</param>
public sealed class MediaConversionConvertOrganicHandler(IMediaConversionService media, OrganicMediaToolSupport support, ILogger<MediaConversionConvertOrganicHandler> logger) : IOrganicExecutableCapabilityHandler
{
    /// <summary>Gets the catalog identifier served by this execution handler.</summary>
    /// <value>Exact capability key.</value>
    public string CapabilityKey => "publisher.media.convert";

    /// <summary>Validates the source file and conversion choice, then queues the job in the media conversion service.</summary>
    /// <param name="envelope">Approved work envelope.</param>
    /// <param name="cancellationToken">Cancels validation; a queued job is owned by the media service afterwards.</param>
    /// <returns>The queued job summary; read progress with publisher.media.job and the file with publisher.media.output.</returns>
    public async Task<object> ExecuteAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = support.ReadParameters(envelope);
            var fileName = support.ReadString(parameters, "fileName");
            var mimeType = support.ReadString(parameters, "mimeType");
            var content = support.ReadString(parameters, "contentBase64");
            if (fileName.Length == 0 || mimeType.Length == 0 || content.Length == 0)
                throw new ArgumentException("fileName, mimeType and contentBase64 are required.");

            var budget = support.GetFileBudgetBytes();
            var buffer = new byte[budget];
            if (!Convert.TryFromBase64String(content, buffer, out var written))
                throw new ArgumentException($"contentBase64 is not valid Base64 or exceeds the 1-Wire file budget of {budget} bytes (OrganicPlugins:MaximumMessageBytes).");

            var (presetId, conversionOptions) = ResolveConversion(parameters);
            if (parameters.TryGetProperty("range", out var range))
            {
                if (!mimeType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) &&
                    !mimeType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("A clip range requires an audio or video source.");
                if (range.ValueKind != JsonValueKind.Object ||
                    !range.TryGetProperty("startSeconds", out var start) || start.ValueKind != JsonValueKind.Number ||
                    !range.TryGetProperty("endSeconds", out var end) || end.ValueKind != JsonValueKind.Number ||
                    !start.TryGetDouble(out var startSeconds) || !end.TryGetDouble(out var endSeconds) ||
                    !double.IsFinite(startSeconds) || !double.IsFinite(endSeconds) || startSeconds < 0 || endSeconds <= startSeconds)
                    throw new ArgumentException("range requires finite startSeconds and endSeconds with 0 ≤ start < end.");
                if (string.Equals(conversionOptions.VideoCodec?.Trim(), "copy", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(conversionOptions.AudioCodec?.Trim(), "copy", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("A clip range needs an encoding preset or profile without stream copy.");
                conversionOptions.StartSeconds = startSeconds;
                conversionOptions.DurationSeconds = endSeconds - startSeconds;
            }
            var capabilities = await media.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
            if (!capabilities.Available)
                throw new InvalidOperationException("FFmpeg is not available in PublisherStudio. " + capabilities.InstallationHint);
            var preset = capabilities.Presets.FirstOrDefault(item => string.Equals(item.Id, presetId, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"Unknown preset '{presetId}'. Read the preset ids from publisher.export.formats.");
            if (!preset.Available)
                throw new InvalidOperationException($"Preset '{preset.Id}' is unavailable: {preset.UnavailableReason}");

            cancellationToken.ThrowIfCancellationRequested();
            // The job outlives this request, so the request token must not cancel the conversion.
            using var source = new MemoryStream(buffer, 0, written, writable: false);
            var job = await media.QueueAsync(source, fileName, mimeType, preset.Id, conversionOptions, CancellationToken.None).ConfigureAwait(false);
            logger.LogInformation("Approved organic media conversion queued job {JobId} with preset {PresetId}.", job.Id, job.PresetId);
            return new
            {
                JobId = job.Id, job.PresetId, job.Status, job.Progress, job.Message,
                job.OutputFileName, job.OutputMimeType, NextCapability = "publisher.media.job"
            };
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Organic media conversion was canceled before it was queued.");
            else
                logger.LogError(exception, "Organic media conversion could not be queued.");
            throw;
        }
    }

    /// <summary>Chooses the preset and options from an operator-saved profile or from the peer's preset and safe options.</summary>
    /// <param name="parameters">The Parameters object.</param>
    /// <returns>The preset identifier and the options to apply.</returns>
    private (string PresetId, MediaConversionOptions Options) ResolveConversion(JsonElement parameters)
    {
        try
        {
            var profileId = support.ReadString(parameters, "profileId");
            if (profileId.Length > 0)
            {
                if (!Guid.TryParse(profileId, out var id) || media.GetProfiles().FirstOrDefault(item => item.Id == id) is not { } profile)
                    throw new ArgumentException("profileId does not match a PublisherStudio media profile. Read the profiles from publisher.export.formats.");
                // Operator-saved profiles are trusted as configured; copy them so the stored profile is never changed.
                var copy = JsonSerializer.Deserialize<MediaConversionOptions>(JsonSerializer.Serialize(profile.Options, support.JsonOptions), support.JsonOptions) ?? new MediaConversionOptions();
                return (profile.PresetId, copy);
            }

            var presetId = support.ReadString(parameters, "presetId");
            if (presetId.Length == 0)
                throw new ArgumentException("presetId or profileId is required. Read both from publisher.export.formats.");
            var options = parameters.TryGetProperty("options", out var element) && element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<MediaConversionOptions>(support.JsonOptions) ?? new MediaConversionOptions()
                : new MediaConversionOptions();
            if (!support.Settings.AllowPeerFilterAndAdvancedArguments &&
                (!string.IsNullOrWhiteSpace(options.VideoFilter) || !string.IsNullOrWhiteSpace(options.AudioFilter) || !string.IsNullOrWhiteSpace(options.AdvancedArguments)))
                throw new ArgumentException("Filter graphs and advanced FFmpeg arguments are not accepted from a linked peer (OrganicPlugins:MediaTools:AllowPeerFilterAndAdvancedArguments). Use a preset option or an operator-saved profile.");
            return (presetId, options);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Organic media conversion parameters were rejected.");
            throw;
        }
    }
}

/// <summary>Returns the progress of one UUID-addressed PublisherStudio conversion job.</summary>
/// <param name="media">Authoritative media conversion service.</param>
/// <param name="support">Shared 1-Wire media tool support.</param>
/// <param name="logger">Structured diagnostics for the capability operation.</param>
public sealed class MediaConversionJobOrganicHandler(IMediaConversionService media, OrganicMediaToolSupport support, ILogger<MediaConversionJobOrganicHandler> logger) : IOrganicExecutableCapabilityHandler
{
    /// <summary>Gets the catalog identifier served by this execution handler.</summary>
    /// <value>Exact capability key.</value>
    public string CapabilityKey => "publisher.media.job";

    /// <summary>Reads one job without exposing user-local physical paths.</summary>
    /// <param name="envelope">Approved work envelope.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>Status, progress, message and output metadata of the job.</returns>
    public Task<object> ExecuteAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var jobId = support.ReadJobId(support.ReadParameters(envelope));
            var job = media.GetJob(jobId) ?? throw new KeyNotFoundException("The conversion job does not exist or was removed.");
            logger.LogDebug("Read organic media conversion job {JobId}.", jobId);
            object result = new
            {
                JobId = job.Id, job.PresetId, job.Status, job.Progress, job.Message,
                job.OutputFileName, job.OutputMimeType, job.OutputSize, job.CreatedUtc, job.CompletedUtc,
                NextCapability = job.Status == MediaConversionJobStatus.Completed ? "publisher.media.output" : "publisher.media.job"
            };
            return Task.FromResult(result);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not read an organic media conversion job.");
            throw;
        }
    }
}

/// <summary>Returns the converted file of one completed job to the approved peer, inline or as a chunked transfer.</summary>
/// <param name="media">Authoritative media conversion service.</param>
/// <param name="support">Shared 1-Wire media tool support.</param>
/// <param name="transfers">Store that serves files larger than one 1-Wire message.</param>
/// <param name="logger">Structured diagnostics for the capability operation.</param>
public sealed class MediaConversionOutputOrganicHandler(IMediaConversionService media, OrganicMediaToolSupport support, OrganicFileTransferStore transfers, ILogger<MediaConversionOutputOrganicHandler> logger) : IOrganicExecutableCapabilityHandler
{
    /// <summary>Gets the catalog identifier served by this execution handler.</summary>
    /// <value>Exact capability key.</value>
    public string CapabilityKey => "publisher.media.output";

    /// <summary>Reads the converted file and returns it inline or announces a chunked transfer.</summary>
    /// <param name="envelope">Approved work envelope.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The file kind, MIME type, file name and bytes or transfer announcement.</returns>
    public async Task<object> ExecuteAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            var jobId = support.ReadJobId(support.ReadParameters(envelope));
            var job = media.GetJob(jobId) ?? throw new KeyNotFoundException("The conversion job does not exist or was removed.");
            if (job.Status != MediaConversionJobStatus.Completed)
                throw new InvalidOperationException($"The conversion job is {job.Status}; read publisher.media.job until it is Completed.");
            var maximumBytes = Math.Max(1, support.Settings.MaximumTransferFileBytes);
            if (job.OutputSize > maximumBytes)
                throw new InvalidOperationException($"The converted file has {job.OutputSize} bytes; the configured limit OrganicPlugins:MediaTools:MaximumTransferFileBytes is {maximumBytes}. Download it in PublisherStudio Media Converter Studio instead.");

            var stream = await media.OpenOutputAsync(jobId, cancellationToken).ConfigureAwait(false)
                ?? throw new FileNotFoundException("The converted file is no longer available.");
            await using (stream.ConfigureAwait(false))
            {
                using var output = new MemoryStream();
                await stream.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
                if (output.Length > maximumBytes)
                    throw new InvalidOperationException($"The converted file exceeds the configured limit OrganicPlugins:MediaTools:MaximumTransferFileBytes ({maximumBytes} bytes).");
                var result = transfers.Offer(envelope.SourcePeerId, output.ToArray(),
                    Path.GetExtension(job.OutputFileName).TrimStart('.').ToLowerInvariant(), job.OutputMimeType, job.OutputFileName);
                result["JobId"] = jobId;
                logger.LogInformation("Returned converted media for job {JobId} ({Bytes} bytes) to an approved peer.", jobId, output.Length);
                return result;
            }
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Reading converted media was canceled.");
            else
                logger.LogError(exception, "Could not return converted media.");
            throw;
        }
    }
}

/// <summary>
/// Describes every export path PublisherStudio offers to linked agents: document kinds, publication and website exports,
/// FFmpeg presets and profiles and the importable file families, all read from their owning catalogs and services.
/// </summary>
/// <param name="media">Authoritative media conversion service.</param>
/// <param name="renderExports">Render export catalog of the publication editor.</param>
/// <param name="fileFormats">Importable file-format families.</param>
/// <param name="support">Shared 1-Wire media tool support.</param>
/// <param name="logger">Structured diagnostics for the capability operation.</param>
public sealed class PublisherExportFormatsOrganicHandler(
    IMediaConversionService media,
    IRenderExportCatalogService renderExports,
    IPublisherFileFormatCapabilityService fileFormats,
    OrganicMediaToolSupport support,
    ILogger<PublisherExportFormatsOrganicHandler> logger) : IOrganicExecutableCapabilityHandler
{
    /// <summary>Gets the catalog identifier served by this execution handler.</summary>
    /// <value>Exact capability key.</value>
    public string CapabilityKey => "publisher.export.formats";

    /// <summary>Builds the export map from the deployed catalog and the live services.</summary>
    /// <param name="envelope">Approved work envelope.</param>
    /// <param name="cancellationToken">Cancels the reads.</param>
    /// <returns>The export map; FFmpeg executable paths are not included.</returns>
    public async Task<object> ExecuteAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            var capabilities = await media.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
            var families = await fileFormats.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
            object result = new
            {
                FileBudgetBytes = support.GetFileBudgetBytes(),
                Documents = new
                {
                    Capability = "publisher.document.create",
                    Kinds = await support.ReadAdvertisedValuesAsync("publisher.document.create", "kind", cancellationToken).ConfigureAwait(false)
                },
                Publication = new
                {
                    Capability = "publisher.publication.export",
                    Scopes = await support.ReadAdvertisedValuesAsync("publisher.publication.export", "scope", cancellationToken).ConfigureAwait(false),
                    Formats = await support.ReadAdvertisedValuesAsync("publisher.publication.export", "format", cancellationToken).ConfigureAwait(false),
                    RenderCatalog = renderExports.GetCapabilities()
                },
                Media = new
                {
                    Capability = "publisher.media.convert",
                    capabilities.Available,
                    capabilities.Version,
                    capabilities.LicenseNotice,
                    capabilities.InstallationHint,
                    Presets = capabilities.Presets.Select(preset => new
                    {
                        preset.Id, preset.Name, preset.Description, preset.InputKind, preset.OutputExtension,
                        preset.OutputMimeType, preset.Lossless, preset.BrowserOriented, preset.Available, preset.UnavailableReason
                    }).ToList(),
                    Profiles = media.GetProfiles().Select(profile => new
                    {
                        profile.Id, profile.Name, profile.Description, profile.PresetId, profile.BuiltIn
                    }).ToList()
                },
                ImportFamilies = families.Families
            };
            logger.LogDebug("Described PublisherStudio export formats for an organic request.");
            return result;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Describing export formats was canceled.");
            else
                logger.LogError(exception, "Could not describe PublisherStudio export formats.");
            throw;
        }
    }
}

/// <summary>
/// Exports the open publication (page, object, all pages, single-file presentation or site, structured website)
/// through the editor's own export producers after the user approved the request in PublisherStudio.
/// </summary>
/// <param name="registry">Registry of open interactive editor sessions.</param>
/// <param name="renderExports">Render export catalog that supplies the MIME type of raster and vector formats.</param>
/// <param name="support">Shared 1-Wire media tool support.</param>
/// <param name="transfers">Store that serves files larger than one 1-Wire message.</param>
/// <param name="logger">Structured diagnostics for the capability operation.</param>
public sealed class PublisherPublicationExportOrganicHandler(
    IPublisherEditorSessionRegistry registry,
    IRenderExportCatalogService renderExports,
    OrganicMediaToolSupport support,
    OrganicFileTransferStore transfers,
    ILogger<PublisherPublicationExportOrganicHandler> logger) : IOrganicExecutableCapabilityHandler
{
    /// <summary>Gets the catalog identifier served by this execution handler.</summary>
    /// <value>Exact capability key.</value>
    public string CapabilityKey => "publisher.publication.export";

    /// <summary>Validates scope and format against the deployed catalog and hands the export to the owning editor circuit.</summary>
    /// <param name="envelope">Approved work envelope.</param>
    /// <param name="cancellationToken">Cancels the wait for the editor.</param>
    /// <returns>The exported file. A failure is raised as an error so the work item reports it.</returns>
    public async Task<object> ExecuteAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = support.ReadParameters(envelope);
            var scope = support.ReadString(parameters, "scope").ToLowerInvariant();
            var format = support.ReadString(parameters, "format").ToLowerInvariant();
            var scopes = await support.ReadAdvertisedValuesAsync(CapabilityKey, "scope", cancellationToken).ConfigureAwait(false);
            var formats = await support.ReadAdvertisedValuesAsync(CapabilityKey, "format", cancellationToken).ConfigureAwait(false);
            if (!scopes.Contains(scope, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException($"scope must be one of: {string.Join(", ", scopes)}.");
            if (!formats.Contains(format, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException($"format must be one of: {string.Join(", ", formats)}.");

            var request = new PublisherPublicationExportRequest
            {
                Scope = scope,
                Format = format,
                Page = support.ReadInt(parameters, "page"),
                ElementId = support.ReadString(parameters, "elementId"),
                Dpi = support.ReadInt(parameters, "dpi") is int requestedDpi and > 0 ? requestedDpi : Math.Max(1, support.Settings.DefaultExportDpi),
                MaximumBytes = Math.Max(1, support.Settings.MaximumTransferFileBytes)
            };
            var result = await registry.ExportAsync(support.ReadString(parameters, "sessionId"), request, cancellationToken).ConfigureAwait(false);
            if (!result.Success)
                throw new InvalidOperationException(result.Message);
            if (renderExports.Find(result.Kind) is { } catalogEntry)
                result.MimeType = catalogEntry.MimeType;
            var offered = transfers.Offer(envelope.SourcePeerId, result.DocumentBytes ?? [], result.Kind, result.MimeType, result.FileName);
            offered["Scope"] = result.Scope;
            offered["PageCount"] = result.PageCount;
            offered["Message"] = result.Message;
            logger.LogInformation("Approved organic publication export produced {Kind} for scope {Scope} ({Bytes} bytes).", result.Kind, result.Scope, result.DocumentBytes?.Length ?? 0);
            return offered;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Approved organic publication export was canceled.");
            else
                logger.LogError(exception, "Approved organic publication export failed.");
            throw;
        }
    }
}

/// <summary>Reads one chunk of a file announced by an approved media or export request; only the requesting peer can read it.</summary>
/// <param name="transfers">Store that holds the announced files.</param>
/// <param name="support">Shared 1-Wire media tool support.</param>
/// <param name="logger">Structured diagnostics for the capability operation.</param>
public sealed class PublisherFileReadOrganicHandler(OrganicFileTransferStore transfers, OrganicMediaToolSupport support, ILogger<PublisherFileReadOrganicHandler> logger) : IOrganicExecutableCapabilityHandler
{
    /// <summary>Gets the catalog identifier served by this execution handler.</summary>
    /// <value>Exact capability key.</value>
    public string CapabilityKey => "publisher.file.read";

    /// <summary>Returns the chunk that starts at the requested offset.</summary>
    /// <param name="envelope">Work envelope from the peer that requested the file.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>Transfer id, offset, total length, completion flag and chunk bytes.</returns>
    public Task<object> ExecuteAsync(OrganicWireEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parameters = support.ReadParameters(envelope);
            if (!Guid.TryParse(support.ReadString(parameters, "transferId"), out var transferId))
                throw new ArgumentException("transferId from a Transfer announcement is required.");
            var offset = parameters.TryGetProperty("offset", out var offsetValue) && offsetValue.ValueKind == JsonValueKind.Number && offsetValue.TryGetInt64(out var requested)
                ? requested
                : 0L;
            var chunk = transfers.Read(envelope.SourcePeerId, transferId, offset);
            logger.LogDebug("Served a chunk of transfer {TransferId} to peer {PeerId}.", transferId, envelope.SourcePeerId);
            return Task.FromResult(chunk);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not serve a 1-Wire file chunk.");
            throw;
        }
    }
}
