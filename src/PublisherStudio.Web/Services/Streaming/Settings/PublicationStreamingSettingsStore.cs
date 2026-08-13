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
    /// Stores the internal gate state used by <see cref="PublicationStreamingSettingsStore"/> while executing its surrounding workflow.
    /// </summary>
    private readonly object _gate = new();
    /// <summary>
    /// Stores the data protector dependency used by <see cref="PublicationStreamingSettingsStore"/> to delegate that application responsibility to its owning collaborator.
    /// </summary>
    private readonly IDataProtector _protector;
    /// <summary>
    /// Stores the internal file path state used by <see cref="PublicationStreamingSettingsStore"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _filePath;
    /// <summary>
    /// Stores the in-memory JSON collection maintained internally by <see cref="PublicationStreamingSettingsStore"/> for its current workflow state.
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
    /// Initializes a new <see cref="PublicationStreamingSettingsStore"/> instance and captures the dependencies or initial state required by its publication streaming settings workflow.
    /// </summary>
    /// <param name="protectionProvider">Data protection provider dependency used by the publication streaming settings workflow to provide the corresponding application capability.</param>
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
    /// Attempts to load in the publication streaming settings persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationStreamingSettingsStore"/>.
    /// </summary>
    /// <param name="publicationId">Identifier of the publication to use for this operation.</param>
    /// <param name="settings">Settings containing the caller-supplied values that control this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
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
    /// Loads or default in the publication streaming settings persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationStreamingSettingsStore"/>.
    /// </summary>
    /// <param name="publicationId">Identifier of the publication to use for this operation.</param>
    /// <returns>The publication streaming settings produced by the operation.</returns>
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
    /// Performs save in the publication streaming settings persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationStreamingSettingsStore"/>.
    /// </summary>
    /// <param name="publicationId">Identifier of the publication to use for this operation.</param>
    /// <param name="settings">Settings containing the caller-supplied values that control this operation.</param>
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
    /// Loads core in the publication streaming settings persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationStreamingSettingsStore"/>.
    /// </summary>
    /// <returns>The dictionary GUID publication streaming settings produced by the operation.</returns>
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
    /// Persists core in the publication streaming settings persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationStreamingSettingsStore"/>.
    /// </summary>
    /// <param name="values">Values value supplied to the publication streaming settings operation and used when producing its result.</param>
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
    /// Performs clone in the publication streaming settings persistence workflow while keeping storage-specific behavior contained within <see cref="PublicationStreamingSettingsStore"/>.
    /// </summary>
    /// <param name="settings">Settings containing the caller-supplied values that control this operation.</param>
    /// <returns>The publication streaming settings produced by the operation.</returns>
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
