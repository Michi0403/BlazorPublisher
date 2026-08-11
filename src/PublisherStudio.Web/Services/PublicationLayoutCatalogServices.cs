using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Services;

/// <summary>
/// Defines the page preset catalog contract.
/// </summary>
public interface IPagePresetCatalog
{
    /// <summary>
    /// Gets all.
    /// </summary>
    IReadOnlyList<PagePreset> GetAll();
    /// <summary>
    /// Runs the find operation.
    /// </summary>
    PagePreset? Find(string? key);
}

/// <summary>
/// Provides page preset catalog operations.
/// </summary>
public sealed class PagePresetCatalog(
    IPublisherRuntimePolicyDataService runtimePolicy,
    ILogger<PagePresetCatalog> logger) : IPagePresetCatalog
{
    /// <summary>
    /// Gets all.
    /// </summary>
    public IReadOnlyList<PagePreset> GetAll()
    {
        try
        {
            var presets = runtimePolicy.DocumentDefaults.PagePresets;
            logger.LogTrace("Resolved {Count} configured publication page presets.", presets.Count);
            return presets;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not resolve publication page presets.");
            throw;
        }
    }

    /// <summary>
    /// Runs the find operation.
    /// </summary>
    public PagePreset? Find(string? key)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            return GetAll().FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not resolve publication page preset {PresetKey}.", key);
            throw;
        }
    }
}

/// <summary>
/// Defines the story page layout service contract.
/// </summary>
public interface IStoryPageLayoutService
{
    /// <summary>
    /// Gets default.
    /// </summary>
    StoryPageLayout GetDefault();
    /// <summary>
    /// Runs the normalize operation.
    /// </summary>
    StoryPageLayout Normalize(
        double pageWidthMm,
        double pageHeightMm,
        double marginTopMm,
        double marginRightMm,
        double marginBottomMm,
        double marginLeftMm);
}

/// <summary>
/// Provides story page layout service operations.
/// </summary>
public sealed class StoryPageLayoutService(
    IPublisherRuntimePolicyDataService runtimePolicy,
    ILogger<StoryPageLayoutService> logger) : IStoryPageLayoutService
{
    /// <summary>
    /// Gets default.
    /// </summary>
    public StoryPageLayout GetDefault()
    {
        try
        {
            var defaults = runtimePolicy.DocumentDefaults;
            return new StoryPageLayout(
                defaults.StoryPageWidthMillimeters,
                defaults.StoryPageHeightMillimeters,
                defaults.StoryMarginTopMillimeters,
                defaults.StoryMarginRightMillimeters,
                defaults.StoryMarginBottomMillimeters,
                defaults.StoryMarginLeftMillimeters);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not create the default story page layout.");
            throw;
        }
    }

    /// <summary>
    /// Runs the normalize operation.
    /// </summary>
    public StoryPageLayout Normalize(
        double pageWidthMm,
        double pageHeightMm,
        double marginTopMm,
        double marginRightMm,
        double marginBottomMm,
        double marginLeftMm)
    {
        try
        {
            var fallback = GetDefault();
            var width = Math.Clamp(double.IsFinite(pageWidthMm) ? pageWidthMm : fallback.PageWidthMm, 25.4, 2000);
            var height = Math.Clamp(double.IsFinite(pageHeightMm) ? pageHeightMm : fallback.PageHeightMm, 25.4, 2000);
            var top = NormalizeMargin(marginTopMm, height);
            var right = NormalizeMargin(marginRightMm, width);
            var bottom = NormalizeMargin(marginBottomMm, height);
            var left = NormalizeMargin(marginLeftMm, width);
            NormalizePair(ref left, ref right, width);
            NormalizePair(ref top, ref bottom, height);
            return new StoryPageLayout(width, height, top, right, bottom, left);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not normalize the story page layout.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes margin.
    /// </summary>
    private double NormalizeMargin(double value, double pageSize) {
    try
    {
        return Math.Clamp(double.IsFinite(value) ? value : 0, 0, Math.Max(0, pageSize - 1));
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(StoryPageLayoutService)}.{nameof(NormalizeMargin)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(StoryPageLayoutService)}.{nameof(NormalizeMargin)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes pair.
    /// </summary>
    private void NormalizePair(ref double first, ref double second, double pageSize)
    {
    try
    {
            var maximum = Math.Max(1, pageSize - 1);
            var sum = first + second;
            if (sum <= maximum || sum <= 0) return;
            var scale = maximum / sum;
            first *= scale;
            second *= scale;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(StoryPageLayoutService)}.{nameof(NormalizePair)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(StoryPageLayoutService)}.{nameof(NormalizePair)} failed.");
        throw;
    }
}
}
