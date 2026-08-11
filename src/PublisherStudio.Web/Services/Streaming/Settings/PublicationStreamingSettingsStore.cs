using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Streaming.Settings;

/// <summary>
/// Keeps publication streaming configuration on the local machine. These settings are
/// deliberately excluded from exported publication files so templates and shared files
/// cannot carry output routing, recording paths, LAN access configuration, or hotkeys to
/// another workstation.
/// </summary>
public sealed class PublicationStreamingSettingsStore
{
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly object _gate = new();
    private readonly IDataProtector _protector;
    private readonly string _filePath;
    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };
    private Dictionary<Guid, PublicationStreamingSettings>? _cache;

    /// <summary>
    /// Runs the publication streaming settings store operation.
    /// </summary>
    public PublicationStreamingSettingsStore(IDataProtectionProvider protectionProvider)
    {
        _protector = protectionProvider.CreateProtector("PublisherStudio.PublicationStreamingSettings.v1");
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PublisherStudio");
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "publication-streaming-settings.dat");
    }

    /// <summary>
    /// Attempts to load.
    /// </summary>
    public bool TryLoad(Guid publicationId, out PublicationStreamingSettings settings)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PublicationStreamingSettingsStore.TryLoad failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Loads or default.
    /// </summary>
    public PublicationStreamingSettings LoadOrDefault(Guid publicationId) {
    try
    {
        return TryLoad(publicationId, out var settings) ? settings : new PublicationStreamingSettings();
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PublicationStreamingSettingsStore.LoadOrDefault failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the save operation.
    /// </summary>
    public void Save(Guid publicationId, PublicationStreamingSettings settings)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PublicationStreamingSettingsStore.Save failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Loads core.
    /// </summary>
    private Dictionary<Guid, PublicationStreamingSettings> LoadCore()
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PublicationStreamingSettingsStore.LoadCore failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Saves core.
    /// </summary>
    private void SaveCore(Dictionary<Guid, PublicationStreamingSettings> values)
    {
    try
    {
            var json = JsonSerializer.Serialize(values, _json);
            var protectedPayload = _protector.Protect(json);
            var temporary = _filePath + ".tmp";
            File.WriteAllText(temporary, protectedPayload);
            File.Move(temporary, _filePath, overwrite: true);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PublicationStreamingSettingsStore.SaveCore failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Runs the clone operation.
    /// </summary>
    private PublicationStreamingSettings Clone(PublicationStreamingSettings settings) {
    try
    {
        return JsonSerializer.Deserialize<PublicationStreamingSettings>(
            JsonSerializer.Serialize(settings, _json), _json) ?? new PublicationStreamingSettings();
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PublicationStreamingSettingsStore.Clone failed: {__serviceMethodException}");
        throw;
    }
}
}
