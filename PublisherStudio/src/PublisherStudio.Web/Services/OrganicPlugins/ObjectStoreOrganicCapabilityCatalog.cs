using PublisherStudio.Services.Configuration;
using PublisherStudio.Services.MediaConversion;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>
/// Runtime PublisherStudio capability catalog. DX function descriptors are loaded from the
/// serializable object store and advertised through the shared WireLibrary contracts. The
/// reviewed legacy catalog remains intact for its non-DX UI and hardware metadata only.
/// </summary>
public sealed class ObjectStoreOrganicCapabilityCatalog(
    IPublisherDxFunctionCatalogDataService functionCatalog,
    OrganicCapabilityCatalog legacyMetadataCatalog,
    IMediaConversionService mediaConversion,
    ILogger<ObjectStoreOrganicCapabilityCatalog> logger) : IOrganicCapabilityCatalog
{
    public async Task<IReadOnlyList<OrganicCapabilityDescriptor>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var capabilities = (await functionCatalog.GetFunctionsAsync(cancellationToken).ConfigureAwait(false)).ToList();
            var media = await mediaConversion.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
            var mediaCapability = capabilities.FirstOrDefault(item =>
                string.Equals(item.Key, "publisher.media.capabilities", StringComparison.OrdinalIgnoreCase));
            if (mediaCapability is not null)
                mediaCapability.Description = $"{mediaCapability.Description.Trim()} FFmpeg available: {media.Available}.";

            logger.LogInformation($"Published {capabilities.Count} PublisherStudio DX function descriptor(s) from the serializable object store through the shared 1-Wire contract.");
            return capabilities;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation($"PublisherStudio object-store DX function catalog loading was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublisherStudio object-store DX function catalog loading failed; the reviewed legacy DX function literals were not used as a runtime fallback.");
            throw;
        }
    }

    public async Task<IReadOnlyList<OrganicSkillDescriptor>> GetSkillsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var capabilities = await GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
            var skills = capabilities
                .SelectMany(capability => capability.Skills.Select(skill => new { Skill = skill, Capability = capability }))
                .GroupBy(item => item.Skill, StringComparer.OrdinalIgnoreCase)
                .Select(group => new OrganicSkillDescriptor
                {
                    Key = group.Key,
                    DisplayName = group.Key,
                    Description = $"PublisherStudio organic skill backed by {group.Select(item => item.Capability.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count()} object-store capability route(s).",
                    SourcePeerId = "publisherstudio",
                    Organs = group.SelectMany(item => item.Capability.Organs).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    CapabilityKeys = group.Select(item => item.Capability.Key).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    UiActivationKeys = group.SelectMany(item => item.Capability.UiActivationKeys).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    IsOnline = group.Any(item => item.Capability.IsOnline),
                    IsEnabled = group.Any(item => item.Capability.IsEnabled),
                    UpdatedUtc = DateTimeOffset.UtcNow
                })
                .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
            logger.LogInformation($"Derived {skills.Count} PublisherStudio organic skill descriptor(s) from the object-store DX function catalog.");
            return skills;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation($"PublisherStudio object-store skill derivation was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublisherStudio object-store skill derivation failed.");
            throw;
        }
    }

    public async Task<IReadOnlyList<OrganicUiFeatureDescriptor>> GetUiFeaturesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var features = await legacyMetadataCatalog.GetUiFeaturesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogDebug($"Loaded {features.Count} reviewed PublisherStudio UI feature descriptor(s) while DX functions remain object-store-owned.");
            return features;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation($"PublisherStudio UI feature metadata loading was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublisherStudio UI feature metadata loading failed.");
            throw;
        }
    }

    public async Task<IReadOnlyList<OrganicHardwareDescriptor>> GetHardwareAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var hardware = await legacyMetadataCatalog.GetHardwareAsync(cancellationToken).ConfigureAwait(false);
            logger.LogDebug($"Loaded {hardware.Count} reviewed PublisherStudio hardware descriptor(s) while DX functions remain object-store-owned.");
            return hardware;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation($"PublisherStudio hardware metadata loading was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublisherStudio hardware metadata loading failed.");
            throw;
        }
    }
}
