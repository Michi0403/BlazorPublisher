using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.OrganicPlugins;

/// <summary>
/// Ensures an AI Assist request reaches the user: when the Council result arrives without a proposal call, the result
/// becomes a proposal for the authorized target; failures and pending LocalGPT approvals become notices.
/// </summary>
public interface IAiAssistDeliveryTracker
{
    /// <summary>Occurs when new delivery notices are available.</summary>
    event Action? Changed;

    /// <summary>Starts tracking a submitted AI Assist Council request.</summary>
    /// <param name="correlationId">Correlation identifier returned by the Council request.</param>
    /// <param name="target">Insertion target authorized for the request.</param>
    /// <param name="kind">Insertion kind.</param>
    void Track(Guid correlationId, string target, string kind);

    /// <summary>Returns and removes the pending delivery notices.</summary>
    /// <returns>Notices in arrival order.</returns>
    IReadOnlyList<AiAssistDeliveryNotice> DrainNotices();
}
