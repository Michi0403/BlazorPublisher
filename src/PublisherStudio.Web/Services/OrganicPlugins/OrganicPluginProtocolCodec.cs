using PublisherStudio.BusinessObjects;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>
/// Represents an organic plugin protocol codec application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class OrganicPluginProtocolCodec : IOrganicPluginProtocolCodec
{
    /// <summary>
    /// Gets the JSON options value that forms part of the organic plugin protocol codec state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The JSON options value exposed by <see cref="OrganicPluginProtocolCodec"/>.</value>
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Gets JSON options.
    /// </summary>
    public JsonSerializerOptions JsonOptions => jsonOptions;

    /// <summary>
    /// Performs serialize for <see cref="OrganicPluginProtocolCodec"/>, keeping the operation consistent with the state and invariants of the surrounding organic plugin protocol codec workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic plugin protocol codec operation and used when producing its result.</param>
    /// <param name="seal">Value indicating whether seal should apply to this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public string Serialize(OrganicWireEnvelope envelope, bool seal = true)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OrganicPluginProtocolCodec.Serialize failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs deserialize and validate for <see cref="OrganicPluginProtocolCodec"/>, keeping the operation consistent with the state and invariants of the surrounding organic plugin protocol codec workflow.
    /// </summary>
    /// <param name="json">Json value supplied to the organic plugin protocol codec operation and used when producing its result.</param>
    /// <returns>The organic wire envelope produced by the operation.</returns>
    public OrganicWireEnvelope DeserializeAndValidate(string json)
    {
    try
    {
            ArgumentException.ThrowIfNullOrWhiteSpace(json);
            if (Encoding.UTF8.GetByteCount(json) > OrganicWireProtocol.MaximumMessageBytes)
                throw new InvalidDataException("The organic 1-Wire message exceeds the supported size limit.");
            var envelope = JsonSerializer.Deserialize<OrganicWireEnvelope>(json, JsonOptions)
                ?? throw new JsonException("The organic 1-Wire envelope is empty.");
            if (!Validate(envelope, out var error)) throw new InvalidDataException(error);
            return envelope;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OrganicPluginProtocolCodec.DeserializeAndValidate failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Performs validate for <see cref="OrganicPluginProtocolCodec"/>, keeping the operation consistent with the state and invariants of the surrounding organic plugin protocol codec workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic plugin protocol codec operation and used when producing its result.</param>
    /// <param name="error">Error value supplied to the organic plugin protocol codec operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool Validate(OrganicWireEnvelope envelope, out string error)
    {
    try
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
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OrganicPluginProtocolCodec.Validate failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Validates payload shape for <see cref="OrganicPluginProtocolCodec"/>, keeping the operation consistent with the state and invariants of the surrounding organic plugin protocol codec workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic plugin protocol codec operation and used when producing its result.</param>
    private void ValidatePayloadShape(OrganicWireEnvelope envelope)
    {
    try
    {
            if (!string.IsNullOrWhiteSpace(envelope.EncryptedPayload) && envelope.Properties is not null)
                throw new InvalidDataException("EncryptedPayload and public Properties are mutually exclusive.");
            if (envelope.Properties is not null && envelope.Properties.Count > 128)
                throw new InvalidDataException("The organic 1-Wire property count exceeds the supported limit.");
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OrganicPluginProtocolCodec.ValidatePayloadShape failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Builds integrity bytes for <see cref="OrganicPluginProtocolCodec"/>, keeping the operation consistent with the state and invariants of the surrounding organic plugin protocol codec workflow.
    /// </summary>
    /// <param name="envelope">Envelope value supplied to the organic plugin protocol codec operation and used when producing its result.</param>
    /// <returns>The byte produced by the operation.</returns>
    private byte[] BuildIntegrityBytes(OrganicWireEnvelope envelope)
    {
    try
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
                Properties = orderedProperties, envelope.EncryptedPayload, envelope.SecurityMode,
                envelope.SecurityKeyId, envelope.EncryptionNonce, envelope.AuthenticationTag, envelope.Signature, envelope.UserConfirmed,
                envelope.ApprovalMode, envelope.WorkOrderKey, envelope.NotBeforeUtc, envelope.WorkflowJson,
                envelope.Error, envelope.RequiresHumanInteractionOnTargetSystem,
                envelope.RequiresAutomatedInteractionOnTargetSystem, envelope.InteractionKind,
                envelope.InteractionValueJson, envelope.InteractionValueContentType
            };
            return JsonSerializer.SerializeToUtf8Bytes(view, JsonOptions);
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OrganicPluginProtocolCodec.BuildIntegrityBytes failed: {__serviceMethodException}");
        throw;
    }
}

    /// <summary>
    /// Computes crc32 for <see cref="OrganicPluginProtocolCodec"/>, keeping the operation consistent with the state and invariants of the surrounding organic plugin protocol codec workflow.
    /// </summary>
    /// <param name="data">Data value supplied to the organic plugin protocol codec operation and used when producing its result.</param>
    /// <returns>The uint produced by the operation.</returns>
    private uint ComputeCrc32(ReadOnlySpan<byte> data)
    {
    try
    {
            var crc = 0xFFFFFFFFu;
            foreach (var value in data)
            {
                crc ^= value;
                for (var bit = 0; bit < 8; bit++) crc = (crc & 1) == 1 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
            return ~crc;
    
    }
    catch (Exception __serviceMethodException)
    {
        System.Diagnostics.Trace.TraceError($"Service method OrganicPluginProtocolCodec.ComputeCrc32 failed: {__serviceMethodException}");
        throw;
    }
}
}
