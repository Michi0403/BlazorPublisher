namespace PublisherStudio.Services.Streaming.Capture;

/// <summary>
/// Unix process-loopback boundary. The Windows ApplicationLoopback COM APIs do not exist on
/// macOS/Linux, so this implementation reports the capability as unavailable without loading any
/// Windows native library.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class UnixProcessLoopbackNativeService(
    ILogger<UnixProcessLoopbackNativeService> logger) : IProcessLoopbackNativeService
{
    /// <summary>
    /// Gets a value indicating whether available applies to the unix process loopback native state.
    /// </summary>
    /// <value>The is available value exposed by <see cref="UnixProcessLoopbackNativeService"/>.</value>
    public bool IsAvailable => false;

    /// <summary>
    /// Performs initialize multithreaded apartment as part of the unix process loopback native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The int produced by the operation.</returns>
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

    /// <summary>
    /// Performs uninitialize apartment as part of the unix process loopback native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
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

    /// <summary>
    /// Performs activate audio interface as part of the unix process loopback native service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="deviceInterfacePath">Device interface path value supplied to the unix process loopback native operation and used when producing its result.</param>
    /// <param name="interfaceId">Identifier of the interface to use for this operation.</param>
    /// <param name="activationParameters">Int ptr dependency used by the unix process loopback native workflow to provide the corresponding application capability.</param>
    /// <param name="completionHandler">Completion handler value supplied to the unix process loopback native operation and used when producing its result.</param>
    /// <param name="activationOperation">Activation operation value supplied to the unix process loopback native operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
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
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class UnixProcessLoopbackCaptureFactory(
    ILogger<UnixProcessLoopbackCaptureFactory> logger) : IProcessLoopbackCaptureFactory
{
    /// <summary>
    /// Performs create using the configuration and dependencies owned by <see cref="UnixProcessLoopbackCaptureFactory"/>.
    /// </summary>
    /// <param name="processId">Identifier of the process to use for this operation.</param>
    /// <param name="destination">Destination value supplied to the unix process loopback capture operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The i process loopback capture produced by the operation.</returns>
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
