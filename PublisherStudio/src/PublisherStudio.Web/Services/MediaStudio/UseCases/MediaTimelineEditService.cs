using PublisherStudio.Domain;

namespace PublisherStudio.Services.MediaStudio.UseCases;

/// <summary>
/// Reusable, non-destructive clip orchestration shared by Video Studio and Audio Studio.
/// Components own pointer state; this service owns deterministic timeline mutations.
/// </summary>
public sealed class MediaTimelineEditService
{
    private const double MinimumSourceLength = .01;

    public List<PublicationMediaSegment> Normalize(IEnumerable<PublicationMediaSegment>? source, bool video)
    {
        var fallbackMime = video ? "video/webm" : "audio/webm";
        return (source ?? [])
            .Where(segment => segment is not null && !string.IsNullOrWhiteSpace(segment.DataUrl))
            .Select(segment => new PublicationMediaSegment
            {
                Id = segment.Id == Guid.Empty ? Guid.NewGuid() : segment.Id,
                Name = string.IsNullOrWhiteSpace(segment.Name) ? (video ? "Video clip" : "Audio clip") : segment.Name.Trim(),
                DataUrl = PublicationMediaData.NormalizeDataUrl(segment.DataUrl, fallbackMime),
                MimeType = PublicationMediaData.NormalizeMimeType(segment.MimeType, fallbackMime),
                PosterDataUrl = segment.PosterDataUrl ?? string.Empty,
                DurationSeconds = Math.Max(MinimumSourceLength, segment.DurationSeconds),
                TrimStartSeconds = Math.Max(0, segment.TrimStartSeconds),
                TrimEndSeconds = Math.Max(0, segment.TrimEndSeconds),
                WaveformSamples = segment.WaveformSamples?.Select(value => Math.Clamp(value, 0, 1)).Take(256).ToList() ?? []
            })
            .Select(segment =>
            {
                segment.TrimStartSeconds = Math.Clamp(segment.TrimStartSeconds, 0, Math.Max(0, segment.DurationSeconds - MinimumSourceLength));
                segment.TrimEndSeconds = segment.TrimEndSeconds > segment.TrimStartSeconds
                    ? Math.Clamp(segment.TrimEndSeconds, segment.TrimStartSeconds + MinimumSourceLength, segment.DurationSeconds)
                    : segment.DurationSeconds;
                return segment;
            })
            .ToList();
    }

    public double TimelineLength(IReadOnlyList<PublicationMediaSegment> segments, double playbackRate)
        => segments.Sum(segment => segment.SourceLengthSeconds) / Math.Max(.1, playbackRate);

    public int SegmentIndexAt(IReadOnlyList<PublicationMediaSegment> segments, double playbackRate, double timelineSeconds)
    {
        if (segments.Count == 0) return -1;
        var cursor = 0d;
        var rate = Math.Max(.1, playbackRate);
        for (var index = 0; index < segments.Count; index++)
        {
            var length = segments[index].SourceLengthSeconds / rate;
            if (timelineSeconds <= cursor + length || index == segments.Count - 1) return index;
            cursor += length;
        }
        return segments.Count - 1;
    }

    public double SegmentTimelineStart(IReadOnlyList<PublicationMediaSegment> segments, int index, double playbackRate)
        => segments.Take(Math.Clamp(index, 0, segments.Count)).Sum(segment => segment.SourceLengthSeconds) / Math.Max(.1, playbackRate);

    public double SourcePositionAt(IReadOnlyList<PublicationMediaSegment> segments, int index, double playbackRate, double timelineSeconds)
    {
        if (index < 0 || index >= segments.Count) return 0;
        var start = SegmentTimelineStart(segments, index, playbackRate);
        var segment = segments[index];
        return Math.Clamp(segment.TrimStartSeconds + Math.Max(0, timelineSeconds - start) * Math.Max(.1, playbackRate),
            segment.TrimStartSeconds, segment.EffectiveTrimEndSeconds);
    }

    public Guid? SplitAt(List<PublicationMediaSegment> segments, double playbackRate, double timelineSeconds)
    {
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
        segments.Insert(index + 1, right);
        return right.Id;
    }

    public Guid InsertAt(
        List<PublicationMediaSegment> segments,
        double playbackRate,
        double timelineSeconds,
        PublicationMediaSegment inserted)
    {
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
        var segmentLength = segments[index].SourceLengthSeconds / Math.Max(.1, playbackRate);
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

    public bool CanMergeBoundary(IReadOnlyList<PublicationMediaSegment> segments, int rightIndex)
    {
        if (rightIndex <= 0 || rightIndex >= segments.Count) return false;
        var left = segments[rightIndex - 1];
        var right = segments[rightIndex];
        return string.Equals(left.DataUrl, right.DataUrl, StringComparison.Ordinal)
            && string.Equals(left.MimeType, right.MimeType, StringComparison.OrdinalIgnoreCase)
            && Math.Abs(left.EffectiveTrimEndSeconds - right.TrimStartSeconds) <= .02;
    }

    public bool MergeBoundary(List<PublicationMediaSegment> segments, int rightIndex)
    {
        if (!CanMergeBoundary(segments, rightIndex)) return false;
        var left = segments[rightIndex - 1];
        var right = segments[rightIndex];
        left.TrimEndSeconds = right.EffectiveTrimEndSeconds;
        left.Name = MergeName(left.Name, right.Name);
        segments.RemoveAt(rightIndex);
        return true;
    }

    public PublicationMediaSegment Clone(PublicationMediaSegment segment) => new()
    {
        Id = segment.Id,
        Name = segment.Name,
        DataUrl = segment.DataUrl,
        MimeType = segment.MimeType,
        PosterDataUrl = segment.PosterDataUrl,
        DurationSeconds = segment.DurationSeconds,
        TrimStartSeconds = segment.TrimStartSeconds,
        TrimEndSeconds = segment.TrimEndSeconds,
        WaveformSamples = [.. segment.WaveformSamples]
    };

    public PublicationMediaSegment Duplicate(PublicationMediaSegment segment)
    {
        var clone = Clone(segment);
        clone.Id = Guid.NewGuid();
        clone.Name = Suffix(segment.Name, "copy");
        return clone;
    }

    private static string Suffix(string name, string suffix)
    {
        var value = string.IsNullOrWhiteSpace(name) ? "Clip" : name.Trim();
        return value.EndsWith($" ({suffix})", StringComparison.OrdinalIgnoreCase) ? value : $"{value} ({suffix})";
    }

    private static string MergeName(string left, string right)
    {
        var leftBase = left.Replace(" (left)", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        var rightBase = right.Replace(" (right)", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        return string.Equals(leftBase, rightBase, StringComparison.OrdinalIgnoreCase) ? leftBase : $"{leftBase} + {rightBase}";
    }
}
