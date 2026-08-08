namespace PublisherStudio.Services.Streaming.UseCases.NativeCapture;

/// <summary>
/// Coordinates native-capture lifecycle operations without exposing the registry to controllers.
/// </summary>
public sealed class NativeCaptureUseCases(NativeCaptureRegistry captures)
{
    private readonly NativeCaptureRegistry _captures = captures;

    /// <summary>
    /// Runs the create operation.
    /// </summary>
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
    /// Attempts to get.
    /// </summary>
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
    /// Runs the stop operation.
    /// </summary>
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
