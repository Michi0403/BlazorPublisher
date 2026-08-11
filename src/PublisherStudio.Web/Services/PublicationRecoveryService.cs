using System.Text.Json;

namespace PublisherStudio.Services;

/// <summary>
/// Represents a publication recovery snapshot.
/// </summary>
public sealed record PublicationRecoverySnapshot(
    Guid DocumentId,
    string DocumentName,
    DateTimeOffset ModifiedUtc,
    DateTimeOffset SavedUtc,
    string Json);

/// <summary>
/// Writes an atomic local recovery copy for the offline-first desktop host.
/// The normal publication file remains user-controlled; this store is only a crash/navigation safety net.
/// </summary>
public sealed class PublicationRecoveryService
{
    /// <summary>
    /// Represents a recovery manifest.
    /// </summary>
    private sealed record RecoveryManifest(Guid DocumentId, string DocumentName, DateTimeOffset ModifiedUtc, DateTimeOffset SavedUtc, string FileName);

    /// <summary>
    /// Runs the new operation.
    /// </summary>
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _root;
    private readonly string _manifestPath;

    /// <summary>
    /// Runs the publication recovery service operation.
    /// </summary>
    public PublicationRecoveryService()
    {
        _root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PublisherStudio",
            "recovery");
        _manifestPath = Path.Combine(_root, "latest.json");
    }

    /// <summary>
    /// Saves async.
    /// </summary>
    public async Task SaveAsync(Guid documentId, string documentName, DateTimeOffset modifiedUtc, string json, CancellationToken cancellationToken = default)
    {
    try
    {
            if (documentId == Guid.Empty || string.IsNullOrWhiteSpace(json)) return;
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Directory.CreateDirectory(_root);
                var fileName = $"{documentId:N}.pubstudio.json";
                var destination = Path.Combine(_root, fileName);
                var temporary = destination + ".tmp";
                await File.WriteAllTextAsync(temporary, json, cancellationToken).ConfigureAwait(false);
                File.Move(temporary, destination, overwrite: true);

                var manifest = new RecoveryManifest(documentId, documentName, modifiedUtc, DateTimeOffset.UtcNow, fileName);
                var manifestTemporary = _manifestPath + ".tmp";
                await File.WriteAllTextAsync(
                    manifestTemporary,
                    JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }),
                    cancellationToken).ConfigureAwait(false);
                File.Move(manifestTemporary, _manifestPath, overwrite: true);
            }
            finally
            {
                _gate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PublicationRecoveryService.SaveAsync failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Attempts to read latest.
    /// </summary>
    public PublicationRecoverySnapshot? TryReadLatest()
    {
    try
    {
            try
            {
                if (!File.Exists(_manifestPath)) return null;
                var manifest = JsonSerializer.Deserialize<RecoveryManifest>(File.ReadAllText(_manifestPath));
                if (manifest is null || string.IsNullOrWhiteSpace(manifest.FileName)) return null;
                var path = Path.Combine(_root, Path.GetFileName(manifest.FileName));
                if (!File.Exists(path)) return null;
                var json = File.ReadAllText(path);
                return string.IsNullOrWhiteSpace(json)
                    ? null
                    : new PublicationRecoverySnapshot(manifest.DocumentId, manifest.DocumentName, manifest.ModifiedUtc, manifest.SavedUtc, json);
            }
            catch
            {
                return null;
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PublicationRecoveryService.TryReadLatest failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Deletes async.
    /// </summary>
    public async Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
    try
    {
            if (documentId == Guid.Empty) return;
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var path = Path.Combine(_root, $"{documentId:N}.pubstudio.json");
                try { if (File.Exists(path)) File.Delete(path); } catch { }

                try
                {
                    if (!File.Exists(_manifestPath)) return;
                    var manifest = JsonSerializer.Deserialize<RecoveryManifest>(File.ReadAllText(_manifestPath));
                    if (manifest?.DocumentId == documentId) File.Delete(_manifestPath);
                }
                catch { }
            }
            finally
            {
                _gate.Release();
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method PublicationRecoveryService.DeleteAsync failed: {__serviceMethodException}");
        throw;
    }
}
}
