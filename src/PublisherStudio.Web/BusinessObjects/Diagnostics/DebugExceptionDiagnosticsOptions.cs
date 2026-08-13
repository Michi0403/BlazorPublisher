namespace PublisherStudio.BusinessObjects.Diagnostics;

/// <summary>
/// Describes development-only first-chance exception diagnostics.
/// </summary>
public sealed class DebugExceptionDiagnosticsOptions
{
    /// <summary>
    /// Gets or sets whether development first-chance exception diagnostics are enabled.
    /// </summary>
    /// <value>The enabled value exposed by <see cref="DebugExceptionDiagnosticsOptions"/>.</value>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets whether expected circuit and cancellation exceptions are logged.
    /// </summary>
    /// <value>The include expected lifecycle exceptions value exposed by <see cref="DebugExceptionDiagnosticsOptions"/>.</value>
    public bool IncludeExpectedLifecycleExceptions { get; set; }

    /// <summary>
    /// Gets or sets whether framework-originated invalid-operation exceptions are logged.
    /// </summary>
    /// <value>The include framework invalid operation exceptions value exposed by <see cref="DebugExceptionDiagnosticsOptions"/>.</value>
    public bool IncludeFrameworkInvalidOperationExceptions { get; set; }

    /// <summary>
    /// Gets or sets the number of detailed entries retained for each exception call site.
    /// </summary>
    /// <value>The detailed occurrences per call site value exposed by <see cref="DebugExceptionDiagnosticsOptions"/>.</value>
    public int DetailedOccurrencesPerCallSite { get; set; }

    /// <summary>
    /// Gets or sets the interval used to summarize repeated exceptions after detailed logging is bounded.
    /// </summary>
    /// <value>The summary every occurrences value exposed by <see cref="DebugExceptionDiagnosticsOptions"/>.</value>
    public int SummaryEveryOccurrences { get; set; }
}
