using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>
/// Holds files produced by approved 1-Wire media and export requests until the requesting peer has read them.
/// Files that fit one 1-Wire message are returned inline; larger files are announced with their length and SHA-256
/// and read in chunks through <c>publisher.file.read</c>. Only the peer that requested a file can read it, and retention,
/// file size and total memory are bounded by <c>OrganicPlugins:MediaTools</c> settings.
/// </summary>
/// <param name="support">Shared 1-Wire media tool support for settings and the message file budget.</param>
/// <param name="logger">Structured diagnostics for stored and read transfers.</param>
public sealed class OrganicFileTransferStore(OrganicMediaToolSupport support, ILogger<OrganicFileTransferStore> logger)
{
    /// <summary>One stored file.</summary>
    /// <param name="PeerId">Peer that requested the file and is allowed to read it.</param>
    /// <param name="Bytes">File content.</param>
    /// <param name="ExpiresUtc">Time after which the file is discarded.</param>
    private sealed record StoredFile(string PeerId, byte[] Bytes, DateTimeOffset ExpiresUtc);

    /// <summary>Stored files by transfer identifier.</summary>
    private readonly ConcurrentDictionary<Guid, StoredFile> files = new();

    /// <summary>Builds the result fields for a produced file: inline bytes when they fit one message, otherwise a chunked transfer announcement.</summary>
    /// <param name="peerId">Peer that requested the file.</param>
    /// <param name="bytes">Produced file.</param>
    /// <param name="kind">Lower-case file extension.</param>
    /// <param name="mimeType">MIME type.</param>
    /// <param name="fileName">Suggested file name.</param>
    /// <returns>Result fields (Kind, MimeType, FileName and DocumentBytes or Transfer).</returns>
    public Dictionary<string, object?> Offer(string peerId, byte[] bytes, string kind, string mimeType, string fileName)
    {
        try
        {
            var result = new Dictionary<string, object?> { ["Kind"] = kind, ["MimeType"] = mimeType, ["FileName"] = fileName };
            if (bytes.Length <= support.GetFileBudgetBytes())
            {
                result["DocumentBytes"] = bytes;
                return result;
            }

            var maximumFileBytes = Math.Max(1, support.Settings.MaximumTransferFileBytes);
            if (bytes.Length > maximumFileBytes)
                throw new InvalidOperationException($"The file has {bytes.Length} bytes; the configured limit OrganicPlugins:MediaTools:MaximumTransferFileBytes is {maximumFileBytes}.");
            var now = DateTimeOffset.UtcNow;
            foreach (var expired in files.Where(pair => pair.Value.ExpiresUtc <= now).Select(pair => pair.Key).ToArray())
                files.TryRemove(expired, out _);
            var retainedBytes = files.Values.Sum(file => (long)file.Bytes.Length);
            var maximumRetainedBytes = Math.Max(1L, support.Settings.MaximumRetainedTransferBytes);
            if (retainedBytes + bytes.Length > maximumRetainedBytes)
                throw new InvalidOperationException($"{retainedBytes} bytes are already waiting to be read; the configured limit OrganicPlugins:MediaTools:MaximumRetainedTransferBytes is {maximumRetainedBytes}. Retry after the pending transfers finish.");

            var transferId = Guid.NewGuid();
            files[transferId] = new StoredFile(peerId, bytes, now.AddMinutes(Math.Max(1, support.Settings.TransferRetentionMinutes)));
            result["Transfer"] = new
            {
                TransferId = transferId,
                Length = bytes.LongLength,
                Sha256 = Convert.ToHexString(SHA256.HashData(bytes)),
                ChunkBytes = support.GetFileBudgetBytes(),
                Capability = "publisher.file.read"
            };
            logger.LogInformation("Stored a {Kind} file of {Bytes} bytes for chunked 1-Wire transfer {TransferId}.", kind, bytes.Length, transferId);
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not offer a {Kind} file over 1-Wire.", kind);
            throw;
        }
    }

    /// <summary>Reads one chunk of a stored file for the peer that requested it; the file is discarded after its last chunk.</summary>
    /// <param name="peerId">Peer asking for the chunk.</param>
    /// <param name="transferId">Announced transfer.</param>
    /// <param name="offset">First byte to read.</param>
    /// <returns>The chunk and whether the file is complete.</returns>
    public object Read(string peerId, Guid transferId, long offset)
    {
        try
        {
            if (!files.TryGetValue(transferId, out var file) || file.ExpiresUtc <= DateTimeOffset.UtcNow)
            {
                files.TryRemove(transferId, out _);
                throw new KeyNotFoundException($"The file transfer has expired or was already read (OrganicPlugins:MediaTools:TransferRetentionMinutes is {support.Settings.TransferRetentionMinutes}).");
            }
            if (!string.Equals(file.PeerId, peerId, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("The file transfer belongs to another peer.");
            if (offset < 0 || offset >= file.Bytes.LongLength)
                throw new ArgumentOutOfRangeException(nameof(offset), "offset is outside the announced file.");

            var count = (int)Math.Min(support.GetFileBudgetBytes(), file.Bytes.LongLength - offset);
            var chunk = file.Bytes.AsSpan((int)offset, count).ToArray();
            var done = offset + count >= file.Bytes.LongLength;
            if (done)
                files.TryRemove(transferId, out _);
            logger.LogDebug("Read {Bytes} bytes at offset {Offset} of 1-Wire transfer {TransferId}; done={Done}.", count, offset, transferId, done);
            return new { TransferId = transferId, Offset = offset, Length = file.Bytes.LongLength, Done = done, DocumentChunk = chunk };
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not read 1-Wire transfer {TransferId} at offset {Offset}.", transferId, offset);
            throw;
        }
    }
}
