using System.Buffers.Binary;
using System.Text;

namespace PublisherStudio.Services;

/// <summary>
/// Discovers font families installed on the computer that runs PublisherStudio.
/// The application is offline-first, so the catalog never contacts a remote service.
/// Users may still type a font family that is not in this catalog.
/// </summary>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
/// <param name="platform">Publisher platform runtime service dependency used by the system font workflow to provide the corresponding application capability.</param>
public sealed class SystemFontCatalog(
    IPublisherPlatformRuntimeService platform,
    ILogger<SystemFontCatalog> logger)
{
    /// <summary>
    /// Stores the internal emergency fallback fonts state used by <see cref="SystemFontCatalog"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string[] EmergencyFallbackFonts =
    [
        "Arial", "Calibri", "Cambria", "Courier New", "Georgia", "Segoe UI", "Tahoma", "Times New Roman", "Verdana"
    ];

    /// <summary>
    /// Stores the internal sync state used by <see cref="SystemFontCatalog"/> while executing its surrounding workflow.
    /// </summary>
    private readonly object _sync = new();
    /// <summary>
    /// Stores the in-memory font families collection maintained internally by <see cref="SystemFontCatalog"/> for its current workflow state.
    /// </summary>
    private IReadOnlyList<string>? _fontFamilies;

    /// <summary>
    /// Gets the font families collection maintained or exposed by this system font instance for downstream processing.
    /// </summary>
    /// <value>The font families value exposed by <see cref="SystemFontCatalog"/>.</value>
    public IReadOnlyList<string> FontFamilies
    {
        get
        {
            lock (_sync)
                return _fontFamilies ??= DiscoverFontFamilies();
        }
    }

    /// <summary>
    /// Performs refresh in the system font directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    public IReadOnlyList<string> Refresh()
    {
        try
        {
            logger.LogTrace($"Entering SystemFontCatalog.Refresh.");
                    lock (_sync)
                        return _fontFamilies = DiscoverFontFamilies();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"SystemFontCatalog.Refresh failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Discovers font families in the system font directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
    internal IReadOnlyList<string> DiscoverFontFamilies()
    {
        try
        {
            logger.LogTrace($"Entering SystemFontCatalog.DiscoverFontFamilies.");
                    var families = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var platformFamily in platform.EnumeratePlatformFontFamilies())
                        AddFamily(families, platformFamily);

                    foreach (var directory in platform.EnumerateFontDirectories())
                        ReadFontDirectory(directory, families);

                    // Always merge the safe local fallback set. A damaged or partially readable font
                    // file must never leave the WordArt or RichEdit dropdown empty.
                    foreach (var fallback in EmergencyFallbackFonts) AddFamily(families, fallback);

                    return families
                        .Where(IsUsableFamilyName)
                        .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
                        .ThenBy(name => name, StringComparer.Ordinal)
                        .ToArray();
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"SystemFontCatalog.DiscoverFontFamilies failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Reads font directory in the system font directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="directory">Directory value supplied to the system font operation and used when producing its result.</param>
    /// <param name="families">String dependency used by the system font workflow to provide the corresponding application capability.</param>
    private void ReadFontDirectory(string directory, ISet<string> families)
    {
        try
        {
            logger.LogTrace($"Entering SystemFontCatalog.ReadFontDirectory.");
                    if (!Directory.Exists(directory)) return;
                    try
                    {
                        var options = new EnumerationOptions
                        {
                            RecurseSubdirectories = true,
                            IgnoreInaccessible = true,
                            ReturnSpecialDirectories = false
                        };
                        foreach (var path in Directory.EnumerateFiles(directory, "*", options))
                        {
                            var extension = Path.GetExtension(path);
                            if (!extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase)
                                && !extension.Equals(".otf", StringComparison.OrdinalIgnoreCase)
                                && !extension.Equals(".ttc", StringComparison.OrdinalIgnoreCase)
                                && !extension.Equals(".otc", StringComparison.OrdinalIgnoreCase)) continue;
                            ReadOpenTypeFamilies(path, families);
                        }
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (IOException) { }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"SystemFontCatalog.ReadFontDirectory failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Reads open type families in the system font directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="path">Path value supplied to the system font operation and used when producing its result.</param>
    /// <param name="families">String dependency used by the system font workflow to provide the corresponding application capability.</param>
    private void ReadOpenTypeFamilies(string path, ISet<string> families)
    {
        try
        {
            logger.LogTrace($"Entering SystemFontCatalog.ReadOpenTypeFamilies.");
                    try
                    {
                        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);
                        if (stream.Length < 12) return;

                        var signature = ReadUInt32BigEndian(reader);
                        if (signature == 0x74746366) // ttcf
                        {
                            _ = ReadUInt32BigEndian(reader); // collection version
                            var count = (int)Math.Min(ReadUInt32BigEndian(reader), 2048u);
                            var offsets = new uint[count];
                            for (var index = 0; index < count; index++) offsets[index] = ReadUInt32BigEndian(reader);
                            foreach (var offset in offsets) ReadOpenTypeFace(reader, offset, families);
                        }
                        else
                        {
                            ReadOpenTypeFace(reader, 0, families);
                        }
                    }
                    catch (EndOfStreamException) { }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                    catch (ArgumentException) { }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"SystemFontCatalog.ReadOpenTypeFamilies failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Reads open type face in the system font directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="reader">Reader value supplied to the system font operation and used when producing its result.</param>
    /// <param name="faceOffset">Face offset value supplied to the system font operation and used when producing its result.</param>
    /// <param name="families">String dependency used by the system font workflow to provide the corresponding application capability.</param>
    private void ReadOpenTypeFace(BinaryReader reader, long faceOffset, ISet<string> families)
    {
        try
        {
            logger.LogTrace($"Entering SystemFontCatalog.ReadOpenTypeFace.");
                    var stream = reader.BaseStream;
                    if (faceOffset < 0 || faceOffset + 12 > stream.Length) return;
                    stream.Position = faceOffset;
                    _ = ReadUInt32BigEndian(reader);
                    var tableCount = Math.Min(ReadUInt16BigEndian(reader), (ushort)4096);
                    stream.Position += 6;

                    uint nameTableOffset = 0;
                    uint nameTableLength = 0;
                    for (var index = 0; index < tableCount; index++)
                    {
                        if (stream.Position + 16 > stream.Length) return;
                        var tag = ReadUInt32BigEndian(reader);
                        _ = ReadUInt32BigEndian(reader); // checksum
                        var offset = ReadUInt32BigEndian(reader);
                        var length = ReadUInt32BigEndian(reader);
                        if (tag == 0x6E616D65) // name
                        {
                            nameTableOffset = offset;
                            nameTableLength = length;
                        }
                    }

                    var nameTableEnd = (long)nameTableOffset + nameTableLength;
                    if (nameTableOffset == 0 || nameTableLength < 6 || nameTableEnd > stream.Length) return;
                    stream.Position = nameTableOffset;
                    _ = ReadUInt16BigEndian(reader); // format
                    var recordCount = Math.Min(ReadUInt16BigEndian(reader), (ushort)8192);
                    var stringOffset = ReadUInt16BigEndian(reader);
                    var records = new List<NameRecord>(recordCount);
                    for (var index = 0; index < recordCount; index++)
                    {
                        if (stream.Position + 12 > nameTableEnd) break;
                        records.Add(new NameRecord(
                            ReadUInt16BigEndian(reader),
                            ReadUInt16BigEndian(reader),
                            ReadUInt16BigEndian(reader),
                            ReadUInt16BigEndian(reader),
                            ReadUInt16BigEndian(reader),
                            ReadUInt16BigEndian(reader)));
                    }

                    var candidates = records
                        .Where(record => record.NameId is 1 or 16 && record.Length > 0)
                        .OrderByDescending(ScoreNameRecord)
                        .ToArray();
                    foreach (var record in candidates)
                    {
                        var absoluteOffset = (long)nameTableOffset + stringOffset + record.Offset;
                        if (absoluteOffset < 0 || absoluteOffset + record.Length > nameTableEnd) continue;
                        stream.Position = absoluteOffset;
                        var bytes = reader.ReadBytes(record.Length);
                        if (bytes.Length != record.Length) continue;
                        var value = DecodeName(record.PlatformId, record.EncodingId, bytes);
                        if (!IsUsableFamilyName(value)) continue;
                        AddFamily(families, value);
                        return;
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"SystemFontCatalog.ReadOpenTypeFace failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs score name record in the system font directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="record">Record value supplied to the system font operation and used when producing its result.</param>
    /// <returns>The int produced by the operation.</returns>
    private int ScoreNameRecord(NameRecord record)
    {
        try
        {
            logger.LogTrace($"Entering SystemFontCatalog.ScoreNameRecord.");
                    var score = record.NameId == 16 ? 100 : 50;
                    if (record.PlatformId == 3) score += 30;
                    else if (record.PlatformId == 0) score += 20;
                    else if (record.PlatformId == 1) score += 10;
                    if (record.LanguageId == 0x0409) score += 15;
                    else if (record.LanguageId == 0) score += 5;
                    return score;
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"SystemFontCatalog.ScoreNameRecord failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Performs decode name in the system font directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="platformId">Identifier of the platform to use for this operation.</param>
    /// <param name="encodingId">Identifier of the encoding to use for this operation.</param>
    /// <param name="bytes">Bytes value supplied to the system font operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string DecodeName(ushort platformId, ushort encodingId, byte[] bytes)
    {
        try
        {
            logger.LogTrace($"Entering SystemFontCatalog.DecodeName.");
                    try
                    {
                        var value = platformId is 0 or 3
                            ? Encoding.BigEndianUnicode.GetString(bytes)
                            : platformId == 1
                                ? Encoding.Latin1.GetString(bytes)
                                : encodingId is 0 or 1 or 10
                                    ? Encoding.BigEndianUnicode.GetString(bytes)
                                    : Encoding.UTF8.GetString(bytes);
                        return value.Replace("\0", string.Empty, StringComparison.Ordinal).Trim();
                    }
                    catch
                    {
                        return string.Empty;
                    }
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"SystemFontCatalog.DecodeName failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds family in the system font directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="families">String dependency used by the system font workflow to provide the corresponding application capability.</param>
    /// <param name="value">Value value supplied to the system font operation and used when producing its result.</param>
    private void AddFamily(ISet<string> families, string? value)
    {
        try
        {
            logger.LogTrace($"Entering SystemFontCatalog.AddFamily.");
                    var name = value?.Replace("\0", string.Empty, StringComparison.Ordinal).Trim();
                    if (IsUsableFamilyName(name)) families.Add(name!);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"SystemFontCatalog.AddFamily failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Determines whether usable family name in the system font directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="value">Value value supplied to the system font operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsUsableFamilyName(string? value)
    {
        try
        {
            logger.LogTrace($"Entering SystemFontCatalog.IsUsableFamilyName.");
                    if (string.IsNullOrWhiteSpace(value) || value.Length > 256) return false;
                    return value[0] is not '.' and not '@' && !value.Any(char.IsControl);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"SystemFontCatalog.IsUsableFamilyName failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Reads u int16 big endian in the system font directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="reader">Reader value supplied to the system font operation and used when producing its result.</param>
    /// <returns>The ushort produced by the operation.</returns>
    private ushort ReadUInt16BigEndian(BinaryReader reader)
    {
        try
        {
            logger.LogTrace($"Entering SystemFontCatalog.ReadUInt16BigEndian.");
                    Span<byte> bytes = stackalloc byte[2];
                    if (reader.Read(bytes) != bytes.Length) throw new EndOfStreamException();
                    return BinaryPrimitives.ReadUInt16BigEndian(bytes);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"SystemFontCatalog.ReadUInt16BigEndian failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Reads u int32 big endian in the system font directory so callers observe a consistent, authoritative runtime view.
    /// </summary>
    /// <param name="reader">Reader value supplied to the system font operation and used when producing its result.</param>
    /// <returns>The uint produced by the operation.</returns>
    private uint ReadUInt32BigEndian(BinaryReader reader)
    {
        try
        {
            logger.LogTrace($"Entering SystemFontCatalog.ReadUInt32BigEndian.");
                    Span<byte> bytes = stackalloc byte[4];
                    if (reader.Read(bytes) != bytes.Length) throw new EndOfStreamException();
                    return BinaryPrimitives.ReadUInt32BigEndian(bytes);
    
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"SystemFontCatalog.ReadUInt32BigEndian failed: {exception.Message}");
            throw;
        }
    }

    /// <summary>
    /// Represents name state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
    /// </summary>
    /// <param name="PlatformId">Identifier of the platform to use for this operation.</param>
    /// <param name="EncodingId">Identifier of the encoding to use for this operation.</param>
    /// <param name="LanguageId">Identifier of the language to use for this operation.</param>
    /// <param name="NameId">Identifier of the name to use for this operation.</param>
    /// <param name="Length">Length value supplied to the system font operation and used when producing its result.</param>
    /// <param name="Offset">Offset value supplied to the system font operation and used when producing its result.</param>
    private sealed record NameRecord(
        ushort PlatformId,
        ushort EncodingId,
        ushort LanguageId,
        ushort NameId,
        ushort Length,
        ushort Offset);
}
