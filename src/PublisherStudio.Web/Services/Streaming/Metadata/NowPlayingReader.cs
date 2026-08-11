using PublisherStudio.BusinessObjects;
using PublisherStudio.Services.Configuration;

namespace PublisherStudio.Services.Streaming.Metadata;

/// <summary>
/// Provides now playing reader operations.
/// </summary>
public sealed class NowPlayingReader(
    IPublisherRuntimePolicyDataService runtimePolicy,
    ILogger<NowPlayingReader> logger)
{

    /// <summary>
    /// Runs the read operation.
    /// </summary>
    public object? Read(string directory)
    {
        try
        {
            if (!Directory.Exists(directory)) return null;
            var file = new DirectoryInfo(directory)
                .EnumerateFiles()
                .Where(item => runtimePolicy.GetCollection(PublisherRuntimeCollection.NowPlayingExtensions).Contains(item.Extension, StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(item => item.LastWriteTimeUtc)
                .FirstOrDefault();
            if (file is null) return null;
            var tags = file.Extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase) ? ReadMp3(file.FullName) : new AudioTags();
            var fallbackTitle = Path.GetFileNameWithoutExtension(file.Name);
            return new
            {
                fileName = file.Name,
                fullPath = file.FullName,
                title = string.IsNullOrWhiteSpace(tags.Title) ? fallbackTitle : tags.Title,
                artist = tags.Artist,
                album = tags.Album,
                year = tags.Year,
                track = tags.Track,
                genre = tags.Genre,
                coverImage = tags.CoverImage,
                lastWriteUtc = file.LastWriteTimeUtc
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not read now-playing metadata from '{directory}'.");
            return null;
        }
    }

    /// <summary>
    /// Reads mp3.
    /// </summary>
    private AudioTags ReadMp3(string path)
    {
        try
        {
            logger.LogTrace($"Entering NowPlayingReader.ReadMp3.");
                    var tags = new AudioTags();
                    using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    if (stream.Length >= 10)
                    {
                        Span<byte> header = stackalloc byte[10];
                        stream.ReadExactly(header);
                        if (header[0] == (byte)'I' && header[1] == (byte)'D' && header[2] == (byte)'3')
                        {
                            var version = header[3];
                            var tagSize = Synchsafe(header[6], header[7], header[8], header[9]);
                            ReadId3V2(stream, Math.Min(tagSize, (int)Math.Min(stream.Length - 10, 16 * 1024 * 1024)), version, tags);
                        }
                    }
                    if (stream.Length >= 128)
                    {
                        stream.Position = stream.Length - 128;
                        var legacy = new byte[128];
                        stream.ReadExactly(legacy);
                        if (legacy[0] == (byte)'T' && legacy[1] == (byte)'A' && legacy[2] == (byte)'G')
                        {
                            tags.Title = First(tags.Title, DecodeLatin1(legacy.AsSpan(3, 30)));
                            tags.Artist = First(tags.Artist, DecodeLatin1(legacy.AsSpan(33, 30)));
                            tags.Album = First(tags.Album, DecodeLatin1(legacy.AsSpan(63, 30)));
                            tags.Year = First(tags.Year, DecodeLatin1(legacy.AsSpan(93, 4)));
                            if (string.IsNullOrWhiteSpace(tags.Track) && legacy[125] == 0 && legacy[126] != 0) tags.Track = legacy[126].ToString();
                            if (string.IsNullOrWhiteSpace(tags.Genre) && legacy[127] != 255) tags.Genre = legacy[127].ToString();
                        }
                    }
                    return tags;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"NowPlayingReader.ReadMp3 failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Reads id3 v2.
    /// </summary>
    private void ReadId3V2(Stream stream, int tagSize, byte version, AudioTags tags)
    {
        try
        {
            logger.LogTrace($"Entering NowPlayingReader.ReadId3V2.");
                    var remaining = tagSize;
                    var frameHeaderSize = version == 2 ? 6 : 10;
                    while (remaining >= frameHeaderSize)
                    {
                        var header = new byte[frameHeaderSize];
                        stream.ReadExactly(header);
                        remaining -= frameHeaderSize;
                        if (header.All(value => value == 0)) break;
                        var frameId = System.Text.Encoding.ASCII.GetString(header, 0, version == 2 ? 3 : 4);
                        var size = version switch
                        {
                            2 => header[3] << 16 | header[4] << 8 | header[5],
                            4 => Synchsafe(header[4], header[5], header[6], header[7]),
                            _ => ReadBigEndianInt(header.AsSpan(4, 4))
                        };
                        if (size <= 0 || size > remaining || size > 8 * 1024 * 1024) break;
                        var payload = new byte[size];
                        stream.ReadExactly(payload);
                        remaining -= size;
                        switch (frameId)
                        {
                            case "TIT2" or "TT2": tags.Title = First(tags.Title, DecodeTextFrame(payload)); break;
                            case "TPE1" or "TP1": tags.Artist = First(tags.Artist, DecodeTextFrame(payload)); break;
                            case "TALB" or "TAL": tags.Album = First(tags.Album, DecodeTextFrame(payload)); break;
                            case "TYER" or "TDRC" or "TYE": tags.Year = First(tags.Year, DecodeTextFrame(payload)); break;
                            case "TRCK" or "TRK": tags.Track = First(tags.Track, DecodeTextFrame(payload)); break;
                            case "TCON" or "TCO": tags.Genre = First(tags.Genre, DecodeTextFrame(payload)); break;
                            case "APIC" or "PIC": tags.CoverImage = First(tags.CoverImage, DecodePictureFrame(payload, version)); break;
                        }
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"NowPlayingReader.ReadId3V2 failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the decode text frame operation.
    /// </summary>
    private string DecodeTextFrame(byte[] payload)
    {
        try
        {
            logger.LogTrace($"Entering NowPlayingReader.DecodeTextFrame.");
                    if (payload.Length < 2) return string.Empty;
                    return DecodeEncodedText(payload[0], payload.AsSpan(1));
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"NowPlayingReader.DecodeTextFrame failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the decode picture frame operation.
    /// </summary>
    private string DecodePictureFrame(byte[] payload, byte version)
    {
        try
        {
            logger.LogTrace($"Entering NowPlayingReader.DecodePictureFrame.");
                    if (payload.Length < 8) return string.Empty;
                    var encoding = payload[0];
                    var index = 1;
                    string mime;
                    if (version == 2)
                    {
                        mime = payload.Length >= 4 ? System.Text.Encoding.ASCII.GetString(payload, 1, 3) : "jpeg";
                        mime = mime.Equals("PNG", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
                        index = 4;
                    }
                    else
                    {
                        var mimeEnd = Array.IndexOf(payload, (byte)0, index);
                        if (mimeEnd < 0) return string.Empty;
                        mime = System.Text.Encoding.ASCII.GetString(payload, index, mimeEnd - index);
                        index = mimeEnd + 1;
                    }
                    if (index >= payload.Length) return string.Empty;
                    index++; // picture type
                    index = SkipTerminatedText(payload, index, encoding);
                    if (index >= payload.Length) return string.Empty;
                    return $"data:{(string.IsNullOrWhiteSpace(mime) ? "image/jpeg" : mime)};base64,{Convert.ToBase64String(payload.AsSpan(index))}";
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"NowPlayingReader.DecodePictureFrame failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the skip terminated text operation.
    /// </summary>
    private int SkipTerminatedText(byte[] bytes, int index, byte encoding)
    {
        try
        {
            logger.LogTrace($"Entering NowPlayingReader.SkipTerminatedText.");
                    var doubleNull = encoding is 1 or 2;
                    while (index < bytes.Length)
                    {
                        if (!doubleNull && bytes[index] == 0) return index + 1;
                        if (doubleNull && index + 1 < bytes.Length && bytes[index] == 0 && bytes[index + 1] == 0) return index + 2;
                        index += doubleNull ? 2 : 1;
                    }
                    return bytes.Length;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"NowPlayingReader.SkipTerminatedText failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the decode encoded text operation.
    /// </summary>
    private string DecodeEncodedText(byte encoding, ReadOnlySpan<byte> bytes)
    {
        try
        {
            var value = encoding switch
            {
                0 => System.Text.Encoding.Latin1.GetString(bytes),
                1 => System.Text.Encoding.Unicode.GetString(bytes),
                2 => System.Text.Encoding.BigEndianUnicode.GetString(bytes),
                3 => System.Text.Encoding.UTF8.GetString(bytes),
                _ => System.Text.Encoding.UTF8.GetString(bytes)
            };
            return value.Trim('\0', ' ', '\r', '\n', '\t', '\ufeff');
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not decode now-playing text metadata.");
            return string.Empty;
        }
    }

    /// <summary>
    /// Runs the decode latin1 operation.
    /// </summary>
    private string DecodeLatin1(ReadOnlySpan<byte> bytes) {
        try
        {
            logger.LogTrace($"Entering NowPlayingReader.DecodeLatin1.");
            return System.Text.Encoding.Latin1.GetString(bytes).Trim('\0', ' ', '\r', '\n', '\t');
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"NowPlayingReader.DecodeLatin1 failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Runs the synchsafe operation.
    /// </summary>
    private int Synchsafe(byte a, byte b, byte c, byte d) {
        try
        {
            logger.LogTrace($"Entering NowPlayingReader.Synchsafe.");
            return (a & 0x7f) << 21 | (b & 0x7f) << 14 | (c & 0x7f) << 7 | d & 0x7f;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"NowPlayingReader.Synchsafe failed: {exception.Message}");
            throw;
        }
    }
    /// <summary>
    /// Reads big endian int.
    /// </summary>
    private int ReadBigEndianInt(ReadOnlySpan<byte> bytes) {
        try
        {
            logger.LogTrace($"Entering NowPlayingReader.ReadBigEndianInt.");
            return bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"NowPlayingReader.ReadBigEndianInt failed: {exception.Message}");
            throw;
        }
    }
    /// <summary>
    /// Runs the first operation.
    /// </summary>
    private string First(string current, string fallback) {
        try
        {
            logger.LogTrace($"Entering NowPlayingReader.First.");
            return string.IsNullOrWhiteSpace(current) ? fallback : current;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"NowPlayingReader.First failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Represents an audio tags.
    /// </summary>
    private sealed class AudioTags
    {
        /// <summary>
        /// Gets or sets title.
        /// </summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets artist.
        /// </summary>
        public string Artist { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets album.
        /// </summary>
        public string Album { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets year.
        /// </summary>
        public string Year { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets track.
        /// </summary>
        public string Track { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets genre.
        /// </summary>
        public string Genre { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets cover image.
        /// </summary>
        public string CoverImage { get; set; } = string.Empty;
    }
}
