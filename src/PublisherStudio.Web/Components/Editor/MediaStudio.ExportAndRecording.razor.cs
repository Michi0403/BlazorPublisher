using System.Globalization;
using System.Text;
using DevExpress.Blazor;
using DevExpress.Blazor.Office;
using DevExpress.Blazor.RichEdit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using static Microsoft.AspNetCore.Components.Web.RenderMode;
using Microsoft.JSInterop;
using Microsoft.Extensions.Options;
using PublisherStudio;
using PublisherStudio.Components;
using PublisherStudio.Components.Editor;
using PublisherStudio.Components.Layout;
using PublisherStudio.Components.OrganicPlugins;
using PublisherStudio.Components.Shared;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services;
using PublisherStudio.Services.Configuration;
using PublisherStudio.Services.Documentation;
using PublisherStudio.Services.MediaConversion;
using PublisherStudio.Services.MediaStudio.UseCases;
using PublisherStudio.Services.OrganicPlugins;
using PublisherStudio.Services.Panels;
using PublisherStudio.Services.PictureStudio.Import;
using PublisherStudio.Services.Publication;
using PublisherStudio.Services.Streaming.MediaHost;
using PublisherStudio.Services.Streaming.Profiles;
using PublisherStudio.Services.Streaming.UseCases.Editor;
using PublisherStudio.Services.UserExperience;
using PublisherStudio.Services.VideoStudio.Export;
using PublisherStudio.Services.VideoStudio.Import;
using System.Text.Json;
using System.Net;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PublisherStudio.Components.Editor;

/// <summary>Maintains the existing MediaStudio component behavior as a cohesive, independently reviewable partial declaration without changing its state or public surface.</summary>
public partial class MediaStudio
{
    /// <summary>
    /// Performs render effects to video for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RenderEffectsToVideo()
    {
        try
        {
            if (!IsVideo || !HasSource || SelectedSegment is null || !IsPlayableSegment(SelectedSegment) || _renderingVideoExport) return;
            _error = null;
            try
            {
                var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
                var start = _videoSelectionIsPoint ? _trimStart : VideoSelectionStart;
                var end = _videoSelectionIsPoint ? _trimEnd : VideoSelectionEnd;
                if (end <= start + .001) { start = _trimStart; end = _trimEnd; }
                var started = await module.InvokeAsync<bool>(
                    "startRenderedVideoExport",
                    "media-studio-preview",
                    _self,
                    BuildVideoEffectConfiguration(RecordingTargetWidth, RecordingTargetHeight),
                    BuildBrowserRecordingOptions(),
                    start,
                    end,
                    Math.Max(.1, _playbackRate),
                    _muted ? 0 : Math.Clamp(_volume, 0, 1),
                    _name).ConfigureAwait(true);
                if (!started)
                {
                    _warning = LT("A rendered video export is already running.");
                    return;
                }
                _renderingVideoExport = true;
                _warning = LT("Rendering the selected range with the current Video Studio layers and effects. The browser writes only the video canvas and audio stream; player controls and editor chrome are not captured.");
                await InvokeAsync(StateHasChanged).ConfigureAwait(true);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException or JSException)
            {
                _renderingVideoExport = false;
                _error = $"{LT("Rendered video export could not start")}: {ex.Message}";
                Logger.LogError(ex, "Rendered Video Studio export could not start for session {SessionId}.", SessionId);
                OperationalNotifications.Error(_error, LT("Rendered video export failed"), nameof(MediaStudio));
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(RenderEffectsToVideo)} failed.");
            throw;
        }
    }

    /// <summary>Exports the selected Video Studio range and active effect layers as animated GIF.</summary>
    private async Task RenderEffectsToGif()
    {
        if (!IsVideo || !HasSource || SelectedSegment is null || !IsPlayableSegment(SelectedSegment) ||
            _renderingVideoExport || _renderingTimelineSelectionExport) return;
        _error = null;
        try
        {
            var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
            var start = _videoSelectionIsPoint ? _trimStart : VideoSelectionStart;
            var end = _videoSelectionIsPoint ? _trimEnd : VideoSelectionEnd;
            if (end <= start + .001) { start = _trimStart; end = _trimEnd; }
            var started = await module.InvokeAsync<bool>("startRenderedGifExport",
                "media-studio-preview", _self,
                BuildVideoEffectConfiguration(RecordingTargetWidth, RecordingTargetHeight),
                BuildBrowserRecordingOptions(), start, end, Math.Max(.1, _playbackRate), _name).ConfigureAwait(true);
            if (!started) { _warning = LT("A rendered video export is already running."); return; }
            _renderingVideoExport = true;
            _warning = LT("Rendering the selected range as animated GIF with the current effect layers (GIF has no audio).");
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException or JSException)
        {
            _renderingVideoExport = false;
            _error = $"{LT("Animated GIF export could not start")}: {ex.Message}";
            Logger.LogError(ex, "Video Studio GIF export could not start for session {SessionId}.", SessionId);
        }
    }

