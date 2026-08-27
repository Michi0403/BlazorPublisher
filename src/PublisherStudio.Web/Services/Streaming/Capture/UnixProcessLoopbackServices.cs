namespace PublisherStudio.Services.Streaming.Capture;

/// <summary>
/// Unix process-loopback boundary. The Windows ApplicationLoopback COM APIs do not exist on
/// macOS/Linux, so this implementation reports the capability as unavailable without loading any
/// Windows native library.
/// </summary>
public sealed class UnixProcessLoopbackNativeService(
    ILogger<UnixProcessLoopbackNativeService> logger) : IProcessLoopbackNativeService
{
    public bool IsAvailable => false;

    public int InitializeMultithreadedApartment()
    {
        try
        {
            throw new PlatformNotSupportedException("Per-process native audio loopback is not available on this Unix host.");
        }
        catch (PlatformNotSupportedException exception)
        {
            logger.LogDebug(exception, "Unix process-loopback apartment initialization was rejected by the platform boundary.");
            throw;
        }
    }

    public void UninitializeApartment()
    {
        try
        {
            logger.LogTrace("Unix process-loopback apartment teardown is a no-op.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not complete Unix process-loopback apartment teardown.");
            throw;
        }
    }

    public int ActivateAudioInterface(
        string deviceInterfacePath,
        ref Guid interfaceId,
        IntPtr activationParameters,
        object completionHandler,
        out object activationOperation)
    {
        try
        {
            activationOperation = null!;
            throw new PlatformNotSupportedException("Per-process native audio loopback is not available on this Unix host.");
        }
        catch (PlatformNotSupportedException exception)
        {
            logger.LogDebug(exception, "Unix process-loopback activation was rejected by the platform boundary.");
            throw;
        }
    }
}

/// <summary>
/// Unix process-loopback factory used by common capture services. It provides an explicit platform
/// boundary instead of allowing Windows capture code to be constructed on macOS/Linux.
/// </summary>
public sealed class UnixProcessLoopbackCaptureFactory(
    ILogger<UnixProcessLoopbackCaptureFactory> logger) : IProcessLoopbackCaptureFactory
{
    public IProcessLoopbackCapture Create(uint processId, Stream destination, CancellationToken cancellationToken)
    {
        try
        {
            throw new PlatformNotSupportedException("Per-process native audio loopback is not available on this Unix host.");
        }
        catch (PlatformNotSupportedException exception)
        {
            logger.LogDebug(exception, "Unix process-loopback capture creation for process {ProcessId} was rejected by the platform boundary.", processId);
            throw;
        }
    }
}
