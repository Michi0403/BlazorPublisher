using PublisherStudio.Domain;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PublisherStudio.Services.OrganicPlugins;

public sealed class OrganicPluginProtocolCodec : IOrganicPluginProtocolCodec
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public string Serialize(OrganicWireEnvelope envelope, bool seal = true)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (seal)
        {
            envelope.NormalizeInteractionKind();
            ValidatePayloadShape(envelope);
            var integrity = BuildIntegrityBytes(envelope);
            envelope.Hash = Convert.ToHexString(SHA256.HashData(integrity));
            envelope.ErrorCheck = ComputeCrc32(integrity).ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
        }
        return JsonSerializer.Serialize(envelope, JsonOptions);
    }

    public OrganicWireEnvelope DeserializeAndValidate(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        if (Encoding.UTF8.GetByteCount(json) > OrganicWireProtocol.MaximumMessageBytes)
            throw new InvalidDataException("The organic 1-Wire message exceeds the supported size limit.");
        var envelope = JsonSerializer.Deserialize<OrganicWireEnvelope>(json, JsonOptions)
            ?? throw new JsonException("The organic 1-Wire envelope is empty.");
        if (!Validate(envelope, out var error)) throw new InvalidDataException(error);
        return envelope;
    }

    public bool Validate(OrganicWireEnvelope envelope, out string error)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(envelope);
            if (!OrganicWireProtocol.IsCompatible(envelope.ProtocolVersion))
                throw new InvalidDataException($"Unsupported organic 1-Wire protocol version '{envelope.ProtocolVersion}'.");
            if (envelope.MessageId == Guid.Empty || envelope.CorrelationId == Guid.Empty)
                throw new InvalidDataException("MessageId and CorrelationId are required.");
            if (envelope.ExpiresUtc is { } expires && expires < DateTimeOffset.UtcNow)
                throw new InvalidDataException("The organic 1-Wire message has expired.");
            ValidatePayloadShape(envelope);
            var integrity = BuildIntegrityBytes(envelope);
            var expectedHash = Convert.ToHexString(SHA256.HashData(integrity));
            var actualHash = envelope.Hash?.Trim().ToUpperInvariant() ?? string.Empty;
            if (actualHash.Length != expectedHash.Length || !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expectedHash), Encoding.ASCII.GetBytes(actualHash)))
                throw new InvalidDataException("The organic 1-Wire hash check failed.");
            var expectedCrc = ComputeCrc32(integrity).ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
            if (!string.Equals(expectedCrc, envelope.ErrorCheck, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The organic 1-Wire transmission error check failed.");
            error = string.Empty;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidDataException)
        {
            error = ex.Message;
            return false;
        }
    }

    private static void ValidatePayloadShape(OrganicWireEnvelope envelope)
    {
        if (!string.IsNullOrWhiteSpace(envelope.EncryptedPayload) && envelope.Properties is not null)
            throw new InvalidDataException("EncryptedPayload and public Properties are mutually exclusive.");
        if (envelope.Properties is not null && envelope.Properties.Count > 128)
            throw new InvalidDataException("The organic 1-Wire property count exceeds the supported limit.");
    }

    private static byte[] BuildIntegrityBytes(OrganicWireEnvelope envelope)
    {
        var orderedProperties = envelope.Properties is null ? null : new SortedDictionary<string, JsonElement>(envelope.Properties, StringComparer.Ordinal);
        var view = new
        {
            envelope.ProtocolVersion, envelope.MessageId, envelope.CorrelationId, envelope.ReplyToMessageId,
            envelope.MessageType, envelope.SourcePeerId, envelope.TargetPeerId, envelope.CreatedUtc,
            envelope.ExpiresUtc, envelope.Sequence, envelope.ExecutionMode, envelope.Controller,
            envelope.Method, envelope.Route, envelope.CapabilityKey,
            Organs = envelope.Organs.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            Skills = envelope.Skills.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            Properties = orderedProperties, envelope.EncryptedPayload, envelope.UserConfirmed,
            envelope.ApprovalMode, envelope.WorkOrderKey, envelope.NotBeforeUtc, envelope.WorkflowJson,
            envelope.Error, envelope.RequiresHumanInteractionOnTargetSystem,
            envelope.RequiresAutomatedInteractionOnTargetSystem, envelope.InteractionKind,
            envelope.InteractionValueJson, envelope.InteractionValueContentType
        };
        return JsonSerializer.SerializeToUtf8Bytes(view, JsonOptions);
    }

    private static uint ComputeCrc32(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++) crc = (crc & 1) == 1 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
        }
        return ~crc;
    }
}