    /// <summary>
    /// Determines whether cel rendered video export for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task CancelRenderedVideoExport()
    {
        try
        {
            try
            {
                if (_module is not null) await _module.InvokeVoidAsync("cancelRenderedVideoExport", "media-studio-preview").ConfigureAwait(true);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException or JSException)
            {
                Logger.LogDebug(ex, "Rendered Video Studio export cancellation could not reach the browser runtime.");
            }
            finally
            {
                _renderingVideoExport = false;
                _renderingTimelineSelectionExport = false;
                await InvokeAsync(StateHasChanged).ConfigureAwait(true);
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(CancelRenderedVideoExport)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs rendered video export ready for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="fileName">File name value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="mimeType">Mime type value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="sizeBytes">Size bytes value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="width">Width value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="height">Height value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="durationSeconds">Duration seconds value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public async Task RenderedVideoExportReady(string fileName, string mimeType, long sizeBytes, int width, int height, double durationSeconds)
    {
        try
        {
            _renderingVideoExport = false;
            _warning = $"{LT("Rendered video exported")}: {fileName} · {width} × {height} · {Time(durationSeconds)} · {FormatBytes(sizeBytes)} · {mimeType}.";
            Logger.LogInformation("Rendered Video Studio export {FileName} completed at {Width}x{Height} with {SizeBytes} bytes and MIME {MimeType}.", fileName, width, height, sizeBytes, mimeType);
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(RenderedVideoExportReady)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs rendered video export failed for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="message">Message value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public async Task RenderedVideoExportFailed(string message)
    {
        try
        {
            _renderingVideoExport = false;
            _error = $"{LT("Rendered video export failed")}: {message}";
            Logger.LogError("Rendered Video Studio export failed: {RenderError}", message);
            OperationalNotifications.Error(_error, LT("Rendered video export failed"), nameof(MediaStudio));
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(RenderedVideoExportFailed)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Starts recording for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="source">Source value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task StartRecording(string source)
    {
        try
        {
            _error = null;
            try
            {
                var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
                var recordingOptions = BuildBrowserRecordingOptions();
                Logger.LogInformation(
                    "Starting Media Studio {MediaKind} recording from {RecordingSource} for session {SessionId} with capture mode {CaptureMode}, target {CaptureWidth}x{CaptureHeight} at {FrameRate} fps and {VideoBitrateKbps} kbps video.",
                    IsVideo ? "video" : "audio",
                    source,
                    SessionId,
                    _recordingSizeMode,
                    RecordingTargetWidth,
                    RecordingTargetHeight,
                    _recordingFrameRate,
                    _recordingVideoBitrateKbps);
                _recording = true;
                _recordingStopping = false;
                var startInfo = await module.InvokeAsync<MediaRecordingStartInfo?>(
                    "startMediaRecording",
                    "media-studio-preview",
                    IsVideo ? "video" : "audio",
                    source,
                    _self,
                    recordingOptions).ConfigureAwait(true);
                if (IsVideo && startInfo is { Width: > 0, Height: > 0 })
                {
                    var codec = string.IsNullOrWhiteSpace(startInfo.MimeType) ? "browser codec" : startInfo.MimeType;
                    var bitrate = startInfo.VideoBitsPerSecond > 0 ? $" · {startInfo.VideoBitsPerSecond / 1000:N0} kbps" : string.Empty;
                    _warning = $"Recording {startInfo.Width} × {startInfo.Height} · {startInfo.FrameRate:0.##} fps · {codec}{bitrate}.";
                }
                await InvokeAsync(StateHasChanged).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    "Starting Media Studio {MediaKind} recording from {RecordingSource} failed for session {SessionId}.",
                    IsVideo ? "video" : "audio",
                    source,
                    SessionId);
                _recording = false;
                _recordingStopping = false;
                _error = ex.Message;
                await InvokeAsync(StateHasChanged).ConfigureAwait(true);
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(StartRecording)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Stops recording for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task StopRecording()
    {
        try
        {
            if (_module is null || !_recording || _recordingStopping) return;
            _error = null;
            _recordingStopping = true;
            _warning = "Finalizing the browser recording…";
            try
            {
                Logger.LogInformation(
                    "Stopping Media Studio {MediaKind} recording for session {SessionId}; browser finalization continues asynchronously.",
                    IsVideo ? "video" : "audio",
                    SessionId);
                await InvokeAsync(StateHasChanged).ConfigureAwait(true);
                var stopRequested = await _module.InvokeAsync<bool>(
                    "stopMediaRecording",
                    "media-studio-preview",
                    _self).ConfigureAwait(true);
                if (!stopRequested)
                {
                    _recording = false;
                    _recordingStopping = false;
                    _error = "The browser no longer has an active or retained recording to stop.";
                    Logger.LogWarning(
                        "Media Studio Stop found no active or retained {MediaKind} recording for session {SessionId}.",
                        IsVideo ? "video" : "audio",
                        SessionId);
                    await InvokeAsync(StateHasChanged).ConfigureAwait(true);
                }
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException)
            {
                Logger.LogWarning(ex,
                    "Media Studio disconnected after requesting stop for {MediaKind} recording session {SessionId}; browser-owned finalization can be recovered after reconnect.",
                    IsVideo ? "video" : "audio",
                    SessionId);
                _warning = "The UI disconnected after Stop was requested. Reconnect this studio to recover the browser-owned recording.";
                await InvokeAsync(StateHasChanged).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    "Requesting Stop for Media Studio {MediaKind} recording failed for session {SessionId}.",
                    IsVideo ? "video" : "audio",
                    SessionId);
                _recordingStopping = false;
                _error = $"The recording could not be stopped: {ex.Message}";
                await InvokeAsync(StateHasChanged).ConfigureAwait(true);
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(StopRecording)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Generates tone for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task GenerateTone()
    {
        try
        {
            try
            {
                var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
                await module.InvokeVoidAsync("clearRetainedRecording", "media-studio-preview").ConfigureAwait(true);
                ClearRetainedRecordingState();
                _dataUrl = CreateToneDataUrl(3, 440, .32);
                _mimeType = "audio/wav";
                _name = "Generated A4 tone.wav";
                RefreshPreviewSource();
                await InspectSource().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _error = ex.Message;
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(GenerateTone)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs clear retained recording state for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void ClearRetainedRecordingState()
    {
        try
        {
            _hasRetainedRecording = false;
            _retainedRecordingSizeBytes = 0;
            _retainedRecordingFileName = string.Empty;
            _retainedRecordingObjectUrl = string.Empty;
            _embedding = false;
            _embeddingProgress = 0;
            _recordingPlacementRequired = false;
            _recordingPlacementBoundary = 0;
            _pendingRecordingInsertTimelineSeconds = null;
            _retainedRecordingCommittedToSequence = false;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ClearRetainedRecordingState)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs media recording cleared for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public Task MediaRecordingCleared()
    {
        try
        {
            ClearRetainedRecordingState();
            _dataUrl = string.Empty;
            RetirePreviewAssets();
            _previewSource = string.Empty;
            _duration = 0;
            _trimStart = 0;
            _trimEnd = 0;
            _posterDataUrl = string.Empty;
            _waveform = [];
            _fadeIn = 0;
            _fadeOut = 0;
            return InvokeAsync(StateHasChanged);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(MediaRecordingCleared)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Applies retained recording info for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="info">Info value supplied to the media studio operation and used when producing its result.</param>
    private void ApplyRetainedRecordingInfo(RetainedMediaRecordingInfo info)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(info);
            _recording = false;
            _recordingStopping = false;
            _recordingBuffer = null;
            _recordingId = null;
            _hasRetainedRecording = info.SizeBytes > 0 && !string.IsNullOrWhiteSpace(info.ObjectUrl);
            _retainedRecordingSizeBytes = Math.Max(0, info.SizeBytes);
            _retainedRecordingFileName = string.IsNullOrWhiteSpace(info.FileName)
                ? RecordingName(info.MimeType, IsVideo)
                : info.FileName;
            _retainedRecordingObjectUrl = info.ObjectUrl ?? string.Empty;
            _retainedRecordingCommittedToSequence = _retainedRecordingCommittedToSequence || info.CommittedToSequence;
            _previewSource = _retainedRecordingObjectUrl;
            _mimeType = NormalizeMimeType(info.MimeType, IsVideo ? "video/webm" : "audio/webm");
            _name = _retainedRecordingFileName;
            _duration = Math.Max(.01, info.DurationSeconds);
            _trimStart = 0;
            _trimEnd = _duration;
            _posterDataUrl = info.PosterDataUrl ?? string.Empty;
            _waveform = info.WaveformSamples ?? [];
            _fadeIn = _fadeOut = 0;
            _error = null;
            _warning = string.IsNullOrWhiteSpace(info.MetadataWarning)
                ? "Recording completed. It remains available for download until this studio closes."
                : $"Recording completed and remains downloadable. Browser metadata warning: {info.MetadataWarning}";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ApplyRetainedRecordingInfo)} failed.");
            throw;
        }
    }

    /// <summary>Requires every newly completed video recording to become an explicit sequence clip before another capture can replace its browser-retained Blob.</summary>
    private void RequireRecordingPlacementIfNeeded()
    {
        try
        {
            if (!IsVideo || !HasRetainedRecording || _retainedRecordingCommittedToSequence) return;
            _recordingPlacementRequired = true;
            _recordingPlacementBoundary = _plannedRecordingPlacementBoundary.HasValue
                ? Math.Clamp(_plannedRecordingPlacementBoundary.Value, 0, _segments.Count)
                : SelectedSegmentIndex >= 0
                    ? Math.Clamp(SelectedSegmentIndex + 1, 0, _segments.Count)
                    : _segments.Count;
            _warning = _segments.Count == 0
                ? LT("Recording completed. Insert it as the first sequence clip before recording again.")
                : LT("Recording completed. Choose whether it belongs first, between existing clips, or last.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Preparing the completed recording placement failed for session {SessionId}.", SessionId);
            OperationalNotifications.Error(ex.Message, LT("Recording placement failed"), nameof(MediaStudio));
        }
    }

    /// <summary>Formats one insertion boundary in the existing video sequence.</summary>
    /// <param name="boundary">Zero-based boundary between existing sequence elements.</param>
    /// <returns>A human-readable placement label.</returns>
    private string RecordingPlacementLabel(int boundary)
    {
        try
        {
            if (_segments.Count == 0) return LT("Only video");
            if (boundary <= 0) return $"{LT("First")} — {LT("before")} {_segments[0].Name}";
            if (boundary >= _segments.Count) return $"{LT("Last")} — {LT("after")} {_segments[^1].Name}";
            return $"{LT("Between")} {_segments[boundary - 1].Name} / {_segments[boundary].Name}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Formatting recording placement boundary {Boundary} failed.", boundary);
            return LT("Recording position");
        }
    }

    /// <summary>Changes the requested sequence boundary for the new recording.</summary>
    /// <param name="args">Browser change event containing the selected boundary.</param>
    private void ChangeRecordingPlacementBoundary(ChangeEventArgs args)
    {
        try
        {
            if (int.TryParse(args.Value?.ToString(), out var boundary))
                _recordingPlacementBoundary = Math.Clamp(boundary, 0, _segments.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Changing the recording placement boundary failed.");
            OperationalNotifications.Error(ex.Message, LT("Recording placement failed"), nameof(MediaStudio));
        }
    }

    /// <summary>Embeds the retained browser recording and inserts it at the explicitly chosen sequence boundary.</summary>
    /// <returns>A task that completes when the recording has been embedded or the operation has failed safely.</returns>
    private async Task ConfirmRecordingPlacement()
    {
        try
        {
            if (!_recordingPlacementRequired || !HasRetainedRecording || _embedding) return;
            var boundary = Math.Clamp(_recordingPlacementBoundary, 0, _segments.Count);
            _pendingRecordingInsertTimelineSeconds = boundary <= 0
                ? 0
                : boundary >= _segments.Count
                    ? SequenceDuration
                    : TimelineEdits.SegmentTimelineStart(_segments, boundary, _playbackRate);
            _recordingPlacementRequired = false;
            _embedding = true;
            _embeddingProgress = 0;
            _error = null;
            _warning = LT("Embedding the recording at the chosen sequence boundary…");
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
            try
            {
                var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
                await module.InvokeAsync<bool>("embedRetainedRecording", "media-studio-preview").ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _embedding = false;
                _pendingRecordingInsertTimelineSeconds = null;
                _recordingPlacementRequired = true;
                Logger.LogError(ex, "Embedding the newly placed recording failed for session {SessionId}.", SessionId);
                _error = $"{LT("The recording could not be inserted.")} {ex.Message}";
                OperationalNotifications.Error(_error, LT("Recording placement failed"), nameof(MediaStudio));
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Confirming the recording placement failed for session {SessionId}.", SessionId);
            _error = ex.Message;
            OperationalNotifications.Error(ex.Message, LT("Recording placement failed"), nameof(MediaStudio));
        }
    }

    /// <summary>Keeps the completed browser recording downloadable without inserting it into the current sequence.</summary>
    private void CancelRecordingPlacement()
    {
        try
        {
            _recordingPlacementRequired = false;
            _pendingRecordingInsertTimelineSeconds = null;
            _plannedRecordingPlacementBoundary = null;
            _warning = LT("Recording kept for download outside the sequence. PublisherStudio will ask for a sequence position again before another video recording can start.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Cancelling recording placement failed for session {SessionId}.", SessionId);
            OperationalNotifications.Error(ex.Message, LT("Recording placement failed"), nameof(MediaStudio));
        }
    }

    /// <summary>
    /// Performs media recording ready for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="info">Info value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public async Task MediaRecordingReady(RetainedMediaRecordingInfo info)
    {
        try
        {
            try
            {
                ApplyRetainedRecordingInfo(info);
                RequireRecordingPlacementIfNeeded();
                Logger.LogInformation(
                    "Browser retained Media Studio {MediaKind} recording {FileName} ({SizeBytes} bytes) for session {SessionId}.",
                    IsVideo ? "video" : "audio",
                    _retainedRecordingFileName,
                    _retainedRecordingSizeBytes,
                    SessionId);
                await InvokeAsync(StateHasChanged).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Applying the completed Media Studio recording failed for session {SessionId}.", SessionId);
                _recording = false;
                _recordingStopping = false;
                _error = $"The completed recording could not be restored in the studio: {ex.Message}";
                await InvokeAsync(StateHasChanged).ConfigureAwait(true);
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(MediaRecordingReady)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Applies metadata that the browser resolved after the completed recording was already made
    /// available for download, editing, insertion, or replacement.
    /// </summary>
    /// <param name="info">The enriched retained-recording metadata for the current browser Blob.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public async Task MediaRecordingMetadataReady(RetainedMediaRecordingInfo info)
    {
        try
        {
            try
            {
                if (!HasRetainedRecording
                    || string.IsNullOrWhiteSpace(info.ObjectUrl)
                    || !string.Equals(_retainedRecordingObjectUrl, info.ObjectUrl, StringComparison.Ordinal))
                    return;

                var previousDuration = Math.Max(.01, _duration);
                var resolvedDuration = Math.Max(.01, info.DurationSeconds);
                var representedFullClip = _trimStart <= .001 && Math.Abs(_trimEnd - previousDuration) <= Math.Max(.02, previousDuration / 10000d);
                _duration = resolvedDuration;
                if (representedFullClip)
                {
                    _trimStart = 0;
                    _trimEnd = resolvedDuration;
                    if (!_videoSelectionUserCommitted)
                    {
                        _videoSelectionPointSeconds = 0;
                        _videoSelectionStartSeconds = 0;
                        _videoSelectionEndSeconds = resolvedDuration;
                    }
                }
                else
                {
                    _trimStart = Math.Clamp(_trimStart, 0, resolvedDuration);
                    _trimEnd = Math.Clamp(_trimEnd, _trimStart, resolvedDuration);
                    _videoSelectionPointSeconds = Math.Clamp(_videoSelectionPointSeconds, 0, resolvedDuration);
                    _videoSelectionStartSeconds = Math.Clamp(_videoSelectionStartSeconds, 0, resolvedDuration);
                    _videoSelectionEndSeconds = Math.Clamp(_videoSelectionEndSeconds, _videoSelectionStartSeconds, resolvedDuration);
                }

                if (!string.IsNullOrWhiteSpace(info.PosterDataUrl)) _posterDataUrl = info.PosterDataUrl;
                if (info.WaveformSamples is { Count: > 0 }) _waveform = info.WaveformSamples;
                if (info.CommittedToSequence) _retainedRecordingCommittedToSequence = true;
                _warning = string.IsNullOrWhiteSpace(info.MetadataWarning)
                    ? "Recording completed. Browser metadata was resolved without blocking Save, Insert, Replace, or Download."
                    : $"Recording completed and remains editable. Browser metadata warning: {info.MetadataWarning}";
                Logger.LogInformation(
                    "Browser enriched retained Media Studio {MediaKind} recording metadata to {DurationSeconds:F3}s for session {SessionId}.",
                    IsVideo ? "video" : "audio",
                    resolvedDuration,
                    SessionId);
                await InvokeAsync(StateHasChanged).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Applying enriched Media Studio recording metadata failed for session {SessionId}.", SessionId);
                _warning = "The recording is available, but optional browser metadata enrichment failed. Save, Insert, Replace, and Download remain available.";
                await InvokeAsync(StateHasChanged).ConfigureAwait(true);
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(MediaRecordingMetadataReady)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs begin media recording transfer for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="transferId">Identifier of the transfer to use for this operation.</param>
    /// <param name="mimeType">Mime type value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="totalBytes">Total bytes value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="chunkCount">Chunk count value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    [JSInvokable]
    public bool BeginMediaRecordingTransfer(string transferId, string mimeType, long totalBytes, int chunkCount)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(transferId) || totalBytes <= 0 || chunkCount <= 0) return false;
            _recordingId = transferId;
            _mimeType = NormalizeMimeType(mimeType, IsVideo ? "video/webm" : "audio/webm");
            _recordingBuffer = new StringBuilder();
            _recordingExpectedChunks = chunkCount;
            _recordingNextChunk = 0;
            _recordingExpectedBytes = totalBytes;
            _embedding = true;
            _embeddingProgress = 0;
            _error = null;
            _warning = "Embedding the completed recording into the publication…";
            TaskRunner.Run(nameof(MediaStudio), nameof(BeginMediaRecordingTransfer), async _ => await InvokeAsync(StateHasChanged).ConfigureAwait(true));
            return true;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(BeginMediaRecordingTransfer)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs append media recording chunk for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="transferId">Identifier of the transfer to use for this operation.</param>
    /// <param name="chunkIndex">Chunk index value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="chunk">Chunk value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    [JSInvokable]
    public bool AppendMediaRecordingChunk(string transferId, int chunkIndex, string chunk)
    {
        try
        {
            if (_recordingId != transferId || _recordingBuffer is null || chunkIndex != _recordingNextChunk || string.IsNullOrEmpty(chunk))
                return false;
            _recordingBuffer.Append(chunk);
            _recordingNextChunk++;
            return true;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(AppendMediaRecordingChunk)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs media recording transfer progress for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="transferredBytes">Transferred bytes value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="totalBytes">Total bytes value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public Task MediaRecordingTransferProgress(long transferredBytes, long totalBytes)
    {
        try
        {
            var total = Math.Max(1, totalBytes > 0 ? totalBytes : _recordingExpectedBytes);
            _embeddingProgress = Math.Clamp(transferredBytes * 100d / total, 0, 100);
            return InvokeAsync(StateHasChanged);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(MediaRecordingTransferProgress)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Completes media recording transfer for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="transferId">Identifier of the transfer to use for this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public async Task CompleteMediaRecordingTransfer(string transferId)
    {
        try
        {
            if (_recordingId != transferId || _recordingBuffer is null || _recordingNextChunk != _recordingExpectedChunks)
                throw new InvalidDataException("The recording transfer did not complete in the expected order.");

            _dataUrl = $"data:{_mimeType};base64,{_recordingBuffer}";
            _recordingBuffer = null;
            _recordingId = null;
            _recordingExpectedChunks = 0;
            _recordingNextChunk = 0;
            _recordingExpectedBytes = 0;
            _recording = false;
            _embedding = false;
            _embeddingProgress = 100;
            _warning = "Recording embedded. The original browser recording is still available for download until this studio closes.";
            if (_pendingRecordingInsertTimelineSeconds.HasValue)
            {
                var insertionTimeline = Math.Clamp(_pendingRecordingInsertTimelineSeconds.Value, 0, SequenceDuration);
                var recordedSegment = CurrentFieldsAsSegment(inheritSelectedEdits: false);
                recordedSegment.Id = Guid.NewGuid();
                TimelineEdits.InsertAt(_segments, _playbackRate, insertionTimeline, recordedSegment);
                _pendingRecordingInsertTimelineSeconds = null;
                _recordingPlacementRequired = false;
                _recordingStartPlacementRequired = false;
                _retainedRecordingCommittedToSequence = true;
                _plannedRecordingPlacementBoundary = null;
                var insertedIndex = _segments.FindIndex(segment => segment.Id == recordedSegment.Id);
                SelectSegment(recordedSegment.Id, seek: false, commitCurrent: false);
                _playhead = insertedIndex >= 0
                    ? TimelineEdits.SegmentTimelineStart(_segments, insertedIndex, _playbackRate)
                    : Math.Clamp(insertionTimeline, 0, SequenceDuration);
                _warning = $"Inserted recording at {Time(insertionTimeline)} without replacing the existing video sequence.";
            }
            else if (IsVideo)
            {
                _recordingPlacementRequired = true;
                throw new InvalidOperationException(LT("A video recording must have an explicit sequence position before it can be embedded."));
            }
            else
            {
                ReplaceOrAddCurrentSegment();
                _retainedRecordingCommittedToSequence = true;
            }
            RefreshPreviewSource();
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(CompleteMediaRecordingTransfer)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs media recording failed for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="message">Message value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public Task MediaRecordingFailed(string message)
    {
        try
        {
            try
            {
                Logger.LogError(
                    "Browser MediaRecorder failed for {MediaKind} session {SessionId}: {RecordingError}",
                    IsVideo ? "video" : "audio",
                    SessionId,
                    message);
                _recording = false;
                _recordingStopping = false;
                _embedding = false;
                _recordingBuffer = null;
                _recordingId = null;
                _recordingExpectedChunks = 0;
                _recordingNextChunk = 0;
                _recordingExpectedBytes = 0;
                _error = message;
                return InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Handling a Media Studio recording failure callback failed for session {SessionId}.", SessionId);
                throw;
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(MediaRecordingFailed)} failed.");
            throw;
        }
    }

    /// <summary>Moves browser-visible preview assets into a deferred-release set until the DOM replacement render has completed.</summary>
    private void RetirePreviewAssets()
    {
        try
        {
            foreach (var id in _previewAssetIds) _retiredPreviewAssetIds.Add(id);
            _previewAssetIds.Clear();
            _previewAssetId = Guid.Empty;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Could not retire Media Studio preview assets.");
            throw;
        }
    }

    /// <summary>Releases preview assets that were retired before the render that has just completed.</summary>
    private void ReleaseRetiredPreviewAssets()
    {
        try
        {
            foreach (var id in _retiredPreviewAssetIds) MediaAssets.Remove(id);
            _retiredPreviewAssetIds.Clear();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Could not release retired Media Studio preview assets.");
            throw;
        }
    }

    /// <summary>Releases every preview asset during final component teardown.</summary>
    private void ReleaseAllPreviewAssets()
    {
        try
        {
            RetirePreviewAssets();
            ReleaseRetiredPreviewAssets();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Could not release all Media Studio preview assets.");
            throw;
        }
    }

    /// <summary>
    /// Refreshes preview source for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void RefreshPreviewSource()
    {
        try
        {
            // Keep the previous URL alive until the render that replaces the media src has completed.
            RetirePreviewAssets();
            if (string.IsNullOrWhiteSpace(_dataUrl))
            {
                _previewSource = string.Empty;
                return;
            }
            _previewAssetId = Guid.NewGuid();
            _previewAssetIds.Add(_previewAssetId);
            _previewSource = MediaAssets.Register(_previewAssetId, _dataUrl, _mimeType);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(RefreshPreviewSource)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs play selection for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task PlaySelection()
    {
        try
        {
            try
            {
                var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
                var start = IsVideo ? VideoSelectionStart : _trimStart;
                var end = IsVideo && !_videoSelectionIsPoint ? VideoSelectionEnd : _trimEnd;
                await module.InvokeAsync<bool>("playMediaRange", "media-studio-preview", start, end, _volume, _playbackRate, _muted, _loop).ConfigureAwait(true);
                _error = null;
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException) { }
            catch (JSException ex) when (IsInterruptedPlaybackException(ex)) { }
            catch (JSException ex)
            {
                _error = $"Video playback could not start: {ex.Message}";
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(PlaySelection)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs pause preview for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task PausePreview()
    {
        try
        {
            try
            {
                var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
                await module.InvokeAsync<bool>("pauseMedia", "media-studio-preview").ConfigureAwait(true);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException or JSException) { }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(PausePreview)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs seek start for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SeekStart()
    {
        try
        {
            var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
            await module.InvokeVoidAsync("seekMedia", "media-studio-preview", _trimStart).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(SeekStart)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs trim range changed for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void TrimRangeChanged(RangeSelectorValueChangedEventArgs args)
    {
        try
        {
            if (!double.IsFinite(_duration) || _duration <= 0) return;
            var values = new List<double>(2);
            foreach (var value in args.CurrentRange)
            {
                if (!TryRangeValue(value, out var number)) return;
                values.Add(number);
            }
            if (values.Count < 2) return;
            ApplyTrimRange(Math.Min(values[0], values[1]), Math.Max(values[0], values[1]));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(TrimRangeChanged)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Attempts to range value for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="number">Number value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool TryRangeValue(object? value, out double number)
    {
        try
        {
            try
            {
                number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (double.IsFinite(number)) return true;
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException) { }

            var text = Convert.ToString(value, CultureInfo.CurrentCulture);
            return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out number) && double.IsFinite(number);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(TryRangeValue)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes trim range for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="start">Start value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="end">End value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="duration">Duration value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The double start double end produced by the operation.</returns>
    private (double Start, double End) NormalizeTrimRange(double start, double end, double duration)
    {
        var safeDuration = double.IsFinite(duration) ? Math.Max(0, duration) : 0;
        if (safeDuration <= 0) return (0, 0);

        var minimumSpan = Math.Min(.01, safeDuration);
        var maximumStart = Math.Max(0, safeDuration - minimumSpan);
        var safeStart = double.IsFinite(start) ? Math.Clamp(start, 0, maximumStart) : 0;
        var requestedEnd = double.IsFinite(end) ? end : safeDuration;
        var minimumEnd = Math.Min(safeDuration, safeStart + minimumSpan);
        var safeEnd = Math.Clamp(requestedEnd, minimumEnd, safeDuration);
        return (safeStart, safeEnd);
    }

    /// <summary>
    /// Performs segment has explicit temporal selection for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="segment">Segment value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool SegmentHasExplicitTemporalSelection(PublicationMediaSegment segment)
    {
        try
        {
            if (!segment.HasTemporalSelection) return false;
            if (segment.TemporalSelectionIsPoint) return true;
            return Math.Abs(segment.TemporalSelectionStartSeconds - segment.TrimStartSeconds) > .02
                || Math.Abs(segment.TemporalSelectionEndSeconds - segment.EffectiveTrimEndSeconds) > .02;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(SegmentHasExplicitTemporalSelection)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs reconcile video duration for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="durationSeconds">Duration seconds value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="preserveCurrentSelection">Value indicating whether preserve current selection should apply to this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool ReconcileVideoDuration(double durationSeconds, bool preserveCurrentSelection)
    {
        try
        {
            if (!IsVideo || SelectedSegment is null || !double.IsFinite(durationSeconds) || durationSeconds <= .01) return false;
            var previousDuration = Math.Max(.01, _duration);
            var tolerance = Math.Max(.02, durationSeconds / 10_000);
            if (Math.Abs(durationSeconds - previousDuration) <= tolerance) return false;

            var previousTrimStart = _trimStart;
            var previousTrimEnd = _trimEnd;
            var previousSelectionStart = VideoSelectionStart;
            var previousSelectionEnd = VideoSelectionEnd;
            var previousSelectionPoint = _videoSelectionIsPoint;
            var selectionWasWholeTrim = !previousSelectionPoint
                && Math.Abs(previousSelectionStart - previousTrimStart) <= .02
                && Math.Abs(previousSelectionEnd - previousTrimEnd) <= .02;
            var placeholderDuration = previousDuration <= .05 || SelectedSegment.DurationSeconds <= .05;
            var trimReachedOldEnd = Math.Abs(previousTrimEnd - previousDuration) <= .02;

            _duration = durationSeconds;
            var requestedTrimEnd = placeholderDuration || trimReachedOldEnd
                ? durationSeconds
                : Math.Min(previousTrimEnd, durationSeconds);
            var trim = NormalizeTrimRange(previousTrimStart, requestedTrimEnd, durationSeconds);
            _trimStart = trim.Start;
            _trimEnd = trim.End;

            var preserve = preserveCurrentSelection || _videoSelectionUserCommitted;
            if (!preserve && (selectionWasWholeTrim || placeholderDuration))
                SetVideoSelectionRange(_trimStart, _trimEnd, pointSelection: false);
            else
                SetVideoSelectionRange(previousSelectionStart, previousSelectionEnd, previousSelectionPoint);
            return true;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ReconcileVideoDuration)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Determines whether interrupted playback exception for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="exception">Exception value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsInterruptedPlaybackException(JSException exception)
    {
        try
        {
            var message = exception.Message ?? string.Empty;
            return message.Contains("play() request was interrupted", StringComparison.OrdinalIgnoreCase)
                || message.Contains("interrupted by a call to pause", StringComparison.OrdinalIgnoreCase)
                || message.Contains("AbortError", StringComparison.OrdinalIgnoreCase);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(IsInterruptedPlaybackException)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs queue video effects refresh for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void QueueVideoEffectsRefresh()
    {
        try
        {
            if (!IsVideo || _disposed) return;
            TaskRunner.Run(
                nameof(MediaStudio),
                nameof(QueueVideoEffectsRefresh),
                async _ => await InvokeAsync(async () =>
                {
                    try
                    {
                        StateHasChanged();
                        await Task.Delay(1).ConfigureAwait(true);
                        if (HasSource) await SyncVideoEffectsAsync().ConfigureAwait(true);
                    }
                    catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException or JSException)
                    {
                        Logger.LogDebug(ex, "Video effect refresh ended because the browser/circuit was no longer available.");
                    }
                }).ConfigureAwait(true));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(QueueVideoEffectsRefresh)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Sets video selection range for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="start">Start value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="end">End value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="pointSelection">Value indicating whether point selection should apply to this operation.</param>
    private void SetVideoSelectionRange(double start, double end, bool pointSelection)
    {
        try
        {
            if (!IsVideo) return;
            var minimum = Math.Max(0, Math.Min(_duration, _trimStart));
            var maximum = Math.Max(minimum, Math.Min(Math.Max(.01, _duration), _trimEnd));
            if (pointSelection || maximum - minimum < .01)
            {
                _videoSelectionIsPoint = true;
                _videoSelectionPointSeconds = Math.Clamp(double.IsFinite(start) ? start : minimum, minimum, maximum);
                _videoSelectionStartSeconds = _videoSelectionPointSeconds;
                _videoSelectionEndSeconds = _videoSelectionPointSeconds;
                return;
            }

            var minimumSpan = Math.Min(.01, maximum - minimum);
            var maximumStart = Math.Max(minimum, maximum - minimumSpan);
            var safeStart = Math.Clamp(double.IsFinite(start) ? start : minimum, minimum, maximumStart);
            var minimumEnd = Math.Min(maximum, safeStart + minimumSpan);
            var safeEnd = Math.Clamp(double.IsFinite(end) ? end : maximum, minimumEnd, maximum);
            _videoSelectionIsPoint = false;
            _videoSelectionStartSeconds = safeStart;
            _videoSelectionEndSeconds = safeEnd;
            _videoSelectionPointSeconds = safeStart;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(SetVideoSelectionRange)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs seek video selection start for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SeekVideoSelectionStartAsync()
    {
        try
        {
            if (!IsVideo || !HasSource) return;
            _playhead = SourcePositionToTimeline(VideoSelectionStart);
            try
            {
                var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
                await module.InvokeVoidAsync("seekMedia", "media-studio-preview", VideoSelectionStart).ConfigureAwait(true);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException or JSException) { }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(SeekVideoSelectionStartAsync)} failed.");
            throw;
        }
    }

}
