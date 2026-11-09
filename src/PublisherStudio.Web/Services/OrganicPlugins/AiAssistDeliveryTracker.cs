using System.Text.Json;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>
/// Watches the 1-Wire result store for the terminal result of tracked AI Assist requests. When LocalGPT finished the
/// Council but no <c>publisher.text.proposal.request</c> reached PublisherStudio and none waits for LocalGPT approval,
/// the Council deliverable becomes a proposal for the tracked target; the page's existing authorization check then
/// inserts it (or keeps it reviewable). Failures and pending LocalGPT approvals become user notices instead of silence.
/// </summary>
/// <param name="results">Shared 1-Wire result and proposal store.</param>
/// <param name="logger">Logger for delivery diagnostics.</param>
public sealed class AiAssistDeliveryTracker : IAiAssistDeliveryTracker, IDisposable
{
    /// <summary>Hours after which an unanswered request is no longer tracked.</summary>
    private const int TrackingLifetimeHours = 12;
    /// <summary>LocalGPT function that delivers text proposals to PublisherStudio.</summary>
    private const string ProposalFunctionName = "publisher.text.proposal.request";
    /// <summary>LocalGPT goal evidence kind for approval-gated function requests.</summary>
    private const string ApprovalGatedEvidenceKind = "ApprovalGatedRequest";
    /// <summary>Heading of the LocalGPT Keyaccount report appended to goal answers.</summary>
    private const string KeyaccountHeading = "## Keyaccount Manager for User";
    /// <summary>Label the final-answer recovery asks models to start with.</summary>
    private const string FinalAnswerLabel = "Final answer:";

    /// <summary>Shared 1-Wire result and proposal store.</summary>
    private readonly IOrganicResultStore results;
    /// <summary>Logger for delivery diagnostics.</summary>
    private readonly ILogger<AiAssistDeliveryTracker> logger;
    /// <summary>Guards the tracking table and notice queue.</summary>
    private readonly object gate = new();
    /// <summary>Tracked requests by correlation identifier.</summary>
    private readonly Dictionary<Guid, AiAssistDeliveryTracking> tracked = new();
    /// <summary>Notices not yet shown by a page.</summary>
    private readonly List<AiAssistDeliveryNotice> notices = new();

