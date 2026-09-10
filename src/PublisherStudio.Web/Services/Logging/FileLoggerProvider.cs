using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Logging;

/// <summary>Provides lightweight category loggers that share one PublisherStudio file writer.</summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    /// <summary>Owns the single queue and background writer used by every category logger.</summary>
    private readonly FileLoggerSharedSink sink;

    /// <summary>Tracks provider shutdown so no new category logger can be created after disposal begins.</summary>
    private bool disposed;

    /// <summary>Initializes a PublisherStudio file logger provider.</summary>
    /// <param name="options">Options monitor providing current file logger configuration.</param>
    public FileLoggerProvider(IOptionsMonitor<FileLoggerCoreOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        try
        {
            sink = new FileLoggerSharedSink(options);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger provider initialization failed: {exception}");
            throw;
        }
    }

    /// <summary>Creates a category facade over the provider-owned shared file sink.</summary>
    /// <param name="categoryName">Logging category represented by the returned logger.</param>
    /// <returns>A lightweight PublisherStudio logger.</returns>
    public ILogger CreateLogger(string categoryName)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return new FileLogger(categoryName, sink);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger creation failed for category '{categoryName}': {exception}");
            throw;
        }
    }

    /// <summary>
    /// Disposes the provider-owned shared file sink so queued entries finish through the single writer before its logging resources are released.
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (disposed)
                return;

            disposed = true;
            sink.Dispose();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger provider shutdown failed: {exception}");
        }
    }
}
