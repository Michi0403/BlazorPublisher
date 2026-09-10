using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Logging;

/// <summary>Provides a lightweight category facade over the provider-owned PublisherStudio file sink.</summary>
public sealed class FileLogger : ILogger
{
    /// <summary>Category attached to each persisted event.</summary>
    private readonly string categoryName;

    /// <summary>Shared provider-owned file sink.</summary>
    private readonly FileLoggerSharedSink sink;

    /// <summary>Reuses one inert scope object for callers that request ILogger scopes.</summary>
    private readonly LoggerNullScope nullScope = new();

    /// <summary>Initializes a category logger over the shared file sink.</summary>
    /// <param name="categoryName">Logging category represented by this logger for ILogger compatibility.</param>
    /// <param name="sink">Provider-owned sink shared by every category.</param>
    internal FileLogger(string categoryName, FileLoggerSharedSink sink)
    {
        this.categoryName = categoryName;
        this.sink = sink;
    }

    /// <summary>Begins an inert logging scope because file output is scope-agnostic.</summary>
    /// <typeparam name="TState">Type of caller-provided scope state.</typeparam>
    /// <param name="state">Caller-provided scope state.</param>
    /// <returns>An inert disposable scope.</returns>
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => nullScope;

    /// <summary>Reports whether the shared sink currently accepts the supplied severity.</summary>
    /// <param name="logLevel">Severity being evaluated.</param>
    /// <returns><see langword="true"/> when the event should be written.</returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        try
        {
            return sink.IsEnabled(logLevel);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger level evaluation failed: {exception}");
            return false;
        }
    }

    /// <summary>Formats and queues one application log event for shared asynchronous persistence.</summary>
    /// <typeparam name="TState">Type of caller-provided log state.</typeparam>
    /// <param name="logLevel">Severity of the event.</param>
    /// <param name="eventId">Event identifier supplied by the logging caller.</param>
    /// <param name="state">Caller-provided logging state.</param>
    /// <param name="exception">Optional exception associated with the event.</param>
    /// <param name="formatter">Formatter used to produce the final event message.</param>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        if (!sink.IsEnabled(logLevel))
            return;

        try
        {
            sink.Enqueue(logLevel, categoryName, formatter(state, exception), exception);
        }
        catch (Exception loggingException)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger failed to queue an event: {loggingException}");
        }
    }
}

/// <summary>
/// Owns one bounded queue and one file-writer thread for the complete PublisherStudio logging provider.
/// This prevents per-category writer threads, competing file opens, exception storms and the resulting allocation/GC pressure.
/// </summary>
internal sealed class FileLoggerSharedSink : IDisposable
{
    /// <summary>Pending formatted log entries shared by every category logger.</summary>
    private readonly BlockingCollection<string> logQueue = new(new ConcurrentQueue<string>(), 8192);

    /// <summary>Monitors runtime file logger options without creating per-category subscriptions.</summary>
    private readonly IOptionsMonitor<FileLoggerCoreOptions> optionsMonitor;

    /// <summary>Owns the only background file writer for this provider.</summary>
    private readonly Thread loggingThread;

    /// <summary>Absolute durable log path used by the writer.</summary>
    private readonly string realPath;

    /// <summary>Tracks sink shutdown.</summary>
    private bool disposed;

    /// <summary>Initializes the shared sink and starts its single background writer.</summary>
    /// <param name="optionsMonitor">Options monitor providing file logger policy.</param>
    internal FileLoggerSharedSink(IOptionsMonitor<FileLoggerCoreOptions> optionsMonitor)
    {
        this.optionsMonitor = optionsMonitor;
        realPath = ResolveLogPath(optionsMonitor.CurrentValue);
        EnsureLogFile();
        loggingThread = new Thread(ProcessLogQueue)
        {
            IsBackground = true,
            Name = "PublisherStudioFileLogger"
        };
        loggingThread.Start();
    }

    /// <summary>Reports whether the sink accepts an event at the supplied severity.</summary>
    /// <param name="logLevel">Severity being evaluated.</param>
    /// <returns><see langword="true"/> when persistence is enabled for the event.</returns>
    internal bool IsEnabled(LogLevel logLevel)
    {
        try
        {
            if (disposed)
                return false;

            var options = optionsMonitor.CurrentValue;
            return options.CoreLogLevel != BusinessObjects.Enums.CoreLogLevel.None
                && (int)logLevel >= (int)options.CoreLogLevel;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio shared file logger level evaluation failed: {exception}");
            return false;
        }
    }

