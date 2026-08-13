using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Services;

/// <summary>
/// Defines the contract for page preset behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IPagePresetCatalog
{
    /// <summary>
    /// Retrieves all in the page preset directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    IReadOnlyList<PagePreset> GetAll();
    /// <summary>
    /// Performs find in the page preset directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="key">Key value supplied to the page preset operation and used when producing its result.</param>
    /// <returns>The page preset produced by the operation.</returns>
    PagePreset? Find(string? key);
}

/// <summary>
/// Maintains the authoritative directory of page preset entries used for discovery, validation, and runtime lookup.
/// </summary>
/// <param name="runtimePolicy">Publisher runtime policy data service dependency used by the page preset workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class PagePresetCatalog(
    IPublisherRuntimePolicyDataService runtimePolicy,
    ILogger<PagePresetCatalog> logger) : IPagePresetCatalog
{
    /// <summary>
    /// Retrieves all in the page preset directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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
    /// Performs find in the page preset directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="key">Key value supplied to the page preset operation and used when producing its result.</param>
    /// <returns>The page preset produced by the operation.</returns>
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
/// Defines the contract for story page layout behavior, allowing callers to depend on the capability without coupling to a concrete implementation.
/// </summary>
public interface IStoryPageLayoutService
{
    /// <summary>
    /// Retrieves default as part of the story page layout service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The story page layout produced by the operation.</returns>
    StoryPageLayout GetDefault();
    /// <summary>
    /// Performs normalize as part of the story page layout service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="pageWidthMm">Page width mm value supplied to the story page layout operation and used when producing its result.</param>
    /// <param name="pageHeightMm">Page height mm value supplied to the story page layout operation and used when producing its result.</param>
    /// <param name="marginTopMm">Margin top mm value supplied to the story page layout operation and used when producing its result.</param>
    /// <param name="marginRightMm">Margin right mm value supplied to the story page layout operation and used when producing its result.</param>
    /// <param name="marginBottomMm">Margin bottom mm value supplied to the story page layout operation and used when producing its result.</param>
    /// <param name="marginLeftMm">Margin left mm value supplied to the story page layout operation and used when producing its result.</param>
    /// <returns>The story page layout produced by the operation.</returns>
    StoryPageLayout Normalize(
        double pageWidthMm,
        double pageHeightMm,
        double marginTopMm,
        double marginRightMm,
        double marginBottomMm,
        double marginLeftMm);
}

/// <summary>
/// Coordinates story page layout behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
/// <param name="runtimePolicy">Publisher runtime policy data service dependency used by the story page layout workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class StoryPageLayoutService(
    IPublisherRuntimePolicyDataService runtimePolicy,
    ILogger<StoryPageLayoutService> logger) : IStoryPageLayoutService
{
    /// <summary>
    /// Retrieves default as part of the story page layout service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The story page layout produced by the operation.</returns>
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
    /// Performs normalize as part of the story page layout service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="pageWidthMm">Page width mm value supplied to the story page layout operation and used when producing its result.</param>
    /// <param name="pageHeightMm">Page height mm value supplied to the story page layout operation and used when producing its result.</param>
    /// <param name="marginTopMm">Margin top mm value supplied to the story page layout operation and used when producing its result.</param>
    /// <param name="marginRightMm">Margin right mm value supplied to the story page layout operation and used when producing its result.</param>
    /// <param name="marginBottomMm">Margin bottom mm value supplied to the story page layout operation and used when producing its result.</param>
    /// <param name="marginLeftMm">Margin left mm value supplied to the story page layout operation and used when producing its result.</param>
    /// <returns>The story page layout produced by the operation.</returns>
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
    /// Normalizes margin as part of the story page layout service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the story page layout operation and used when producing its result.</param>
    /// <param name="pageSize">Page size value supplied to the story page layout operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
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
    /// Normalizes pair as part of the story page layout service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="first">First value supplied to the story page layout operation and used when producing its result.</param>
    /// <param name="second">Second value supplied to the story page layout operation and used when producing its result.</param>
    /// <param name="pageSize">Page size value supplied to the story page layout operation and used when producing its result.</param>
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
