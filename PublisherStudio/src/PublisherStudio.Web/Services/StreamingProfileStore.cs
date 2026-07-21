using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using PublisherStudio.Domain;

namespace PublisherStudio.Services;

public sealed class StreamingProfileStore
{
    private readonly IDataProtector _protector;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath;
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public StreamingProfileStore(IDataProtectionProvider protectionProvider)
    {
        _protector = protectionProvider.CreateProtector("PublisherStudio.StreamingProfiles.v1");
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PublisherStudio", "Streaming");
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "profiles.json");
    }

    public async Task<StreamingMachineSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await LoadCoreAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<StreamingProviderProfile> SaveProviderAsync(StreamingProviderProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var stored = await LoadStoredAsync(cancellationToken);
            var existing = stored.Providers.FirstOrDefault(item => item.Id == profile.Id);
            var secret = string.IsNullOrWhiteSpace(profile.Secret)
                ? existing?.ProtectedSecret ?? string.Empty
                : _protector.Protect(profile.Secret);
            var chatSecret = string.IsNullOrWhiteSpace(profile.ChatSecret)
                ? existing?.ProtectedChatSecret ?? string.Empty
                : _protector.Protect(profile.ChatSecret);
            var replacement = new StoredProviderProfile
            {
                Id = profile.Id == Guid.Empty ? Guid.NewGuid() : profile.Id,
                Name = string.IsNullOrWhiteSpace(profile.Name) ? "Streaming profile" : profile.Name.Trim(),
                Provider = profile.Provider,
                Transport = profile.Transport,
                Endpoint = profile.Endpoint?.Trim() ?? string.Empty,
                ChannelId = profile.ChannelId?.Trim() ?? string.Empty,
                AccountName = profile.AccountName?.Trim() ?? string.Empty,
                ProtectedSecret = secret,
                ChatEnabled = profile.ChatEnabled,
                ProtectedChatSecret = chatSecret,
                Enabled = profile.Enabled
            };
            if (existing is null) stored.Providers.Add(replacement);
            else stored.Providers[stored.Providers.IndexOf(existing)] = replacement;
            await SaveStoredAsync(stored, cancellationToken);
            return ToPublic(replacement);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteProviderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var stored = await LoadStoredAsync(cancellationToken);
            stored.Providers.RemoveAll(item => item.Id == id);
            await SaveStoredAsync(stored, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveMachineOptionsAsync(StreamingMachineSettings settings, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var stored = await LoadStoredAsync(cancellationToken);
            stored.FfmpegPath = settings.FfmpegPath?.Trim() ?? string.Empty;
            stored.DefaultRecordingDirectory = settings.DefaultRecordingDirectory?.Trim() ?? string.Empty;
            stored.MediaHostPort = Math.Clamp(settings.MediaHostPort, 1024, 65535);
            stored.HardwareEncoder = settings.HardwareEncoder;
            stored.Devices = (settings.Devices ?? [])
                .Select(profile => new StreamingDeviceProfile
                {
                    Id = profile.Id == Guid.Empty ? Guid.NewGuid() : profile.Id,
                    Name = string.IsNullOrWhiteSpace(profile.Name) ? profile.Kind.ToString() : profile.Name.Trim(),
                    Kind = profile.Kind,
                    DeviceId = profile.DeviceId?.Trim() ?? string.Empty,
                    AudioDeviceId = profile.AudioDeviceId?.Trim() ?? string.Empty,
                    ApplicationId = profile.ApplicationId?.Trim() ?? string.Empty,
                    WindowTitle = profile.WindowTitle?.Trim() ?? string.Empty,
                    CaptureBackend = profile.CaptureBackend,
                    NativeBackend = profile.NativeBackend?.Trim() ?? string.Empty,
                    UseDeviceTimestamps = profile.UseDeviceTimestamps
                })
                .GroupBy(profile => profile.Id)
                .Select(group => group.First())
                .ToList();
            await SaveStoredAsync(stored, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<string?> ResolveSecretAsync(Guid profileId, CancellationToken cancellationToken = default) =>
        ResolveProtectedValueAsync(profileId, chatSecret: false, cancellationToken: cancellationToken);

    public Task<string?> ResolveChatSecretAsync(Guid profileId, CancellationToken cancellationToken = default) =>
        ResolveProtectedValueAsync(profileId, chatSecret: true, cancellationToken: cancellationToken);

    private async Task<string?> ResolveProtectedValueAsync(Guid profileId, bool chatSecret, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var storedProfile = (await LoadStoredAsync(cancellationToken)).Providers.FirstOrDefault(item => item.Id == profileId);
            var protectedValue = chatSecret ? storedProfile?.ProtectedChatSecret : storedProfile?.ProtectedSecret;
            if (string.IsNullOrWhiteSpace(protectedValue)) return null;
            try { return _protector.Unprotect(protectedValue); }
            catch { return null; }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<StreamingMachineSettings> LoadCoreAsync(CancellationToken cancellationToken)
    {
        var stored = await LoadStoredAsync(cancellationToken);
        return new StreamingMachineSettings
        {
            Providers = stored.Providers.Select(ToPublic).ToList(),
            Devices = stored.Devices ?? [],
            FfmpegPath = stored.FfmpegPath,
            DefaultRecordingDirectory = stored.DefaultRecordingDirectory,
            MediaHostPort = stored.MediaHostPort,
            HardwareEncoder = stored.HardwareEncoder
        };
    }

    private static StreamingProviderProfile ToPublic(StoredProviderProfile profile) => new()
    {
        Id = profile.Id,
        Name = profile.Name,
        Provider = profile.Provider,
        Transport = profile.Transport,
        Endpoint = profile.Endpoint,
        ChannelId = profile.ChannelId,
        AccountName = profile.AccountName,
        HasStoredSecret = !string.IsNullOrWhiteSpace(profile.ProtectedSecret),
        ChatEnabled = profile.ChatEnabled,
        HasStoredChatSecret = !string.IsNullOrWhiteSpace(profile.ProtectedChatSecret),
        Enabled = profile.Enabled
    };

    private async Task<StoredStreamingMachineSettings> LoadStoredAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath)) return new StoredStreamingMachineSettings();
        try
        {
            await using var stream = File.OpenRead(_filePath);
            return await JsonSerializer.DeserializeAsync<StoredStreamingMachineSettings>(stream, _json, cancellationToken)
                ?? new StoredStreamingMachineSettings();
        }
        catch
        {
            var backup = _filePath + ".invalid-" + DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
            try { File.Move(_filePath, backup, overwrite: true); } catch { }
            return new StoredStreamingMachineSettings();
        }
    }

    private async Task SaveStoredAsync(StoredStreamingMachineSettings settings, CancellationToken cancellationToken)
    {
        var temporary = _filePath + ".tmp";
        await using (var stream = File.Create(temporary))
            await JsonSerializer.SerializeAsync(stream, settings, _json, cancellationToken);
        File.Move(temporary, _filePath, overwrite: true);
    }

    private sealed class StoredStreamingMachineSettings
    {
        public List<StoredProviderProfile> Providers { get; set; } = [];
        public List<StreamingDeviceProfile> Devices { get; set; } = [];
        public string FfmpegPath { get; set; } = string.Empty;
        public string DefaultRecordingDirectory { get; set; } = string.Empty;
        public int MediaHostPort { get; set; } = 17847;
        public StreamingHardwareEncoderPreference HardwareEncoder { get; set; } = StreamingHardwareEncoderPreference.Auto;
    }

    private sealed class StoredProviderProfile
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public PublicationStreamProvider Provider { get; set; }
        public PublicationStreamTransport Transport { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public string ChannelId { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string ProtectedSecret { get; set; } = string.Empty;
        public bool ChatEnabled { get; set; }
        public string ProtectedChatSecret { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
    }
}
