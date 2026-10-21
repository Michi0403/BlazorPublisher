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
    /// Performs video source duration resolved for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="durationSeconds">Duration seconds value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public async Task VideoSourceDurationResolved(double durationSeconds)
    {
        try
        {
            if (!IsVideo || SelectedSegment is null) return;
            var placeholderDuration = _duration <= .05 || SelectedSegment.DurationSeconds <= .05;
            if (!ReconcileVideoDuration(durationSeconds, preserveCurrentSelection: false)) return;
            CommitSelectedSegment();
            _warning = placeholderDuration
                ? $"Recovered the browser video duration ({Time(durationSeconds)}) without replacing the committed selection."
                : $"Updated the selected clip duration to {Time(durationSeconds)} from browser metadata.";
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(VideoSourceDurationResolved)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs video playhead committed for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="sourceSeconds">Source seconds value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public async Task VideoPlayheadCommitted(double sourceSeconds)
    {
        try
        {
            if (!IsVideo || !HasSource) return;
            var source = Math.Clamp(double.IsFinite(sourceSeconds) ? sourceSeconds : _trimStart, _trimStart, _trimEnd);
            _playhead = SourcePositionToTimeline(source);
            var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
            await module.InvokeVoidAsync("seekMedia", "media-studio-preview", source).ConfigureAwait(true);
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(VideoPlayheadCommitted)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs video cutline committed for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="sourceSeconds">Source seconds value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public async Task VideoCutlineCommitted(double sourceSeconds)
    {
        try
        {
            if (!IsVideo || !HasSource) return;
            var source = Math.Clamp(double.IsFinite(sourceSeconds) ? sourceSeconds : _trimStart, _trimStart, _trimEnd);
            _playhead = SourcePositionToTimeline(source);
            AddCutLineAtPlayhead();
            await SeekSelectedAtPlayheadAsync().ConfigureAwait(true);
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(VideoCutlineCommitted)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs media studio drop insertion point selected for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="sourceSeconds">Source seconds value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="pointSelection">Value indicating whether point selection should apply to this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public Task MediaStudioDropInsertionPointSelected(double sourceSeconds, bool pointSelection)
    {
        try
        {
            if (!IsVideo || !HasSource) return Task.CompletedTask;
            var minimum = pointSelection ? sourceSeconds : VideoSelectionStart;
            var maximum = pointSelection ? sourceSeconds : VideoSelectionEnd;
            if (maximum < minimum) (minimum, maximum) = (maximum, minimum);
            _pendingDropSourceSeconds = Math.Clamp(
                double.IsFinite(sourceSeconds) ? sourceSeconds : minimum,
                Math.Max(0, minimum),
                Math.Max(Math.Max(0, minimum), maximum));
            _pendingDropIsPoint = pointSelection;
            return Task.CompletedTask;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(MediaStudioDropInsertionPointSelected)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs media studio drop timeline point selected for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="timelineSeconds">Timeline seconds value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public Task MediaStudioDropTimelinePointSelected(double timelineSeconds)
    {
        try
        {
            if (!HasSource) return Task.CompletedTask;
            _pendingDropTimelineSeconds = Math.Clamp(
                double.IsFinite(timelineSeconds) ? timelineSeconds : _playhead,
                0,
                SequenceDuration);
            _pendingDropSourceSeconds = null;
            return Task.CompletedTask;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(MediaStudioDropTimelinePointSelected)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs media studio timeline segment dropped for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="segmentId">Identifier of the segment to use for this operation.</param>
    /// <param name="timelineSeconds">Timeline seconds value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public async Task MediaStudioTimelineSegmentDropped(string segmentId, double timelineSeconds)
    {
        try
        {
            if (!Guid.TryParse(segmentId, out var id) || !_segments.Any(segment => segment.Id == id)) return;
            CommitSelectedSegment();
            var movedId = TimelineEdits.MoveAt(_segments, _playbackRate, id, timelineSeconds);
            SelectSegment(movedId, seek: false);
            var movedIndex = _segments.FindIndex(segment => segment.Id == movedId);
            _playhead = movedIndex >= 0
                ? TimelineEdits.SegmentTimelineStart(_segments, movedIndex, _playbackRate)
                : Math.Clamp(timelineSeconds, 0, SequenceDuration);
            _warning = $"Moved {SelectedClipName} to {Time(_playhead)} in the sequence.";
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(MediaStudioTimelineSegmentDropped)} failed.");
            throw;
        }
    }

    /// <summary>Reorders a Video Studio effect layer from the shared drag-and-drop layer stack.</summary>
    /// <param name="draggedId">Identifier of the dragged to use for this operation.</param>
    /// <param name="targetId">Identifier of the target to use for this operation.</param>
    /// <param name="placeAfter">Value indicating whether place after should apply to this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public async Task MediaStudioVideoLayerDropped(string draggedId, string targetId, bool placeAfter)
    {
        try
        {
            if (!Guid.TryParse(draggedId, out var dragged) || !Guid.TryParse(targetId, out var target) || dragged == target || SelectedSegment is not { } segment) return;
            var visual = segment.VideoLayers.Select(layer => layer.Id).ToList();
            if (!visual.Remove(dragged)) return;
            var targetIndex = visual.IndexOf(target);
            if (targetIndex < 0) return;
            visual.Insert(Math.Clamp(targetIndex + (placeAfter ? 1 : 0), 0, visual.Count), dragged);
            var byId = segment.VideoLayers.ToDictionary(layer => layer.Id);
            segment.VideoLayers = visual.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
            _selectedVideoLayerId = dragged;
            CommitSelectedSegment(refreshVideoEffects: true);
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(MediaStudioVideoLayerDropped)} failed.");
            throw;
        }
    }

    /// <summary>Reorders one filter inside the selected Video Studio effect layer through drag and drop.</summary>
    /// <param name="draggedId">Identifier of the dragged to use for this operation.</param>
    /// <param name="targetId">Identifier of the target to use for this operation.</param>
    /// <param name="placeAfter">Value indicating whether place after should apply to this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public async Task MediaStudioVideoFilterDropped(string draggedId, string targetId, bool placeAfter)
    {
        try
        {
            if (!Guid.TryParse(draggedId, out var dragged) || !Guid.TryParse(targetId, out var target) || dragged == target || SelectedVideoLayer is not { } layer) return;
            var visual = layer.Filters.Select(filter => filter.Id).ToList();
            if (!visual.Remove(dragged)) return;
            var targetIndex = visual.IndexOf(target);
            if (targetIndex < 0) return;
            visual.Insert(Math.Clamp(targetIndex + (placeAfter ? 1 : 0), 0, visual.Count), dragged);
            var byId = layer.Filters.ToDictionary(filter => filter.Id);
            layer.Filters = visual.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
            _selectedVideoFilterId = dragged;
            CommitSelectedSegment(refreshVideoEffects: true);
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(MediaStudioVideoFilterDropped)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs source position to timeline for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="sourceSeconds">Source seconds value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double SourcePositionToTimeline(double sourceSeconds)
    {
        try
        {
            var index = SelectedSegmentIndex;
            if (index < 0 || index >= _segments.Count) return Math.Clamp(_playhead, 0, SequenceDuration);
            var segment = _segments[index];
            var source = Math.Clamp(
                double.IsFinite(sourceSeconds) ? sourceSeconds : segment.TrimStartSeconds,
                segment.TrimStartSeconds,
                segment.EffectiveTrimEndSeconds);
            return TimelineEdits.SegmentTimelineStart(_segments, index, _playbackRate)
                + (source - segment.TrimStartSeconds) / (Math.Max(.1, _playbackRate) * Math.Max(.0001, Math.Abs(segment.Speed)));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(SourcePositionToTimeline)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs prepare pending insert for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="segment">Segment value supplied to the media studio operation and used when producing its result.</param>
    private void PreparePendingInsert(PublicationMediaSegment segment)
    {
        try
        {
            if (_pendingDropTimelineSeconds.HasValue)
            {
                _pendingInsertSegment = segment;
                _pendingInsertTimelineSeconds = Math.Clamp(_pendingDropTimelineSeconds.Value, 0, SequenceDuration);
                _pendingInsertSourceSeconds = 0;
                _pendingInsertMinimumSourceSeconds = 0;
                _pendingInsertMaximumSourceSeconds = 0;
                _pendingDropTimelineSeconds = null;
                _pendingDropSourceSeconds = null;
                _pendingDropIsPoint = false;
                _warning = $"Choose Insert here to place {segment.Name} at sequence time {Time(_pendingInsertTimelineSeconds.Value)}.";
                return;
            }

            _pendingInsertTimelineSeconds = null;
            var pointSelection = _pendingDropSourceSeconds.HasValue ? _pendingDropIsPoint : _videoSelectionIsPoint;
            var minimum = pointSelection ? (_pendingDropSourceSeconds ?? _videoSelectionPointSeconds) : _trimStart;
            if (!pointSelection) minimum = VideoSelectionStart;
            var maximum = pointSelection ? minimum : VideoSelectionEnd;
            if (maximum < minimum) (minimum, maximum) = (maximum, minimum);
            minimum = Math.Clamp(minimum, 0, Math.Max(.01, _duration));
            maximum = Math.Clamp(maximum, minimum, Math.Max(minimum, _duration));

            _pendingInsertSegment = segment;
            _pendingInsertMinimumSourceSeconds = minimum;
            _pendingInsertMaximumSourceSeconds = maximum;
            var suggested = _pendingDropSourceSeconds ?? (pointSelection ? minimum : minimum + (maximum - minimum) / 2);
            _pendingInsertSourceSeconds = Math.Clamp(suggested, minimum, maximum);
            _pendingDropSourceSeconds = null;
            _pendingDropIsPoint = false;
            _warning = pointSelection
                ? $"Choose Insert here to place {segment.Name} at {Time(_pendingInsertSourceSeconds)}."
                : $"Choose the exact timestamp inside the selected range for {segment.Name}.";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(PreparePendingInsert)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change pending insert position for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangePendingInsertPosition(ChangeEventArgs args)
    {
        try
        {
            if (!TryDouble(args, out var value)) return;
            _pendingInsertSourceSeconds = Math.Clamp(
                value,
                _pendingInsertMinimumSourceSeconds,
                Math.Max(_pendingInsertMinimumSourceSeconds, _pendingInsertMaximumSourceSeconds));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangePendingInsertPosition)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs confirm pending insert for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void ConfirmPendingInsert()
    {
        try
        {
            if (_pendingInsertSegment is null) return;
            CommitSelectedSegment();
            var insertionTimeline = _pendingInsertTimelineSeconds ?? SourcePositionToTimeline(_pendingInsertSourceSeconds);
            var inserted = _pendingInsertSegment;
            TimelineEdits.InsertAt(_segments, _playbackRate, insertionTimeline, inserted);
            var insertedIndex = _segments.FindIndex(segment => segment.Id == inserted.Id);
            ClearPendingInsert();
            SelectSegment(inserted.Id, seek: false);
            _playhead = insertedIndex >= 0
                ? TimelineEdits.SegmentTimelineStart(_segments, insertedIndex, _playbackRate)
                : Math.Clamp(insertionTimeline, 0, SequenceDuration);
            _warning = $"Inserted {inserted.Name} at {Time(insertionTimeline)} in the sequence.";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ConfirmPendingInsert)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Determines whether cel pending insert for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void CancelPendingInsert()
    {
        try
        {
            var name = _pendingInsertSegment?.Name;
            ClearPendingInsert();
            if (!string.IsNullOrWhiteSpace(name)) _warning = $"Cancelled insertion of {name}.";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(CancelPendingInsert)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs clear pending insert for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void ClearPendingInsert()
    {
        try
        {
            _pendingInsertSegment = null;
            _pendingInsertSourceSeconds = 0;
            _pendingInsertMinimumSourceSeconds = 0;
            _pendingInsertMaximumSourceSeconds = 0;
            _pendingInsertTimelineSeconds = null;
            _pendingDropSourceSeconds = null;
            _pendingDropTimelineSeconds = null;
            _pendingDropIsPoint = false;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ClearPendingInsert)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs begin frame region for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void BeginFrameRegion() {
        try
        {
            BeginActiveFrameRegion(editMorph: false);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(BeginFrameRegion)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs begin morph region for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void BeginMorphRegion() {
        try
        {
            BeginActiveFrameRegion(editMorph: true);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(BeginMorphRegion)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs begin active frame region for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="editMorph">Value indicating whether edit morph should apply to this operation.</param>
    private void BeginActiveFrameRegion(bool editMorph)
    {
        try
        {
            if (!IsVideo || !HasSource || SelectedVideoLayer is null) return;
            CommitFrameRegionToSelectedLayer();
            LoadFrameRegionFromSelectedLayer();
            _editingMorphRegion = editMorph;
            _frameDraft.Clear();
            _frameDraft.AddRange(CloneFramePoints(StoredActiveFramePoints));
            _selectedFramePointIndex = null;
            _frameRegionEditing = true;
            _mouseMode = MediaStudioMouseMode.FrameRegion;
            var label = editMorph ? "morph target" : "source region";
            _warning = $"Editing the {label} owned by {SelectedVideoLayer.Name}. A grabbed point stays selected until another point or the canvas is selected.";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(BeginActiveFrameRegion)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs frame overlay pointer down for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task FrameOverlayPointerDown(PointerEventArgs args)
    {
        try
        {
            if (!IsVideo || args.Button != 0 || _mouseMode != MediaStudioMouseMode.FrameRegion) return;
            if (!_frameRegionEditing) BeginActiveFrameRegion(_editingMorphRegion);
            _selectedFramePointIndex = null;
            var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
            var point = await module.InvokeAsync<MediaFramePoint>(
                "normalizedPoint",
                "media-studio-frame-overlay-host",
                args.ClientX,
                args.ClientY).ConfigureAwait(true);
            point.X = Math.Clamp(point.X, 0, 1);
            point.Y = Math.Clamp(point.Y, 0, 1);
            if (_frameDraft.Count == 0 || Distance(_frameDraft[^1], point) > .003)
            {
                _frameDraft.Add(point);
                _selectedFramePointIndex = _frameDraft.Count - 1;
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(FrameOverlayPointerDown)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs finish frame region for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task FinishFrameRegion()
    {
        try
        {
            if (!CanFinishFrameRegion) return Task.CompletedTask;
            if (_frameDraft.Count > 3 && Distance(_frameDraft[0], _frameDraft[^1]) < .015)
                _frameDraft.RemoveAt(_frameDraft.Count - 1);
            if (_frameDraft.Count < 3) return Task.CompletedTask;
            var target = StoredActiveFramePoints;
            target.Clear();
            target.AddRange(CloneFramePoints(_frameDraft));
            _selectedFramePointIndex = Math.Clamp(_selectedFramePointIndex ?? target.Count - 1, 0, target.Count - 1);
            _frameDraft.Clear();
            _frameRegionEditing = false;
            _mouseMode = MediaStudioMouseMode.SelectSection;
            CommitFrameRegionToSelectedLayer();
            RefreshSelectedLayerOpenScad(commit: false);
            CommitSelectedSegment(refreshVideoEffects: true);
            _warning = $"Applied the {(_editingMorphRegion ? "morph target" : "source region")} to {SelectedVideoLayer?.Name}.";
            return Task.CompletedTask;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(FinishFrameRegion)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Determines whether cel frame region for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void CancelFrameRegion()
    {
        try
        {
            _frameDraft.Clear();
            _frameRegionEditing = false;
            _selectedFramePointIndex = null;
            if (_mouseMode == MediaStudioMouseMode.FrameRegion) _mouseMode = MediaStudioMouseMode.SelectSection;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(CancelFrameRegion)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs clear frame region for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void ClearFrameRegion()
    {
        try
        {
            var label = _editingMorphRegion ? "morph target" : "source region";
            StoredActiveFramePoints.Clear();
            _frameDraft.Clear();
            _frameRegionEditing = false;
            _selectedFramePointIndex = null;
            if (_mouseMode == MediaStudioMouseMode.FrameRegion) _mouseMode = MediaStudioMouseMode.SelectSection;
            CommitFrameRegionToSelectedLayer();
            RefreshSelectedLayerOpenScad(commit: false);
            CommitSelectedSegment(refreshVideoEffects: true);
            _warning = $"Cleared the {label} on {SelectedVideoLayer?.Name}.";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ClearFrameRegion)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Removes last frame point for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void RemoveLastFramePoint()
    {
        try
        {
            if (_frameDraft.Count == 0) return;
            _frameDraft.RemoveAt(_frameDraft.Count - 1);
            _selectedFramePointIndex = _frameDraft.Count == 0 ? null : _frameDraft.Count - 1;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(RemoveLastFramePoint)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Removes selected frame point for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void RemoveSelectedFramePoint()
    {
        try
        {
            if (_selectedFramePointIndex is not int index) return;
            var points = _frameRegionEditing ? _frameDraft : StoredActiveFramePoints;
            if (index < 0 || index >= points.Count) return;
            points.RemoveAt(index);
            _selectedFramePointIndex = points.Count == 0 ? null : Math.Min(index, points.Count - 1);
            if (!_frameRegionEditing)
            {
                CommitFrameRegionToSelectedLayer();
                RefreshSelectedLayerOpenScad(commit: false);
                CommitSelectedSegment(refreshVideoEffects: true);
            }
            _warning = $"Removed point {index + 1} from the {(_editingMorphRegion ? "morph target" : "source region")}.";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(RemoveSelectedFramePoint)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs copy frame region for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void CopyFrameRegion()
    {
        try
        {
            var points = ActiveFramePoints;
            if (points.Count < 3) return;
            _frameClipboard.Clear();
            _frameClipboard.AddRange(CloneFramePoints(points));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(CopyFrameRegion)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs paste frame region for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void PasteFrameRegion()
    {
        try
        {
            if (!CanPasteFrameRegion || SelectedVideoLayer is null) return;
            var target = _frameRegionEditing ? _frameDraft : StoredActiveFramePoints;
            target.Clear();
            target.AddRange(CloneFramePoints(_frameClipboard));
            _selectedFramePointIndex = target.Count - 1;
            if (!_frameRegionEditing)
            {
                CommitFrameRegionToSelectedLayer();
                RefreshSelectedLayerOpenScad(commit: false);
                CommitSelectedSegment(refreshVideoEffects: true);
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(PasteFrameRegion)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs clone temporal section for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="section">Section value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The media temporal section produced by the operation.</returns>
    private MediaTemporalSection CloneTemporalSection(MediaTemporalSection section) {
        try
        {
            return new()
    {
        Id = section.Id == Guid.Empty ? Guid.NewGuid() : section.Id,
        Name = string.IsNullOrWhiteSpace(section.Name) ? "Cut section" : section.Name.Trim(),
        Enabled = section.Enabled,
        StartSeconds = section.StartSeconds,
        EndSeconds = section.EndSeconds
    };
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(CloneTemporalSection)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs video cut section style for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="section">Section value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string VideoCutSectionStyle(MediaTemporalSection section)
    {
        try
        {
            var visibleStart = Math.Max(0, Math.Min(_duration, _trimStart));
            var visibleEnd = Math.Max(visibleStart + .01, Math.Min(Math.Max(.01, _duration), _trimEnd));
            var visibleSpan = Math.Max(.01, visibleEnd - visibleStart);
            var start = Math.Clamp((section.StartSeconds - visibleStart) / visibleSpan, 0, 1);
            var end = Math.Clamp((section.EndSeconds - visibleStart) / visibleSpan, start, 1);
            return $"left:{Percent(start)};width:{Percent(Math.Max(.001, end - start))}";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(VideoCutSectionStyle)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Persists selected range as cut section for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void SaveSelectedRangeAsCutSection()
    {
        try
        {
            if (!CanSaveCutSection || SelectedSegment is not { } segment) return;
            _videoSelectionUserCommitted = true;
            CommitSelectedSegment();
            segment = SelectedSegment!;
            var section = new MediaTemporalSection
            {
                Name = $"Cut section {segment.CutSections.Count + 1}",
                StartSeconds = VideoSelectionStart,
                EndSeconds = VideoSelectionEnd
            };
            segment.CutSections.Add(section);
            _selectedCutSectionId = section.Id;
            CommitSelectedSegment(refreshVideoEffects: true);
            _warning = $"Saved {section.Name}: {Time(section.StartSeconds)} — {Time(section.EndSeconds)}.";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(SaveSelectedRangeAsCutSection)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs select cut section for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    private void SelectCutSection(Guid id)
    {
        try
        {
            if (SelectedSegment?.CutSections.Any(section => section.Id == id) != true) return;
            _selectedCutSectionId = id;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(SelectCutSection)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs use selected cut section for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void UseSelectedCutSection()
    {
        try
        {
            if (SelectedCutSection is not { } section) return;
            SetVideoSelectionRange(section.StartSeconds, section.EndSeconds, pointSelection: false);
            _videoSelectionUserCommitted = true;
            CommitSelectedSegment(refreshVideoEffects: true);
            _playhead = SourcePositionToTimeline(VideoSelectionStart);
            TaskRunner.Run(nameof(MediaStudio), nameof(UseSelectedCutSection), async _ => await InvokeAsync(SeekVideoSelectionStartAsync).ConfigureAwait(true));
            _warning = $"Loaded {section.Name} into the current selection.";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(UseSelectedCutSection)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Removes selected cut section for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void RemoveSelectedCutSection()
    {
        try
        {
            if (SelectedSegment is not { } segment || _selectedCutSectionId is not Guid id) return;
            var index = segment.CutSections.FindIndex(section => section.Id == id);
            if (index < 0) return;
            var removed = segment.CutSections[index].Name;
            segment.CutSections.RemoveAt(index);
            _selectedCutSectionId = segment.CutSections.Count == 0
                ? null
                : segment.CutSections[Math.Clamp(index, 0, segment.CutSections.Count - 1)].Id;
            CommitSelectedSegment(refreshVideoEffects: true);
            _warning = $"Removed {removed}. The current selection was not changed.";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(RemoveSelectedCutSection)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs select initial video layer for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="segment">Segment value supplied to the media studio operation and used when producing its result.</param>
    private void SelectInitialVideoLayer(PublicationMediaSegment segment)
    {
        try
        {
            if (!IsVideo)
            {
                _selectedVideoLayerId = null;
                _selectedVideoFilterId = null;
                _frameClipPolygon.Clear();
                _frameDraft.Clear();
                return;
            }

            segment.VideoLayers = TimelineEdits.NormalizeVideoLayers(
                segment.VideoLayers,
                segment.TrimStartSeconds,
                segment.EffectiveTrimEndSeconds,
                segment.Name);
            var selected = segment.VideoLayers.FirstOrDefault(layer => layer.Id == _selectedVideoLayerId)
                ?? segment.VideoLayers.FirstOrDefault();
            _selectedVideoLayerId = selected?.Id;
            _selectedVideoFilterId = selected?.Filters.FirstOrDefault(filter => filter.Id == _selectedVideoFilterId)?.Id
                ?? selected?.Filters.FirstOrDefault()?.Id;
            LoadFrameRegionFromSelectedLayer();
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(SelectInitialVideoLayer)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Loads frame region from selected layer for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void LoadFrameRegionFromSelectedLayer()
    {
        try
        {
            _frameClipPolygon.Clear();
            _morphClipPolygon.Clear();
            _frameDraft.Clear();
            _frameRegionEditing = false;
            _selectedFramePointIndex = null;
            if (SelectedVideoLayer?.Region?.Points is { } points)
                _frameClipPolygon.AddRange(CloneFramePoints(points));
            if (SelectedVideoLayer?.MorphRegion?.Points is { } morphPoints)
                _morphClipPolygon.AddRange(CloneFramePoints(morphPoints));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(LoadFrameRegionFromSelectedLayer)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs commit frame region to selected layer for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void CommitFrameRegionToSelectedLayer()
    {
        try
        {
            if (!IsVideo || SelectedVideoLayer is not { } layer) return;
            layer.Region ??= new VideoFrameRegion();
            layer.Region.Points = CloneFramePoints(_frameClipPolygon);
            layer.MorphRegion ??= new VideoFrameRegion { Name = "Morph target" };
            layer.MorphRegion.Points = CloneFramePoints(_morphClipPolygon);
            layer.MorphEnabled = layer.MorphEnabled && layer.MorphRegion.Points.Count >= 3;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(CommitFrameRegionToSelectedLayer)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs select video layer for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    private void SelectVideoLayer(Guid id)
    {
        try
        {
            if (SelectedSegment?.VideoLayers.Any(layer => layer.Id == id) != true) return;
            CommitFrameRegionToSelectedLayer();
            _selectedVideoLayerId = id;
            _selectedVideoFilterId = SelectedVideoLayer?.Filters.FirstOrDefault()?.Id;
            LoadFrameRegionFromSelectedLayer();
            _warning = $"Selected video layer {SelectedVideoLayer?.Name}.";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(SelectVideoLayer)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs toggle auto create selection layers for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ToggleAutoCreateSelectionLayers(ChangeEventArgs args) {
        try
        {
            _autoCreateSelectionLayers = Bool(args);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ToggleAutoCreateSelectionLayers)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Creates selection layer for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void CreateSelectionLayer() {
        try
        {
            CreateOrReuseSelectionLayer(forceNew: true, announce: true);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(CreateSelectionLayer)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Creates or reuse selection layer for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="forceNew">Value indicating whether force new should apply to this operation.</param>
    /// <param name="announce">Value indicating whether announce should apply to this operation.</param>
    /// <returns>The video effect layer produced by the operation.</returns>
    private VideoEffectLayer? CreateOrReuseSelectionLayer(bool forceNew, bool announce)
    {
        try
        {
            if (!CanCreateSelectionLayer || SelectedSegment is not { } segment) return null;
            CommitFrameRegionToSelectedLayer();
            var start = VideoSelectionStart;
            var end = VideoSelectionEnd;
            VideoEffectLayer? layer = null;
            if (!forceNew)
            {
                layer = segment.VideoLayers.FirstOrDefault(candidate =>
                    candidate.Kind == VideoEffectLayerKind.Selection2D && candidate.HasTemporalRange &&
                    Math.Abs(candidate.TemporalStartSeconds - start) <= .02 &&
                    Math.Abs(candidate.TemporalEndSeconds - end) <= .02);
            }
            if (layer is null)
            {
                layer = TimelineEdits.CreateDefaultVideoLayer($"Selection layer {segment.VideoLayers.Count(candidate => candidate.Kind == VideoEffectLayerKind.Selection2D) + 1}");
                layer.Kind = VideoEffectLayerKind.Selection2D;
                layer.Name = $"Selection {Time(start)}–{Time(end)}";
                layer.HasTemporalRange = true;
                layer.TemporalStartSeconds = start;
                layer.TemporalEndSeconds = end;
                var sourceRegion = _frameClipPolygon.Count >= 3
                    ? _frameClipPolygon
                    : SelectedVideoLayer?.Region?.Points;
                if (sourceRegion is { Count: >= 3 }) layer.Region.Points = CloneFramePoints(sourceRegion);
                segment.VideoLayers.Add(layer);
            }
            _selectedVideoLayerId = layer.Id;
            _selectedVideoFilterId = layer.Filters.FirstOrDefault()?.Id;
            LoadFrameRegionFromSelectedLayer();
            CommitSelectedSegment(refreshVideoEffects: true);
            if (announce) _warning = $"Created {layer.Name}. Effects on this layer render only from {Time(start)} to {Time(end)} and inside its frame region.";
            return layer;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(CreateOrReuseSelectionLayer)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Adds video layer for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void AddVideoLayer()
    {
        try
        {
            if (SelectedSegment is not { } segment) return;
            CommitFrameRegionToSelectedLayer();
            var layer = TimelineEdits.CreateDefaultVideoLayer($"Layer {segment.VideoLayers.Count + 1}");
            layer.Name = $"Video layer {segment.VideoLayers.Count + 1}";
            if (!_videoSelectionIsPoint && VideoSelectionEnd - VideoSelectionStart >= .01)
            {
                layer.HasTemporalRange = true;
                layer.TemporalStartSeconds = VideoSelectionStart;
                layer.TemporalEndSeconds = VideoSelectionEnd;
            }
            segment.VideoLayers.Add(layer);
            _selectedVideoLayerId = layer.Id;
            _selectedVideoFilterId = null;
            LoadFrameRegionFromSelectedLayer();
            CommitSelectedSegment(refreshVideoEffects: true);
            _warning = $"Added {layer.Name}. Its region and filters are independent from the other layers.";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(AddVideoLayer)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs duplicate video layer for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void DuplicateVideoLayer()
    {
        try
        {
            if (SelectedSegment is not { } segment || SelectedVideoLayer is not { } selected) return;
            CommitFrameRegionToSelectedLayer();
            var index = segment.VideoLayers.FindIndex(layer => layer.Id == selected.Id);
            if (index < 0) return;
            var clone = TimelineEdits.CloneVideoLayer(selected);
            clone.Id = Guid.NewGuid();
            clone.Name = $"{selected.Name} (copy)";
            clone.Region.Id = Guid.NewGuid();
            clone.MorphRegion.Id = Guid.NewGuid();
            foreach (var filter in clone.Filters) filter.Id = Guid.NewGuid();
            segment.VideoLayers.Insert(index + 1, clone);
            _selectedVideoLayerId = clone.Id;
            _selectedVideoFilterId = clone.Filters.FirstOrDefault()?.Id;
            LoadFrameRegionFromSelectedLayer();
            CommitSelectedSegment(refreshVideoEffects: true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(DuplicateVideoLayer)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs move video layer up for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void MoveVideoLayerUp() {
        try
        {
            MoveSelectedVideoLayer(1);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(MoveVideoLayerUp)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs move video layer down for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void MoveVideoLayerDown() {
        try
        {
            MoveSelectedVideoLayer(-1);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(MoveVideoLayerDown)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs move selected video layer for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="offset">Offset value supplied to the media studio operation and used when producing its result.</param>
    private void MoveSelectedVideoLayer(int offset)
    {
        try
        {
            if (SelectedSegment is not { } segment) return;
            CommitFrameRegionToSelectedLayer();
            var index = SelectedVideoLayerIndex;
            var target = index + offset;
            if (index < 0 || target < 0 || target >= segment.VideoLayers.Count) return;
            var layer = segment.VideoLayers[index];
            segment.VideoLayers.RemoveAt(index);
            segment.VideoLayers.Insert(target, layer);
            CommitSelectedSegment(refreshVideoEffects: true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(MoveSelectedVideoLayer)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Deletes video layer for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void DeleteVideoLayer()
    {
        try
        {
            if (!CanDeleteVideoLayer || SelectedSegment is not { } segment) return;
            var index = SelectedVideoLayerIndex;
            if (index < 0) return;
            var removed = segment.VideoLayers[index].Name;
            segment.VideoLayers.RemoveAt(index);
            var next = segment.VideoLayers[Math.Clamp(index, 0, segment.VideoLayers.Count - 1)];
            _selectedVideoLayerId = next.Id;
            _selectedVideoFilterId = next.Filters.FirstOrDefault()?.Id;
            LoadFrameRegionFromSelectedLayer();
            CommitSelectedSegment(refreshVideoEffects: true);
            _warning = $"Deleted {removed}.";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(DeleteVideoLayer)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs toggle video layer visibility for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    private void ToggleVideoLayerVisibility(Guid id)
    {
        try
        {
            var layer = SelectedSegment?.VideoLayers.FirstOrDefault(candidate => candidate.Id == id);
            if (layer is null) return;
            layer.Visible = !layer.Visible;
            CommitSelectedSegment(refreshVideoEffects: true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ToggleVideoLayerVisibility)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change video layer name for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeVideoLayerName(ChangeEventArgs args)
    {
        try
        {
            if (SelectedVideoLayer is not { } layer) return;
            layer.Name = string.IsNullOrWhiteSpace(Text(args)) ? "Video layer" : Text(args).Trim();
            CommitSelectedSegment(refreshVideoEffects: true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeVideoLayerName)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change video layer opacity for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeVideoLayerOpacity(ChangeEventArgs args)
    {
        try
        {
            if (SelectedVideoLayer is not { } layer || !TryDouble(args, out var value)) return;
            layer.Opacity = Math.Clamp(value, 0, 1);
            CommitSelectedSegment(refreshVideoEffects: true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeVideoLayerOpacity)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change video layer blend mode for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeVideoLayerBlendMode(ChangeEventArgs args)
    {
        try
        {
            if (SelectedVideoLayer is not { } layer || !Enum.TryParse<VideoEffectBlendMode>(Text(args), out var blend)) return;
            layer.BlendMode = blend;
            CommitSelectedSegment(refreshVideoEffects: true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeVideoLayerBlendMode)} failed.");
            throw;
        }
    }

}
