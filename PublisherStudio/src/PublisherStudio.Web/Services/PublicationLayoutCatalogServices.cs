using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Services;

public interface IPagePresetCatalog
{
    IReadOnlyList<PagePreset> GetAll();
    PagePreset? Find(string? key);
}

public sealed class PagePresetCatalog(
    IPublisherRuntimePolicyDataService runtimePolicy,
    ILogger<PagePresetCatalog> logger) : IPagePresetCatalog
{
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

public interface IStoryPageLayoutService
{
    StoryPageLayout GetDefault();
    StoryPageLayout Normalize(
        double pageWidthMm,
        double pageHeightMm,
        double marginTopMm,
        double marginRightMm,
        double marginBottomMm,
        double marginLeftMm);
}

public sealed class StoryPageLayoutService(
    IPublisherRuntimePolicyDataService runtimePolicy,
    ILogger<StoryPageLayoutService> logger) : IStoryPageLayoutService
{
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

    private double NormalizeMargin(double value, double pageSize) =>
        Math.Clamp(double.IsFinite(value) ? value : 0, 0, Math.Max(0, pageSize - 1));

    private void NormalizePair(ref double first, ref double second, double pageSize)
    {
        var maximum = Math.Max(1, pageSize - 1);
        var sum = first + second;
        if (sum <= maximum || sum <= 0) return;
        var scale = maximum / sum;
        first *= scale;
        second *= scale;
    }
}
