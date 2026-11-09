namespace PublisherStudio.BusinessObjects;

/// <summary>Delivery state of one submitted AI Assist Council request.</summary>
public sealed class AiAssistDeliveryTracking
{
    /// <summary>Gets or sets the Council request correlation identifier.</summary>
    /// <value>Correlation identifier returned when the request was sent.</value>
    public Guid CorrelationId { get; set; }

    /// <summary>Gets or sets the insertion target the request was authorized for.</summary>
    /// <value>Insertion target key.</value>
    public string Target { get; set; } = string.Empty;

    /// <summary>Gets or sets the insertion kind (writing, spreadsheet, project, openscad).</summary>
    /// <value>Insertion kind.</value>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets when tracking started.</summary>
    /// <value>Creation time.</value>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets a value indicating whether a proposal for the target already arrived through the proposal function.</summary>
    /// <value><see langword="true"/> after a proposal for the target was observed.</value>
    public bool ProposalSeen { get; set; }

    /// <summary>Gets or sets a value indicating whether the terminal Council result was handled.</summary>
    /// <value><see langword="true"/> after the WorkResult or error was processed.</value>
    public bool Completed { get; set; }
}
