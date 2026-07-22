using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using PublisherStudio.Domain;

namespace PublisherStudio.Services;

/// <summary>
/// Keeps publication streaming configuration on the local machine. These settings are
/// deliberately excluded from exported publication files so templates and shared files
/// cannot carry output routing, recording paths, LAN access configuration, or hotkeys to
/// another workstation.
/// </summary>
public sealed class PublicationStreamingSettingsStore
{
    private readonly object _gate = new();
    private readonly IDataProtector _protector;
    private readonly string _filePath;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };
    private Dictionary<Guid, PublicationStreamingSettings>? _cache;

    public PublicationStreamingSettingsStore(IDataProtectionProvider protectionProvider)
    {
        _protector = protectionProvider.CreateProtector("PublisherStudio.PublicationStreamingSettings.v1");
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PublisherStudio");
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "publication-streaming-settings.dat");
    }

    public bool TryLoad(Guid publicationId, out PublicationStreamingSettings settings)
    {
        lock (_gate)
        {
            var values = LoadCore();
            if (publicationId != Guid.Empty && values.TryGetValue(publicationId, out var stored))
            {
                settings = Clone(stored);
                return true;
            }
        }

        settings = new PublicationStreamingSettings();
        return false;
    }

    public PublicationStreamingSettings LoadOrDefault(Guid publicationId) =>
        TryLoad(publicationId, out var settings) ? settings : new PublicationStreamingSettings();

    public void Save(Guid publicationId, PublicationStreamingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (publicationId == Guid.Empty) return;

        lock (_gate)
        {
            var values = LoadCore();
            values[publicationId] = Clone(settings);
            SaveCore(values);
        }
    }

    private Dictionary<Guid, PublicationStreamingSettings> LoadCore()
    {
        if (_cache is not null) return _cache;
        if (!File.Exists(_filePath)) return _cache = [];

        try
        {
            var protectedPayload = File.ReadAllText(_filePath);
            var json = _protector.Unprotect(protectedPayload);
            return _cache = JsonSerializer.Deserialize<Dictionary<Guid, PublicationStreamingSettings>>(json, _json) ?? [];
        }
        catch
        {
            var backup = _filePath + ".invalid-" + DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
            try { File.Move(_filePath, backup, overwrite: true); } catch { }
            return _cache = [];
        }
    }

    private void SaveCore(Dictionary<Guid, PublicationStreamingSettings> values)
    {
        var json = JsonSerializer.Serialize(values, _json);
        var protectedPayload = _protector.Protect(json);
        var temporary = _filePath + ".tmp";
        File.WriteAllText(temporary, protectedPayload);
        File.Move(temporary, _filePath, overwrite: true);
    }

    private PublicationStreamingSettings Clone(PublicationStreamingSettings settings) =>
        JsonSerializer.Deserialize<PublicationStreamingSettings>(
            JsonSerializer.Serialize(settings, _json), _json) ?? new PublicationStreamingSettings();
}
