using PublisherStudio.BusinessObjects;
using System.Text.Json;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>
/// Creates configured organic wire envelope instances from the application's current dependencies and runtime settings.
/// </summary>
/// <param name="codec">Organic plugin protocol codec dependency used by the organic wire envelope workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class OrganicWireEnvelopeFactory(
    IOrganicPluginProtocolCodec codec,
    ILogger<OrganicWireEnvelopeFactory> logger) : IOrganicWireEnvelopeFactory
{
    /// <summary>
    /// Creates work envelope using the configuration and dependencies owned by <see cref="OrganicWireEnvelopeFactory"/>.
    /// </summary>
    /// <param name="item">Item value supplied to the organic wire envelope operation and used when producing its result.</param>
    /// <param name="sourcePeerId">Identifier of the source peer to use for this operation.</param>
    /// <returns>The organic wire envelope produced by the operation.</returns>
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
