using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;

namespace PublisherStudio.Services;

/// <summary>
/// Discovers font families installed on the computer that runs PublisherStudio.
/// The application is offline-first, so the catalog never contacts a remote service.
/// Users may still type a font family that is not in this catalog.
/// </summary>
public sealed class SystemFontCatalog
{
    private static readonly string[] EmergencyFallbackFonts =
    [
        "Arial", "Calibri", "Cambria", "Courier New", "Georgia", "Segoe UI", "Tahoma", "Times New Roman", "Verdana"
    ];

    private readonly object _sync = new();
    private IReadOnlyList<string>? _fontFamilies;

    public IReadOnlyList<string> FontFamilies
    {
        get
        {
            lock (_sync)
                return _fontFamilies ??= DiscoverFontFamilies();
        }
    }

    public IReadOnlyList<string> Refresh()
    {
        lock (_sync)
            return _fontFamilies = DiscoverFontFamilies();
    }

    internal static IReadOnlyList<string> DiscoverFontFamilies()
    {
        var families = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ReadFontConfigFamilies(families);

        foreach (var directory in EnumerateFontDirectories())
            ReadFontDirectory(directory, families);

        if (families.Count == 0)
        {
            foreach (var fallback in EmergencyFallbackFonts) families.Add(fallback);
        }

        return families
            .Where(IsUsableFamilyName)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<string> EnumerateFontDirectories()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Add(string? path)
        {
            if (!string.IsNullOrWhiteSpace(path)) paths.Add(path);
        }

        if (OperatingSystem.IsWindows())
        {
            var windowsDirectory = Environment.GetEnvironmentVariable("WINDIR");
            if (string.IsNullOrWhiteSpace(windowsDirectory))
            {
                var systemDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
                windowsDirectory = string.IsNullOrWhiteSpace(systemDirectory) ? null : Directory.GetParent(systemDirectory)?.FullName;
            }
            Add(string.IsNullOrWhiteSpace(windowsDirectory) ? null : Path.Combine(windowsDirectory, "Fonts"));
            var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            Add(string.IsNullOrWhiteSpace(localData) ? null : Path.Combine(localData, "Microsoft", "Windows", "Fonts"));
        }
        else if (OperatingSystem.IsMacOS())
        {
            Add("/System/Library/Fonts");
            Add("/Library/Fonts");
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            Add(string.IsNullOrWhiteSpace(home) ? null : Path.Combine(home, "Library", "Fonts"));
        }
        else
        {
            Add("/usr/share/fonts");
            Add("/usr/local/share/fonts");
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            Add(string.IsNullOrWhiteSpace(home) ? null : Path.Combine(home, ".fonts"));
            Add(string.IsNullOrWhiteSpace(home) ? null : Path.Combine(home, ".local", "share", "fonts"));
        }

        return paths;
    }

    private static void ReadFontConfigFamilies(ISet<string> families)
    {
        if (OperatingSystem.IsWindows()) return;
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "fc-list",
                Arguments = "--format=%{family}\\n",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            if (process is null) return;

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(4000))
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return;
            }

            var output = outputTask.GetAwaiter().GetResult();
            _ = errorTask.GetAwaiter().GetResult();
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                foreach (var alias in line.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    AddFamily(families, alias);
            }
        }
        catch
        {
            // Fontconfig is optional. The OpenType parser below remains the portable fallback.
        }
    }

    private static void ReadFontDirectory(string directory, ISet<string> families)
    {
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

    private static void ReadOpenTypeFamilies(string path, ISet<string> families)
    {
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

    private static void ReadOpenTypeFace(BinaryReader reader, long faceOffset, ISet<string> families)
    {
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

    private static int ScoreNameRecord(NameRecord record)
    {
        var score = record.NameId == 16 ? 100 : 50;
        if (record.PlatformId == 3) score += 30;
        else if (record.PlatformId == 0) score += 20;
        else if (record.PlatformId == 1) score += 10;
        if (record.LanguageId == 0x0409) score += 15;
        else if (record.LanguageId == 0) score += 5;
        return score;
    }

    private static string DecodeName(ushort platformId, ushort encodingId, byte[] bytes)
    {
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

    private static void AddFamily(ISet<string> families, string? value)
    {
        var name = value?.Replace("\0", string.Empty, StringComparison.Ordinal).Trim();
        if (IsUsableFamilyName(name)) families.Add(name!);
    }

    private static bool IsUsableFamilyName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 256) return false;
        return value[0] is not '.' and not '@' && !value.Any(char.IsControl);
    }

    private static ushort ReadUInt16BigEndian(BinaryReader reader)
    {
        Span<byte> bytes = stackalloc byte[2];
        if (reader.Read(bytes) != bytes.Length) throw new EndOfStreamException();
        return BinaryPrimitives.ReadUInt16BigEndian(bytes);
    }

    private static uint ReadUInt32BigEndian(BinaryReader reader)
    {
        Span<byte> bytes = stackalloc byte[4];
        if (reader.Read(bytes) != bytes.Length) throw new EndOfStreamException();
        return BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }

    private sealed record NameRecord(
        ushort PlatformId,
        ushort EncodingId,
        ushort LanguageId,
        ushort NameId,
        ushort Length,
        ushort Offset);
}
