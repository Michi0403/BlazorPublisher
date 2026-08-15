using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PublisherStudio.BusinessObjects;

namespace PublisherStudio.Services.Logging;

/// <summary>Provides PublisherStudio file logger instances to the Microsoft logging pipeline.</summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    /// <summary>Stores the options monitor supplied to newly created file loggers.</summary>
    private readonly IOptionsMonitor<FileLoggerCoreOptions> options;

    /// <summary>Tracks provider shutdown so no new category logger can be created after disposal begins.</summary>
    private bool disposed;

    /// <summary>Initializes a PublisherStudio file logger provider.</summary>
    /// <param name="options">Options monitor providing current file logger configuration.</param>
    public FileLoggerProvider(IOptionsMonitor<FileLoggerCoreOptions> options)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(options);
            this.options = options;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger provider initialization failed: {exception}");
            throw;
        }
    }

    /// <summary>Creates a file logger for the supplied logging category.</summary>
    /// <param name="categoryName">Logging category that will own the returned logger.</param>
    /// <returns>A PublisherStudio file logger.</returns>
    public ILogger CreateLogger(string categoryName)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return new FileLogger(categoryName, options);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger creation failed for category '{categoryName}': {exception}");
            throw;
        }
    }

    /// <summary>Releases resources owned by the provider.</summary>
    public void Dispose()
    {
        try
        {
            disposed = true;
            GC.SuppressFinalize(this);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"PublisherStudio file logger provider shutdown failed: {exception}");
        }
    }
}
