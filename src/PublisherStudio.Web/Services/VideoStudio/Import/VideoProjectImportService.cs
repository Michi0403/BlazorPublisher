using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Services.VideoStudio.Import;

/// <summary>
/// Imports openly documented timeline/project formats into PublisherStudio's canonical video-project model.
/// The adapter never mutates an active publication and reports every known approximation or loss.
/// </summary>
public sealed class VideoProjectImportService(
    IPublisherRuntimePolicyDataService runtimePolicy,
    IPublisherRuntimePatternService runtimePatterns,
    ILogger<VideoProjectImportService> logger)
{
    /// <summary>
    /// Gets supported extensions.
    /// </summary>
    public IReadOnlyList<string> SupportedExtensions => runtimePolicy.GetCollection(PublisherRuntimeCollection.VideoProjectExtensions);

    /// <summary>
    /// Imports async.
    /// </summary>
    public async Task<VideoProjectImportResult> ImportAsync(
        Stream source,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.ImportAsync.");
                    ArgumentNullException.ThrowIfNull(source);
                    fileName ??= string.Empty;
                    var extension = Path.GetExtension(fileName).ToLowerInvariant();
                    if (!SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                        throw new InvalidDataException($"Unsupported video-project format '{extension}'.");

                    if (extension == ".otioz")
                        return await ImportOtioBundleAsync(source, fileName, cancellationToken).ConfigureAwait(false);

                    var bytes = await ReadAllAsync(source, cancellationToken).ConfigureAwait(false);
                    var result = extension switch
                    {
                        ".otio" => ImportOtio(bytes, fileName, null),
                        ".mlt" or ".kdenlive" => ImportMlt(bytes, fileName),
                        ".xges" => ImportXges(bytes, fileName),
                        ".osp" => ImportOpenShot(bytes, fileName),
                        ".edl" => ImportEdl(bytes, fileName),
                        _ => throw new InvalidDataException($"Unsupported video-project format '{extension}'.")
                    };
                    return FinalizeResult(result, fileName);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.ImportAsync failed: {exception.Message}");
            throw;
        }
    }

    private async Task<VideoProjectImportResult> ImportOtioBundleAsync(
        Stream source,
        string fileName,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.ImportOtioBundleAsync.");
                    using var archiveBuffer = new MemoryStream(await ReadAllAsync(source, cancellationToken).ConfigureAwait(false));
                    using var archive = new ZipArchive(archiveBuffer, ZipArchiveMode.Read, leaveOpen: false);
                    if (archive.Entries.Count > runtimePolicy.MaximumVideoArchiveEntries)
                        throw new InvalidDataException($"The OTIOZ archive contains more than {runtimePolicy.MaximumVideoArchiveEntries} entries.");

                    var entryMap = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
                    foreach (var entry in archive.Entries)
                    {
                        var path = NormalizeArchivePath(entry.FullName);
                        if (string.IsNullOrWhiteSpace(path)) continue;
                        entryMap[path] = entry;
                    }

                    if (!entryMap.TryGetValue("content.otio", out var contentEntry))
                        throw new InvalidDataException("The OTIOZ bundle does not contain the required top-level content.otio entry.");

                    byte[] content;
                    await using (var stream = contentEntry.Open())
                        content = await ReadAllAsync(stream, cancellationToken).ConfigureAwait(false);

                    EmbeddedMedia? Resolver(string targetUrl)
                    {
                        var normalized = NormalizeOtioTarget(targetUrl);
                        if (string.IsNullOrWhiteSpace(normalized)) return null;
                        var candidates = new[]
                        {
                            normalized,
                            $"media/{Path.GetFileName(normalized)}",
                            Path.GetFileName(normalized)
                        };
                        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
                        {
                            if (!entryMap.TryGetValue(candidate, out var entry) || entry.Length <= 0) continue;
                            using var media = entry.Open();
                            var bytes = ReadAll(media);
                            var mime = MimeFromPath(candidate, "video/mp4");
                            return new EmbeddedMedia(candidate, mime, bytes);
                        }
                        return null;
                    }

                    var result = ImportOtio(content, fileName, Resolver);
                    result.Issues.Add(new InterchangeIssue(
                        InterchangeIssueSeverity.Information,
                        "OTIOZ_BUNDLE_IMPORTED",
                        "The OTIO timeline and any matched bundled media were imported from content.otio.",
                        fileName));
                    return FinalizeResult(result, fileName);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.ImportOtioBundleAsync failed: {exception.Message}");
            throw;
        }
    }

    private VideoProjectImportResult ImportOtio(
        byte[] bytes,
        string fileName,
        Func<string, EmbeddedMedia?>? bundledMediaResolver)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.ImportOtio.");
                    using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions
                    {
                        AllowTrailingCommas = true,
                        CommentHandling = JsonCommentHandling.Skip,
                        MaxDepth = 256
                    });

                    var issues = new List<InterchangeIssue>();
                    var root = document.RootElement;
                    var schema = JsonString(root, "OTIO_SCHEMA");
                    var project = new VideoProjectDocument
                    {
                        Name = JsonString(root, "name", Path.GetFileNameWithoutExtension(fileName)),
                        SourceFormat = "OpenTimelineIO",
                        SourceFormatVersion = SchemaVersion(schema),
                        FrameRate = FindOtioRate(root, 24)
                    };

                    var trackContainer = root;
                    if (schema.StartsWith("Timeline", StringComparison.OrdinalIgnoreCase)
                        && root.TryGetProperty("tracks", out var tracksElement))
                        trackContainer = tracksElement;

                    var trackChildren = JsonArray(trackContainer, "children").ToArray();
                    if (trackChildren.Length == 0 && SchemaName(schema) == "Track")
                        trackChildren = [trackContainer];

                    var trackOrder = 0;
                    foreach (var trackNode in trackChildren)
                    {
                        if (SchemaName(JsonString(trackNode, "OTIO_SCHEMA")) != "Track")
                        {
                            issues.Add(new InterchangeIssue(
                                InterchangeIssueSeverity.Loss,
                                "OTIO_NESTED_STACK_FLATTENED",
                                "A nested OTIO stack was flattened into a generated video track.",
                                JsonString(trackNode, "name")));
                            ImportOtioNestedStack(trackNode, project, issues, ref trackOrder, bundledMediaResolver);
                            continue;
                        }

                        var kindText = JsonString(trackNode, "kind", "Video");
                        var track = new MediaTimelineTrack
                        {
                            Name = JsonString(trackNode, "name", $"{kindText} {trackOrder + 1}"),
                            Kind = kindText.Equals("Audio", StringComparison.OrdinalIgnoreCase)
                                ? MediaTimelineTrackKind.Audio
                                : MediaTimelineTrackKind.Video,
                            Order = trackOrder++,
                            Enabled = JsonBool(trackNode, "enabled", true)
                        };
                        ImportOtioTrackChildren(trackNode, track, project, issues, bundledMediaResolver);
                        project.Tracks.Add(track);
                    }

                    ImportOtioMarkers(root, project.Markers, project.FrameRate);
                    if (project.Tracks.Count == 0)
                        issues.Add(new InterchangeIssue(InterchangeIssueSeverity.Loss, "OTIO_NO_TRACKS", "No importable OTIO tracks were found.", fileName));

                    project.ActiveTrackId = project.Tracks.FirstOrDefault(track => track.Kind == MediaTimelineTrackKind.Video)?.Id
                        ?? project.Tracks.FirstOrDefault()?.Id
                        ?? Guid.Empty;

                    return new VideoProjectImportResult { Project = project, Issues = issues };
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.ImportOtio failed: {exception.Message}");
            throw;
        }
    }

    private void ImportOtioNestedStack(
        JsonElement stack,
        VideoProjectDocument project,
        List<InterchangeIssue> issues,
        ref int trackOrder,
        Func<string, EmbeddedMedia?>? bundledMediaResolver)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.ImportOtioNestedStack.");
                    var nestedTracks = JsonArray(stack, "children")
                        .Where(node => SchemaName(JsonString(node, "OTIO_SCHEMA")) == "Track")
                        .ToArray();
                    if (nestedTracks.Length == 0) return;
                    foreach (var nested in nestedTracks)
                    {
                        var kindText = JsonString(nested, "kind", "Video");
                        var track = new MediaTimelineTrack
                        {
                            Name = JsonString(nested, "name", $"Nested {kindText} {trackOrder + 1}"),
                            Kind = kindText.Equals("Audio", StringComparison.OrdinalIgnoreCase)
                                ? MediaTimelineTrackKind.Audio
                                : MediaTimelineTrackKind.Video,
                            Order = trackOrder++,
                            Enabled = JsonBool(nested, "enabled", true)
                        };
                        ImportOtioTrackChildren(nested, track, project, issues, bundledMediaResolver);
                        project.Tracks.Add(track);
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.ImportOtioNestedStack failed: {exception.Message}");
            throw;
        }
    }

    private void ImportOtioTrackChildren(
        JsonElement trackNode,
        MediaTimelineTrack track,
        VideoProjectDocument project,
        List<InterchangeIssue> issues,
        Func<string, EmbeddedMedia?>? bundledMediaResolver)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.ImportOtioTrackChildren.");
                    var cursor = 0d;
                    PublicationMediaSegment? previousClip = null;
                    foreach (var child in JsonArray(trackNode, "children"))
                    {
                        var schema = SchemaName(JsonString(child, "OTIO_SCHEMA"));
                        if (schema == "Gap")
                        {
                            var gapDuration = OtioTimeRangeDuration(child, project.FrameRate);
                            if (gapDuration <= 0) continue;
                            track.Segments.Add(new PublicationMediaSegment
                            {
                                Name = JsonString(child, "name", "Gap"),
                                IsGap = true,
                                Enabled = JsonBool(child, "enabled", true),
                                TimelineStartSeconds = cursor,
                                TimelineDurationSeconds = gapDuration,
                                DurationSeconds = gapDuration,
                                TrimEndSeconds = gapDuration,
                                SourceRate = project.FrameRate
                            });
                            cursor += gapDuration;
                            previousClip = null;
                            continue;
                        }

                        if (schema == "Transition")
                        {
                            var inOffset = OtioRationalSeconds(child, "in_offset", project.FrameRate);
                            var outOffset = OtioRationalSeconds(child, "out_offset", project.FrameRate);
                            var duration = Math.Max(.001, inOffset + outOffset);
                            project.Transitions.Add(new MediaTimelineTransition
                            {
                                Name = JsonString(child, "name", "OTIO transition"),
                                Kind = MediaTimelineTransitionKind.Dissolve,
                                TrackId = track.Id,
                                FromSegmentId = previousClip?.Id,
                                TimelineStartSeconds = Math.Max(0, cursor - inOffset),
                                DurationSeconds = duration,
                                Metadata = { ["otio_schema"] = JsonString(child, "OTIO_SCHEMA") }
                            });
                            issues.Add(new InterchangeIssue(
                                InterchangeIssueSeverity.Warning,
                                "OTIO_TRANSITION_APPROXIMATED",
                                "The OTIO transition timing was retained, but its foreign effect implementation was approximated as a dissolve.",
                                JsonString(child, "name")));
                            continue;
                        }

                        if (schema != "Clip")
                        {
                            issues.Add(new InterchangeIssue(
                                InterchangeIssueSeverity.Loss,
                                "OTIO_ITEM_UNSUPPORTED",
                                $"OTIO item type '{schema}' is not directly editable and was skipped.",
                                JsonString(child, "name")));
                            continue;
                        }

                        var sourceStart = OtioTimeRangeStart(child, project.FrameRate);
                        var sourceDuration = OtioTimeRangeDuration(child, project.FrameRate);
                        var speed = OtioLinearSpeed(child, issues);
                        var timelineDuration = Math.Max(.01, sourceDuration / Math.Max(.0001, Math.Abs(speed)));
                        var reference = child.TryGetProperty("media_reference", out var mediaReference)
                            ? mediaReference
                            : default;
                        var referenceSchema = reference.ValueKind == JsonValueKind.Object
                            ? SchemaName(JsonString(reference, "OTIO_SCHEMA"))
                            : "MissingReference";
                        var targetUrl = reference.ValueKind == JsonValueKind.Object
                            ? JsonString(reference, "target_url")
                            : string.Empty;
                        var embedded = !string.IsNullOrWhiteSpace(targetUrl) ? bundledMediaResolver?.Invoke(targetUrl) : null;
                        var mime = embedded?.MimeType ?? MimeFromPath(targetUrl, track.Kind == MediaTimelineTrackKind.Audio ? "audio/mpeg" : "video/mp4");
                        var name = JsonString(child, "name", Path.GetFileName(UriPath(targetUrl)));
                        if (string.IsNullOrWhiteSpace(name)) name = $"Clip {track.Segments.Count + 1}";

                        var segment = new PublicationMediaSegment
                        {
                            Name = name,
                            Enabled = JsonBool(child, "enabled", true),
                            TimelineStartSeconds = cursor,
                            TimelineDurationSeconds = timelineDuration,
                            SourceRate = OtioSourceRate(child, project.FrameRate),
                            Speed = speed,
                            DurationSeconds = Math.Max(sourceStart + sourceDuration, sourceDuration),
                            TrimStartSeconds = sourceStart,
                            TrimEndSeconds = sourceStart + sourceDuration,
                            MimeType = mime,
                            DataUrl = embedded is null ? string.Empty : DataUrl(embedded.MimeType, embedded.Bytes),
                            SourceReference = new MediaSourceReference
                            {
                                Id = JsonString(reference, "name", name),
                                Uri = targetUrl,
                                OriginalPath = UriPath(targetUrl),
                                MimeType = mime,
                                Missing = embedded is null,
                                Metadata = { ["otio_reference_schema"] = referenceSchema }
                            },
                            ImportMetadata =
                            {
                                ["source_format"] = "OpenTimelineIO",
                                ["otio_schema"] = JsonString(child, "OTIO_SCHEMA")
                            }
                        };
                        if (track.Kind == MediaTimelineTrackKind.Video)
                            segment.VideoLayers.Add(DefaultVideoLayer(name, sourceStart, sourceStart + sourceDuration));
                        track.Segments.Add(segment);
                        previousClip = segment;
                        cursor += timelineDuration;

                        ImportOtioMarkers(child, project.Markers, project.FrameRate, segment.TimelineStartSeconds);
                        var effects = JsonArray(child, "effects").ToArray();
                        foreach (var effect in effects)
                        {
                            var effectSchema = SchemaName(JsonString(effect, "OTIO_SCHEMA"));
                            if (effectSchema == "LinearTimeWarp") continue;
                            issues.Add(new InterchangeIssue(
                                InterchangeIssueSeverity.Loss,
                                "OTIO_EFFECT_RETAINED_AS_METADATA",
                                $"OTIO effect '{JsonString(effect, "effect_name", effectSchema)}' was retained as compatibility metadata but is not rendered.",
                                name));
                        }
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.ImportOtioTrackChildren failed: {exception.Message}");
            throw;
        }
    }

    private double OtioLinearSpeed(JsonElement clip, List<InterchangeIssue> issues)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.OtioLinearSpeed.");
                    foreach (var effect in JsonArray(clip, "effects"))
                    {
                        var schema = SchemaName(JsonString(effect, "OTIO_SCHEMA"));
                        if (schema == "LinearTimeWarp")
                        {
                            var scalar = JsonDouble(effect, "time_scalar", 1);
                            return Math.Abs(scalar) < .0001 ? 1 : scalar;
                        }
                        if (schema == "FreezeFrame")
                        {
                            issues.Add(new InterchangeIssue(
                                InterchangeIssueSeverity.Loss,
                                "OTIO_FREEZE_FRAME_APPROXIMATED",
                                "An OTIO freeze frame was imported as a very slow clip because PublisherStudio does not yet have a native freeze-frame clip type.",
                                JsonString(clip, "name")));
                            return .0001;
                        }
                    }
                    return 1;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.OtioLinearSpeed failed: {exception.Message}");
            throw;
        }
    }

    private void ImportOtioMarkers(
        JsonElement owner,
        List<MediaProjectMarker> markers,
        double fallbackRate,
        double timelineOffset = 0)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.ImportOtioMarkers.");
                    foreach (var marker in JsonArray(owner, "markers"))
                    {
                        var markedRange = marker.TryGetProperty("marked_range", out var range) ? range : default;
                        var start = markedRange.ValueKind == JsonValueKind.Object ? OtioTimeRangeStart(markedRange, fallbackRate) : 0;
                        var duration = markedRange.ValueKind == JsonValueKind.Object ? OtioTimeRangeDuration(markedRange, fallbackRate) : 0;
                        markers.Add(new MediaProjectMarker
                        {
                            Name = JsonString(marker, "name", "OTIO marker"),
                            Color = OtioMarkerColor(JsonString(marker, "color")),
                            StartSeconds = timelineOffset + start,
                            DurationSeconds = duration,
                            Comment = JsonString(marker, "comment")
                        });
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.ImportOtioMarkers failed: {exception.Message}");
            throw;
        }
    }

    private VideoProjectImportResult ImportMlt(byte[] bytes, string fileName)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.ImportMlt.");
                    var document = ParseSafeXml(bytes);
                    var root = document.Root ?? throw new InvalidDataException("The MLT XML document has no root element.");
                    if (!root.Name.LocalName.Equals("mlt", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("The file is not an MLT XML project.");

                    var issues = new List<InterchangeIssue>();
                    var profile = root.Elements().FirstOrDefault(element => element.Name.LocalName == "profile");
                    var frameRate = Ratio(
                        AttributeDouble(profile, "frame_rate_num", 25),
                        AttributeDouble(profile, "frame_rate_den", 1),
                        25);
                    var project = new VideoProjectDocument
                    {
                        Name = Property(root, "kdenlive:docproperties.documentid", Path.GetFileNameWithoutExtension(fileName)),
                        SourceFormat = Path.GetExtension(fileName).Equals(".kdenlive", StringComparison.OrdinalIgnoreCase)
                            ? "Kdenlive / MLT XML"
                            : "MLT XML",
                        SourceFormatVersion = Attribute(root, "version"),
                        FrameRate = frameRate,
                        Width = (int)Math.Max(1, AttributeDouble(profile, "width", 1920)),
                        Height = (int)Math.Max(1, AttributeDouble(profile, "height", 1080))
                    };

                    var producers = new Dictionary<string, XElement>(StringComparer.Ordinal);
                    foreach (var producer in root.Elements().Where(element => element.Name.LocalName is "producer" or "chain"))
                    {
                        var id = Attribute(producer, "id");
                        if (!string.IsNullOrWhiteSpace(id)) producers[id] = producer;
                    }
                    var playlists = root.Elements().Where(element => element.Name.LocalName == "playlist")
                        .Where(element => !string.IsNullOrWhiteSpace(Attribute(element, "id")))
                        .ToDictionary(element => Attribute(element, "id"), StringComparer.Ordinal);

                    var tractors = root.Elements().Where(element => element.Name.LocalName == "tractor").ToArray();
                    var mainTractor = tractors.LastOrDefault(tractor => tractor.Elements().Any(element => element.Name.LocalName == "multitrack"))
                        ?? tractors.LastOrDefault();
                    var trackRefs = mainTractor?.Elements().FirstOrDefault(element => element.Name.LocalName == "multitrack")?
                        .Elements().Where(element => element.Name.LocalName == "track").ToArray()
                        ?? [];

                    var order = 0;
                    foreach (var trackRef in trackRefs)
                    {
                        var playlistId = Attribute(trackRef, "producer");
                        if (!playlists.TryGetValue(playlistId, out var playlist)) continue;
                        var hide = Attribute(trackRef, "hide");
                        var kind = hide.Equals("video", StringComparison.OrdinalIgnoreCase)
                            ? MediaTimelineTrackKind.Audio
                            : MediaTimelineTrackKind.Video;
                        var track = new MediaTimelineTrack
                        {
                            Name = Property(playlist, "kdenlive:track_name", string.IsNullOrWhiteSpace(playlistId) ? $"Track {order + 1}" : playlistId),
                            Kind = kind,
                            Order = order++,
                            Muted = Property(playlist, "hide", string.Empty).Contains("audio", StringComparison.OrdinalIgnoreCase)
                        };
                        ImportMltPlaylist(playlist, track, producers, frameRate, issues);
                        project.Tracks.Add(track);
                    }

                    if (project.Tracks.Count == 0)
                    {
                        foreach (var playlist in playlists.Values)
                        {
                            if (!playlist.Elements().Any(element => element.Name.LocalName is "entry" or "blank")) continue;
                            var track = new MediaTimelineTrack
                            {
                                Name = Property(playlist, "kdenlive:track_name", Attribute(playlist, "id")),
                                Kind = MediaTimelineTrackKind.Video,
                                Order = order++
                            };
                            ImportMltPlaylist(playlist, track, producers, frameRate, issues);
                            if (track.Segments.Count > 0) project.Tracks.Add(track);
                        }
                    }

                    if (mainTractor is not null)
                        ImportMltTransitions(mainTractor, project, issues, frameRate);

                    project.ActiveTrackId = project.Tracks.FirstOrDefault(track => track.Kind == MediaTimelineTrackKind.Video)?.Id
                        ?? project.Tracks.FirstOrDefault()?.Id
                        ?? Guid.Empty;
                    if (project.Tracks.Count == 0)
                        issues.Add(new InterchangeIssue(InterchangeIssueSeverity.Loss, "MLT_NO_PLAYLISTS", "No editable MLT playlists were found.", fileName));

                    return new VideoProjectImportResult { Project = project, Issues = issues };
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.ImportMlt failed: {exception.Message}");
            throw;
        }
    }

    private void ImportMltPlaylist(
        XElement playlist,
        MediaTimelineTrack track,
        IReadOnlyDictionary<string, XElement> producers,
        double frameRate,
        List<InterchangeIssue> issues)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.ImportMltPlaylist.");
                    var cursor = 0d;
                    foreach (var item in playlist.Elements().Where(element => element.Name.LocalName is "entry" or "blank"))
                    {
                        if (item.Name.LocalName == "blank")
                        {
                            var duration = MltDuration(Attribute(item, "length"), frameRate);
                            if (duration <= 0) continue;
                            track.Segments.Add(new PublicationMediaSegment
                            {
                                Name = "Gap",
                                IsGap = true,
                                TimelineStartSeconds = cursor,
                                TimelineDurationSeconds = duration,
                                DurationSeconds = duration,
                                TrimEndSeconds = duration,
                                SourceRate = frameRate
                            });
                            cursor += duration;
                            continue;
                        }

                        var producerId = Attribute(item, "producer");
                        producers.TryGetValue(producerId, out var producer);
                        var inValue = Attribute(item, "in");
                        var outValue = Attribute(item, "out");
                        var sourceStart = MltPosition(inValue, frameRate);
                        var sourceEnd = MltPosition(outValue, frameRate);
                        var sourceDuration = sourceEnd >= sourceStart
                            ? Math.Max(1 / frameRate, sourceEnd - sourceStart + 1 / frameRate)
                            : Math.Max(1 / frameRate, MltDuration(Property(producer, "length", string.Empty), frameRate));
                        var resource = Property(producer, "resource", Property(producer, "warp_resource", string.Empty));
                        var service = Property(producer, "mlt_service", string.Empty);
                        var name = Property(producer, "kdenlive:clipname", Path.GetFileName(UriPath(resource)));
                        if (string.IsNullOrWhiteSpace(name)) name = string.IsNullOrWhiteSpace(producerId) ? $"Clip {track.Segments.Count + 1}" : producerId;
                        var mime = MimeFromPath(resource, track.Kind == MediaTimelineTrackKind.Audio ? "audio/mpeg" : "video/mp4");
                        var speed = ParseMltSpeed(resource, producer);
                        var timelineDuration = sourceDuration / Math.Max(.0001, Math.Abs(speed));
                        var segment = new PublicationMediaSegment
                        {
                            Name = name,
                            TimelineStartSeconds = cursor,
                            TimelineDurationSeconds = timelineDuration,
                            SourceRate = frameRate,
                            Speed = speed,
                            DurationSeconds = Math.Max(sourceEnd, sourceDuration),
                            TrimStartSeconds = sourceStart,
                            TrimEndSeconds = sourceStart + sourceDuration,
                            MimeType = mime,
                            SourceReference = new MediaSourceReference
                            {
                                Id = producerId,
                                Uri = resource,
                                OriginalPath = UriPath(resource),
                                MimeType = mime,
                                Missing = !string.IsNullOrWhiteSpace(resource)
                            },
                            ImportMetadata =
                            {
                                ["source_format"] = "MLT XML",
                                ["mlt_service"] = service,
                                ["producer_id"] = producerId
                            }
                        };
                        if (track.Kind == MediaTimelineTrackKind.Video)
                            segment.VideoLayers.Add(DefaultVideoLayer(name, sourceStart, sourceStart + sourceDuration));
                        track.Segments.Add(segment);
                        cursor += timelineDuration;

                        if (producer is not null && producer.Elements().Any(element => element.Name.LocalName == "filter"))
                            issues.Add(new InterchangeIssue(
                                InterchangeIssueSeverity.Loss,
                                "MLT_FILTERS_RETAINED_AS_METADATA",
                                "MLT producer filters were detected but are not yet mapped to PublisherStudio live-filter parameters.",
                                name));
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.ImportMltPlaylist failed: {exception.Message}");
            throw;
        }
    }

    private void ImportMltTransitions(
        XElement tractor,
        VideoProjectDocument project,
        List<InterchangeIssue> issues,
        double frameRate)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.ImportMltTransitions.");
                    foreach (var transition in tractor.Elements().Where(element => element.Name.LocalName == "transition"))
                    {
                        var start = MltPosition(Attribute(transition, "in"), frameRate);
                        var end = MltPosition(Attribute(transition, "out"), frameRate);
                        var service = Property(transition, "mlt_service", string.Empty);
                        var bTrack = (int)PropertyDouble(transition, "b_track", -1);
                        var targetTrack = project.Tracks.ElementAtOrDefault(Math.Max(0, bTrack - 1));
                        project.Transitions.Add(new MediaTimelineTransition
                        {
                            Name = string.IsNullOrWhiteSpace(service) ? "MLT transition" : service,
                            Kind = service.Contains("luma", StringComparison.OrdinalIgnoreCase)
                                ? MediaTimelineTransitionKind.Wipe
                                : MediaTimelineTransitionKind.Dissolve,
                            TrackId = targetTrack?.Id ?? Guid.Empty,
                            TimelineStartSeconds = start,
                            DurationSeconds = Math.Max(1 / frameRate, end - start + 1 / frameRate),
                            Metadata = { ["mlt_service"] = service }
                        });
                        issues.Add(new InterchangeIssue(
                            InterchangeIssueSeverity.Warning,
                            "MLT_TRANSITION_APPROXIMATED",
                            "MLT transition timing was retained; service-specific rendering is approximated by the closest canonical transition.",
                            service));
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.ImportMltTransitions failed: {exception.Message}");
            throw;
        }
    }

    private VideoProjectImportResult ImportXges(byte[] bytes, string fileName)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.ImportXges.");
                    var document = ParseSafeXml(bytes);
                    var root = document.Root ?? throw new InvalidDataException("The XGES document has no root element.");
                    if (!root.Name.LocalName.Equals("ges", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("The file is not a GStreamer Editing Services project.");

                    var issues = new List<InterchangeIssue>();
                    var projectNode = root.Descendants().FirstOrDefault(element => element.Name.LocalName == "project") ?? root;
                    var timeline = projectNode.Descendants().FirstOrDefault(element => element.Name.LocalName == "timeline")
                        ?? throw new InvalidDataException("The XGES project does not contain a timeline.");
                    var project = new VideoProjectDocument
                    {
                        Name = Path.GetFileNameWithoutExtension(fileName),
                        SourceFormat = "XGES / GStreamer Editing Services",
                        SourceFormatVersion = Attribute(root, "version"),
                        FrameRate = ParseCapsFrameRate(Attribute(timeline, "properties"), 30)
                    };

                    var assets = projectNode.Descendants().Where(element => element.Name.LocalName == "asset")
                        .Where(element => !string.IsNullOrWhiteSpace(Attribute(element, "id")))
                        .ToDictionary(element => Attribute(element, "id"), StringComparer.Ordinal);
                    var order = 0;
                    foreach (var layerNode in timeline.Elements().Where(element => element.Name.LocalName == "layer")
                                 .OrderBy(element => AttributeDouble(element, "priority", order)))
                    {
                        var priority = (int)AttributeDouble(layerNode, "priority", order);
                        var videoTrack = new MediaTimelineTrack
                        {
                            Name = $"Video layer {priority + 1}",
                            Kind = MediaTimelineTrackKind.Video,
                            Order = order++
                        };
                        var audioTrack = new MediaTimelineTrack
                        {
                            Name = $"Audio layer {priority + 1}",
                            Kind = MediaTimelineTrackKind.Audio,
                            Order = order++
                        };

                        foreach (var clipNode in layerNode.Elements().Where(element => element.Name.LocalName == "clip"))
                        {
                            var assetId = Attribute(clipNode, "asset-id");
                            assets.TryGetValue(assetId, out var asset);
                            var start = Nanoseconds(AttributeDouble(clipNode, "start", 0));
                            var inPoint = Nanoseconds(AttributeDouble(clipNode, "inpoint", 0));
                            var duration = Math.Max(.01, Nanoseconds(AttributeDouble(clipNode, "duration", 0)));
                            var trackTypes = (int)AttributeDouble(clipNode, "track-types", 4);
                            var name = PropertyFromSerialized(Attribute(clipNode, "properties"), "name", Path.GetFileName(UriPath(assetId)));
                            if (string.IsNullOrWhiteSpace(name)) name = $"Clip {videoTrack.Segments.Count + audioTrack.Segments.Count + 1}";
                            var typeName = Attribute(clipNode, "type-name");
                            var sourceDuration = Math.Max(duration + inPoint, duration);

                            if ((trackTypes & 4) != 0 || trackTypes == 0)
                            {
                                var segment = XgesSegment(name, assetId, asset, start, inPoint, duration, sourceDuration, MediaTimelineTrackKind.Video, project.FrameRate, typeName);
                                videoTrack.Segments.Add(segment);
                            }
                            if ((trackTypes & 2) != 0)
                            {
                                var segment = XgesSegment(name, assetId, asset, start, inPoint, duration, sourceDuration, MediaTimelineTrackKind.Audio, project.FrameRate, typeName);
                                audioTrack.Segments.Add(segment);
                            }

                            if (clipNode.Descendants().Any(element => element.Name.LocalName.Contains("effect", StringComparison.OrdinalIgnoreCase)))
                                issues.Add(new InterchangeIssue(
                                    InterchangeIssueSeverity.Loss,
                                    "XGES_EFFECTS_RETAINED_AS_METADATA",
                                    "XGES effects were detected and retained as source metadata, but their GStreamer-specific parameters are not rendered.",
                                    name));
                        }

                        if (videoTrack.Segments.Count > 0) project.Tracks.Add(videoTrack);
                        if (audioTrack.Segments.Count > 0) project.Tracks.Add(audioTrack);
                    }

                    project.ActiveTrackId = project.Tracks.FirstOrDefault(track => track.Kind == MediaTimelineTrackKind.Video)?.Id
                        ?? project.Tracks.FirstOrDefault()?.Id
                        ?? Guid.Empty;
                    if (project.Tracks.Count == 0)
                        issues.Add(new InterchangeIssue(InterchangeIssueSeverity.Loss, "XGES_NO_CLIPS", "No importable XGES clips were found.", fileName));
                    return new VideoProjectImportResult { Project = project, Issues = issues };
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.ImportXges failed: {exception.Message}");
            throw;
        }
    }

    private PublicationMediaSegment XgesSegment(
        string name,
        string assetId,
        XElement? asset,
        double start,
        double inPoint,
        double duration,
        double sourceDuration,
        MediaTimelineTrackKind kind,
        double frameRate,
        string typeName)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.XgesSegment.");
                    var mime = MimeFromPath(assetId, kind == MediaTimelineTrackKind.Audio ? "audio/mpeg" : "video/mp4");
                    var segment = new PublicationMediaSegment
                    {
                        Name = name,
                        TimelineStartSeconds = start,
                        TimelineDurationSeconds = duration,
                        SourceRate = frameRate,
                        DurationSeconds = sourceDuration,
                        TrimStartSeconds = inPoint,
                        TrimEndSeconds = inPoint + duration,
                        MimeType = mime,
                        SourceReference = new MediaSourceReference
                        {
                            Id = assetId,
                            Uri = assetId,
                            OriginalPath = UriPath(assetId),
                            MimeType = mime,
                            Missing = true,
                            Metadata = { ["extractable_type"] = Attribute(asset, "extractable-type-name") }
                        },
                        ImportMetadata =
                        {
                            ["source_format"] = "XGES",
                            ["type_name"] = typeName
                        }
                    };
                    if (kind == MediaTimelineTrackKind.Video)
                        segment.VideoLayers.Add(DefaultVideoLayer(name, inPoint, inPoint + duration));
                    return segment;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.XgesSegment failed: {exception.Message}");
            throw;
        }
    }

    private VideoProjectImportResult ImportOpenShot(byte[] bytes, string fileName)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.ImportOpenShot.");
                    using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions
                    {
                        AllowTrailingCommas = true,
                        CommentHandling = JsonCommentHandling.Skip,
                        MaxDepth = 256
                    });
                    var root = document.RootElement;
                    var issues = new List<InterchangeIssue>();
                    var project = new VideoProjectDocument
                    {
                        Name = JsonString(root, "name", Path.GetFileNameWithoutExtension(fileName)),
                        SourceFormat = "OpenShot Project",
                        SourceFormatVersion = JsonString(root, "version"),
                        FrameRate = OpenShotFrameRate(root),
                        Width = OpenShotProfileInt(root, "width", 1920),
                        Height = OpenShotProfileInt(root, "height", 1080)
                    };

                    var files = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                    foreach (var file in JsonArray(root, "files"))
                    {
                        var id = JsonString(file, "id");
                        if (!string.IsNullOrWhiteSpace(id)) files[id] = file;
                    }

                    var tracks = new Dictionary<int, MediaTimelineTrack>();
                    foreach (var clip in JsonArray(root, "clips"))
                    {
                        var layer = (int)Math.Round(JsonDouble(clip, "layer", 0));
                        var fileId = JsonString(clip, "file_id");
                        files.TryGetValue(fileId, out var file);
                        var path = JsonString(file, "path", JsonString(file, "url"));
                        var mediaType = JsonString(file, "media_type", "video");
                        var kind = mediaType.Equals("audio", StringComparison.OrdinalIgnoreCase)
                            ? MediaTimelineTrackKind.Audio
                            : MediaTimelineTrackKind.Video;
                        var trackKey = layer * 10 + (kind == MediaTimelineTrackKind.Audio ? 1 : 0);
                        if (!tracks.TryGetValue(trackKey, out var track))
                        {
                            track = new MediaTimelineTrack
                            {
                                Name = $"{kind} layer {layer + 1}",
                                Kind = kind,
                                Order = trackKey
                            };
                            tracks[trackKey] = track;
                        }

                        var position = JsonDouble(clip, "position", 0);
                        var start = JsonDouble(clip, "start", 0);
                        var end = JsonDouble(clip, "end", start + JsonDouble(file, "duration", 5));
                        var duration = Math.Max(.01, end - start);
                        var name = JsonString(clip, "title", JsonString(file, "name", Path.GetFileName(path)));
                        if (string.IsNullOrWhiteSpace(name)) name = $"Clip {track.Segments.Count + 1}";
                        var mime = MimeFromPath(path, kind == MediaTimelineTrackKind.Audio ? "audio/mpeg" : "video/mp4");
                        var segment = new PublicationMediaSegment
                        {
                            Name = name,
                            TimelineStartSeconds = position,
                            TimelineDurationSeconds = duration,
                            SourceRate = project.FrameRate,
                            DurationSeconds = Math.Max(end, JsonDouble(file, "duration", end)),
                            TrimStartSeconds = start,
                            TrimEndSeconds = end,
                            MimeType = mime,
                            SourceReference = new MediaSourceReference
                            {
                                Id = fileId,
                                Uri = path,
                                OriginalPath = path,
                                MimeType = mime,
                                Missing = true
                            },
                            ImportMetadata =
                            {
                                ["source_format"] = "OpenShot",
                                ["clip_id"] = JsonString(clip, "id")
                            }
                        };
                        if (kind == MediaTimelineTrackKind.Video)
                            segment.VideoLayers.Add(DefaultVideoLayer(name, start, end));
                        track.Segments.Add(segment);

                        if (JsonArray(clip, "effects").Any())
                            issues.Add(new InterchangeIssue(
                                InterchangeIssueSeverity.Loss,
                                "OPENSHOT_EFFECTS_RETAINED_AS_METADATA",
                                "OpenShot clip effects were detected but are not yet mapped to PublisherStudio live-filter parameters.",
                                name));
                    }

                    project.Tracks = tracks.Values.OrderBy(track => track.Order).ToList();
                    foreach (var transition in JsonArray(root, "transitions"))
                    {
                        var start = JsonDouble(transition, "position", 0);
                        var end = JsonDouble(transition, "end", start + JsonDouble(transition, "duration", .5));
                        project.Transitions.Add(new MediaTimelineTransition
                        {
                            Name = JsonString(transition, "title", "OpenShot transition"),
                            Kind = MediaTimelineTransitionKind.Dissolve,
                            TrackId = project.Tracks.FirstOrDefault()?.Id ?? Guid.Empty,
                            TimelineStartSeconds = start,
                            DurationSeconds = Math.Max(.01, end - start)
                        });
                    }
                    if (project.Transitions.Count > 0)
                        issues.Add(new InterchangeIssue(
                            InterchangeIssueSeverity.Warning,
                            "OPENSHOT_TRANSITIONS_APPROXIMATED",
                            "OpenShot transition timing was retained and mapped to canonical dissolves.",
                            fileName));

                    project.ActiveTrackId = project.Tracks.FirstOrDefault(track => track.Kind == MediaTimelineTrackKind.Video)?.Id
                        ?? project.Tracks.FirstOrDefault()?.Id
                        ?? Guid.Empty;
                    if (project.Tracks.Count == 0)
                        issues.Add(new InterchangeIssue(InterchangeIssueSeverity.Loss, "OPENSHOT_NO_CLIPS", "No importable OpenShot clips were found.", fileName));
                    return new VideoProjectImportResult { Project = project, Issues = issues };
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.ImportOpenShot failed: {exception.Message}");
            throw;
        }
    }

    private VideoProjectImportResult ImportEdl(byte[] bytes, string fileName)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.ImportEdl.");
                    var text = DecodeText(bytes);
                    var issues = new List<InterchangeIssue>();
                    var frameRate = ParseEdlRate(text, 24);
                    var project = new VideoProjectDocument
                    {
                        Name = EdlTitle(text, Path.GetFileNameWithoutExtension(fileName)),
                        SourceFormat = "CMX 3600 EDL",
                        FrameRate = frameRate
                    };
                    var track = new MediaTimelineTrack { Name = "EDL video", Kind = MediaTimelineTrackKind.Video, Order = 0 };
                    PublicationMediaSegment? lastSegment = null;
                    foreach (var rawLine in text.Replace("\r", string.Empty).Split('\n'))
                    {
                        var line = rawLine.TrimEnd();
                        var match = runtimePatterns.GetRegex(PublisherRuntimePattern.VideoEdlEvent).Match(line);
                        if (match.Success)
                        {
                            var eventNumber = match.Groups["event"].Value;
                            var reel = match.Groups["reel"].Value;
                            var transitionCode = match.Groups["transition"].Value;
                            var sourceIn = ParseTimecode(match.Groups["sourceIn"].Value, frameRate);
                            var sourceOut = ParseTimecode(match.Groups["sourceOut"].Value, frameRate);
                            var recordIn = ParseTimecode(match.Groups["recordIn"].Value, frameRate);
                            var recordOut = ParseTimecode(match.Groups["recordOut"].Value, frameRate);
                            var duration = Math.Max(1 / frameRate, recordOut - recordIn);
                            lastSegment = new PublicationMediaSegment
                            {
                                Name = $"Event {eventNumber}",
                                TimelineStartSeconds = recordIn,
                                TimelineDurationSeconds = duration,
                                SourceRate = frameRate,
                                DurationSeconds = Math.Max(sourceOut, sourceOut - sourceIn),
                                TrimStartSeconds = sourceIn,
                                TrimEndSeconds = Math.Max(sourceIn + 1 / frameRate, sourceOut),
                                MimeType = "video/mp4",
                                SourceReference = new MediaSourceReference
                                {
                                    Id = reel,
                                    ReelName = reel,
                                    Missing = true
                                },
                                ImportMetadata =
                                {
                                    ["source_format"] = "CMX 3600 EDL",
                                    ["event"] = eventNumber,
                                    ["transition"] = transitionCode
                                },
                                VideoLayers = [DefaultVideoLayer($"Event {eventNumber}", sourceIn, Math.Max(sourceIn + 1 / frameRate, sourceOut))]
                            };
                            track.Segments.Add(lastSegment);
                            if (!transitionCode.Equals("C", StringComparison.OrdinalIgnoreCase))
                            {
                                project.Transitions.Add(new MediaTimelineTransition
                                {
                                    Name = transitionCode,
                                    Kind = transitionCode.StartsWith("D", StringComparison.OrdinalIgnoreCase)
                                        ? MediaTimelineTransitionKind.Dissolve
                                        : MediaTimelineTransitionKind.Unknown,
                                    TrackId = track.Id,
                                    ToSegmentId = lastSegment.Id,
                                    TimelineStartSeconds = recordIn,
                                    DurationSeconds = Math.Min(duration, .5),
                                    Metadata = { ["cmx_code"] = transitionCode }
                                });
                            }
                            continue;
                        }

                        if (lastSegment is null) continue;
                        var clipName = runtimePatterns.GetRegex(PublisherRuntimePattern.VideoEdlClipName).Match(line);
                        if (clipName.Success)
                        {
                            var name = clipName.Groups["name"].Value.Trim();
                            if (!string.IsNullOrWhiteSpace(name)) lastSegment.Name = name;
                            continue;
                        }
                        var sourceFile = runtimePatterns.GetRegex(PublisherRuntimePattern.VideoEdlSourceFile).Match(line);
                        if (sourceFile.Success)
                        {
                            var path = sourceFile.Groups["path"].Value.Trim();
                            lastSegment.SourceReference.Uri = path;
                            lastSegment.SourceReference.OriginalPath = path;
                            lastSegment.SourceReference.MimeType = MimeFromPath(path, "video/mp4");
                            lastSegment.MimeType = lastSegment.SourceReference.MimeType;
                        }
                    }

                    project.Tracks.Add(track);
                    project.ActiveTrackId = track.Id;
                    issues.Add(new InterchangeIssue(
                        InterchangeIssueSeverity.Warning,
                        "EDL_RATE_ASSUMED",
                        $"CMX 3600 does not reliably carry a frame rate. This import used {frameRate:0.###} fps; verify timecode before cutting.",
                        fileName));
                    issues.Add(new InterchangeIssue(
                        InterchangeIssueSeverity.Loss,
                        "EDL_FEATURE_LIMIT",
                        "EDL preserves cuts, reel/source names, record timing and simple transition codes only; layers, filters, regions and most metadata are not represented by the format.",
                        fileName));
                    if (track.Segments.Count == 0)
                        issues.Add(new InterchangeIssue(InterchangeIssueSeverity.Loss, "EDL_NO_EVENTS", "No CMX event lines were recognized.", fileName));
                    return new VideoProjectImportResult { Project = project, Issues = issues };
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.ImportEdl failed: {exception.Message}");
            throw;
        }
    }

    private VideoProjectImportResult FinalizeResult(VideoProjectImportResult result, string fileName)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.FinalizeResult.");
                    var project = result.Project;
                    project.Name = string.IsNullOrWhiteSpace(project.Name) ? Path.GetFileNameWithoutExtension(fileName) : project.Name.Trim();
                    project.FrameRate = double.IsFinite(project.FrameRate) && project.FrameRate > 0 ? project.FrameRate : 30;
                    project.Width = Math.Clamp(project.Width, 1, 32768);
                    project.Height = Math.Clamp(project.Height, 1, 32768);
                    project.Tracks ??= [];
                    project.Transitions ??= [];
                    project.Markers ??= [];
                    project.Metadata ??= [];

                    foreach (var track in project.Tracks)
                    {
                        track.Id = track.Id == Guid.Empty ? Guid.NewGuid() : track.Id;
                        track.Name = string.IsNullOrWhiteSpace(track.Name) ? $"{track.Kind} track" : track.Name.Trim();
                        track.Segments ??= [];
                        track.Segments = track.Segments
                            .Where(segment => segment is not null)
                            .OrderBy(segment => segment.TimelineStartSeconds)
                            .ThenBy(segment => segment.Name, StringComparer.OrdinalIgnoreCase)
                            .ToList();
                        foreach (var segment in track.Segments)
                        {
                            segment.Id = segment.Id == Guid.Empty ? Guid.NewGuid() : segment.Id;
                            segment.Name = string.IsNullOrWhiteSpace(segment.Name) ? "Clip" : segment.Name.Trim();
                            segment.SourceReference ??= new MediaSourceReference();
                            segment.ImportMetadata ??= [];
                            segment.CutSections ??= [];
                            segment.VideoLayers ??= [];
                            segment.WaveformSamples ??= [];
                            segment.TimelineStartSeconds = Math.Max(0, Finite(segment.TimelineStartSeconds));
                            segment.TimelineDurationSeconds = Math.Max(.01, Finite(segment.TimelineDurationSeconds, segment.SourceLengthSeconds));
                            var normalizedSpeed = Finite(segment.Speed, 1);
                            segment.Speed = Math.Abs(normalizedSpeed) < .0001 ? 1 : normalizedSpeed;
                            segment.DurationSeconds = Math.Max(.01, Finite(segment.DurationSeconds, segment.TrimEndSeconds));
                            segment.TrimStartSeconds = Math.Clamp(Finite(segment.TrimStartSeconds), 0, segment.DurationSeconds);
                            segment.TrimEndSeconds = Math.Clamp(
                                Finite(segment.TrimEndSeconds, segment.DurationSeconds),
                                Math.Min(segment.DurationSeconds, segment.TrimStartSeconds + .01),
                                segment.DurationSeconds);
                            segment.SourceReference.Missing = !segment.IsGap && string.IsNullOrWhiteSpace(segment.DataUrl);
                            if (track.Kind == MediaTimelineTrackKind.Video && !segment.IsGap && segment.VideoLayers.Count == 0)
                                segment.VideoLayers.Add(DefaultVideoLayer(segment.Name, segment.TrimStartSeconds, segment.TrimEndSeconds));
                        }
                    }

                    if (project.ActiveTrackId == Guid.Empty || project.Tracks.All(track => track.Id != project.ActiveTrackId))
                        project.ActiveTrackId = project.Tracks.FirstOrDefault(track => track.Kind == MediaTimelineTrackKind.Video)?.Id
                            ?? project.Tracks.FirstOrDefault()?.Id
                            ?? Guid.Empty;

                    var missing = project.Tracks.SelectMany(track => track.Segments)
                        .Count(segment => !segment.IsGap && string.IsNullOrWhiteSpace(segment.DataUrl));
                    if (missing > 0)
                        result.Issues.Add(new InterchangeIssue(
                            InterchangeIssueSeverity.Warning,
                            "PROJECT_MEDIA_RELINK_REQUIRED",
                            $"{missing} imported clip reference{(missing == 1 ? string.Empty : "s")} need local media relinking before preview or rendering.",
                            fileName));

                    return result;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.FinalizeResult failed: {exception.Message}");
            throw;
        }
    }

    private XDocument ParseSafeXml(byte[] bytes)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.ParseSafeXml.");
                    using var stream = new MemoryStream(bytes, writable: false);
                    using var reader = XmlReader.Create(stream, new XmlReaderSettings
                    {
                        DtdProcessing = DtdProcessing.Prohibit,
                        XmlResolver = null,
                        MaxCharactersInDocument = 0,
                        IgnoreComments = false,
                        IgnoreWhitespace = false
                    });
                    return XDocument.Load(reader, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.ParseSafeXml failed: {exception.Message}");
            throw;
        }
    }

    private async Task<byte[]> ReadAllAsync(Stream source, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.ReadAllAsync.");
                    using var buffer = new MemoryStream();
                    await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
                    return buffer.ToArray();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.ReadAllAsync failed: {exception.Message}");
            throw;
        }
    }

    private byte[] ReadAll(Stream source)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.ReadAll.");
                    using var buffer = new MemoryStream();
                    source.CopyTo(buffer);
                    return buffer.ToArray();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.ReadAll failed: {exception.Message}");
            throw;
        }
    }

    private string NormalizeArchivePath(string path)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.NormalizeArchivePath.");
                    var normalized = (path ?? string.Empty).Replace('\\', '/').TrimStart('/');
                    if (string.IsNullOrWhiteSpace(normalized) || normalized.EndsWith('/')) return string.Empty;
                    if (normalized.Split('/').Any(part => part is ".." or "."))
                        throw new InvalidDataException($"Unsafe archive path '{path}'.");
                    return normalized;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.NormalizeArchivePath failed: {exception.Message}");
            throw;
        }
    }

    private string NormalizeOtioTarget(string targetUrl)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.NormalizeOtioTarget.");
                    var value = UriPath(targetUrl).Replace('\\', '/').TrimStart('/');
                    if (value.StartsWith("./", StringComparison.Ordinal)) value = value[2..];
                    return value;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.NormalizeOtioTarget failed: {exception.Message}");
            throw;
        }
    }

    private IEnumerable<JsonElement> JsonArray(JsonElement owner, string property)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.JsonArray.");
                    if (owner.ValueKind == JsonValueKind.Object
                        && owner.TryGetProperty(property, out var value)
                        && value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var child in value.EnumerateArray()) yield return child;
                    }
    
        }
        finally
        {
            logger.LogTrace($"Completed iterator VideoProjectImportService.JsonArray.");
        }
    }

    private string JsonString(JsonElement owner, string property, string fallback = "")
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.JsonString.");
                    if (owner.ValueKind != JsonValueKind.Object || !owner.TryGetProperty(property, out var value)) return fallback;
                    return value.ValueKind switch
                    {
                        JsonValueKind.String => value.GetString() ?? fallback,
                        JsonValueKind.Number => value.GetRawText(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        _ => fallback
                    };
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.JsonString failed: {exception.Message}");
            throw;
        }
    }

    private double JsonDouble(JsonElement owner, string property, double fallback = 0)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.JsonDouble.");
                    if (owner.ValueKind != JsonValueKind.Object || !owner.TryGetProperty(property, out var value)) return fallback;
                    if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number)) return number;
                    if (value.ValueKind == JsonValueKind.String
                        && double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number)) return number;
                    return fallback;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.JsonDouble failed: {exception.Message}");
            throw;
        }
    }

    private bool JsonBool(JsonElement owner, string property, bool fallback)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.JsonBool.");
                    if (owner.ValueKind != JsonValueKind.Object || !owner.TryGetProperty(property, out var value)) return fallback;
                    if (value.ValueKind == JsonValueKind.True) return true;
                    if (value.ValueKind == JsonValueKind.False) return false;
                    return fallback;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.JsonBool failed: {exception.Message}");
            throw;
        }
    }

    private string SchemaName(string schema)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.SchemaName.");
                    var separator = schema.IndexOf('.');
                    return separator > 0 ? schema[..separator] : schema;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.SchemaName failed: {exception.Message}");
            throw;
        }
    }

    private string SchemaVersion(string schema)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.SchemaVersion.");
                    var separator = schema.IndexOf('.');
                    return separator >= 0 && separator < schema.Length - 1 ? schema[(separator + 1)..] : string.Empty;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.SchemaVersion failed: {exception.Message}");
            throw;
        }
    }

    private double FindOtioRate(JsonElement root, double fallback)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.FindOtioRate.");
                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var property in root.EnumerateObject())
                        {
                            if (property.NameEquals("rate") && property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetDouble(out var rate) && rate > 0)
                                return rate;
                            var nested = FindOtioRate(property.Value, 0);
                            if (nested > 0) return nested;
                        }
                    }
                    else if (root.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in root.EnumerateArray())
                        {
                            var nested = FindOtioRate(item, 0);
                            if (nested > 0) return nested;
                        }
                    }
                    return fallback;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.FindOtioRate failed: {exception.Message}");
            throw;
        }
    }

    private double OtioTimeRangeStart(JsonElement owner, double fallbackRate)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.OtioTimeRangeStart.");
                    var range = owner;
                    if (owner.ValueKind == JsonValueKind.Object && owner.TryGetProperty("source_range", out var sourceRange)) range = sourceRange;
                    return OtioRationalSeconds(range, "start_time", fallbackRate);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.OtioTimeRangeStart failed: {exception.Message}");
            throw;
        }
    }

    private double OtioTimeRangeDuration(JsonElement owner, double fallbackRate)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.OtioTimeRangeDuration.");
                    var range = owner;
                    if (owner.ValueKind == JsonValueKind.Object && owner.TryGetProperty("source_range", out var sourceRange)) range = sourceRange;
                    return OtioRationalSeconds(range, "duration", fallbackRate);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.OtioTimeRangeDuration failed: {exception.Message}");
            throw;
        }
    }

    private double OtioRationalSeconds(JsonElement owner, string property, double fallbackRate)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.OtioRationalSeconds.");
                    if (owner.ValueKind != JsonValueKind.Object || !owner.TryGetProperty(property, out var time)) return 0;
                    var value = JsonDouble(time, "value", 0);
                    var rate = JsonDouble(time, "rate", fallbackRate);
                    return rate > 0 ? value / rate : 0;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.OtioRationalSeconds failed: {exception.Message}");
            throw;
        }
    }

    private double OtioSourceRate(JsonElement owner, double fallbackRate)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.OtioSourceRate.");
                    if (owner.ValueKind == JsonValueKind.Object
                        && owner.TryGetProperty("source_range", out var range)
                        && range.TryGetProperty("duration", out var duration))
                        return JsonDouble(duration, "rate", fallbackRate);
                    return fallbackRate;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.OtioSourceRate failed: {exception.Message}");
            throw;
        }
    }

    private string OtioMarkerColor(string color) {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.OtioMarkerColor.");
            return color.ToLowerInvariant() switch
    {
        "red" => "#ef4444",
        "orange" => "#f97316",
        "yellow" => "#eab308",
        "green" => "#22c55e",
        "cyan" => "#06b6d4",
        "blue" => "#3b82f6",
        "purple" => "#a855f7",
        "magenta" => "#d946ef",
        "pink" => "#ec4899",
        _ => "#f59e0b"
    };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.OtioMarkerColor failed: {exception.Message}");
            throw;
        }
    }

    private string Attribute(XElement? element, string name, string fallback = "")
        {
            try
            {
                logger.LogTrace($"Entering VideoProjectImportService.Attribute.");
                return element?.Attribute(name)?.Value ?? fallback;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"VideoProjectImportService.Attribute failed: {exception.Message}");
                throw;
            }
        }

    private double AttributeDouble(XElement? element, string name, double fallback)
        {
            try
            {
                logger.LogTrace($"Entering VideoProjectImportService.AttributeDouble.");
                return ParseDouble(Attribute(element, name), fallback);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"VideoProjectImportService.AttributeDouble failed: {exception.Message}");
                throw;
            }
        }

    private string Property(XElement? element, string name, string fallback = "")
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.Property.");
                    if (element is null) return fallback;
                    var property = element.Elements().FirstOrDefault(child => child.Name.LocalName == "property" && Attribute(child, "name") == name);
                    return property?.Value ?? fallback;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.Property failed: {exception.Message}");
            throw;
        }
    }

    private double PropertyDouble(XElement? element, string name, double fallback)
        {
            try
            {
                logger.LogTrace($"Entering VideoProjectImportService.PropertyDouble.");
                return ParseDouble(Property(element, name), fallback);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"VideoProjectImportService.PropertyDouble failed: {exception.Message}");
                throw;
            }
        }

    private double Ratio(double numerator, double denominator, double fallback)
        {
            try
            {
                logger.LogTrace($"Entering VideoProjectImportService.Ratio.");
                return denominator > 0 && numerator > 0 ? numerator / denominator : fallback;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"VideoProjectImportService.Ratio failed: {exception.Message}");
                throw;
            }
        }

    private double MltPosition(string value, double frameRate)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.MltPosition.");
                    if (string.IsNullOrWhiteSpace(value)) return 0;
                    if (value.Contains(':')) return ParseClock(value, frameRate);
                    return ParseDouble(value, 0) / Math.Max(.001, frameRate);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.MltPosition failed: {exception.Message}");
            throw;
        }
    }

    private double MltDuration(string value, double frameRate)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.MltDuration.");
                    if (string.IsNullOrWhiteSpace(value)) return 0;
                    if (value.Contains(':')) return ParseClock(value, frameRate);
                    return Math.Max(0, ParseDouble(value, 0) / Math.Max(.001, frameRate));
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.MltDuration failed: {exception.Message}");
            throw;
        }
    }

    private double ParseMltSpeed(string resource, XElement? producer)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.ParseMltSpeed.");
                    var speed = PropertyDouble(producer, "warp_speed", 1);
                    if (Math.Abs(speed) > .0001 && Math.Abs(speed - 1) > .0001) return speed;
                    var colon = resource.IndexOf(':');
                    if (resource.StartsWith("timewarp:", StringComparison.OrdinalIgnoreCase) && colon >= 0)
                    {
                        var slash = resource.IndexOf(':', colon + 1);
                        if (slash > colon && double.TryParse(resource[(colon + 1)..slash], NumberStyles.Float, CultureInfo.InvariantCulture, out speed))
                            return Math.Abs(speed) < .0001 ? 1 : speed;
                    }
                    return 1;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.ParseMltSpeed failed: {exception.Message}");
            throw;
        }
    }

    private double ParseCapsFrameRate(string serialized, double fallback)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.ParseCapsFrameRate.");
                    var match = Regex.Match(serialized ?? string.Empty, @"framerate=\(fraction\)(?<num>\d+)\/(?<den>\d+)", RegexOptions.IgnoreCase);
                    if (!match.Success) return fallback;
                    return Ratio(ParseDouble(match.Groups["num"].Value, 0), ParseDouble(match.Groups["den"].Value, 1), fallback);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.ParseCapsFrameRate failed: {exception.Message}");
            throw;
        }
    }

    private string PropertyFromSerialized(string serialized, string property, string fallback)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.PropertyFromSerialized.");
                    if (string.IsNullOrWhiteSpace(serialized)) return fallback;
                    var match = Regex.Match(serialized, $@"(?:^|,\s*){Regex.Escape(property)}=\([^)]*\)(?<value>[^,;]+)", RegexOptions.IgnoreCase);
                    return match.Success ? match.Groups["value"].Value.Trim().Trim('"') : fallback;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.PropertyFromSerialized failed: {exception.Message}");
            throw;
        }
    }

    private double Nanoseconds(double value) {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.Nanoseconds.");
            return value / 1_000_000_000d;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.Nanoseconds failed: {exception.Message}");
            throw;
        }
    }

    private double OpenShotFrameRate(JsonElement root)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.OpenShotFrameRate.");
                    if (root.TryGetProperty("profile", out var profile))
                    {
                        if (profile.TryGetProperty("fps", out var fps))
                        {
                            var num = JsonDouble(fps, "num", JsonDouble(fps, "numerator", 0));
                            var den = JsonDouble(fps, "den", JsonDouble(fps, "denominator", 1));
                            return Ratio(num, den, 30);
                        }
                        var direct = JsonDouble(profile, "fps", 0);
                        if (direct > 0) return direct;
                    }
                    return 30;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.OpenShotFrameRate failed: {exception.Message}");
            throw;
        }
    }

    private int OpenShotProfileInt(JsonElement root, string property, int fallback)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.OpenShotProfileInt.");
                    if (root.TryGetProperty("profile", out var profile))
                        return (int)Math.Max(1, JsonDouble(profile, property, fallback));
                    return fallback;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.OpenShotProfileInt failed: {exception.Message}");
            throw;
        }
    }

    private double ParseEdlRate(string text, double fallback)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.ParseEdlRate.");
                    var match = Regex.Match(text, @"(?im)^\s*\*?\s*(?:FPS|FRAME\s*RATE)\s*[:=]\s*(?<rate>\d+(?:\.\d+)?)\s*$");
                    return match.Success ? ParseDouble(match.Groups["rate"].Value, fallback) : fallback;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.ParseEdlRate failed: {exception.Message}");
            throw;
        }
    }

    private string EdlTitle(string text, string fallback)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.EdlTitle.");
                    var match = Regex.Match(text, @"(?im)^\s*TITLE\s*:\s*(?<title>.+?)\s*$");
                    return match.Success ? match.Groups["title"].Value.Trim() : fallback;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.EdlTitle failed: {exception.Message}");
            throw;
        }
    }

    private double ParseTimecode(string value, double frameRate)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.ParseTimecode.");
                    var parts = value.Split(':', ';');
                    if (parts.Length != 4) return 0;
                    var hours = ParseDouble(parts[0], 0);
                    var minutes = ParseDouble(parts[1], 0);
                    var seconds = ParseDouble(parts[2], 0);
                    var frames = ParseDouble(parts[3], 0);
                    return hours * 3600 + minutes * 60 + seconds + frames / Math.Max(.001, frameRate);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.ParseTimecode failed: {exception.Message}");
            throw;
        }
    }

    private double ParseClock(string value, double frameRate)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.ParseClock.");
                    var normalized = value.Trim();
                    if (TimeSpan.TryParse(normalized, CultureInfo.InvariantCulture, out var time)) return time.TotalSeconds;
                    return ParseTimecode(normalized, frameRate);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.ParseClock failed: {exception.Message}");
            throw;
        }
    }

    private double ParseDouble(string? value, double fallback)
        {
            try
            {
                logger.LogTrace($"Entering VideoProjectImportService.ParseDouble.");
                return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && double.IsFinite(parsed)
            ? parsed
            : fallback;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"VideoProjectImportService.ParseDouble failed: {exception.Message}");
                throw;
            }
        }

    private double Finite(double value, double fallback = 0) {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.Finite.");
            return double.IsFinite(value) ? value : fallback;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.Finite failed: {exception.Message}");
            throw;
        }
    }

    private string UriPath(string? uri)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.UriPath.");
                    var value = uri?.Trim() ?? string.Empty;
                    if (value.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
                        && Uri.TryCreate(value, UriKind.Absolute, out var parsed))
                        return Uri.UnescapeDataString(parsed.LocalPath);
                    return Uri.UnescapeDataString(value.Replace("file://", string.Empty, StringComparison.OrdinalIgnoreCase));
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.UriPath failed: {exception.Message}");
            throw;
        }
    }

    private string MimeFromPath(string? path, string fallback)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.MimeFromPath.");
                    var extension = Path.GetExtension(UriPath(path)).ToLowerInvariant();
                    return extension switch
                    {
                        ".mp4" or ".m4v" => "video/mp4",
                        ".webm" => fallback.StartsWith("audio/", StringComparison.Ordinal) ? "audio/webm" : "video/webm",
                        ".ogv" or ".ogg" => fallback.StartsWith("audio/", StringComparison.Ordinal) ? "audio/ogg" : "video/ogg",
                        ".mov" => "video/quicktime",
                        ".mkv" => "video/x-matroska",
                        ".avi" => "video/x-msvideo",
                        ".mp3" => "audio/mpeg",
                        ".wav" or ".bwf" => "audio/wav",
                        ".flac" => "audio/flac",
                        ".aac" => "audio/aac",
                        ".m4a" => "audio/mp4",
                        ".opus" => "audio/ogg",
                        ".png" => "image/png",
                        ".jpg" or ".jpeg" => "image/jpeg",
                        ".webp" => "image/webp",
                        _ => fallback
                    };
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.MimeFromPath failed: {exception.Message}");
            throw;
        }
    }

    private string DataUrl(string mimeType, byte[] bytes)
        {
            try
            {
                logger.LogTrace($"Entering VideoProjectImportService.DataUrl.");
                return $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"VideoProjectImportService.DataUrl failed: {exception.Message}");
                throw;
            }
        }

    private VideoEffectLayer DefaultVideoLayer(string name, double start, double end) {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.DefaultVideoLayer.");
            return new()
    {
        Name = string.IsNullOrWhiteSpace(name) ? "Base video" : $"{name} · base",
        HasTemporalRange = true,
        TemporalStartSeconds = Math.Max(0, start),
        TemporalEndSeconds = Math.Max(start, end),
        Region = new VideoFrameRegion { Name = "Full frame" }
    };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.DefaultVideoLayer failed: {exception.Message}");
            throw;
        }
    }

    private string DecodeText(byte[] bytes)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.DecodeText.");
                    if (bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf)
                        return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
                    return Encoding.UTF8.GetString(bytes);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.DecodeText failed: {exception.Message}");
            throw;
        }
    }

    private string FormatBytes(long bytes)
    {
        try
        {
            logger.LogTrace($"Entering VideoProjectImportService.FormatBytes.");
                    if (bytes >= 1024L * 1024 * 1024) return $"{bytes / (1024d * 1024 * 1024):0.#} GB";
                    if (bytes >= 1024L * 1024) return $"{bytes / (1024d * 1024):0.#} MB";
                    return $"{bytes / 1024d:0.#} KB";
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"VideoProjectImportService.FormatBytes failed: {exception.Message}");
            throw;
        }
    }

    private sealed record EmbeddedMedia(string Path, string MimeType, byte[] Bytes);



}
