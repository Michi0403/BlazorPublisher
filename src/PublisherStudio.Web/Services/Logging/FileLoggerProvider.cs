using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Logging;

/// <summary>Provides a bounded, DI-owned PublisherStudio application file log.</summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, FileLogger> loggers = new(StringComparer.Ordinal);
    private readonly BlockingCollection<ApplicationLogEntry> queue;
    private readonly Thread writerThread;
    private readonly FileLoggerCoreOptions options;
    private readonly string path;
    private int disposed;

    public FileLoggerProvider(IOptionsMonitor<FileLoggerCoreOptions> optionsMonitor)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(optionsMonitor);
            options = optionsMonitor.CurrentValue;
            path = options.ResolvePath();
            queue = new BlockingCollection<ApplicationLogEntry>(Math.Max(128, options.MaxQueueLength));
            writerThread = new Thread(ProcessQueue)
            {
                IsBackground = true,
                Name = "PublisherStudioFileLogger"
            };
            writerThread.Start();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger provider initialization failed: {exception}");
            throw;
        }
    }

    /// <summary>Resolved log-file path used by this provider.</summary>
    public string FilePath => path;

    public ILogger CreateLogger(string categoryName)
    {
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
            return loggers.GetOrAdd(categoryName, (name, provider) => new FileLogger(name, provider), this);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger creation failed: {exception}");
            throw;
        }
    }

    internal bool IsEnabled(LogLevel level)
    {
        try
        {
            return Volatile.Read(ref disposed) == 0 &&
                options.CoreLogLevel != BusinessObjects.Enums.CoreLogLevel.None &&
                (int)level >= (int)options.CoreLogLevel;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger provider level evaluation failed: {exception}");
            return false;
        }
    }

    internal void Enqueue(ApplicationLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (!IsEnabled((LogLevel)entry.LogLevelValue) || queue.IsAddingCompleted) return;
        try
        {
            if (!queue.TryAdd(entry))
                System.Diagnostics.Trace.TraceWarning("PublisherStudio file-log queue is full; one log event was dropped.");
        }
        catch (InvalidOperationException)
        {
            // Provider is completing during application shutdown.
        }
        catch (Exception loggingException)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger enqueue failed: {loggingException}");
        }
    }

    private void ProcessQueue()
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            foreach (var entry in queue.GetConsumingEnumerable())
            {
                try
                {
                    File.AppendAllText(path, FormatEntry(entry) + Environment.NewLine, Encoding.UTF8);
                }
                catch (IOException exception)
                {
                    System.Diagnostics.Trace.TraceError($"PublisherStudio could not append to '{path}': {exception}");
                }
                catch (UnauthorizedAccessException exception)
                {
                    System.Diagnostics.Trace.TraceError($"PublisherStudio cannot write '{path}': {exception}");
                }
            }
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger background writer failed: {exception}");
        }
    }

    private string FormatEntry(ApplicationLogEntry entry)
    {
        try
        {
            var builder = new StringBuilder(320)
                .Append(entry.TimestampUtc.ToString("O"))
                .Append(" [Machine: ").Append(entry.MachineName).Append(']')
                .Append(" [Process: ").Append(entry.ProcessId).Append(']')
                .Append(" [Thread: ").Append(entry.ThreadId).Append(']')
                .Append(" [Level: ").Append(entry.Level).Append(']')
                .Append(" [Category: ").Append(entry.Category).Append(']')
                .Append(" [EventId: ").Append(entry.EventId);
            if (!string.IsNullOrWhiteSpace(entry.EventName)) builder.Append('/').Append(entry.EventName);
            builder.Append("] ").Append(entry.Message);
            if (!string.IsNullOrWhiteSpace(entry.Exception))
                builder.AppendLine().Append("Exception: ").Append(entry.Exception);
            return builder.ToString();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file log formatting failed: {exception}");
            return $"{entry.TimestampUtc:O} [Level: {entry.Level}] [Category: {entry.Category}] {entry.Message}";
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        try
        {
            queue.CompleteAdding();
            if (!writerThread.Join(TimeSpan.FromSeconds(5)))
                System.Diagnostics.Trace.TraceWarning("PublisherStudio file logger did not drain within five seconds during shutdown.");
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger shutdown failed: {exception}");
        }
        finally
        {
            queue.Dispose();
            loggers.Clear();
        }
    }
}