    /// <summary>Initializes the tracker and subscribes to result store changes.</summary>
    /// <param name="results">Shared 1-Wire result and proposal store.</param>
    /// <param name="logger">Logger for delivery diagnostics.</param>
    public AiAssistDeliveryTracker(IOrganicResultStore results, ILogger<AiAssistDeliveryTracker> logger)
    {
        this.results = results;
        this.logger = logger;
        try
        {
            results.Changed += OnResultsChanged;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "AI Assist delivery tracker could not subscribe to 1-Wire results.");
            throw;
        }
    }

    /// <inheritdoc />
    public event Action? Changed;

    /// <inheritdoc />
    public void Track(Guid correlationId, string target, string kind)
    {
        try
        {
            lock (gate)
            {
                tracked[correlationId] = new AiAssistDeliveryTracking { CorrelationId = correlationId, Target = target, Kind = kind };
            }
            logger.LogInformation("Tracking delivery of AI Assist request {CorrelationId} ({Kind}) for target {Target}.", correlationId, kind, target);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not start tracking AI Assist request {CorrelationId}.", correlationId);
            throw;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<AiAssistDeliveryNotice> DrainNotices()
    {
        try
        {
            lock (gate)
            {
                var drained = notices.ToList();
                notices.Clear();
                return drained;
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not read AI Assist delivery notices.");
            throw;
        }
    }

    /// <summary>Unsubscribes from the result store.</summary>
    public void Dispose()
    {
        try
        {
            results.Changed -= OnResultsChanged;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "AI Assist delivery tracker could not unsubscribe from 1-Wire results.");
        }
    }

    /// <summary>Marks observed proposals and handles terminal results of tracked requests.</summary>
    private void OnResultsChanged()
    {
        try
        {
            var due = new List<(AiAssistDeliveryTracking Tracking, OrganicPluginWorkItem Item)>();
            lock (gate)
            {
                if (tracked.Count == 0) return;
                foreach (var expired in tracked.Values.Where(item => item.CreatedUtc < DateTimeOffset.UtcNow.AddHours(-TrackingLifetimeHours)).ToList())
                {
                    tracked.Remove(expired.CorrelationId);
                    logger.LogWarning("AI Assist request {CorrelationId} received no Council result within {Hours} hours; tracking stopped.", expired.CorrelationId, TrackingLifetimeHours);
                }
                var proposalTargets = results.GetTextProposals().Select(item => item.Target).ToHashSet(StringComparer.Ordinal);
                foreach (var tracking in tracked.Values.Where(item => proposalTargets.Contains(item.Target)))
                    tracking.ProposalSeen = true;
                foreach (var item in results.GetResults())
                {
                    if (!tracked.TryGetValue(item.CorrelationId, out var tracking) || tracking.Completed) continue;
                    if (item.Request.MessageType is not (OrganicWireMessageType.WorkResult or OrganicWireMessageType.Error)) continue;
                    tracking.Completed = true; // claimed before delivery; re-entrant store events see it as handled
                    due.Add((tracking, item));
                }
                foreach (var (tracking, _) in due)
                    tracked.Remove(tracking.CorrelationId);
            }
            foreach (var (tracking, item) in due)
                Deliver(tracking, item);
            if (due.Count > 0)
                Changed?.Invoke();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "AI Assist delivery tracking failed while processing 1-Wire results.");
        }
    }

    /// <summary>Handles one terminal Council result of a tracked request.</summary>
    /// <param name="tracking">Tracked request.</param>
    /// <param name="item">Terminal WorkResult or error.</param>
    private void Deliver(AiAssistDeliveryTracking tracking, OrganicPluginWorkItem item)
    {
        try
        {
            if (item.Request.MessageType == OrganicWireMessageType.Error)
            {
                AddNotice(tracking, "Error", "AI Assist failed",
                    string.IsNullOrWhiteSpace(item.Error) ? "LocalGPT reported an error for the AI Assist request. Open the run in LocalGPT for details." : item.Error);
                logger.LogWarning("AI Assist request {CorrelationId} ended with a LocalGPT error.", tracking.CorrelationId);
                return;
            }
            if (tracking.ProposalSeen)
            {
                logger.LogInformation("AI Assist request {CorrelationId} delivered its result through the proposal function.", tracking.CorrelationId);
                return;
            }
            var council = ReadCouncilResult(item.Request);
            if (council.ProposalAwaitingApproval)
            {
                AddNotice(tracking, "Info", "AI Assist waits for LocalGPT approval",
                    "The Council prepared the text proposal. Approve it in LocalGPT (Approvals & team); it is inserted here afterwards.");
                logger.LogInformation("AI Assist request {CorrelationId} finished; its text proposal waits for approval in LocalGPT.", tracking.CorrelationId);
                return;
            }
            if (string.IsNullOrWhiteSpace(council.Deliverable))
            {
                AddNotice(tracking, "Error", "AI Assist returned no result",
                    "The Council finished without a usable result and without a proposal. Open the run in LocalGPT for details.");
                logger.LogWarning("AI Assist request {CorrelationId} finished without a deliverable or proposal.", tracking.CorrelationId);
                return;
            }
            results.AddTextProposal(new OrganicTextInsertionProposal
            {
                Target = tracking.Target,
                Text = council.Deliverable,
                Reason = "Council result delivered with the finished run (no separate proposal call)"
            });
            logger.LogInformation(
                "AI Assist request {CorrelationId} finished without a proposal call; its {Length} character deliverable was offered for target {Target}.",
                tracking.CorrelationId, council.Deliverable.Length, tracking.Target);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not deliver the result of AI Assist request {CorrelationId}.", tracking.CorrelationId);
            AddNotice(tracking, "Error", "AI Assist result could not be read", "The Council result arrived but could not be read. Open the run in LocalGPT for details.");
        }
    }

    /// <summary>Reads the deliverable and the proposal approval state from a LocalGPT Council result.</summary>
    /// <param name="envelope">WorkResult envelope.</param>
    /// <returns>The clean deliverable and whether a proposal request waits for LocalGPT approval.</returns>
    private (string Deliverable, bool ProposalAwaitingApproval) ReadCouncilResult(OrganicWireEnvelope envelope)
    {
        try
        {
            if (envelope.Properties is null || !envelope.Properties.TryGetValue("ResultJson", out var value))
                return (string.Empty, false);
            var serialized = value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(serialized) ? "{}" : serialized);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return (string.Empty, false);
            var deliverable = ReadString(root, "DeliverableText");
            if (string.IsNullOrWhiteSpace(deliverable))
                deliverable = ReadString(root, "FinalAnswer");
            var awaiting = false;
            if (TryGetProperty(root, "GoalExecution", out var goal) && goal.ValueKind == JsonValueKind.Object &&
                TryGetProperty(goal, "Evidence", out var evidence) && evidence.ValueKind == JsonValueKind.Array)
            {
                awaiting = evidence.EnumerateArray().Any(item => item.ValueKind == JsonValueKind.Object &&
                    string.Equals(ReadString(item, "Kind"), ApprovalGatedEvidenceKind, StringComparison.Ordinal) &&
                    ReadString(item, "Source").StartsWith(ProposalFunctionName + ":", StringComparison.OrdinalIgnoreCase) &&
                    TryGetProperty(item, "Succeeded", out var succeeded) && succeeded.ValueKind == JsonValueKind.True);
            }
            return (Clean(deliverable), awaiting);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not parse a LocalGPT Council result for AI Assist delivery.");
            throw;
        }
    }

    /// <summary>Removes the LocalGPT Keyaccount report and a leading final-answer label from a Council answer.</summary>
    /// <param name="text">Council answer.</param>
    /// <returns>The deliverable text.</returns>
    private string Clean(string text)
    {
        try
        {
            var cleaned = text;
            var keyaccount = cleaned.IndexOf(KeyaccountHeading, StringComparison.Ordinal);
            if (keyaccount >= 0)
                cleaned = cleaned[..keyaccount];
            cleaned = cleaned.Trim();
            if (cleaned.StartsWith(FinalAnswerLabel, StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned[FinalAnswerLabel.Length..].TrimStart();
            return cleaned;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not clean a Council answer for AI Assist delivery.");
            throw;
        }
    }

    /// <summary>Reads a string property case-insensitively.</summary>
    /// <param name="element">JSON object.</param>
    /// <param name="name">Property name.</param>
    /// <returns>The string value, or empty.</returns>
    private string ReadString(JsonElement element, string name)
    {
        try
        {
            return TryGetProperty(element, name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not read Council result property {Property}.", name);
            throw;
        }
    }

    /// <summary>Finds a property case-insensitively (LocalGPT serializes PascalCase; older peers may use camelCase).</summary>
    /// <param name="element">JSON object.</param>
    /// <param name="name">Property name.</param>
    /// <param name="value">Found value.</param>
    /// <returns><see langword="true"/> when the property exists.</returns>
    private bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        try
        {
            foreach (var property in element.EnumerateObject())
            {
                if (!property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) continue;
                value = property.Value;
                return true;
            }
            value = default;
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not search Council result property {Property}.", name);
            throw;
        }
    }

    /// <summary>Queues a user notice for the AI Assist page.</summary>
    /// <param name="tracking">Tracked request.</param>
    /// <param name="severity">Notice severity.</param>
    /// <param name="title">Notice title.</param>
    /// <param name="message">Notice text.</param>
    private void AddNotice(AiAssistDeliveryTracking tracking, string severity, string title, string message)
    {
        try
        {
            lock (gate)
            {
                notices.Add(new AiAssistDeliveryNotice(tracking.CorrelationId, severity, title, message));
                while (notices.Count > 50) notices.RemoveAt(0);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not queue an AI Assist delivery notice for {CorrelationId}.", tracking.CorrelationId);
        }
    }
}
