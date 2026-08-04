using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.MediaStudio.UseCases;

/// <summary>
/// Reusable, non-destructive clip orchestration shared by Video Studio, Audio Studio and live video inputs.
/// Components own pointer state; this service owns deterministic timeline, cut-section and video-layer mutations.
/// </summary>
public sealed class MediaTimelineEditService(
    PublicationMediaData mediaData,
    PublisherStudio.Services.Configuration.IPublisherRuntimePolicyDataService runtimePolicy,
    ILogger<MediaTimelineEditService> logger)
{
    private double MinimumSourceLength => runtimePolicy.MinimumMediaSourceLength;

    public List<PublicationMediaSegment> Normalize(IEnumerable<PublicationMediaSegment>? source, bool video)
    {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.Normalize.");
                    var fallbackMime = video ? "video/webm" : "audio/webm";
                    return (source ?? [])
                        .Where(segment => segment is not null && (segment.IsGap || !string.IsNullOrWhiteSpace(segment.DataUrl) || !string.IsNullOrWhiteSpace(segment.SourceReference?.Uri) || !string.IsNullOrWhiteSpace(segment.SourceReference?.Id) || !string.IsNullOrWhiteSpace(segment.SourceReference?.ReelName)))
                        .Select(segment => new PublicationMediaSegment
                        {
                            Id = segment.Id == Guid.Empty ? Guid.NewGuid() : segment.Id,
                            Name = string.IsNullOrWhiteSpace(segment.Name) ? (video ? "Video clip" : "Audio clip") : segment.Name.Trim(),
                            DataUrl = mediaData.NormalizeDataUrl(segment.DataUrl, fallbackMime),
                            MimeType = mediaData.NormalizeMimeType(segment.MimeType, fallbackMime),
                            PosterDataUrl = segment.PosterDataUrl ?? string.Empty,
                            SourceReference = CloneSourceReference(segment.SourceReference),
                            Enabled = segment.Enabled,
                            IsGap = segment.IsGap,
                            TimelineStartSeconds = Math.Max(0, segment.TimelineStartSeconds),
                            TimelineDurationSeconds = Math.Max(0, segment.TimelineDurationSeconds),
                            SourceRate = Math.Max(0, segment.SourceRate),
                            Speed = Math.Abs(segment.Speed) < .0001 ? 1 : segment.Speed,
                            DurationSeconds = Math.Max(MinimumSourceLength, segment.DurationSeconds),
                            TrimStartSeconds = Math.Max(0, segment.TrimStartSeconds),
                            TrimEndSeconds = Math.Max(0, segment.TrimEndSeconds),
                            HasTemporalSelection = segment.HasTemporalSelection,
                            TemporalSelectionCommitted = segment.TemporalSelectionCommitted,
                            TemporalSelectionIsPoint = segment.TemporalSelectionIsPoint,
                            TemporalSelectionStartSeconds = Math.Max(0, segment.TemporalSelectionStartSeconds),
                            TemporalSelectionEndSeconds = Math.Max(0, segment.TemporalSelectionEndSeconds),
                            CutSections = CloneTemporalSections(segment.CutSections),
                            VideoLayers = video ? CloneVideoLayers(segment.VideoLayers) : [],
                            WaveformSamples = segment.WaveformSamples?.Select(value => Math.Clamp(value, 0, 1)).Take(256).ToList() ?? [],
                            ImportMetadata = new Dictionary<string, string>(segment.ImportMetadata ?? [])
                        })
                        .Select(segment =>
                        {
                            segment.TrimStartSeconds = Math.Clamp(segment.TrimStartSeconds, 0, Math.Max(0, segment.DurationSeconds - MinimumSourceLength));
                            segment.TrimEndSeconds = segment.TrimEndSeconds > segment.TrimStartSeconds
                                ? Math.Clamp(segment.TrimEndSeconds, segment.TrimStartSeconds + MinimumSourceLength, segment.DurationSeconds)
                                : segment.DurationSeconds;
                            NormalizeTemporalSelection(segment);
                            NormalizeCutSections(segment);
                            if (video && !segment.IsGap) segment.VideoLayers = NormalizeVideoLayers(segment.VideoLayers, segment.TrimStartSeconds, segment.EffectiveTrimEndSeconds, segment.Name);
                            else if (segment.IsGap) segment.VideoLayers.Clear();
                            return segment;
                        })
                        .ToList();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.Normalize failed: {exception.Message}");
            throw;
        }
    }

    public VideoProjectDocument CloneVideoProject(VideoProjectDocument? source)
    {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.CloneVideoProject.");
                    if (source is null) return new VideoProjectDocument();
                    return new VideoProjectDocument
                    {
                        FormatVersion = source.FormatVersion,
                        Name = source.Name,
                        SourceFormat = source.SourceFormat,
                        SourceFormatVersion = source.SourceFormatVersion,
                        FrameRate = source.FrameRate,
                        Width = source.Width,
                        Height = source.Height,
                        ActiveTrackId = source.ActiveTrackId,
                        Tracks = (source.Tracks ?? []).Select(track => new MediaTimelineTrack
                        {
                            Id = track.Id,
                            Name = track.Name,
                            Kind = track.Kind,
                            Order = track.Order,
                            Enabled = track.Enabled,
                            Muted = track.Muted,
                            Locked = track.Locked,
                            Segments = (track.Segments ?? []).Select(Clone).ToList()
                        }).ToList(),
                        Transitions = (source.Transitions ?? []).Select(transition => new MediaTimelineTransition
                        {
                            Id = transition.Id,
                            Name = transition.Name,
                            Kind = transition.Kind,
                            TrackId = transition.TrackId,
                            FromSegmentId = transition.FromSegmentId,
                            ToSegmentId = transition.ToSegmentId,
                            TimelineStartSeconds = transition.TimelineStartSeconds,
                            DurationSeconds = transition.DurationSeconds,
                            Metadata = new Dictionary<string, string>(transition.Metadata ?? [])
                        }).ToList(),
                        Markers = (source.Markers ?? []).Select(marker => new MediaProjectMarker
                        {
                            Id = marker.Id,
                            Name = marker.Name,
                            Color = marker.Color,
                            StartSeconds = marker.StartSeconds,
                            DurationSeconds = marker.DurationSeconds,
                            Comment = marker.Comment
                        }).ToList(),
                        Metadata = new Dictionary<string, string>(source.Metadata ?? [])
                    };
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.CloneVideoProject failed: {exception.Message}");
            throw;
        }
    }

    public List<PublicationMediaSegment> CreateTrackProjection(VideoProjectDocument project, Guid trackId)
    {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.CreateTrackProjection.");
                    ArgumentNullException.ThrowIfNull(project);
                    var track = (project.Tracks ?? []).FirstOrDefault(candidate => candidate.Id == trackId)
                        ?? project.Tracks?.FirstOrDefault();
                    if (track is null) return [];

                    var result = new List<PublicationMediaSegment>();
                    var cursor = 0d;
                    foreach (var source in (track.Segments ?? []).OrderBy(segment => segment.TimelineStartSeconds).ThenBy(segment => segment.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        var start = Math.Max(0, source.TimelineStartSeconds);
                        if (start > cursor + .001)
                        {
                            var gapLength = start - cursor;
                            result.Add(new PublicationMediaSegment
                            {
                                Name = "Gap",
                                IsGap = true,
                                TimelineStartSeconds = cursor,
                                TimelineDurationSeconds = gapLength,
                                DurationSeconds = gapLength,
                                TrimEndSeconds = gapLength,
                                SourceRate = project.FrameRate,
                                ImportMetadata = { ["projection_gap"] = "true" }
                            });
                            cursor = start;
                        }

                        var clone = Clone(source);
                        if (start < cursor - .001)
                        {
                            clone.ImportMetadata["original_timeline_start"] = start.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                            clone.ImportMetadata["overlap_flattened_for_editor"] = "true";
                        }
                        clone.TimelineStartSeconds = cursor;
                        clone.TimelineDurationSeconds = clone.TimelineDurationSeconds > 0
                            ? clone.TimelineDurationSeconds
                            : clone.SourceLengthSeconds / Math.Max(.0001, Math.Abs(clone.Speed));
                        result.Add(clone);
                        cursor += Math.Max(MinimumSourceLength, clone.TimelineDurationSeconds);
                    }
                    return result;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.CreateTrackProjection failed: {exception.Message}");
            throw;
        }
    }

    public void ReplaceTrackProjection(VideoProjectDocument project, Guid trackId, IEnumerable<PublicationMediaSegment> projection)
    {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.ReplaceTrackProjection.");
                    ArgumentNullException.ThrowIfNull(project);
                    var track = (project.Tracks ?? []).FirstOrDefault(candidate => candidate.Id == trackId);
                    if (track is null) return;
                    var cursor = 0d;
                    track.Segments = (projection ?? []).Select(Clone).Select(segment =>
                    {
                        segment.TimelineStartSeconds = cursor;
                        segment.TimelineDurationSeconds = segment.TimelineDurationSeconds > 0
                            ? segment.TimelineDurationSeconds
                            : segment.SourceLengthSeconds / Math.Max(.0001, Math.Abs(segment.Speed));
                        cursor += Math.Max(MinimumSourceLength, segment.TimelineDurationSeconds);
                        return segment;
                    }).ToList();
                    project.ActiveTrackId = track.Id;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.ReplaceTrackProjection failed: {exception.Message}");
            throw;
        }
    }

    public VideoEffectLayer CreateDefaultVideoLayer(string? name = null) {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.CreateDefaultVideoLayer.");
            return new()
    {
        Name = string.IsNullOrWhiteSpace(name) ? "Base video" : $"{name.Trim()} · base",
        Kind = VideoEffectLayerKind.BaseVideo,
        Region = new VideoFrameRegion { Name = "Full frame" },
        MorphRegion = new VideoFrameRegion { Name = "Morph target" },
        HtmlExportSupport = PublicationHtmlExportSupport.Native,
        HtmlExportNote = "Rendered by the shared Video Studio, Mainframe and HTML runtime."
    };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.CreateDefaultVideoLayer failed: {exception.Message}");
            throw;
        }
    }

    public List<VideoEffectLayer> NormalizeVideoLayers(
        IEnumerable<VideoEffectLayer>? source,
        double minimumSeconds,
        double maximumSeconds,
        string? defaultName = null)
    {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.NormalizeVideoLayers.");
                    var minimum = Math.Max(0, double.IsFinite(minimumSeconds) ? minimumSeconds : 0);
                    var maximum = Math.Max(minimum, double.IsFinite(maximumSeconds) ? maximumSeconds : minimum);
                    var normalized = (source ?? [])
                        .Where(layer => layer is not null)
                        .Take(64)
                        .Select((layer, index) => NormalizeVideoLayer(CloneVideoLayer(layer), minimum, maximum, index))
                        .ToList();
                    if (normalized.Count == 0) normalized.Add(CreateDefaultVideoLayer(defaultName));
                    return normalized;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.NormalizeVideoLayers failed: {exception.Message}");
            throw;
        }
    }

    public double TimelineLength(IReadOnlyList<PublicationMediaSegment> segments, double playbackRate)
        {
            try
            {
                logger.LogTrace($"Entering MediaTimelineEditService.TimelineLength.");
                return segments.Sum(segment => SegmentTimelineLength(segment, playbackRate));
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"MediaTimelineEditService.TimelineLength failed: {exception.Message}");
                throw;
            }
        }

    public int SegmentIndexAt(IReadOnlyList<PublicationMediaSegment> segments, double playbackRate, double timelineSeconds)
    {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.SegmentIndexAt.");
                    if (segments.Count == 0) return -1;
                    var cursor = 0d;
                    var rate = Math.Max(.1, playbackRate);
                    for (var index = 0; index < segments.Count; index++)
                    {
                        var length = SegmentTimelineLength(segments[index], rate);
                        if (timelineSeconds <= cursor + length || index == segments.Count - 1) return index;
                        cursor += length;
                    }
                    return segments.Count - 1;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.SegmentIndexAt failed: {exception.Message}");
            throw;
        }
    }

    public double SegmentTimelineStart(IReadOnlyList<PublicationMediaSegment> segments, int index, double playbackRate)
        {
            try
            {
                logger.LogTrace($"Entering MediaTimelineEditService.SegmentTimelineStart.");
                return segments.Take(Math.Clamp(index, 0, segments.Count)).Sum(segment => SegmentTimelineLength(segment, playbackRate));
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"MediaTimelineEditService.SegmentTimelineStart failed: {exception.Message}");
                throw;
            }
        }

    public double SourcePositionAt(IReadOnlyList<PublicationMediaSegment> segments, int index, double playbackRate, double timelineSeconds)
    {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.SourcePositionAt.");
                    if (index < 0 || index >= segments.Count) return 0;
                    var start = SegmentTimelineStart(segments, index, playbackRate);
                    var segment = segments[index];
                    var localRate = Math.Max(.0001, Math.Abs(segment.Speed));
                    return Math.Clamp(segment.TrimStartSeconds + Math.Max(0, timelineSeconds - start) * Math.Max(.1, playbackRate) * localRate,
                        segment.TrimStartSeconds, segment.EffectiveTrimEndSeconds);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.SourcePositionAt failed: {exception.Message}");
            throw;
        }
    }

    public Guid? SplitAt(List<PublicationMediaSegment> segments, double playbackRate, double timelineSeconds)
    {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.SplitAt.");
                    var index = SegmentIndexAt(segments, playbackRate, timelineSeconds);
                    if (index < 0) return null;
                    var segment = segments[index];
                    var sourcePosition = SourcePositionAt(segments, index, playbackRate, timelineSeconds);
                    if (sourcePosition <= segment.TrimStartSeconds + MinimumSourceLength || sourcePosition >= segment.EffectiveTrimEndSeconds - MinimumSourceLength)
                        return null;

                    var right = Clone(segment);
                    right.Id = Guid.NewGuid();
                    right.Name = Suffix(segment.Name, "right");
                    right.TrimStartSeconds = sourcePosition;
                    segment.TrimEndSeconds = sourcePosition;
                    segment.Name = Suffix(segment.Name, "left");
                    NormalizeTemporalSelection(segment);
                    NormalizeTemporalSelection(right);
                    NormalizeCutSections(segment);
                    NormalizeCutSections(right);
                    segment.VideoLayers = NormalizeVideoLayers(segment.VideoLayers, segment.TrimStartSeconds, segment.EffectiveTrimEndSeconds, segment.Name);
                    right.VideoLayers = NormalizeVideoLayers(right.VideoLayers, right.TrimStartSeconds, right.EffectiveTrimEndSeconds, right.Name);
                    segments.Insert(index + 1, right);
                    return right.Id;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.SplitAt failed: {exception.Message}");
            throw;
        }
    }

    public Guid InsertAt(
        List<PublicationMediaSegment> segments,
        double playbackRate,
        double timelineSeconds,
        PublicationMediaSegment inserted)
    {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.InsertAt.");
                    ArgumentNullException.ThrowIfNull(inserted);
                    inserted.Id = inserted.Id == Guid.Empty ? Guid.NewGuid() : inserted.Id;

                    if (segments.Count == 0)
                    {
                        segments.Add(inserted);
                        return inserted.Id;
                    }

                    var timelineLength = TimelineLength(segments, playbackRate);
                    var position = double.IsFinite(timelineSeconds)
                        ? Math.Clamp(timelineSeconds, 0, Math.Max(0, timelineLength))
                        : timelineLength;
                    var tolerance = Math.Min(MinimumSourceLength, Math.Max(.001, timelineLength / 10_000));

                    if (position <= tolerance)
                    {
                        segments.Insert(0, inserted);
                        return inserted.Id;
                    }

                    if (position >= timelineLength - tolerance)
                    {
                        segments.Add(inserted);
                        return inserted.Id;
                    }

                    var index = SegmentIndexAt(segments, playbackRate, position);
                    if (index < 0)
                    {
                        segments.Add(inserted);
                        return inserted.Id;
                    }

                    var segmentStart = SegmentTimelineStart(segments, index, playbackRate);
                    var segmentLength = SegmentTimelineLength(segments[index], playbackRate);
                    if (position <= segmentStart + tolerance)
                    {
                        segments.Insert(index, inserted);
                        return inserted.Id;
                    }

                    if (position >= segmentStart + segmentLength - tolerance)
                    {
                        segments.Insert(index + 1, inserted);
                        return inserted.Id;
                    }

                    var rightId = SplitAt(segments, playbackRate, position);
                    var rightIndex = rightId is Guid id ? segments.FindIndex(segment => segment.Id == id) : -1;
                    segments.Insert(rightIndex >= 0 ? rightIndex : index + 1, inserted);
                    return inserted.Id;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.InsertAt failed: {exception.Message}");
            throw;
        }
    }

    public bool CanMergeBoundary(IReadOnlyList<PublicationMediaSegment> segments, int rightIndex)
    {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.CanMergeBoundary.");
                    if (rightIndex <= 0 || rightIndex >= segments.Count) return false;
                    var left = segments[rightIndex - 1];
                    var right = segments[rightIndex];
                    return !left.IsGap && !right.IsGap
                        && string.Equals(left.DataUrl, right.DataUrl, StringComparison.Ordinal)
                        && string.Equals(left.SourceReference?.Uri, right.SourceReference?.Uri, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(left.MimeType, right.MimeType, StringComparison.OrdinalIgnoreCase)
                        && Math.Abs(left.EffectiveTrimEndSeconds - right.TrimStartSeconds) <= .02;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.CanMergeBoundary failed: {exception.Message}");
            throw;
        }
    }

    public bool MergeBoundary(List<PublicationMediaSegment> segments, int rightIndex)
    {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.MergeBoundary.");
                    if (!CanMergeBoundary(segments, rightIndex)) return false;
                    var left = segments[rightIndex - 1];
                    var right = segments[rightIndex];
                    left.TrimEndSeconds = right.EffectiveTrimEndSeconds;
                    left.Name = MergeName(left.Name, right.Name);
                    left.CutSections.AddRange(CloneTemporalSections(right.CutSections));
                    NormalizeTemporalSelection(left);
                    NormalizeCutSections(left);
                    left.VideoLayers = NormalizeVideoLayers(left.VideoLayers, left.TrimStartSeconds, left.EffectiveTrimEndSeconds, left.Name);
                    segments.RemoveAt(rightIndex);
                    return true;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.MergeBoundary failed: {exception.Message}");
            throw;
        }
    }

    public PublicationMediaSegment Clone(PublicationMediaSegment segment) {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.Clone.");
            return new()
    {
        Id = segment.Id,
        Name = segment.Name,
        DataUrl = segment.DataUrl,
        MimeType = segment.MimeType,
        PosterDataUrl = segment.PosterDataUrl,
        SourceReference = CloneSourceReference(segment.SourceReference),
        Enabled = segment.Enabled,
        IsGap = segment.IsGap,
        TimelineStartSeconds = segment.TimelineStartSeconds,
        TimelineDurationSeconds = segment.TimelineDurationSeconds,
        SourceRate = segment.SourceRate,
        Speed = segment.Speed,
        DurationSeconds = segment.DurationSeconds,
        TrimStartSeconds = segment.TrimStartSeconds,
        TrimEndSeconds = segment.TrimEndSeconds,
        HasTemporalSelection = segment.HasTemporalSelection,
        TemporalSelectionCommitted = segment.TemporalSelectionCommitted,
        TemporalSelectionIsPoint = segment.TemporalSelectionIsPoint,
        TemporalSelectionStartSeconds = segment.TemporalSelectionStartSeconds,
        TemporalSelectionEndSeconds = segment.TemporalSelectionEndSeconds,
        CutSections = CloneTemporalSections(segment.CutSections),
        VideoLayers = CloneVideoLayers(segment.VideoLayers),
        WaveformSamples = [.. segment.WaveformSamples],
        ImportMetadata = new Dictionary<string, string>(segment.ImportMetadata ?? [])
    };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.Clone failed: {exception.Message}");
            throw;
        }
    }

    private double SegmentTimelineLength(PublicationMediaSegment segment, double playbackRate)
    {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.SegmentTimelineLength.");
                    var sourceLength = segment.TimelineDurationSeconds > 0
                        ? segment.TimelineDurationSeconds
                        : segment.SourceLengthSeconds / Math.Max(.0001, Math.Abs(segment.Speed));
                    return Math.Max(MinimumSourceLength, sourceLength) / Math.Max(.1, playbackRate);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.SegmentTimelineLength failed: {exception.Message}");
            throw;
        }
    }

    private MediaSourceReference CloneSourceReference(MediaSourceReference? source) {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.CloneSourceReference.");
            return new()
    {
        Id = source?.Id ?? string.Empty,
        Uri = source?.Uri ?? string.Empty,
        OriginalPath = source?.OriginalPath ?? string.Empty,
        MimeType = source?.MimeType ?? string.Empty,
        ReelName = source?.ReelName ?? string.Empty,
        Missing = source?.Missing ?? false,
        Metadata = new Dictionary<string, string>(source?.Metadata ?? [])
    };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.CloneSourceReference failed: {exception.Message}");
            throw;
        }
    }

    public PublicationMediaSegment Duplicate(PublicationMediaSegment segment)
    {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.Duplicate.");
                    var clone = Clone(segment);
                    clone.Id = Guid.NewGuid();
                    clone.Name = Suffix(segment.Name, "copy");
                    foreach (var section in clone.CutSections) section.Id = Guid.NewGuid();
                    foreach (var layer in clone.VideoLayers)
                    {
                        layer.Id = Guid.NewGuid();
                        layer.Region.Id = Guid.NewGuid();
                        layer.MorphRegion.Id = Guid.NewGuid();
                        foreach (var filter in layer.Filters) filter.Id = Guid.NewGuid();
                    }
                    return clone;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.Duplicate failed: {exception.Message}");
            throw;
        }
    }

    public VideoEffectLayer CloneVideoLayer(VideoEffectLayer layer) {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.CloneVideoLayer.");
            return new()
    {
        Id = layer.Id,
        Name = layer.Name,
        Visible = layer.Visible,
        Locked = layer.Locked,
        Kind = layer.Kind,
        Opacity = layer.Opacity,
        BlendMode = layer.BlendMode,
        HasTemporalRange = layer.HasTemporalRange,
        TemporalStartSeconds = layer.TemporalStartSeconds,
        TemporalEndSeconds = layer.TemporalEndSeconds,
        Region = CloneVideoRegion(layer.Region),
        MorphRegion = CloneVideoRegion(layer.MorphRegion),
        MorphEnabled = layer.MorphEnabled,
        AnimateMorph = layer.AnimateMorph,
        MorphAmount = layer.MorphAmount,
        AnimationSpeed = layer.AnimationSpeed,
        Depth = layer.Depth,
        Roundness = layer.Roundness,
        OpenScadScript = layer.OpenScadScript,
        HtmlExportSupport = layer.HtmlExportSupport,
        HtmlExportNote = layer.HtmlExportNote,
        Filters = CloneVideoFilters(layer.Filters)
    };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.CloneVideoLayer failed: {exception.Message}");
            throw;
        }
    }

    public VideoEffectFilter CreateFilter(VideoEffectFilterKind kind) {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.CreateFilter.");
            return kind switch
    {
        VideoEffectFilterKind.Brightness => new() { Kind = kind, Name = "Brightness", Amount = 1 },
        VideoEffectFilterKind.Contrast => new() { Kind = kind, Name = "Contrast", Amount = 1 },
        VideoEffectFilterKind.Saturation => new() { Kind = kind, Name = "Saturation", Amount = 1 },
        VideoEffectFilterKind.HueRotation => new() { Kind = kind, Name = "Hue rotation", Amount = 0 },
        VideoEffectFilterKind.Blur => new() { Kind = kind, Name = "Blur", Amount = 0 },
        VideoEffectFilterKind.Grayscale => new() { Kind = kind, Name = "Grayscale", Amount = 1 },
        VideoEffectFilterKind.Sepia => new() { Kind = kind, Name = "Sepia", Amount = 1 },
        VideoEffectFilterKind.Invert => new() { Kind = kind, Name = "Invert", Amount = 1 },
        VideoEffectFilterKind.ChromaKey => new() { Kind = kind, Name = "Chroma key", Amount = .35, SecondaryAmount = .12, TertiaryAmount = .3, ResidualOpacity = 0, Color = "#00ff00" },
        VideoEffectFilterKind.Vignette => new() { Kind = kind, Name = "Vignette", Amount = .45, SecondaryAmount = .55 },
        VideoEffectFilterKind.Grain => new() { Kind = kind, Name = "Film grain", Amount = .12, SecondaryAmount = 17 },
        VideoEffectFilterKind.ColorWash => new() { Kind = kind, Name = "Color wash", Amount = .25, Color = "#3b82f6" },
        _ => new() { Kind = kind, Name = kind.ToString(), Amount = 1 }
    };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.CreateFilter failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Projects the Mainframe live-input adjustment controls into one canonical layer while preserving
    /// any additional user-authored layers. Streaming preview and program output therefore use the same
    /// layer/filter renderer as Video Studio instead of a separate chroma/filter implementation.
    /// </summary>
    public void SynchronizeLiveSourceLayer(LiveSourceElement source)
    {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.SynchronizeLiveSourceLayer.");
                    ArgumentNullException.ThrowIfNull(source);
                    source.VideoLayers ??= [];
                    if (!source.IsVisual)
                    {
                        source.VideoLayers.Clear();
                        return;
                    }

                    source.VideoLayers = source.VideoLayers.Count > 0
                        ? NormalizeVideoLayers(source.VideoLayers, 0, 24 * 60 * 60, source.Name)
                        : [];
                    var controls = source.VideoLayers.FirstOrDefault(layer =>
                        string.Equals(layer.Name, "Live input controls", StringComparison.OrdinalIgnoreCase));
                    if (controls is null)
                    {
                        controls = CreateDefaultVideoLayer();
                        controls.Name = "Live input controls";
                        source.VideoLayers.Insert(0, controls);
                    }

                    controls.Visible = true;
                    controls.Locked = false;
                    controls.Opacity = 1;
                    controls.BlendMode = VideoEffectBlendMode.Normal;
                    controls.HasTemporalRange = false;
                    var regionId = controls.Region?.Id is Guid existingRegionId && existingRegionId != Guid.Empty
                        ? existingRegionId
                        : Guid.NewGuid();
                    controls.Region = new VideoFrameRegion { Id = regionId, Name = "Full frame" };

                    UpsertLiveFilter(controls, VideoEffectFilterKind.Brightness, source.Brightness, 1);
                    UpsertLiveFilter(controls, VideoEffectFilterKind.Contrast, source.Contrast, 1);
                    UpsertLiveFilter(controls, VideoEffectFilterKind.Saturation, source.Saturation, 1);
                    UpsertLiveFilter(controls, VideoEffectFilterKind.HueRotation, source.HueRotation, 0);
                    UpsertLiveFilter(controls, VideoEffectFilterKind.Blur, source.Blur, 0);

                    var chroma = controls.Filters.FirstOrDefault(filter => filter.Kind == VideoEffectFilterKind.ChromaKey);
                    if (!source.ChromaKeyEnabled)
                    {
                        if (chroma is not null) controls.Filters.Remove(chroma);
                    }
                    else
                    {
                        chroma ??= CreateFilter(VideoEffectFilterKind.ChromaKey);
                        if (!controls.Filters.Contains(chroma)) controls.Filters.Add(chroma);
                        chroma.Enabled = true;
                        chroma.Color = source.ChromaKeyColor;
                        chroma.Amount = source.ChromaSimilarity;
                        chroma.SecondaryAmount = source.ChromaSmoothness;
                        chroma.TertiaryAmount = source.ChromaSpill;
                        chroma.ResidualOpacity = source.ChromaResidualOpacity;
                        NormalizeFilter(chroma);
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.SynchronizeLiveSourceLayer failed: {exception.Message}");
            throw;
        }
    }

    private void UpsertLiveFilter(VideoEffectLayer layer, VideoEffectFilterKind kind, double value, double neutral)
    {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.UpsertLiveFilter.");
                    var filter = layer.Filters.FirstOrDefault(candidate => candidate.Kind == kind);
                    if (Math.Abs(value - neutral) <= .0001)
                    {
                        if (filter is not null) layer.Filters.Remove(filter);
                        return;
                    }

                    filter ??= CreateFilter(kind);
                    if (!layer.Filters.Contains(filter)) layer.Filters.Add(filter);
                    filter.Enabled = true;
                    filter.Amount = value;
                    NormalizeFilter(filter);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.UpsertLiveFilter failed: {exception.Message}");
            throw;
        }
    }

    private List<MediaTemporalSection> CloneTemporalSections(IEnumerable<MediaTemporalSection>? sections) {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.CloneTemporalSections.");
            return (sections ?? [])
        .Where(section => section is not null)
        .Take(128)
        .Select(section => new MediaTemporalSection
        {
            Id = section.Id == Guid.Empty ? Guid.NewGuid() : section.Id,
            Name = string.IsNullOrWhiteSpace(section.Name) ? "Cut section" : section.Name.Trim(),
            Enabled = section.Enabled,
            StartSeconds = section.StartSeconds,
            EndSeconds = section.EndSeconds
        })
        .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.CloneTemporalSections failed: {exception.Message}");
            throw;
        }
    }

    private List<VideoEffectLayer> CloneVideoLayers(IEnumerable<VideoEffectLayer>? layers) {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.CloneVideoLayers.");
            return (layers ?? [])
        .Where(layer => layer is not null)
        .Take(64)
        .Select(CloneVideoLayer)
        .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.CloneVideoLayers failed: {exception.Message}");
            throw;
        }
    }

    private VideoFrameRegion CloneVideoRegion(VideoFrameRegion? region) {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.CloneVideoRegion.");
            return new()
    {
        Id = region?.Id is Guid id && id != Guid.Empty ? id : Guid.NewGuid(),
        Name = string.IsNullOrWhiteSpace(region?.Name) ? "Full frame" : region.Name.Trim(),
        Inverted = region?.Inverted == true,
        Points = (region?.Points ?? [])
            .Where(point => point is not null)
            .Take(256)
            .Select(point => new MediaFramePoint { X = point.X, Y = point.Y })
            .ToList()
    };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.CloneVideoRegion failed: {exception.Message}");
            throw;
        }
    }

    private List<VideoEffectFilter> CloneVideoFilters(IEnumerable<VideoEffectFilter>? filters) {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.CloneVideoFilters.");
            return (filters ?? [])
        .Where(filter => filter is not null)
        .Take(64)
        .Select(filter => new VideoEffectFilter
        {
            Id = filter.Id == Guid.Empty ? Guid.NewGuid() : filter.Id,
            Name = string.IsNullOrWhiteSpace(filter.Name) ? filter.Kind.ToString() : filter.Name.Trim(),
            Kind = filter.Kind,
            Enabled = filter.Enabled,
            Amount = filter.Amount,
            SecondaryAmount = filter.SecondaryAmount,
            TertiaryAmount = filter.TertiaryAmount,
            ResidualOpacity = filter.ResidualOpacity,
            Color = string.IsNullOrWhiteSpace(filter.Color) ? "#00ff00" : filter.Color,
            HtmlExportSupport = filter.HtmlExportSupport,
            HtmlExportNote = filter.HtmlExportNote
        })
        .ToList();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.CloneVideoFilters failed: {exception.Message}");
            throw;
        }
    }

    private VideoEffectLayer NormalizeVideoLayer(VideoEffectLayer layer, double minimum, double maximum, int index)
    {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.NormalizeVideoLayer.");
                    layer.Id = layer.Id == Guid.Empty ? Guid.NewGuid() : layer.Id;
                    layer.Name = string.IsNullOrWhiteSpace(layer.Name) ? $"Video layer {index + 1}" : layer.Name.Trim();
                    layer.Opacity = Math.Clamp(double.IsFinite(layer.Opacity) ? layer.Opacity : 1, 0, 1);
                    if (!Enum.IsDefined(layer.BlendMode)) layer.BlendMode = VideoEffectBlendMode.Normal;
                    if (!Enum.IsDefined(layer.Kind)) layer.Kind = VideoEffectLayerKind.BaseVideo;
                    layer.Region ??= new VideoFrameRegion();
                    layer.Region.Id = layer.Region.Id == Guid.Empty ? Guid.NewGuid() : layer.Region.Id;
                    layer.Region.Name = string.IsNullOrWhiteSpace(layer.Region.Name) ? "Full frame" : layer.Region.Name.Trim();
                    layer.Region.Points = layer.Region.Points
                        .Where(point => point is not null)
                        .Take(256)
                        .Select(point => new MediaFramePoint
                        {
                            X = Math.Clamp(double.IsFinite(point.X) ? point.X : 0, 0, 1),
                            Y = Math.Clamp(double.IsFinite(point.Y) ? point.Y : 0, 0, 1)
                        })
                        .ToList();
                    if (layer.Region.Points.Count is > 0 and < 3) layer.Region.Points.Clear();
                    layer.MorphRegion ??= new VideoFrameRegion { Name = "Morph target" };
                    layer.MorphRegion.Id = layer.MorphRegion.Id == Guid.Empty ? Guid.NewGuid() : layer.MorphRegion.Id;
                    layer.MorphRegion.Name = string.IsNullOrWhiteSpace(layer.MorphRegion.Name) ? "Morph target" : layer.MorphRegion.Name.Trim();
                    layer.MorphRegion.Points = layer.MorphRegion.Points
                        .Where(point => point is not null)
                        .Take(256)
                        .Select(point => new MediaFramePoint
                        {
                            X = Math.Clamp(double.IsFinite(point.X) ? point.X : 0, 0, 1),
                            Y = Math.Clamp(double.IsFinite(point.Y) ? point.Y : 0, 0, 1)
                        })
                        .ToList();
                    if (layer.MorphRegion.Points.Count is > 0 and < 3) layer.MorphRegion.Points.Clear();
                    layer.MorphEnabled = layer.MorphEnabled && layer.MorphRegion.Points.Count >= 3;
                    layer.MorphAmount = Math.Clamp(double.IsFinite(layer.MorphAmount) ? layer.MorphAmount : 0, 0, 1);
                    layer.AnimationSpeed = Math.Clamp(double.IsFinite(layer.AnimationSpeed) ? layer.AnimationSpeed : 1, 0, 8);
                    layer.Depth = Math.Clamp(double.IsFinite(layer.Depth) ? layer.Depth : .18, .02, .5);
                    layer.Roundness = Math.Clamp(double.IsFinite(layer.Roundness) ? layer.Roundness : .12, 0, .5);
                    layer.HtmlExportSupport = layer.Kind == VideoEffectLayerKind.Blob3D
                        ? PublicationHtmlExportSupport.CanvasRuntime
                        : PublicationHtmlExportSupport.Native;
                    layer.HtmlExportNote = layer.Kind == VideoEffectLayerKind.Blob3D
                        ? "Animated browser canvas fallback is available. Native OpenSCAD mesh output must be rendered before HTML export."
                        : "Rendered by the shared Video Studio/Mainframe/HTML runtime.";

                    if (layer.HasTemporalRange)
                    {
                        var start = Math.Clamp(double.IsFinite(layer.TemporalStartSeconds) ? layer.TemporalStartSeconds : minimum, minimum, maximum);
                        var end = Math.Clamp(double.IsFinite(layer.TemporalEndSeconds) ? layer.TemporalEndSeconds : maximum, start, maximum);
                        if (end - start < MinimumSourceLength) layer.HasTemporalRange = false;
                        else
                        {
                            layer.TemporalStartSeconds = start;
                            layer.TemporalEndSeconds = end;
                        }
                    }
                    if (!layer.HasTemporalRange)
                    {
                        layer.TemporalStartSeconds = minimum;
                        layer.TemporalEndSeconds = maximum;
                    }

                    layer.Filters = CloneVideoFilters(layer.Filters);
                    foreach (var filter in layer.Filters) NormalizeFilter(filter);
                    return layer;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.NormalizeVideoLayer failed: {exception.Message}");
            throw;
        }
    }

    private void NormalizeFilter(VideoEffectFilter filter)
    {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.NormalizeFilter.");
                    filter.Id = filter.Id == Guid.Empty ? Guid.NewGuid() : filter.Id;
                    filter.Name = string.IsNullOrWhiteSpace(filter.Name) ? filter.Kind.ToString() : filter.Name.Trim();
                    filter.Color = NormalizeColor(filter.Color, filter.Kind == VideoEffectFilterKind.ChromaKey ? "#00ff00" : "#3b82f6");
                    filter.ResidualOpacity = Math.Clamp(double.IsFinite(filter.ResidualOpacity) ? filter.ResidualOpacity : 0, 0, 1);
                    switch (filter.Kind)
                    {
                        case VideoEffectFilterKind.Brightness:
                        case VideoEffectFilterKind.Contrast:
                        case VideoEffectFilterKind.Saturation:
                            filter.Amount = Math.Clamp(double.IsFinite(filter.Amount) ? filter.Amount : 1, 0, 4);
                            break;
                        case VideoEffectFilterKind.HueRotation:
                            filter.Amount = Math.Clamp(double.IsFinite(filter.Amount) ? filter.Amount : 0, -360, 360);
                            break;
                        case VideoEffectFilterKind.Blur:
                            filter.Amount = Math.Clamp(double.IsFinite(filter.Amount) ? filter.Amount : 0, 0, 64);
                            break;
                        case VideoEffectFilterKind.Grayscale:
                        case VideoEffectFilterKind.Sepia:
                        case VideoEffectFilterKind.Invert:
                        case VideoEffectFilterKind.ColorWash:
                        case VideoEffectFilterKind.Grain:
                            filter.Amount = Math.Clamp(double.IsFinite(filter.Amount) ? filter.Amount : 0, 0, 1);
                            break;
                        case VideoEffectFilterKind.ChromaKey:
                            filter.Amount = Math.Clamp(double.IsFinite(filter.Amount) ? filter.Amount : .35, 0, 1);
                            filter.SecondaryAmount = Math.Clamp(double.IsFinite(filter.SecondaryAmount) ? filter.SecondaryAmount : .12, .001, 1);
                            filter.TertiaryAmount = Math.Clamp(double.IsFinite(filter.TertiaryAmount) ? filter.TertiaryAmount : .3, 0, 1);
                            break;
                        case VideoEffectFilterKind.Vignette:
                            filter.Amount = Math.Clamp(double.IsFinite(filter.Amount) ? filter.Amount : .45, 0, 1);
                            filter.SecondaryAmount = Math.Clamp(double.IsFinite(filter.SecondaryAmount) ? filter.SecondaryAmount : .55, 0, 1);
                            break;
                    }
                    var canvasEffect = filter.Kind is VideoEffectFilterKind.ChromaKey or VideoEffectFilterKind.Vignette or VideoEffectFilterKind.Grain or VideoEffectFilterKind.ColorWash;
                    filter.HtmlExportSupport = canvasEffect ? PublicationHtmlExportSupport.CanvasRuntime : PublicationHtmlExportSupport.Native;
                    filter.HtmlExportNote = canvasEffect
                        ? "HTML compatible through the shared canvas runtime; external media must permit canvas access (CORS)."
                        : "HTML compatible through native browser filters.";
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.NormalizeFilter failed: {exception.Message}");
            throw;
        }
    }

    private string NormalizeColor(string? value, string fallback)
    {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.NormalizeColor.");
                    var color = value?.Trim() ?? string.Empty;
                    if (color.Length == 7 && color[0] == '#' && color.Skip(1).All(Uri.IsHexDigit)) return color.ToLowerInvariant();
                    return fallback;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.NormalizeColor failed: {exception.Message}");
            throw;
        }
    }

    private string Suffix(string name, string suffix)
    {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.Suffix.");
                    var value = string.IsNullOrWhiteSpace(name) ? "Clip" : name.Trim();
                    return value.EndsWith($" ({suffix})", StringComparison.OrdinalIgnoreCase) ? value : $"{value} ({suffix})";
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.Suffix failed: {exception.Message}");
            throw;
        }
    }

    private void NormalizeTemporalSelection(PublicationMediaSegment segment)
    {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.NormalizeTemporalSelection.");
                    var minimum = segment.TrimStartSeconds;
                    var maximum = segment.EffectiveTrimEndSeconds;
                    if (!segment.HasTemporalSelection)
                    {
                        segment.TemporalSelectionCommitted = false;
                        segment.TemporalSelectionIsPoint = false;
                        segment.TemporalSelectionStartSeconds = minimum;
                        segment.TemporalSelectionEndSeconds = maximum;
                        return;
                    }

                    var start = double.IsFinite(segment.TemporalSelectionStartSeconds)
                        ? Math.Clamp(segment.TemporalSelectionStartSeconds, minimum, maximum)
                        : minimum;
                    if (!segment.TemporalSelectionCommitted)
                    {
                        var candidateEnd = double.IsFinite(segment.TemporalSelectionEndSeconds)
                            ? Math.Clamp(segment.TemporalSelectionEndSeconds, start, maximum)
                            : maximum;
                        segment.TemporalSelectionCommitted = segment.TemporalSelectionIsPoint
                            || Math.Abs(start - minimum) > .02
                            || Math.Abs(candidateEnd - maximum) > .02;
                    }
                    if (segment.TemporalSelectionIsPoint || maximum - minimum < MinimumSourceLength)
                    {
                        segment.TemporalSelectionIsPoint = true;
                        segment.TemporalSelectionStartSeconds = start;
                        segment.TemporalSelectionEndSeconds = start;
                        return;
                    }

                    var maximumStart = Math.Max(minimum, maximum - MinimumSourceLength);
                    start = Math.Clamp(start, minimum, maximumStart);
                    var end = double.IsFinite(segment.TemporalSelectionEndSeconds)
                        ? Math.Clamp(segment.TemporalSelectionEndSeconds, start + MinimumSourceLength, maximum)
                        : maximum;
                    segment.TemporalSelectionStartSeconds = start;
                    segment.TemporalSelectionEndSeconds = end;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.NormalizeTemporalSelection failed: {exception.Message}");
            throw;
        }
    }

    private void NormalizeCutSections(PublicationMediaSegment segment)
    {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.NormalizeCutSections.");
                    var minimum = segment.TrimStartSeconds;
                    var maximum = segment.EffectiveTrimEndSeconds;
                    segment.CutSections = CloneTemporalSections(segment.CutSections)
                        .Select(section =>
                        {
                            var start = Math.Clamp(double.IsFinite(section.StartSeconds) ? section.StartSeconds : minimum, minimum, maximum);
                            var end = Math.Clamp(double.IsFinite(section.EndSeconds) ? section.EndSeconds : start, start, maximum);
                            section.StartSeconds = start;
                            section.EndSeconds = end;
                            return section;
                        })
                        .Where(section => section.EndSeconds - section.StartSeconds >= MinimumSourceLength)
                        .OrderBy(section => section.StartSeconds)
                        .ThenBy(section => section.EndSeconds)
                        .Take(128)
                        .ToList();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.NormalizeCutSections failed: {exception.Message}");
            throw;
        }
    }

    private string MergeName(string left, string right)
    {
        try
        {
            logger.LogTrace($"Entering MediaTimelineEditService.MergeName.");
                    var leftBase = left.Replace(" (left)", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
                    var rightBase = right.Replace(" (right)", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
                    return string.Equals(leftBase, rightBase, StringComparison.OrdinalIgnoreCase) ? leftBase : $"{leftBase} + {rightBase}";
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"MediaTimelineEditService.MergeName failed: {exception.Message}");
            throw;
        }
    }
}
