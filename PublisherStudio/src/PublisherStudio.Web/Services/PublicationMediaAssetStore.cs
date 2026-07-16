using System.Collections.Concurrent;
using System.Security.Cryptography;
using PublisherStudio.Domain;

namespace PublisherStudio.Services;

/// <summary>
/// Keeps embedded publication media on the server and exposes it through a small ranged HTTP URL.
/// This prevents multi-megabyte data URLs from being copied into every Blazor render batch.
/// The original data URL remains in the document model so saved projects stay self-contained.
/// </summary>
public sealed class PublicationMediaAssetStore
{
    private sealed record MediaAsset(byte[] Bytes, string MimeType, string Version, string SourceKey, DateTimeOffset LastAccessUtc);

    private readonly ConcurrentDictionary<Guid, MediaAsset> _assets = new();

    public string GetOrRegister(PublicationMediaElement media)
        => Register(media.Id, media.DataUrl, media.MimeType);

    public string Register(Guid id, string? source, string? declaredMimeType)
    {
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

    public string RegisterBytes(Guid id, byte[] bytes, string? mimeType)
        => RegisterBytes(id, bytes, mimeType, sourceKey: null);

    private string RegisterBytes(Guid id, byte[] bytes, string? mimeType, string? sourceKey)
    {
        if (id == Guid.Empty || bytes.Length == 0) return string.Empty;
        var normalizedMime = PublicationMediaData.NormalizeMimeType(mimeType, "application/octet-stream");
        var version = CreateVersion(bytes, normalizedMime);
        var asset = new MediaAsset(bytes, normalizedMime, version, sourceKey ?? $"bytes:{version}", DateTimeOffset.UtcNow);

        _assets[id] = asset;
        return BuildUrl(id, version);
    }

    public bool TryGet(Guid id, out byte[] bytes, out string mimeType, out string version)
    {
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

    public bool Copy(Guid sourceId, Guid targetId)
    {
        if (sourceId == Guid.Empty || targetId == Guid.Empty || !_assets.TryGetValue(sourceId, out var asset)) return false;
        _assets[targetId] = asset with { LastAccessUtc = DateTimeOffset.UtcNow };
        return true;
    }

    public void RegisterDocument(PublicationDocument document)
    {
        foreach (var media in document.Pages.SelectMany(page => page.Elements).OfType<PublicationMediaElement>())
            Register(media.Id, media.DataUrl, media.MimeType);
    }

    public void Remove(Guid id) => _assets.TryRemove(id, out _);

    private static string BuildUrl(Guid id, string version)
        => $"/api/assets/media/{id:D}?v={Uri.EscapeDataString(version)}";

    private static string CreateSourceKey(string source, string? mimeType)
    {
        var firstLength = Math.Min(192, source.Length);
        var lastLength = Math.Min(192, Math.Max(0, source.Length - firstLength));
        var first = source[..firstLength];
        var last = lastLength > 0 ? source[^lastLength..] : string.Empty;
        var sample = $"{first}|{source.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)}|{mimeType ?? string.Empty}|{last}";
        return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(sample)))[..16].ToLowerInvariant();
    }

    private static string CreateVersion(byte[] bytes, string mimeType)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(bytes.AsSpan(0, Math.Min(bytes.Length, 64 * 1024)));
        if (bytes.Length > 64 * 1024)
            hash.AppendData(bytes.AsSpan(Math.Max(0, bytes.Length - 64 * 1024)));
        hash.AppendData(System.Text.Encoding.UTF8.GetBytes($"|{bytes.Length}|{mimeType}"));
        return Convert.ToHexString(hash.GetHashAndReset())[..16].ToLowerInvariant();
    }

    private static bool TryDecodeDataUrl(string source, string? declaredMimeType, out byte[] bytes, out string mimeType)
    {
        bytes = [];
        mimeType = PublicationMediaData.NormalizeMimeType(declaredMimeType, "application/octet-stream");
        if (!source.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return false;

        var marker = source.LastIndexOf(";base64,", StringComparison.OrdinalIgnoreCase);
        if (marker < 5) return false;
        mimeType = PublicationMediaData.NormalizeMimeType(source.Substring(5, marker - 5), mimeType);
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
}
