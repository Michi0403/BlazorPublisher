using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using PublisherStudio.BusinessObjects.Diagnostics;

namespace PublisherStudio.Diagnostics;

/// <summary>
/// Adds bounded, contextual application logging for first-chance exceptions during development.
/// </summary>
/// <param name="environment">Host environment dependency used by the debug first chance exception logging workflow to provide the corresponding application capability.</param>
/// <param name="configuredOptions">Debug exception diagnostics options dependency used by the debug first chance exception logging workflow to provide the corresponding application capability.</param>
/// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
public sealed class DebugFirstChanceExceptionLoggingHostedService(
    IHostEnvironment environment,
    IOptions<DebugExceptionDiagnosticsOptions> configuredOptions,
    ILogger<DebugFirstChanceExceptionLoggingHostedService> logger) : IHostedService, IDisposable
{
    /// <summary>
    /// Stores the in-memory occurrences collection maintained internally by <see cref="DebugFirstChanceExceptionLoggingHostedService"/> for its current workflow state.
    /// </summary>
    private readonly ConcurrentDictionary<string, ExceptionOccurrence> occurrences = new(StringComparer.Ordinal);
    /// <summary>
    /// Stores the internal options state used by <see cref="DebugFirstChanceExceptionLoggingHostedService"/> while executing its surrounding workflow.
    /// </summary>
    private readonly DebugExceptionDiagnosticsOptions options = configuredOptions.Value;
    /// <summary>
    /// Stores the internal handling exception state used by <see cref="DebugFirstChanceExceptionLoggingHostedService"/> while executing its surrounding workflow.
    /// </summary>
    private int handlingException;
    /// <summary>
    /// Stores the internal subscribed state used by <see cref="DebugFirstChanceExceptionLoggingHostedService"/> while executing its surrounding workflow.
    /// </summary>
    private bool subscribed;

    /// <summary>
    /// Performs start as part of the debug first chance exception logging service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="_">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task StartAsync(CancellationToken _)
    {
        if (!environment.IsDevelopment() || !options.Enabled)
        {
            logger.LogDebug("PublisherStudio development first-chance exception diagnostics are disabled.");
            return Task.CompletedTask;
        }

        ValidateOptions();
        AppDomain.CurrentDomain.FirstChanceException += HandleFirstChanceException;
        subscribed = true;
        logger.LogInformation(
            "PublisherStudio development first-chance exception diagnostics are active. Detailed entries per call site: {DetailedLimit}; repeat summary interval: {SummaryInterval}.",
            options.DetailedOccurrencesPerCallSite,
            options.SummaryEveryOccurrences);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs stop as part of the debug first chance exception logging service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="_">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    public Task StopAsync(CancellationToken _)
    {
        Unsubscribe();
        LogFinalSummaries();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Releases the framework event subscription.
    /// </summary>
    public void Dispose()
    {
        Unsubscribe();
    }

    /// <summary>
    /// Handles first chance exception as part of the debug first chance exception logging service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="_">_ value supplied to the debug first chance exception logging operation and used when producing its result.</param>
    /// <param name="eventArgs">Event args value supplied to the debug first chance exception logging operation and used when producing its result.</param>
    private void HandleFirstChanceException(object? _, FirstChanceExceptionEventArgs eventArgs)
    {
        if (Interlocked.CompareExchange(ref handlingException, 1, 0) != 0)
            return;

        try
        {
            var exception = eventArgs.Exception;
            var stackTrace = exception.StackTrace ?? string.Empty;
            var applicationOwned = stackTrace.Contains("PublisherStudio.", StringComparison.Ordinal);
            var level = ResolveLogLevel(exception, applicationOwned);
            if (level is null)
                return;

            var callSite = ResolveCallSite(stackTrace);
            var fingerprint = $"{exception.GetType().FullName}|{callSite}";
            var occurrence = occurrences.GetOrAdd(fingerprint, _ => new ExceptionOccurrence(exception.GetType().FullName ?? exception.GetType().Name, callSite));
            var count = Interlocked.Increment(ref occurrence.Count);

            if (count <= options.DetailedOccurrencesPerCallSite)
            {
                logger.Log(
                    level.Value,
                    exception,
                    "First-chance exception observed in development. Classification: {Classification}; call site: {CallSite}; occurrence: {Occurrence}.",
                    applicationOwned ? "PublisherStudio" : "Framework lifecycle",
                    callSite,
                    count);
                return;
            }

            if (count % options.SummaryEveryOccurrences == 0)
            {
                logger.LogDebug(
                    "Repeated first-chance exception summary. Type: {ExceptionType}; call site: {CallSite}; observed occurrences: {Occurrence}; detailed logging is bounded.",
                    occurrence.ExceptionType,
                    occurrence.CallSite,
                    count);
            }
        }
        catch (Exception observerException)
        {
            Trace.TraceError($"PublisherStudio first-chance exception diagnostics failed: {observerException}");
        }
        finally
        {
            Volatile.Write(ref handlingException, 0);
        }
    }

    /// <summary>
    /// Resolves log level as part of the debug first chance exception logging service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="exception">Exception value supplied to the debug first chance exception logging operation and used when producing its result.</param>
    /// <param name="applicationOwned">Value indicating whether application owned should apply to this operation.</param>
    /// <returns>The log level produced by the operation.</returns>
    private LogLevel? ResolveLogLevel(Exception exception, bool applicationOwned)
    {
        if (applicationOwned)
        {
            return exception is OperationCanceledException or JSDisconnectedException or ObjectDisposedException
                ? LogLevel.Debug
                : LogLevel.Warning;
        }

        if (exception is InvalidOperationException && options.IncludeFrameworkInvalidOperationExceptions)
            return LogLevel.Debug;

        if (options.IncludeExpectedLifecycleExceptions
            && exception is (OperationCanceledException or JSDisconnectedException or ObjectDisposedException))
            return LogLevel.Debug;

        return null;
    }

    /// <summary>
    /// Resolves call site as part of the debug first chance exception logging service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="stackTrace">Stack trace value supplied to the debug first chance exception logging operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ResolveCallSite(string stackTrace)
    {
        if (string.IsNullOrWhiteSpace(stackTrace))
            return "stack unavailable";

        var lines = stackTrace.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return lines.FirstOrDefault(line => line.Contains("PublisherStudio.", StringComparison.Ordinal))
            ?? lines.FirstOrDefault()
            ?? "stack unavailable";
    }

    /// <summary>
    /// Validates options as part of the debug first chance exception logging service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void ValidateOptions()
    {
        if (options.DetailedOccurrencesPerCallSite < 1)
            throw new InvalidDataException("PublisherStudio debug exception diagnostics require at least one detailed occurrence per call site.");
        if (options.SummaryEveryOccurrences < 2)
            throw new InvalidDataException("PublisherStudio debug exception diagnostics require a repetition summary interval of at least two.");
    }

    /// <summary>
    /// Performs log final summaries as part of the debug first chance exception logging service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void LogFinalSummaries()
    {
        foreach (var occurrence in occurrences.Values.Where(value => value.Count > options.DetailedOccurrencesPerCallSite))
        {
            logger.LogDebug(
                "Development first-chance exception final summary. Type: {ExceptionType}; call site: {CallSite}; total occurrences: {Occurrence}.",
                occurrence.ExceptionType,
                occurrence.CallSite,
                occurrence.Count);
        }
    }

    /// <summary>
    /// Performs unsubscribe as part of the debug first chance exception logging service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    private void Unsubscribe()
    {
        if (!subscribed)
            return;

        AppDomain.CurrentDomain.FirstChanceException -= HandleFirstChanceException;
        subscribed = false;
    }

    /// <summary>
    /// Represents an exception occurrence helper type nested within <see cref="DebugFirstChanceExceptionLoggingHostedService"/>, grouping the state or behavior used only by that containing workflow.
    /// </summary>
    /// <param name="exceptionType">Exception type value supplied to the debug first chance exception logging operation and used when producing its result.</param>
    /// <param name="callSite">Call site value supplied to the debug first chance exception logging operation and used when producing its result.</param>
    private sealed class ExceptionOccurrence(string exceptionType, string callSite)
    {
        /// <summary>
        /// Gets the exception type value that forms part of the exception occurrence state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The exception type value exposed by <see cref="ExceptionOccurrence"/>.</value>
        internal string ExceptionType { get; } = exceptionType;
        /// <summary>
        /// Gets the call site value that forms part of the exception occurrence state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The call site value exposed by <see cref="ExceptionOccurrence"/>.</value>
        internal string CallSite { get; } = callSite;
        /// <summary>
        /// Stores the internal count state used by <see cref="ExceptionOccurrence"/> while executing its surrounding workflow.
        /// </summary>
        internal int Count;
    }
}
