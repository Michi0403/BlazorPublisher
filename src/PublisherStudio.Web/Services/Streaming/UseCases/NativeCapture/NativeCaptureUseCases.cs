namespace PublisherStudio.Services.Streaming.UseCases.NativeCapture;

/// <summary>
/// Coordinates native-capture lifecycle operations without exposing the registry to controllers.
/// </summary>
/// <param name="captures">Native capture registry dependency used by the native capture use cases workflow to provide the corresponding application capability.</param>
public sealed class NativeCaptureUseCases(NativeCaptureRegistry captures)
{
    /// <summary>
    /// Stores the native capture registry dependency used by <see cref="NativeCaptureUseCases"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly NativeCaptureRegistry _captures = captures;

    /// <summary>
    /// Performs create for <see cref="NativeCaptureUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding native capture use cases workflow.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The native capture session produced by the operation.</returns>
    public NativeCaptureSession Create(NativeCaptureRequest request) {
    try
    {
        return _captures.Create(request);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method NativeCaptureUseCases.Create failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Attempts to get for <see cref="NativeCaptureUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding native capture use cases workflow.
    /// </summary>
    /// <param name="captureId">Identifier of the capture to use for this operation.</param>
    /// <param name="capture">Capture value supplied to the native capture use cases operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool TryGet(Guid captureId, out NativeCaptureSession capture) {
    try
    {
        return _captures.TryGet(captureId, out capture!);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method NativeCaptureUseCases.TryGet failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs stop for <see cref="NativeCaptureUseCases"/>, keeping the operation consistent with the state and invariants of the surrounding native capture use cases workflow.
    /// </summary>
    /// <param name="captureId">Identifier of the capture to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool Stop(Guid captureId) {
    try
    {
        return _captures.Stop(captureId);
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method NativeCaptureUseCases.Stop failed: {__serviceMethodException}");
        throw;
    }
}
}
