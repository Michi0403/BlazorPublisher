
namespace PublisherStudio.Services.Streaming.Capture;

/// <summary>
/// Represents a discovered native media device info application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Id">Identifier of the resource to use for this operation.</param>
/// <param name="Name">Name value supplied to the discovered native media device info operation and used when producing its result.</param>
/// <param name="Kind">Kind value supplied to the discovered native media device info operation and used when producing its result.</param>
/// <param name="Backend">Backend value supplied to the discovered native media device info operation and used when producing its result.</param>
/// <param name="ProcessId">Identifier of the process to use for this operation.</param>
/// <param name="WindowTitle">Window title value supplied to the discovered native media device info operation and used when producing its result.</param>
public sealed record DiscoveredNativeMediaDeviceInfo(
    string Id,
    string Name,
    string Kind,
    string Backend,
    string? ProcessId = null,
    string? WindowTitle = null);

/// <summary>
/// Platform-neutral facade for native media-device discovery. Host-specific DirectShow, AVFoundation,
/// V4L2 and process-loopback details are owned by the injected platform implementation.
/// </summary>
public sealed class NativeDeviceDiscovery(
    INativeDeviceDiscoveryPlatformService platformDiscovery,
    ILogger<NativeDeviceDiscovery> logger)
{
    public async Task<IReadOnlyList<DiscoveredNativeMediaDeviceInfo>> DiscoverAsync(
        string? ffmpegPath,
        CancellationToken cancellationToken)
    {
        try
        {
            var discovered = await platformDiscovery.DiscoverAsync(ffmpegPath, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Discovered {DeviceCount} native media devices.", discovered.Count);
            return discovered;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not discover native media devices.");
            throw;
        }
    }
}
