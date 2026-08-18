using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Services.Streaming;

/// <summary>Represents a resolution, frame-rate, video-bitrate, audio-bitrate, and key-frame recommendation produced from runtime policy rather than machine-specific constants.</summary>
/// <param name="Width">Width value supplied to the media quality recommendation operation and used when producing its result.</param>
/// <param name="Height">Height value supplied to the media quality recommendation operation and used when producing its result.</param>
/// <param name="FrameRate">Frame rate value supplied to the media quality recommendation operation and used when producing its result.</param>
/// <param name="VideoBitrateKbps">Video bitrate kbps value supplied to the media quality recommendation operation and used when producing its result.</param>
/// <param name="AudioBitrateKbps">Audio bitrate kbps value supplied to the media quality recommendation operation and used when producing its result.</param>
/// <param name="KeyFrameIntervalSeconds">Key frame interval seconds value supplied to the media quality recommendation operation and used when producing its result.</param>
public sealed record MediaQualityRecommendation(
    int Width,
    int Height,
    int FrameRate,
    int VideoBitrateKbps,
    int AudioBitrateKbps,
    int KeyFrameIntervalSeconds);

/// <summary>Provides configurable adaptive media-quality recommendations shared by PublisherStudio recording and streaming workflows.</summary>
/// <param name="runtimePolicy">Publisher runtime policy data service dependency used by the media quality recommendation workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class MediaQualityRecommendationService(
    IPublisherRuntimePolicyDataService runtimePolicy,
    ILogger<MediaQualityRecommendationService> logger)
{
    /// <summary>Recommends a video bitrate for the supplied geometry, frame rate, content kind, codec, and publication quality profile.</summary>
    /// <param name="width">Width value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="height">Height value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="frameRate">Frame rate value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="contentKind">Content kind value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="codec">Codec value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="profile">Profile value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="maximumKbps">Maximum kbps value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    public int RecommendVideoBitrateKbps(
        int width,
        int height,
        int frameRate,
        string contentKind,
        string codec,
        PublicationAdaptiveQualityProfile profile,
        int? maximumKbps = null)
    {
        try
        {
            var defaults = runtimePolicy.MediaSessionDefaults;
            var policy = defaults.AdaptiveQuality;
            var bpp = contentKind.Trim().ToLowerInvariant() switch
            {
                "screen" or "detail" => policy.ScreenBitsPerPixel,
                "camera" or "motion" => policy.CameraBitsPerPixel,
                "lan" => policy.LanBitsPerPixel,
                "provider" or "output" => policy.ProviderBitsPerPixel,
                _ => policy.MixedBitsPerPixel
            };
            if (bpp <= 0)
                return Math.Clamp(defaults.VideoBitrateKbps, defaults.MinimumVideoBitrateKbps, maximumKbps ?? defaults.MaximumVideoBitrateKbps);

            var profileMultiplier = profile switch
            {
                PublicationAdaptiveQualityProfile.Efficiency => Positive(policy.EfficiencyMultiplier, 1d),
                PublicationAdaptiveQualityProfile.Balanced => Positive(policy.BalancedMultiplier, 1d),
                _ => Positive(policy.QualityMultiplier, 1d)
            };
            var codecFactor = codec.Trim().ToLowerInvariant() switch
            {
                "vp9" => Positive(policy.Vp9BitrateFactor, 1d),
                "vp8" => Positive(policy.Vp8BitrateFactor, 1d),
                "hevc" or "h265" => Positive(policy.HevcBitrateFactor, 1d),
                "av1" => Positive(policy.Av1BitrateFactor, 1d),
                _ => Positive(policy.H264BitrateFactor, 1d)
            };
            var safeWidth = Math.Clamp(width, defaults.MinimumWidth, defaults.MaximumWidth);
            var safeHeight = Math.Clamp(height, defaults.MinimumHeight, defaults.MaximumHeight);
            var safeFrameRate = Math.Clamp(frameRate, defaults.MinimumFrameRate, defaults.MaximumFrameRate);
            var calculated = (long)Math.Round(safeWidth * (double)safeHeight * safeFrameRate * bpp * profileMultiplier * codecFactor / 1000d, MidpointRounding.AwayFromZero);
            var maximum = Math.Clamp(maximumKbps ?? defaults.MaximumVideoBitrateKbps, defaults.MinimumVideoBitrateKbps, defaults.MaximumVideoBitrateKbps);
            return (int)Math.Clamp(calculated, defaults.MinimumVideoBitrateKbps, maximum);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not recommend an adaptive video bitrate for {Width}x{Height} at {FrameRate} fps.", width, height, frameRate);
            throw;
        }
    }

    /// <summary>Recommends an audio bitrate from the configured per-channel policy and publication quality profile.</summary>
    /// <param name="channels">Channels value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="profile">Profile value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="preferredKbps">Preferred kbps value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    public int RecommendAudioBitrateKbps(int channels, PublicationAdaptiveQualityProfile profile, int? preferredKbps = null)
    {
        try
        {
            var policy = runtimePolicy.MediaSessionDefaults.AdaptiveQuality;
            var safeChannels = Math.Max(1, channels > 0 ? channels : Math.Max(1, policy.DefaultAudioChannels));
            var multiplier = profile switch
            {
                PublicationAdaptiveQualityProfile.Efficiency => Positive(policy.EfficiencyMultiplier, 1d),
                PublicationAdaptiveQualityProfile.Balanced => Positive(policy.BalancedMultiplier, 1d),
                _ => Positive(policy.QualityMultiplier, 1d)
            };
            var calculated = (int)Math.Round(Math.Max(1, policy.AudioBitratePerChannelKbps) * safeChannels * multiplier, MidpointRounding.AwayFromZero);
            if (preferredKbps is > 0)
                calculated = Math.Max(calculated, preferredKbps.Value);
            return Math.Clamp(calculated, Math.Max(32, policy.MinimumAudioBitrateKbps), Math.Max(Math.Max(32, policy.MinimumAudioBitrateKbps), policy.MaximumAudioBitrateKbps));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not recommend an adaptive audio bitrate for {Channels} channel(s).", channels);
            throw;
        }
    }

    /// <summary>Applies the configured provider quality knowledge while never requiring an upscale beyond the publication master.</summary>
    /// <param name="provider">Publication stream provider dependency used by the media quality recommendation workflow to provide the corresponding application capability.</param>
    /// <param name="preset">Preset value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="masterWidth">Master width value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="masterHeight">Master height value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="masterFrameRate">Master frame rate value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="codec">Codec value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="profile">Profile value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="useProviderKnowledge">Value indicating whether use provider knowledge should apply to this operation.</param>
    /// <returns>The media quality recommendation produced by the operation.</returns>
    public MediaQualityRecommendation RecommendProviderOutput(
        PublicationStreamProvider provider,
        PublicationStreamQualityPreset preset,
        int masterWidth,
        int masterHeight,
        int masterFrameRate,
        PublicationStreamVideoCodec codec,
        PublicationAdaptiveQualityProfile profile,
        bool useProviderKnowledge = true)
    {
        try
        {
            var defaults = runtimePolicy.MediaSessionDefaults;
            var adaptive = defaults.AdaptiveQuality;
            var fallbackPolicy = adaptive.ProviderProfiles.FirstOrDefault(item => string.Equals(item.Provider, "Default", StringComparison.OrdinalIgnoreCase));
            var providerPolicy = useProviderKnowledge
                ? adaptive.ProviderProfiles.FirstOrDefault(item => string.Equals(item.Provider, provider.ToString(), StringComparison.OrdinalIgnoreCase)) ?? fallbackPolicy
                : fallbackPolicy;
            var requestedPreset = preset == PublicationStreamQualityPreset.Custom ? PublicationStreamQualityPreset.Recommended : preset;
            var tier = providerPolicy?.Tiers.FirstOrDefault(item => string.Equals(item.Preset, requestedPreset.ToString(), StringComparison.OrdinalIgnoreCase))
                ?? providerPolicy?.Tiers.FirstOrDefault(item => string.Equals(item.Preset, PublicationStreamQualityPreset.Recommended.ToString(), StringComparison.OrdinalIgnoreCase));

            var tierWidth = tier?.Width > 0 ? tier.Width : defaults.OutputWidth;
            var tierHeight = tier?.Height > 0 ? tier.Height : defaults.OutputHeight;
            var fitted = FitWithin(
                tierWidth,
                tierHeight,
                masterWidth > 0 ? masterWidth : defaults.MasterWidth,
                masterHeight > 0 ? masterHeight : defaults.MasterHeight,
                defaults.MinimumWidth,
                defaults.MinimumHeight);
            var frameRate = Math.Clamp(
                Math.Min(tier?.FrameRate > 0 ? tier.FrameRate : defaults.OutputFrameRate, masterFrameRate > 0 ? masterFrameRate : defaults.MasterFrameRate),
                defaults.MinimumFrameRate,
                defaults.MaximumFrameRate);
            var maximumBitrate = tier?.MaximumVideoBitrateKbps > 0 ? tier.MaximumVideoBitrateKbps : defaults.MaximumVideoBitrateKbps;
            var videoBitrate = RecommendVideoBitrateKbps(fitted.Width, fitted.Height, frameRate, "provider", codec.ToString(), profile, maximumBitrate);
            var audioBitrate = RecommendAudioBitrateKbps(adaptive.DefaultAudioChannels, profile, tier?.AudioBitrateKbps);
            var keyFrames = tier?.KeyFrameIntervalSeconds > 0 ? tier.KeyFrameIntervalSeconds : Math.Max(1, defaults.KeyFrameIntervalSeconds);
            return new(fitted.Width, fitted.Height, frameRate, videoBitrate, audioBitrate, keyFrames);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not recommend adaptive provider settings for {Provider} and {Preset}.", provider, preset);
            throw;
        }
    }

    /// <summary>Recommends LAN video and audio settings from the configured adaptive LAN policy and current master geometry.</summary>
    /// <param name="masterWidth">Master width value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="masterHeight">Master height value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="masterFrameRate">Master frame rate value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="profile">Profile value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <returns>The media quality recommendation produced by the operation.</returns>
    public MediaQualityRecommendation RecommendLan(
        int masterWidth,
        int masterHeight,
        int masterFrameRate,
        PublicationAdaptiveQualityProfile profile)
    {
        try
        {
            var defaults = runtimePolicy.MediaSessionDefaults;
            var adaptive = defaults.AdaptiveQuality;
            var maximumWidth = adaptive.LanMaximumWidth > 0 ? Math.Min(adaptive.LanMaximumWidth, defaults.MaximumWidth) : defaults.MaximumWidth;
            var maximumHeight = adaptive.LanMaximumHeight > 0 ? Math.Min(adaptive.LanMaximumHeight, defaults.MaximumHeight) : defaults.MaximumHeight;
            var fitted = FitWithin(
                masterWidth > 0 ? masterWidth : defaults.MasterWidth,
                masterHeight > 0 ? masterHeight : defaults.MasterHeight,
                maximumWidth,
                maximumHeight,
                defaults.MinimumWidth,
                defaults.MinimumHeight);
            var maximumFrameRate = adaptive.LanMaximumFrameRate > 0 ? Math.Min(adaptive.LanMaximumFrameRate, defaults.MaximumFrameRate) : defaults.MaximumFrameRate;
            var frameRate = Math.Clamp(Math.Min(masterFrameRate > 0 ? masterFrameRate : defaults.MasterFrameRate, maximumFrameRate), defaults.MinimumFrameRate, defaults.MaximumFrameRate);
            var videoBitrate = RecommendVideoBitrateKbps(fitted.Width, fitted.Height, frameRate, "lan", "h264", profile);
            var audioBitrate = RecommendAudioBitrateKbps(adaptive.DefaultAudioChannels, profile);
            return new(fitted.Width, fitted.Height, frameRate, videoBitrate, audioBitrate, Math.Max(1, defaults.KeyFrameIntervalSeconds));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not recommend adaptive LAN settings.");
            throw;
        }
    }

    /// <summary>
    /// Performs fit within as part of the media quality recommendation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="sourceWidth">Source width value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="sourceHeight">Source height value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="maximumWidth">Maximum width value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="maximumHeight">Maximum height value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="minimumWidth">Minimum width value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="minimumHeight">Minimum height value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <returns>The int width int height produced by the operation.</returns>
    private (int Width, int Height) FitWithin(int sourceWidth, int sourceHeight, int maximumWidth, int maximumHeight, int minimumWidth, int minimumHeight)
    {
        try
        {
            var width = Math.Max(minimumWidth, sourceWidth);
            var height = Math.Max(minimumHeight, sourceHeight);
            var scale = Math.Min(1d, Math.Min(maximumWidth / (double)Math.Max(1, width), maximumHeight / (double)Math.Max(1, height)));
            width = Math.Max(minimumWidth, (int)Math.Round(width * scale, MidpointRounding.AwayFromZero));
            height = Math.Max(minimumHeight, (int)Math.Round(height * scale, MidpointRounding.AwayFromZero));
            if ((width & 1) != 0) width--;
            if ((height & 1) != 0) height--;
            return (Math.Max(minimumWidth, width), Math.Max(minimumHeight, height));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not fit adaptive media geometry within configured bounds.");
            throw;
        }
    }

    /// <summary>
    /// Performs positive as part of the media quality recommendation service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the media quality recommendation operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double Positive(double value, double fallback)
    {
        try
        {
            return double.IsFinite(value) && value > 0 ? value : fallback;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not normalize an adaptive media policy multiplier.");
            throw;
        }
    }
}
