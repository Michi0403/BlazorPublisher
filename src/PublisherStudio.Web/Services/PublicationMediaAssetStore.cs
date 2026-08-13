using System.Collections.Concurrent;
using System.Security.Cryptography;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services;

/// <summary>
/// Keeps embedded publication media on the server and exposes it through a small ranged HTTP URL.
/// This prevents multi-megabyte data URLs from being copied into every Blazor render batch.
/// The original data URL remains in the document model so saved projects stay self-contained.
/// </summary>
/// <param name="mediaData">Media data value supplied to the publication media asset operation and used when producing its result.</param>
/// <param name="elementTraversal">Element traversal value supplied to the publication media asset operation and used when producing its result.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class PublicationMediaAssetStore(
    PublicationMediaData mediaData,
    PublicationElementTraversal elementTraversal,
    ILogger<PublicationMediaAssetStore> logger)
{
    /// <summary>
    /// Represents a media asset helper type nested within <see cref="PublicationMediaAssetStore"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    /// <param name="Bytes">Bytes value supplied to the publication media asset operation and used when producing its result.</param>
    /// <param name="MimeType">Mime type value supplied to the publication media asset operation and used when producing its result.</param>
    /// <param name="Version">Version value supplied to the publication media asset operation and used when producing its result.</param>
    /// <param name="SourceKey">Source key value supplied to the publication media asset operation and used when producing its result.</param>
    /// <param name="LastAccessUtc">Last access utc value supplied to the publication media asset operation and used when producing its result.</param>
    private sealed record MediaAsset(byte[] Bytes, string MimeType, string Version, string SourceKey, DateTimeOffset LastAccessUtc);

    /// <summary>
    /// Stores the in-memory assets collection maintained internally by <see cref="PublicationMediaAssetStore"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, MediaAsset> _assets = new();

    /// <summary>
    /// Retrieves or register in the publication media asset persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationMediaAssetStore"/>.
    /// </summary>
    /// <param name="media">Media value supplied to the publication media asset operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string GetOrRegister(PublicationMediaElement media)
    {
        try
        {
            logger.LogTrace($"Entering PublicationMediaAssetStore.GetOrRegister.");
                    var first = media.EffectiveSegments.FirstOrDefault();
                    return first is null
                        ? Register(media.Id, media.DataUrl, media.MimeType)
                        : GetOrRegister(first);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationMediaAssetStore.GetOrRegister failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Retrieves or register in the publication media asset persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationMediaAssetStore"/>.
    /// </summary>
    /// <param name="segment">Segment value supplied to the publication media asset operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string GetOrRegister(PublicationMediaSegment segment)
        {
            try
            {
                logger.LogTrace($"Entering PublicationMediaAssetStore.GetOrRegister.");
                return Register(segment.Id, segment.DataUrl, segment.MimeType);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"PublicationMediaAssetStore.GetOrRegister failed: {exception.Message}");
                throw;
            }
        }

    /// <summary>
    /// Performs register in the publication media asset persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationMediaAssetStore"/>.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="source">Source value supplied to the publication media asset operation and used when producing its result.</param>
    /// <param name="declaredMimeType">Declared mime type value supplied to the publication media asset operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string Register(Guid id, string? source, string? declaredMimeType)
    {
        try
        {
            logger.LogTrace($"Entering PublicationMediaAssetStore.Register.");
                    if (id == Guid.Empty || string.IsNullOrWhiteSpace(source)) return source ?? string.Empty;
                    if (!source.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return source;

                    var sourceKey = CreateSourceKey(source, declaredMimeType);
                    if (_assets.TryGetValue(id, out var cached) && cached.SourceKey == sourceKey)
                    {
                        _assets[id] = cached with { LastAccessUtc = DateTimeOffset.UtcNow };
                        return BuildUrl(id, cached.Version);
                    }

                    if (!TryDecodeDataUrl(source, declaredMimeType, out var bytes, out var mimeType)) return source;
                    return RegisterBytes(id, bytes, mimeType, sourceKey);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationMediaAssetStore.Register failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Registers bytes in the publication media asset persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationMediaAssetStore"/>.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="bytes">Bytes value supplied to the publication media asset operation and used when producing its result.</param>
    /// <param name="mimeType">Mime type value supplied to the publication media asset operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    public string RegisterBytes(Guid id, byte[] bytes, string? mimeType)
        {
            try
            {
                logger.LogTrace($"Entering PublicationMediaAssetStore.RegisterBytes.");
                return RegisterBytes(id, bytes, mimeType, sourceKey: null);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"PublicationMediaAssetStore.RegisterBytes failed: {exception.Message}");
                throw;
            }
        }

    /// <summary>
    /// Registers bytes in the publication media asset persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationMediaAssetStore"/>.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="bytes">Bytes value supplied to the publication media asset operation and used when producing its result.</param>
    /// <param name="mimeType">Mime type value supplied to the publication media asset operation and used when producing its result.</param>
    /// <param name="sourceKey">Source key value supplied to the publication media asset operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string RegisterBytes(Guid id, byte[] bytes, string? mimeType, string? sourceKey)
    {
        try
        {
            logger.LogTrace($"Entering PublicationMediaAssetStore.RegisterBytes.");
                    if (id == Guid.Empty || bytes.Length == 0) return string.Empty;
                    var normalizedMime = mediaData.NormalizeMimeType(mimeType, "application/octet-stream");
                    var version = CreateVersion(bytes, normalizedMime);
                    var asset = new MediaAsset(bytes, normalizedMime, version, sourceKey ?? $"bytes:{version}", DateTimeOffset.UtcNow);

                    _assets[id] = asset;
                    return BuildUrl(id, version);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationMediaAssetStore.RegisterBytes failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Attempts to get in the publication media asset persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationMediaAssetStore"/>.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="bytes">Bytes value supplied to the publication media asset operation and used when producing its result.</param>
    /// <param name="mimeType">Mime type value supplied to the publication media asset operation and used when producing its result.</param>
    /// <param name="version">Version value supplied to the publication media asset operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool TryGet(Guid id, out byte[] bytes, out string mimeType, out string version)
    {
        try
        {
            logger.LogTrace($"Entering PublicationMediaAssetStore.TryGet.");
                    if (_assets.TryGetValue(id, out var asset))
                    {
                        _assets[id] = asset with { LastAccessUtc = DateTimeOffset.UtcNow };
                        bytes = asset.Bytes;
                        mimeType = asset.MimeType;
                        version = asset.Version;
                        return true;
                    }

                    bytes = [];
                    mimeType = "application/octet-stream";
                    version = string.Empty;
                    return false;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationMediaAssetStore.TryGet failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs copy in the publication media asset persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationMediaAssetStore"/>.
    /// </summary>
    /// <param name="sourceId">Identifier of the source to use for this operation.</param>
    /// <param name="targetId">Identifier of the target to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool Copy(Guid sourceId, Guid targetId)
    {
        try
        {
            logger.LogTrace($"Entering PublicationMediaAssetStore.Copy.");
                    if (sourceId == Guid.Empty || targetId == Guid.Empty || !_assets.TryGetValue(sourceId, out var asset)) return false;
                    _assets[targetId] = asset with { LastAccessUtc = DateTimeOffset.UtcNow };
                    return true;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationMediaAssetStore.Copy failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Registers document in the publication media asset persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationMediaAssetStore"/>.
    /// </summary>
    /// <param name="document">Document value supplied to the publication media asset operation and used when producing its result.</param>
    public void RegisterDocument(PublicationDocument document)
    {
        try
        {
            logger.LogTrace($"Entering PublicationMediaAssetStore.RegisterDocument.");
                    foreach (var media in elementTraversal.Descendants(document).OfType<PublicationMediaElement>())
                    {
                        foreach (var segment in media.EffectiveSegments)
                            GetOrRegister(segment);
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationMediaAssetStore.RegisterDocument failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs remove in the publication media asset persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationMediaAssetStore"/>.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    public void Remove(Guid id) {
        try
        {
            logger.LogTrace($"Entering PublicationMediaAssetStore.Remove.");
            _assets.TryRemove(id, out _);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationMediaAssetStore.Remove failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Builds URL in the publication media asset persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationMediaAssetStore"/>.
    /// </summary>
    /// <param name="id">Identifier of the resource to use for this operation.</param>
    /// <param name="version">Version value supplied to the publication media asset operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string BuildUrl(Guid id, string version)
        {
            try
            {
                logger.LogTrace($"Entering PublicationMediaAssetStore.BuildUrl.");
                return $"/api/assets/media/{id:D}?v={Uri.EscapeDataString(version)}";
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"PublicationMediaAssetStore.BuildUrl failed: {exception.Message}");
                throw;
            }
        }

    /// <summary>
    /// Creates source key in the publication media asset persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationMediaAssetStore"/>.
    /// </summary>
    /// <param name="source">Source value supplied to the publication media asset operation and used when producing its result.</param>
    /// <param name="mimeType">Mime type value supplied to the publication media asset operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string CreateSourceKey(string source, string? mimeType)
    {
        try
        {
            logger.LogTrace($"Entering PublicationMediaAssetStore.CreateSourceKey.");
                    var firstLength = Math.Min(192, source.Length);
                    var lastLength = Math.Min(192, Math.Max(0, source.Length - firstLength));
                    var first = source[..firstLength];
                    var last = lastLength > 0 ? source[^lastLength..] : string.Empty;
                    var sample = $"{first}|{source.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)}|{mimeType ?? string.Empty}|{last}";
                    return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(sample)))[..16].ToLowerInvariant();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationMediaAssetStore.CreateSourceKey failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Creates version in the publication media asset persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationMediaAssetStore"/>.
    /// </summary>
    /// <param name="bytes">Bytes value supplied to the publication media asset operation and used when producing its result.</param>
    /// <param name="mimeType">Mime type value supplied to the publication media asset operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string CreateVersion(byte[] bytes, string mimeType)
    {
        try
        {
            logger.LogTrace($"Entering PublicationMediaAssetStore.CreateVersion.");
                    using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                    hash.AppendData(bytes.AsSpan(0, Math.Min(bytes.Length, 64 * 1024)));
                    if (bytes.Length > 64 * 1024)
                        hash.AppendData(bytes.AsSpan(Math.Max(0, bytes.Length - 64 * 1024)));
                    hash.AppendData(System.Text.Encoding.UTF8.GetBytes($"|{bytes.Length}|{mimeType}"));
                    return Convert.ToHexString(hash.GetHashAndReset())[..16].ToLowerInvariant();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationMediaAssetStore.CreateVersion failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Attempts to decode data URL in the publication media asset persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationMediaAssetStore"/>.
    /// </summary>
    /// <param name="source">Source value supplied to the publication media asset operation and used when producing its result.</param>
    /// <param name="declaredMimeType">Declared mime type value supplied to the publication media asset operation and used when producing its result.</param>
    /// <param name="bytes">Bytes value supplied to the publication media asset operation and used when producing its result.</param>
    /// <param name="mimeType">Mime type value supplied to the publication media asset operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool TryDecodeDataUrl(string source, string? declaredMimeType, out byte[] bytes, out string mimeType)
    {
        try
        {
            logger.LogTrace($"Entering PublicationMediaAssetStore.TryDecodeDataUrl.");
                    bytes = [];
                    mimeType = mediaData.NormalizeMimeType(declaredMimeType, "application/octet-stream");
                    if (!source.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return false;

                    var marker = source.LastIndexOf(";base64,", StringComparison.OrdinalIgnoreCase);
                    if (marker < 5) return false;
                    mimeType = mediaData.NormalizeMimeType(source.Substring(5, marker - 5), mimeType);
                    try
                    {
                        bytes = Convert.FromBase64String(source[(marker + 8)..]);
                        return bytes.Length > 0;
                    }
                    catch (FormatException)
                    {
                        bytes = [];
                        return false;
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"PublicationMediaAssetStore.TryDecodeDataUrl failed: {exception.Message}");
            throw;
        }
    }
}
