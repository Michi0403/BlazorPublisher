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
    /// <summary>Computes RecordingPolicy for the render path with component-local diagnostics.</summary>
    /// <returns>The publisher media session defaults policy produced by the operation.</returns>
    private PublisherMediaSessionDefaultsPolicy GetRecordingPolicyForRender()
    {
        try
        {
            return RuntimePolicy.MediaSessionDefaults;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.RecordingPolicy failed.");
            throw;
        }
    }

    /// <summary>Computes RecordingTargetWidth for the render path with component-local diagnostics.</summary>
    /// <returns>The int produced by the operation.</returns>
    private int GetRecordingTargetWidthForRender()
    {
        try
        {
            return ResolveRecordingSizeSelection(_recordingSizeMode).Width;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.RecordingTargetWidth failed.");
            throw;
        }
    }

    /// <summary>Computes RecordingTargetHeight for the render path with component-local diagnostics.</summary>
    /// <returns>The int produced by the operation.</returns>
    private int GetRecordingTargetHeightForRender()
    {
        try
        {
            return ResolveRecordingSizeSelection(_recordingSizeMode).Height;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.RecordingTargetHeight failed.");
            throw;
        }
    }

    /// <summary>Computes HasVideoProject for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetHasVideoProjectForRender()
    {
        try
        {
            return IsVideo && _videoProject is { Tracks.Count: > 0 };
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.HasVideoProject failed.");
            throw;
        }
    }

    /// <summary>Computes ProjectVideoTracks for the render path with component-local diagnostics.</summary>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<MediaTimelineTrack> GetProjectVideoTracksForRender()
    {
        try
        {
            return _videoProject?.Tracks
        .Where(track => track.Kind == MediaTimelineTrackKind.Video)
        .OrderBy(track => track.Order)
        .ToList() ?? [];
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.ProjectVideoTracks failed.");
            throw;
        }
    }

    /// <summary>Computes VideoProjectMissingMediaCount for the render path with component-local diagnostics.</summary>
    /// <returns>The int produced by the operation.</returns>
    private int GetVideoProjectMissingMediaCountForRender()
    {
        try
        {
            return _videoProject?.Tracks
        .SelectMany(track => track.Segments)
        .Count(segment => !segment.IsGap && string.IsNullOrWhiteSpace(segment.DataUrl) && segment.SourceReference.Missing) ?? 0;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.VideoProjectMissingMediaCount failed.");
            throw;
        }
    }

    /// <summary>Computes VideoProjectTitle for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetVideoProjectTitleForRender()
    {
        try
        {
            return _videoProject?.Name ?? "Video project";
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.VideoProjectTitle failed.");
            throw;
        }
    }

    /// <summary>Computes VideoProjectFormatSummary for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetVideoProjectFormatSummaryForRender()
    {
        try
        {
            return _videoProject is null
        ? string.Empty
        : string.IsNullOrWhiteSpace(_videoProject.SourceFormatVersion)
            ? _videoProject.SourceFormat
            : $"{_videoProject.SourceFormat} {_videoProject.SourceFormatVersion}";
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.VideoProjectFormatSummary failed.");
            throw;
        }
    }

    /// <summary>Computes VideoProjectCanvasSummary for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetVideoProjectCanvasSummaryForRender()
    {
        try
        {
            return _videoProject is null
        ? string.Empty
        : $"{_videoProject.Width} × {_videoProject.Height} · {_videoProject.FrameRate:0.###} fps";
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.VideoProjectCanvasSummary failed.");
            throw;
        }
    }

    /// <summary>Computes VideoProjectTrackSummary for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetVideoProjectTrackSummaryForRender()
    {
        try
        {
            return _videoProject is null
        ? string.Empty
        : $"{_videoProject.Tracks.Count} track{(_videoProject.Tracks.Count == 1 ? string.Empty : "s")} · {_videoProject.Transitions.Count} transition{(_videoProject.Transitions.Count == 1 ? string.Empty : "s")}";
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.VideoProjectTrackSummary failed.");
            throw;
        }
    }

    /// <summary>Computes VideoProjectMissingSummary for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetVideoProjectMissingSummaryForRender()
    {
        try
        {
            return VideoProjectMissingMediaCount == 0
        ? "All referenced media linked"
        : $"{VideoProjectMissingMediaCount} missing media reference{(VideoProjectMissingMediaCount == 1 ? string.Empty : "s")}";
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.VideoProjectMissingSummary failed.");
            throw;
        }
    }

    /// <summary>Computes SelectedSegment for the render path with component-local diagnostics.</summary>
    /// <returns>The publication media segment produced by the operation.</returns>
    private PublicationMediaSegment? GetSelectedSegmentForRender()
    {
        try
        {
            return _segments.FirstOrDefault(segment => segment.Id == _selectedSegmentId);
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.SelectedSegment failed.");
            throw;
        }
    }

    /// <summary>Computes SelectedTimelineSegments for the render path with component-local diagnostics.</summary>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<PublicationMediaSegment> GetSelectedTimelineSegmentsForRender()
    {
        try
        {
            return _segments
        .Where(segment => _selectedSegmentIds.Contains(segment.Id))
        .ToList();
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.SelectedTimelineSegments failed.");
            throw;
        }
    }

    /// <summary>Computes CanExportSelectedTimelineRanges for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanExportSelectedTimelineRangesForRender()
    {
        try
        {
            return IsVideo
        && !_renderingVideoExport
        && !_renderingTimelineSelectionExport
        && SelectedTimelineSegments.Any(IsPlayableSegment);
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.CanExportSelectedTimelineRanges failed.");
            throw;
        }
    }

    /// <summary>Computes SelectedVideoLayers for the render path with component-local diagnostics.</summary>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<VideoEffectLayer> GetSelectedVideoLayersForRender()
    {
        try
        {
            return SelectedSegment?.VideoLayers ?? [];
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.SelectedVideoLayers failed.");
            throw;
        }
    }

    /// <summary>Computes SelectedVideoLayer for the render path with component-local diagnostics.</summary>
    /// <returns>The video effect layer produced by the operation.</returns>
    private VideoEffectLayer? GetSelectedVideoLayerForRender()
    {
        try
        {
            return SelectedSegment?.VideoLayers.FirstOrDefault(layer => layer.Id == _selectedVideoLayerId);
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.SelectedVideoLayer failed.");
            throw;
        }
    }

    /// <summary>Computes SelectedVideoFilters for the render path with component-local diagnostics.</summary>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<VideoEffectFilter> GetSelectedVideoFiltersForRender()
    {
        try
        {
            return SelectedVideoLayer?.Filters ?? [];
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.SelectedVideoFilters failed.");
            throw;
        }
    }

    /// <summary>Computes SelectedVideoFilter for the render path with component-local diagnostics.</summary>
    /// <returns>The video effect filter produced by the operation.</returns>
    private VideoEffectFilter? GetSelectedVideoFilterForRender()
    {
        try
        {
            return SelectedVideoLayer?.Filters.FirstOrDefault(filter => filter.Id == _selectedVideoFilterId);
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.SelectedVideoFilter failed.");
            throw;
        }
    }

    /// <summary>Computes SelectedCutSections for the render path with component-local diagnostics.</summary>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<MediaTemporalSection> GetSelectedCutSectionsForRender()
    {
        try
        {
            return SelectedSegment?.CutSections ?? [];
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.SelectedCutSections failed.");
            throw;
        }
    }

    /// <summary>Computes SelectedCutSection for the render path with component-local diagnostics.</summary>
    /// <returns>The media temporal section produced by the operation.</returns>
    private MediaTemporalSection? GetSelectedCutSectionForRender()
    {
        try
        {
            return SelectedSegment?.CutSections.FirstOrDefault(section => section.Id == _selectedCutSectionId);
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.SelectedCutSection failed.");
            throw;
        }
    }

    /// <summary>Computes SelectedSegmentIndex for the render path with component-local diagnostics.</summary>
    /// <returns>The int produced by the operation.</returns>
    private int GetSelectedSegmentIndexForRender()
    {
        try
        {
            return _selectedSegmentId is Guid id ? _segments.FindIndex(segment => segment.Id == id) : -1;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.SelectedSegmentIndex failed.");
            throw;
        }
    }

    /// <summary>Computes HasSelectedSegment for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetHasSelectedSegmentForRender()
    {
        try
        {
            return SelectedSegment is not null;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.HasSelectedSegment failed.");
            throw;
        }
    }

    /// <summary>Computes HasSource for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetHasSourceForRender()
    {
        try
        {
            return (_segments.Count > 0 || !string.IsNullOrWhiteSpace(_dataUrl) || HasRetainedRecording || HasVideoProject) && (_duration > 0 || _segments.Count > 0 || HasVideoProject);
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.HasSource failed.");
            throw;
        }
    }

    /// <summary>Computes CanPasteSection for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanPasteSectionForRender()
    {
        try
        {
            return _clipClipboard is not null;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.CanPasteSection failed.");
            throw;
        }
    }

    /// <summary>Computes CanDeleteSection for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanDeleteSectionForRender()
    {
        try
        {
            return _segments.Count > 0 && HasSelectedSegment;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.CanDeleteSection failed.");
            throw;
        }
    }

    /// <summary>Computes CanAddCutLine for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanAddCutLineForRender()
    {
        try
        {
            return HasSource && _segments.Count > 0;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.CanAddCutLine failed.");
            throw;
        }
    }

    /// <summary>Computes CanRemoveCutLine for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanRemoveCutLineForRender()
    {
        try
        {
            return SelectedSegmentIndex > 0 && TimelineEdits.CanMergeBoundary(_segments, SelectedSegmentIndex);
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.CanRemoveCutLine failed.");
            throw;
        }
    }

    /// <summary>Computes CanCutVideoSelection for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanCutVideoSelectionForRender()
    {
        try
        {
            return IsVideo && HasSelectedSegment && (_videoSelectionIsPoint || VideoSelectionEnd - VideoSelectionStart >= .01);
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.CanCutVideoSelection failed.");
            throw;
        }
    }

    /// <summary>Computes CanUseVideoSelectionAsTrim for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanUseVideoSelectionAsTrimForRender()
    {
        try
        {
            return IsVideo && HasSelectedSegment && !_videoSelectionIsPoint && VideoSelectionEnd - VideoSelectionStart >= .01;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.CanUseVideoSelectionAsTrim failed.");
            throw;
        }
    }

    /// <summary>Computes CanCopyVideoSelection for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanCopyVideoSelectionForRender()
    {
        try
        {
            return CanUseVideoSelectionAsTrim;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.CanCopyVideoSelection failed.");
            throw;
        }
    }

    /// <summary>Computes CanSaveCutSection for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanSaveCutSectionForRender()
    {
        try
        {
            return IsVideo && HasSelectedSegment && !_videoSelectionIsPoint && VideoSelectionEnd - VideoSelectionStart >= .01;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.CanSaveCutSection failed.");
            throw;
        }
    }

    /// <summary>Computes CanApplySelectionToLayer for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanApplySelectionToLayerForRender()
    {
        try
        {
            return CanSaveCutSection && SelectedVideoLayer is not null;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.CanApplySelectionToLayer failed.");
            throw;
        }
    }

    /// <summary>Computes SelectedVideoLayerIndex for the render path with component-local diagnostics.</summary>
    /// <returns>The int produced by the operation.</returns>
    private int GetSelectedVideoLayerIndexForRender()
    {
        try
        {
            return SelectedSegment?.VideoLayers.FindIndex(layer => layer.Id == _selectedVideoLayerId) ?? -1;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.SelectedVideoLayerIndex failed.");
            throw;
        }
    }

    /// <summary>Computes CanMoveVideoLayerUp for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanMoveVideoLayerUpForRender()
    {
        try
        {
            return SelectedVideoLayerIndex >= 0 && SelectedVideoLayerIndex < (SelectedSegment?.VideoLayers.Count ?? 0) - 1;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.CanMoveVideoLayerUp failed.");
            throw;
        }
    }

    /// <summary>Computes CanMoveVideoLayerDown for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanMoveVideoLayerDownForRender()
    {
        try
        {
            return SelectedVideoLayerIndex > 0;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.CanMoveVideoLayerDown failed.");
            throw;
        }
    }

    /// <summary>Computes CanDeleteVideoLayer for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanDeleteVideoLayerForRender()
    {
        try
        {
            return SelectedVideoLayer is not null && (SelectedSegment?.VideoLayers.Count ?? 0) > 1;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.CanDeleteVideoLayer failed.");
            throw;
        }
    }

    /// <summary>Computes SelectedVideoFilterIndex for the render path with component-local diagnostics.</summary>
    /// <returns>The int produced by the operation.</returns>
    private int GetSelectedVideoFilterIndexForRender()
    {
        try
        {
            return SelectedVideoLayer?.Filters.FindIndex(filter => filter.Id == _selectedVideoFilterId) ?? -1;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.SelectedVideoFilterIndex failed.");
            throw;
        }
    }

    /// <summary>Computes CanMoveVideoFilterUp for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanMoveVideoFilterUpForRender()
    {
        try
        {
            return SelectedVideoFilterIndex >= 0 && SelectedVideoFilterIndex < (SelectedVideoLayer?.Filters.Count ?? 0) - 1;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.CanMoveVideoFilterUp failed.");
            throw;
        }
    }

    /// <summary>Computes CanMoveVideoFilterDown for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanMoveVideoFilterDownForRender()
    {
        try
        {
            return SelectedVideoFilterIndex > 0;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.CanMoveVideoFilterDown failed.");
            throw;
        }
    }

    /// <summary>Computes SequenceDuration for the render path with component-local diagnostics.</summary>
    /// <returns>The double produced by the operation.</returns>
    private double GetSequenceDurationForRender()
    {
        try
        {
            return Math.Max(.01, TimelineEdits.TimelineLength(_segments, _playbackRate));
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.SequenceDuration failed.");
            throw;
        }
    }

    /// <summary>Computes StoredActiveFramePoints for the render path with component-local diagnostics.</summary>
    /// <returns>The collection produced by the operation.</returns>
    private List<MediaFramePoint> GetStoredActiveFramePointsForRender()
    {
        try
        {
            return _editingMorphRegion ? _morphClipPolygon : _frameClipPolygon;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.StoredActiveFramePoints failed.");
            throw;
        }
    }

    /// <summary>Computes ActiveFramePoints for the render path with component-local diagnostics.</summary>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<MediaFramePoint> GetActiveFramePointsForRender()
    {
        try
        {
            return _frameRegionEditing ? _frameDraft : StoredActiveFramePoints;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.ActiveFramePoints failed.");
            throw;
        }
    }

    /// <summary>Computes SelectedFramePoint for the render path with component-local diagnostics.</summary>
    /// <returns>The media frame point produced by the operation.</returns>
    private MediaFramePoint? GetSelectedFramePointForRender()
    {
        try
        {
            return _selectedFramePointIndex is int index && index >= 0 && index < ActiveFramePoints.Count ? ActiveFramePoints[index] : null;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.SelectedFramePoint failed.");
            throw;
        }
    }

    /// <summary>Computes HasFrameRegion for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetHasFrameRegionForRender()
    {
        try
        {
            return _frameClipPolygon.Count >= 3;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.HasFrameRegion failed.");
            throw;
        }
    }

    /// <summary>Computes HasMorphRegion for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetHasMorphRegionForRender()
    {
        try
        {
            return _morphClipPolygon.Count >= 3;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.HasMorphRegion failed.");
            throw;
        }
    }

    /// <summary>Computes ActiveFrameRegionInverted for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetActiveFrameRegionInvertedForRender()
    {
        try
        {
            return _editingMorphRegion
        ? SelectedVideoLayer?.MorphRegion?.Inverted == true
        : SelectedVideoLayer?.Region?.Inverted == true;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.ActiveFrameRegionInverted failed.");
            throw;
        }
    }

    /// <summary>Computes ActiveFrameRegionName for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetActiveFrameRegionNameForRender()
    {
        try
        {
            return _editingMorphRegion ? "morph target" : "source region";
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.ActiveFrameRegionName failed.");
            throw;
        }
    }

    /// <summary>Computes CanFinishFrameRegion for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanFinishFrameRegionForRender()
    {
        try
        {
            return _frameRegionEditing && _frameDraft.Count >= 3;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.CanFinishFrameRegion failed.");
            throw;
        }
    }

    /// <summary>Computes CanPasteFrameRegion for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanPasteFrameRegionForRender()
    {
        try
        {
            return _frameClipboard.Count >= 3;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.CanPasteFrameRegion failed.");
            throw;
        }
    }

    /// <summary>Computes CanCreateSelectionLayer for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanCreateSelectionLayerForRender()
    {
        try
        {
            return CanSaveCutSection;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.CanCreateSelectionLayer failed.");
            throw;
        }
    }

    /// <summary>Computes CanInsertLayerIntoMainframe for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanInsertLayerIntoMainframeForRender()
    {
        try
        {
            return SelectedVideoLayer is { Kind: VideoEffectLayerKind.Blob3D };
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.CanInsertLayerIntoMainframe failed.");
            throw;
        }
    }

    /// <summary>Computes VideoSelectionStart for the render path with component-local diagnostics.</summary>
    /// <returns>The double produced by the operation.</returns>
    private double GetVideoSelectionStartForRender()
    {
        try
        {
            return _videoSelectionIsPoint ? _videoSelectionPointSeconds : _videoSelectionStartSeconds;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.VideoSelectionStart failed.");
            throw;
        }
    }

    /// <summary>Computes VideoSelectionEnd for the render path with component-local diagnostics.</summary>
    /// <returns>The double produced by the operation.</returns>
    private double GetVideoSelectionEndForRender()
    {
        try
        {
            return _videoSelectionIsPoint ? _videoSelectionPointSeconds : _videoSelectionEndSeconds;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.VideoSelectionEnd failed.");
            throw;
        }
    }

    /// <summary>Computes VideoSelectionLabel for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetVideoSelectionLabelForRender()
    {
        try
        {
            return _videoSelectionIsPoint ? LT("Timestamp") : LT("Range");
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.VideoSelectionLabel failed.");
            throw;
        }
    }

    /// <summary>Computes VideoSelectionReadout for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetVideoSelectionReadoutForRender()
    {
        try
        {
            return _videoSelectionIsPoint
        ? $"/ {Time(_duration)}"
        : $"— {Time(VideoSelectionEnd)} / {Time(_duration)}";
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.VideoSelectionReadout failed.");
            throw;
        }
    }

    /// <summary>Computes VideoSelectionDetailedSummary for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetVideoSelectionDetailedSummaryForRender()
    {
        try
        {
            return _videoSelectionIsPoint
        ? $"Source timestamp {Time(VideoSelectionStart)} · project timestamp {Time(VideoSelectionTimelineStart)}"
        : $"Source {Time(VideoSelectionStart)} — {Time(VideoSelectionEnd)} · project {Time(VideoSelectionTimelineStart)} — {Time(VideoSelectionTimelineEnd)} · {Time(VideoSelectionEnd - VideoSelectionStart)} selected";
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.VideoSelectionDetailedSummary failed.");
            throw;
        }
    }

    /// <summary>Computes VideoOrAudioSelectionTitle for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetVideoOrAudioSelectionTitleForRender()
    {
        try
        {
            return IsVideo && _videoSelectionIsPoint ? LT("Selected timestamp") : LT("Selected range");
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.VideoOrAudioSelectionTitle failed.");
            throw;
        }
    }

    /// <summary>Computes VideoOrAudioSelectionSummary for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetVideoOrAudioSelectionSummaryForRender()
    {
        try
        {
            return IsVideo && _videoSelectionIsPoint
        ? $"{SelectedClipName} · {Time(_videoSelectionPointSeconds)} / {Time(_duration)}"
        : IsVideo
            ? $"{SelectedClipName} · {Time(VideoSelectionStart)} — {Time(VideoSelectionEnd)} / {Time(_duration)}"
            : $"{Time(_trimStart)} — {Time(_trimEnd)} / {Time(_duration)}";
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.VideoOrAudioSelectionSummary failed.");
            throw;
        }
    }

    /// <summary>Computes SelectedClipName for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetSelectedClipNameForRender()
    {
        try
        {
            return SelectedSegment?.Name ?? (IsVideo ? LT("No selected video clip") : LT("No selected audio clip"));
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.SelectedClipName failed.");
            throw;
        }
    }

    /// <summary>Computes SendToConverterMenuText for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetSendToConverterMenuTextForRender()
    {
        try
        {
            return IsVideo ? LT("Send selected clip/range to Media Converter") : LT("Send selected audio clip to Media Converter");
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.SendToConverterMenuText failed.");
            throw;
        }
    }

    /// <summary>Computes SelectedSegmentTimelineStart for the render path with component-local diagnostics.</summary>
    /// <returns>The double produced by the operation.</returns>
    private double GetSelectedSegmentTimelineStartForRender()
    {
        try
        {
            return SelectedSegmentIndex >= 0
        ? TimelineEdits.SegmentTimelineStart(_segments, SelectedSegmentIndex, _playbackRate)
        : 0;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.SelectedSegmentTimelineStart failed.");
            throw;
        }
    }

    /// <summary>Computes SelectedSegmentSourceStart for the render path with component-local diagnostics.</summary>
    /// <returns>The double produced by the operation.</returns>
    private double GetSelectedSegmentSourceStartForRender()
    {
        try
        {
            return SelectedSegment?.TrimStartSeconds ?? 0;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.SelectedSegmentSourceStart failed.");
            throw;
        }
    }

    /// <summary>Computes SelectedSegmentSourceEnd for the render path with component-local diagnostics.</summary>
    /// <returns>The double produced by the operation.</returns>
    private double GetSelectedSegmentSourceEndForRender()
    {
        try
        {
            return SelectedSegment?.EffectiveTrimEndSeconds ?? 0;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.SelectedSegmentSourceEnd failed.");
            throw;
        }
    }

    /// <summary>Computes SelectedSegmentEffectiveRate for the render path with component-local diagnostics.</summary>
    /// <returns>The double produced by the operation.</returns>
    private double GetSelectedSegmentEffectiveRateForRender()
    {
        try
        {
            return Math.Max(.0001, Math.Max(.1, _playbackRate) * Math.Max(.0001, Math.Abs(SelectedSegment?.Speed ?? 1)));
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.SelectedSegmentEffectiveRate failed.");
            throw;
        }
    }

    /// <summary>Computes PendingInsertHasRange for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetPendingInsertHasRangeForRender()
    {
        try
        {
            return _pendingInsertMaximumSourceSeconds - _pendingInsertMinimumSourceSeconds > .01;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.PendingInsertHasRange failed.");
            throw;
        }
    }

    /// <summary>Computes VideoSequenceSelectionStartStyle for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetVideoSequenceSelectionStartStyleForRender()
    {
        try
        {
            return $"left:{Percent(VideoSelectionTimelineStart / Math.Max(.01, SequenceDuration))}";
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.VideoSequenceSelectionStartStyle failed.");
            throw;
        }
    }

    /// <summary>Computes VideoSequenceSelectionEndStyle for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetVideoSequenceSelectionEndStyleForRender()
    {
        try
        {
            return $"left:{Percent(VideoSelectionTimelineEnd / Math.Max(.01, SequenceDuration))}";
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.VideoSequenceSelectionEndStyle failed.");
            throw;
        }
    }

    /// <summary>Computes VideoSequenceSelectionTitle for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetVideoSequenceSelectionTitleForRender()
    {
        try
        {
            return _videoSelectionIsPoint
        ? $"{SelectedClipName}: timestamp {Time(VideoSelectionStart)}"
        : $"{SelectedClipName}: selected {Time(VideoSelectionStart)} — {Time(VideoSelectionEnd)}";
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.VideoSequenceSelectionTitle failed.");
            throw;
        }
    }

    /// <summary>Computes FramePolylinePoints for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetFramePolylinePointsForRender()
    {
        try
        {
            return EditorText.BuildMediaStudioSvgPoints(ActiveFramePoints);
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.FramePolylinePoints failed.");
            throw;
        }
    }

    /// <summary>Computes FramePolygonPoints for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetFramePolygonPointsForRender()
    {
        try
        {
            return FramePolylinePoints;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.FramePolygonPoints failed.");
            throw;
        }
    }

    /// <summary>Computes FrameDimPath for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetFrameDimPathForRender()
    {
        try
        {
            return EditorText.BuildMediaStudioSvgDimPath(ActiveFramePoints);
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.FrameDimPath failed.");
            throw;
        }
    }

    /// <summary>Computes SelectedSectionSummary for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetSelectedSectionSummaryForRender()
    {
        try
        {
            return SelectedSegment is { } segment
        ? _selectedSegmentIds.Count > 1
            ? $"{_selectedSegmentIds.Count} {LT("sections")} · {segment.Name}"
            : $"{segment.Name} · {Time(segment.SourceLengthSeconds / Math.Max(.1, _playbackRate))}"
        : LT("None");
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.SelectedSectionSummary failed.");
            throw;
        }
    }

    /// <summary>Computes ImportMenuText for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetImportMenuTextForRender()
    {
        try
        {
            return IsVideo ? LT("Import video…") : LT("Import audio…");
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.ImportMenuText failed.");
            throw;
        }
    }

    /// <summary>Computes Accept for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetAcceptForRender()
    {
        try
        {
            return IsVideo ? "video/mp4,video/webm,video/ogg,video/quicktime" : "audio/mpeg,audio/wav,audio/ogg,audio/webm,audio/mp4,audio/aac,audio/flac";
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.Accept failed.");
            throw;
        }
    }

    /// <summary>Computes MimeLabel for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetMimeLabelForRender()
    {
        try
        {
            return string.IsNullOrWhiteSpace(_mimeType) ? (IsVideo ? LT("Video") : LT("Audio")) : _mimeType;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.MimeLabel failed.");
            throw;
        }
    }

    /// <summary>Computes SourceStatus for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetSourceStatusForRender()
    {
        try
        {
            return _renderingTimelineSelectionExport
        ? LT("Exporting selected ranges…")
        : _renderingVideoExport
        ? LT("Rendering effects to video…")
        : _recordingStopping
        ? LT("Finalizing recording…")
        : _recording
            ? LT("Recording…")
        : _embedding
            ? $"{LT("Embedding recording")} · {_embeddingProgress:0}%"
            : HasRetainedRecording
                ? $"{LT("Recording ready")} · {FormatBytes(_retainedRecordingSizeBytes)} · {LT("download protected")}"
                : HasSource ? $"{LT("Ready")} · {Time(_trimEnd - _trimStart)} {LT("selected")}" : LT("No source");
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.SourceStatus failed.");
            throw;
        }
    }

    /// <summary>Computes ApplyButtonText for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetApplyButtonTextForRender()
    {
        try
        {
            return _embedding ? $"{LT("Embedding")} {_embeddingProgress:0}%" : _saving ? LT("Applying…") : EditingExisting ? LT("Apply media") : LT("Insert media");
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.ApplyButtonText failed.");
            throw;
        }
    }

    /// <summary>Computes FooterStatus for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetFooterStatusForRender()
    {
        try
        {
            return _error ?? _warning ??
        (HasRetainedRecording
            ? $"{MimeLabel} · {Time(_duration)} · {FormatBytes(_retainedRecordingSizeBytes)} · {LT("available to download")}"
            : HasSource ? $"{MimeLabel} · {Time(_duration)}" : LT("Import or record a source to begin."));
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.FooterStatus failed.");
            throw;
        }
    }

    /// <summary>Computes DisplayWaveform for the render path with component-local diagnostics.</summary>
    /// <returns>The collection produced by the operation.</returns>
    private IEnumerable<double> GetDisplayWaveformForRender()
    {
        try
        {
            return _waveform.Count > 0 ? _waveform : Enumerable.Repeat(.22, 64);
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.DisplayWaveform failed.");
            throw;
        }
    }

    /// <summary>Computes MaxFade for the render path with component-local diagnostics.</summary>
    /// <returns>The double produced by the operation.</returns>
    private double GetMaxFadeForRender()
    {
        try
        {
            return Math.Max(.05, Math.Min(10, (_trimEnd - _trimStart) / Math.Max(.25, _playbackRate) / 2));
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.MaxFade failed.");
            throw;
        }
    }

    /// <summary>Computes RangeSelectorEnd for the render path with component-local diagnostics.</summary>
    /// <returns>The double produced by the operation.</returns>
    private double GetRangeSelectorEndForRender()
    {
        try
        {
            return Math.Max(.01, double.IsFinite(_duration) ? _duration : 0);
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.RangeSelectorEnd failed.");
            throw;
        }
    }

    /// <summary>Computes RangeSelectorMinRange for the render path with component-local diagnostics.</summary>
    /// <returns>The double produced by the operation.</returns>
    private double GetRangeSelectorMinRangeForRender()
    {
        try
        {
            return Math.Min(.05, RangeSelectorEnd);
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.RangeSelectorMinRange failed.");
            throw;
        }
    }

    /// <summary>Computes MajorTick for the render path with component-local diagnostics.</summary>
    /// <returns>The double produced by the operation.</returns>
    private double GetMajorTickForRender()
    {
        try
        {
            return _duration switch { <= 10 => 1, <= 30 => 5, <= 120 => 10, <= 600 => 60, _ => 300 };
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.MajorTick failed.");
            throw;
        }
    }

    /// <summary>Computes MinorTick for the render path with component-local diagnostics.</summary>
    /// <returns>The double produced by the operation.</returns>
    private double GetMinorTickForRender()
    {
        try
        {
            return Math.Max(.1, MajorTick / 5);
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property MediaStudio.MinorTick failed.");
            throw;
        }
    }
}
