using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Logging;

/// <summary>
/// Writes PublisherStudio application log entries to a background file queue using the same runtime-directory fallback as LocalGPT.
/// </summary>
public sealed class FileLogger : ILogger, IDisposable
{
    /// <summary>Stores the resolved log path used by this logger instance.</summary>
    private readonly string realPath;

    /// <summary>Captures the provider severity and destination policy used for every event written by this logger instance.</summary>
    private readonly FileLoggerCoreOptions options;

    /// <summary>Stores pending formatted log messages until the writer thread persists them.</summary>
    private readonly BlockingCollection<string> logQueue = new();

    /// <summary>Owns the dedicated background writer that drains queued messages without blocking application callers.</summary>
    private readonly Thread loggingThread;

    /// <summary>Tracks shutdown state so producers stop enqueueing after the logging pipeline begins disposal.</summary>
    private bool disposed;

    /// <summary>Reuses one inert scope object for callers that request ILogger scopes even though file output is scope-agnostic.</summary>
    private readonly LoggerNullScope nullScope = new();

    /// <summary>Initializes a file logger for one logging category.</summary>
    /// <param name="categoryName">Logging category represented by this logger. The value is accepted for ILogger compatibility.</param>
    /// <param name="optionsMonitor">Options monitor providing the current file logger settings.</param>
    public FileLogger(string categoryName, IOptionsMonitor<FileLoggerCoreOptions> optionsMonitor)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(optionsMonitor);
            options = optionsMonitor.CurrentValue;
            realPath = string.IsNullOrWhiteSpace(options.FilePath)
                ? Path.Combine(Directory.GetCurrentDirectory(), "PublisherStudio.log")
                : Path.GetFullPath(Environment.ExpandEnvironmentVariables(options.FilePath));

            loggingThread = new Thread(ProcessLogQueue)
            {
                IsBackground = true,
                Name = "PublisherStudioFileLogger"
            };
            loggingThread.Start();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger initialization failed for category '{categoryName}': {exception}");
            throw;
        }
    }

    /// <summary>Begins an inert logging scope because the file provider does not persist scope objects separately.</summary>
    /// <typeparam name="TState">Type of caller-provided scope state.</typeparam>
    /// <param name="state">Caller-provided scope state.</param>
    /// <returns>An inert disposable scope.</returns>
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        try
        {
            return nullScope;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger scope creation failed: {exception}");
            throw;
        }
    }

    /// <summary>Compares an event severity with the configured file threshold while also honoring provider shutdown.</summary>
    /// <param name="logLevel">Severity being evaluated.</param>
    /// <returns><see langword="true"/> when the event should be written; otherwise <see langword="false"/>.</returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        try
        {
            return !disposed &&
                options.CoreLogLevel != BusinessObjects.Enums.CoreLogLevel.None &&
                (int)logLevel >= (int)options.CoreLogLevel;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger level evaluation failed: {exception}");
            return false;
        }
    }

    /// <summary>Formats and queues one application log event for asynchronous file persistence.</summary>
    /// <typeparam name="TState">Type of caller-provided log state.</typeparam>
    /// <param name="logLevel">Severity of the event.</param>
    /// <param name="eventId">Event identifier supplied by the logging caller.</param>
    /// <param name="state">Caller-provided logging state.</param>
    /// <param name="exception">Optional exception associated with the event.</param>
    /// <param name="formatter">Formatter used to produce the final event message.</param>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(formatter);
            if (!IsEnabled(logLevel)) return;

            var builder = new StringBuilder()
                .Append(DateTime.UtcNow.ToString("O"))
                .Append(" [Machine: ").Append(Environment.MachineName).Append(']')
                .Append(" [Level: ").Append(logLevel).Append("] ")
                .Append(formatter(state, exception));

            if (exception is not null)
                builder.AppendLine().Append("Exception: ").Append(exception);

            if (!logQueue.IsAddingCompleted)
                logQueue.Add(builder.ToString());
        }
        catch (InvalidOperationException)
        {
            // The queue may complete concurrently during application shutdown.
        }
        catch (Exception loggingException)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger failed to queue an event: {loggingException}");
        }
    }

    /// <summary>Consumes queued log messages and appends them to the configured file.</summary>
    private void ProcessLogQueue()
    {
        try
        {
            foreach (var message in logQueue.GetConsumingEnumerable())
            {
                try
                {
                    var directory = Path.GetDirectoryName(realPath);
                    if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    File.AppendAllText(realPath, message + Environment.NewLine, Encoding.UTF8);
                }
                catch (IOException exception)
                {
                    System.Diagnostics.Trace.TraceError($"PublisherStudio could not append to '{realPath}': {exception}");
                }
                catch (UnauthorizedAccessException exception)
                {
                    System.Diagnostics.Trace.TraceError($"PublisherStudio cannot write '{realPath}': {exception}");
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Trace.TraceError($"PublisherStudio file logger write failed: {exception}");
                }
            }
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger background writer failed: {exception}");
        }
    }

    /// <summary>Stops the background writer and releases the queue owned by this logger.</summary>
    public void Dispose()
    {
        try
        {
            if (disposed) return;
            disposed = true;
            logQueue.CompleteAdding();
            loggingThread.Join();
            logQueue.Dispose();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger shutdown failed: {exception}");
        }
    }
}
