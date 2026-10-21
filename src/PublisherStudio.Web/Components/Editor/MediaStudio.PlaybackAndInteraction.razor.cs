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
    /// Performs change video selection mode for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ChangeVideoSelectionMode(ChangeEventArgs args)
    {
        try
        {
            if (!IsVideo) return;
            var point = string.Equals(Text(args), "Point", StringComparison.OrdinalIgnoreCase);
            if (point)
            {
                SetVideoSelectionRange(VideoSelectionStart, VideoSelectionStart, pointSelection: true);
            }
            else
            {
                var start = Math.Clamp(_videoSelectionPointSeconds, _trimStart, _trimEnd);
                var end = Math.Min(_trimEnd, start + Math.Max(.01, (_trimEnd - _trimStart) / 4));
                if (end - start < .01)
                {
                    end = _trimEnd;
                    start = Math.Max(_trimStart, end - .01);
                }
                SetVideoSelectionRange(start, end, pointSelection: false);
            }
            _videoSelectionUserCommitted = true;
            CommitSelectedSegment();
            if (_autoCreateSelectionLayers && !_videoSelectionIsPoint) CreateOrReuseSelectionLayer(forceNew: false, announce: false);
            await SeekVideoSelectionStartAsync().ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeVideoSelectionMode)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change video selection start for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ChangeVideoSelectionStart(ChangeEventArgs args)
    {
        try
        {
            if (!TryDouble(args, out var value)) return;
            SetVideoSelectionRange(value, _videoSelectionIsPoint ? value : VideoSelectionEnd, _videoSelectionIsPoint);
            _videoSelectionUserCommitted = true;
            CommitSelectedSegment();
            if (_autoCreateSelectionLayers && !_videoSelectionIsPoint) CreateOrReuseSelectionLayer(forceNew: false, announce: false);
            await SeekVideoSelectionStartAsync().ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeVideoSelectionStart)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change video selection end for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ChangeVideoSelectionEnd(ChangeEventArgs args)
    {
        try
        {
            if (!TryDouble(args, out var value) || _videoSelectionIsPoint) return;
            SetVideoSelectionRange(VideoSelectionStart, value, pointSelection: false);
            _videoSelectionUserCommitted = true;
            CommitSelectedSegment();
            if (_autoCreateSelectionLayers) CreateOrReuseSelectionLayer(forceNew: false, announce: false);
            await SeekVideoSelectionStartAsync().ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeVideoSelectionEnd)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Sets video fit mode for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="mode">Mode value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SetVideoFitModeAsync(PublicationVideoFitMode mode)
    {
        try
        {
            if (!IsVideo) return;
            _videoFitMode = mode;
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
            try
            {
                var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
                await module.InvokeVoidAsync("refreshMediaStudioOverlay", "media-studio-preview").ConfigureAwait(true);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException or JSException) { }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(SetVideoFitModeAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs video fit contain for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task VideoFitContain() {
        try
        {
            return SetVideoFitModeAsync(PublicationVideoFitMode.Contain);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(VideoFitContain)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs video fit cover for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task VideoFitCover() {
        try
        {
            return SetVideoFitModeAsync(PublicationVideoFitMode.Cover);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(VideoFitCover)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs video fit stretch for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task VideoFitStretch() {
        try
        {
            return SetVideoFitModeAsync(PublicationVideoFitMode.Stretch);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(VideoFitStretch)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change video fit mode for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ChangeVideoFitMode(ChangeEventArgs args)
    {
        try
        {
            if (Enum.TryParse<PublicationVideoFitMode>(Text(args), out var mode)) await SetVideoFitModeAsync(mode).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeVideoFitMode)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs video fit mode text for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="mode">Mode value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="label">Label value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string VideoFitModeText(PublicationVideoFitMode mode, string label) {
        try
        {
     var localized = LT(label); return _videoFitMode == mode ? $"✓ {localized}" : localized; 
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(VideoFitModeText)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs video fit mode name for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="mode">Mode value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string VideoFitModeName(PublicationVideoFitMode mode) {
        try
        {
            return mode switch
    {
        PublicationVideoFitMode.Cover => "Fill canvas (crop)",
        PublicationVideoFitMode.Stretch => "Stretch to canvas",
        _ => "Fit whole video"
    };
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(VideoFitModeName)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Applies trim range for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="start">Start value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="end">End value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="commit">Value indicating whether commit should apply to this operation.</param>
    private void ApplyTrimRange(double start, double end, bool commit = true)
    {
        try
        {
            if (commit && _pendingInsertSegment is not null) ClearPendingInsert();
            var trim = NormalizeTrimRange(start, end, _duration);
            _trimStart = trim.Start;
            _trimEnd = trim.End;
            if (IsVideo) SetVideoSelectionRange(VideoSelectionStart, VideoSelectionEnd, _videoSelectionIsPoint);
            ClampFades();
            if (commit) CommitSelectedSegment();
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ApplyTrimRange)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change name for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeName(ChangeEventArgs args) {
        try
        {
     _name = string.IsNullOrWhiteSpace(Text(args)) ? (IsVideo ? "Video" : "Audio") : Text(args); CommitSelectedSegment(); 
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeName)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change trim start for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeTrimStart(ChangeEventArgs args) {
        try
        {
     if (TryDouble(args, out var value)) ApplyTrimRange(value, _trimEnd); 
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeTrimStart)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change trim end for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeTrimEnd(ChangeEventArgs args) {
        try
        {
     if (TryDouble(args, out var value)) ApplyTrimRange(_trimStart, value); 
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeTrimEnd)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change playback rate for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangePlaybackRate(ChangeEventArgs args) {
        try
        {
     if (TryDouble(args, out var value)) { _playbackRate = Math.Clamp(value, .25, 2); ClampFades(); } 
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangePlaybackRate)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change volume for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeVolume(ChangeEventArgs args) {
        try
        {
     if (TryDouble(args, out var value)) _volume = Math.Clamp(value, 0, 1); 
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeVolume)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change fade in for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeFadeIn(ChangeEventArgs args) {
        try
        {
     if (TryDouble(args, out var value)) _fadeIn = Math.Clamp(value, 0, MaxFade); 
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeFadeIn)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change fade out for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeFadeOut(ChangeEventArgs args) {
        try
        {
     if (TryDouble(args, out var value)) _fadeOut = Math.Clamp(value, 0, MaxFade); 
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeFadeOut)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle muted for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ToggleMuted(ChangeEventArgs args) {
        try
        {
            _muted = Bool(args);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ToggleMuted)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle loop for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ToggleLoop(ChangeEventArgs args) {
        try
        {
            _loop = Bool(args);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ToggleLoop)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle auto play for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ToggleAutoPlay(ChangeEventArgs args) {
        try
        {
            _autoPlay = Bool(args);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ToggleAutoPlay)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change playback trigger for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangePlaybackTrigger(ChangeEventArgs args) {
        try
        {
     if (Enum.TryParse<PublicationMediaPlaybackTrigger>(Text(args), out var value)) _playbackTrigger = value; 
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangePlaybackTrigger)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs clamp fades for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void ClampFades() {
        try
        {
     _fadeIn = Math.Min(_fadeIn, MaxFade); _fadeOut = Math.Min(_fadeOut, MaxFade); 
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ClampFades)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs send selected media to converter for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SendSelectedMediaToConverter()
    {
        try
        {
            if (SelectedSegment is not { } segment) return;
            try
            {
                CommitSelectedSegment();
                segment = SelectedSegment!;
                _ = MediaAssets.GetOrRegister(segment);
                if (!MediaAssets.TryGet(segment.Id, out var bytes, out var storedMime, out _))
                    throw new InvalidOperationException("The selected clip is not retained as local media and cannot be handed to the converter.");

                var selectedVideoRange = IsVideo && _videoSelectionUserCommitted;
                var start = selectedVideoRange ? VideoSelectionStart : segment.TrimStartSeconds;
                var end = selectedVideoRange && !_videoSelectionIsPoint
                    ? VideoSelectionEnd
                    : segment.EffectiveTrimEndSeconds;
                var duration = Math.Max(.001, end - start);
                var studioName = IsVideo ? "Video Studio" : "Audio Studio";
                var request = new MediaConversionInsertRequest(
                    string.IsNullOrWhiteSpace(segment.Name) ? (IsVideo ? "video-clip" : "audio-clip") : segment.Name,
                    MediaData.NormalizeMimeType(segment.MimeType, storedMime),
                    bytes,
                    selectedVideoRange ? "Video Studio selected range" : $"{studioName} selected clip")
                {
                    SuggestedPresetId = IsVideo ? "webm-vp9" : "audio-webm-opus",
                    SuggestedOptions = IsVideo
                        ? new MediaConversionOptions
                        {
                            Target = MediaConversionTarget.PublisherStudioWeb,
                            StartSeconds = Math.Max(0, start),
                            DurationSeconds = duration,
                            ScaleMode = MediaConversionScaleMode.Preserve,
                            PreserveAspectRatio = true,
                            PixelFormat = "yuv420p",
                            FastStart = true
                        }
                        : new MediaConversionOptions
                        {
                            Target = MediaConversionTarget.PublisherStudioWeb,
                            StartSeconds = Math.Max(0, start),
                            DurationSeconds = duration,
                            DisableVideo = true
                        }
                };
                await SendToMediaConverter.InvokeAsync(request).ConfigureAwait(true);
            }
            catch (Exception exception)
            {
                _error = exception.Message;
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(SendSelectedMediaToConverter)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs download recording for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task DownloadRecording()
    {
        try
        {
            if (!CanDownloadCurrentMedia) return;
            try
            {
                var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
                var fileName = HasRetainedRecording
                    ? _retainedRecordingFileName
                    : SelectedSegment?.Name ?? _name;
                await module.InvokeAsync<bool>("downloadCurrentMedia", "media-studio-preview", fileName).ConfigureAwait(true);
                Logger.LogInformation(
                    "Downloaded Media Studio source {FileName} for session {SessionId}; retained browser recording: {RetainedRecording}.",
                    fileName,
                    SessionId,
                    HasRetainedRecording);
                _warning = $"Downloaded {fileName}.";
                OperationalNotifications.Success(_warning, LT("Media downloaded"), nameof(MediaStudio));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Downloading the current Media Studio source failed for session {SessionId}.", SessionId);
                _error = $"The selected media could not be downloaded: {ex.Message}";
                OperationalNotifications.Error(_error, LT("Media download failed"), nameof(MediaStudio));
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(DownloadRecording)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs save for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task Save()
    {
        try
        {
            if (_saving || _embedding || !HasSource || _recording) return;
            if (_recordingPlacementRequired)
            {
                _warning = LT("Choose where the completed recording belongs before applying the Video Studio changes.");
                OperationalNotifications.Warning(_warning, LT("Recording position required"), nameof(MediaStudio));
                return;
            }
            _saving = true;
            _error = null;
            var applied = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
            try
            {
                var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
                try { await module.InvokeVoidAsync("pauseMedia", "media-studio-preview").ConfigureAwait(true); }
                catch (JSException) { }
                catch (TaskCanceledException) { }

                if (HasRetainedRecording && string.IsNullOrWhiteSpace(_dataUrl))
                {
                    _embedding = true;
                    _embeddingProgress = 0;
                    _warning = $"Embedding {FormatBytes(_retainedRecordingSizeBytes)} in chunks. The original remains downloadable.";
                    await InvokeAsync(StateHasChanged).ConfigureAwait(true);
                    try
                    {
                        await module.InvokeAsync<bool>("embedRetainedRecording", "media-studio-preview").ConfigureAwait(true);
                    }
                    catch (Exception embedError)
                    {
                        _embedding = false;
                        _recordingBuffer = null;
                        _recordingId = null;
                        try
                        {
                            await module.InvokeVoidAsync("downloadRetainedRecording", "media-studio-preview", _retainedRecordingFileName).ConfigureAwait(true);
                            _error = $"Embedding failed, so the completed recording was downloaded instead and remains available here. {embedError.Message}";
                        }
                        catch (Exception downloadError)
                        {
                            _error = $"Embedding failed, but the completed recording remains available through Download recording. {embedError.Message} Download error: {downloadError.Message}";
                        }
                        return;
                    }
                }

                if (string.IsNullOrWhiteSpace(_dataUrl) && _segments.Count == 0)
                    throw new InvalidDataException("The completed recording has not been embedded yet.");

                CommitProjectTrack();
                var normalizedSegments = TimelineEdits.Normalize(_segments, IsVideo);
                if (normalizedSegments.Count == 0)
                    throw new InvalidDataException("The media sequence does not contain an insertable section.");
                var firstSegment = normalizedSegments.FirstOrDefault(IsPlayableSegment)
                    ?? normalizedSegments.FirstOrDefault(segment => !segment.IsGap)
                    ?? normalizedSegments[0];

                await Saved.InvokeAsync(new MediaEditorResult
                {
                    Kind = Kind,
                    Name = firstSegment.Name,
                    DataUrl = firstSegment.DataUrl,
                    MimeType = firstSegment.MimeType,
                    PosterDataUrl = firstSegment.PosterDataUrl,
                    PreviewAssetId = _previewAssetId,
                    DurationSeconds = firstSegment.DurationSeconds,
                    TrimStartSeconds = firstSegment.TrimStartSeconds,
                    TrimEndSeconds = firstSegment.EffectiveTrimEndSeconds,
                    Volume = _volume,
                    PlaybackRate = _playbackRate,
                    FadeInSeconds = _fadeIn,
                    FadeOutSeconds = _fadeOut,
                    Muted = _muted,
                    Loop = _loop,
                    AutoPlay = _autoPlay,
                    PlaybackTrigger = _playbackTrigger,
                    VideoFitMode = _videoFitMode,
                    WaveformSamples = [.. firstSegment.WaveformSamples],
                    Segments = normalizedSegments,
                    VideoProject = IsVideo && _videoProject is not null
                        ? TimelineEdits.CloneVideoProject(_videoProject)
                        : null,
                    FrameClipPolygon = CloneFramePoints(normalizedSegments.FirstOrDefault(segment => !segment.IsGap)?.VideoLayers.FirstOrDefault()?.Region?.Points)
                }).ConfigureAwait(true);
                applied = true;
                try
                {
                    await module.InvokeVoidAsync("disposeMediaStudio", "media-studio-preview").ConfigureAwait(true);
                }
                catch (JSException) { }
                catch (TaskCanceledException) { }
                finally
                {
                    _initializedSession = Guid.Empty;
                    // Do not remove the current preview URL until the parent render has removed/replaced it.
                    RetirePreviewAssets();
                }
            }
            catch (JSDisconnectedException)
            {
                _error = "The browser connection closed before the media could be inserted. A completed browser recording remains downloadable while this studio is open.";
            }
            catch (TaskCanceledException)
            {
                _error = "The media insert operation was cancelled. A completed browser recording remains downloadable while this studio is open.";
            }
            catch (Exception ex)
            {
                _error = $"The media could not be inserted: {ex.Message}";
            }
            finally
            {
                _embedding = false;
                _saving = false;
                if (!applied)
                {
                    try { await InvokeAsync(StateHasChanged).ConfigureAwait(true); }
                    catch (JSDisconnectedException) { }
                    catch (TaskCanceledException) { }
                    catch (ObjectDisposedException) { }
                }
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(Save)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Determines whether cel for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task Cancel()
    {
        try
        {
            _recording = false;
            _saving = false;
            _embedding = false;
            _recordingBuffer = null;
            _recordingId = null;
            if (_module is not null)
            {
                try
                {
                    await _module.InvokeVoidAsync("disposeMediaStudio", "media-studio-preview").ConfigureAwait(true);
                    _initializedSession = Guid.Empty;
                }
                catch (JSDisconnectedException) { }
                catch (TaskCanceledException) { }
            }
            await Cancelled.InvokeAsync().ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(Cancel)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Creates tone data URL for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="durationSeconds">Duration seconds value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="frequency">Frequency value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="volume">Volume value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string CreateToneDataUrl(double durationSeconds, double frequency, double volume)
    {
        try
        {
            var sampleRate = RuntimePolicy.AudioSampleRate;
            var frames = Math.Max(1, (int)Math.Floor(sampleRate * Math.Max(.1, durationSeconds)));
            using var stream = new MemoryStream(44 + frames * 2);
            using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + frames * 2);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(sampleRate);
            writer.Write(sampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(frames * 2);
            var attack = (int)(sampleRate * .025);
            var release = (int)(sampleRate * .12);
            for (var frame = 0; frame < frames; frame++)
            {
                var attackGain = Math.Min(1, frame / (double)Math.Max(1, attack));
                var releaseGain = Math.Min(1, (frames - frame) / (double)Math.Max(1, release));
                var envelope = Math.Min(attackGain, releaseGain);
                var sample = Math.Sin(2 * Math.PI * frequency * frame / sampleRate) * Math.Clamp(volume, 0, 1) * envelope;
                writer.Write((short)Math.Round(sample * short.MaxValue));
            }
            writer.Flush();
            return $"data:audio/wav;base64,{Convert.ToBase64String(stream.ToArray())}";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(CreateToneDataUrl)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Resolves imported MIME type for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="fileName">File name value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="declaredMimeType">Declared mime type value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="video">Value indicating whether video should apply to this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ResolveImportedMimeType(string fileName, string? declaredMimeType, bool video)
    {
        try
        {
            var fallback = video ? "video/mp4" : "audio/mpeg";
            var declared = NormalizeMimeType(declaredMimeType, string.Empty);
            if (declared.StartsWith(video ? "video/" : "audio/", StringComparison.OrdinalIgnoreCase))
                return declared;

            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".mp4" => video ? "video/mp4" : "audio/mp4",
                ".m4v" => "video/x-m4v",
                ".webm" => video ? "video/webm" : "audio/webm",
                ".ogv" => "video/ogg",
                ".ogg" or ".oga" => video ? "video/ogg" : "audio/ogg",
                ".mov" => "video/quicktime",
                ".mp3" => "audio/mpeg",
                ".m4a" or ".aac" => "audio/mp4",
                ".wav" => "audio/wav",
                ".flac" => "audio/flac",
                _ => fallback
            };
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ResolveImportedMimeType)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes MIME type for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="mimeType">Mime type value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="fallback">Fallback value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeMimeType(string? mimeType, string fallback)
    {
        try
        {
            var value = mimeType?.Trim() ?? string.Empty;
            var separator = value.IndexOf(';');
            if (separator >= 0) value = value[..separator].Trim();
            return value.Contains('/') ? value.ToLowerInvariant() : fallback;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(NormalizeMimeType)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes media data URL for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="dataUrl">Data url value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="fallbackMimeType">Fallback mime type value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeMediaDataUrl(string? dataUrl, string fallbackMimeType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dataUrl) || !dataUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                return dataUrl ?? string.Empty;
            var marker = dataUrl.LastIndexOf(";base64,", StringComparison.OrdinalIgnoreCase);
            if (marker < 0) return dataUrl;
            var header = dataUrl.Substring(5, marker - 5);
            var mimeType = NormalizeMimeType(header, fallbackMimeType);
            return $"data:{mimeType};base64,{dataUrl[(marker + 8)..]}";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(NormalizeMediaDataUrl)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs recording name for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="mimeType">Mime type value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="isVideo">Value indicating whether is video should apply to this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RecordingName(string mimeType, bool isVideo)
    {
        try
        {
            var extension = mimeType.ToLowerInvariant() switch
            {
                var value when value.Contains("mp4", StringComparison.Ordinal) => "mp4",
                var value when value.Contains("ogg", StringComparison.Ordinal) => "ogg",
                var value when value.Contains("wav", StringComparison.Ordinal) => "wav",
                _ => "webm"
            };
            return $"Recorded {(isVideo ? "Video" : "Audio")}.{extension}";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(RecordingName)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs playback trigger name for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string PlaybackTriggerName(PublicationMediaPlaybackTrigger value) {
        try
        {
            return LT(value switch
    {
        PublicationMediaPlaybackTrigger.OnPageEnter => "When the page opens",
        PublicationMediaPlaybackTrigger.OnClick => "When the media object is clicked",
        _ => "With the object animation"
    });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(PlaybackTriggerName)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs wave height for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string WaveHeight(double value) {
        try
        {
            return $"{Math.Clamp(value, .05, 1) * 100:0.#}%";
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(WaveHeight)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs time for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Time(double value) {
        try
        {
            return TimeSpan.FromSeconds(Math.Max(0, value)).ToString(value >= 3600 ? @"h\:mm\:ss\.f" : @"m\:ss\.f", CultureInfo.InvariantCulture);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(Time)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs inv for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Inv(double value) {
        try
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(Inv)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs text for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Text(ChangeEventArgs args) {
        try
        {
            return args.Value?.ToString() ?? string.Empty;
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(Text)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs bool for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool Bool(ChangeEventArgs args) {
        try
        {
            return args.Value is bool value ? value : bool.TryParse(Text(args), out var parsed) && parsed;
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(Bool)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Attempts to double for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="value">Value value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool TryDouble(ChangeEventArgs args, out double value) {
        try
        {
            return double.TryParse(Text(args), NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(TryDouble)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs format bytes for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="bytes">Bytes value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string FormatBytes(long bytes)
    {
        try
        {
            string[] units = ["B", "KB", "MB", "GB", "TB"];
            var value = Math.Max(0, bytes);
            var unit = 0;
            var display = (double)value;
            while (display >= 1024 && unit < units.Length - 1)
            {
                display /= 1024;
                unit++;
            }
            return $"{display:0.##} {units[unit]}";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(FormatBytes)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Represents browser media recording state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
    /// </summary>
    public sealed class BrowserMediaRecordingState
    {
        /// <summary>
        /// Gets or sets a value indicating whether recording applies to the browser media recording state.
        /// </summary>
        /// <value><see langword="true"/> while capture is active; otherwise <see langword="false"/>.</value>
        public bool IsRecording { get; set; }
        /// <summary>Gets or sets whether Stop was requested and browser Blob finalization is still running.</summary>
        /// <value><see langword="true"/> while the completed Blob is being finalized; otherwise <see langword="false"/>.</value>
        public bool IsFinalizing { get; set; }
        /// <summary>
        /// Gets or sets the retained recording value that forms part of the browser media recording state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The retained recording value exposed by <see cref="BrowserMediaRecordingState"/>.</value>
        public RetainedMediaRecordingInfo? RetainedRecording { get; set; }
        /// <summary>
        /// Gets or sets the finalization error value that forms part of the browser media recording state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The finalization error value exposed by <see cref="BrowserMediaRecordingState"/>.</value>
        public string FinalizationError { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a retained media recording info helper type nested within <see cref="MediaStudio"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    public sealed class RetainedMediaRecordingInfo
    {
        /// <summary>
        /// Gets or sets the object URL that identifies the network or application endpoint associated with this retained media recording info state.
        /// </summary>
        /// <value>The object URL value exposed by <see cref="RetainedMediaRecordingInfo"/>.</value>
        public string ObjectUrl { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the MIME type value that forms part of the retained media recording info state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The MIME type value exposed by <see cref="RetainedMediaRecordingInfo"/>.</value>
        public string MimeType { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the file name used by this retained media recording info instance to locate the associated file-system resource.
        /// </summary>
        /// <value>The file name value exposed by <see cref="RetainedMediaRecordingInfo"/>.</value>
        public string FileName { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the size bytes value that forms part of the retained media recording info state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The size bytes value exposed by <see cref="RetainedMediaRecordingInfo"/>.</value>
        public long SizeBytes { get; set; }
        /// <summary>
        /// Gets or sets the duration seconds value that forms part of the retained media recording info state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The duration seconds value exposed by <see cref="RetainedMediaRecordingInfo"/>.</value>
        public double DurationSeconds { get; set; }
        /// <summary>
        /// Gets or sets the waveform samples collection maintained or exposed by this retained media recording info instance for downstream processing.
        /// </summary>
        /// <value>The waveform samples value exposed by <see cref="RetainedMediaRecordingInfo"/>.</value>
        public List<double> WaveformSamples { get; set; } = [];
        /// <summary>
        /// Gets or sets the poster data URL that identifies the network or application endpoint associated with this retained media recording info state.
        /// </summary>
        /// <value>The poster data URL value exposed by <see cref="RetainedMediaRecordingInfo"/>.</value>
        public string PosterDataUrl { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets a value indicating whether this retained browser recording has already been committed as a canonical media-sequence clip.
        /// </summary>
        /// <value><see langword="true"/> when starting another capture may safely release the retained browser Blob because the sequence already owns the media.</value>
        public bool CommittedToSequence { get; set; }
        /// <summary>
        /// Gets or sets the metadata warning value that forms part of the retained media recording info state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The metadata warning value exposed by <see cref="RetainedMediaRecordingInfo"/>.</value>
        public string MetadataWarning { get; set; } = string.Empty;
    }

    /// <summary>Represents the compact result returned by a multi-clip selected-range browser export.</summary>
    private sealed class TimelineSelectionExportResult
    {
        /// <summary>Gets or sets the number of source clips included in the export.</summary>
        /// <value>The clip count value exposed by <see cref="MediaStudio"/>.</value>
        public int ClipCount { get; set; }
        /// <summary>Gets or sets the number of files downloaded by the browser.</summary>
        /// <value>The file count value exposed by <see cref="MediaStudio"/>.</value>
        public int FileCount { get; set; }
        /// <summary>Gets or sets the combined byte count of the generated files.</summary>
        /// <value>The size bytes value exposed by <see cref="MediaStudio"/>.</value>
        public long SizeBytes { get; set; }
        /// <summary>Gets or sets the combined output file name when a single file was requested.</summary>
        /// <value>The file name value exposed by <see cref="MediaStudio"/>.</value>
        public string FileName { get; set; } = string.Empty;
        /// <summary>Gets or sets whether the browser export was cancelled before files were produced.</summary>
        /// <value>The cancelled value exposed by <see cref="MediaStudio"/>.</value>
        public bool Cancelled { get; set; }
    }

    /// <summary>
    /// Represents a media recording start info helper type nested within <see cref="MediaStudio"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    private sealed class MediaRecordingStartInfo
    {
        /// <summary>
        /// Gets or sets the MIME type value that forms part of the media recording start info state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The MIME type value exposed by <see cref="MediaRecordingStartInfo"/>.</value>
        public string MimeType { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the width value that forms part of the media recording start info state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The width value exposed by <see cref="MediaRecordingStartInfo"/>.</value>
        public int Width { get; set; }
        /// <summary>
        /// Gets or sets the height value that forms part of the media recording start info state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The height value exposed by <see cref="MediaRecordingStartInfo"/>.</value>
        public int Height { get; set; }
        /// <summary>
        /// Gets or sets the frame rate value that forms part of the media recording start info state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The frame rate value exposed by <see cref="MediaRecordingStartInfo"/>.</value>
        public double FrameRate { get; set; }
        /// <summary>
        /// Gets or sets the video bits per second value that forms part of the media recording start info state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The video bits per second value exposed by <see cref="MediaRecordingStartInfo"/>.</value>
        public int VideoBitsPerSecond { get; set; }
        /// <summary>
        /// Gets or sets the audio bits per second value that forms part of the media recording start info state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The audio bits per second value exposed by <see cref="MediaRecordingStartInfo"/>.</value>
        public int AudioBitsPerSecond { get; set; }
    }

    /// <summary>
    /// Releases resources owned by <see cref="MediaStudio"/> and leaves the media studio workflow in a safely disposed state.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async ValueTask DisposeAsync()
    {
        try
        {
            _disposed = true;
            try
            {
                if (_module is not null) await _module.InvokeVoidAsync("disposeMediaStudio", "media-studio-preview").ConfigureAwait(true);
            }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
            finally
            {
                // Browser-side media teardown happens first; only then invalidate its backing URLs.
                ReleaseAllPreviewAssets();
                _self?.Dispose();
            }

            try
            {
                if (_module is not null) await _module.DisposeAsync().ConfigureAwait(true);
            }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(DisposeAsync)} failed.");
            throw;
        }
    }



    /// <summary>Computes IsVideo for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetIsVideoForRender()
    {
        try
        {
            return Kind == PublicationElementKind.Video;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.IsVideo failed.");
            throw;
        }
    }

    /// <summary>Computes HasRetainedRecording for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetHasRetainedRecordingForRender()
    {
        try
        {
            return _hasRetainedRecording && _retainedRecordingSizeBytes > 0;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.HasRetainedRecording failed.");
            throw;
        }
    }

    /// <summary>Computes CanDownloadCurrentMedia for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanDownloadCurrentMediaForRender()
    {
        try
        {
            return HasRetainedRecording || (SelectedSegment is { IsGap: false } && !string.IsNullOrWhiteSpace(_previewSource));
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.CanDownloadCurrentMedia failed.");
            throw;
        }
    }

    /// <summary>Computes DownloadMediaText for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetDownloadMediaTextForRender()
    {
        try
        {
            return HasRetainedRecording ? LT("Download recording") : LT("Download selected source");
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.DownloadMediaText failed.");
            throw;
        }
    }

}
