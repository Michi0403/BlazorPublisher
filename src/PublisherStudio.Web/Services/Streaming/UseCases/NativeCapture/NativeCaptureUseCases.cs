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
    public NativeCaptureSession Create(NativeCaptureRequest request) => _captures.Create(request);

    /// <summary>
    /// Attempts to get.
    /// </summary>
    public bool TryGet(Guid captureId, out NativeCaptureSession capture) =>
        _captures.TryGet(captureId, out capture!);

    /// <summary>
    /// Runs the stop operation.
    /// </summary>
    public bool Stop(Guid captureId) => _captures.Stop(captureId);
}
