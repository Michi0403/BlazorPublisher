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
    /// Handles the after render async lifecycle or event notification for <see cref="MediaStudio"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="firstRender">Value indicating whether first render should apply to this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            // OnAfterRender runs only after Blazor has applied the latest DOM. Old preview URLs
            // are therefore no longer browser-visible and can be removed without a stale-src 404 race.
            ReleaseRetiredPreviewAssets();
            if (!Visible || _disposed || SessionId == Guid.Empty) return;
            var stateChanged = false;
            try
            {
                var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
                var shouldRefreshRecordingOwnership = _recordingRecoveryAttemptedSession != SessionId || _recording || _recordingStopping;
                if (shouldRefreshRecordingOwnership)
                {
                    _recordingRecoveryAttemptedSession = SessionId;
                    var stateInvocation = module.InvokeAsync<BrowserMediaRecordingState>(
                        "getMediaRecordingState",
                        "media-studio-preview",
                        _self);
                    var browserState = await stateInvocation.ConfigureAwait(true);
                    if (browserState.RetainedRecording is { SizeBytes: > 0 } retainedRecording)
                    {
                        var changed = !HasRetainedRecording
                            || _recording
                            || _recordingStopping
                            || !string.Equals(_retainedRecordingObjectUrl, retainedRecording.ObjectUrl, StringComparison.Ordinal);
                        ApplyRetainedRecordingInfo(retainedRecording);
                        RequireRecordingPlacementIfNeeded();
                        stateChanged |= changed;
                        if (changed)
                        {
                            Logger.LogInformation(
                                "Recovered retained {MediaKind} recording {FileName} ({SizeBytes} bytes) after Media Studio render/circuit attachment.",
                                IsVideo ? "video" : "audio",
                                retainedRecording.FileName,
                                retainedRecording.SizeBytes);
                        }
                    }
                    else if (browserState.IsFinalizing)
                    {
                        var changed = !_recording || !_recordingStopping;
                        _recording = true;
                        _recordingStopping = true;
                        _warning = "Browser recording is finalizing after the UI reconnected.";
                        stateChanged |= changed;
                        if (changed)
                        {
                            Logger.LogInformation(
                                "Recovered finalizing {MediaKind} recording state after Media Studio render/circuit attachment.",
                                IsVideo ? "video" : "audio");
                        }
                    }
                    else if (browserState.IsRecording)
                    {
                        var changed = !_recording || _recordingStopping;
                        _recording = true;
                        _recordingStopping = false;
                        _warning = "Browser recording is still active after the UI reconnected.";
                        stateChanged |= changed;
                        if (changed)
                        {
                            Logger.LogInformation(
                                "Recovered active {MediaKind} recording state after Media Studio render/circuit attachment.",
                                IsVideo ? "video" : "audio");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(browserState.FinalizationError))
                    {
                        _recording = false;
                        _recordingStopping = false;
                        _error = $"Recording finalization failed in the browser: {browserState.FinalizationError}";
                        stateChanged = true;
                        Logger.LogError(
                            "Browser reported a retained recording finalization error after Media Studio reconnect: {RecordingError}",
                            browserState.FinalizationError);
                    }
                }

                if (IsVideo && HasSource) await SyncVideoEffectsAsync(module).ConfigureAwait(true);
                if (stateChanged) await InvokeAsync(StateHasChanged).ConfigureAwait(true);
            }
            catch (JSDisconnectedException ex)
            {
                Logger.LogWarning(ex, "Media Studio browser connection disconnected while attaching or recovering recording state.");
            }
            catch (TaskCanceledException ex)
            {
                Logger.LogWarning(ex, "Media Studio browser attachment or recording-state recovery was canceled.");
            }
            catch (ObjectDisposedException ex)
            {
                Logger.LogDebug(ex, "Media Studio was disposed while browser attachment or recording-state recovery was in progress.");
            }
            catch (JSException ex)
            {
                Logger.LogError(ex, "Media Studio browser initialization failed.");
                _error = $"Media Studio could not initialize: {ex.Message}";
                try { await InvokeAsync(StateHasChanged).ConfigureAwait(true); }
                catch (ObjectDisposedException disposedException)
                {
                    Logger.LogDebug(disposedException, "Media Studio was disposed before its initialization error could be rendered.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected Media Studio render/recovery failure.");
                _error = $"Media Studio could not restore browser media state: {ex.Message}";
                try { await InvokeAsync(StateHasChanged).ConfigureAwait(true); }
                catch (ObjectDisposedException disposedException)
                {
                    Logger.LogDebug(disposedException, "Media Studio was disposed before its recovery error could be rendered.");
                }
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(OnAfterRenderAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Ensures media module for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>The ijs object reference produced by the operation.</returns>
    private async Task<IJSObjectReference> EnsureMediaModuleAsync()
    {
        try
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MediaStudio));
            _module ??= await JS.InvokeAsync<IJSObjectReference>("import", "./js/mediaStudioInterop.js?v=4.3.0").ConfigureAwait(true);
            _self ??= DotNetObjectReference.Create(this);
            if (SessionId != Guid.Empty)
            {
                // The sequence timeline and its two selection handles are rendered only after a source exists.
                // Rebinding is intentionally idempotent so a session that started empty becomes fully interactive
                // immediately after import, and so rerendered selected segments remain clickable/adjustable.
                await _module.InvokeVoidAsync(
                    "initializeMediaStudio",
                    "media-studio-preview",
                    _self,
                    SessionId.ToString("N"),
                    "media-studio-window",
                    IsVideo ? "media-studio-frame-stage" : string.Empty,
                    IsVideo ? "media-studio-frame-overlay-host" : string.Empty,
                    IsVideo ? "media-studio-video-time-overlay" : string.Empty,
                    "media-studio-sequence-timeline",
                    "media-studio-file-input",
                    "media-studio-insert-file-input",
                    IsVideo ? "media-studio-project-input" : string.Empty,
                    IsVideo ? "video" : "audio").ConfigureAwait(true);
                _initializedSession = SessionId;
            }
            return _module;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(EnsureMediaModuleAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Builds video effect configuration for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="outputWidth">Output width value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="outputHeight">Output height value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The object produced by the operation.</returns>
    private object BuildVideoEffectConfiguration(int outputWidth = 0, int outputHeight = 0) {
        try
        {
            return new
    {
        fitMode = _videoFitMode switch
        {
            PublicationVideoFitMode.Cover => "cover",
            PublicationVideoFitMode.Stretch => "stretch",
            _ => "contain"
        },
        forceCanvas = true,
        outputWidth,
        outputHeight,
        maximumPixels = 0,
        layers = VideoLayerPayload(SelectedSegment?.VideoLayers ?? [])
    };
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(BuildVideoEffectConfiguration)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs sync video effects for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="module">Javascript object reference dependency used by the media studio workflow to provide the corresponding application capability.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SyncVideoEffectsAsync(IJSObjectReference? module = null)
    {
        try
        {
            if (!IsVideo || !HasSource || SelectedSegment is null || !IsPlayableSegment(SelectedSegment)) return;
            module ??= await EnsureMediaModuleAsync().ConfigureAwait(true);
            await module.InvokeVoidAsync("configureVideoEffects", "media-studio-preview", "media-studio-video-effects", BuildVideoEffectConfiguration()).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(SyncVideoEffectsAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs video layer payload for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="layers">Video effect layer dependency used by the media studio workflow to provide the corresponding application capability.</param>
    /// <returns>The object produced by the operation.</returns>
    private object[] VideoLayerPayload(IEnumerable<VideoEffectLayer> layers) {
        try
        {
            return layers
        .Where(layer => layer is not null)
        .Select(layer =>
        {
            var sourceRegion = layer.Region ?? new VideoFrameRegion();
            var morphRegion = layer.MorphRegion ?? new VideoFrameRegion { Name = "Morph target" };
            IReadOnlyList<MediaFramePoint> sourcePoints = sourceRegion.Points;
            IReadOnlyList<MediaFramePoint> morphPoints = morphRegion.Points;
            if (layer.Id == _selectedVideoLayerId && _frameRegionEditing)
            {
                if (_editingMorphRegion) morphPoints = ActiveFramePoints;
                else sourcePoints = ActiveFramePoints;
            }
            return (object)new
            {
                id = layer.Id.ToString("D"),
                layer.Name,
                layer.Visible,
                kind = layer.Kind.ToString(),
                layer.Opacity,
                blendMode = layer.BlendMode.ToString(),
                layer.HasTemporalRange,
                layer.TemporalStartSeconds,
                layer.TemporalEndSeconds,
                layer.MorphEnabled,
                layer.AnimateMorph,
                layer.MorphAmount,
                layer.AnimationSpeed,
                layer.Depth,
                layer.Roundness,
                htmlExportSupport = layer.HtmlExportSupport.ToString(),
                region = new
                {
                    sourceRegion.Inverted,
                    points = sourcePoints.Select(point => new { point.X, point.Y }).ToArray()
                },
                morphRegion = new
                {
                    morphRegion.Inverted,
                    points = morphPoints.Select(point => new { point.X, point.Y }).ToArray()
                },
                filters = (layer.Filters ?? []).Select(filter => new
                {
                    id = filter.Id.ToString("D"),
                    kind = filter.Kind.ToString(),
                    filter.Enabled,
                    filter.Amount,
                    filter.SecondaryAmount,
                    filter.TertiaryAmount,
                    filter.ResidualOpacity,
                    filter.Color,
                    htmlExportSupport = filter.HtmlExportSupport.ToString()
                }).ToArray()
            };
        })
        .ToArray();
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(VideoLayerPayload)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs commit selected segment for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="refreshVideoEffects">Value indicating whether refresh video effects should apply to this operation.</param>
    private void CommitSelectedSegment(bool refreshVideoEffects = false)
    {
        try
        {
            var segment = SelectedSegment;
            if (segment is null) return;
            CommitFrameRegionToSelectedLayer();
            if (segment.IsGap)
            {
                var gapDuration = Math.Max(.01, double.IsFinite(_duration) ? _duration : segment.TimelineDurationSeconds);
                segment.Name = "Gap";
                segment.DataUrl = string.Empty;
                segment.MimeType = string.Empty;
                segment.DurationSeconds = gapDuration;
                segment.TrimStartSeconds = 0;
                segment.TrimEndSeconds = gapDuration;
                segment.TimelineDurationSeconds = gapDuration;
                segment.HasTemporalSelection = false;
                segment.TemporalSelectionCommitted = false;
                segment.VideoLayers.Clear();
                return;
            }
            segment.Name = string.IsNullOrWhiteSpace(_name) ? (IsVideo ? "Video clip" : "Audio clip") : _name.Trim();
            segment.DataUrl = NormalizeMediaDataUrl(_dataUrl, IsVideo ? "video/webm" : "audio/webm");
            segment.MimeType = NormalizeMimeType(_mimeType, IsVideo ? "video/webm" : "audio/webm");
            segment.PosterDataUrl = _posterDataUrl;
            segment.DurationSeconds = Math.Max(.01, double.IsFinite(_duration) ? _duration : 0);
            var trim = NormalizeTrimRange(_trimStart, _trimEnd, segment.DurationSeconds);
            segment.TrimStartSeconds = trim.Start;
            segment.TrimEndSeconds = trim.End;
            segment.HasTemporalSelection = IsVideo;
            segment.TemporalSelectionCommitted = IsVideo && _videoSelectionUserCommitted;
            segment.TemporalSelectionIsPoint = _videoSelectionIsPoint;
            segment.TemporalSelectionStartSeconds = VideoSelectionStart;
            segment.TemporalSelectionEndSeconds = VideoSelectionEnd;
            if (IsVideo)
                segment.VideoLayers = TimelineEdits.NormalizeVideoLayers(segment.VideoLayers, segment.TrimStartSeconds, segment.EffectiveTrimEndSeconds, segment.Name);
            segment.WaveformSamples = [.. _waveform];
            if (refreshVideoEffects) QueueVideoEffectsRefresh();
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(CommitSelectedSegment)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs select segment for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="seek">Value indicating whether seek should apply to this operation.</param>
    /// <param name="forceReload">Value indicating whether force reload should apply to this operation.</param>
    /// <param name="commitCurrent">Value indicating whether commit current should apply to this operation.</param>
    /// <param name="preserveTimelineSelection">Value indicating whether preserve timeline selection should apply to this operation.</param>
    private void SelectSegment(Guid id, bool seek = true, bool forceReload = false, bool commitCurrent = true, bool preserveTimelineSelection = false)
    {
        try
        {
            var reloadingCurrent = _selectedSegmentId == id && SelectedSegment is not null;
            if (reloadingCurrent && !forceReload)
            {
                if (!preserveTimelineSelection) SetSingleTimelineSelection(id);
                if (seek) TaskRunner.Run(nameof(MediaStudio), nameof(SelectSegment), async _ => await InvokeAsync(SeekSelectedAtPlayheadAsync).ConfigureAwait(true));
                return;
            }
            if (_pendingInsertSegment is not null) ClearPendingInsert();
            if (!reloadingCurrent && commitCurrent) CommitSelectedSegment();
            var segment = _segments.FirstOrDefault(item => item.Id == id);
            if (segment is null) return;
            _selectedSegmentId = segment.Id;
            if (!preserveTimelineSelection) SetSingleTimelineSelection(segment.Id);
            else _selectedSegmentIds.Add(segment.Id);
            _name = segment.Name;
            _dataUrl = segment.DataUrl;
            _mimeType = segment.MimeType;
            _posterDataUrl = segment.PosterDataUrl;
            _duration = Math.Max(.01, segment.DurationSeconds);
            _trimStart = segment.TrimStartSeconds;
            _trimEnd = segment.EffectiveTrimEndSeconds;
            var hasSelection = IsVideo && segment.HasTemporalSelection;
            _videoSelectionUserCommitted = hasSelection && (segment.TemporalSelectionCommitted || SegmentHasExplicitTemporalSelection(segment));
            _videoSelectionIsPoint = hasSelection && segment.TemporalSelectionIsPoint;
            _videoSelectionPointSeconds = hasSelection ? segment.TemporalSelectionStartSeconds : _trimStart;
            _videoSelectionStartSeconds = hasSelection ? segment.TemporalSelectionStartSeconds : _trimStart;
            _videoSelectionEndSeconds = hasSelection ? segment.TemporalSelectionEndSeconds : _trimEnd;
            _waveform = [.. segment.WaveformSamples];
            ApplyTrimRange(_trimStart, _trimEnd, commit: false);
            SetVideoSelectionRange(_videoSelectionStartSeconds, _videoSelectionEndSeconds, _videoSelectionIsPoint);
            _selectedCutSectionId = null;
            SelectInitialVideoLayer(segment);
            RefreshPreviewSource();
            if (seek) TaskRunner.Run(nameof(MediaStudio), nameof(SelectSegment), async _ => await InvokeAsync(SeekSelectedAtPlayheadAsync).ConfigureAwait(true));

        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(SelectSegment)} failed.");
            throw;
        }
    }

    /// <summary>Replaces the timeline multi-selection with one clip and moves the Shift anchor to that clip.</summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    private void SetSingleTimelineSelection(Guid id)
    {
        try
        {
            _selectedSegmentIds.Clear();
            _selectedSegmentIds.Add(id);
            _timelineSelectionAnchorId = id;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Could not replace the Media Studio timeline selection with segment {SegmentId}.", id);
            throw;
        }
    }

    /// <summary>Applies plain, Ctrl/Command, and Shift timeline-selection semantics without losing the primary inspector clip.</summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="shiftKey">Value indicating whether shift key should apply to this operation.</param>
    /// <param name="additive">Value indicating whether additive should apply to this operation.</param>
    private void ApplyTimelineSelection(Guid id, bool shiftKey, bool additive)
    {
        try
        {
            var targetIndex = _segments.FindIndex(segment => segment.Id == id);
            if (targetIndex < 0) return;
            if (shiftKey)
            {
                var anchorId = _timelineSelectionAnchorId ?? _selectedSegmentId ?? id;
                var anchorIndex = _segments.FindIndex(segment => segment.Id == anchorId);
                if (anchorIndex < 0) anchorIndex = targetIndex;
                if (!additive) _selectedSegmentIds.Clear();
                var first = Math.Min(anchorIndex, targetIndex);
                var last = Math.Max(anchorIndex, targetIndex);
                for (var index = first; index <= last; index++) _selectedSegmentIds.Add(_segments[index].Id);
                _timelineSelectionAnchorId ??= anchorId;
                return;
            }

            if (additive)
            {
                if (_selectedSegmentIds.Contains(id) && _selectedSegmentIds.Count > 1) _selectedSegmentIds.Remove(id);
                else _selectedSegmentIds.Add(id);
                _timelineSelectionAnchorId = id;
                return;
            }

            SetSingleTimelineSelection(id);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Could not apply Media Studio timeline selection semantics for segment {SegmentId}.", id);
            throw;
        }
    }

    /// <summary>Handles timeline selection, including contiguous Shift selection and additive Ctrl/Command selection.</summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SelectTimelineSegment(Guid id, MouseEventArgs args)
    {
        try
        {
            CommitSelectedSegment();
            ApplyTimelineSelection(id, args.ShiftKey, args.CtrlKey || args.MetaKey);
            var primaryId = _selectedSegmentIds.Contains(id)
                ? id
                : _segments.FirstOrDefault(segment => _selectedSegmentIds.Contains(segment.Id))?.Id;
            if (!primaryId.HasValue) return;
            SelectSegment(primaryId.Value, seek: false, commitCurrent: false, preserveTimelineSelection: true);
            if (SelectedSegment is null) return;
            _playhead = TimelineEdits.SegmentTimelineStart(_segments, SelectedSegmentIndex, _playbackRate);
            await SeekSelectedAtPlayheadAsync().ConfigureAwait(true);
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);

        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(SelectTimelineSegment)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs clear selected segment fields for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void ClearSelectedSegmentFields()
    {
        try
        {
            _selectedSegmentId = null;
            _selectedSegmentIds.Clear();
            _timelineSelectionAnchorId = null;
            _name = IsVideo ? "Untitled Video" : "Untitled Audio";
            _dataUrl = string.Empty;
            _mimeType = string.Empty;
            _posterDataUrl = string.Empty;
            _duration = _trimStart = _trimEnd = 0;
            _videoSelectionIsPoint = false;
            _videoSelectionPointSeconds = 0;
            _videoSelectionStartSeconds = 0;
            _videoSelectionEndSeconds = 0;
            _videoSelectionUserCommitted = false;
            _selectedVideoLayerId = null;
            _selectedVideoFilterId = null;
            _selectedCutSectionId = null;
            _frameClipPolygon.Clear();
            _morphClipPolygon.Clear();
            _frameDraft.Clear();
            _editingMorphRegion = false;
            _selectedFramePointIndex = null;
            ClearPendingInsert();
            _waveform = [];
            RefreshPreviewSource();
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ClearSelectedSegmentFields)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs current fields as segment for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="inheritSelectedEdits">Value indicating whether cut sections and video layers should be cloned from the selected segment.</param>
    /// <returns>The publication media segment produced by the operation.</returns>
    private PublicationMediaSegment CurrentFieldsAsSegment(bool inheritSelectedEdits = true)
    {
        try
        {
            var selected = inheritSelectedEdits ? SelectedSegment : null;
            var segment = new PublicationMediaSegment
            {
                Name = _name,
                DataUrl = _dataUrl,
                MimeType = _mimeType,
                PosterDataUrl = _posterDataUrl,
                DurationSeconds = _duration,
                TrimStartSeconds = _trimStart,
                TrimEndSeconds = _trimEnd,
                HasTemporalSelection = IsVideo,
                TemporalSelectionCommitted = IsVideo && _videoSelectionUserCommitted,
                TemporalSelectionIsPoint = _videoSelectionIsPoint,
                TemporalSelectionStartSeconds = VideoSelectionStart,
                TemporalSelectionEndSeconds = VideoSelectionEnd,
                CutSections = selected?.CutSections.Select(CloneTemporalSection).ToList() ?? [],
                VideoLayers = IsVideo
                    ? (selected?.VideoLayers.Select(TimelineEdits.CloneVideoLayer).ToList() ?? [TimelineEdits.CreateDefaultVideoLayer(_name)])
                    : [],
                WaveformSamples = [.. _waveform]
            };
            return TimelineEdits.Normalize([segment], IsVideo).First();
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(CurrentFieldsAsSegment)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs replace or add current segment for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void ReplaceOrAddCurrentSegment()
    {
        try
        {
            var next = CurrentFieldsAsSegment();
            var index = SelectedSegmentIndex;
            if (index >= 0)
            {
                next.Id = _segments[index].Id;
                _segments[index] = next;
            }
            else
            {
                _segments.Add(next);
            }
            SelectSegment(next.Id, seek: false);
            _playhead = Math.Clamp(_playhead, 0, SequenceDuration);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ReplaceOrAddCurrentSegment)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs distance for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="first">First value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="second">Second value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double Distance(MediaFramePoint first, MediaFramePoint second)
    {
        try
        {
            var x = first.X - second.X;
            var y = first.Y - second.Y;
            return Math.Sqrt(x * x + y * y);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(Distance)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs clone frame points for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="points">Media frame point dependency used by the media studio workflow to provide the corresponding application capability.</param>
    /// <returns>The collection produced by the operation.</returns>
    private List<MediaFramePoint> CloneFramePoints(IEnumerable<MediaFramePoint>? points) {
        try
        {
            return (points ?? []).Select(point => new MediaFramePoint
        {
            X = Math.Clamp(point.X, 0, 1),
            Y = Math.Clamp(point.Y, 0, 1)
        }).ToList();
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(CloneFramePoints)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs frame clip CSS for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="points">Media frame point dependency used by the media studio workflow to provide the corresponding application capability.</param>
    /// <returns>The string produced by the operation.</returns>
    private string FrameClipCss(IReadOnlyList<MediaFramePoint> points) {
        try
        {
            return EditorText.BuildMediaStudioFramePolygonCss(points);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(FrameClipCss)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs percent for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string Percent(double value) {
        try
        {
            return $"{Math.Clamp(value, 0, 1) * 100:0.###}%";
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(Percent)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs segment style for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="segment">Segment value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string SegmentStyle(PublicationMediaSegment segment)
    {
        try
        {
            var duration = Math.Max(.01, SequenceDuration);
            var segmentDuration = segment.TimelineDurationSeconds > 0
                ? segment.TimelineDurationSeconds
                : segment.SourceLengthSeconds / Math.Max(.1, Math.Abs(segment.Speed) * _playbackRate);
            return $"width:{Percent(segmentDuration / duration)}";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(SegmentStyle)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs segment title for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="segment">Segment value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string SegmentTitle(PublicationMediaSegment segment)
    {
        try
        {
            var length = segment.TimelineDurationSeconds > 0
                ? segment.TimelineDurationSeconds
                : segment.SourceLengthSeconds / Math.Max(.1, Math.Abs(segment.Speed) * _playbackRate);
            if (segment.IsGap) return $"Gap · {Time(length)}";
            if (!IsPlayableSegment(segment))
            {
                var reference = string.IsNullOrWhiteSpace(segment.SourceReference.OriginalPath)
                    ? segment.SourceReference.Uri
                    : segment.SourceReference.OriginalPath;
                return $"{segment.Name} · missing media · {Time(length)} · {reference}";
            }
            return $"{segment.Name} · {Time(length)}";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(SegmentTitle)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Determines whether playable segment for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="segment">Segment value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsPlayableSegment(PublicationMediaSegment segment) {
        try
        {
            return !segment.IsGap && !string.IsNullOrWhiteSpace(segment.DataUrl);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(IsPlayableSegment)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs mouse mode text for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="mode">Mode value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="label">Label value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string MouseModeText(MediaStudioMouseMode mode, string label) {
        try
        {
     var localized = LT(label); return _mouseMode == mode ? $"✓ {localized}" : localized; 
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(MouseModeText)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs select section mode for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void SelectSectionMode() {
        try
        {
            SetMouseMode(MediaStudioMouseMode.SelectSection);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(SelectSectionMode)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs place playhead mode for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void PlacePlayheadMode() {
        try
        {
            SetMouseMode(MediaStudioMouseMode.PlacePlayhead);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(PlacePlayheadMode)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds cut line mode for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void AddCutLineMode() {
        try
        {
            SetMouseMode(MediaStudioMouseMode.AddCutLine);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(AddCutLineMode)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs frame region mode for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void FrameRegionMode() {
        try
        {
            BeginFrameRegion();
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(FrameRegionMode)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change video canvas mouse mode for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeVideoCanvasMouseMode(ChangeEventArgs args)
    {
        try
        {
            if (!Enum.TryParse<MediaStudioMouseMode>(Text(args), out var mode)) return;
            if (mode == MediaStudioMouseMode.FrameRegion) BeginFrameRegion();
            else SetMouseMode(mode);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeVideoCanvasMouseMode)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Sets mouse mode for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="mode">Mode value supplied to the media studio operation and used when producing its result.</param>
    private void SetMouseMode(MediaStudioMouseMode mode)
    {
        try
        {
            if (!IsVideo && mode == MediaStudioMouseMode.FrameRegion) mode = MediaStudioMouseMode.SelectSection;
            if (_frameRegionEditing && mode != MediaStudioMouseMode.FrameRegion) _frameDraft.Clear();
            _frameRegionEditing = mode == MediaStudioMouseMode.FrameRegion && _frameRegionEditing;
            _mouseMode = mode;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(SetMouseMode)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs timeline pointer down for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task TimelinePointerDown(PointerEventArgs args)
    {
        try
        {
            if (args.Button != 0 || !HasSource) return;
            var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
            var ratio = await module.InvokeAsync<double>("horizontalRatio", "media-studio-sequence-timeline", args.ClientX).ConfigureAwait(true);
            if (args.ShiftKey || args.CtrlKey || args.MetaKey)
            {
                CommitSelectedSegment();
                _playhead = Math.Clamp(ratio, 0, 1) * SequenceDuration;
                var index = TimelineEdits.SegmentIndexAt(_segments, _playbackRate, _playhead);
                if (index < 0) return;
                var targetId = _segments[index].Id;
                ApplyTimelineSelection(targetId, args.ShiftKey, args.CtrlKey || args.MetaKey);
                var primaryId = _selectedSegmentIds.Contains(targetId)
                    ? targetId
                    : _segments.FirstOrDefault(segment => _selectedSegmentIds.Contains(segment.Id))?.Id;
                if (!primaryId.HasValue) return;
                SelectSegment(primaryId.Value, seek: false, commitCurrent: false, preserveTimelineSelection: true);
                await SeekSelectedAtPlayheadAsync().ConfigureAwait(true);
                await InvokeAsync(StateHasChanged).ConfigureAwait(true);
                return;
            }
            await ActivateTimelinePosition(ratio, allowCut: true).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(TimelinePointerDown)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs show timeline context menu for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ShowTimelineContextMenu(MouseEventArgs args)
    {
        try
        {
            if (HasSource)
            {
                var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
                var ratio = await module.InvokeAsync<double>("horizontalRatio", "media-studio-sequence-timeline", args.ClientX).ConfigureAwait(true);
                await ActivateTimelinePosition(ratio, allowCut: false).ConfigureAwait(true);
            }
            await _mediaContextMenu.ShowAsync(args).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ShowTimelineContextMenu)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs activate timeline position for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="ratio">Ratio value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="allowCut">Value indicating whether allow cut should apply to this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ActivateTimelinePosition(double ratio, bool allowCut)
    {
        try
        {
            CommitSelectedSegment();
            if (_segments.Count == 0) return;
            _playhead = Math.Clamp(ratio, 0, 1) * SequenceDuration;
            var index = TimelineEdits.SegmentIndexAt(_segments, _playbackRate, _playhead);
            if (index >= 0) SelectSegment(_segments[index].Id, seek: false);
            if (allowCut && _mouseMode == MediaStudioMouseMode.AddCutLine)
            {
                var rightId = TimelineEdits.SplitAt(_segments, _playbackRate, _playhead);
                if (rightId is Guid id) SelectSegment(id, seek: false);
            }
            await SeekSelectedAtPlayheadAsync().ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ActivateTimelinePosition)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs seek selected at playhead for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SeekSelectedAtPlayheadAsync()
    {
        try
        {
            if (_module is null || _segments.Count == 0 || SelectedSegmentIndex < 0) return;
            try
            {
                var sourcePosition = TimelineEdits.SourcePositionAt(_segments, SelectedSegmentIndex, _playbackRate, _playhead);
                await _module.InvokeVoidAsync("seekMedia", "media-studio-preview", sourcePosition).ConfigureAwait(true);
            }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch (JSException) { }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(SeekSelectedAtPlayheadAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Adds cut line at playhead for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void AddCutLineAtPlayhead()
    {
        try
        {
            CommitSelectedSegment();
            var rightId = TimelineEdits.SplitAt(_segments, _playbackRate, _playhead);
            if (rightId is Guid id) SelectSegment(id, seek: false);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(AddCutLineAtPlayhead)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Removes cut line for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void RemoveCutLine()
    {
        try
        {
            CommitSelectedSegment();
            var index = SelectedSegmentIndex;
            if (!TimelineEdits.MergeBoundary(_segments, index)) return;
            SelectSegment(_segments[index - 1].Id, seek: false);
            _playhead = Math.Clamp(_playhead, 0, SequenceDuration);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(RemoveCutLine)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs copy selected section for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void CopySelectedSection()
    {
        try
        {
            CommitSelectedSegment();
            if (SelectedSegment is { } segment) _clipClipboard = TimelineEdits.Clone(segment);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(CopySelectedSection)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs copy video selection for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void CopyVideoSelection()
    {
        try
        {
            if (!CanCopyVideoSelection || SelectedSegment is not { } segment) return;
            CommitSelectedSegment();
            var copy = TimelineEdits.Clone(segment);
            copy.TrimStartSeconds = VideoSelectionStart;
            copy.TrimEndSeconds = VideoSelectionEnd;
            copy.Name = $"{segment.Name} (selection)";
            _clipClipboard = copy;
            _warning = $"Copied {Time(copy.SourceLengthSeconds)} from {segment.Name}. Paste inserts it after the selected sequence clip.";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(CopyVideoSelection)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs use video selection as trim for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void UseVideoSelectionAsTrim()
    {
        try
        {
            if (!CanUseVideoSelectionAsTrim) return;
            var start = VideoSelectionStart;
            var end = VideoSelectionEnd;
            ApplyTrimRange(start, end);
            SetVideoSelectionRange(_trimStart, _trimEnd, pointSelection: false);
            _videoSelectionUserCommitted = true;
            CommitSelectedSegment();
            _playhead = SourcePositionToTimeline(_trimStart);
            _warning = $"Trimmed {SelectedClipName} to {Time(_trimStart)} — {Time(_trimEnd)}.";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(UseVideoSelectionAsTrim)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs cut video selection for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void CutVideoSelection()
    {
        try
        {
            if (!CanCutVideoSelection) return;
            CommitSelectedSegment();
            var start = VideoSelectionStart;
            var end = VideoSelectionEnd;
            var startTimeline = SourcePositionToTimeline(start);

            if (_videoSelectionIsPoint)
            {
                var rightId = TimelineEdits.SplitAt(_segments, _playbackRate, startTimeline);
                if (rightId is Guid id) SelectSegment(id, seek: false);
                _playhead = Math.Clamp(startTimeline, 0, SequenceDuration);
                _warning = $"Added a cutline at {Time(startTimeline)} in the project sequence.";
                return;
            }

            var endTimeline = SourcePositionToTimeline(end);
            TimelineEdits.SplitAt(_segments, _playbackRate, endTimeline);
            TimelineEdits.SplitAt(_segments, _playbackRate, startTimeline);
            var middleTimeline = startTimeline + Math.Max(.001, endTimeline - startTimeline) / 2;
            var middleIndex = TimelineEdits.SegmentIndexAt(_segments, _playbackRate, middleTimeline);
            if (middleIndex >= 0) SelectSegment(_segments[middleIndex].Id, seek: false);
            _playhead = Math.Clamp(startTimeline, 0, SequenceDuration);
            _warning = $"Created clip boundaries around {Time(startTimeline)} — {Time(endTimeline)} and selected the middle clip.";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(CutVideoSelection)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs paste section for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void PasteSection()
    {
        try
        {
            if (_clipClipboard is null) return;
            CommitSelectedSegment();
            var clone = TimelineEdits.Duplicate(_clipClipboard);
            var index = SelectedSegmentIndex;
            _segments.Insert(index >= 0 ? index + 1 : _segments.Count, clone);
            SelectSegment(clone.Id, seek: false);
            _playhead = TimelineEdits.SegmentTimelineStart(_segments, _segments.IndexOf(clone), _playbackRate);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(PasteSection)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Deletes selected section for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void DeleteSelectedSection()
    {
        try
        {
            if (!CanDeleteSection) return;
            CommitSelectedSegment();
            var index = SelectedSegmentIndex;
            _segments.RemoveAt(index);
            if (_segments.Count == 0)
            {
                ClearSelectedSegmentFields();
                _selectedSegmentId = null;
                _playhead = 0;
                return;
            }

            var nextIndex = Math.Clamp(index, 0, _segments.Count - 1);
            SelectSegment(_segments[nextIndex].Id, seek: false);
            _playhead = TimelineEdits.SegmentTimelineStart(_segments, nextIndex, _playbackRate);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(DeleteSelectedSection)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Opens insert media picker for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task OpenInsertMediaPicker()
    {
        try
        {
            try
            {
                _pendingDropSourceSeconds = null;
                _pendingDropTimelineSeconds = null;
                _pendingDropIsPoint = _videoSelectionIsPoint;
                var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
                await module.InvokeVoidAsync("clickElement", "media-studio-insert-file-input").ConfigureAwait(true);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException or ObjectDisposedException or JSException)
            {
                _error = $"The insert picker could not be opened: {ex.Message}";
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(OpenInsertMediaPicker)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs video time selection committed for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="startSeconds">Start seconds value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="endSeconds">End seconds value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="pointSelection">Value indicating whether point selection should apply to this operation.</param>
    /// <param name="sourceDurationSeconds">Source duration seconds value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public Task VideoTimeSelectionCommitted(double startSeconds, double endSeconds, bool pointSelection, double sourceDurationSeconds)
    {
        try
        {
            if (!IsVideo || !HasSource) return Task.CompletedTask;
            if (_pendingInsertSegment is not null) ClearPendingInsert();
            ReconcileVideoDuration(sourceDurationSeconds, preserveCurrentSelection: true);
            SetVideoSelectionRange(startSeconds, endSeconds, pointSelection);
            _videoSelectionUserCommitted = true;
            CommitSelectedSegment();
            if (_autoCreateSelectionLayers && !pointSelection) CreateOrReuseSelectionLayer(forceNew: false, announce: false);
            _playhead = SourcePositionToTimeline(VideoSelectionStart);
            return InvokeAsync(StateHasChanged);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(VideoTimeSelectionCommitted)} failed.");
            throw;
        }
    }

}
