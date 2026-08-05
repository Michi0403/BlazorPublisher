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
public sealed class DebugFirstChanceExceptionLoggingHostedService(
    IHostEnvironment environment,
    IOptions<DebugExceptionDiagnosticsOptions> configuredOptions,
    ILogger<DebugFirstChanceExceptionLoggingHostedService> logger) : IHostedService, IDisposable
{
    private readonly ConcurrentDictionary<string, ExceptionOccurrence> occurrences = new(StringComparer.Ordinal);
    private readonly DebugExceptionDiagnosticsOptions options = configuredOptions.Value;
    private int handlingException;
    private bool subscribed;

    /// <summary>
    /// Starts development exception observation when configured.
    /// </summary>
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
    /// Stops development exception observation and records bounded repetition summaries.
    /// </summary>
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

    private string ResolveCallSite(string stackTrace)
    {
        if (string.IsNullOrWhiteSpace(stackTrace))
            return "stack unavailable";

        var lines = stackTrace.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return lines.FirstOrDefault(line => line.Contains("PublisherStudio.", StringComparison.Ordinal))
            ?? lines.FirstOrDefault()
            ?? "stack unavailable";
    }

    private void ValidateOptions()
    {
        if (options.DetailedOccurrencesPerCallSite < 1)
            throw new InvalidDataException("PublisherStudio debug exception diagnostics require at least one detailed occurrence per call site.");
        if (options.SummaryEveryOccurrences < 2)
            throw new InvalidDataException("PublisherStudio debug exception diagnostics require a repetition summary interval of at least two.");
    }

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

    private void Unsubscribe()
    {
        if (!subscribed)
            return;

        AppDomain.CurrentDomain.FirstChanceException -= HandleFirstChanceException;
        subscribed = false;
    }

    private sealed class ExceptionOccurrence(string exceptionType, string callSite)
    {
        internal string ExceptionType { get; } = exceptionType;
        internal string CallSite { get; } = callSite;
        internal int Count;
    }
}
