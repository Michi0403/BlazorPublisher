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
    /// Performs change video layer kind for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeVideoLayerKind(ChangeEventArgs args)
    {
        try
        {
            if (SelectedVideoLayer is not { } layer || !Enum.TryParse<VideoEffectLayerKind>(Text(args), out var kind)) return;
            layer.Kind = kind;
            if (kind == VideoEffectLayerKind.Selection2D && !layer.HasTemporalRange && CanSaveCutSection)
            {
                layer.HasTemporalRange = true;
                layer.TemporalStartSeconds = VideoSelectionStart;
                layer.TemporalEndSeconds = VideoSelectionEnd;
            }
            if (kind == VideoEffectLayerKind.Blob3D)
            {
                layer.HtmlExportSupport = PublicationHtmlExportSupport.CanvasRuntime;
                layer.HtmlExportNote = "Animated in HTML through the shared canvas runtime. Native OpenSCAD mesh quality requires a pre-rendered asset.";
                if (layer.Region.Points.Count < 3) layer.Region.Points = DefaultBlobRegion();
                if (layer.MorphRegion.Points.Count < 3)
                    layer.MorphRegion.Points = layer.Region.Points.Select((point, index) => new MediaFramePoint
                    {
                        X = Math.Clamp(point.X + (index % 2 == 0 ? -.04 : .04), 0, 1),
                        Y = Math.Clamp(point.Y + (index % 3 == 0 ? .05 : -.03), 0, 1)
                    }).ToList();
                layer.MorphEnabled = true;
            }
            else
            {
                layer.HtmlExportSupport = PublicationHtmlExportSupport.Native;
                layer.HtmlExportNote = "Rendered by the shared Video Studio, Mainframe and HTML runtime.";
            }
            LoadFrameRegionFromSelectedLayer();
            if (kind == VideoEffectLayerKind.Blob3D) RefreshSelectedLayerOpenScad(commit: false);
            CommitSelectedSegment(refreshVideoEffects: true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeVideoLayerKind)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Converts selected layer to blob for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void ConvertSelectedLayerToBlob()
    {
        try
        {
            if (SelectedVideoLayer is not { } layer) return;
            layer.Kind = VideoEffectLayerKind.Blob3D;
            if (layer.Region.Points.Count < 3) layer.Region.Points = DefaultBlobRegion();
            if (layer.MorphRegion.Points.Count < 3)
                layer.MorphRegion.Points = layer.Region.Points.Select((point, index) => new MediaFramePoint
                {
                    X = Math.Clamp(point.X + (index % 2 == 0 ? -.045 : .045), 0, 1),
                    Y = Math.Clamp(point.Y + (index % 3 == 0 ? .04 : -.035), 0, 1)
                }).ToList();
            layer.MorphEnabled = true;
            layer.AnimateMorph = true;
            layer.HtmlExportSupport = PublicationHtmlExportSupport.CanvasRuntime;
            layer.HtmlExportNote = "Animated in HTML through the shared canvas runtime. Native OpenSCAD mesh quality requires a pre-rendered asset.";
            LoadFrameRegionFromSelectedLayer();
            RefreshSelectedLayerOpenScad(commit: false);
            CommitSelectedSegment(refreshVideoEffects: true);
            _warning = $"Converted {layer.Name} to an animated 3D blob. Draw source and morph regions, then reuse it in Mainframe or HTML export.";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ConvertSelectedLayerToBlob)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs default blob region for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    private List<MediaFramePoint> DefaultBlobRegion() {
        try
        {
            return [
        new() { X = .28, Y = .22 }, new() { X = .65, Y = .18 }, new() { X = .82, Y = .43 },
        new() { X = .72, Y = .76 }, new() { X = .35, Y = .82 }, new() { X = .18, Y = .52 }
    ];
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(DefaultBlobRegion)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs toggle video layer morph for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ToggleVideoLayerMorph(ChangeEventArgs args)
    {
        try
        {
            if (SelectedVideoLayer is not { } layer) return;
            layer.MorphEnabled = Bool(args) && layer.MorphRegion.Points.Count >= 3;
            RefreshSelectedLayerOpenScad(commit: false);
            CommitSelectedSegment(refreshVideoEffects: true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ToggleVideoLayerMorph)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs toggle video layer morph animation for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ToggleVideoLayerMorphAnimation(ChangeEventArgs args)
    {
        try
        {
            if (SelectedVideoLayer is not { } layer) return;
            layer.AnimateMorph = Bool(args);
            CommitSelectedSegment(refreshVideoEffects: true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ToggleVideoLayerMorphAnimation)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change video layer morph amount for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeVideoLayerMorphAmount(ChangeEventArgs args) {
        try
        {
            ChangeSelectedVideoLayerNumber(args, (layer, value) => layer.MorphAmount = Math.Clamp(value, 0, 1), refreshScad: false);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeVideoLayerMorphAmount)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change video layer animation speed for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeVideoLayerAnimationSpeed(ChangeEventArgs args) {
        try
        {
            ChangeSelectedVideoLayerNumber(args, (layer, value) => layer.AnimationSpeed = Math.Clamp(value, 0, 8), refreshScad: false);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeVideoLayerAnimationSpeed)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change video layer depth for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeVideoLayerDepth(ChangeEventArgs args) {
        try
        {
            ChangeSelectedVideoLayerNumber(args, (layer, value) => layer.Depth = Math.Clamp(value, .02, .5), refreshScad: true);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeVideoLayerDepth)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change video layer roundness for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeVideoLayerRoundness(ChangeEventArgs args) {
        try
        {
            ChangeSelectedVideoLayerNumber(args, (layer, value) => layer.Roundness = Math.Clamp(value, 0, .5), refreshScad: true);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeVideoLayerRoundness)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change selected video layer number for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="assign">Assign value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="refreshScad">Value indicating whether refresh OpenSCAD should apply to this operation.</param>
    private void ChangeSelectedVideoLayerNumber(ChangeEventArgs args, Action<VideoEffectLayer, double> assign, bool refreshScad)
    {
        try
        {
            if (SelectedVideoLayer is not { } layer || !TryDouble(args, out var value)) return;
            assign(layer, value);
            if (refreshScad) RefreshSelectedLayerOpenScad(commit: false);
            CommitSelectedSegment(refreshVideoEffects: true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeSelectedVideoLayerNumber)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Refreshes selected layer open OpenSCAD for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void RefreshSelectedLayerOpenScad() {
        try
        {
            RefreshSelectedLayerOpenScad(commit: true);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(RefreshSelectedLayerOpenScad)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Refreshes selected layer open OpenSCAD for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="commit">Value indicating whether commit should apply to this operation.</param>
    private void RefreshSelectedLayerOpenScad(bool commit)
    {
        try
        {
            if (SelectedVideoLayer is not { Kind: VideoEffectLayerKind.Blob3D } layer) return;
            CommitFrameRegionToSelectedLayer();
            layer.OpenScadScript = VideoLayerInterchange.CreateOpenScad(layer);
            if (commit) CommitSelectedSegment(refreshVideoEffects: true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(RefreshSelectedLayerOpenScad)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs insert selected layer into mainframe for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task InsertSelectedLayerIntoMainframe()
    {
        try
        {
            if (SelectedVideoLayer is not { Kind: VideoEffectLayerKind.Blob3D } layer || !InsertLayerIntoMainframe.HasDelegate) return;
            CommitFrameRegionToSelectedLayer();
            layer.OpenScadScript = VideoLayerInterchange.CreateOpenScad(layer);
            var request = VideoLayerInterchange.CreateMainframeInsert(layer);
            await InsertLayerIntoMainframe.InvokeAsync(request).ConfigureAwait(true);
            _warning = $"Inserted {layer.Name} into Mainframe as an interactive HTML canvas object with embedded OpenSCAD source.";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(InsertSelectedLayerIntoMainframe)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs toggle video layer temporal range for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ToggleVideoLayerTemporalRange(ChangeEventArgs args)
    {
        try
        {
            if (SelectedVideoLayer is not { } layer) return;
            layer.HasTemporalRange = Bool(args);
            if (layer.HasTemporalRange)
            {
                layer.TemporalStartSeconds = _videoSelectionIsPoint ? _trimStart : VideoSelectionStart;
                layer.TemporalEndSeconds = _videoSelectionIsPoint ? _trimEnd : VideoSelectionEnd;
            }
            CommitSelectedSegment(refreshVideoEffects: true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ToggleVideoLayerTemporalRange)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change video layer temporal start for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeVideoLayerTemporalStart(ChangeEventArgs args)
    {
        try
        {
            if (SelectedVideoLayer is not { } layer || !TryDouble(args, out var value)) return;
            layer.TemporalStartSeconds = Math.Clamp(value, _trimStart, Math.Max(_trimStart, layer.TemporalEndSeconds - .01));
            layer.HasTemporalRange = true;
            CommitSelectedSegment(refreshVideoEffects: true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeVideoLayerTemporalStart)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change video layer temporal end for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeVideoLayerTemporalEnd(ChangeEventArgs args)
    {
        try
        {
            if (SelectedVideoLayer is not { } layer || !TryDouble(args, out var value)) return;
            layer.TemporalEndSeconds = Math.Clamp(value, Math.Min(_trimEnd, layer.TemporalStartSeconds + .01), _trimEnd);
            layer.HasTemporalRange = true;
            CommitSelectedSegment(refreshVideoEffects: true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeVideoLayerTemporalEnd)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs use selection for video layer time for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void UseSelectionForVideoLayerTime() {
        try
        {
            ApplySelectionToSelectedVideoLayer();
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(UseSelectionForVideoLayerTime)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Applies selection to selected video layer for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void ApplySelectionToSelectedVideoLayer()
    {
        try
        {
            if (!CanApplySelectionToLayer || _selectedVideoLayerId is not Guid layerId) return;
            _videoSelectionUserCommitted = true;
            CommitSelectedSegment();
            var layer = SelectedSegment?.VideoLayers.FirstOrDefault(candidate => candidate.Id == layerId);
            if (layer is null) return;
            layer.HasTemporalRange = true;
            layer.TemporalStartSeconds = VideoSelectionStart;
            layer.TemporalEndSeconds = VideoSelectionEnd;
            CommitSelectedSegment(refreshVideoEffects: true);
            _warning = $"Applied {Time(layer.TemporalStartSeconds)} — {Time(layer.TemporalEndSeconds)} to {layer.Name}.";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ApplySelectionToSelectedVideoLayer)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change new video filter kind for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeNewVideoFilterKind(ChangeEventArgs args)
    {
        try
        {
            if (Enum.TryParse<VideoEffectFilterKind>(Text(args), out var kind)) _newVideoFilterKind = kind;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeNewVideoFilterKind)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Adds selected video filter for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void AddSelectedVideoFilter() {
        try
        {
            AddVideoFilter(_newVideoFilterKind);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(AddSelectedVideoFilter)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds chroma key filter for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void AddChromaKeyFilter() {
        try
        {
            AddVideoFilter(VideoEffectFilterKind.ChromaKey);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(AddChromaKeyFilter)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds vignette filter for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void AddVignetteFilter() {
        try
        {
            AddVideoFilter(VideoEffectFilterKind.Vignette);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(AddVignetteFilter)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds blur filter for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void AddBlurFilter() {
        try
        {
            AddVideoFilter(VideoEffectFilterKind.Blur);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(AddBlurFilter)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds grain filter for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void AddGrainFilter() {
        try
        {
            AddVideoFilter(VideoEffectFilterKind.Grain);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(AddGrainFilter)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Adds color adjustment filters for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void AddColorAdjustmentFilters()
    {
        try
        {
            if (SelectedVideoLayer is not { } layer) return;
            foreach (var kind in new[] { VideoEffectFilterKind.Brightness, VideoEffectFilterKind.Contrast, VideoEffectFilterKind.Saturation })
            {
                var existing = layer.Filters.FirstOrDefault(filter => filter.Kind == kind);
                if (existing is null) layer.Filters.Add(TimelineEdits.CreateFilter(kind));
            }
            _selectedVideoFilterId = layer.Filters.LastOrDefault()?.Id;
            CommitSelectedSegment(refreshVideoEffects: true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(AddColorAdjustmentFilters)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Adds video filter for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="kind">Kind value supplied to the media studio operation and used when producing its result.</param>
    private void AddVideoFilter(VideoEffectFilterKind kind)
    {
        try
        {
            if (SelectedVideoLayer is not { } layer) return;
            var filter = TimelineEdits.CreateFilter(kind);
            layer.Filters.Add(filter);
            _selectedVideoFilterId = filter.Id;
            CommitSelectedSegment(refreshVideoEffects: true);
            _warning = $"Added {filter.Name} to {layer.Name}.";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(AddVideoFilter)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs select video filter for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    private void SelectVideoFilter(Guid id)
    {
        try
        {
            if (SelectedVideoLayer?.Filters.Any(filter => filter.Id == id) == true) _selectedVideoFilterId = id;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(SelectVideoFilter)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs toggle video filter for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    private void ToggleVideoFilter(Guid id)
    {
        try
        {
            var filter = SelectedVideoLayer?.Filters.FirstOrDefault(candidate => candidate.Id == id);
            if (filter is null) return;
            filter.Enabled = !filter.Enabled;
            CommitSelectedSegment(refreshVideoEffects: true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ToggleVideoFilter)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change video filter name for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeVideoFilterName(ChangeEventArgs args)
    {
        try
        {
            if (SelectedVideoFilter is not { } filter) return;
            filter.Name = string.IsNullOrWhiteSpace(Text(args)) ? VideoFilterKindName(filter.Kind) : Text(args).Trim();
            CommitSelectedSegment(refreshVideoEffects: true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeVideoFilterName)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change video filter color for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeVideoFilterColor(ChangeEventArgs args)
    {
        try
        {
            if (SelectedVideoFilter is not { } filter) return;
            var value = Text(args).Trim();
            if (value.Length == 7 && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit)) filter.Color = value.ToLowerInvariant();
            CommitSelectedSegment(refreshVideoEffects: true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeVideoFilterColor)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change video filter amount for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeVideoFilterAmount(ChangeEventArgs args) {
        try
        {
            ChangeSelectedVideoFilterNumber(args, (filter, value) => filter.Amount = value);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeVideoFilterAmount)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change video filter secondary amount for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeVideoFilterSecondaryAmount(ChangeEventArgs args) {
        try
        {
            ChangeSelectedVideoFilterNumber(args, (filter, value) => filter.SecondaryAmount = value);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeVideoFilterSecondaryAmount)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change video filter tertiary amount for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeVideoFilterTertiaryAmount(ChangeEventArgs args) {
        try
        {
            ChangeSelectedVideoFilterNumber(args, (filter, value) => filter.TertiaryAmount = value);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeVideoFilterTertiaryAmount)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change video filter residual opacity for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeVideoFilterResidualOpacity(ChangeEventArgs args) {
        try
        {
            ChangeSelectedVideoFilterNumber(args, (filter, value) => filter.ResidualOpacity = value);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeVideoFilterResidualOpacity)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change selected video filter number for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="assign">Assign value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeSelectedVideoFilterNumber(ChangeEventArgs args, Action<VideoEffectFilter, double> assign)
    {
        try
        {
            if (SelectedVideoFilter is not { } filter || !TryDouble(args, out var value)) return;
            assign(filter, value);
            CommitSelectedSegment(refreshVideoEffects: true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeSelectedVideoFilterNumber)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs move video filter up for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void MoveVideoFilterUp() {
        try
        {
            MoveSelectedVideoFilter(1);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(MoveVideoFilterUp)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs move video filter down for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void MoveVideoFilterDown() {
        try
        {
            MoveSelectedVideoFilter(-1);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(MoveVideoFilterDown)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs move selected video filter for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="offset">Offset value supplied to the media studio operation and used when producing its result.</param>
    private void MoveSelectedVideoFilter(int offset)
    {
        try
        {
            if (SelectedVideoLayer is not { } layer) return;
            var index = SelectedVideoFilterIndex;
            var target = index + offset;
            if (index < 0 || target < 0 || target >= layer.Filters.Count) return;
            var filter = layer.Filters[index];
            layer.Filters.RemoveAt(index);
            layer.Filters.Insert(target, filter);
            CommitSelectedSegment(refreshVideoEffects: true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(MoveSelectedVideoFilter)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Removes selected video filter for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    private void RemoveSelectedVideoFilter()
    {
        try
        {
            if (SelectedVideoLayer is not { } layer || _selectedVideoFilterId is not Guid id) return;
            var index = layer.Filters.FindIndex(filter => filter.Id == id);
            if (index < 0) return;
            layer.Filters.RemoveAt(index);
            _selectedVideoFilterId = layer.Filters.Count == 0
                ? null
                : layer.Filters[Math.Clamp(index, 0, layer.Filters.Count - 1)].Id;
            CommitSelectedSegment(refreshVideoEffects: true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(RemoveSelectedVideoFilter)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs video layer kind name for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="kind">Kind value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string VideoLayerKindName(VideoEffectLayerKind kind) {
        try
        {
            return LT(kind switch
    {
        VideoEffectLayerKind.Selection2D => "Selection effect layer",
        VideoEffectLayerKind.Blob3D => "Interactive 3D blob",
        _ => "Base video layer"
    });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(VideoLayerKindName)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs HTML support label for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="support">Support value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string HtmlSupportLabel(PublicationHtmlExportSupport support) {
        try
        {
            return LT(support switch
    {
        PublicationHtmlExportSupport.CanvasRuntime => "HTML · canvas runtime",
        PublicationHtmlExportSupport.RenderBeforeExport => "Render before HTML export",
        _ => "HTML native"
    });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(HtmlSupportLabel)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs HTML support CSS for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="support">Support value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string HtmlSupportCss(PublicationHtmlExportSupport support) {
        try
        {
            return support switch
    {
        PublicationHtmlExportSupport.CanvasRuntime => "canvas",
        PublicationHtmlExportSupport.RenderBeforeExport => "render",
        _ => "native"
    };
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(HtmlSupportCss)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs video layer summary for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="layer">Layer value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string VideoLayerSummary(VideoEffectLayer layer)
    {
        try
        {
            var region = layer.Region.IsFullFrame ? LT("Full frame") : $"{layer.Region.Points.Count} {LT("region points")}";
            var filters = layer.Filters.Count == 1 ? $"1 {LT("filter")}" : $"{layer.Filters.Count} {LT("filters")}";
            var range = layer.HasTemporalRange ? $"{Time(layer.TemporalStartSeconds)}—{Time(layer.TemporalEndSeconds)}" : LT("full clip");
            return $"{VideoLayerKindName(layer.Kind)} · {region} · {filters} · {range}";
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(VideoLayerSummary)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs video blend mode name for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="mode">Mode value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string VideoBlendModeName(VideoEffectBlendMode mode) {
        try
        {
            return LT(mode switch
    {
        VideoEffectBlendMode.Multiply => "Multiply",
        VideoEffectBlendMode.Screen => "Screen",
        VideoEffectBlendMode.Overlay => "Overlay",
        VideoEffectBlendMode.Darken => "Darken",
        VideoEffectBlendMode.Lighten => "Lighten",
        _ => "Normal"
    });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(VideoBlendModeName)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs video filter kind name for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="kind">Kind value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string VideoFilterKindName(VideoEffectFilterKind kind) {
        try
        {
            return LT(kind switch
    {
        VideoEffectFilterKind.HueRotation => "Hue rotation",
        VideoEffectFilterKind.ChromaKey => "Chroma key",
        VideoEffectFilterKind.ColorWash => "Color wash",
        _ => kind.ToString()
    });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(VideoFilterKindName)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs video filter summary for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="filter">Filter value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string VideoFilterSummary(VideoEffectFilter filter) {
        try
        {
            return filter.Kind switch
    {
        VideoEffectFilterKind.ChromaKey => $"{filter.Color} · {LT("similarity")} {filter.Amount:0.##}",
        VideoEffectFilterKind.ColorWash => $"{filter.Color} · {filter.Amount:P0}",
        VideoEffectFilterKind.HueRotation => $"{filter.Amount:0.#}°",
        VideoEffectFilterKind.Blur => $"{filter.Amount:0.#} px",
        _ => $"{filter.Amount:0.##}"
    };
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(VideoFilterSummary)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs video filter amount label for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="kind">Kind value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string VideoFilterAmountLabel(VideoEffectFilterKind kind) {
        try
        {
            return LT(kind switch
    {
        VideoEffectFilterKind.HueRotation => "Rotation",
        VideoEffectFilterKind.Blur => "Radius",
        VideoEffectFilterKind.Grain => "Amount",
        VideoEffectFilterKind.ColorWash => "Opacity",
        _ => "Amount"
    });
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(VideoFilterAmountLabel)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs video filter minimum for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="kind">Kind value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double VideoFilterMinimum(VideoEffectFilterKind kind) {
        try
        {
            return kind == VideoEffectFilterKind.HueRotation ? -360 : 0;
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(VideoFilterMinimum)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs video filter maximum for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="kind">Kind value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double VideoFilterMaximum(VideoEffectFilterKind kind) {
        try
        {
            return kind switch
    {
        VideoEffectFilterKind.Brightness or VideoEffectFilterKind.Contrast or VideoEffectFilterKind.Saturation => 4,
        VideoEffectFilterKind.HueRotation => 360,
        VideoEffectFilterKind.Blur => 64,
        _ => 1
    };
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(VideoFilterMaximum)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs video filter step for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="kind">Kind value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double VideoFilterStep(VideoEffectFilterKind kind) {
        try
        {
            return kind switch
    {
        VideoEffectFilterKind.HueRotation => 1,
        VideoEffectFilterKind.Blur => .1,
        _ => .01
    };
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(VideoFilterStep)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs toggle frame region inverted for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ToggleFrameRegionInverted(ChangeEventArgs args)
    {
        try
        {
            if (SelectedVideoLayer is not { } layer) return;
            var region = _editingMorphRegion ? layer.MorphRegion : layer.Region;
            region ??= new VideoFrameRegion { Name = _editingMorphRegion ? "Morph target" : "Frame region" };
            region.Inverted = Bool(args);
            if (_editingMorphRegion) layer.MorphRegion = region;
            else layer.Region = region;
            CommitSelectedSegment(refreshVideoEffects: true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ToggleFrameRegionInverted)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change selected frame point x for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeSelectedFramePointX(ChangeEventArgs args) {
        try
        {
            ChangeSelectedFramePoint(args, changeX: true);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeSelectedFramePointX)} failed.");
            throw;
        }
    }
    /// <summary>
    /// Performs change selected frame point y for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    private void ChangeSelectedFramePointY(ChangeEventArgs args) {
        try
        {
            ChangeSelectedFramePoint(args, changeX: false);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeSelectedFramePointY)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change selected frame point for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="args">Args value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="changeX">Value indicating whether change x should apply to this operation.</param>
    private void ChangeSelectedFramePoint(ChangeEventArgs args, bool changeX)
    {
        try
        {
            if (!TryDouble(args, out var value) || _selectedFramePointIndex is not int index) return;
            var points = _frameRegionEditing ? _frameDraft : StoredActiveFramePoints;
            if (index < 0 || index >= points.Count) return;
            if (changeX) points[index].X = Math.Clamp(value, 0, 1);
            else points[index].Y = Math.Clamp(value, 0, 1);
            if (!_frameRegionEditing)
            {
                CommitFrameRegionToSelectedLayer();
                RefreshSelectedLayerOpenScad(commit: false);
                CommitSelectedSegment(refreshVideoEffects: true);
            }
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(ChangeSelectedFramePoint)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs video frame point selected for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="pointIndex">Point index value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public Task VideoFramePointSelected(int pointIndex)
    {
        try
        {
            if (!IsVideo || _mouseMode != MediaStudioMouseMode.FrameRegion || pointIndex < 0 || pointIndex >= ActiveFramePoints.Count)
                return Task.CompletedTask;
            _selectedFramePointIndex = pointIndex;
            return InvokeAsync(StateHasChanged);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(VideoFramePointSelected)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs video frame point committed for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="pointIndex">Point index value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="x">X value supplied to the media studio operation and used when producing its result.</param>
    /// <param name="y">Y value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public Task VideoFramePointCommitted(int pointIndex, double x, double y)
    {
        try
        {
            if (!IsVideo || _mouseMode != MediaStudioMouseMode.FrameRegion || pointIndex < 0) return Task.CompletedTask;
            var points = _frameRegionEditing ? _frameDraft : StoredActiveFramePoints;
            if (pointIndex >= points.Count) return Task.CompletedTask;
            _selectedFramePointIndex = pointIndex;
            points[pointIndex].X = Math.Clamp(double.IsFinite(x) ? x : points[pointIndex].X, 0, 1);
            points[pointIndex].Y = Math.Clamp(double.IsFinite(y) ? y : points[pointIndex].Y, 0, 1);
            if (!_frameRegionEditing)
            {
                CommitFrameRegionToSelectedLayer();
                RefreshSelectedLayerOpenScad(commit: false);
                CommitSelectedSegment(refreshVideoEffects: true);
            }
            return InvokeAsync(StateHasChanged);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(VideoFramePointCommitted)} failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs media studio shortcut requested for <see cref="MediaStudio"/>, keeping the operation consistent with the state and invariants of the surrounding media studio workflow.
    /// </summary>
    /// <param name="command">Command value supplied to the media studio operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public Task MediaStudioShortcutRequested(string command)
    {
        try
        {
            switch (command)
            {
                case "copy":
                    if (_mouseMode == MediaStudioMouseMode.FrameRegion && HasFrameRegion) CopyFrameRegion();
                    else CopySelectedSection();
                    break;
                case "paste":
                    if (_mouseMode == MediaStudioMouseMode.FrameRegion && CanPasteFrameRegion) PasteFrameRegion();
                    else PasteSection();
                    break;
                case "delete":
                    if (_mouseMode == MediaStudioMouseMode.FrameRegion)
                    {
                        if (SelectedFramePoint is not null) RemoveSelectedFramePoint();
                        else if (_frameRegionEditing && _frameDraft.Count > 0) RemoveLastFramePoint();
                    }
                    else DeleteSelectedSection();
                    break;
                case "commit":
                    if (_frameRegionEditing) return FinishFrameRegion();
                    break;
                case "cancel":
                    CancelFrameRegion();
                    break;
            }
            return InvokeAsync(StateHasChanged);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, $"Component method {nameof(MediaStudio)}.{nameof(MediaStudioShortcutRequested)} failed.");
            throw;
        }
    }

}
