namespace PublisherStudio.BusinessObjects;

/// <summary>
/// A user-facing message about the delivery of one AI Assist request, produced by the delivery tracker and shown by
/// the AI Assist page as a notification.
/// </summary>
/// <param name="CorrelationId">Council request correlation identifier.</param>
/// <param name="Severity">Notification severity: Success, Info, Warning or Error.</param>
/// <param name="Title">Short notification title.</param>
/// <param name="Message">Notification text.</param>
public sealed record AiAssistDeliveryNotice(Guid CorrelationId, string Severity, string Title, string Message);