    /// <summary>Formats and enqueues one log entry without blocking on file I/O.</summary>
    /// <param name="logLevel">Event severity.</param>
    /// <param name="categoryName">Logging category.</param>
    /// <param name="message">Formatted message.</param>
    /// <param name="exception">Optional associated exception.</param>
    internal void Enqueue(LogLevel logLevel, string categoryName, string message, Exception? exception)
    {
        try
        {
            if (disposed || logQueue.IsAddingCompleted)
                return;

            var builder = new StringBuilder(256)
                .Append(DateTime.UtcNow.ToString("O"))
                .Append(" [Machine: ").Append(Environment.MachineName).Append(']')
                .Append(" [Level: ").Append(logLevel).Append(']')
                .Append(" [Category: ").Append(categoryName).Append("] ")
                .Append(message);

            if (exception is not null)
                builder.AppendLine().Append("Exception: ").Append(exception);

            // Never let logging back-pressure stall the renderer. Under an extreme burst, drop the
            // newest file-only entry; console/debug providers still receive the original event.
            logQueue.TryAdd(builder.ToString());
        }
        catch (InvalidOperationException)
        {
            // Shutdown can complete the queue between the state check and TryAdd.
        }
        catch (Exception enqueueException)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio shared file logger enqueue failed: {enqueueException}");
        }
    }

    /// <summary>Creates the durable log directory and file before the writer thread starts.</summary>
    private void EnsureLogFile()
    {
        try
        {
            var directory = Path.GetDirectoryName(realPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            using (File.Open(realPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) { }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            System.Diagnostics.Trace.TraceWarning($"PublisherStudio could not pre-create log file '{realPath}': {exception.Message}");
        }
    }

    /// <summary>Consumes the shared queue and persists entries serially.</summary>
    private void ProcessLogQueue()
    {
        try
        {
            var directory = Path.GetDirectoryName(realPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            using var stream = new FileStream(
                realPath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 16 * 1024,
                FileOptions.SequentialScan);
            using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 16 * 1024)
            {
                AutoFlush = true
            };

            foreach (var message in logQueue.GetConsumingEnumerable())
                writer.WriteLine(message);
        }
        catch (IOException exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio could not keep its shared log writer open for '{realPath}': {exception}");
        }
        catch (UnauthorizedAccessException exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio cannot write '{realPath}': {exception}");
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio shared file logger background writer failed: {exception}");
        }
    }

    /// <summary>Resolves the log target to durable per-user storage while honoring safe absolute overrides.</summary>
    /// <param name="currentOptions">Current file logger options.</param>
    /// <returns>An absolute log path independent of the process working directory.</returns>
    private string ResolveLogPath(FileLoggerCoreOptions currentOptions)
    {
        try
        {
            var defaultPath = PublisherApplicationDataPaths.ResolveUserPath("PublisherStudio.log");
            var configured = currentOptions.FilePath?.Trim();
            if (string.IsNullOrWhiteSpace(configured))
                return defaultPath;

            configured = Environment.ExpandEnvironmentVariables(configured);
            if (!Path.IsPathRooted(configured))
                return PublisherApplicationDataPaths.ResolveUserPath(configured);

            var fullConfigured = Path.GetFullPath(configured);
            var applicationRoot = Path.GetFullPath(AppContext.BaseDirectory);
            var relativeToApplication = Path.GetRelativePath(applicationRoot, fullConfigured);
            var outsideApplication = relativeToApplication.Equals("..", StringComparison.Ordinal)
                || relativeToApplication.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || Path.IsPathRooted(relativeToApplication);
            return outsideApplication ? fullConfigured : defaultPath;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            System.Diagnostics.Trace.TraceWarning($"PublisherStudio could not resolve its normal log path: {exception.Message}");
            return Path.Combine(Path.GetTempPath(), "PublisherStudio", "PublisherStudio.log");
        }
    }

    /// <summary>
    /// Completes the shared log queue, gives the provider-owned writer thread an opportunity to drain pending entries, and then releases queue resources.
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (disposed)
                return;

            disposed = true;
            logQueue.CompleteAdding();
            if (!loggingThread.Join(TimeSpan.FromSeconds(5)))
                System.Diagnostics.Trace.TraceWarning("PublisherStudio file logger writer did not stop within five seconds.");
            logQueue.Dispose();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio shared file logger shutdown failed: {exception}");
        }
    }
}
