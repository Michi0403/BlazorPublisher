using System.Collections.Concurrent;
using System.Security.Cryptography;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services;

/// <summary>
/// Keeps embedded publication media on the server and exposes it through a small ranged HTTP URL.
/// This prevents multi-megabyte data URLs from being copied into every Blazor render batch.
/// The original data URL remains in the document model so saved projects stay self-contained.
/// </summary>
public sealed class PublicationMediaAssetStore(
    PublicationMediaData mediaData,
    PublicationElementTraversal elementTraversal,
    ILogger<PublicationMediaAssetStore> logger)
{
    /// <summary>
    /// Represents a media asset.
    /// </summary>
    private sealed record MediaAsset(byte[] Bytes, string MimeType, string Version, string SourceKey, DateTimeOffset LastAccessUtc);

    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, MediaAsset> _assets = new();

    /// <summary>
    /// Gets or register.
    /// </summary>
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
    /// Gets or register.
    /// </summary>
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
    /// Runs the register operation.
    /// </summary>
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
    /// Registers bytes.
    /// </summary>
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
    /// Registers bytes.
    /// </summary>
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
    /// Attempts to get.
    /// </summary>
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
    /// Runs the copy operation.
    /// </summary>
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
    /// Registers document.
    /// </summary>
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
    /// Runs the remove operation.
    /// </summary>
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
    /// Builds URL.
    /// </summary>
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
    /// Creates source key.
    /// </summary>
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
    /// Creates version.
    /// </summary>
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
    /// Attempts to decode data URL.
    /// </summary>
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
