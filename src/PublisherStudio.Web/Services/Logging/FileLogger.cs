using Microsoft.Extensions.Logging;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Logging;

/// <summary>Writes category events through the DI-owned PublisherStudio file-log provider.</summary>
internal sealed class FileLogger(string categoryName, FileLoggerProvider provider) : ILogger
{
    private readonly IDisposable nullScope = new NullScope();

    private sealed class NullScope : IDisposable
    {
        public void Dispose()
        {
            try
            {
                // ILogger scopes are intentionally not persisted by the file provider.
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.TraceError($"PublisherStudio null logger scope disposal failed: {exception}");
            }
        }
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        try
        {
            return nullScope;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger scope creation failed: {exception}");
            return nullScope;
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        try
        {
            return provider.IsEnabled(logLevel);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger level evaluation failed: {exception}");
            return false;
        }
    }

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

            provider.Enqueue(new ApplicationLogEntry
            {
                TimestampUtc = DateTime.UtcNow,
                Level = logLevel.ToString(),
                LogLevelValue = (int)logLevel,
                Category = categoryName,
                EventId = eventId.Id,
                EventName = eventId.Name,
                Message = formatter(state, exception),
                ExceptionType = exception?.GetType().FullName,
                ExceptionMessage = exception?.Message,
                ExceptionStackTrace = exception?.StackTrace,
                Exception = exception?.ToString(),
                MachineName = Environment.MachineName,
                ProcessId = Environment.ProcessId,
                ThreadId = Environment.CurrentManagedThreadId
            });
        }
        catch (Exception loggingException)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger failed to enqueue an event: {loggingException}");
        }
    }
}
