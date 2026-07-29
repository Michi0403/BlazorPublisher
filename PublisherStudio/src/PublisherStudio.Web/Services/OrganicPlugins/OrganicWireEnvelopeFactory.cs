using PublisherStudio.Domain;
using System.Text.Json;

namespace PublisherStudio.Services.OrganicPlugins;

public sealed class OrganicWireEnvelopeFactory(
    IOrganicPluginProtocolCodec codec,
    ILogger<OrganicWireEnvelopeFactory> logger) : IOrganicWireEnvelopeFactory
{
    public OrganicWireEnvelope CreateWorkEnvelope(OrganicPluginWorkItem item, string sourcePeerId)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(item);
            ArgumentException.ThrowIfNullOrWhiteSpace(sourcePeerId);
            var envelope = new OrganicWireEnvelope
            {
                MessageType = item.Status == OrganicWorkStatus.PendingApproval
                    ? OrganicWireMessageType.ApprovalRequired
                    : item.Status is OrganicWorkStatus.Queued or OrganicWorkStatus.Running
                        ? OrganicWireMessageType.WorkAccepted
                        : OrganicWireMessageType.WorkResult,
                CorrelationId = item.CorrelationId,
                ReplyToMessageId = item.MessageId,
                SourcePeerId = sourcePeerId,
                TargetPeerId = item.PeerId,
                CapabilityKey = item.CapabilityKey,
                Error = item.Error,
                InteractionValueJson = item.Request.InteractionValueJson,
                InteractionValueContentType = item.Request.InteractionValueContentType,
                Properties = new Dictionary<string, JsonElement>
                {
                    ["WorkItemId"] = JsonSerializer.SerializeToElement(item.Id, codec.JsonOptions),
                    ["Status"] = JsonSerializer.SerializeToElement(item.Status.ToString(), codec.JsonOptions),
                    ["ResultJson"] = JsonSerializer.SerializeToElement(item.ResultJson, codec.JsonOptions),
                    ["InteractionValueJson"] = JsonSerializer.SerializeToElement(item.Request.InteractionValueJson, codec.JsonOptions),
                    ["InteractionValueContentType"] = JsonSerializer.SerializeToElement(item.Request.InteractionValueContentType, codec.JsonOptions)
                }
            };
            logger.LogTrace($"Created organic work envelope for correlation {item.CorrelationId}.");
            return envelope;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Could not create an organic work envelope for correlation {item?.CorrelationId}.");
            throw;
        }
    }
}
