using System.Text.Json;
using Microsoft.Extensions.Options;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>Reads approved generated media through the existing paired 1-Wire connection.</summary>
public sealed class PublisherGeneratedMediaService(ILocalGptConnectionService localGpt, IOrganicPluginProtocolCodec codec,
    IOptions<OrganicPluginOptions> options, ILogger<PublisherGeneratedMediaService> logger)
{
    internal const string CapabilityKey = "localgpt.media.delivery.read";

    /// <summary>Returns validated media bytes, or null while LocalGPT is still waiting for a successful generation.</summary>
    public async Task<PublisherGeneratedMedia?> DownloadAsync(string peerId, Guid councilCorrelationId, string kind, CancellationToken cancellationToken)
    {
        try
        {
        var expectedType = kind switch { "picture" => "image/png", "video" => "video/mp4", _ => throw new ArgumentException("Unsupported generated media kind.", nameof(kind)) };
        var configuredFrameSize = Math.Min(options.Value.MaximumMessageBytes, OrganicWireProtocol.MaximumMessageBytes);
        var chunkSize = (configuredFrameSize - 2048) / 4;
        if (chunkSize <= 0) throw new InvalidOperationException("The configured 1-Wire message size is too small for media delivery.");

        string artifactId = string.Empty;
        string fileName = string.Empty;
        long length = 0;
        using var stream = new MemoryStream();
        do
        {
            if (!localGpt.State.IsLinked || !string.Equals(peerId, localGpt.State.PeerId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Reconnect the LocalGPT peer that started this media request.");
            var requestId = await localGpt.SendEnvelopeAsync(new OrganicWireEnvelope
            {
                MessageType = OrganicWireMessageType.Invoke,
                CapabilityKey = CapabilityKey,
                TargetPeerId = peerId,
                Controller = "OneWire",
                Method = "GET",
                Route = "/api/onewire/http-json",
                Properties = new Dictionary<string, JsonElement>
                {
                    ["Parameters"] = JsonSerializer.SerializeToElement(new
                    {
                        CouncilCorrelationId = councilCorrelationId, ArtifactId = artifactId,
                        Offset = stream.Length, Count = chunkSize
                    }, codec.JsonOptions)
                }
            }, cancellationToken).ConfigureAwait(false);
            var response = await localGpt.WaitForResultAsync(requestId, TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
            if (response.MessageType == OrganicWireMessageType.ApprovalRequired)
                throw new InvalidOperationException("LocalGPT requested approval for the media transfer. Its peer-scoped media delivery capability must be available before retrying.");
            if (response.MessageType == OrganicWireMessageType.Error)
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(response.Error) ? "LocalGPT could not return the generated media." : response.Error);
            if (response.MessageType != OrganicWireMessageType.WorkResult || !string.Equals(response.SourcePeerId, peerId, StringComparison.OrdinalIgnoreCase) ||
                response.CapabilityKey != CapabilityKey || response.Properties is null ||
                !response.Properties.TryGetValue("Media", out var payload) ||
                payload.Deserialize<PublisherMediaReadResult>(codec.JsonOptions) is not { } chunk)
                throw new InvalidDataException("LocalGPT returned an invalid generated media receipt.");
            if (chunk.Status == "Waiting" && stream.Length == 0) return null;
            if (chunk.Status == "CompletedWithoutFile" && stream.Length == 0)
                throw new InvalidOperationException("The LocalGPT Council ended without a generated file. Open the Council in LocalGPT Chat to see what blocked generation.");
            if (chunk.Status == "Unavailable") throw new InvalidOperationException("The generated media request has expired or is unavailable.");
            if (chunk.Status != "Ready" || chunk.ContentType != expectedType || chunk.Offset != stream.Length ||
                chunk.Length <= 0 || chunk.Length > options.Value.MaximumGeneratedMediaBytes ||
                chunk.Data is not { Length: > 0 } data || data.Length > chunkSize || data.Length > chunk.Length - stream.Length ||
                string.IsNullOrWhiteSpace(chunk.ArtifactId) || string.IsNullOrWhiteSpace(chunk.FileName))
                throw new InvalidDataException("LocalGPT returned an incompatible or oversized media artifact.");

            if (stream.Length == 0)
            {
                artifactId = chunk.ArtifactId;
                fileName = Path.GetFileName(chunk.FileName);
                length = chunk.Length;
            }
            else if (artifactId != chunk.ArtifactId || length != chunk.Length || fileName != chunk.FileName)
                throw new InvalidDataException("The generated media artifact changed during transfer.");

            stream.Write(data);
        } while (stream.Length < length);

        var bytes = stream.ToArray();
        var valid = kind == "picture"
            ? bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            : bytes.Length >= 12 && bytes.AsSpan(4, 4).SequenceEqual("ftyp"u8);
        if (!valid) throw new InvalidDataException("The generated media does not match its file format.");
        return new(fileName, expectedType, bytes);
        }
        catch (OperationCanceledException error) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(error, "Canceled retrieval of generated media for Council {CouncilCorrelationId}.", councilCorrelationId);
            throw;
        }
        catch (Exception error)
        {
            logger.LogWarning(error, "Could not retrieve generated media for Council {CouncilCorrelationId}.", councilCorrelationId);
            throw;
        }
    }
}
