using System.ComponentModel.DataAnnotations;

namespace PublisherStudio.BusinessObjects;

/// <summary>Represents one structured PublisherStudio application-log event.</summary>
public sealed class ApplicationLogEntry
{
    [Key]
    public long Id { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string Level { get; set; } = "Information";
    public int LogLevelValue { get; set; }
    public string Category { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string? EventName { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? ExceptionStackTrace { get; set; }
    public string? Exception { get; set; }
    public string MachineName { get; set; } = Environment.MachineName;
    public int ProcessId { get; set; } = Environment.ProcessId;
    public int ThreadId { get; set; } = Environment.CurrentManagedThreadId;
}

/// <summary>Compact projection of a structured PublisherStudio application-log event.</summary>
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
