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
    /// Performs show media context menu for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ShowMediaContextMenu(MouseEventArgs args)
    {
        try
        {
            await _mediaContextMenu.ShowAsync(args).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ShowMediaContextMenu)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Sets trim start at playhead for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SetTrimStartAtPlayhead()
    {
        try
        {
            var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
            var position = await module.InvokeAsync<double>("getMediaPosition", "media-studio-preview").ConfigureAwait(true);
            ApplyTrimRange(position, _trimEnd);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(SetTrimStartAtPlayhead)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Sets trim end at playhead for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SetTrimEndAtPlayhead()
    {
        try
        {
            var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
            var position = await module.InvokeAsync<double>("getMediaPosition", "media-studio-preview").ConfigureAwait(true);
            ApplyTrimRange(_trimStart, position);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(SetTrimEndAtPlayhead)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs reset trim for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void ResetTrim()
    {
        try
        {
            ApplyTrimRange(0, _duration);
            if (IsVideo)
            {
                SetVideoSelectionRange(_trimStart, _trimEnd, pointSelection: false);
                _videoSelectionUserCommitted = true;
                CommitSelectedSegment();
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ResetTrim)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle muted from menu for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void ToggleMutedFromMenu() {
        try
        {
            _muted = !_muted;
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ToggleMutedFromMenu)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs toggle loop from menu for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void ToggleLoopFromMenu() {
        try
        {
            _loop = !_loop;
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ToggleLoopFromMenu)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Opens video project picker for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task OpenVideoProjectPicker()
    {
        try
        {
            if (!IsVideo) return;
            try
            {
                var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
                await module.InvokeVoidAsync("clickElement", "media-studio-project-input").ConfigureAwait(true);
            }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                _error = $"The video-project picker could not be opened: {ex.Message}";
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(OpenVideoProjectPicker)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Opens video project media picker for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task OpenVideoProjectMediaPicker()
    {
        try
        {
            if (!HasVideoProject) return;
            try
            {
                var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
                await module.InvokeVoidAsync("clickElement", "media-studio-project-media-input").ConfigureAwait(true);
            }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                _error = $"The project-media picker could not be opened: {ex.Message}";
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(OpenVideoProjectMediaPicker)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Loads video project for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Input file change event args dependency used by the media studio workflow to provide the corresponding application capability.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task LoadVideoProject(InputFileChangeEventArgs args)
    {
        try
        {
            if (!IsVideo) return;
            try
            {
                CommitProjectTrack();
                var stream = args.File.OpenReadStream(long.MaxValue);
                await using var configuredStreamAsyncDisposal = stream.ConfigureAwait(true);
                var imported = await VideoProjectImporter.ImportAsync(stream, args.File.Name).ConfigureAwait(true);
                _videoProject = TimelineEdits.CloneVideoProject(imported.Project);
                _videoProjectIssues = [.. imported.Issues];
                _activeProjectTrackId = ResolveActiveProjectTrackId(_videoProject);
                LoadProjectTrack(_activeProjectTrackId);
                var missing = VideoProjectMissingMediaCount;
                var report = _videoProjectIssues.Count == 0
                    ? "without compatibility warnings"
                    : $"with {_videoProjectIssues.Count} compatibility report item{(_videoProjectIssues.Count == 1 ? string.Empty : "s")}";
                _warning = $"Imported {_videoProject.SourceFormat} project '{_videoProject.Name}' {report}. "
                    + (missing == 0 ? "All referenced media is linked." : $"Relink {missing} missing media reference{(missing == 1 ? string.Empty : "s")} to preview it.");
                _error = null;
            }
            catch (Exception ex)
            {
                _error = $"The open video project could not be imported: {ex.Message}";
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(LoadVideoProject)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs relink video project media for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Input file change event args dependency used by the media studio workflow to provide the corresponding application capability.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task RelinkVideoProjectMedia(InputFileChangeEventArgs args)
    {
        try
        {
            if (_videoProject is null) return;
            try
            {
                CommitProjectTrack();
                var files = args.GetMultipleFiles(512);
                var linked = 0;
                var unmatched = new List<string>();
                foreach (var file in files)
                {
                    var stream = file.OpenReadStream(long.MaxValue);
                    await using var configuredStreamAsyncDisposal = stream.ConfigureAwait(true);
                    using var buffer = new MemoryStream();
                    await stream.CopyToAsync(buffer).ConfigureAwait(true);
                    if (buffer.Length == 0) continue;
                    var mime = ResolveProjectMediaMimeType(file.Name, file.ContentType);
                    var dataUrl = $"data:{mime};base64,{Convert.ToBase64String(buffer.ToArray())}";
                    var matches = _videoProject.Tracks
                        .SelectMany(track => track.Segments)
                        .Where(segment => !segment.IsGap && MatchesProjectMedia(segment, file.Name))
                        .ToList();
                    if (matches.Count == 0)
                    {
                        unmatched.Add(file.Name);
                        continue;
                    }
                    foreach (var segment in matches)
                    {
                        segment.DataUrl = dataUrl;
                        segment.MimeType = mime;
                        segment.SourceReference.MimeType = mime;
                        segment.SourceReference.Missing = false;
                        segment.SourceReference.Metadata["relinked_name"] = file.Name;
                        linked++;
                    }
                }
                LoadProjectTrack(_activeProjectTrackId);
                _warning = linked == 0
                    ? "No selected file matched an imported source name, path, reel, or identifier."
                    : $"Relinked {linked} project clip reference{(linked == 1 ? string.Empty : "s")}."
                        + (unmatched.Count == 0 ? string.Empty : $" Unmatched: {string.Join(", ", unmatched.Take(8))}.");
                _error = null;
            }
            catch (Exception ex)
            {
                _error = $"Project media could not be relinked: {ex.Message}";
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(RelinkVideoProjectMedia)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change project track for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ChangeProjectTrack(ChangeEventArgs args)
    {
        try
        {
            if (!Guid.TryParse(Text(args), out var trackId) || _videoProject is null || trackId == _activeProjectTrackId) return;
            CommitProjectTrack();
            LoadProjectTrack(trackId);
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeProjectTrack)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Loads project track for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="trackId">Identifier of the track to use for this operation.</param>
    private void LoadProjectTrack(Guid trackId)
    {
        try
        {
            if (_videoProject is null) return;
            var track = _videoProject.Tracks.FirstOrDefault(candidate => candidate.Id == trackId && candidate.Kind == MediaTimelineTrackKind.Video)
                ?? _videoProject.Tracks.OrderBy(candidate => candidate.Order).FirstOrDefault(candidate => candidate.Kind == MediaTimelineTrackKind.Video)
                ?? _videoProject.Tracks.OrderBy(candidate => candidate.Order).FirstOrDefault();
            if (track is null)
            {
                _segments.Clear();
                _activeProjectTrackId = Guid.Empty;
                ClearSelectedSegmentFields();
                return;
            }

            _activeProjectTrackId = track.Id;
            _videoProject.ActiveTrackId = track.Id;
            _segments.Clear();
            _segments.AddRange(TimelineEdits.Normalize(TimelineEdits.CreateTrackProjection(_videoProject, track.Id), video: true));
            var selected = _segments.FirstOrDefault(IsPlayableSegment)
                ?? _segments.FirstOrDefault(segment => !segment.IsGap)
                ?? _segments.FirstOrDefault();
            if (selected is null) ClearSelectedSegmentFields();
            else SelectSegment(selected.Id, seek: false, forceReload: true);
            _playhead = 0;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(LoadProjectTrack)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs commit project track for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void CommitProjectTrack()
    {
        try
        {
            CommitSelectedSegment();
            if (_videoProject is null || _activeProjectTrackId == Guid.Empty) return;
            TimelineEdits.ReplaceTrackProjection(_videoProject, _activeProjectTrackId, _segments);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(CommitProjectTrack)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Resolves active project track identifier for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="project">Project value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The GUID produced by the operation.</returns>
    private Guid ResolveActiveProjectTrackId(VideoProjectDocument project)
    {
        try
        {
            var active = project.Tracks.FirstOrDefault(track => track.Id == project.ActiveTrackId && track.Kind == MediaTimelineTrackKind.Video);
            return active?.Id
                ?? project.Tracks.OrderBy(track => track.Order).FirstOrDefault(track => track.Kind == MediaTimelineTrackKind.Video)?.Id
                ?? project.Tracks.OrderBy(track => track.Order).FirstOrDefault()?.Id
                ?? Guid.Empty;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ResolveActiveProjectTrackId)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs matches project media for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="segment">Segment value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="selectedFileName">Selected file name value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool MatchesProjectMedia(PublicationMediaSegment segment, string selectedFileName)
    {
        try
        {
            var targetName = Path.GetFileName(selectedFileName);
            var targetStem = Path.GetFileNameWithoutExtension(targetName);
            var candidates = new[]
            {
                segment.Name,
                segment.SourceReference.Id,
                segment.SourceReference.ReelName,
                segment.SourceReference.OriginalPath,
                segment.SourceReference.Uri,
                segment.SourceReference.Metadata.TryGetValue("target_url", out var target) ? target : string.Empty
            };
            return candidates
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .SelectMany(value => new[] { ProjectReferenceFileName(value), Path.GetFileNameWithoutExtension(ProjectReferenceFileName(value)) })
                .Any(value => string.Equals(value, targetName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value, targetStem, StringComparison.OrdinalIgnoreCase));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(MatchesProjectMedia)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs project reference file name for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ProjectReferenceFileName(string value)
    {
        try
        {
            var clean = value.Trim().Replace('\\', '/');
            if (Uri.TryCreate(clean, UriKind.Absolute, out var uri) && uri.IsFile)
                clean = uri.LocalPath.Replace('\\', '/');
            var query = clean.IndexOfAny(['?', '#']);
            if (query >= 0) clean = clean[..query];
            return Path.GetFileName(clean);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ProjectReferenceFileName)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Resolves project media MIME type for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="fileName">File name value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="declaredMimeType">Declared mime type value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ResolveProjectMediaMimeType(string fileName, string? declaredMimeType)
    {
        try
        {
            var declared = NormalizeMimeType(declaredMimeType, string.Empty);
            if (!string.IsNullOrWhiteSpace(declared)) return declared;
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".mp4" or ".m4v" => "video/mp4",
                ".webm" => "video/webm",
                ".ogv" or ".ogg" => "video/ogg",
                ".mov" => "video/quicktime",
                ".wav" => "audio/wav",
                ".mp3" => "audio/mpeg",
                ".m4a" or ".aac" => "audio/mp4",
                ".flac" => "audio/flac",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ResolveProjectMediaMimeType)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Opens media picker for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task OpenMediaPicker()
    {
        try
        {
            try
            {
                var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
                await module.InvokeVoidAsync("clickElement", "media-studio-file-input").ConfigureAwait(true);
            }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                _error = $"The media picker could not be opened: {ex.Message}";
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(OpenMediaPicker)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Loads file for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Input file change event args dependency used by the media studio workflow to provide the corresponding application capability.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task LoadFile(InputFileChangeEventArgs args)
    {
        try
        {
            try
            {
                var segment = await ReadMediaFileAsync(args.File).ConfigureAwait(true);
                var index = SelectedSegmentIndex;
                if (index >= 0)
                {
                    segment.Id = _segments[index].Id;
                    _segments[index] = segment;
                }
                else
                {
                    _segments.Add(segment);
                }
                SelectSegment(segment.Id, seek: false, forceReload: true);
                _playhead = TimelineEdits.SegmentTimelineStart(_segments, _segments.IndexOf(segment), _playbackRate);
                var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
                await module.InvokeVoidAsync("clearRetainedRecording", "media-studio-preview").ConfigureAwait(true);
                ClearRetainedRecordingState();
            }
            catch (Exception ex)
            {
                _error = $"The media source could not be decoded: {ex.Message}";
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(LoadFile)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Loads inserted file for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Input file change event args dependency used by the media studio workflow to provide the corresponding application capability.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task LoadInsertedFile(InputFileChangeEventArgs args)
    {
        try
        {
            try
            {
                CommitSelectedSegment();
                var segment = await ReadMediaFileAsync(args.File).ConfigureAwait(true);
                if (IsVideo && SelectedSegmentIndex >= 0)
                {
                    PreparePendingInsert(segment);
                }
                else
                {
                    var insertionTimeline = Math.Clamp(_playhead, 0, SequenceDuration);
                    TimelineEdits.InsertAt(_segments, _playbackRate, insertionTimeline, segment);
                    var index = _segments.FindIndex(item => item.Id == segment.Id);
                    SelectSegment(segment.Id, seek: false);
                    _playhead = index >= 0
                        ? TimelineEdits.SegmentTimelineStart(_segments, index, _playbackRate)
                        : insertionTimeline;
                    _warning = $"Inserted {segment.Name} at {Time(insertionTimeline)} in the sequence.";
                }
            }
            catch (Exception ex)
            {
                _error = $"The media could not be inserted into the selected range: {ex.Message}";
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(LoadInsertedFile)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs media studio file drop rejected for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="message">Message value supplied to the media studio operation and used when producing its result.</param>
    [JSInvokable]
    public void MediaStudioFileDropRejected(string? message)
    {
        try
        {
            _error = string.IsNullOrWhiteSpace(message)
                ? $"Drop a supported {(IsVideo ? "video" : "audio")} file into this Studio."
                : message;
            TaskRunner.Run(nameof(MediaStudio), nameof(MediaStudioFileDropRejected), async _ => await InvokeAsync(StateHasChanged).ConfigureAwait(true));
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(MediaStudioFileDropRejected)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Reads media file for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="file">Browser file dependency used by the media studio workflow to provide the corresponding application capability.</param>
    /// <returns>The publication media segment produced by the operation.</returns>
    private async Task<PublicationMediaSegment> ReadMediaFileAsync(IBrowserFile file)
    {
        try
        {
            Guid inspectionAssetId = Guid.Empty;
            try
            {
                var mimeType = ResolveImportedMimeType(file.Name, file.ContentType, IsVideo);
                var expectedPrefix = IsVideo ? "video/" : "audio/";
                if (!mimeType.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Select a supported {(IsVideo ? "video" : "audio")} file.");

                var stream = file.OpenReadStream(long.MaxValue);
                await using var configuredStreamAsyncDisposal = stream.ConfigureAwait(true);
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer).ConfigureAwait(true);
                var bytes = buffer.ToArray();
                if (bytes.Length == 0) throw new InvalidDataException("The selected media file is empty.");

                var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
                inspectionAssetId = Guid.NewGuid();
                var localUrl = MediaAssets.RegisterBytes(inspectionAssetId, bytes, mimeType);
                MediaSourceInfo info;
                try
                {
                    info = await module.InvokeAsync<MediaSourceInfo>("inspectMediaDataUrl", localUrl, IsVideo ? "video" : "audio").ConfigureAwait(true);
                    _warning = null;
                }
                catch (Exception metadataError)
                {
                    info = new MediaSourceInfo
                    {
                        MimeType = mimeType,
                        DurationSeconds = 5,
                        WaveformSamples = []
                    };
                    _warning = $"Imported without browser metadata. Adjust the trim length if needed. {metadataError.Message}";
                }

                mimeType = NormalizeMimeType(string.IsNullOrWhiteSpace(info.MimeType) ? mimeType : info.MimeType,
                    IsVideo ? "video/mp4" : "audio/mpeg");
                var duration = Math.Max(.01, info.DurationSeconds);
                _error = null;
                return new PublicationMediaSegment
                {
                    Name = file.Name,
                    MimeType = mimeType,
                    DataUrl = NormalizeMediaDataUrl($"data:{mimeType};base64,{Convert.ToBase64String(bytes)}", mimeType),
                    PosterDataUrl = info.PosterDataUrl ?? string.Empty,
                    DurationSeconds = duration,
                    TrimStartSeconds = 0,
                    TrimEndSeconds = duration,
                    WaveformSamples = info.WaveformSamples ?? []
                };
            }
            finally
            {
                if (inspectionAssetId != Guid.Empty) MediaAssets.Remove(inspectionAssetId);
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ReadMediaFileAsync)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs inspect source for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task InspectSource()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_dataUrl)) return;
            try
            {
                var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
                var fallbackMimeType = IsVideo ? "video/webm" : "audio/webm";
                _mimeType = NormalizeMimeType(_mimeType, fallbackMimeType);
                _dataUrl = NormalizeMediaDataUrl(_dataUrl, _mimeType);
                RefreshPreviewSource();
                var info = await module.InvokeAsync<MediaSourceInfo>("inspectMediaSource", "media-studio-preview", _previewSource, IsVideo ? "video" : "audio").ConfigureAwait(true);
                _duration = Math.Max(.01, info.DurationSeconds);
                _trimStart = 0;
                _trimEnd = _duration;
                _posterDataUrl = info.PosterDataUrl ?? string.Empty;
                _waveform = info.WaveformSamples ?? [];
                _fadeIn = _fadeOut = 0;
                _error = null;
                ReplaceOrAddCurrentSegment();
            }
            catch (Exception ex)
            {
                _error = $"The media source could not be decoded: {ex.Message}";
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(InspectSource)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs record camera for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task RecordCamera() {
        try
        {
            return RequestVideoRecording("camera");
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(RecordCamera)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs record screen for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task RecordScreen() {
        try
        {
            return RequestVideoRecording("screen");
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(RecordScreen)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs record microphone for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task RecordMicrophone() {
        try
        {
            return StartRecording("microphone");
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(RecordMicrophone)} failed.");
            throw;
        }
    }

    /// <summary>Requests a camera or screen recording without allowing a retained recording to be silently replaced.</summary>
    /// <param name="source">Browser capture source to start after any sequence placement choice.</param>
    /// <returns>A task that completes after the request has started or the placement UI has been shown.</returns>
    private Task RequestVideoRecording(string source)
    {
        try
        {
            if (!IsVideo) return StartRecording(source);
            if (HasRetainedRecording && !_retainedRecordingCommittedToSequence)
            {
                _recordingPlacementRequired = true;
                _recordingPlacementBoundary = Math.Clamp(_recordingPlacementBoundary, 0, _segments.Count);
                _warning = LT("Insert or explicitly keep the completed recording before starting another recording so no captured media is lost.");
                OperationalNotifications.Warning(_warning, LT("Recording position required"), nameof(MediaStudio));
                return InvokeAsync(StateHasChanged);
            }
            if (_segments.Count == 0)
            {
                _plannedRecordingPlacementBoundary = null;
                return StartRecording(source);
            }

            _recordingStartPlacementRequired = true;
            _pendingRecordingSource = source;
            _recordingStartPlacementBoundary = SelectedSegmentIndex >= 0
                ? Math.Clamp(SelectedSegmentIndex + 1, 0, _segments.Count)
                : _segments.Count;
            _warning = LT("Choose where the next recording belongs before opening the browser capture picker.");
            return InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Preparing Media Studio video recording placement failed for session {SessionId}.", SessionId);
            OperationalNotifications.Error(ex.Message, LT("Recording placement failed"), nameof(MediaStudio));
            throw;
        }
    }

    /// <summary>Changes the pre-recording sequence boundary for the next video capture.</summary>
    /// <param name="args">Browser change event containing the selected boundary.</param>
    private void ChangeRecordingStartPlacementBoundary(ChangeEventArgs args)
    {
        try
        {
            if (int.TryParse(args.Value?.ToString(), out var boundary))
                _recordingStartPlacementBoundary = Math.Clamp(boundary, 0, _segments.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Changing the pre-recording sequence boundary failed.");
            OperationalNotifications.Error(ex.Message, LT("Recording placement failed"), nameof(MediaStudio));
        }
    }

    /// <summary>Confirms the pre-recording sequence boundary and starts the browser capture picker.</summary>
    /// <returns>A task that completes when the recording start request has finished.</returns>
    private Task ConfirmRecordingStartPlacement()
    {
        try
        {
            if (!_recordingStartPlacementRequired || string.IsNullOrWhiteSpace(_pendingRecordingSource)) return Task.CompletedTask;
            var source = _pendingRecordingSource;
            _plannedRecordingPlacementBoundary = Math.Clamp(_recordingStartPlacementBoundary, 0, _segments.Count);
            _recordingStartPlacementRequired = false;
            _pendingRecordingSource = string.Empty;
            _warning = LT("The new recording will keep the selected sequence position when capture completes.");
            return StartRecording(source);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Confirming the pre-recording sequence boundary failed for session {SessionId}.", SessionId);
            OperationalNotifications.Error(ex.Message, LT("Recording placement failed"), nameof(MediaStudio));
            throw;
        }
    }

    /// <summary>Cancels the pending pre-recording sequence boundary without changing the current sequence.</summary>
    private void CancelRecordingStartPlacement()
    {
        try
        {
            _recordingStartPlacementRequired = false;
            _recordingStartPlacementBoundary = 0;
            _pendingRecordingSource = string.Empty;
            _plannedRecordingPlacementBoundary = null;
            _warning = null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Cancelling the pre-recording sequence boundary failed for session {SessionId}.", SessionId);
            OperationalNotifications.Error(ex.Message, LT("Recording placement failed"), nameof(MediaStudio));
        }
    }

    /// <summary>
    /// Performs change recording quality mode for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeRecordingQualityMode(ChangeEventArgs args)
    {
        try
        {
            _recordingAdaptiveQuality = string.Equals(args.Value?.ToString(), "adaptive", StringComparison.OrdinalIgnoreCase);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeRecordingQualityMode)} failed.");
            throw;
        }
    }

    /// <summary>Changes the capture-size preset while preserving the explicit custom dimensions for later reuse.</summary>
    /// <param name="args">Browser change event containing the selected capture-size key.</param>
    private void ChangeRecordingSizeMode(ChangeEventArgs args)
    {
        try
        {
            var requested = args.Value?.ToString()?.Trim().ToLowerInvariant();
            _recordingSizeMode = string.IsNullOrWhiteSpace(requested) ? "source" : requested;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Changing the Media Studio capture-size preset failed.");
            OperationalNotifications.Error(ex.Message, LT("Recording quality failed"), nameof(MediaStudio));
        }
    }

    /// <summary>Changes the standard or source-native frame-rate preset used for browser capture.</summary>
    /// <param name="args">Browser change event containing the selected frame-rate key.</param>
    private void ChangeRecordingFrameRatePreset(ChangeEventArgs args)
    {
        try
        {
            var value = args.Value?.ToString()?.Trim() ?? string.Empty;
            if (string.Equals(value, "source", StringComparison.OrdinalIgnoreCase))
            {
                _recordingFrameRate = 0;
                return;
            }
            if (string.Equals(value, "custom", StringComparison.OrdinalIgnoreCase)) return;
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var frameRate) && double.IsFinite(frameRate))
                _recordingFrameRate = Math.Clamp(frameRate, RecordingPolicy.MinimumFrameRate, RecordingPolicy.MaximumFrameRate);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Changing the Media Studio frame-rate preset failed.");
            OperationalNotifications.Error(ex.Message, LT("Recording quality failed"), nameof(MediaStudio));
        }
    }

    /// <summary>Changes the explicit capture frame rate, including fractional cinema and NTSC rates.</summary>
    /// <param name="args">Browser change event containing the requested frame rate.</param>
    private void ChangeRecordingFrameRate(ChangeEventArgs args)
    {
        try
        {
            if (!TryDouble(args, out var frameRate)) return;
            _recordingFrameRate = _recordingAdaptiveQuality && frameRate <= 0
                ? 0
                : Math.Clamp(frameRate, RecordingPolicy.MinimumFrameRate, RecordingPolicy.MaximumFrameRate);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Changing the Media Studio capture frame rate failed.");
            OperationalNotifications.Error(ex.Message, LT("Recording quality failed"), nameof(MediaStudio));
        }
    }

    /// <summary>Resolves a capture-size key into pixel dimensions and a human-readable label.</summary>
    /// <param name="mode">Capture-size key selected in the Video Studio.</param>
    /// <returns>The width, height, and label represented by the selected capture-size key.</returns>
    private (int Width, int Height, string Name) ResolveRecordingSizeSelection(string? mode)
    {
        try
        {
            var normalized = mode?.Trim().ToLowerInvariant() ?? "source";
            if (normalized.StartsWith("viewport:", StringComparison.Ordinal))
            {
                var key = normalized["viewport:".Length..];
                var preset = _recordingViewportPresets.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));
                if (preset is not null) return (preset.Width, preset.Height, $"{LT("Panel / Div preset")} · {LT(preset.Name)}");
            }

            return normalized switch
            {
                "video-8k" => (7680, 4320, "8K UHD"),
                "video-4k" => (3840, 2160, "4K UHD"),
                "video-dci4k" => (4096, 2160, "DCI 4K"),
                "video-uwqhd" => (3440, 1440, "UWQHD"),
                "video-qhd" => (2560, 1440, "QHD"),
                "video-fhd" => (1920, 1080, "Full HD"),
                "video-hd" => (1280, 720, "HD"),
                "video-vertical4k" => (2160, 3840, LT("Vertical 4K")),
                "video-verticalfhd" => (1080, 1920, LT("Vertical Full HD")),
                "video-square2k" => (2160, 2160, LT("Square 2K")),
                "video-square" => (1080, 1080, LT("Square Full HD")),
                "master" => (RecordingPolicy.MasterWidth, RecordingPolicy.MasterHeight, LT("Streaming master")),
                "output" => (RecordingPolicy.OutputWidth, RecordingPolicy.OutputHeight, LT("Streaming output")),
                "custom" => (_recordingCustomWidth, _recordingCustomHeight, LT("Custom")),
                _ => (0, 0, LT("Source/native"))
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Resolving Media Studio capture-size preset {CaptureSizeMode} failed.", mode);
            return (0, 0, LT("Source/native"));
        }
    }

    /// <summary>Formats one Panel Studio viewport key for reuse by the Video Studio capture-size selector.</summary>
    /// <param name="key">Stable Panel Studio preview-preset key.</param>
    /// <returns>The Video Studio capture-size key.</returns>
    private string RecordingViewportPresetValue(string key)
    {
        try
        {
            return $"viewport:{key}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Formatting a Panel Studio viewport preset for Video Studio failed.");
            throw;
        }
    }

    /// <summary>Formats pixel dimensions as their reduced integer aspect ratio.</summary>
    /// <param name="width">Pixel width.</param>
    /// <param name="height">Pixel height.</param>
    /// <returns>A reduced width-to-height ratio such as 16:9 or 9:16.</returns>
    private string AspectRatioLabel(int width, int height)
    {
        try
        {
            var left = Math.Max(1, Math.Abs(width));
            var right = Math.Max(1, Math.Abs(height));
            var a = left;
            var b = right;
            while (b != 0)
            {
                var remainder = a % b;
                a = b;
                b = remainder;
            }
            var divisor = Math.Max(1, a);
            return $"{left / divisor}:{right / divisor}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Formatting Media Studio aspect ratio {Width}x{Height} failed.", width, height);
            return $"{Math.Max(1, width)}:{Math.Max(1, height)}";
        }
    }

    /// <summary>Returns the standard frame-rate key that matches the current frame rate, or custom when no standard matches.</summary>
    /// <returns>The source, standard frame-rate, or custom key for the current capture setting.</returns>
    private string GetRecordingFrameRatePresetKey()
    {
        try
        {
            if (_recordingAdaptiveQuality && _recordingFrameRate <= .0005) return "source";
            double[] standards = [23.976, 24, 25, 29.97, 30, 48, 50, 59.94, 60, 90, 120, 144, 165, 240];
            foreach (var standard in standards)
            {
                if (Math.Abs(_recordingFrameRate - standard) <= .005)
                    return standard.ToString("0.###", CultureInfo.InvariantCulture);
            }
            return "custom";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Resolving the Media Studio frame-rate preset failed.");
            return "custom";
        }
    }

    /// <summary>
    /// Normalizes adaptive profile for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeAdaptiveProfile(string? value)
    {
        try
        {
            var normalized = value?.Trim().ToLowerInvariant();
            return normalized is "efficiency" or "balanced" or "quality" ? normalized : "quality";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(NormalizeAdaptiveProfile)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Normalizes recording codec preference for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="value">Value value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string NormalizeRecordingCodecPreference(string? value)
    {
        try
        {
            var normalized = value?.Trim().ToLowerInvariant();
            return normalized is "vp8" or "vp9" ? normalized : "auto";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(NormalizeRecordingCodecPreference)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Builds browser recording options for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>The object produced by the operation.</returns>
    private object BuildBrowserRecordingOptions()
    {
        try
        {
            var policy = RecordingPolicy;
            var adaptive = policy.AdaptiveQuality;
            var width = RecordingTargetWidth;
            var height = RecordingTargetHeight;
            if (_recordingSizeMode != "source")
            {
                width = Math.Clamp(width, policy.MinimumWidth, policy.MaximumWidth);
                height = Math.Clamp(height, policy.MinimumHeight, policy.MaximumHeight);
            }

            _recordingFrameRate = _recordingAdaptiveQuality && _recordingFrameRate <= 0
                ? 0
                : Math.Clamp(_recordingFrameRate, policy.MinimumFrameRate, policy.MaximumFrameRate);
            _recordingVideoBitrateKbps = Math.Clamp(_recordingVideoBitrateKbps, policy.MinimumVideoBitrateKbps, policy.MaximumVideoBitrateKbps);
            _recordingAudioBitrateKbps = Math.Clamp(_recordingAudioBitrateKbps, 32, Math.Max(32, adaptive.MaximumAudioBitrateKbps));
            _recordingCodecPreference = NormalizeRecordingCodecPreference(_recordingCodecPreference);
            _recordingAdaptiveProfile = NormalizeAdaptiveProfile(_recordingAdaptiveProfile);

            return new
            {
                width,
                height,
                frameRate = _recordingAdaptiveQuality ? 0 : _recordingFrameRate,
                maximumFrameRate = _recordingAdaptiveQuality ? _recordingFrameRate : 0,
                videoBitsPerSecond = _recordingAdaptiveQuality && _recordingAdaptVideo ? 0 : _recordingVideoBitrateKbps * 1000,
                audioBitsPerSecond = _recordingAdaptiveQuality && _recordingAdaptAudio ? 0 : _recordingAudioBitrateKbps * 1000,
                recorderChunkMilliseconds = Math.Clamp(adaptive.RecorderChunkMilliseconds, 100, 5000),
                codecPreference = _recordingCodecPreference,
                adaptive = new
                {
                    enabled = _recordingAdaptiveQuality,
                    adaptVideo = _recordingAdaptVideo,
                    adaptAudio = _recordingAdaptAudio,
                    useBrowserCapabilityProbe = _recordingUseBrowserCapabilityProbe,
                    profile = _recordingAdaptiveProfile,
                    preserveNativeResolution = _recordingPreserveNativeResolution,
                    allowFrameRateReduction = _recordingAllowFrameRateReduction,
                    allowResolutionReduction = _recordingAllowResolutionReduction,
                    preferPowerEfficientCodec = adaptive.PreferPowerEfficientCodec,
                    preferSmoothEncoding = adaptive.PreferSmoothEncoding,
                    codecPreferenceOrder = adaptive.BrowserCodecPreferenceOrder,
                    screenBitsPerPixel = adaptive.ScreenBitsPerPixel,
                    cameraBitsPerPixel = adaptive.CameraBitsPerPixel,
                    mixedBitsPerPixel = adaptive.MixedBitsPerPixel,
                    efficiencyMultiplier = adaptive.EfficiencyMultiplier,
                    balancedMultiplier = adaptive.BalancedMultiplier,
                    qualityMultiplier = adaptive.QualityMultiplier,
                    vp9BitrateFactor = adaptive.Vp9BitrateFactor,
                    vp8BitrateFactor = adaptive.Vp8BitrateFactor,
                    minimumVideoBitrateKbps = policy.MinimumVideoBitrateKbps,
                    maximumVideoBitrateKbps = policy.MaximumVideoBitrateKbps,
                    minimumAudioBitrateKbps = adaptive.MinimumAudioBitrateKbps,
                    audioBitratePerChannelKbps = adaptive.AudioBitratePerChannelKbps,
                    maximumAudioBitrateKbps = adaptive.MaximumAudioBitrateKbps,
                    defaultAudioChannels = adaptive.DefaultAudioChannels,
                    maximumAutomaticFrameRate = adaptive.MaximumAutomaticFrameRate,
                    minimumFrameRate = policy.MinimumFrameRate,
                    frameRateFallbackRatio = adaptive.FrameRateFallbackRatio,
                    resolutionFallbackRatio = adaptive.ResolutionFallbackRatio,
                    maximumAdaptationAttempts = adaptive.MaximumAdaptationAttempts,
                    metadataPosterMaximumPixels = Math.Max(0, adaptive.MetadataPosterMaximumPixels),
                    metadataAnalysisDelayMilliseconds = Math.Clamp(adaptive.MetadataAnalysisDelayMilliseconds, 0, 10000)
                }
            };
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(BuildBrowserRecordingOptions)} failed.");
            throw;
        }
    }

    /// <summary>Exports each selected timeline clip range into its own rendered file.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task ExportSelectedTimelineRangesSeparately()
    {
        try
        {
            return ExportSelectedTimelineRanges(combined: false);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Could not start separate selected-range export.");
            throw;
        }
    }

    /// <summary>Exports the selected timeline clip ranges in sequence order into one rendered file.</summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private Task ExportSelectedTimelineRangesCombined()
    {
        try
        {
            return ExportSelectedTimelineRanges(combined: true);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Could not start combined selected-range export.");
            throw;
        }
    }

    /// <summary>Builds browser-safe selected-range payloads and delegates the actual rendering to the Media Studio module.</summary>
    /// <param name="combined">Value indicating whether combined should apply to this operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ExportSelectedTimelineRanges(bool combined)
    {
        if (!CanExportSelectedTimelineRanges) return;
        CommitSelectedSegment();
        var selected = SelectedTimelineSegments.Where(IsPlayableSegment).ToList();
        if (selected.Count == 0) return;
        _error = null;
        _renderingTimelineSelectionExport = true;
        try
        {
            var clips = selected.Select(segment =>
            {
                var trimStart = Math.Max(0, segment.TrimStartSeconds);
                var trimEnd = Math.Max(trimStart + .001, segment.EffectiveTrimEndSeconds);
                var useSelection = segment.HasTemporalSelection
                    && segment.TemporalSelectionCommitted
                    && !segment.TemporalSelectionIsPoint
                    && segment.TemporalSelectionEndSeconds > segment.TemporalSelectionStartSeconds + .001;
                var maximumRangeStart = Math.Max(trimStart, trimEnd - .001);
                var rangeStart = useSelection ? Math.Clamp(segment.TemporalSelectionStartSeconds, trimStart, maximumRangeStart) : trimStart;
                var rangeEnd = useSelection ? Math.Clamp(segment.TemporalSelectionEndSeconds, rangeStart + .001, trimEnd) : trimEnd;
                return new
                {
                    id = segment.Id.ToString("D"),
                    name = segment.Name,
                    source = MediaAssets.GetOrRegister(segment),
                    mimeType = segment.MimeType,
                    startSeconds = rangeStart,
                    endSeconds = rangeEnd,
                    playbackRate = Math.Max(.1, _playbackRate * Math.Max(.1, Math.Abs(segment.Speed))),
                    audioGain = _muted ? 0 : Math.Clamp(_volume, 0, 1),
                    effectConfig = new
                    {
                        fitMode = _videoFitMode.ToString().ToLowerInvariant(),
                        layers = VideoLayerPayload(segment.VideoLayers)
                    }
                };
            }).ToArray();
            var module = await EnsureMediaModuleAsync().ConfigureAwait(true);
            var result = await module.InvokeAsync<TimelineSelectionExportResult>(
                "exportSelectedTimelineRanges",
                "media-studio-preview",
                clips,
                BuildBrowserRecordingOptions(),
                combined,
                LT("Selected ranges")).ConfigureAwait(true);
            if (result.Cancelled)
            {
                _warning = LT("Selected range export cancelled.");
                return;
            }
            _warning = combined
                ? $"{LT("Exported selected ranges combined")}: {result.FileName} · {result.ClipCount} {LT("sections")} · {FormatBytes(result.SizeBytes)}."
                : $"{LT("Exported selected ranges separately")}: {result.FileCount} {LT("files")} · {result.ClipCount} {LT("sections")} · {FormatBytes(result.SizeBytes)}.";
            Logger.LogInformation(
                "Exported {ClipCount} selected Video Studio ranges as {FileCount} file(s), combined={Combined}, total bytes={SizeBytes}.",
                result.ClipCount, result.FileCount, combined, result.SizeBytes);
        }
        catch (Exception ex)
        {
            _error = $"{LT("Selected range export failed")}: {ex.Message}";
            Logger.LogError(ex, "Selected Video Studio range export failed for session {SessionId}.", SessionId);
            OperationalNotifications.Error(_error, LT("Selected range export failed"), nameof(MediaStudio));
        }
        finally
        {
            _renderingTimelineSelectionExport = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
        }
    }

}
