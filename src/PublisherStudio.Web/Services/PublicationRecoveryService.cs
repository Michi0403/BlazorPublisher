using System.Text.Json;

namespace PublisherStudio.Services;

/// <summary>
/// Represents a publication recovery snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="DocumentId">Identifier of the document to use for this operation.</param>
/// <param name="DocumentName">Document name value supplied to the publication recovery snapshot operation and used when producing its result.</param>
/// <param name="ModifiedUtc">Modified utc value supplied to the publication recovery snapshot operation and used when producing its result.</param>
/// <param name="SavedUtc">Saved utc value supplied to the publication recovery snapshot operation and used when producing its result.</param>
/// <param name="Json">Json value supplied to the publication recovery snapshot operation and used when producing its result.</param>
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
    /// Represents a recovery manifest helper type nested within <see cref="PublicationRecoveryService"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    /// <param name="DocumentId">Identifier of the document to use for this operation.</param>
    /// <param name="DocumentName">Document name value supplied to the publication recovery operation and used when producing its result.</param>
    /// <param name="ModifiedUtc">Modified utc value supplied to the publication recovery operation and used when producing its result.</param>
    /// <param name="SavedUtc">Saved utc value supplied to the publication recovery operation and used when producing its result.</param>
    /// <param name="FileName">File name value supplied to the publication recovery operation and used when producing its result.</param>
    private sealed record RecoveryManifest(Guid DocumentId, string DocumentName, DateTimeOffset ModifiedUtc, DateTimeOffset SavedUtc, string FileName);

    /// <summary>
    /// Stores the synchronization primitive that protects concurrent access to gate state owned by <see cref="PublicationRecoveryService"/>.
    /// </summary>
    private readonly SemaphoreSlim _gate = new(1, 1);
    /// <summary>
    /// Stores the internal root state used by <see cref="PublicationRecoveryService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _root;
    /// <summary>
    /// Stores the internal manifest path state used by <see cref="PublicationRecoveryService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string _manifestPath;

    /// <summary>
    /// Initializes a new <see cref="PublicationRecoveryService"/> instance and captures the dependencies or initial state required by its publication recovery workflow.
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
    /// Performs save as part of the publication recovery service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="documentId">Identifier of the document to use for this operation.</param>
    /// <param name="documentName">Document name value supplied to the publication recovery operation and used when producing its result.</param>
    /// <param name="modifiedUtc">Modified utc value supplied to the publication recovery operation and used when producing its result.</param>
    /// <param name="json">Json value supplied to the publication recovery operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
    /// Attempts to read latest as part of the publication recovery service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <returns>The publication recovery snapshot produced by the operation.</returns>
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
    /// Performs delete as part of the publication recovery service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="documentId">Identifier of the document to use for this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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
