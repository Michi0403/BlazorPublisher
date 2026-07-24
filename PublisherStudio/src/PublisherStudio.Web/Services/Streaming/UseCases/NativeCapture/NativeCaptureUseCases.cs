namespace PublisherStudio.Services.Streaming.UseCases.NativeCapture;

/// <summary>
/// Coordinates native-capture lifecycle operations without exposing the registry to controllers.
/// </summary>
public sealed class NativeCaptureUseCases(NativeCaptureRegistry captures)
{
    private readonly NativeCaptureRegistry _captures = captures;

    public NativeCaptureSession Create(NativeCaptureRequest request) => _captures.Create(request);

    public bool TryGet(Guid captureId, out NativeCaptureSession capture) =>
        _captures.TryGet(captureId, out capture!);

    public bool Stop(Guid captureId) => _captures.Stop(captureId);
}
