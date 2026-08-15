using System.ComponentModel.DataAnnotations;

namespace PublisherStudio.BusinessObjects;

/// <summary>
/// Represents one structured PublisherStudio application log event and the process context that produced it.
/// </summary>
public sealed class ApplicationLogEntry
{
    /// <summary>Gets or sets the stable identifier of the persisted application log event.</summary>
    /// <value>The database or persistence identifier assigned to the event.</value>
    [Key]
    public long Id { get; set; }

    /// <summary>Gets or sets the UTC timestamp at which the event was produced.</summary>
    /// <value>The UTC event timestamp.</value>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the textual logging severity associated with the event.</summary>
    /// <value>The logging level name, such as Information, Warning, or Error.</value>
    public string Level { get; set; } = "Information";

    /// <summary>Gets or sets the numeric logging severity associated with the event.</summary>
    /// <value>The numeric value of the corresponding Microsoft logging level.</value>
    public int LogLevelValue { get; set; }

    /// <summary>Identifies the logging category that produced the event so diagnostics can be grouped by source.</summary>
    /// <value>The fully qualified logging category name when available.</value>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the numeric event identifier supplied by the logging caller.</summary>
    /// <value>The numeric event identifier.</value>
    public int EventId { get; set; }

    /// <summary>Gets or sets the optional event name supplied by the logging caller.</summary>
    /// <value>The optional event name.</value>
    public string? EventName { get; set; }

    /// <summary>Contains the human-readable application message rendered by the logging formatter.</summary>
    /// <value>The formatted message text.</value>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the formatted exception text associated with the event.</summary>
    /// <value>The exception including type, message, and stack trace when one was supplied.</value>
    public string? Exception { get; set; }

    /// <summary>Gets or sets the machine name on which the event was produced.</summary>
    /// <value>The operating-system machine name.</value>
    public string MachineName { get; set; } = Environment.MachineName;

    /// <summary>Gets or sets the process identifier of the PublisherStudio process that produced the event.</summary>
    /// <value>The operating-system process identifier.</value>
    public int ProcessId { get; set; } = Environment.ProcessId;

    /// <summary>Gets or sets the managed thread identifier that produced the event.</summary>
    /// <value>The managed thread identifier.</value>
    public int ThreadId { get; set; } = Environment.CurrentManagedThreadId;
}

/// <summary>Provides a compact projection of one structured PublisherStudio application log event.</summary>
/// <param name="Id">Stable identifier of the log event.</param>
/// <param name="TimestampUtc">UTC timestamp at which the event was produced.</param>
/// <param name="Level">Textual logging severity.</param>
/// <param name="LogLevelValue">Numeric logging severity.</param>
/// <param name="Category">Logging category that produced the event.</param>
/// <param name="EventId">Numeric event identifier supplied by the logging caller.</param>
/// <param name="EventName">Optional event name supplied by the logging caller.</param>
/// <param name="Message">Formatted application log message.</param>
/// <param name="Exception">Formatted exception text when an exception was supplied.</param>
public sealed record ApplicationLogSummary(
    long Id,
    DateTime TimestampUtc,
    string Level,
    int LogLevelValue,
    string Category,
    int EventId,
    string? EventName,
    string Message,
    string? Exception);
