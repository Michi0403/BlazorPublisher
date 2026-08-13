namespace PublisherStudio.BusinessObjects.Streaming;

/// <summary>
/// Represents the input contract for toggle, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
/// <param name="Enabled">Value indicating whether the option is enabled should apply to this operation.</param>
public sealed record ToggleRequest(bool Enabled);
/// <summary>
/// Represents the input contract for program page, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
/// <param name="PageId">Identifier of the page to use for this operation.</param>
public sealed record ProgramPageRequest(Guid PageId);
/// <summary>
/// Represents an ingest announcement application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Kind">Kind value supplied to the ingest announcement operation and used when producing its result.</param>
/// <param name="Url">Url value supplied to the ingest announcement operation and used when producing its result.</param>
/// <param name="Codec">Codec value supplied to the ingest announcement operation and used when producing its result.</param>
/// <param name="Width">Width value supplied to the ingest announcement operation and used when producing its result.</param>
/// <param name="Height">Height value supplied to the ingest announcement operation and used when producing its result.</param>
/// <param name="FrameRate">Frame rate value supplied to the ingest announcement operation and used when producing its result.</param>
/// <param name="OutputId">Identifier of the output to use for this operation.</param>
public sealed record IngestAnnouncement(string Kind, string Url, string Codec, int Width, int Height, int FrameRate, Guid? OutputId = null);
/// <summary>
/// Represents a media host hotkey event application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Command">Command value supplied to the media host hotkey event operation and used when producing its result.</param>
/// <param name="TargetId">Identifier of the target to use for this operation.</param>
/// <param name="TriggeredUtc">Triggered utc value supplied to the media host hotkey event operation and used when producing its result.</param>
public sealed record MediaHostHotkeyEvent(string Command, Guid? TargetId, DateTimeOffset TriggeredUtc);
/// <summary>
/// Represents a media hotkey application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="Id">Identifier of the resource to use for this operation.</param>
/// <param name="Gesture">Gesture value supplied to the media hotkey operation and used when producing its result.</param>
/// <param name="Command">Command value supplied to the media hotkey operation and used when producing its result.</param>
/// <param name="TargetId">Identifier of the target to use for this operation.</param>
/// <param name="Global">Value indicating whether global should apply to this operation.</param>
public sealed record MediaHotkey(Guid Id, string Gesture, string Command, Guid? TargetId, bool Global);

/// <summary>
/// Represents the input contract for native capture, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class NativeCaptureRequest
{
    /// <summary>
    /// Gets or sets the kind value that forms part of the native capture state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="NativeCaptureRequest"/>.</value>
    public string Kind { get; set; } = "Camera";
    /// <summary>
    /// Gets or sets the stable device identifier used to identify or correlate this native capture instance with related application state.
    /// </summary>
    /// <value>The device identifier value exposed by <see cref="NativeCaptureRequest"/>.</value>
    public string DeviceId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable audio device identifier used to identify or correlate this native capture instance with related application state.
    /// </summary>
    /// <value>The audio device identifier value exposed by <see cref="NativeCaptureRequest"/>.</value>
    public string AudioDeviceId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable application identifier used to identify or correlate this native capture instance with related application state.
    /// </summary>
    /// <value>The application identifier value exposed by <see cref="NativeCaptureRequest"/>.</value>
    public string ApplicationId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the native backend value that forms part of the native capture state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The native backend value exposed by <see cref="NativeCaptureRequest"/>.</value>
    public string NativeBackend { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the network URL that identifies the network or application endpoint associated with this native capture state.
    /// </summary>
    /// <value>The network URL value exposed by <see cref="NativeCaptureRequest"/>.</value>
    public string NetworkUrl { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether audio applies to the native capture state.
    /// </summary>
    /// <value>The include audio value exposed by <see cref="NativeCaptureRequest"/>.</value>
    public bool IncludeAudio { get; set; }
    /// <summary>
    /// Gets or sets the width value that forms part of the native capture state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The width value exposed by <see cref="NativeCaptureRequest"/>.</value>
    public int Width { get; set; } = 1920;
    /// <summary>
    /// Gets or sets the height value that forms part of the native capture state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The height value exposed by <see cref="NativeCaptureRequest"/>.</value>
    public int Height { get; set; } = 1080;
    /// <summary>
    /// Gets or sets the frame rate value that forms part of the native capture state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The frame rate value exposed by <see cref="NativeCaptureRequest"/>.</value>
    public int FrameRate { get; set; } = 60;
    /// <summary>
    /// Gets or sets a value indicating whether device timestamps applies to the native capture state.
    /// </summary>
    /// <value>The use device timestamps value exposed by <see cref="NativeCaptureRequest"/>.</value>
    public bool UseDeviceTimestamps { get; set; } = true;
    /// <summary>
    /// Gets or sets the FFmpeg path used by this native capture instance to locate the associated file-system resource.
    /// </summary>
    /// <value>The FFmpeg path value exposed by <see cref="NativeCaptureRequest"/>.</value>
    public string FfmpegPath { get; set; } = string.Empty;
}

/// <summary>
/// Represents the input contract for chat send, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
/// <param name="Message">Message value supplied to the chat send operation and used when producing its result.</param>
public sealed record ChatSendRequest(string Message);
